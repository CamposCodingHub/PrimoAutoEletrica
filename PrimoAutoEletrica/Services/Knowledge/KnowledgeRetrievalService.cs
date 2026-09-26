using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;

namespace PrimoAutoEletrica.Services.Knowledge
{
    public interface IKnowledgeRetrievalService
    {
        DeterministicKnowledgeIndex Index { get; }
        Task<int> RebuildIndexAsync(CancellationToken ct = default);
        Task<KnowledgeSearchResult> SearchAsync(string query, int maxResults = 20, KnowledgeType? typeFilter = null, CancellationToken ct = default);
        Task<KnowledgeSearchResult> SearchAsync(KnowledgeSearchQuery query, CancellationToken ct = default);
        IReadOnlyList<KnowledgeItem> GetIndexedProceduresD01ToD17();
    }

    /// <summary>
    /// Retrieval C2.1: adapta KnowledgeRepository + roteiros D01–D17 (+ biblioteca) no índice determinístico.
    /// Não cria tabelas novas; não toca DB protegido. Roteiros via CreateRoteirosDiagnostico() (sem App.Database).
    /// </summary>
    public sealed class KnowledgeRetrievalService : IKnowledgeRetrievalService
    {
        private readonly IKnowledgeRepository _knowledgeRepository;
        private readonly IOrdemServicoRepository? _ordemServicoRepository;
        private readonly LoggerService? _logger;
        private readonly DeterministicKnowledgeIndex _index = new();
        private readonly object _rebuildGate = new();

        public DeterministicKnowledgeIndex Index => _index;

        public KnowledgeRetrievalService(
            IKnowledgeRepository knowledgeRepository,
            IOrdemServicoRepository? ordemServicoRepository = null,
            LoggerService? logger = null)
        {
            _knowledgeRepository = knowledgeRepository ?? throw new ArgumentNullException(nameof(knowledgeRepository));
            _ordemServicoRepository = ordemServicoRepository;
            _logger = logger;
        }

        public async Task<int> RebuildIndexAsync(CancellationToken ct = default)
        {
            lock (_rebuildGate)
            {
                _index.Clear();
            }

            var items = new List<KnowledgeItem>();

            // 1) Roteiros D01–D17 (fonte canônica — prova 17/17)
            foreach (var r in AutoEletricaTecnicaService.CreateRoteirosDiagnostico())
            {
                items.Add(KnowledgeItemAdapters.FromDiagnosticRoteiro(r));
            }

            // 2) Biblioteca técnica
            var biblioteca = AutoEletricaTecnicaService.CreateBibliotecaTecnica();
            for (var i = 0; i < biblioteca.Count; i++)
            {
                items.Add(KnowledgeItemAdapters.FromBibliotecaItem(biblioteca[i], i + 1));
            }

            // 3) Artigos técnicos SQLite
            try
            {
                var artigos = await _knowledgeRepository.ObterArtigosAsync(status: KnowledgeStatus.PUBLISHED, ct: ct).ConfigureAwait(false);
                foreach (var a in artigos)
                {
                    items.Add(KnowledgeItemAdapters.FromTechnicalKnowledge(a));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"KnowledgeRetrieval: falha ao carregar artigos técnicos: {ex.Message}");
            }

            // 4) Casos de diagnóstico SQLite
            try
            {
                var casos = await _knowledgeRepository.ObterCasosAsync(ct: ct).ConfigureAwait(false);
                foreach (var c in casos)
                {
                    items.Add(KnowledgeItemAdapters.FromDiagnosticCase(c));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"KnowledgeRetrieval: falha ao carregar casos diagnósticos: {ex.Message}");
            }

            // 5) OS recentes (opcional)
            if (_ordemServicoRepository != null)
            {
                try
                {
                    var ordens = _ordemServicoRepository.ObterTodos()?.Take(50);
                    if (ordens != null)
                    {
                        foreach (var os in ordens)
                        {
                            var mapped = KnowledgeItemAdapters.FromWorkOrderSnapshot(os);
                            if (mapped != null) items.Add(mapped);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"KnowledgeRetrieval: falha ao indexar OS: {ex.Message}");
                }
            }

            lock (_rebuildGate)
            {
                _index.UpsertMany(items);
                return _index.Count;
            }
        }

        public Task<KnowledgeSearchResult> SearchAsync(string query, int maxResults = 20, KnowledgeType? typeFilter = null, CancellationToken ct = default)
        {
            return SearchAsync(new KnowledgeSearchQuery
            {
                Text = query,
                MaxResults = maxResults,
                TypeFilter = typeFilter
            }, ct);
        }

        public async Task<KnowledgeSearchResult> SearchAsync(KnowledgeSearchQuery query, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            ct.ThrowIfCancellationRequested();

            if (_index.Count == 0)
            {
                await RebuildIndexAsync(ct).ConfigureAwait(false);
            }

            return _index.Search(query);
        }

        public IReadOnlyList<KnowledgeItem> GetIndexedProceduresD01ToD17()
        {
            return _index.SnapshotItems()
                .Where(i => i.Type == KnowledgeType.PROCEDURE
                            && i.Code.Length == 3
                            && i.Code.StartsWith("D", StringComparison.OrdinalIgnoreCase)
                            && int.TryParse(i.Code.AsSpan(1), out var n)
                            && n >= 1 && n <= 17)
                .OrderBy(i => i.Code, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
