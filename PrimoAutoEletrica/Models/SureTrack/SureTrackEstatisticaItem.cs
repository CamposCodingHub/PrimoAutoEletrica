using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models.SureTrack
{
    public class SureTrackEstatisticaItem
    {
        public string CausaRaiz { get; set; } = string.Empty;
        public int TotalOcorrencias { get; set; }
        public double PercentualProbabilidade { get; set; } // ex: 64.0 para 64%
        public List<string> PecasMaisFrequentes { get; set; } = new();
        public string ProcedimentoRecomendado { get; set; } = string.Empty;
        public string DicaTesteRapido { get; set; } = string.Empty;
        public string CorBarra { get; set; } = "#3B82F6"; // Cor visual para gráfico
        public string PercentualFormatado => $"{PercentualProbabilidade:F0}%";
        public string OcorrenciasFormatado => $"{TotalOcorrencias} {(TotalOcorrencias == 1 ? "caso" : "casos")}";
    }

    public class SureTrackConsultaResultado
    {
        public string TermoConsulta { get; set; } = string.Empty;
        public int TotalCasosAnalisados { get; set; }
        public List<SureTrackEstatisticaItem> Estatisticas { get; set; } = new();
        public List<string> DicasAtalho15Min { get; set; } = new();
        public List<CasoResolvidoSureTrack> CasosIndividuais { get; set; } = new();
        public SureTrackEstatisticaItem? CausaMaisProvavel => Estatisticas.Count > 0 ? Estatisticas[0] : null;

        public string ResumoEstatisticoFormatado
        {
            get
            {
                if (TotalCasosAnalisados == 0)
                {
                    return "Nenhum caso empírico catalogado para esta consulta.";
                }

                var linhas = new List<string>
                {
                    $"📊 **Histórico de Casos Confirmados (Base da Oficina - {TotalCasosAnalisados} ocorrências):**"
                };

                foreach (var est in Estatisticas)
                {
                    var pecasInfo = est.PecasMaisFrequentes.Count > 0
                        ? $" (Peças: {string.Join(", ", est.PecasMaisFrequentes)})"
                        : string.Empty;
                    linhas.Add($"• **{est.PercentualFormatado}** das vezes: {est.CausaRaiz} ({est.OcorrenciasFormatado}){pecasInfo}");
                }

                if (DicasAtalho15Min.Count > 0)
                {
                    linhas.Add("\n⏱️ **Dica de 15 Minutos (Atalho do Eletricista Sênior):**");
                    foreach (var dica in DicasAtalho15Min)
                    {
                        linhas.Add($"👉 {dica}");
                    }
                }

                return string.Join("\n", linhas);
            }
        }
    }
}
