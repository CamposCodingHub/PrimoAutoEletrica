using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Models.SureTrack;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public sealed class SureTrackService : ISureTrackService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService? _logger;
        private bool _inicializado = false;

        public SureTrackService(DatabaseService databaseService, LoggerService? logger = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger;
        }

        private DbConnection ObterConexaoAberta()
        {
            var conn = _databaseService.GetConnection();
            conn.Open();
            return conn;
        }

        public async Task GarantirCasosIniciaisOficinaAsync()
        {
            if (_inicializado) return;

            using var conn = ObterConexaoAberta();
            using var cmdCount = conn.CreateCommand();
            cmdCount.CommandText = "SELECT COUNT(*) FROM CasosResolvidosSureTrack;";
            var countObj = await cmdCount.ExecuteScalarAsync();
            int count = Convert.ToInt32(countObj);

            if (count > 0)
            {
                _inicializado = true;
                return;
            }

            var casosIniciais = ObterCasosCuradosOficina();
            using var trans = conn.BeginTransaction();
            try
            {
                foreach (var c in casosIniciais)
                {
                    using var cmdInsert = conn.CreateCommand();
                    cmdInsert.Transaction = trans;
                    cmdInsert.CommandText = @"
                        INSERT INTO CasosResolvidosSureTrack (
                            OrdemServicoOrigemId, OrdemServicoOrigemNumero, Montadora, Modelo,
                            Motorizacao, Ano, SintomaPrincipal, CodigosDTC, CausaRaizDetectada,
                            ProcedimentoSolucao, PecasSubstituidasJson, DicaTesteRapido,
                            DataResolucao, OcorrenciasConfirmadas, OrigemCaso
                        ) VALUES (
                            @OSId, @OSNum, @Montadora, @Modelo,
                            @Motor, @Ano, @Sintoma, @DTC, @Causa,
                            @Procedimento, @PecasJson, @Dica,
                            @Data, @Ocorr, @Origem
                        );";

                    cmdInsert.Parameters.AddWithValue("@OSId", (object?)c.OrdemServicoOrigemId ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@OSNum", (object?)c.OrdemServicoOrigemNumero ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@Montadora", c.Montadora);
                    cmdInsert.Parameters.AddWithValue("@Modelo", c.Modelo);
                    cmdInsert.Parameters.AddWithValue("@Motor", (object?)c.Motorizacao ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@Ano", c.Ano);
                    cmdInsert.Parameters.AddWithValue("@Sintoma", c.SintomaPrincipal);
                    cmdInsert.Parameters.AddWithValue("@DTC", (object?)c.CodigosDTC ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@Causa", c.CausaRaizDetectada);
                    cmdInsert.Parameters.AddWithValue("@Procedimento", c.ProcedimentoSolucao);
                    cmdInsert.Parameters.AddWithValue("@PecasJson", c.PecasSubstituidasJson ?? "[]");
                    cmdInsert.Parameters.AddWithValue("@Dica", (object?)c.DicaTesteRapido ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@Data", c.DataResolucao.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    cmdInsert.Parameters.AddWithValue("@Ocorr", c.OcorrenciasConfirmadas);
                    cmdInsert.Parameters.AddWithValue("@Origem", c.OrigemCaso ?? "RedeHomologada");

                    await cmdInsert.ExecuteNonQueryAsync();
                }

                trans.Commit();
                _inicializado = true;
                _logger?.LogInfo($"SureTrack: {casosIniciais.Count} casos curados de bancada foram indexados com sucesso.");
            }
            catch (Exception ex)
            {
                trans.Rollback();
                _logger?.LogError($"Falha ao semear casos iniciais SureTrack: {ex.Message}");
            }
        }

        public async Task<int> ObterTotalCasosAsync()
        {
            await GarantirCasosIniciaisOficinaAsync();
            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM CasosResolvidosSureTrack;";
            var res = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(res);
        }

        public async Task<int> IndexarOrdemServicoConcluidaAsync(OrdemServico ordem)
        {
            if (ordem == null) return 0;

            await GarantirCasosIniciaisOficinaAsync();

            string causaRaiz = !string.IsNullOrWhiteSpace(ordem.DiagnosticoFinal)
                ? ordem.DiagnosticoFinal.Trim()
                : (!string.IsNullOrWhiteSpace(ordem.Diagnostico) ? ordem.Diagnostico.Trim() : string.Empty);

            string sintoma = !string.IsNullOrWhiteSpace(ordem.ProblemaRelatado)
                ? ordem.ProblemaRelatado.Trim()
                : "Defeito no sistema elétrico";

            if (string.IsNullOrWhiteSpace(causaRaiz))
            {
                // Se a OS não registrou diagnóstico, não é possível indexar como causa raiz empírica
                return 0;
            }

            var (montadora, modelo, motor, ano) = ExtrairDadosVeiculo(ordem.VeiculoDescricaoSnapshot);
            var dtcsEncontrados = ExtrairDTCs(causaRaiz + " " + sintoma);
            string dtcStr = string.Join(", ", dtcsEncontrados);

            var pecas = ordem.Itens?
                .Where(i => string.Equals(i.Tipo, "Peca", StringComparison.OrdinalIgnoreCase) || i.EstoqueMovimentado)
                .Select(i => i.Descricao.Trim())
                .Distinct()
                .ToList() ?? new List<string>();

            string pecasJson = JsonSerializer.Serialize(pecas);

            using var conn = ObterConexaoAberta();

            // Verifica se já existe caso similar registrado para o mesmo modelo e causa
            using var cmdBusca = conn.CreateCommand();
            cmdBusca.CommandText = @"
                SELECT Id, OcorrenciasConfirmadas 
                FROM CasosResolvidosSureTrack 
                WHERE Modelo = @Modelo AND CausaRaizDetectada = @Causa
                LIMIT 1;";
            cmdBusca.Parameters.AddWithValue("@Modelo", modelo);
            cmdBusca.Parameters.AddWithValue("@Causa", causaRaiz);

            using var reader = await cmdBusca.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                int idExistente = reader.GetInt32(0);
                int ocorrencias = reader.GetInt32(1) + 1;
                reader.Close();

                using var cmdUpdate = conn.CreateCommand();
                cmdUpdate.CommandText = @"
                    UPDATE CasosResolvidosSureTrack SET
                        OcorrenciasConfirmadas = @Ocorr,
                        DataResolucao = @Data,
                        PecasSubstituidasJson = @PecasJson
                    WHERE Id = @Id;";
                cmdUpdate.Parameters.AddWithValue("@Ocorr", ocorrencias);
                cmdUpdate.Parameters.AddWithValue("@Data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                cmdUpdate.Parameters.AddWithValue("@PecasJson", pecasJson);
                cmdUpdate.Parameters.AddWithValue("@Id", idExistente);

                await cmdUpdate.ExecuteNonQueryAsync();
                _logger?.LogInfo($"SureTrack: Ocorrência incrementada para caso existente ID {idExistente} ({ocorrencias} confirmações).");
                return idExistente;
            }
            reader.Close();

            // Insere novo caso
            using var cmdInsert = conn.CreateCommand();
            cmdInsert.CommandText = @"
                INSERT INTO CasosResolvidosSureTrack (
                    OrdemServicoOrigemId, OrdemServicoOrigemNumero, Montadora, Modelo,
                    Motorizacao, Ano, SintomaPrincipal, CodigosDTC, CausaRaizDetectada,
                    ProcedimentoSolucao, PecasSubstituidasJson, DicaTesteRapido,
                    DataResolucao, OcorrenciasConfirmadas, OrigemCaso
                ) VALUES (
                    @OSId, @OSNum, @Montadora, @Modelo,
                    @Motor, @Ano, @Sintoma, @DTC, @Causa,
                    @Procedimento, @PecasJson, @Dica,
                    @Data, 1, 'OficinaLocal'
                );
                SELECT last_insert_rowid();";

            cmdInsert.Parameters.AddWithValue("@OSId", ordem.Id.ToString());
            cmdInsert.Parameters.AddWithValue("@OSNum", (object?)ordem.Numero ?? DBNull.Value);
            cmdInsert.Parameters.AddWithValue("@Montadora", montadora);
            cmdInsert.Parameters.AddWithValue("@Modelo", modelo);
            cmdInsert.Parameters.AddWithValue("@Motor", motor);
            cmdInsert.Parameters.AddWithValue("@Ano", ano);
            cmdInsert.Parameters.AddWithValue("@Sintoma", sintoma);
            cmdInsert.Parameters.AddWithValue("@DTC", (object?)dtcStr ?? DBNull.Value);
            cmdInsert.Parameters.AddWithValue("@Causa", causaRaiz);
            cmdInsert.Parameters.AddWithValue("@Procedimento", !string.IsNullOrWhiteSpace(ordem.ObservacoesInternas) ? ordem.ObservacoesInternas : "Reparo concluído conforme diagnóstico técnico.");
            cmdInsert.Parameters.AddWithValue("@PecasJson", pecasJson);
            cmdInsert.Parameters.AddWithValue("@Dica", $"Caso originado da OS {ordem.Numero} com troca de peças comprovada.");
            cmdInsert.Parameters.AddWithValue("@Data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

            var newIdObj = await cmdInsert.ExecuteScalarAsync();
            int newId = Convert.ToInt32(newIdObj);
            _logger?.LogInfo($"SureTrack: Nova OS indexada com sucesso! Caso ID {newId} para {modelo}.");
            return newId;
        }

        public async Task<SureTrackConsultaResultado> ConsultarEstatisticasAsync(string? modelo, string? dtc, string? sintoma)
        {
            await GarantirCasosIniciaisOficinaAsync();

            var todosCasos = await ObterTodosCasosInternoAsync();
            var resultado = new SureTrackConsultaResultado();

            string modeloNorm = NormalizarString(modelo);
            string dtcNorm = NormalizarString(dtc);
            string sintomaNorm = NormalizarString(sintoma);

            // Filtragem hierárquica
            var casosFiltrados = todosCasos.Where(c =>
            {
                bool matchModelo = string.IsNullOrWhiteSpace(modeloNorm) ||
                    NormalizarString(c.Modelo).Contains(modeloNorm) ||
                    NormalizarString(c.Montadora).Contains(modeloNorm) ||
                    modeloNorm.Contains(NormalizarString(c.Modelo));

                bool matchDtc = string.IsNullOrWhiteSpace(dtcNorm) ||
                    NormalizarString(c.CodigosDTC).Contains(dtcNorm);

                bool matchSintoma = string.IsNullOrWhiteSpace(sintomaNorm) ||
                    NormalizarString(c.SintomaPrincipal).Contains(sintomaNorm) ||
                    NormalizarString(c.CausaRaizDetectada).Contains(sintomaNorm);

                if (!string.IsNullOrWhiteSpace(dtcNorm) && !string.IsNullOrWhiteSpace(modeloNorm))
                {
                    return matchDtc && matchModelo;
                }
                if (!string.IsNullOrWhiteSpace(dtcNorm))
                {
                    return matchDtc;
                }
                if (!string.IsNullOrWhiteSpace(modeloNorm) && !string.IsNullOrWhiteSpace(sintomaNorm))
                {
                    return matchModelo && matchSintoma;
                }
                if (!string.IsNullOrWhiteSpace(modeloNorm))
                {
                    return matchModelo;
                }
                if (!string.IsNullOrWhiteSpace(sintomaNorm))
                {
                    return matchSintoma;
                }

                return true;
            }).ToList();

            // Se a busca filtrada não achar nada específico, mas havia termo, tenta busca mais relaxada
            if (casosFiltrados.Count == 0 && (!string.IsNullOrWhiteSpace(modeloNorm) || !string.IsNullOrWhiteSpace(dtcNorm) || !string.IsNullOrWhiteSpace(sintomaNorm)))
            {
                casosFiltrados = todosCasos.Where(c =>
                {
                    string combinada = NormalizarString($"{c.Montadora} {c.Modelo} {c.SintomaPrincipal} {c.CodigosDTC} {c.CausaRaizDetectada}");
                    return (!string.IsNullOrWhiteSpace(dtcNorm) && combinada.Contains(dtcNorm)) ||
                           (!string.IsNullOrWhiteSpace(modeloNorm) && combinada.Contains(modeloNorm)) ||
                           (!string.IsNullOrWhiteSpace(sintomaNorm) && combinada.Contains(sintomaNorm));
                }).ToList();
            }

            // Se mesmo assim não achar nada, retorna os top casos da oficina para que o técnico nunca veja tela vazia
            if (casosFiltrados.Count == 0)
            {
                casosFiltrados = todosCasos.OrderByDescending(c => c.OcorrenciasConfirmadas).Take(6).ToList();
            }

            resultado.CasosIndividuais = casosFiltrados;
            int totalOcorrenciasGeral = casosFiltrados.Sum(c => c.OcorrenciasConfirmadas);
            resultado.TotalCasosAnalisados = totalOcorrenciasGeral;

            // Paleta de cores para os cartões de estatística
            string[] paletaCores = new[] { "#10B981", "#3B82F6", "#F59E0B", "#8B5CF6", "#EC4899", "#14B8A6" };

            // Agrupa por Causa Raiz para calcular as probabilidades
            var grupos = casosFiltrados
                .GroupBy(c => c.CausaRaizDetectada)
                .Select(g => new
                {
                    Causa = g.Key,
                    TotalOcorrencias = g.Sum(x => x.OcorrenciasConfirmadas),
                    PrimeiroCaso = g.First(),
                    Pecas = g.SelectMany(x => x.ObterPecasSubstituidas()).Distinct().ToList()
                })
                .OrderByDescending(g => g.TotalOcorrencias)
                .ToList();

            int corIndex = 0;
            foreach (var g in grupos)
            {
                double perc = totalOcorrenciasGeral > 0 ? (g.TotalOcorrencias * 100.0) / totalOcorrenciasGeral : 0;
                var itemEst = new SureTrackEstatisticaItem
                {
                    CausaRaiz = g.Causa,
                    TotalOcorrencias = g.TotalOcorrencias,
                    PercentualProbabilidade = Math.Round(perc, 1),
                    PecasMaisFrequentes = g.Pecas,
                    ProcedimentoRecomendado = g.PrimeiroCaso.ProcedimentoSolucao,
                    DicaTesteRapido = g.PrimeiroCaso.DicaTesteRapido,
                    CorBarra = paletaCores[corIndex % paletaCores.Length]
                };
                resultado.Estatisticas.Add(itemEst);
                corIndex++;

                if (!string.IsNullOrWhiteSpace(g.PrimeiroCaso.DicaTesteRapido) && !resultado.DicasAtalho15Min.Contains(g.PrimeiroCaso.DicaTesteRapido))
                {
                    resultado.DicasAtalho15Min.Add(g.PrimeiroCaso.DicaTesteRapido);
                }
            }

            return resultado;
        }

        public async Task<List<CasoResolvidoSureTrack>> ObterUltimosCasosAsync(int limite = 25)
        {
            await GarantirCasosIniciaisOficinaAsync();
            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM CasosResolvidosSureTrack 
                ORDER BY Id DESC 
                LIMIT @Limit;";
            cmd.Parameters.AddWithValue("@Limit", limite);

            var lista = new List<CasoResolvidoSureTrack>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(MapearCaso(reader));
            }
            return lista;
        }

        public async Task InserirCasoManualAsync(CasoResolvidoSureTrack caso)
        {
            if (caso == null) throw new ArgumentNullException(nameof(caso));
            await GarantirCasosIniciaisOficinaAsync();

            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO CasosResolvidosSureTrack (
                    OrdemServicoOrigemId, OrdemServicoOrigemNumero, Montadora, Modelo,
                    Motorizacao, Ano, SintomaPrincipal, CodigosDTC, CausaRaizDetectada,
                    ProcedimentoSolucao, PecasSubstituidasJson, DicaTesteRapido,
                    DataResolucao, OcorrenciasConfirmadas, OrigemCaso
                ) VALUES (
                    @OSId, @OSNum, @Montadora, @Modelo,
                    @Motor, @Ano, @Sintoma, @DTC, @Causa,
                    @Procedimento, @PecasJson, @Dica,
                    @Data, @Ocorr, @Origem
                );";

            cmd.Parameters.AddWithValue("@OSId", (object?)caso.OrdemServicoOrigemId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OSNum", (object?)caso.OrdemServicoOrigemNumero ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Montadora", caso.Montadora ?? "Universal");
            cmd.Parameters.AddWithValue("@Modelo", caso.Modelo ?? "Universal");
            cmd.Parameters.AddWithValue("@Motor", (object?)caso.Motorizacao ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Ano", caso.Ano);
            cmd.Parameters.AddWithValue("@Sintoma", caso.SintomaPrincipal ?? string.Empty);
            cmd.Parameters.AddWithValue("@DTC", (object?)caso.CodigosDTC ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Causa", caso.CausaRaizDetectada ?? string.Empty);
            cmd.Parameters.AddWithValue("@Procedimento", caso.ProcedimentoSolucao ?? string.Empty);
            cmd.Parameters.AddWithValue("@PecasJson", caso.PecasSubstituidasJson ?? "[]");
            cmd.Parameters.AddWithValue("@Dica", (object?)caso.DicaTesteRapido ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Data", caso.DataResolucao.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("@Ocorr", Math.Max(1, caso.OcorrenciasConfirmadas));
            cmd.Parameters.AddWithValue("@Origem", caso.OrigemCaso ?? "OficinaLocal");

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<List<CasoResolvidoSureTrack>> ObterTodosCasosInternoAsync()
        {
            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM CasosResolvidosSureTrack ORDER BY OcorrenciasConfirmadas DESC;";

            var lista = new List<CasoResolvidoSureTrack>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(MapearCaso(reader));
            }
            return lista;
        }

        private static CasoResolvidoSureTrack MapearCaso(DbDataReader reader)
        {
            return new CasoResolvidoSureTrack
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                OrdemServicoOrigemId = reader.IsDBNull(reader.GetOrdinal("OrdemServicoOrigemId")) ? null : reader.GetString(reader.GetOrdinal("OrdemServicoOrigemId")),
                OrdemServicoOrigemNumero = reader.IsDBNull(reader.GetOrdinal("OrdemServicoOrigemNumero")) ? null : reader.GetString(reader.GetOrdinal("OrdemServicoOrigemNumero")),
                Montadora = reader.GetString(reader.GetOrdinal("Montadora")),
                Modelo = reader.GetString(reader.GetOrdinal("Modelo")),
                Motorizacao = reader.IsDBNull(reader.GetOrdinal("Motorizacao")) ? string.Empty : reader.GetString(reader.GetOrdinal("Motorizacao")),
                Ano = reader.GetInt32(reader.GetOrdinal("Ano")),
                SintomaPrincipal = reader.GetString(reader.GetOrdinal("SintomaPrincipal")),
                CodigosDTC = reader.IsDBNull(reader.GetOrdinal("CodigosDTC")) ? string.Empty : reader.GetString(reader.GetOrdinal("CodigosDTC")),
                CausaRaizDetectada = reader.GetString(reader.GetOrdinal("CausaRaizDetectada")),
                ProcedimentoSolucao = reader.GetString(reader.GetOrdinal("ProcedimentoSolucao")),
                PecasSubstituidasJson = reader.IsDBNull(reader.GetOrdinal("PecasSubstituidasJson")) ? "[]" : reader.GetString(reader.GetOrdinal("PecasSubstituidasJson")),
                DicaTesteRapido = reader.IsDBNull(reader.GetOrdinal("DicaTesteRapido")) ? string.Empty : reader.GetString(reader.GetOrdinal("DicaTesteRapido")),
                DataResolucao = DateTime.TryParse(reader.GetString(reader.GetOrdinal("DataResolucao")), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : DateTime.Now,
                OcorrenciasConfirmadas = reader.GetInt32(reader.GetOrdinal("OcorrenciasConfirmadas")),
                OrigemCaso = reader.IsDBNull(reader.GetOrdinal("OrigemCaso")) ? "OficinaLocal" : reader.GetString(reader.GetOrdinal("OrigemCaso"))
            };
        }

        private static (string Montadora, string Modelo, string Motor, int Ano) ExtrairDadosVeiculo(string? snapshot)
        {
            if (string.IsNullOrWhiteSpace(snapshot))
                return ("Universal", "Universal", "", 2020);

            var texto = snapshot.Trim();
            int ano = 2020;
            var matchAno = Regex.Match(texto, @"\b(19\d\d|20\d\d)\b");
            if (matchAno.Success && int.TryParse(matchAno.Value, out int a))
            {
                ano = a;
            }

            string montadora = "Universal";
            if (texto.Contains("Chevrolet", StringComparison.OrdinalIgnoreCase) || texto.Contains("GM", StringComparison.OrdinalIgnoreCase)) montadora = "Chevrolet";
            else if (texto.Contains("Volkswagen", StringComparison.OrdinalIgnoreCase) || texto.Contains("VW", StringComparison.OrdinalIgnoreCase)) montadora = "Volkswagen";
            else if (texto.Contains("Fiat", StringComparison.OrdinalIgnoreCase)) montadora = "Fiat";
            else if (texto.Contains("Ford", StringComparison.OrdinalIgnoreCase)) montadora = "Ford";
            else if (texto.Contains("Renault", StringComparison.OrdinalIgnoreCase)) montadora = "Renault";
            else if (texto.Contains("Toyota", StringComparison.OrdinalIgnoreCase)) montadora = "Toyota";
            else if (texto.Contains("Hyundai", StringComparison.OrdinalIgnoreCase)) montadora = "Hyundai";
            else if (texto.Contains("Honda", StringComparison.OrdinalIgnoreCase)) montadora = "Honda";
            else if (texto.Contains("Mercedes", StringComparison.OrdinalIgnoreCase)) montadora = "Mercedes-Benz";

            string modelo = texto;
            string[] modelosComuns = new[] { "Onix", "Prisma", "Gol", "Fox", "Voyage", "Palio", "Uno", "Strada", "Siena", "Ka", "Fiesta", "Sandero", "Logan", "Corolla", "Hilux", "HB20", "Civic", "Fit", "Accelo", "Atego" };
            foreach (var m in modelosComuns)
            {
                if (texto.Contains(m, StringComparison.OrdinalIgnoreCase))
                {
                    modelo = m;
                    break;
                }
            }

            return (montadora, modelo, "", ano);
        }

        private static List<string> ExtrairDTCs(string texto)
        {
            var matches = Regex.Matches(texto.ToUpperInvariant(), @"\b([PBUS]\d{4})\b");
            var set = new HashSet<string>();
            foreach (Match m in matches)
            {
                set.Add(m.Value);
            }
            return set.ToList();
        }

        private static string NormalizarString(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var norm = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in norm)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().ToLowerInvariant().Trim();
        }

        private static List<CasoResolvidoSureTrack> ObterCasosCuradosOficina()
        {
            return new List<CasoResolvidoSureTrack>
            {
                new CasoResolvidoSureTrack
                {
                    Montadora = "Chevrolet",
                    Modelo = "Onix",
                    Motorizacao = "1.0 / 1.4 SPE/4",
                    Ano = 2017,
                    SintomaPrincipal = "Motor falha em aceleração, luz da injeção piscando",
                    CodigosDTC = "P0300, P0304",
                    CausaRaizDetectada = "Fuga de corrente na bobina de ignição multiponto por trinca térmica no corpo plástico",
                    ProcedimentoSolucao = "Substituição do módulo de bobina de ignição 4 saídas e troca do jogo de velas com calibração de folga 0.8mm",
                    PecasSubstituidasJson = JsonSerializer.Serialize(new[] { "Bobina de Ignição Multiponto Delphi/GM", "Jogo de Velas NGK BPR6EY" }),
                    DicaTesteRapido = "Retirar a bobina e inspecionar o corpo cilíndrico contra a luz; marcas brancas ou acinzentadas indicam fuga de faísca para o cabeçote.",
                    DataResolucao = DateTime.Now.AddDays(-12),
                    OcorrenciasConfirmadas = 14,
                    OrigemCaso = "RedeHomologada"
                },
                new CasoResolvidoSureTrack
                {
                    Montadora = "Volkswagen",
                    Modelo = "Gol",
                    Motorizacao = "1.0 / 1.6 EA111",
                    Ano = 2014,
                    SintomaPrincipal = "Eletroventilador armado direto na velocidade máxima mesmo com motor frio",
                    CodigosDTC = "P0118",
                    CausaRaizDetectada = "Sensor de temperatura do líquido de arrefecimento (ECT) em circuito aberto ou relé duplo colado",
                    ProcedimentoSolucao = "Substituição do sensor de temperatura ECT na carcaça da válvula termostática e verificação do conector 4 vias",
                    PecasSubstituidasJson = JsonSerializer.Serialize(new[] { "Sensor de Temperatura ECT MTE-Thomson", "Relé Duplo de Arrefecimento" }),
                    DicaTesteRapido = "Desconectar o chicote do sensor de temperatura; a ECU ME7.5.20 aciona a velocidade 2 em estratégia de emergência confirmando o circuito.",
                    DataResolucao = DateTime.Now.AddDays(-18),
                    OcorrenciasConfirmadas = 11,
                    OrigemCaso = "RedeHomologada"
                },
                new CasoResolvidoSureTrack
                {
                    Montadora = "Fiat",
                    Modelo = "Palio",
                    Motorizacao = "1.0 / 1.4 Fire Flex",
                    Ano = 2012,
                    SintomaPrincipal = "Não dá partida; motor de arranque estala mas não vira (linha 50 sem força)",
                    CodigosDTC = "P0562",
                    CausaRaizDetectada = "Queda de tensão excessiva na malha de aterramento entre o motor e o chassi (terminal W oxidado)",
                    ProcedimentoSolucao = "Instalação de reforço de cabo de aterramento 25mm² estanhado direto do polo negativo da bateria ao parafuso da caixa de marchas",
                    PecasSubstituidasJson = JsonSerializer.Serialize(new[] { "Cabo de Aterramento 25mm² Cobre", "Terminal Olhal Estanhado", "Escovas do Motor de Arranque" }),
                    DicaTesteRapido = "Colocar o multímetro em 20V DC entre o bloco do motor e o polo negativo da bateria durante o arranque; queda superior a 0.20V acusa mau contato.",
                    DataResolucao = DateTime.Now.AddDays(-5),
                    OcorrenciasConfirmadas = 15,
                    OrigemCaso = "RedeHomologada"
                },
                new CasoResolvidoSureTrack
                {
                    Montadora = "Ford",
                    Modelo = "Ka",
                    Motorizacao = "1.0 Ti-VCT 3 Cilindros",
                    Ano = 2018,
                    SintomaPrincipal = "Motor fraco, marcha lenta oscilando e barulho de corrente",
                    CodigosDTC = "P0016, P0017",
                    CausaRaizDetectada = "Válvula solenóide atuadora do comando de válvulas variável (VVT) travada com borra ou desgaste interno",
                    ProcedimentoSolucao = "Troca da solenóide VVT, limpeza do alojamento e substituição do óleo de motor pelo fluido normatizado 5W20 WSS-M2C948-B",
                    PecasSubstituidasJson = JsonSerializer.Serialize(new[] { "Válvula Solenóide do Comando VVT", "Óleo Motor 5W20 Sintético", "Filtro de Óleo Motorcraft" }),
                    DicaTesteRapido = "Medir a resistência da solenóide VVT (deve dar entre 7.5Ω e 9.5Ω a 20°C); aplicar pulsos de 12V e conferir clique mecânico imediato.",
                    DataResolucao = DateTime.Now.AddDays(-24),
                    OcorrenciasConfirmadas = 8,
                    OrigemCaso = "RedeHomologada"
                },
                new CasoResolvidoSureTrack
                {
                    Montadora = "Renault",
                    Modelo = "Sandero",
                    Motorizacao = "1.0 / 1.6 SCe",
                    Ano = 2019,
                    SintomaPrincipal = "Bateria descarrega de um dia para o outro; aviso 'Carga de bateria fraca'",
                    CodigosDTC = "U0111",
                    CausaRaizDetectada = "Sensor de corrente IBS do polo negativo em falha na rede LIN ou alternador pilotado com diodo em fuga",
                    ProcedimentoSolucao = "Substituição do sensor inteligente de bateria (IBS) e reaperto do torque de fixação no borne negativo",
                    PecasSubstituidasJson = JsonSerializer.Serialize(new[] { "Sensor Inteligente de Bateria IBS Renault", "Regulador de Voltagem LIN" }),
                    DicaTesteRapido = "Aguardar 20 minutos com as portas travadas para o modo sleep da BCM; o consumo parasita normal deve ser estritamente inferior a 40mA (0.040A).",
                    DataResolucao = DateTime.Now.AddDays(-3),
                    OcorrenciasConfirmadas = 9,
                    OrigemCaso = "RedeHomologada"
                },
                new CasoResolvidoSureTrack
                {
                    Montadora = "Toyota",
                    Modelo = "Corolla",
                    Motorizacao = "1.8 / 2.0 Dual VVT-i",
                    Ano = 2016,
                    SintomaPrincipal = "Engasgos ao acelerar e consumo alto de combustível",
                    CodigosDTC = "P0101, P0171",
                    CausaRaizDetectada = "Filamento aquecido do sensor de fluxo de ar MAF impregnado com pó e película de óleo",
                    ProcedimentoSolucao = "Desmontagem e desengraxe ultradelicado do filamento do sensor com spray MAF Cleaner e troca do filtro de ar de motor",
                    PecasSubstituidasJson = JsonSerializer.Serialize(new[] { "Limpeza Técnica Spray MAF Cleaner", "Filtro de Ar de Motor Denso Original" }),
                    DicaTesteRapido = "No scanner, em marcha lenta a 650 RPM aquecido e ar desligado, o fluxo de ar deve marcar entre 1.8 e 2.2 g/s. Abaixo de 1.6 g/s acusa sujeira.",
                    DataResolucao = DateTime.Now.AddDays(-9),
                    OcorrenciasConfirmadas = 12,
                    OrigemCaso = "RedeHomologada"
                },
                new CasoResolvidoSureTrack
                {
                    Montadora = "Toyota",
                    Modelo = "Hilux",
                    Motorizacao = "2.8 / 3.0 D-4D",
                    Ano = 2015,
                    SintomaPrincipal = "Corta aceleração em subida forte, entra em modo de emergência (limp home)",
                    CodigosDTC = "P0093, P1229",
                    CausaRaizDetectada = "Válvula dosadora SCV da bomba de alta pressão Denso travando por desgaste de assentamento",
                    ProcedimentoSolucao = "Substituição do kit da válvula dosadora SCV na bomba injetora e reinicialização dos parâmetros de injeção",
                    PecasSubstituidasJson = JsonSerializer.Serialize(new[] { "Kit Válvula Dosadora SCV Denso", "Filtro de Combustível Diesel com Dreno" }),
                    DicaTesteRapido = "Em marcha lenta, a pressão nominal do rail deve ser de 30 a 35 MPa. Flutuações superiores a 5 MPa na lenta denunciam travamento mecânico da SCV.",
                    DataResolucao = DateTime.Now.AddDays(-15),
                    OcorrenciasConfirmadas = 10,
                    OrigemCaso = "RedeHomologada"
                },
                new CasoResolvidoSureTrack
                {
                    Montadora = "Hyundai",
                    Modelo = "HB20",
                    Motorizacao = "1.0 12V Kappa",
                    Ano = 2016,
                    SintomaPrincipal = "Dificuldade na partida a quente; motor gira 4 a 6 segundos antes de pegar",
                    CodigosDTC = "P0340",
                    CausaRaizDetectada = "Sensor de fase do comando (CMP) com perda intermitente de sinal sob alta temperatura",
                    ProcedimentoSolucao = "Troca do sensor de fase hall no cabeçote e inspeção da integridade da vedação do conector",
                    PecasSubstituidasJson = JsonSerializer.Serialize(new[] { "Sensor de Posição de Fase CMP Mobis" }),
                    DicaTesteRapido = "Com osciloscópio, o sinal no pino central do sensor de fase deve pulsar onda quadrada limpa de 0V a 5V mesmo a 95°C de temperatura de bloco.",
                    DataResolucao = DateTime.Now.AddDays(-7),
                    OcorrenciasConfirmadas = 9,
                    OrigemCaso = "RedeHomologada"
                },
                new CasoResolvidoSureTrack
                {
                    Montadora = "Honda",
                    Modelo = "Civic",
                    Motorizacao = "1.8 / 2.0 i-VTEC",
                    Ano = 2014,
                    SintomaPrincipal = "Ar-condicionado desarma após 10 a 15 minutos em dias quentes",
                    CodigosDTC = "B1234",
                    CausaRaizDetectada = "Relé da embreagem do compressor (Omron G8HL) colando internamente por aquecimento dos contatos",
                    ProcedimentoSolucao = "Substituição do relé auxiliar na caixa de fusíveis do motor por relé reforçado Mitsuba e verificação do gap da embreagem",
                    PecasSubstituidasJson = JsonSerializer.Serialize(new[] { "Relé Auxiliar de Ar-Condicionado Mitsuba", "Carga de Gás Ecológico R134a" }),
                    DicaTesteRapido = "Quando o ar parar de gelar, dê leves batidinhas no topo do relé com o cabo de uma chave; se o compressor atracar na hora, o relé está colando.",
                    DataResolucao = DateTime.Now.AddDays(-14),
                    OcorrenciasConfirmadas = 13,
                    OrigemCaso = "RedeHomologada"
                },
                new CasoResolvidoSureTrack
                {
                    Montadora = "Mercedes-Benz",
                    Modelo = "Accelo",
                    Motorizacao = "OM924 24V",
                    Ano = 2018,
                    SintomaPrincipal = "Painel indica falha de motor (luz amarela acesa), corte de potência",
                    CodigosDTC = "MR 04038",
                    CausaRaizDetectada = "Fios do par trançado da rede CAN esmagados no chicote dianteiro próximo ao suporte do radiador",
                    ProcedimentoSolucao = "Desmontagem do chicote, recuperação do trançado das vias amarela e azul com solda de prata e proteção com fita de tecido antichamas",
                    PecasSubstituidasJson = JsonSerializer.Serialize(new[] { "Reparo de Chicote Automotivo 24V", "Espaguete Corrugado e Fita Tecido Antichamas" }),
                    DicaTesteRapido = "Com chave desligada, medir resistência entre pinos 6 e 14 da tomada de diagnóstico: a leitura normal é 60.0Ω. Se der 120Ω, há fio rompido.",
                    DataResolucao = DateTime.Now.AddDays(-2),
                    OcorrenciasConfirmadas = 16,
                    OrigemCaso = "RedeHomologada"
                }
            };
        }
    }
}
