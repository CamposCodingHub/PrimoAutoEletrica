using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models.BibliotecaTecnica;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public sealed class BibliotecaTecnicaService : IBibliotecaTecnicaService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService? _logger;
        private bool _inicializado = false;

        public BibliotecaTecnicaService(DatabaseService databaseService, LoggerService? logger = null)
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

        public async Task<bool> GarantirCargaInicialAsync()
        {
            if (_inicializado) return true;

            try
            {
                using var conn = ObterConexaoAberta();
                using var cmdCount = conn.CreateCommand();
                cmdCount.CommandText = "SELECT COUNT(*) FROM ModulosEletronicos;";
                var countObj = await cmdCount.ExecuteScalarAsync();
                int count = Convert.ToInt32(countObj);

                if (count == 0)
                {
                    await InserirCargaInicialModulosAsync(conn);
                }

                using var cmdCentrais = conn.CreateCommand();
                cmdCentrais.CommandText = "SELECT COUNT(*) FROM CentraisEletricas;";
                var countCentrais = Convert.ToInt32(await cmdCentrais.ExecuteScalarAsync());
                var centraisSeed = ObterCentraisCuradas();
                if (countCentrais < centraisSeed.Count)
                {
                    await InserirCargaInicialCentraisAsync(conn);
                }

                using var cmdPesados = conn.CreateCommand();
                cmdPesados.CommandText = "SELECT COUNT(*) FROM EspecificacoesLinhaPesada24V;";
                var countPesados = Convert.ToInt32(await cmdPesados.ExecuteScalarAsync());
                var pesadosSeed = ObterPesadosCurados();
                if (countPesados < pesadosSeed.Count)
                {
                    await InserirCargaInicialPesadosAsync(conn);
                }

                _inicializado = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao garantir carga inicial da Biblioteca Técnica: {ex.Message}");
                return false;
            }
        }

        private static List<string> ExtrairTermosRelevantes(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return new();

            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "qual", "quais", "como", "onde", "quando", "quem", "por", "porque", "pra", "para",
                "de", "da", "do", "das", "dos", "em", "na", "no", "nas", "nos", "um", "uma", "uns", "umas",
                "o", "a", "os", "as", "e", "ou", "que", "se", "com", "sem", "sob", "sobre",
                "pinagem", "pinagens", "pinout", "modulo", "módulo", "modulos", "módulos", "central",
                "centrais", "caixa", "fusivel", "fusiveis", "fusível", "fusíveis", "rele", "relé", "reles", "relés",
                "esquema", "diagrama", "eletrico", "elétrico", "tabela", "manual", "especificacao", "especificações",
                "posicao", "posicoes", "posição", "posições", "preciso", "precisa", "ano", "anos", "ola", "olá",
                "bom", "dia", "tarde", "noite", "favor", "passa", "mostra", "me", "passar", "mostrar", "saber", "gostaria"
            };

            var delimitadores = new[] { ' ', ',', '.', ';', ':', '?', '!', '\t', '\n', '\r', '/', '(', ')' };
            var palavras = texto.Split(delimitadores, StringSplitOptions.RemoveEmptyEntries);
            var tokens = new List<string>();
            foreach (var p in palavras)
            {
                var limpa = p.Trim();
                if (limpa.Length >= 2 && !stopWords.Contains(limpa) && !tokens.Contains(limpa, StringComparer.OrdinalIgnoreCase))
                {
                    tokens.Add(limpa);
                }
            }

            return tokens;
        }

        public async Task<List<PinagemModulo>> ObterModulosAsync(string? termoBusca = null, string? montadora = null, string? tensao = null)
        {
            await GarantirCargaInicialAsync();

            var modulos = await ExecutarBuscaModulosSqlAsync(termoBusca, montadora, tensao);
            if (modulos.Count == 0 && !string.IsNullOrWhiteSpace(termoBusca))
            {
                var tokens = ExtrairTermosRelevantes(termoBusca);
                foreach (var token in tokens)
                {
                    var parciais = await ExecutarBuscaModulosSqlAsync(token, montadora, tensao);
                    foreach (var p in parciais)
                    {
                        if (!modulos.Any(m => m.Id == p.Id))
                        {
                            modulos.Add(p);
                        }
                    }
                }
            }

            return modulos;
        }

        private async Task<List<PinagemModulo>> ExecutarBuscaModulosSqlAsync(string? termo, string? montadora, string? tensao)
        {
            var modulos = new List<PinagemModulo>();
            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();

            var sql = "SELECT Id, CodigoModulo, NomeModulo, Montadora, ModelosAplicacao, SistemaTipo, TensaoOperacao, DescricaoConectores, ObservacoesTecnicas FROM ModulosEletronicos WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(montadora) && montadora != "Todas")
            {
                sql += " AND Montadora = @Montadora";
                cmd.Parameters.AddWithValue("@Montadora", montadora);
            }

            if (!string.IsNullOrWhiteSpace(tensao) && tensao != "Todas")
            {
                sql += " AND TensaoOperacao = @Tensao";
                cmd.Parameters.AddWithValue("@Tensao", tensao);
            }

            if (!string.IsNullOrWhiteSpace(termo))
            {
                sql += " AND (NomeModulo LIKE @Termo OR CodigoModulo LIKE @Termo OR ModelosAplicacao LIKE @Termo OR SistemaTipo LIKE @Termo OR Montadora LIKE @Termo)";
                cmd.Parameters.AddWithValue("@Termo", $"%{termo.Trim()}%");
            }

            sql += " ORDER BY Montadora, NomeModulo;";
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                modulos.Add(new PinagemModulo
                {
                    Id = reader.GetInt32(0),
                    CodigoModulo = reader.GetString(1),
                    NomeModulo = reader.GetString(2),
                    Montadora = reader.GetString(3),
                    ModelosAplicacao = reader.GetString(4),
                    SistemaTipo = reader.GetString(5),
                    TensaoOperacao = reader.GetString(6),
                    DescricaoConectores = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    ObservacoesTecnicas = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                });
            }

            // Carrega pinos para cada modulo retornado
            foreach (var m in modulos)
            {
                m.Pinos = await ObterPinosDoModuloAsync(conn, m.Id);
            }

            return modulos;
        }

        public async Task<PinagemModulo?> ObterModuloPorCodigoAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;
            await GarantirCargaInicialAsync();

            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, CodigoModulo, NomeModulo, Montadora, ModelosAplicacao, SistemaTipo, TensaoOperacao, DescricaoConectores, ObservacoesTecnicas 
                                FROM ModulosEletronicos 
                                WHERE CodigoModulo = @Codigo OR Id = @CodigoId LIMIT 1;";
            cmd.Parameters.AddWithValue("@Codigo", codigo.Trim());
            int idParsed = int.TryParse(codigo, out var parsed) ? parsed : -1;
            cmd.Parameters.AddWithValue("@CodigoId", idParsed);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var modulo = new PinagemModulo
                {
                    Id = reader.GetInt32(0),
                    CodigoModulo = reader.GetString(1),
                    NomeModulo = reader.GetString(2),
                    Montadora = reader.GetString(3),
                    ModelosAplicacao = reader.GetString(4),
                    SistemaTipo = reader.GetString(5),
                    TensaoOperacao = reader.GetString(6),
                    DescricaoConectores = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    ObservacoesTecnicas = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                };

                modulo.Pinos = await ObterPinosDoModuloAsync(conn, modulo.Id);
                return modulo;
            }

            return null;
        }

        private async Task<List<PinoConectorInfo>> ObterPinosDoModuloAsync(DbConnection conn, int moduloId)
        {
            var pinos = new List<PinoConectorInfo>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, ModuloId, Conector, NumeroPino, FuncaoSinal, TipoSinal, CorFio, TensaoEsperada, ObservacoesTecnicas 
                                FROM PinosConectores 
                                WHERE ModuloId = @ModuloId 
                                ORDER BY Conector, NumeroPino;";
            cmd.Parameters.AddWithValue("@ModuloId", moduloId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                pinos.Add(new PinoConectorInfo
                {
                    Id = reader.GetInt32(0),
                    ModuloId = reader.GetInt32(1),
                    Conector = reader.GetString(2),
                    NumeroPino = reader.GetString(3),
                    FuncaoSinal = reader.GetString(4),
                    TipoSinal = reader.GetString(5),
                    CorFio = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    TensaoEsperada = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    ObservacoesTecnicas = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                });
            }

            return pinos;
        }

        public async Task<List<CentralEletricaFusivel>> ObterCentraisEletricasAsync(string? termoBusca = null, string? montadora = null, string? tensao = null)
        {
            await GarantirCargaInicialAsync();

            var centrais = await ExecutarBuscaCentraisSqlAsync(termoBusca, montadora, tensao);
            if (centrais.Count == 0 && !string.IsNullOrWhiteSpace(termoBusca))
            {
                var tokens = ExtrairTermosRelevantes(termoBusca);
                foreach (var token in tokens)
                {
                    var parciais = await ExecutarBuscaCentraisSqlAsync(token, montadora, tensao);
                    foreach (var c in parciais)
                    {
                        if (!centrais.Any(x => x.Id == c.Id))
                        {
                            centrais.Add(c);
                        }
                    }
                }
            }

            return centrais;
        }

        private async Task<List<CentralEletricaFusivel>> ExecutarBuscaCentraisSqlAsync(string? termo, string? montadora, string? tensao)
        {
            var centrais = new List<CentralEletricaFusivel>();
            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();

            var sql = "SELECT Id, CodigoCentral, Titulo, Montadora, ModelosAplicacao, Localizacao, TensaoNominal, FusiveisJson, RelesJson FROM CentraisEletricas WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(montadora) && montadora != "Todas")
            {
                sql += " AND Montadora = @Montadora";
                cmd.Parameters.AddWithValue("@Montadora", montadora);
            }

            if (!string.IsNullOrWhiteSpace(tensao) && tensao != "Todas")
            {
                sql += " AND TensaoNominal = @Tensao";
                cmd.Parameters.AddWithValue("@Tensao", tensao);
            }

            if (!string.IsNullOrWhiteSpace(termo))
            {
                sql += " AND (Titulo LIKE @Termo OR CodigoCentral LIKE @Termo OR ModelosAplicacao LIKE @Termo OR Montadora LIKE @Termo OR FusiveisJson LIKE @Termo OR RelesJson LIKE @Termo)";
                cmd.Parameters.AddWithValue("@Termo", $"%{termo.Trim()}%");
            }

            sql += " ORDER BY Montadora, Titulo;";
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var fusiveisJson = reader.GetString(7);
                var relesJson = reader.GetString(8);

                var fusiveis = JsonSerializer.Deserialize<List<FusivelItemInfo>>(fusiveisJson) ?? new();
                var reles = JsonSerializer.Deserialize<List<ReleItemInfo>>(relesJson) ?? new();

                centrais.Add(new CentralEletricaFusivel
                {
                    Id = reader.GetInt32(0),
                    CodigoCentral = reader.GetString(1),
                    Titulo = reader.GetString(2),
                    Montadora = reader.GetString(3),
                    ModelosAplicacao = reader.GetString(4),
                    Localizacao = reader.GetString(5),
                    TensaoNominal = reader.GetString(6),
                    Fusiveis = fusiveis,
                    Reles = reles
                });
            }

            return centrais;
        }

        public async Task<CentralEletricaFusivel?> ObterCentralPorCodigoAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;
            await GarantirCargaInicialAsync();

            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, CodigoCentral, Titulo, Montadora, ModelosAplicacao, Localizacao, TensaoNominal, FusiveisJson, RelesJson 
                                FROM CentraisEletricas 
                                WHERE CodigoCentral = @Codigo OR Id = @CodigoId LIMIT 1;";
            cmd.Parameters.AddWithValue("@Codigo", codigo.Trim());
            int idParsed = int.TryParse(codigo, out var parsed) ? parsed : -1;
            cmd.Parameters.AddWithValue("@CodigoId", idParsed);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var fusiveis = JsonSerializer.Deserialize<List<FusivelItemInfo>>(reader.GetString(7)) ?? new();
                var reles = JsonSerializer.Deserialize<List<ReleItemInfo>>(reader.GetString(8)) ?? new();

                return new CentralEletricaFusivel
                {
                    Id = reader.GetInt32(0),
                    CodigoCentral = reader.GetString(1),
                    Titulo = reader.GetString(2),
                    Montadora = reader.GetString(3),
                    ModelosAplicacao = reader.GetString(4),
                    Localizacao = reader.GetString(5),
                    TensaoNominal = reader.GetString(6),
                    Fusiveis = fusiveis,
                    Reles = reles
                };
            }

            return null;
        }

        public async Task<List<VeiculoPesado24VEspecificacao>> ObterEspecificacoesPesadosAsync(string? termoBusca = null)
        {
            await GarantirCargaInicialAsync();

            var lista = await ExecutarBuscaPesadosSqlAsync(termoBusca);
            if (lista.Count == 0 && !string.IsNullOrWhiteSpace(termoBusca))
            {
                var tokens = ExtrairTermosRelevantes(termoBusca);
                foreach (var token in tokens)
                {
                    var parciais = await ExecutarBuscaPesadosSqlAsync(token);
                    foreach (var item in parciais)
                    {
                        if (!lista.Any(x => x.Id == item.Id))
                        {
                            lista.Add(item);
                        }
                    }
                }
            }

            return lista;
        }

        private async Task<List<VeiculoPesado24VEspecificacao>> ExecutarBuscaPesadosSqlAsync(string? termo)
        {
            var lista = new List<VeiculoPesado24VEspecificacao>();
            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();

            var sql = "SELECT Id, Montadora, Modelo, TensaoSistema, AlternadorEspecificacao, BateriasEspecificacao, ConsumoStandbyMaximo, TorqueCabecote, FolgaValvulas, ArCondicionadoGasGramas, ArCondicionadoOleoTipo, DicasEletricasChassi FROM EspecificacoesLinhaPesada24V WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(termo))
            {
                sql += " AND (Montadora LIKE @Termo OR Modelo LIKE @Termo OR AlternadorEspecificacao LIKE @Termo OR DicasEletricasChassi LIKE @Termo)";
                cmd.Parameters.AddWithValue("@Termo", $"%{termo.Trim()}%");
            }

            sql += " ORDER BY Montadora, Modelo;";
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new VeiculoPesado24VEspecificacao
                {
                    Id = reader.GetInt32(0),
                    Montadora = reader.GetString(1),
                    Modelo = reader.GetString(2),
                    TensaoSistema = reader.GetString(3),
                    AlternadorEspecificacao = reader.GetString(4),
                    BateriasEspecificacao = reader.GetString(5),
                    ConsumoStandbyMaximo = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    TorqueCabecote = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    FolgaValvulas = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    ArCondicionadoGasGramas = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                    ArCondicionadoOleoTipo = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    DicasEletricasChassi = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
                });
            }

            return lista;
        }

        public async Task<VeiculoPesado24VEspecificacao?> ObterEspecificacaoPesadoPorModeloAsync(string modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo)) return null;
            await GarantirCargaInicialAsync();

            var item = await ObterEspecificacaoPesadoPorModeloSqlAsync(modelo);
            if (item != null) return item;

            var tokens = ExtrairTermosRelevantes(modelo);
            foreach (var token in tokens)
            {
                item = await ObterEspecificacaoPesadoPorModeloSqlAsync(token);
                if (item != null) return item;
            }

            return null;
        }

        private async Task<VeiculoPesado24VEspecificacao?> ObterEspecificacaoPesadoPorModeloSqlAsync(string modelo)
        {
            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, Montadora, Modelo, TensaoSistema, AlternadorEspecificacao, BateriasEspecificacao, ConsumoStandbyMaximo, TorqueCabecote, FolgaValvulas, ArCondicionadoGasGramas, ArCondicionadoOleoTipo, DicasEletricasChassi 
                                FROM EspecificacoesLinhaPesada24V 
                                WHERE Modelo LIKE @Modelo OR Montadora LIKE @Modelo LIMIT 1;";
            cmd.Parameters.AddWithValue("@Modelo", $"%{modelo.Trim()}%");

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new VeiculoPesado24VEspecificacao
                {
                    Id = reader.GetInt32(0),
                    Montadora = reader.GetString(1),
                    Modelo = reader.GetString(2),
                    TensaoSistema = reader.GetString(3),
                    AlternadorEspecificacao = reader.GetString(4),
                    BateriasEspecificacao = reader.GetString(5),
                    ConsumoStandbyMaximo = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    TorqueCabecote = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    FolgaValvulas = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    ArCondicionadoGasGramas = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                    ArCondicionadoOleoTipo = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    DicasEletricasChassi = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
                };
            }

            return null;
        }

        #region Seed Curado de Fábrica e Bancada

        private async Task InserirCargaInicialModulosAsync(DbConnection conn)
        {
            var modulosSeed = ObterModulosCurados();
            using var trans = conn.BeginTransaction();
            try
            {
                foreach (var m in modulosSeed)
                {
                    using var cmdMod = conn.CreateCommand();
                    cmdMod.Transaction = trans;
                    cmdMod.CommandText = @"
                        INSERT INTO ModulosEletronicos (
                            CodigoModulo, NomeModulo, Montadora, ModelosAplicacao, SistemaTipo, TensaoOperacao, DescricaoConectores, ObservacoesTecnicas
                        ) VALUES (
                            @Codigo, @Nome, @Montadora, @Modelos, @Sistema, @Tensao, @Conectores, @Obs
                        );
                        SELECT last_insert_rowid();";

                    cmdMod.Parameters.AddWithValue("@Codigo", m.CodigoModulo);
                    cmdMod.Parameters.AddWithValue("@Nome", m.NomeModulo);
                    cmdMod.Parameters.AddWithValue("@Montadora", m.Montadora);
                    cmdMod.Parameters.AddWithValue("@Modelos", m.ModelosAplicacao);
                    cmdMod.Parameters.AddWithValue("@Sistema", m.SistemaTipo);
                    cmdMod.Parameters.AddWithValue("@Tensao", m.TensaoOperacao);
                    cmdMod.Parameters.AddWithValue("@Conectores", m.DescricaoConectores);
                    cmdMod.Parameters.AddWithValue("@Obs", m.ObservacoesTecnicas);

                    var moduloIdObj = await cmdMod.ExecuteScalarAsync();
                    int moduloId = Convert.ToInt32(moduloIdObj);

                    foreach (var p in m.Pinos)
                    {
                        using var cmdPin = conn.CreateCommand();
                        cmdPin.Transaction = trans;
                        cmdPin.CommandText = @"
                            INSERT INTO PinosConectores (
                                ModuloId, Conector, NumeroPino, FuncaoSinal, TipoSinal, CorFio, TensaoEsperada, ObservacoesTecnicas
                            ) VALUES (
                                @ModuloId, @Conector, @NumeroPino, @Funcao, @Tipo, @Cor, @Tensao, @Obs
                            );";

                        cmdPin.Parameters.AddWithValue("@ModuloId", moduloId);
                        cmdPin.Parameters.AddWithValue("@Conector", p.Conector);
                        cmdPin.Parameters.AddWithValue("@NumeroPino", p.NumeroPino);
                        cmdPin.Parameters.AddWithValue("@Funcao", p.FuncaoSinal);
                        cmdPin.Parameters.AddWithValue("@Tipo", p.TipoSinal);
                        cmdPin.Parameters.AddWithValue("@Cor", p.CorFio);
                        cmdPin.Parameters.AddWithValue("@Tensao", p.TensaoEsperada);
                        cmdPin.Parameters.AddWithValue("@Obs", p.ObservacoesTecnicas);

                        await cmdPin.ExecuteNonQueryAsync();
                    }
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        private async Task InserirCargaInicialCentraisAsync(DbConnection conn)
        {
            var centraisSeed = ObterCentraisCuradas();
            using var trans = conn.BeginTransaction();
            try
            {
                foreach (var c in centraisSeed)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = trans;
                    cmd.CommandText = @"
                        INSERT OR REPLACE INTO CentraisEletricas (
                            CodigoCentral, Titulo, Montadora, ModelosAplicacao, Localizacao, TensaoNominal, FusiveisJson, RelesJson
                        ) VALUES (
                            @Codigo, @Titulo, @Montadora, @Modelos, @Local, @Tensao, @Fusiveis, @Reles
                        );";

                    cmd.Parameters.AddWithValue("@Codigo", c.CodigoCentral);
                    cmd.Parameters.AddWithValue("@Titulo", c.Titulo);
                    cmd.Parameters.AddWithValue("@Montadora", c.Montadora);
                    cmd.Parameters.AddWithValue("@Modelos", c.ModelosAplicacao);
                    cmd.Parameters.AddWithValue("@Local", c.Localizacao);
                    cmd.Parameters.AddWithValue("@Tensao", c.TensaoNominal);
                    cmd.Parameters.AddWithValue("@Fusiveis", JsonSerializer.Serialize(c.Fusiveis));
                    cmd.Parameters.AddWithValue("@Reles", JsonSerializer.Serialize(c.Reles));

                    await cmd.ExecuteNonQueryAsync();
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        private async Task InserirCargaInicialPesadosAsync(DbConnection conn)
        {
            var pesadosSeed = ObterPesadosCurados();
            using var trans = conn.BeginTransaction();
            try
            {
                foreach (var p in pesadosSeed)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = trans;
                    cmd.CommandText = @"
                        INSERT OR REPLACE INTO EspecificacoesLinhaPesada24V (
                            Montadora, Modelo, TensaoSistema, AlternadorEspecificacao, BateriasEspecificacao, ConsumoStandbyMaximo, TorqueCabecote, FolgaValvulas, ArCondicionadoGasGramas, ArCondicionadoOleoTipo, DicasEletricasChassi
                        ) VALUES (
                            @Montadora, @Modelo, @Tensao, @Alternador, @Baterias, @Consumo, @Torque, @Folga, @Gas, @Oleo, @Dicas
                        );";

                    cmd.Parameters.AddWithValue("@Montadora", p.Montadora);
                    cmd.Parameters.AddWithValue("@Modelo", p.Modelo);
                    cmd.Parameters.AddWithValue("@Tensao", p.TensaoSistema);
                    cmd.Parameters.AddWithValue("@Alternador", p.AlternadorEspecificacao);
                    cmd.Parameters.AddWithValue("@Baterias", p.BateriasEspecificacao);
                    cmd.Parameters.AddWithValue("@Consumo", p.ConsumoStandbyMaximo);
                    cmd.Parameters.AddWithValue("@Torque", p.TorqueCabecote);
                    cmd.Parameters.AddWithValue("@Folga", p.FolgaValvulas);
                    cmd.Parameters.AddWithValue("@Gas", p.ArCondicionadoGasGramas);
                    cmd.Parameters.AddWithValue("@Oleo", p.ArCondicionadoOleoTipo);
                    cmd.Parameters.AddWithValue("@Dicas", p.DicasEletricasChassi);

                    await cmd.ExecuteNonQueryAsync();
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        private List<PinagemModulo> ObterModulosCurados()
        {
            var lista = new List<PinagemModulo>();

            // 1. VW Bosch ME 7.5.20 / ME 7.5.30 (EA111)
            var vwMe75 = new PinagemModulo
            {
                CodigoModulo = "BOSCH-ME7520",
                NomeModulo = "Bosch Motronic ME 7.5.20 / ME 7.5.30",
                Montadora = "Volkswagen",
                ModelosAplicacao = "Gol G4/G5/G6, Fox, Voyage, Saveiro 1.0 e 1.6 TotalFlex (EA111)",
                SistemaTipo = "Injeção Eletrônica Flex Leve",
                TensaoOperacao = "12V",
                DescricaoConectores = "Conector A (Cinza / Menor - 40 Pinos) e Conector B (Preto / Maior - 64 Pinos)",
                ObservacoesTecnicas = "ECU montada na parede corta-fogo. Aterramento central da carcaça no bloco e cabeçote."
            };
            vwMe75.Pinos.AddRange(new[]
            {
                new PinoConectorInfo { Conector = "Conector A", NumeroPino = "03", FuncaoSinal = "Linha 30 (+12V Bateria Permanente)", TipoSinal = "Alimentação Positiva", CorFio = "Vermelho/Branco", TensaoEsperada = "12.6V", ObservacoesTecnicas = "Protegido pelo fusível F14 (15A)." },
                new PinoConectorInfo { Conector = "Conector A", NumeroPino = "15", FuncaoSinal = "Linha 15 (+12V Pós-Chave Ignição)", TipoSinal = "Alimentação Positiva", CorFio = "Preto/Azul", TensaoEsperada = "12.4V", ObservacoesTecnicas = "Com chave ligada. Se faltar, motor não dá partida." },
                new PinoConectorInfo { Conector = "Conector A", NumeroPino = "01", FuncaoSinal = "Linha 31 (Massa / Terra de Potência)", TipoSinal = "Massa / Aterramento", CorFio = "Marrom", TensaoEsperada = "0.0V (<= 0.05V)", ObservacoesTecnicas = "Aterramento no cabeçote. Queda de tensão >0.1V causa falhas randômicas." },
                new PinoConectorInfo { Conector = "Conector A", NumeroPino = "02", FuncaoSinal = "Linha 31 (Massa / Terra de Potência)", TipoSinal = "Massa / Aterramento", CorFio = "Marrom", TensaoEsperada = "0.0V (<= 0.05V)", ObservacoesTecnicas = "Aterramento duplo obrigatório." },
                new PinoConectorInfo { Conector = "Conector A", NumeroPino = "60", FuncaoSinal = "CAN-High (Comunicação Rede de Bordo)", TipoSinal = "Comunicação CAN / K", CorFio = "Laranja/Preto", TensaoEsperada = "2.5V a 3.5V", ObservacoesTecnicas = "60 Ohms entre pinos 60 e 58 com bateria desligada." },
                new PinoConectorInfo { Conector = "Conector A", NumeroPino = "58", FuncaoSinal = "CAN-Low (Comunicação Rede de Bordo)", TipoSinal = "Comunicação CAN / K", CorFio = "Laranja/Marrom", TensaoEsperada = "1.5V a 2.5V", ObservacoesTecnicas = "Comunicação com Painel de Instrumentos e Imobilizador." },
                new PinoConectorInfo { Conector = "Conector B", NumeroPino = "05", FuncaoSinal = "Comando Bobina Cilindros 1 e 4", TipoSinal = "Saída Atuador / PWM", CorFio = "Verde/Branco", TensaoEsperada = "Pulso 5V / Dwell", ObservacoesTecnicas = "Bobina estática com módulo de potência integrado." },
                new PinoConectorInfo { Conector = "Conector B", NumeroPino = "06", FuncaoSinal = "Comando Bobina Cilindros 2 e 3", TipoSinal = "Saída Atuador / PWM", CorFio = "Verde/Preto", TensaoEsperada = "Pulso 5V / Dwell", ObservacoesTecnicas = "Se faltar faísca em dois cilindros, conferir este pulso." },
                new PinoConectorInfo { Conector = "Conector B", NumeroPino = "12", FuncaoSinal = "Eletroinjetor 1 (Comando Negativo Pulsado)", TipoSinal = "Saída Atuador / PWM", CorFio = "Marrom/Branco", TensaoEsperada = "Pico Indutivo ~60V", ObservacoesTecnicas = "Resistência do injetor: 11 a 14 Ohms." },
                new PinoConectorInfo { Conector = "Conector B", NumeroPino = "13", FuncaoSinal = "Eletroinjetor 2 (Comando Negativo Pulsado)", TipoSinal = "Saída Atuador / PWM", CorFio = "Marrom/Amarelo", TensaoEsperada = "Pico Indutivo ~60V", ObservacoesTecnicas = "Resistência do injetor: 11 a 14 Ohms." },
                new PinoConectorInfo { Conector = "Conector B", NumeroPino = "26", FuncaoSinal = "Eletroinjetor 3 (Comando Negativo Pulsado)", TipoSinal = "Saída Atuador / PWM", CorFio = "Marrom/Azul", TensaoEsperada = "Pico Indutivo ~60V", ObservacoesTecnicas = "Resistência do injetor: 11 a 14 Ohms." },
                new PinoConectorInfo { Conector = "Conector B", NumeroPino = "27", FuncaoSinal = "Eletroinjetor 4 (Comando Negativo Pulsado)", TipoSinal = "Saída Atuador / PWM", CorFio = "Marrom/Cinza", TensaoEsperada = "Pico Indutivo ~60V", ObservacoesTecnicas = "Resistência do injetor: 11 a 14 Ohms." },
                new PinoConectorInfo { Conector = "Conector B", NumeroPino = "35", FuncaoSinal = "Sensor de Rotação CKP (Sinal Hall)", TipoSinal = "Sinal Sensor", CorFio = "Preto/Cinza", TensaoEsperada = "Onda Quadrada 0V-5V", ObservacoesTecnicas = "Roda fônica 60-2 montada no flange traseiro do virabrequim." },
                new PinoConectorInfo { Conector = "Conector B", NumeroPino = "41", FuncaoSinal = "Comando Relé da Bomba de Combustível", TipoSinal = "Saída Atuador / PWM", CorFio = "Azul/Amarelo", TensaoEsperada = "0V acionado / 12V off", ObservacoesTecnicas = "Aterra o terminal 85 do Relé Principal/Bomba R02." },
                new PinoConectorInfo { Conector = "Conector B", NumeroPino = "48", FuncaoSinal = "Comando Eletroventilador 1ª Velocidade", TipoSinal = "Saída Atuador / PWM", CorFio = "Lilás/Branco", TensaoEsperada = "0V acionado / 12V off", ObservacoesTecnicas = "Disparo com 96°C ou quando acionado o ar-condicionado." }
            });
            lista.Add(vwMe75);

            // 2. GM Delphi MT27E / MT80 (SPE/4)
            var gmDelphi = new PinagemModulo
            {
                CodigoModulo = "DELPHI-MT27E",
                NomeModulo = "Delphi MT27E / MT80",
                Montadora = "Chevrolet / GM",
                ModelosAplicacao = "Onix, Prisma, Spin, Cobalt, Sonic 1.0/1.4/1.8 SPE/4 e EconoFlex",
                SistemaTipo = "Injeção Eletrônica Flex Leve",
                TensaoOperacao = "12V",
                DescricaoConectores = "Conector J1 (Preto - 73 Pinos) e Conector J2 (Cinza - 73 Pinos)",
                ObservacoesTecnicas = "ECU integrada ao compartimento do motor ao lado da bateria."
            };
            gmDelphi.Pinos.AddRange(new[]
            {
                new PinoConectorInfo { Conector = "Conector J1", NumeroPino = "01", FuncaoSinal = "Linha 31 (Terra Geral de Potência)", TipoSinal = "Massa / Aterramento", CorFio = "Preto", TensaoEsperada = "0.0V", ObservacoesTecnicas = "Ligado diretamente ao polo negativo da bateria." },
                new PinoConectorInfo { Conector = "Conector J1", NumeroPino = "19", FuncaoSinal = "Linha 30 (+12V Bateria Protegido)", TipoSinal = "Alimentação Positiva", CorFio = "Vermelho", TensaoEsperada = "12.6V", ObservacoesTecnicas = "Fusível F18 da central da bateria (20A)." },
                new PinoConectorInfo { Conector = "Conector J1", NumeroPino = "20", FuncaoSinal = "Linha 15 (+12V Chave de Ignição)", TipoSinal = "Alimentação Positiva", CorFio = "Rosa/Preto", TensaoEsperada = "12.4V", ObservacoesTecnicas = "Alimentação pós-relé principal de ignição." },
                new PinoConectorInfo { Conector = "Conector J1", NumeroPino = "38", FuncaoSinal = "GMLAN CAN-High (Comunicação BCM e Painel)", TipoSinal = "Comunicação CAN / K", CorFio = "Azul", TensaoEsperada = "2.6V a 3.5V", ObservacoesTecnicas = "Barramento de 500 kbps para rede de alta velocidade." },
                new PinoConectorInfo { Conector = "Conector J1", NumeroPino = "39", FuncaoSinal = "GMLAN CAN-Low (Comunicação BCM e Painel)", TipoSinal = "Comunicação CAN / K", CorFio = "Branco", TensaoEsperada = "1.5V a 2.4V", ObservacoesTecnicas = "Resistência terminal combinada 60 Ohms." },
                new PinoConectorInfo { Conector = "Conector J2", NumeroPino = "08", FuncaoSinal = "Comando Injetor 1 (Pulsado à Massa)", TipoSinal = "Saída Atuador / PWM", CorFio = "Verde/Preto", TensaoEsperada = "Pico Indutivo ~65V", ObservacoesTecnicas = "Resistência da bobina do injetor: 12 Ohms." },
                new PinoConectorInfo { Conector = "Conector J2", NumeroPino = "09", FuncaoSinal = "Comando Injetor 2 (Pulsado à Massa)", TipoSinal = "Saída Atuador / PWM", CorFio = "Verde/Branco", TensaoEsperada = "Pico Indutivo ~65V", ObservacoesTecnicas = "Resistência da bobina do injetor: 12 Ohms." },
                new PinoConectorInfo { Conector = "Conector J2", NumeroPino = "10", FuncaoSinal = "Comando Injetor 3 (Pulsado à Massa)", TipoSinal = "Saída Atuador / PWM", CorFio = "Verde/Azul", TensaoEsperada = "Pico Indutivo ~65V", ObservacoesTecnicas = "Resistência da bobina do injetor: 12 Ohms." },
                new PinoConectorInfo { Conector = "Conector J2", NumeroPino = "11", FuncaoSinal = "Comando Injetor 4 (Pulsado à Massa)", TipoSinal = "Saída Atuador / PWM", CorFio = "Verde/Vermelho", TensaoEsperada = "Pico Indutivo ~65V", ObservacoesTecnicas = "Resistência da bobina do injetor: 12 Ohms." },
                new PinoConectorInfo { Conector = "Conector J2", NumeroPino = "25", FuncaoSinal = "Motor Borboleta Eletrônica M+ (Abertura)", TipoSinal = "Saída Atuador / PWM", CorFio = "Amarelo/Preto", TensaoEsperada = "PWM 12V Bipolar", ObservacoesTecnicas = "Comando em ponte H com inversão de polaridade." },
                new PinoConectorInfo { Conector = "Conector J2", NumeroPino = "26", FuncaoSinal = "Motor Borboleta Eletrônica M- (Fechamento)", TipoSinal = "Saída Atuador / PWM", CorFio = "Marrom/Preto", TensaoEsperada = "PWM 12V Bipolar", ObservacoesTecnicas = "Resistência do motor do TBI: 3.5 a 5.5 Ohms." },
                new PinoConectorInfo { Conector = "Conector J2", NumeroPino = "44", FuncaoSinal = "Sensor ECT Temperatura da Água", TipoSinal = "Sinal Sensor", CorFio = "Amarelo", TensaoEsperada = "2.8V frio / 0.5V quente", ObservacoesTecnicas = "NTC com pull-up interno de 5V." }
            });
            lista.Add(gmDelphi);

            // 3. Fiat Magneti Marelli 4AF / 4DF (Fire Flex)
            var fiatMarelli = new PinagemModulo
            {
                CodigoModulo = "MARELLI-4AF",
                NomeModulo = "Magneti Marelli IAW 4AF / 4DF",
                Montadora = "Fiat",
                ModelosAplicacao = "Palio, Siena, Strada, Uno, Weekend 1.0 e 1.4 8V Fire Flex",
                SistemaTipo = "Injeção Eletrônica Flex Leve",
                TensaoOperacao = "12V",
                DescricaoConectores = "Conector do Chicote do Motor (Cinza / 52 Pinos) e Conector do Chicote do Veículo (Preto / 28 Pinos)",
                ObservacoesTecnicas = "Fixada na caixa de ar ou no coletor. Crônico: queima do drive de bobina por velas desgastadas."
            };
            fiatMarelli.Pinos.AddRange(new[]
            {
                new PinoConectorInfo { Conector = "Conector Veículo", NumeroPino = "04", FuncaoSinal = "Linha 15 (+12V Pós-Chave Ignição)", TipoSinal = "Alimentação Positiva", CorFio = "Laranja/Branco", TensaoEsperada = "12.4V", ObservacoesTecnicas = "Vem do comutador de ignição protegido por fusível F24." },
                new PinoConectorInfo { Conector = "Conector Veículo", NumeroPino = "23", FuncaoSinal = "Linha 30 (+12V Bateria Permanente)", TipoSinal = "Alimentação Positiva", CorFio = "Vermelho", TensaoEsperada = "12.6V", ObservacoesTecnicas = "Memória da ECU. Se cortar, zera parâmetros adaptativos de combustível." },
                new PinoConectorInfo { Conector = "Conector Veículo", NumeroPino = "25", FuncaoSinal = "Linha K (Diagnose OBD-II Pino 7)", TipoSinal = "Comunicação CAN / K", CorFio = "Cinza/Vermelho", TensaoEsperada = "11.5V em repouso", ObservacoesTecnicas = "Protocolo ISO 9141-2 para scanner automotivo." },
                new PinoConectorInfo { Conector = "Conector Motor", NumeroPino = "01", FuncaoSinal = "Linha 31 (Massa / Terra do Motor)", TipoSinal = "Massa / Aterramento", CorFio = "Preto", TensaoEsperada = "0.0V", ObservacoesTecnicas = "Aterrado na carcaça do câmbio / motor Fire." },
                new PinoConectorInfo { Conector = "Conector Motor", NumeroPino = "02", FuncaoSinal = "Linha 31 (Massa / Terra do Motor)", TipoSinal = "Massa / Aterramento", CorFio = "Preto", TensaoEsperada = "0.0V", ObservacoesTecnicas = "Ponto de aterramento redundante de potência." },
                new PinoConectorInfo { Conector = "Conector Motor", NumeroPino = "10", FuncaoSinal = "Comando Bobina A (Cilindros 1 e 4)", TipoSinal = "Saída Atuador / PWM", CorFio = "Azul/Vermelho", TensaoEsperada = "Pico Indutivo ~350V", ObservacoesTecnicas = "Transistor interno chaveia negativo primário da bobina." },
                new PinoConectorInfo { Conector = "Conector Motor", NumeroPino = "38", FuncaoSinal = "Comando Bobina B (Cilindros 2 e 3)", TipoSinal = "Saída Atuador / PWM", CorFio = "Azul/Preto", TensaoEsperada = "Pico Indutivo ~350V", ObservacoesTecnicas = "Atenção: vela com gap excessivo queima o driver interno da ECU!" },
                new PinoConectorInfo { Conector = "Conector Motor", NumeroPino = "15", FuncaoSinal = "Comando Eletroinjetor Cilindro 1", TipoSinal = "Saída Atuador / PWM", CorFio = "Castanho/Branco", TensaoEsperada = "Pico ~60V", ObservacoesTecnicas = "Resistência da bobina do bico: 13.5 a 15.5 Ohms." },
                new PinoConectorInfo { Conector = "Conector Motor", NumeroPino = "16", FuncaoSinal = "Comando Eletroinjetor Cilindro 2", TipoSinal = "Saída Atuador / PWM", CorFio = "Castanho/Vermelho", TensaoEsperada = "Pico ~60V", ObservacoesTecnicas = "Resistência da bobina do bico: 13.5 a 15.5 Ohms." },
                new PinoConectorInfo { Conector = "Conector Motor", NumeroPino = "28", FuncaoSinal = "Sensor de Rotação CKP Sinal +", TipoSinal = "Sinal Sensor", CorFio = "Branco", TensaoEsperada = "Onda Senoidal Indutiva", ObservacoesTecnicas = "Resistência da bobina do sensor indutivo: 550 a 680 Ohms." },
                new PinoConectorInfo { Conector = "Conector Motor", NumeroPino = "29", FuncaoSinal = "Sensor de Rotação CKP Sinal -", TipoSinal = "Sinal Sensor", CorFio = "Marrom", TensaoEsperada = "Malha de blindagem", ObservacoesTecnicas = "Cabo blindado contra ruído do alternador e ignição." }
            });
            lista.Add(fiatMarelli);

            // 4. Scania Coordenador COO7 (24V Linha Pesada)
            var scaniaCoo7 = new PinagemModulo
            {
                CodigoModulo = "SCANIA-COO7",
                NomeModulo = "Coordenador Central Scania COO7",
                Montadora = "Scania",
                ModelosAplicacao = "Séries P, G, R, Streamline P310, G400, R440, R480 Euro 5 (24V)",
                SistemaTipo = "Gateway & Coordenador Geral do Veículo 24V",
                TensaoOperacao = "24V",
                DescricaoConectores = "5 Conectores Multivias (C1 Preto, C2 Azul, C3 Verde, C4 Amarelo, C5 Vermelho)",
                ObservacoesTecnicas = "Cérebro do caminhão Scania. Interliga todas as redes CAN (Vermelha, Amarela, Verde) e distribui alimentação 24V."
            };
            scaniaCoo7.Pinos.AddRange(new[]
            {
                new PinoConectorInfo { Conector = "Conector C1", NumeroPino = "01", FuncaoSinal = "Linha 15 (+24V Pós-Chave Ignição)", TipoSinal = "Alimentação Positiva", CorFio = "Preto", TensaoEsperada = "24.5V a 28.4V", ObservacoesTecnicas = "Acionamento da chave geral e trava de partida." },
                new PinoConectorInfo { Conector = "Conector C1", NumeroPino = "02", FuncaoSinal = "Linha 30 (+24V Direto das Baterias)", TipoSinal = "Alimentação Positiva", CorFio = "Vermelho", TensaoEsperada = "25.2V a 28.5V", ObservacoesTecnicas = "Alimentação permanente protegida pelo fusível central F14 (10A 24V)." },
                new PinoConectorInfo { Conector = "Conector C1", NumeroPino = "03", FuncaoSinal = "Linha 31 (Massa / Terra Chassi)", TipoSinal = "Massa / Aterramento", CorFio = "Castanho", TensaoEsperada = "0.0V (<= 0.05V)", ObservacoesTecnicas = "Ponto de massa da travessa da cabine basculante." },
                new PinoConectorInfo { Conector = "Conector C2", NumeroPino = "05", FuncaoSinal = "CAN Amarela - High (Barramento de Motor EMS S7/S8)", TipoSinal = "Comunicação CAN / K", CorFio = "Amarelo", TensaoEsperada = "2.7V a 3.5V", ObservacoesTecnicas = "Rede CAN dedicada ao módulo do motor diesel Scania." },
                new PinoConectorInfo { Conector = "Conector C2", NumeroPino = "06", FuncaoSinal = "CAN Amarela - Low (Barramento de Motor EMS S7/S8)", TipoSinal = "Comunicação CAN / K", CorFio = "Branco", TensaoEsperada = "1.5V a 2.3V", ObservacoesTecnicas = "Resistência terminal 60 Ohms no circuito." },
                new PinoConectorInfo { Conector = "Conector C3", NumeroPino = "07", FuncaoSinal = "CAN Verde - High (Barramento do Painel ICL e Tacógrafo)", TipoSinal = "Comunicação CAN / K", CorFio = "Verde", TensaoEsperada = "2.7V a 3.5V", ObservacoesTecnicas = "Rede de instrumentação e telemetria de frota." },
                new PinoConectorInfo { Conector = "Conector C3", NumeroPino = "08", FuncaoSinal = "CAN Verde - Low (Barramento do Painel ICL e Tacógrafo)", TipoSinal = "Comunicação CAN / K", CorFio = "Castanho", TensaoEsperada = "1.5V a 2.3V", ObservacoesTecnicas = "Comunicação contínua com Tacógrafo digital BVDR." },
                new PinoConectorInfo { Conector = "Conector C4", NumeroPino = "01", FuncaoSinal = "CAN Vermelha - High (Barramento de Segurança Freios EBS)", TipoSinal = "Comunicação CAN / K", CorFio = "Vermelho/Branco", TensaoEsperada = "2.6V a 3.5V", ObservacoesTecnicas = "Comunicação prioritária com os moduladores de freio Knorr/Wabco." },
                new PinoConectorInfo { Conector = "Conector C4", NumeroPino = "02", FuncaoSinal = "CAN Vermelha - Low (Barramento de Segurança Freios EBS)", TipoSinal = "Comunicação CAN / K", CorFio = "Azul/Branco", TensaoEsperada = "1.5V a 2.4V", ObservacoesTecnicas = "Falha nesta rede acende triângulo vermelho e corta piloto automático." },
                new PinoConectorInfo { Conector = "Conector C5", NumeroPino = "04", FuncaoSinal = "Sinal de Velocidade D3 Tacógrafo (Linha Kitar)", TipoSinal = "Sinal Sensor", CorFio = "Amarelo/Azul", TensaoEsperada = "Pulsos 8V", ObservacoesTecnicas = "Sinal do sensor Kitas no câmbio Opticruise." }
            });
            lista.Add(scaniaCoo7);

            // 5. Mercedes-Benz PLD / MR (24V Linha Pesada)
            var mbPld = new PinagemModulo
            {
                CodigoModulo = "MB-PLD-MR",
                NomeModulo = "Módulo de Gerenciamento do Motor Mercedes-Benz PLD / MR",
                Montadora = "Mercedes-Benz",
                ModelosAplicacao = "Accelo 815/1016, Atego 1719/2426, Axor 1933/2544, Actros OM904/906/926/457 LA 24V",
                SistemaTipo = "Injeção Eletrônica Diesel 24V",
                TensaoOperacao = "24V",
                DescricaoConectores = "Conector do Veículo (16 Pinos - Chicote Chassi/Cabine) e Conector do Motor (55 Pinos - Chicote Unidades)",
                ObservacoesTecnicas = "Montado na lateral do bloco do motor diesel com resfriamento a combustível. Chaveia alta tensão nas unidades injetoras (UIS/PDE)."
            };
            mbPld.Pinos.AddRange(new[]
            {
                new PinoConectorInfo { Conector = "Conector Veículo (16V)", NumeroPino = "01", FuncaoSinal = "Linha 30 (+24V Bateria Permanente)", TipoSinal = "Alimentação Positiva", CorFio = "Vermelho/Preto", TensaoEsperada = "24.8V a 28.2V", ObservacoesTecnicas = "Fusível F06 do painel (10A 24V)." },
                new PinoConectorInfo { Conector = "Conector Veículo (16V)", NumeroPino = "02", FuncaoSinal = "Linha 30 (+24V Bateria Permanente)", TipoSinal = "Alimentação Positiva", CorFio = "Vermelho/Preto", TensaoEsperada = "24.8V a 28.2V", ObservacoesTecnicas = "Linha de alimentação redundante." },
                new PinoConectorInfo { Conector = "Conector Veículo (16V)", NumeroPino = "03", FuncaoSinal = "Linha 31 (Massa / Terra de Potência)", TipoSinal = "Massa / Aterramento", CorFio = "Marrom", TensaoEsperada = "0.0V", ObservacoesTecnicas = "Aterrado diretamente no bloco do motor." },
                new PinoConectorInfo { Conector = "Conector Veículo (16V)", NumeroPino = "04", FuncaoSinal = "Linha 31 (Massa / Terra de Potência)", TipoSinal = "Massa / Aterramento", CorFio = "Marrom", TensaoEsperada = "0.0V", ObservacoesTecnicas = "Aterramento duplo de potência." },
                new PinoConectorInfo { Conector = "Conector Veículo (16V)", NumeroPino = "07", FuncaoSinal = "CAN do Motor - High (Comunicação com Módulo ADM/FR)", TipoSinal = "Comunicação CAN / K", CorFio = "Amarelo", TensaoEsperada = "2.8V a 3.6V", ObservacoesTecnicas = "Link de comunicação essencial para liberação de partida e imobilizador." },
                new PinoConectorInfo { Conector = "Conector Veículo (16V)", NumeroPino = "08", FuncaoSinal = "CAN do Motor - Low (Comunicação com Módulo ADM/FR)", TipoSinal = "Comunicação CAN / K", CorFio = "Azul", TensaoEsperada = "1.4V a 2.2V", ObservacoesTecnicas = "Se desconectar, motor entra em modo de emergência limitado a 1.200 RPM." },
                new PinoConectorInfo { Conector = "Conector Veículo (16V)", NumeroPino = "11", FuncaoSinal = "Válvula Proporcional Top Brake (Freio Motor)", TipoSinal = "Saída Atuador / PWM", CorFio = "Cinza/Vermelho", TensaoEsperada = "24V acionamento", ObservacoesTecnicas = "Atua no cabeçote descomprimindo as válvulas de escape." },
                new PinoConectorInfo { Conector = "Conector Motor (55V)", NumeroPino = "01", FuncaoSinal = "Unidade Injetora Cilindro 1 (Alta Tensão Chaveada)", TipoSinal = "Saída Atuador / PWM", CorFio = "Preto/Amarelo", TensaoEsperada = "Pico Chaveado ~90V", ObservacoesTecnicas = "Tensão de disparo de alta frequência gerada por conversor boost interno." },
                new PinoConectorInfo { Conector = "Conector Motor (55V)", NumeroPino = "15", FuncaoSinal = "Sensor de Rotação do Volante (Sinal Indutivo)", TipoSinal = "Sinal Sensor", CorFio = "Branco/Preto", TensaoEsperada = "Onda Senoidal ~4V AC", ObservacoesTecnicas = "Resistência da bobina: 1.050 a 1.250 Ohms." },
                new PinoConectorInfo { Conector = "Conector Motor (55V)", NumeroPino = "17", FuncaoSinal = "Sensor de Fase do Comando (Sinal Indutivo)", TipoSinal = "Sinal Sensor", CorFio = "Cinza/Azul", TensaoEsperada = "Onda Senoidal ~2V AC", ObservacoesTecnicas = "Permite identificar a sincronização de injeção no 1º cilindro." }
            });
            lista.Add(mbPld);

            // 6. Volvo FH/FM LCM (24V Linha Pesada)
            var volvoLcm = new PinagemModulo
            {
                CodigoModulo = "VOLVO-LCM",
                NomeModulo = "Módulo Central de Chassi e Iluminação Volvo LCM",
                Montadora = "Volvo",
                ModelosAplicacao = "Volvo FH 400, FH 440, FH 480, FH 520, FM 370 24V D13A",
                SistemaTipo = "Módulo de Chassi e Iluminação Inteligente 24V",
                TensaoOperacao = "24V",
                DescricaoConectores = "3 Conectores de Alta Corrente (EA Cinza, EB Verde, EC Preto)",
                ObservacoesTecnicas = "Substitui relés e fusíveis convencionais de iluminação por transistores PROFET inteligentes com proteção contra curto-circuito."
            };
            volvoLcm.Pinos.AddRange(new[]
            {
                new PinoConectorInfo { Conector = "Conector EA", NumeroPino = "01", FuncaoSinal = "Linha 30 (+24V Bateria Alta Corrente)", TipoSinal = "Alimentação Positiva", CorFio = "Vermelho Grosso (6.0 mm²)", TensaoEsperada = "25.0V a 28.5V", ObservacoesTecnicas = "Barramento mestre de alimentação das lâmpadas e atuadores." },
                new PinoConectorInfo { Conector = "Conector EA", NumeroPino = "02", FuncaoSinal = "Linha 31 (Massa Principal de Potência)", TipoSinal = "Massa / Aterramento", CorFio = "Preto Grosso (6.0 mm²)", TensaoEsperada = "0.0V", ObservacoesTecnicas = "Ponto de massa no chassi dianteiro." },
                new PinoConectorInfo { Conector = "Conector EA", NumeroPino = "07", FuncaoSinal = "Saída Farol Baixo Esquerdo (PWM Estabilizado)", TipoSinal = "Saída Atuador / PWM", CorFio = "Amarelo/Branco", TensaoEsperada = "24V PWM Estabilizado", ObservacoesTecnicas = "LCM modula a tensão para 24.0V exatos para aumentar vida útil da lâmpada H7." },
                new PinoConectorInfo { Conector = "Conector EA", NumeroPino = "08", FuncaoSinal = "Saída Farol Baixo Direito (PWM Estabilizado)", TipoSinal = "Saída Atuador / PWM", CorFio = "Amarelo/Preto", TensaoEsperada = "24V PWM Estabilizado", ObservacoesTecnicas = "Proteção desliga automaticamente em caso de curto e grava código no painel." },
                new PinoConectorInfo { Conector = "Conector EB", NumeroPino = "03", FuncaoSinal = "Saída Farol Alto Esquerdo e Direito", TipoSinal = "Saída Atuador / PWM", CorFio = "Azul/Branco", TensaoEsperada = "24V Direto", ObservacoesTecnicas = "Linha 56a comutada por estado sólido." },
                new PinoConectorInfo { Conector = "Conector EB", NumeroPino = "12", FuncaoSinal = "Saída Luz de Freio Cavalo e Carreta", TipoSinal = "Saída Atuador / PWM", CorFio = "Vermelho/Amarelo", TensaoEsperada = "24V ao pisar no freio", ObservacoesTecnicas = "Alimenta tomada de 7 pinos ISO 1185 da carreta." },
                new PinoConectorInfo { Conector = "Conector EC", NumeroPino = "05", FuncaoSinal = "Barramento J1939 CAN - High", TipoSinal = "Comunicação CAN / K", CorFio = "Amarelo", TensaoEsperada = "2.8V a 3.5V", ObservacoesTecnicas = "Recebe comandos do interruptor de luzes e computador de bordo." },
                new PinoConectorInfo { Conector = "Conector EC", NumeroPino = "06", FuncaoSinal = "Barramento J1939 CAN - Low", TipoSinal = "Comunicação CAN / K", CorFio = "Verde", TensaoEsperada = "1.5V a 2.2V", ObservacoesTecnicas = "60 Ohms no barramento J1939." }
            });
            lista.Add(volvoLcm);

            return lista;
        }

        private List<CentralEletricaFusivel> ObterCentraisCuradas()
        {
            var lista = new List<CentralEletricaFusivel>();

            // 1. VW Gol G5 / Voyage EA111
            var vwCentral = new CentralEletricaFusivel
            {
                CodigoCentral = "CENTRAL-VW-EA111",
                Titulo = "Central Elétrica do Painel & Vão do Motor - VW Gol G5 / Voyage EA111",
                Montadora = "Volkswagen",
                ModelosAplicacao = "Gol G4/G5/G6, Voyage, Fox, Saveiro 1.0 e 1.6 TotalFlex",
                Localizacao = "Abaixo do Painel (Lado Motorista) e Caixa da Bateria",
                TensaoNominal = "12V",
                Fusiveis = new List<FusivelItemInfo>
                {
                    new FusivelItemInfo { Numero = "F01", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Painel de Instrumentos, Conector OBD-II e Luz de Cortesia", ReleAssociado = "R01 - Alívio X" },
                    new FusivelItemInfo { Numero = "F05", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Limpador e Lavador de Para-brisa", ReleAssociado = "R05 - Temporizador Limpador" },
                    new FusivelItemInfo { Numero = "F09", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Farol Baixo Esquerdo", ReleAssociado = "Direto Chave Farol" },
                    new FusivelItemInfo { Numero = "F10", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Farol Baixo Direito", ReleAssociado = "Direto Chave Farol" },
                    new FusivelItemInfo { Numero = "F13", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Bomba de Combustível e Bicos Injetores", ReleAssociado = "R02 - Relé Principal / Bomba" },
                    new FusivelItemInfo { Numero = "F14", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Módulo de Injeção Eletrônica ME 7.5.20 e Bobina", ReleAssociado = "R02 - Relé Principal" },
                    new FusivelItemInfo { Numero = "F19", CapacidadeAmperes = 30, CorPadrao = "Verde (30A)", CircuitoProtegido = "Eletroventilador de Arrefecimento 1ª Velocidade", ReleAssociado = "R03 - Relé Ventoinha 1" },
                    new FusivelItemInfo { Numero = "F20", CapacidadeAmperes = 40, CorPadrao = "Laranja (40A)", CircuitoProtegido = "Eletroventilador de Arrefecimento 2ª Velocidade (A/C)", ReleAssociado = "R04 - Relé Ventoinha 2" },
                    new FusivelItemInfo { Numero = "F24", CapacidadeAmperes = 20, CorPadrao = "Amarelo (20A)", CircuitoProtegido = "Vidros Elétricos Dianteiros", ReleAssociado = "R01 - Alívio Contato X" },
                    new FusivelItemInfo { Numero = "F28", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Tomada 12V e Acendedor de Cigarros", ReleAssociado = "Sem relé" }
                },
                Reles = new List<ReleItemInfo>
                {
                    new ReleItemInfo { Posicao = "R01", NomeFuncao = "Relé de Alívio Linha Contato X", TipoPinos = "4 Pinos (40A)", PinagemReferencia = "30=Bat, 85=Terra, 86=Chave Linha 15, 87=Cargas de Conforto (Desliga na partida)" },
                    new ReleItemInfo { Posicao = "R02", NomeFuncao = "Relé Principal da Injeção e Bomba de Combustível", TipoPinos = "4 Pinos (30A)", PinagemReferencia = "30=+12V Fusível F14, 85=Comando Negativo ECU pino B41, 86=+12V Pós-Chave, 87=Saída Bomba e Injetores" },
                    new ReleItemInfo { Posicao = "R03", NomeFuncao = "Relé do Eletroventilador Baixa Velocidade", TipoPinos = "4 Pinos (40A)", PinagemReferencia = "30=Bat Fusível F19, 85=ECU pino B48, 86=Linha 15, 87=Resistor Ventoinha" },
                    new ReleItemInfo { Posicao = "R04", NomeFuncao = "Relé do Eletroventilador Alta Velocidade", TipoPinos = "4 Pinos (50A Maxi)", PinagemReferencia = "30=Bat Fusível F20, 85=Pressostato A/C, 86=Linha 15, 87=Direto Motor Ventoinha" }
                }
            };
            lista.Add(vwCentral);

            // 2. GM Onix / Prisma SPE/4
            var gmCentral = new CentralEletricaFusivel
            {
                CodigoCentral = "CENTRAL-GM-ONIX",
                Titulo = "Central de Fusíveis do Vão do Motor e Bateria - Chevrolet Onix / Prisma SPE/4",
                Montadora = "Chevrolet / GM",
                ModelosAplicacao = "Onix, Prisma, Spin, Cobalt 1.0 e 1.4 SPE/4",
                Localizacao = "Vão do Motor (Sobre a Torre de Suspensão / Bateria)",
                TensaoNominal = "12V",
                Fusiveis = new List<FusivelItemInfo>
                {
                    new FusivelItemInfo { Numero = "F04", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Bomba de Combustível", ReleAssociado = "KR20 - Relé Bomba" },
                    new FusivelItemInfo { Numero = "F11", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Embreagem do Compressor do Ar-Condicionado", ReleAssociado = "KR44 - Relé Compressor A/C" },
                    new FusivelItemInfo { Numero = "F18", CapacidadeAmperes = 20, CorPadrao = "Amarelo (20A)", CircuitoProtegido = "Módulo de Gerenciamento do Motor ECM Delphi", ReleAssociado = "KR75 - Relé de Potência Motor" },
                    new FusivelItemInfo { Numero = "F22", CapacidadeAmperes = 30, CorPadrao = "Verde (30A)", CircuitoProtegido = "Motor de Partida (Solenóide Automático Linha 50)", ReleAssociado = "KR23 - Relé Motor de Partida" },
                    new FusivelItemInfo { Numero = "F31", CapacidadeAmperes = 40, CorPadrao = "Laranja (40A)", CircuitoProtegido = "Eletroventilador de Arrefecimento", ReleAssociado = "KR30 - Relé Ventoinha PWM" },
                    new FusivelItemInfo { Numero = "F35", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Bobinas de Ignição e Injetores de Combustível", ReleAssociado = "KR75 - Relé de Potência" }
                },
                Reles = new List<ReleItemInfo>
                {
                    new ReleItemInfo { Posicao = "KR20", NomeFuncao = "Relé da Bomba de Combustível", TipoPinos = "Micro Relé 4 Pinos (20A)", PinagemReferencia = "Comandado pela ECM pino J1-42" },
                    new ReleItemInfo { Posicao = "KR23", NomeFuncao = "Relé de Partida Linha 50", TipoPinos = "Micro Relé 4 Pinos (30A)", PinagemReferencia = "Comandado pelo BCM e trava do pedal de embreagem" },
                    new ReleItemInfo { Posicao = "KR44", NomeFuncao = "Relé do Compressor A/C", TipoPinos = "Micro Relé 4 Pinos (20A)", PinagemReferencia = "Comandado pelo botão do painel via rede CAN para a ECM" },
                    new ReleItemInfo { Posicao = "KR75", NomeFuncao = "Relé Principal de Potência do Motor", TipoPinos = "Mini Relé 4 Pinos (40A)", PinagemReferencia = "Chaveia positivo permanente para sensores e atuadores" }
                }
            };
            lista.Add(gmCentral);

            // 3. Scania Séries R / Streamline 24V
            var scaniaCentral = new CentralEletricaFusivel
            {
                CodigoCentral = "CENTRAL-SCANIA-R-24V",
                Titulo = "Central Elétrica Principal da Cabine 24V - Scania Série R / Streamline",
                Montadora = "Scania",
                ModelosAplicacao = "Scania R440, R480, G400, P310, Streamline Euro 5 (24V)",
                Localizacao = "Abaixo do Painel de Instrumentos (Lado Passageiro)",
                TensaoNominal = "24V",
                Fusiveis = new List<FusivelItemInfo>
                {
                    new FusivelItemInfo { Numero = "F01", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A 24V)", CircuitoProtegido = "Módulo de Gerenciamento do Motor EMS S8", ReleAssociado = "RP1 - Relé Ignição 24V" },
                    new FusivelItemInfo { Numero = "F07", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A 24V)", CircuitoProtegido = "Tacógrafo Digital BVDR (Alimentação Permanente Linha 30)", ReleAssociado = "Direto das Baterias" },
                    new FusivelItemInfo { Numero = "F12", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A 24V)", CircuitoProtegido = "Painel de Instrumentos ICL e Módulo Chave de Seta", ReleAssociado = "RP1 - Relé Ignição" },
                    new FusivelItemInfo { Numero = "F14", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A 24V)", CircuitoProtegido = "Coordenador Central COO7 (Linha 30)", ReleAssociado = "Alimentação Principal COO7" },
                    new FusivelItemInfo { Numero = "F18", CapacidadeAmperes = 20, CorPadrao = "Amarelo (20A 24V)", CircuitoProtegido = "Módulo de Freios Eletrônicos EBS Knorr", ReleAssociado = "RP5 - Relé de Freio" },
                    new FusivelItemInfo { Numero = "F22", CapacidadeAmperes = 15, CorPadrao = "Azul (15A 24V)", CircuitoProtegido = "Câmbio Automatizado Opticruise e Retarder", ReleAssociado = "RP8 - Relé de Transmissão" },
                    new FusivelItemInfo { Numero = "F31", CapacidadeAmperes = 20, CorPadrao = "Amarelo (20A 24V)", CircuitoProtegido = "Climatizador de Teto / Ar-Condicionado 24V", ReleAssociado = "RP11 - Relé Climatização" },
                    new FusivelItemInfo { Numero = "F40", CapacidadeAmperes = 25, CorPadrao = "Branco (25A 24V)", CircuitoProtegido = "Faróis de Longo Alcance no Teto (High Beam)", ReleAssociado = "RP14 - Relé Farol Teto 24V" }
                },
                Reles = new List<ReleItemInfo>
                {
                    new ReleItemInfo { Posicao = "RP1", NomeFuncao = "Relé Principal de Ignição Linha 15 (24V)", TipoPinos = "4 Pinos 24V (30A)", PinagemReferencia = "Bobina 24V. Nunca substituir por relé de 12V sob risco de queima imediata!" },
                    new ReleItemInfo { Posicao = "RP5", NomeFuncao = "Relé de Bloqueio de Partida", TipoPinos = "5 Pinos Reversor 24V", PinagemReferencia = "Controlado pelo COO7 para impedir partida com marcha engatada" },
                    new ReleItemInfo { Posicao = "RP14", NomeFuncao = "Relé dos Faróis de Teto 24V", TipoPinos = "4 Pinos 24V (40A)", PinagemReferencia = "Linha de potência direta das baterias protegida por F40" }
                }
            };
            lista.Add(scaniaCentral);

            // 4. Mercedes-Benz Atego 24V
            var mbCentral = new CentralEletricaFusivel
            {
                CodigoCentral = "CENTRAL-MB-ATEGO-24V",
                Titulo = "Central de Fusíveis do Painel 24V - Mercedes-Benz Atego 2426 / 1719",
                Montadora = "Mercedes-Benz",
                ModelosAplicacao = "Atego 1719, 2426, 2430, Axor 1933, Accelo Euro 5 (24V)",
                Localizacao = "Painel Lado Carona (Abaixo do Porta-Luvas)",
                TensaoNominal = "24V",
                Fusiveis = new List<FusivelItemInfo>
                {
                    new FusivelItemInfo { Numero = "F01", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A 24V)", CircuitoProtegido = "Painel de Instrumentos INS e Tacógrafo", ReleAssociado = "K01 - Relé Linha 15" },
                    new FusivelItemInfo { Numero = "F06", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A 24V)", CircuitoProtegido = "Módulo de Gerenciamento do Motor PLD / MR", ReleAssociado = "Alimentação direta 24V" },
                    new FusivelItemInfo { Numero = "F07", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A 24V)", CircuitoProtegido = "Módulo de Cabine ADM / FR (Imobilizador e Acelerador)", ReleAssociado = "K01 - Relé Linha 15" },
                    new FusivelItemInfo { Numero = "F11", CapacidadeAmperes = 15, CorPadrao = "Azul (15A 24V)", CircuitoProtegido = "Módulo de Tratamento de Gases SCR / Arla 32", ReleAssociado = "K04 - Relé Dreno Arla" },
                    new FusivelItemInfo { Numero = "F15", CapacidadeAmperes = 20, CorPadrao = "Amarelo (20A 24V)", CircuitoProtegido = "Ventilador da Caixa de Ar-Condicionado 24V", ReleAssociado = "K05 - Relé Ventilação" },
                    new FusivelItemInfo { Numero = "F23", CapacidadeAmperes = 25, CorPadrao = "Branco (25A 24V)", CircuitoProtegido = "Secador de Ar Eletrônico APU / Knorr", ReleAssociado = "Resistência de Aquecimento APU" }
                },
                Reles = new List<ReleItemInfo>
                {
                    new ReleItemInfo { Posicao = "K01", NomeFuncao = "Relé Principal Terminal 15 (+24V)", TipoPinos = "4 Pinos 24V (40A)", PinagemReferencia = "Comandado pelo comutador de ignição" },
                    new ReleItemInfo { Posicao = "K03", NomeFuncao = "Relé Auxiliar de Partida Linha 50 (24V)", TipoPinos = "4 Pinos 24V (70A Alta Potência)", PinagemReferencia = "Aciona o solenóide do motor de partida de 24V" }
                }
            };
            lista.Add(mbCentral);

            // 5. Hyundai HB20 / HB20S / HB20X 1.0 e 1.6 (Caixa Interna do Painel / BCM)
            var hb20Painel = new CentralEletricaFusivel
            {
                CodigoCentral = "CENTRAL-HYUNDAI-HB20-PAINEL",
                Titulo = "Central de Fusíveis Interna (Painel de Instrumentos / BCM) - Hyundai HB20 / HB20S / HB20X (2012 a 2019)",
                Montadora = "Hyundai",
                ModelosAplicacao = "HB20, HB20S, HB20X 1.0 12V 3C e 1.6 16V Flex (2012, 2013, 2014, 2015, 2016, 2017, 2018, 2019)",
                Localizacao = "Abaixo do painel, lado esquerdo do motorista (atrás da tampa de acesso)",
                TensaoNominal = "12V",
                Fusiveis = new List<FusivelItemInfo>
                {
                    new FusivelItemInfo { Numero = "F01", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Módulo de Controle do Motor (ECU) / Imobilizador", ReleAssociado = "Relé Principal da Injeção" },
                    new FusivelItemInfo { Numero = "F02", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Tomada de Força 12V Dianteira e Acendedor de Cigarros", ReleAssociado = "Relé Acessórios ACC" },
                    new FusivelItemInfo { Numero = "F03", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Bomba de Combustível / Eletrobomba Partida a Frio", ReleAssociado = "Relé da Bomba" },
                    new FusivelItemInfo { Numero = "F04", CapacidadeAmperes = 20, CorPadrao = "Amarelo (20A)", CircuitoProtegido = "Travamento Elétrico das Portas (Atuadores)", ReleAssociado = "Módulo BCM" },
                    new FusivelItemInfo { Numero = "F05", CapacidadeAmperes = 25, CorPadrao = "Branco (25A)", CircuitoProtegido = "Vidros Elétricos Dianteiros (Motorista e Passageiro)", ReleAssociado = "Relé de Vidros" },
                    new FusivelItemInfo { Numero = "F06", CapacidadeAmperes = 25, CorPadrao = "Branco (25A)", CircuitoProtegido = "Vidros Elétricos Traseiros", ReleAssociado = "Relé de Vidros" },
                    new FusivelItemInfo { Numero = "F07", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Luzes de Freio (Interruptor do Pedal e Brake Light)", ReleAssociado = "Linha 30 Direta" },
                    new FusivelItemInfo { Numero = "F08", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Painel de Instrumentos / Computador de Bordo", ReleAssociado = "Linha 15 Ignição" },
                    new FusivelItemInfo { Numero = "F09", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Sistema de Airbag SRS e Pré-Tensionadores", ReleAssociado = "Módulo Airbag" },
                    new FusivelItemInfo { Numero = "F10", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Limpador e Lavador do Para-brisa (Esguicho d'água)", ReleAssociado = "Relé Limpador" },
                    new FusivelItemInfo { Numero = "F11", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Luz de Marcha a Ré e Sensores de Estacionamento", ReleAssociado = "Linha 15" },
                    new FusivelItemInfo { Numero = "F12", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Central Multimídia / Rádio / Conector USB", ReleAssociado = "Linha ACC" },
                    new FusivelItemInfo { Numero = "F13", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Chave de Seta / Iluminação dos Instrumentos", ReleAssociado = "BCM" },
                    new FusivelItemInfo { Numero = "F14", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Desembaçador Térmico do Vidro Traseiro", ReleAssociado = "Relé Desembaçador" },
                    new FusivelItemInfo { Numero = "F15", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Comando do Ar-Condicionado e Ventilação Interna", ReleAssociado = "Linha 15" },
                    new FusivelItemInfo { Numero = "F16", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Lanternas Traseiras e Luz de Placa", ReleAssociado = "Linha 58 Iluminação" },
                    new FusivelItemInfo { Numero = "F17", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Retrovisores Elétricos Externos", ReleAssociado = "Linha ACC" },
                    new FusivelItemInfo { Numero = "F18", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Tomada Auxiliar 12V Traseira", ReleAssociado = "Relé ACC" }
                },
                Reles = new List<ReleItemInfo>
                {
                    new ReleItemInfo { Posicao = "RL01", NomeFuncao = "Relé de Acessórios (Linha ACC)", TipoPinos = "Micro Relé 4 Pinos (20A)", PinagemReferencia = "Desliga som e tomadas 12V durante partida" },
                    new ReleItemInfo { Posicao = "RL02", NomeFuncao = "Relé dos Vidros Elétricos", TipoPinos = "Micro Relé 4 Pinos (30A)", PinagemReferencia = "Alimentado por pós-chave Linha 15" },
                    new ReleItemInfo { Posicao = "RL03", NomeFuncao = "Relé do Desembaçador Traseiro", TipoPinos = "Micro Relé 4 Pinos (25A)", PinagemReferencia = "Temporizador gerenciado pelo BCM (15 min)" }
                }
            };
            lista.Add(hb20Painel);

            // 6. Hyundai HB20 / HB20S / HB20X (Caixa do Compartimento do Motor / Cofre)
            var hb20Cofre = new CentralEletricaFusivel
            {
                CodigoCentral = "CENTRAL-HYUNDAI-HB20-COFRE",
                Titulo = "Central de Fusíveis & Relés do Compartimento do Motor (Cofre) - Hyundai HB20 / HB20S / HB20X",
                Montadora = "Hyundai",
                ModelosAplicacao = "HB20, HB20S, HB20X 1.0 e 1.6 Flex (2012 a 2019)",
                Localizacao = "Vão do motor, lado direito próximo à bateria e amortecedor dianteiro",
                TensaoNominal = "12V",
                Fusiveis = new List<FusivelItemInfo>
                {
                    new FusivelItemInfo { Numero = "F_BATT", CapacidadeAmperes = 125, CorPadrao = "Maxi Fusível Preto (125A)", CircuitoProtegido = "Alimentação Geral do Chicote e Alternador", ReleAssociado = "Linha 30 Direta Bateria" },
                    new FusivelItemInfo { Numero = "F_EPS", CapacidadeAmperes = 80, CorPadrao = "Maxi Fusível Preto (80A)", CircuitoProtegido = "Direção Elétrica MDPS (Motor na Coluna)", ReleAssociado = "Módulo EPS Direto" },
                    new FusivelItemInfo { Numero = "F_ABS1", CapacidadeAmperes = 40, CorPadrao = "Maxi Laranja (40A)", CircuitoProtegido = "Motor da Bomba Hidráulica do ABS", ReleAssociado = "Central ABS" },
                    new FusivelItemInfo { Numero = "F_ABS2", CapacidadeAmperes = 30, CorPadrao = "Maxi Rosa (30A)", CircuitoProtegido = "Válvulas Solenoides do Freio ABS", ReleAssociado = "Central ABS" },
                    new FusivelItemInfo { Numero = "F_FAN_HI", CapacidadeAmperes = 40, CorPadrao = "Maxi Laranja (40A)", CircuitoProtegido = "Eletroventilador de Arrefecimento - Alta Velocidade", ReleAssociado = "R02 - Relé Ventoinha Alta" },
                    new FusivelItemInfo { Numero = "F_FAN_LOW", CapacidadeAmperes = 30, CorPadrao = "Maxi Rosa (30A)", CircuitoProtegido = "Eletroventilador de Arrefecimento - Baixa Velocidade", ReleAssociado = "R03 - Relé Ventoinha Baixa" },
                    new FusivelItemInfo { Numero = "F_IGN", CapacidadeAmperes = 30, CorPadrao = "Rosa (30A)", CircuitoProtegido = "Comutador de Ignição (Linhas 15 e 50 Partida)", ReleAssociado = "R04 - Relé de Partida" },
                    new FusivelItemInfo { Numero = "F_HORN", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Buzina (Alimentação do Contato 87)", ReleAssociado = "R01 - Relé da Buzina" },
                    new FusivelItemInfo { Numero = "F_LOW_L", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Farol Baixo Esquerdo", ReleAssociado = "Relé do Farol Baixo" },
                    new FusivelItemInfo { Numero = "F_LOW_R", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Farol Baixo Direito", ReleAssociado = "Relé do Farol Baixo" },
                    new FusivelItemInfo { Numero = "F_HI", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Farol Alto (Esquerdo e Direito)", ReleAssociado = "Relé do Farol Alto" },
                    new FusivelItemInfo { Numero = "F_AC", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Embreagem Eletromagnética do Compressor A/C", ReleAssociado = "R06 - Relé Compressor A/C" },
                    new FusivelItemInfo { Numero = "F_FOG", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Faróis de Neblina / Milha", ReleAssociado = "Relé Farol Neblina" },
                    new FusivelItemInfo { Numero = "F_ECU", CapacidadeAmperes = 20, CorPadrao = "Amarelo (20A)", CircuitoProtegido = "Relé Principal da Injeção Eletrônica e Bobinas", ReleAssociado = "R05 - Relé Principal ECU" }
                },
                Reles = new List<ReleItemInfo>
                {
                    new ReleItemInfo { Posicao = "R01", NomeFuncao = "Relé da Buzina", TipoPinos = "Micro Relé 4 Pinos (20A)", PinagemReferencia = "Comando negativo da cinta do volante. Saída 87 envia 12V para a buzina na grade dianteira" },
                    new ReleItemInfo { Posicao = "R02", NomeFuncao = "Relé do Eletroventilador (Alta Velocidade)", TipoPinos = "Mini Relé 4 Pinos (40A)", PinagemReferencia = "Acionado pela ECU aos 102°C ou pressão alta no A/C" },
                    new ReleItemInfo { Posicao = "R03", NomeFuncao = "Relé do Eletroventilador (Baixa Velocidade)", TipoPinos = "Mini Relé 4 Pinos (30A)", PinagemReferencia = "Acionado pela ECU aos 96°C através do resistor" },
                    new ReleItemInfo { Posicao = "R04", NomeFuncao = "Relé de Partida (Motor de Arranque)", TipoPinos = "Micro Relé 4 Pinos (30A)", PinagemReferencia = "Envia Linha 50 (+12V) para o automático de partida" },
                    new ReleItemInfo { Posicao = "R05", NomeFuncao = "Relé Principal do Motor (Main Relay ECU)", TipoPinos = "Mini Relé 4 Pinos (30A)", PinagemReferencia = "Alimenta bicos, bobinas, cânister e sensores" },
                    new ReleItemInfo { Posicao = "R06", NomeFuncao = "Relé do Compressor A/C", TipoPinos = "Micro Relé 4 Pinos (20A)", PinagemReferencia = "Atraca a embreagem magnética do compressor" }
                }
            };
            lista.Add(hb20Cofre);

            // 7. Fiat Palio / Siena / Strada / Uno 1.0 e 1.4 Fire / Fire EVO
            var fiatFire = new CentralEletricaFusivel
            {
                CodigoCentral = "CENTRAL-FIAT-FIRE-EVO",
                Titulo = "Central de Fusíveis do Vão do Motor & Bateria - Fiat Palio / Strada / Uno Fire / Fire EVO",
                Montadora = "Fiat",
                ModelosAplicacao = "Palio, Uno, Strada, Siena, Fiorino, Weekend 1.0 e 1.4 8V Fire e Fire EVO (2008 a 2021)",
                Localizacao = "Vão do motor, ao lado esquerdo da bateria e módulo IAW",
                TensaoNominal = "12V",
                Fusiveis = new List<FusivelItemInfo>
                {
                    new FusivelItemInfo { Numero = "F01", CapacidadeAmperes = 70, CorPadrao = "Maxi Preto (70A)", CircuitoProtegido = "Alimentação do Painel e Caixa de Fusíveis Interna", ReleAssociado = "Linha 30 Geral" },
                    new FusivelItemInfo { Numero = "F03", CapacidadeAmperes = 20, CorPadrao = "Amarelo (20A)", CircuitoProtegido = "Comutador de Ignição Linha 15", ReleAssociado = "Contato de Ignição" },
                    new FusivelItemInfo { Numero = "F10", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Buzina (Comando no volante)", ReleAssociado = "T03 - Relé Buzina" },
                    new FusivelItemInfo { Numero = "F16", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Compressor do Ar-Condicionado", ReleAssociado = "T05 - Relé Compressor A/C" },
                    new FusivelItemInfo { Numero = "F17", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Módulo de Injeção Eletrônica Marelli IAW 4AF / 7GF", ReleAssociado = "T09 - Relé Principal" },
                    new FusivelItemInfo { Numero = "F18", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Bomba de Combustível e Bicos Injetores", ReleAssociado = "T10 - Relé Bomba" },
                    new FusivelItemInfo { Numero = "F06", CapacidadeAmperes = 30, CorPadrao = "Verde (30A)", CircuitoProtegido = "Eletroventilador Baixa Velocidade", ReleAssociado = "T06 - Relé Ventoinha 1" },
                    new FusivelItemInfo { Numero = "F07", CapacidadeAmperes = 40, CorPadrao = "Laranja (40A)", CircuitoProtegido = "Eletroventilador Alta Velocidade", ReleAssociado = "T07 - Relé Ventoinha 2" }
                },
                Reles = new List<ReleItemInfo>
                {
                    new ReleItemInfo { Posicao = "T03", NomeFuncao = "Relé da Buzina", TipoPinos = "Micro Relé 4 Pinos (20A)", PinagemReferencia = "Comando massa vindo da chave de seta" },
                    new ReleItemInfo { Posicao = "T06", NomeFuncao = "Relé da 1ª Velocidade da Ventoinha", TipoPinos = "Mini Relé 4 Pinos (30A)", PinagemReferencia = "Acionado pela central com 97°C" },
                    new ReleItemInfo { Posicao = "T07", NomeFuncao = "Relé da 2ª Velocidade da Ventoinha", TipoPinos = "Mini Relé 4 Pinos (50A)", PinagemReferencia = "Acionado com 102°C ou A/C ligado" },
                    new ReleItemInfo { Posicao = "T09", NomeFuncao = "Relé Principal da Injeção", TipoPinos = "Mini Relé 4 Pinos (30A)", PinagemReferencia = "Alimenta bobinas e sensores da injeção" },
                    new ReleItemInfo { Posicao = "T10", NomeFuncao = "Relé da Eletrobomba de Combustível", TipoPinos = "Micro Relé 4 Pinos (20A)", PinagemReferencia = "Aterra pelo pino da ECU por 2s na chave e contínuo no giro" }
                }
            };
            lista.Add(fiatFire);

            // 8. Toyota Corolla 1.8 e 2.0 Dual VVT-i
            var corollaCentral = new CentralEletricaFusivel
            {
                CodigoCentral = "CENTRAL-TOYOTA-COROLLA",
                Titulo = "Central de Fusíveis do Cofre e Painel - Toyota Corolla 1.8 e 2.0 Dual VVT-i (2009 a 2019)",
                Montadora = "Toyota",
                ModelosAplicacao = "Corolla XEi, GLi, Altis 1.8 e 2.0 Flex (2009 a 2019)",
                Localizacao = "Cofre do Motor lado esquerdo e Painel interno abaixo da tampa porta-objetos",
                TensaoNominal = "12V",
                Fusiveis = new List<FusivelItemInfo>
                {
                    new FusivelItemInfo { Numero = "F_HORN", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Buzina (Horn Relay)", ReleAssociado = "Relé HORN integrado" },
                    new FusivelItemInfo { Numero = "F_EFI", CapacidadeAmperes = 20, CorPadrao = "Amarelo (20A)", CircuitoProtegido = "Injeção Eletrônica EFI e Bomba de Combustível", ReleAssociado = "Relé EFI MAIN" },
                    new FusivelItemInfo { Numero = "F_IGN", CapacidadeAmperes = 10, CorPadrao = "Vermelho (10A)", CircuitoProtegido = "Bobinas de Ignição Individuais COP", ReleAssociado = "Relé IG2" },
                    new FusivelItemInfo { Numero = "F_FAN", CapacidadeAmperes = 40, CorPadrao = "Laranja (40A)", CircuitoProtegido = "Eletroventilador de Arrefecimento PWM", ReleAssociado = "Módulo Ventilador" },
                    new FusivelItemInfo { Numero = "F_CIG", CapacidadeAmperes = 15, CorPadrao = "Azul (15A)", CircuitoProtegido = "Tomada de Acessórios 12V e Isqueiro", ReleAssociado = "Relé ACC" }
                },
                Reles = new List<ReleItemInfo>
                {
                    new ReleItemInfo { Posicao = "EFI MAIN", NomeFuncao = "Relé Principal da Injeção Toyota", TipoPinos = "4 Pinos Toyota Denso (30A)", PinagemReferencia = "Alimentação direta da ECU e bicos injetores" },
                    new ReleItemInfo { Posicao = "HORN", NomeFuncao = "Relé da Buzina", TipoPinos = "Micro Relé (15A)", PinagemReferencia = "Comando vindo da cinta espiral do volante (Clock Spring)" }
                }
            };
            lista.Add(corollaCentral);

            return lista;
        }

        private List<VeiculoPesado24VEspecificacao> ObterPesadosCurados()
        {
            return new List<VeiculoPesado24VEspecificacao>
            {
                new VeiculoPesado24VEspecificacao
                {
                    Montadora = "Scania",
                    Modelo = "Scania R440 Streamline DC13 Euro 5 (24V)",
                    TensaoSistema = "24V Nominal (28.4V Carregando em 1.200 RPM)",
                    AlternadorEspecificacao = "Bosch 28V 100A / 150A com regulador digital multifunção LIN",
                    BateriasEspecificacao = "2x Baterias 12V 180Ah a 225Ah ligadas em série (Total 24V). Balanceamento obrigatório: diferença máxima 0.25V entre as duas. Se houver desbalanceamento, a mais fraca entra em ebulição e a outra sulfata.",
                    ConsumoStandbyMaximo = "<= 45mA (0.045A) após 15 minutos do corte de repouso",
                    TorqueCabecote = "Motores DC13 com cabeçotes individuais: 1ª Etapa: 60 Nm | 2ª Etapa: 150 Nm | 3ª Etapa: +90° angular contínuo.",
                    FolgaValvulas = "Com motor totalmente frio: Admissão = 0.45 mm | Escape = 0.70 mm. Verificar folga da ponte de válvulas antes do ajuste final.",
                    ArCondicionadoGasGramas = "850g ± 25g de gás refrigerante R134a",
                    ArCondicionadoOleoTipo = "180 ml de óleo sintético PAG 46 com contraste UV",
                    DicasEletricasChassi = "Chicote do sensor de velocidade / tacógrafo passando pelo cardan tem proteção reforçada; falha no COO7 erro 5304 é comumente curto à massa por atrito na presilha do chassi. Nunca usar lâmpadas 12V ou puxar 12V de apenas uma bateria para rádio sem conversor 24V/12V!"
                },
                new VeiculoPesado24VEspecificacao
                {
                    Montadora = "Mercedes-Benz",
                    Modelo = "Mercedes-Benz Atego 2426 Euro 5 OM926 LA (24V)",
                    TensaoSistema = "24V Nominal (28.2V Carregando)",
                    AlternadorEspecificacao = "Bosch 28V 80A / 100A",
                    BateriasEspecificacao = "2x Baterias 12V 135Ah / 170Ah ligadas em série (Total 24V).",
                    ConsumoStandbyMaximo = "<= 40mA (0.040A) após desligamento da chave geral",
                    TorqueCabecote = "1ª Etapa: 60 Nm | 2ª Etapa: 120 Nm | 3ª Etapa: +90° angular | 4ª Etapa: +90° angular.",
                    FolgaValvulas = "Motor frio: Admissão = 0.40 mm | Escape = 0.60 mm. Regulagem com balancim do Top Brake aliviado.",
                    ArCondicionadoGasGramas = "750g ± 25g de gás refrigerante R134a",
                    ArCondicionadoOleoTipo = "160 ml de óleo PAG 46 para compressor Denso/Sanden",
                    DicasEletricasChassi = "Relé de partida auxiliar Linha 50 fica próximo ao motor de partida; se queimar o fusível F06 do PLD, o caminhão não dá partida e não comunica no painel. Atenção ao aterramento do módulo MR na carcaça do motor."
                },
                new VeiculoPesado24VEspecificacao
                {
                    Montadora = "Volvo",
                    Modelo = "Volvo FH 440 / FH 500 D13A Euro 5 (24V)",
                    TensaoSistema = "24V Nominal (28.5V Carregando)",
                    AlternadorEspecificacao = "Melco / Bosch 28V 110A / 120A",
                    BateriasEspecificacao = "2x Baterias 12V 220Ah ligadas em série no suporte traseiro do chassi.",
                    ConsumoStandbyMaximo = "<= 50mA (0.050A) com tacógrafo e alarme ativos",
                    TorqueCabecote = "Cabeçote único: 1ª Etapa: 100 Nm | 2ª Etapa: +90° | 3ª Etapa: +90° em espiral partindo do centro.",
                    FolgaValvulas = "Motor frio: Admissão = 0.20 mm | Escape = 0.80 mm. Freio motor VEB: calibrar folga do balancim do VEB com lâmina de 1.60 mm.",
                    ArCondicionadoGasGramas = "1100g ± 50g de gás refrigerante R134a",
                    ArCondicionadoOleoTipo = "240 ml de óleo sintético PAG 100 para compressor Zexel",
                    DicasEletricasChassi = "Curto-circuito no chicote dos faróis desliga o canal correspondente no módulo LCM por 3 tentativas. Se persistir, o canal é bloqueado permanentemente até apagar o DTC pelo scanner ou via menu do display do painel."
                }
            };
        }

        #endregion
    }
}
