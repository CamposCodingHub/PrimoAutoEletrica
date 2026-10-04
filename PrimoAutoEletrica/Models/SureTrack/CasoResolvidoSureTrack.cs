using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PrimoAutoEletrica.Models.SureTrack
{
    public class CasoResolvidoSureTrack
    {
        public int Id { get; set; }
        public string? OrdemServicoOrigemId { get; set; }
        public string? OrdemServicoOrigemNumero { get; set; }
        public string Montadora { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Motorizacao { get; set; } = string.Empty;
        public int Ano { get; set; }
        public string SintomaPrincipal { get; set; } = string.Empty;
        public string CodigosDTC { get; set; } = string.Empty; // ex: "P0300, P0304"
        public string CausaRaizDetectada { get; set; } = string.Empty;
        public string ProcedimentoSolucao { get; set; } = string.Empty;
        public string PecasSubstituidasJson { get; set; } = "[]";
        public string DicaTesteRapido { get; set; } = string.Empty; // 15-Minute Short-Cut tip
        public DateTime DataResolucao { get; set; } = DateTime.Now;
        public int OcorrenciasConfirmadas { get; set; } = 1;
        public string OrigemCaso { get; set; } = "OficinaLocal"; // OficinaLocal, RedeHomologada

        public string VeiculoFormatado => $"{Montadora} {Modelo} {Motorizacao} ({Ano})".Trim();

        public List<string> ObterPecasSubstituidas()
        {
            if (string.IsNullOrWhiteSpace(PecasSubstituidasJson)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(PecasSubstituidasJson) ?? new List<string>();
            }
            catch
            {
                return new List<string> { PecasSubstituidasJson };
            }
        }

        public void DefinirPecasSubstituidas(IEnumerable<string> pecas)
        {
            PecasSubstituidasJson = JsonSerializer.Serialize(pecas ?? Array.Empty<string>());
        }
    }
}
