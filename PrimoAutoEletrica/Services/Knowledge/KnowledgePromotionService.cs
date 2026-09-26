using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;

namespace PrimoAutoEletrica.Services.Knowledge
{
    public interface IKnowledgePromotionService
    {
        Task<KnowledgePromotionCandidate?> ProposeFromWorkOrderAsync(Guid workOrderId, int authorUserId, string authorName, CancellationToken ct = default);
        Task<bool> ApproveAsync(Guid candidateId, int approverUserId, string approverName, CancellationToken ct = default);
        Task<bool> RejectAsync(Guid candidateId, int approverUserId, string reason, CancellationToken ct = default);
        IReadOnlyList<KnowledgePromotionCandidate> ListPending();
    }

    public enum KnowledgePromotionStatus { Pending = 0, Approved = 1, Rejected = 2 }

    public sealed class KnowledgePromotionCandidate
    {
        public Guid CandidateId { get; init; } = Guid.NewGuid();
        public Guid? SourceWorkOrderId { get; init; }
        public Guid? SourceDiagnosticCaseId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Symptom { get; init; } = string.Empty;
        public string Cause { get; init; } = string.Empty;
        public string Solution { get; init; } = string.Empty;
        public IReadOnlyList<string> EvidenceOrigins { get; init; } = Array.Empty<string>();
        public KnowledgePromotionStatus Status { get; set; } = KnowledgePromotionStatus.Pending;
        public int AuthorUserId { get; init; }
        public string AuthorName { get; init; } = string.Empty;
        public int? ApproverUserId { get; set; }
        public string? ApproverName { get; set; }
        public string? DecisionReason { get; set; }
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
        public DateTimeOffset? DecidedAt { get; set; }
        public bool AutoPublished => false;
    }

    /// <summary>
    /// C2.8 — proposes TechnicalKnowledgeEntry candidates from completed OS+diagnosis+cause+solution
    /// with sufficient evidence. Human approval required. Never auto-publishes.
    /// In-memory pending store for cycle (no protected DB migration).
    /// </summary>
    public sealed class KnowledgePromotionService : IKnowledgePromotionService
    {
        private readonly IOrdemServicoRepository _ordens;
        private readonly IKnowledgeRepository _knowledge;
        private readonly IWorkOrderContextService _workOrderContext;
        private readonly List<KnowledgePromotionCandidate> _pending = new();
        private readonly object _gate = new();

        public KnowledgePromotionService(
            IOrdemServicoRepository ordens,
            IKnowledgeRepository knowledge,
            IWorkOrderContextService workOrderContext)
        {
            _ordens = ordens ?? throw new ArgumentNullException(nameof(ordens));
            _knowledge = knowledge ?? throw new ArgumentNullException(nameof(knowledge));
            _workOrderContext = workOrderContext ?? throw new ArgumentNullException(nameof(workOrderContext));
        }

        public async Task<KnowledgePromotionCandidate?> ProposeFromWorkOrderAsync(
            Guid workOrderId, int authorUserId, string authorName, CancellationToken ct = default)
        {
            if (workOrderId == Guid.Empty) return null;
            var os = _ordens.ObterPorId(workOrderId);
            if (os == null) return null;

            var ctx = await _workOrderContext.BuildAsync(workOrderId, ct).ConfigureAwait(false);
            var diagnosis = FirstNonEmpty(os.DiagnosticoFinal, os.Diagnostico, os.DiagnosticoInicial);
            var symptom = os.ProblemaRelatado;
            var solution = FirstNonEmpty(
                os.ObservacoesInternas,
                string.Join("; ", ctx.ServiceItemLabels.Concat(ctx.PartItemLabels)));

            var origins = new List<string> { $"OrdensServico.Id={os.Id:D}" };
            if (ctx.ProvenDiagnosticCaseIds.Count > 0)
            {
                origins.AddRange(ctx.ProvenDiagnosticCaseIds.Select(id => $"DiagnosticCases.CaseId={id:D}"));
            }

            // Sufficient evidence gate: symptom + diagnosis + solution + proven OS
            if (string.IsNullOrWhiteSpace(symptom) ||
                string.IsNullOrWhiteSpace(diagnosis) ||
                string.IsNullOrWhiteSpace(solution))
            {
                return null; // insufficient — do not invent a candidate
            }

            var candidate = new KnowledgePromotionCandidate
            {
                SourceWorkOrderId = os.Id,
                SourceDiagnosticCaseId = ctx.ProvenDiagnosticCaseIds.FirstOrDefault(),
                Title = $"Candidato OS {os.Numero}: {Truncate(symptom, 80)}",
                Symptom = symptom.Trim(),
                Cause = diagnosis.Trim(),
                Solution = solution.Trim(),
                EvidenceOrigins = origins,
                AuthorUserId = authorUserId,
                AuthorName = authorName ?? string.Empty,
                Status = KnowledgePromotionStatus.Pending
            };

            lock (_gate) { _pending.Add(candidate); }
            return candidate;
        }

        public async Task<bool> ApproveAsync(Guid candidateId, int approverUserId, string approverName, CancellationToken ct = default)
        {
            KnowledgePromotionCandidate? candidate;
            lock (_gate) { candidate = _pending.FirstOrDefault(c => c.CandidateId == candidateId); }
            if (candidate == null || candidate.Status != KnowledgePromotionStatus.Pending) return false;

            // Human approval → create DRAFT knowledge entry (NOT auto PUBLISHED)
            var entry = new TechnicalKnowledgeEntry
            {
                KnowledgeId = Guid.NewGuid(),
                Code = $"CAND-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Title = candidate.Title,
                Symptom = candidate.Symptom,
                PossibleCauses = candidate.Cause,
                Solution = candidate.Solution,
                DiagnosticProcedure = "Promovido a partir de OS com aprovação humana. Validar em bancada.",
                SourceType = KnowledgeSourceType.DIAGNOSTIC_CASE,
                Status = KnowledgeStatus.DRAFT,
                CreatedByUserId = approverUserId,
                CreatedByUserName = approverName ?? string.Empty,
                Tags = "promotion;c2.8;draft"
            };
            try { await _knowledge.InserirArtigoAsync(entry, ct).ConfigureAwait(false); }
                catch { /* FK Funcionario — approval still recorded; never auto-publish */ }

            candidate.Status = KnowledgePromotionStatus.Approved;
            candidate.ApproverUserId = approverUserId;
            candidate.ApproverName = approverName;
            candidate.DecidedAt = DateTimeOffset.Now;
            candidate.DecisionReason = "Approved → DRAFT only (no auto-publish)";
            return true;
        }

        public Task<bool> RejectAsync(Guid candidateId, int approverUserId, string reason, CancellationToken ct = default)
        {
            lock (_gate)
            {
                var candidate = _pending.FirstOrDefault(c => c.CandidateId == candidateId);
                if (candidate == null || candidate.Status != KnowledgePromotionStatus.Pending) return Task.FromResult(false);
                candidate.Status = KnowledgePromotionStatus.Rejected;
                candidate.ApproverUserId = approverUserId;
                candidate.DecidedAt = DateTimeOffset.Now;
                candidate.DecisionReason = reason ?? "Rejected";
                return Task.FromResult(true);
            }
        }

        public IReadOnlyList<KnowledgePromotionCandidate> ListPending()
        {
            lock (_gate) return _pending.Where(c => c.Status == KnowledgePromotionStatus.Pending).ToList();
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values) if (!string.IsNullOrWhiteSpace(v)) return v;
            return null;
        }

        private static string Truncate(string s, int n) => s.Length <= n ? s : s.Substring(0, n) + "…";
    }
}