using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public sealed class FerramentaService : IFerramentaService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService? _logger;

        public FerramentaService(DatabaseService databaseService, LoggerService? logger = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger;
        }

        public async Task<List<Ferramenta>> ListarFerramentasAsync(
            string? termoBusca = null,
            CategoriaFerramenta? categoria = null,
            StatusFerramenta? status = null)
        {
            return await Task.Run(() =>
            {
                var lista = new List<Ferramenta>();

                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                var sql = "SELECT * FROM Ferramentas WHERE Ativo = 1";

                if (!string.IsNullOrWhiteSpace(termoBusca))
                {
                    sql += " AND (CodigoPatrimonio LIKE @Termo OR Nome LIKE @Termo OR MarcaModelo LIKE @Termo OR NumeroSerie LIKE @Termo OR LocalizacaoArmario LIKE @Termo)";
                    cmd.Parameters.AddWithValue("@Termo", $"%{termoBusca.Trim()}%");
                }

                if (categoria.HasValue)
                {
                    sql += " AND Categoria = @Categoria";
                    cmd.Parameters.AddWithValue("@Categoria", (int)categoria.Value);
                }

                if (status.HasValue)
                {
                    sql += " AND Status = @Status";
                    cmd.Parameters.AddWithValue("@Status", (int)status.Value);
                }

                sql += " ORDER BY CASE Status WHEN 2 THEN 1 WHEN 1 THEN 2 WHEN 4 THEN 3 ELSE 4 END, Nome ASC;";
                cmd.CommandText = sql;

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(MapearFerramenta(reader));
                }

                return lista;
            });
        }

        public async Task<Ferramenta?> ObterPorIdAsync(Guid id)
        {
            return await Task.Run(() =>
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM Ferramentas WHERE Id = @Id LIMIT 1;";
                cmd.Parameters.AddWithValue("@Id", id.ToString());

                using var reader = cmd.ExecuteReader();
                return reader.Read() ? MapearFerramenta(reader) : null;
            });
        }

        public async Task<Ferramenta?> ObterPorCodigoOuSerieAsync(string codigoOuSerie)
        {
            if (string.IsNullOrWhiteSpace(codigoOuSerie)) return null;

            return await Task.Run(() =>
            {
                var termo = codigoOuSerie.Trim();

                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT * FROM Ferramentas 
                    WHERE Ativo = 1 AND (trim(upper(CodigoPatrimonio)) = upper(@Termo) OR trim(upper(NumeroSerie)) = upper(@Termo))
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@Termo", termo);

                using var reader = cmd.ExecuteReader();
                return reader.Read() ? MapearFerramenta(reader) : null;
            });
        }

        public async Task<ResumoFerramentaria> ObterResumoAsync()
        {
            var todas = await ListarFerramentasAsync();
            return new ResumoFerramentaria
            {
                TotalFerramentas = todas.Count,
                Disponiveis = todas.Count(f => f.Status == StatusFerramenta.Disponivel),
                EmUso = todas.Count(f => f.Status == StatusFerramenta.EmUso),
                EmManutencao = todas.Count(f => f.Status == StatusFerramenta.EmManutencao),
                Avariadas = todas.Count(f => f.Status == StatusFerramenta.Avariada || f.Status == StatusFerramenta.Extraviada),
                CalibracaoVencendo = todas.Count(f => f.CalibracaoVencida)
            };
        }

        public async Task SalvarFerramentaAsync(Ferramenta ferramenta)
        {
            if (ferramenta == null) throw new ArgumentNullException(nameof(ferramenta));

            await Task.Run(() =>
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO Ferramentas
                    (
                        Id, CodigoPatrimonio, Nome, Categoria, MarcaModelo,
                        NumeroSerie, LocalizacaoArmario, Status, ValorAquisicao,
                        DataAquisicao, RequerCalibracaoPeriodica, IntervaloCalibracaoDias,
                        UltimaCalibracao, ProximaCalibracao, FuncionarioPosseAtualId,
                        FuncionarioPosseAtualNome, OrdemServicoAtualId, NumeroOSAtual,
                        DataHoraRetiradaAtual, Observacoes, Ativo
                    )
                    VALUES
                    (
                        @Id, @CodigoPatrimonio, @Nome, @Categoria, @MarcaModelo,
                        @NumeroSerie, @LocalizacaoArmario, @Status, @ValorAquisicao,
                        @DataAquisicao, @RequerCalibracaoPeriodica, @IntervaloCalibracaoDias,
                        @UltimaCalibracao, @ProximaCalibracao, @FuncionarioPosseAtualId,
                        @FuncionarioPosseAtualNome, @OrdemServicoAtualId, @NumeroOSAtual,
                        @DataHoraRetiradaAtual, @Observacoes, @Ativo
                    );";

                cmd.Parameters.AddWithValue("@Id", ferramenta.Id.ToString());
                cmd.Parameters.AddWithValue("@CodigoPatrimonio", ferramenta.CodigoPatrimonio ?? string.Empty);
                cmd.Parameters.AddWithValue("@Nome", ferramenta.Nome ?? string.Empty);
                cmd.Parameters.AddWithValue("@Categoria", (int)ferramenta.Categoria);
                cmd.Parameters.AddWithValue("@MarcaModelo", ferramenta.MarcaModelo ?? string.Empty);
                cmd.Parameters.AddWithValue("@NumeroSerie", ferramenta.NumeroSerie ?? string.Empty);
                cmd.Parameters.AddWithValue("@LocalizacaoArmario", ferramenta.LocalizacaoArmario ?? string.Empty);
                cmd.Parameters.AddWithValue("@Status", (int)ferramenta.Status);
                cmd.Parameters.AddWithValue("@ValorAquisicao", (double)ferramenta.ValorAquisicao);
                cmd.Parameters.AddWithValue("@DataAquisicao", ferramenta.DataAquisicao.ToString("o", CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("@RequerCalibracaoPeriodica", ferramenta.RequerCalibracaoPeriodica ? 1 : 0);
                cmd.Parameters.AddWithValue("@IntervaloCalibracaoDias", ferramenta.IntervaloCalibracaoDias);
                cmd.Parameters.AddWithValue("@UltimaCalibracao", (object?)ferramenta.UltimaCalibracao?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProximaCalibracao", (object?)ferramenta.ProximaCalibracao?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FuncionarioPosseAtualId", (object?)ferramenta.FuncionarioPosseAtualId?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FuncionarioPosseAtualNome", (object?)ferramenta.FuncionarioPosseAtualNome ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OrdemServicoAtualId", (object?)ferramenta.OrdemServicoAtualId?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NumeroOSAtual", (object?)ferramenta.NumeroOSAtual ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DataHoraRetiradaAtual", (object?)ferramenta.DataHoraRetiradaAtual?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Observacoes", ferramenta.Observacoes ?? string.Empty);
                cmd.Parameters.AddWithValue("@Ativo", ferramenta.Ativo ? 1 : 0);

                cmd.ExecuteNonQuery();
                _logger?.LogInfo($"Ferramenta '{ferramenta.CodigoPatrimonio} - {ferramenta.Nome}' gravada com sucesso.");
            });
        }

        public async Task ExcluirFerramentaAsync(Guid id)
        {
            await Task.Run(() =>
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE Ferramentas SET Ativo = 0 WHERE Id = @Id;";
                cmd.Parameters.AddWithValue("@Id", id.ToString());
                cmd.ExecuteNonQuery();
            });
        }

        public async Task<MovimentacaoFerramenta> RegistrarRetiradaAsync(
            Guid ferramentaId,
            Guid funcionarioId,
            string funcionarioNome,
            Guid? ordemServicoId,
            string? numeroOS,
            DateTime? previsaoDevolucao,
            string estadoConservacao = "OK",
            string registradoPor = "Sistema")
        {
            return await Task.Run(() =>
            {
                var ferramenta = ObterPorIdAsync(ferramentaId).Result;
                if (ferramenta == null)
                {
                    throw new InvalidOperationException($"Ferramenta ID '{ferramentaId}' não encontrada.");
                }

                if (ferramenta.Status == StatusFerramenta.EmUso)
                {
                    throw new InvalidOperationException(
                        $"A ferramenta '{ferramenta.Nome}' já consta em uso com o técnico '{ferramenta.FuncionarioPosseAtualNome}'. Registre a devolução anterior primeiro.");
                }

                using var connection = _databaseService.GetConnection();
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var agora = DateTime.Now;

                    // 1. Atualizar ferramenta
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"
                            UPDATE Ferramentas
                            SET Status = @Status,
                                FuncionarioPosseAtualId = @FuncId,
                                FuncionarioPosseAtualNome = @FuncNome,
                                OrdemServicoAtualId = @OSId,
                                NumeroOSAtual = @OSNumero,
                                DataHoraRetiradaAtual = @Agora
                            WHERE Id = @Id;";

                        cmd.Parameters.AddWithValue("@Status", (int)StatusFerramenta.EmUso);
                        cmd.Parameters.AddWithValue("@FuncId", funcionarioId.ToString());
                        cmd.Parameters.AddWithValue("@FuncNome", funcionarioNome ?? string.Empty);
                        cmd.Parameters.AddWithValue("@OSId", (object?)ordemServicoId?.ToString() ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@OSNumero", (object?)numeroOS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Agora", agora.ToString("o", CultureInfo.InvariantCulture));
                        cmd.Parameters.AddWithValue("@Id", ferramentaId.ToString());

                        cmd.ExecuteNonQuery();
                    }

                    // 2. Criar registro de movimentacao
                    var movimentacao = new MovimentacaoFerramenta
                    {
                        Id = Guid.NewGuid(),
                        FerramentaId = ferramentaId,
                        CodigoPatrimonio = ferramenta.CodigoPatrimonio,
                        FerramentaNome = ferramenta.Nome,
                        FuncionarioId = funcionarioId,
                        FuncionarioNome = funcionarioNome,
                        OrdemServicoId = ordemServicoId,
                        NumeroOS = numeroOS,
                        DataRetirada = agora,
                        PrevisaoDevolucao = previsaoDevolucao ?? agora.AddHours(4),
                        EstadoConservacaoRetirada = estadoConservacao ?? "OK",
                        RegistradoPor = registradoPor ?? "Sistema"
                    };

                    using (var movCmd = connection.CreateCommand())
                    {
                        movCmd.Transaction = transaction;
                        movCmd.CommandText = @"
                            INSERT INTO MovimentacoesFerramentas
                            (
                                Id, FerramentaId, CodigoPatrimonio, FerramentaNome,
                                FuncionarioId, FuncionarioNome, OrdemServicoId, NumeroOS,
                                DataRetirada, PrevisaoDevolucao, EstadoConservacaoRetirada,
                                RegistradoPor
                            )
                            VALUES
                            (
                                @Id, @FerramentaId, @CodigoPatrimonio, @FerramentaNome,
                                @FuncionarioId, @FuncionarioNome, @OrdemServicoId, @NumeroOS,
                                @DataRetirada, @PrevisaoDevolucao, @EstadoConservacaoRetirada,
                                @RegistradoPor
                            );";

                        movCmd.Parameters.AddWithValue("@Id", movimentacao.Id.ToString());
                        movCmd.Parameters.AddWithValue("@FerramentaId", movimentacao.FerramentaId.ToString());
                        movCmd.Parameters.AddWithValue("@CodigoPatrimonio", movimentacao.CodigoPatrimonio);
                        movCmd.Parameters.AddWithValue("@FerramentaNome", movimentacao.FerramentaNome);
                        movCmd.Parameters.AddWithValue("@FuncionarioId", movimentacao.FuncionarioId.ToString());
                        movCmd.Parameters.AddWithValue("@FuncionarioNome", movimentacao.FuncionarioNome);
                        movCmd.Parameters.AddWithValue("@OrdemServicoId", (object?)movimentacao.OrdemServicoId?.ToString() ?? DBNull.Value);
                        movCmd.Parameters.AddWithValue("@NumeroOS", (object?)movimentacao.NumeroOS ?? DBNull.Value);
                        movCmd.Parameters.AddWithValue("@DataRetirada", movimentacao.DataRetirada.ToString("o", CultureInfo.InvariantCulture));
                        movCmd.Parameters.AddWithValue("@PrevisaoDevolucao", (object?)movimentacao.PrevisaoDevolucao?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                        movCmd.Parameters.AddWithValue("@EstadoConservacaoRetirada", movimentacao.EstadoConservacaoRetirada);
                        movCmd.Parameters.AddWithValue("@RegistradoPor", movimentacao.RegistradoPor);

                        movCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    _logger?.LogInfo($"Retirada da ferramenta '{ferramenta.CodigoPatrimonio}' para técnico '{funcionarioNome}' registrada com sucesso.");
                    return movimentacao;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger?.LogError($"Falha ao registrar retirada da ferramenta '{ferramentaId}'.", ex);
                    throw;
                }
            });
        }

        public async Task<MovimentacaoFerramenta> RegistrarDevolucaoAsync(
            Guid ferramentaId,
            string estadoConservacaoDevolucao = "OK",
            string? observacoesDevolucao = null,
            string registradoPor = "Sistema")
        {
            return await Task.Run(() =>
            {
                var ferramenta = ObterPorIdAsync(ferramentaId).Result;
                if (ferramenta == null)
                {
                    throw new InvalidOperationException($"Ferramenta ID '{ferramentaId}' não encontrada.");
                }

                using var connection = _databaseService.GetConnection();
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var agora = DateTime.Now;
                    bool avariada = !string.IsNullOrWhiteSpace(estadoConservacaoDevolucao) &&
                                    (estadoConservacaoDevolucao.Contains("Avariad", StringComparison.OrdinalIgnoreCase) ||
                                     estadoConservacaoDevolucao.Contains("Defeit", StringComparison.OrdinalIgnoreCase) ||
                                     estadoConservacaoDevolucao.Contains("Quebrad", StringComparison.OrdinalIgnoreCase));

                    var novoStatus = avariada ? StatusFerramenta.Avariada : StatusFerramenta.Disponivel;

                    // 1. Atualizar ferramenta
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"
                            UPDATE Ferramentas
                            SET Status = @Status,
                                FuncionarioPosseAtualId = NULL,
                                FuncionarioPosseAtualNome = NULL,
                                OrdemServicoAtualId = NULL,
                                NumeroOSAtual = NULL,
                                DataHoraRetiradaAtual = NULL
                            WHERE Id = @Id;";

                        cmd.Parameters.AddWithValue("@Status", (int)novoStatus);
                        cmd.Parameters.AddWithValue("@Id", ferramentaId.ToString());
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Fechar movimentacao aberta
                    MovimentacaoFerramenta? movimentacao = null;
                    using (var selCmd = connection.CreateCommand())
                    {
                        selCmd.Transaction = transaction;
                        selCmd.CommandText = @"
                            SELECT * FROM MovimentacoesFerramentas
                            WHERE FerramentaId = @FerramentaId AND DataDevolucao IS NULL
                            ORDER BY DataRetirada DESC LIMIT 1;";
                        selCmd.Parameters.AddWithValue("@FerramentaId", ferramentaId.ToString());

                        using var reader = selCmd.ExecuteReader();
                        if (reader.Read())
                        {
                            movimentacao = MapearMovimentacao(reader);
                        }
                    }

                    if (movimentacao != null)
                    {
                        using var updCmd = connection.CreateCommand();
                        updCmd.Transaction = transaction;
                        updCmd.CommandText = @"
                            UPDATE MovimentacoesFerramentas
                            SET DataDevolucao = @DataDev,
                                EstadoConservacaoDevolucao = @EstadoDev,
                                ObservacaoDevolucao = @ObsDev
                            WHERE Id = @Id;";

                        updCmd.Parameters.AddWithValue("@DataDev", agora.ToString("o", CultureInfo.InvariantCulture));
                        updCmd.Parameters.AddWithValue("@EstadoDev", estadoConservacaoDevolucao ?? "OK");
                        updCmd.Parameters.AddWithValue("@ObsDev", (object?)observacoesDevolucao ?? DBNull.Value);
                        updCmd.Parameters.AddWithValue("@Id", movimentacao.Id.ToString());
                        updCmd.ExecuteNonQuery();

                        movimentacao.DataDevolucao = agora;
                        movimentacao.EstadoConservacaoDevolucao = estadoConservacaoDevolucao;
                        movimentacao.ObservacaoDevolucao = observacoesDevolucao;
                    }
                    else
                    {
                        movimentacao = new MovimentacaoFerramenta
                        {
                            Id = Guid.NewGuid(),
                            FerramentaId = ferramentaId,
                            CodigoPatrimonio = ferramenta.CodigoPatrimonio,
                            FerramentaNome = ferramenta.Nome,
                            DataDevolucao = agora,
                            EstadoConservacaoDevolucao = estadoConservacaoDevolucao,
                            ObservacaoDevolucao = observacoesDevolucao,
                            RegistradoPor = registradoPor
                        };
                    }

                    transaction.Commit();
                    _logger?.LogInfo($"Devolução da ferramenta '{ferramenta.CodigoPatrimonio}' registrada com sucesso.");
                    return movimentacao;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger?.LogError($"Falha ao registrar devolução da ferramenta '{ferramentaId}'.", ex);
                    throw;
                }
            });
        }

        public async Task<List<Ferramenta>> ObterFerramentasPendentesOSAsync(Guid ordemServicoId)
        {
            if (ordemServicoId == Guid.Empty) return new List<Ferramenta>();

            return await Task.Run(() =>
            {
                var lista = new List<Ferramenta>();

                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT * FROM Ferramentas
                    WHERE OrdemServicoAtualId = @OSId AND Status = @Status AND Ativo = 1;";
                cmd.Parameters.AddWithValue("@OSId", ordemServicoId.ToString());
                cmd.Parameters.AddWithValue("@Status", (int)StatusFerramenta.EmUso);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(MapearFerramenta(reader));
                }

                return lista;
            });
        }

        public async Task<List<MovimentacaoFerramenta>> ObterHistoricoMovimentacoesAsync(Guid? ferramentaId = null, int limite = 50)
        {
            return await Task.Run(() =>
            {
                var lista = new List<MovimentacaoFerramenta>();

                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                var sql = "SELECT * FROM MovimentacoesFerramentas WHERE 1=1";

                if (ferramentaId.HasValue && ferramentaId.Value != Guid.Empty)
                {
                    sql += " AND FerramentaId = @FerramentaId";
                    cmd.Parameters.AddWithValue("@FerramentaId", ferramentaId.Value.ToString());
                }

                sql += $" ORDER BY DataRetirada DESC LIMIT {Math.Max(1, limite)};";
                cmd.CommandText = sql;

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(MapearMovimentacao(reader));
                }

                return lista;
            });
        }

        public async Task<List<Ferramenta>> ObterFerramentasEmAtrasoDevolucaoAsync()
        {
            return await Task.Run(() =>
            {
                var lista = new List<Ferramenta>();
                var agora = DateTime.Now.ToString("o", CultureInfo.InvariantCulture);

                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT f.* FROM Ferramentas f
                    INNER JOIN MovimentacoesFerramentas m ON f.Id = m.FerramentaId
                    WHERE f.Status = 2 AND m.DataDevolucao IS NULL AND m.PrevisaoDevolucao IS NOT NULL AND m.PrevisaoDevolucao < @Agora
                    GROUP BY f.Id;";
                cmd.Parameters.AddWithValue("@Agora", agora);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(MapearFerramenta(reader));
                }

                return lista;
            });
        }

        public async Task RegistrarCalibracaoAsync(Guid ferramentaId, DateTime dataCalibracao, int proximoIntervaloDias = 365, string? observacoes = null)
        {
            await Task.Run(() =>
            {
                var proxima = dataCalibracao.AddDays(proximoIntervaloDias > 0 ? proximoIntervaloDias : 365);

                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Ferramentas
                    SET UltimaCalibracao = @Ultima,
                        ProximaCalibracao = @Proxima,
                        IntervaloCalibracaoDias = @Intervalo,
                        Status = CASE WHEN Status = 3 THEN 1 ELSE Status END
                    WHERE Id = @Id;";

                cmd.Parameters.AddWithValue("@Ultima", dataCalibracao.ToString("o", CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("@Proxima", proxima.ToString("o", CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("@Intervalo", proximoIntervaloDias);
                cmd.Parameters.AddWithValue("@Id", ferramentaId.ToString());

                cmd.ExecuteNonQuery();
                _logger?.LogInfo($"Calibração registrada para ferramenta ID '{ferramentaId}'. Próxima: {proxima:dd/MM/yyyy}.");
            });
        }

        private static Ferramenta MapearFerramenta(DbDataReader reader)
        {
            var f = new Ferramenta
            {
                Id = Guid.TryParse(reader["Id"]?.ToString(), out var id) ? id : Guid.NewGuid(),
                CodigoPatrimonio = reader["CodigoPatrimonio"]?.ToString() ?? string.Empty,
                Nome = reader["Nome"]?.ToString() ?? string.Empty,
                Categoria = Enum.TryParse<CategoriaFerramenta>(reader["Categoria"]?.ToString(), out var cat) ? cat : CategoriaFerramenta.DiagnosticoEletronico,
                MarcaModelo = reader["MarcaModelo"]?.ToString() ?? string.Empty,
                NumeroSerie = reader["NumeroSerie"]?.ToString() ?? string.Empty,
                LocalizacaoArmario = reader["LocalizacaoArmario"]?.ToString() ?? string.Empty,
                Status = Enum.TryParse<StatusFerramenta>(reader["Status"]?.ToString(), out var st) ? st : StatusFerramenta.Disponivel,
                ValorAquisicao = Convert.ToDecimal(reader["ValorAquisicao"] ?? 0),
                RequerCalibracaoPeriodica = Convert.ToInt32(reader["RequerCalibracaoPeriodica"] ?? 0) == 1,
                IntervaloCalibracaoDias = Convert.ToInt32(reader["IntervaloCalibracaoDias"] ?? 365),
                FuncionarioPosseAtualNome = reader["FuncionarioPosseAtualNome"]?.ToString(),
                NumeroOSAtual = reader["NumeroOSAtual"]?.ToString(),
                Observacoes = reader["Observacoes"]?.ToString() ?? string.Empty,
                Ativo = Convert.ToInt32(reader["Ativo"] ?? 1) == 1
            };

            if (Guid.TryParse(reader["FuncionarioPosseAtualId"]?.ToString(), out var funcId))
                f.FuncionarioPosseAtualId = funcId;
            if (Guid.TryParse(reader["OrdemServicoAtualId"]?.ToString(), out var osId))
                f.OrdemServicoAtualId = osId;

            if (DateTime.TryParse(reader["DataAquisicao"]?.ToString(), out var da))
                f.DataAquisicao = da;
            if (DateTime.TryParse(reader["UltimaCalibracao"]?.ToString(), out var uc))
                f.UltimaCalibracao = uc;
            if (DateTime.TryParse(reader["ProximaCalibracao"]?.ToString(), out var pc))
                f.ProximaCalibracao = pc;
            if (DateTime.TryParse(reader["DataHoraRetiradaAtual"]?.ToString(), out var dr))
                f.DataHoraRetiradaAtual = dr;

            return f;
        }

        private static MovimentacaoFerramenta MapearMovimentacao(DbDataReader reader)
        {
            var m = new MovimentacaoFerramenta
            {
                Id = Guid.TryParse(reader["Id"]?.ToString(), out var id) ? id : Guid.NewGuid(),
                FerramentaId = Guid.TryParse(reader["FerramentaId"]?.ToString(), out var fId) ? fId : Guid.Empty,
                CodigoPatrimonio = reader["CodigoPatrimonio"]?.ToString() ?? string.Empty,
                FerramentaNome = reader["FerramentaNome"]?.ToString() ?? string.Empty,
                FuncionarioId = Guid.TryParse(reader["FuncionarioId"]?.ToString(), out var funcId) ? funcId : Guid.Empty,
                FuncionarioNome = reader["FuncionarioNome"]?.ToString() ?? string.Empty,
                NumeroOS = reader["NumeroOS"]?.ToString(),
                EstadoConservacaoRetirada = reader["EstadoConservacaoRetirada"]?.ToString() ?? "OK",
                EstadoConservacaoDevolucao = reader["EstadoConservacaoDevolucao"]?.ToString(),
                ObservacaoDevolucao = reader["ObservacaoDevolucao"]?.ToString(),
                RegistradoPor = reader["RegistradoPor"]?.ToString() ?? "Sistema"
            };

            if (Guid.TryParse(reader["OrdemServicoId"]?.ToString(), out var osId))
                m.OrdemServicoId = osId;

            if (DateTime.TryParse(reader["DataRetirada"]?.ToString(), out var dr))
                m.DataRetirada = dr;
            if (DateTime.TryParse(reader["PrevisaoDevolucao"]?.ToString(), out var pd))
                m.PrevisaoDevolucao = pd;
            if (DateTime.TryParse(reader["DataDevolucao"]?.ToString(), out var dd))
                m.DataDevolucao = dd;

            return m;
        }
    }
}
