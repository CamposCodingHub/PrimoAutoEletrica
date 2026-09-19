using System;
using System.Linq;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public sealed class GarantiaAlerta
    {
        public string NumeroOs { get; set; } = string.Empty;
        public DateTime? Validade { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }

    public sealed class GarantiaRetornoService
    {
        public GarantiaAlerta? VerificarRetornoEmGarantia(Guid? veiculoId, string? sintomaOuDefeito = null)
        {
            if (!veiculoId.HasValue || veiculoId == Guid.Empty)
            {
                return null;
            }

            var ordens = App.Repositories.OrdensServico.ObterTodos()
                .Where(o => o.Ativo
                            && o.VeiculoId == veiculoId
                            && o.GarantiaValidaAte.HasValue
                            && o.GarantiaValidaAte.Value.Date >= DateTime.Today)
                .OrderByDescending(o => o.GarantiaValidaAte)
                .ToList();

            if (ordens.Count == 0)
            {
                return null;
            }

            var alvo = ordens.First();
            if (!string.IsNullOrWhiteSpace(sintomaOuDefeito))
            {
                var termo = sintomaOuDefeito.Trim();
                var match = ordens.FirstOrDefault(o =>
                    Contains(o.ProblemaRelatado, termo)
                    || Contains(o.Diagnostico, termo)
                    || Contains(o.DiagnosticoFinal, termo)
                    || Contains(o.GarantiaObservacoes, termo));
                if (match != null)
                {
                    alvo = match;
                }
            }

            return new GarantiaAlerta
            {
                NumeroOs = alvo.Numero,
                Validade = alvo.GarantiaValidaAte,
                Mensagem = $"Veiculo com garantia ativa ate {alvo.GarantiaValidaAte:dd/MM/yyyy} (OS {alvo.Numero}). Avalie se e retorno/reincidencia."
            };
        }

        private static bool Contains(string? haystack, string needle) =>
            !string.IsNullOrWhiteSpace(haystack)
            && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }
}
