using System;

namespace PrimoAutoEletrica.Models.DiagnosticoGuiado
{
    /// <summary>
    /// Parâmetros de entrada para o cálculo de queda de tensão (Voltage Drop Analysis).
    /// </summary>
    public class ParametrosCalculoQuedaTensao
    {
        public double TensaoFonteVolts { get; set; } = 12.60;
        public double TensaoCargaVolts { get; set; } = 12.40;
        public double CorrenteAmperes { get; set; } = 10.0;
        public double ComprimentoCaboMetros { get; set; } = 2.0;
        public double SecaoCaboMm2 { get; set; } = 2.5;
        public string MaterialCondutor { get; set; } = "Cobre";
        public string TipoCircuito { get; set; } = "Potência (SAE J1128)"; // Potência (SAE J1128), Aterramento / Massa, Sinal / Sensores, Linha Pesada 24V
        public string? IdentificacaoCircuito { get; set; } = "Circuito de Carga / Alimentação";
        public string? VeiculoPlaca { get; set; }
    }

    /// <summary>
    /// Status normativo de conformidade de queda de tensão segundo padrões automotivos SAE J1128 e DIN 72551.
    /// </summary>
    public enum StatusConformidadeQuedaTensao
    {
        Conforme = 1,          // Dentro do limite ótimo
        Toleravel = 2,         // Próximo ao limiar máximo tolerado
        NaoConformeCritico = 3  // Queda excessiva, risco de falha ou superaquecimento
    }

    /// <summary>
    /// Resultado consolidado do cálculo de queda de tensão e análise da resistência parasita.
    /// </summary>
    public class ResultadoQuedaTensao
    {
        public double TensaoFonteVolts { get; set; }
        public double TensaoCargaVolts { get; set; }
        public double CorrenteAmperes { get; set; }
        public double QuedaTensaoVolts { get; set; }
        public double PercentualQueda { get; set; }
        public double ResistenciaParasitaOhms { get; set; }
        public double PotenciaDissipadaWatts { get; set; }
        public double LimiteMaximoToleradoVolts { get; set; }
        public StatusConformidadeQuedaTensao StatusConformidade { get; set; } = StatusConformidadeQuedaTensao.Conforme;
        public string DiagnosticoTecnico { get; set; } = string.Empty;
        public string AcaoRecomendada { get; set; } = string.Empty;
        public double SecaoMinimaRecomendadaMm2 { get; set; }
        public DateTime DataCalculo { get; set; } = DateTime.Now;

        public string ObterResumoFormatado()
        {
            var statusBadge = StatusConformidade switch
            {
                StatusConformidadeQuedaTensao.Conforme => "✅ CONFORME (SAE/DIN)",
                StatusConformidadeQuedaTensao.Toleravel => "⚠️ TOLERÁVEL (Limiar Máximo)",
                _ => "❌ NÃO CONFORME (Queda Crítica)"
            };

            return $"Queda de Tensão: {QuedaTensaoVolts:F2}V ({PercentualQueda:F1}%) | Status: {statusBadge}\n" +
                   $"Resistência Parasita: {ResistenciaParasitaOhms:F4} Ω | Calor Dissipado: {PotenciaDissipadaWatts:F2} W\n" +
                   $"Limite Tolerado: {LimiteMaximoToleradoVolts:F2}V | Bitola Mínima Sugerida: {SecaoMinimaRecomendadaMm2:F2} mm²\n" +
                   $"Diagnóstico: {DiagnosticoTecnico}\n" +
                   $"Ação: {AcaoRecomendada}";
        }
    }

    /// <summary>
    /// Registro histórico persistido no SQLite de cálculos de queda de tensão realizados na oficina.
    /// </summary>
    public class HistoricoCalculoQuedaTensao
    {
        public int Id { get; set; }
        public string DataHora { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string IdentificacaoCircuito { get; set; } = string.Empty;
        public string? VeiculoPlaca { get; set; }
        public string TipoCircuito { get; set; } = "Potência";
        public double TensaoFonteVolts { get; set; }
        public double TensaoCargaVolts { get; set; }
        public double CorrenteAmperes { get; set; }
        public double QuedaTensaoVolts { get; set; }
        public double ResistenciaParasitaOhms { get; set; }
        public double PotenciaDissipadaWatts { get; set; }
        public string StatusConformidade { get; set; } = "Conforme";
        public string DiagnosticoTecnico { get; set; } = string.Empty;
        public string AcaoRecomendada { get; set; } = string.Empty;
    }
}
