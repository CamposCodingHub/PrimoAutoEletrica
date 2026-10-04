using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models.DiagnosticoGuiado;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public class CalculadoraQuedaTensaoService : ICalculadoraQuedaTensaoService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService? _logger;
        private const double ResistividadeCobre = 0.0172; // Ohm * mm² / metro a 20°C

        private static readonly double[] BitolasComerciais = new double[]
        {
            0.5, 0.75, 1.0, 1.5, 2.5, 4.0, 6.0, 10.0, 16.0, 25.0, 35.0, 50.0, 70.0, 95.0
        };

        public CalculadoraQuedaTensaoService(DatabaseService databaseService, LoggerService? logger = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger;
        }

        public ResultadoQuedaTensao CalcularQuedaTensao(ParametrosCalculoQuedaTensao parametros)
        {
            if (parametros == null) throw new ArgumentNullException(nameof(parametros));

            var resultado = new ResultadoQuedaTensao
            {
                TensaoFonteVolts = parametros.TensaoFonteVolts,
                TensaoCargaVolts = parametros.TensaoCargaVolts,
                CorrenteAmperes = parametros.CorrenteAmperes,
                DataCalculo = DateTime.Now
            };

            var queda = Math.Max(0, parametros.TensaoFonteVolts - parametros.TensaoCargaVolts);
            resultado.QuedaTensaoVolts = Math.Round(queda, 4);

            if (parametros.TensaoFonteVolts > 0)
            {
                resultado.PercentualQueda = Math.Round((queda / parametros.TensaoFonteVolts) * 100.0, 2);
            }

            if (parametros.CorrenteAmperes > 0)
            {
                resultado.ResistenciaParasitaOhms = Math.Round(queda / parametros.CorrenteAmperes, 4);
                resultado.PotenciaDissipadaWatts = Math.Round(queda * parametros.CorrenteAmperes, 2);
            }

            // Definição dos limites normativos conforme SAE J1128 / DIN 72551
            var tipoCircuito = parametros.TipoCircuito?.ToLowerInvariant() ?? "potência";
            double limiteTolerado = 0.20; // Padrão SAE J1128 para potência 12V

            if (tipoCircuito.Contains("aterramento") || tipoCircuito.Contains("terra") || tipoCircuito.Contains("massa"))
            {
                limiteTolerado = 0.10; // Norma DIN para circuitos de retorno terra
            }
            else if (tipoCircuito.Contains("sinal") || tipoCircuito.Contains("sensor") || tipoCircuito.Contains("eletrônic"))
            {
                limiteTolerado = 0.05; // Sensores e sinais analógicos de precisão
            }
            else if (tipoCircuito.Contains("24v") || tipoCircuito.Contains("pesad"))
            {
                limiteTolerado = 0.40; // Linha pesada 24V aceita até 0.40V em força
            }

            resultado.LimiteMaximoToleradoVolts = limiteTolerado;

            // Análise de conformidade e geração do diagnóstico
            if (resultado.QuedaTensaoVolts <= limiteTolerado)
            {
                resultado.StatusConformidade = StatusConformidadeQuedaTensao.Conforme;
                resultado.DiagnosticoTecnico = $"Circuito em perfeita conformidade normativa. Queda de {resultado.QuedaTensaoVolts:F2}V está abaixo do teto de {limiteTolerado:F2}V.";
                resultado.AcaoRecomendada = "Nenhuma intervenção necessária. Fiação, terminais e conexões estão íntegros e com baixa resistência.";
            }
            else if (resultado.QuedaTensaoVolts <= limiteTolerado * 1.5)
            {
                resultado.StatusConformidade = StatusConformidadeQuedaTensao.Toleravel;
                resultado.DiagnosticoTecnico = $"Queda de tensão limítrofe ({resultado.QuedaTensaoVolts:F2}V vs limite {limiteTolerado:F2}V). Resistência parasita de {resultado.ResistenciaParasitaOhms:F3} Ω detectada.";
                resultado.AcaoRecomendada = "Inspecionar terminais de conexão quanto a leve oxidação superficial e reapertar porcas/parafusos de fixação.";
            }
            else
            {
                resultado.StatusConformidade = StatusConformidadeQuedaTensao.NaoConformeCritico;
                var vezesAcima = resultado.QuedaTensaoVolts / Math.Max(0.01, limiteTolerado);
                resultado.DiagnosticoTecnico = $"QUEDA EXCESSIVA CRÍTICA! {vezesAcima:F1}x acima do limite SAE/DIN. O circuito dissipa {resultado.PotenciaDissipadaWatts:F1} Watts de calor no ponto com defeito (Resistência parasita de {resultado.ResistenciaParasitaOhms:F3} Ω).";
                
                if (tipoCircuito.Contains("aterramento") || tipoCircuito.Contains("terra"))
                {
                    resultado.AcaoRecomendada = "Aterramento deficiente: Desmontar terminal olhal de chassi, raspar tinta/ferrugem até o metal brilhante e aplicar vaselina líquida ou spray protetor.";
                }
                else
                {
                    resultado.AcaoRecomendada = "Verificar conectores derretidos, soquetes frouxos, fusível oxidado ou bitola do fio subdimensionada. Risco de superaquecimento e perda de eficiência da carga.";
                }
            }

            // Cálculo da bitola ideal recomendada
            resultado.SecaoMinimaRecomendadaMm2 = CalcularBitolaIdeal(
                parametros.CorrenteAmperes,
                parametros.ComprimentoCaboMetros,
                parametros.TensaoFonteVolts,
                limiteTolerado);

            return resultado;
        }

        public double CalcularBitolaIdeal(double correnteA, double comprimentoM, double tensaoNominalV, double maxQuedaVolts = 0.20)
        {
            if (correnteA <= 0 || comprimentoM <= 0 || maxQuedaVolts <= 0) return 1.5;

            // 2ª Lei de Ohm com ida e volta: S = (2 * rho * L * I) / deltaV
            var secaoCalculada = (2.0 * ResistividadeCobre * comprimentoM * correnteA) / maxQuedaVolts;

            foreach (var bitola in BitolasComerciais)
            {
                if (bitola >= secaoCalculada)
                {
                    return bitola;
                }
            }

            return Math.Round(secaoCalculada, 2);
        }

        public async Task<bool> SalvarHistoricoAsync(HistoricoCalculoQuedaTensao historico)
        {
            if (historico == null) return false;

            return await Task.Run(() =>
            {
                try
                {
                    using var conn = _databaseService.GetConnection();
                    conn.Open();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO HistoricoCalculosQuedaTensao
                        (DataHora, IdentificacaoCircuito, VeiculoPlaca, TipoCircuito, TensaoFonteVolts, TensaoCargaVolts, CorrenteAmperes, QuedaTensaoVolts, ResistenciaParasitaOhms, PotenciaDissipadaWatts, StatusConformidade, DiagnosticoTecnico, AcaoRecomendada)
                        VALUES
                        (@DataHora, @Identificacao, @Placa, @Tipo, @Fonte, @Carga, @Corrente, @Queda, @Resistencia, @Potencia, @Status, @Diagnostico, @Acao);";

                    cmd.Parameters.AddWithValue("@DataHora", historico.DataHora);
                    cmd.Parameters.AddWithValue("@Identificacao", historico.IdentificacaoCircuito);
                    cmd.Parameters.AddWithValue("@Placa", (object?)historico.VeiculoPlaca ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Tipo", historico.TipoCircuito);
                    cmd.Parameters.AddWithValue("@Fonte", historico.TensaoFonteVolts);
                    cmd.Parameters.AddWithValue("@Carga", historico.TensaoCargaVolts);
                    cmd.Parameters.AddWithValue("@Corrente", historico.CorrenteAmperes);
                    cmd.Parameters.AddWithValue("@Queda", historico.QuedaTensaoVolts);
                    cmd.Parameters.AddWithValue("@Resistencia", historico.ResistenciaParasitaOhms);
                    cmd.Parameters.AddWithValue("@Potencia", historico.PotenciaDissipadaWatts);
                    cmd.Parameters.AddWithValue("@Status", historico.StatusConformidade);
                    cmd.Parameters.AddWithValue("@Diagnostico", (object?)historico.DiagnosticoTecnico ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Acao", (object?)historico.AcaoRecomendada ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Erro ao persistir cálculo de queda de tensão no histórico.", ex);
                    return false;
                }
            });
        }

        public async Task<List<HistoricoCalculoQuedaTensao>> ObterHistoricoAsync(int limite = 50)
        {
            return await Task.Run(() =>
            {
                var lista = new List<HistoricoCalculoQuedaTensao>();
                try
                {
                    using var conn = _databaseService.GetConnection();
                    conn.Open();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT Id, DataHora, IdentificacaoCircuito, VeiculoPlaca, TipoCircuito, TensaoFonteVolts, TensaoCargaVolts, CorrenteAmperes, QuedaTensaoVolts, ResistenciaParasitaOhms, PotenciaDissipadaWatts, StatusConformidade, DiagnosticoTecnico, AcaoRecomendada
                        FROM HistoricoCalculosQuedaTensao
                        ORDER BY Id DESC
                        LIMIT @Limite;";
                    cmd.Parameters.AddWithValue("@Limite", limite);

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        lista.Add(new HistoricoCalculoQuedaTensao
                        {
                            Id = reader.GetInt32(0),
                            DataHora = reader.GetString(1),
                            IdentificacaoCircuito = reader.GetString(2),
                            VeiculoPlaca = reader.IsDBNull(3) ? null : reader.GetString(3),
                            TipoCircuito = reader.GetString(4),
                            TensaoFonteVolts = reader.GetDouble(5),
                            TensaoCargaVolts = reader.GetDouble(6),
                            CorrenteAmperes = reader.GetDouble(7),
                            QuedaTensaoVolts = reader.GetDouble(8),
                            ResistenciaParasitaOhms = reader.GetDouble(9),
                            PotenciaDissipadaWatts = reader.GetDouble(10),
                            StatusConformidade = reader.GetString(11),
                            DiagnosticoTecnico = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                            AcaoRecomendada = reader.IsDBNull(13) ? string.Empty : reader.GetString(13)
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Erro ao consultar histórico de cálculos de queda de tensão.", ex);
                }
                return lista;
            });
        }
    }
}
