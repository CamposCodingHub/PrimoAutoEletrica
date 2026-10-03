using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public interface ITransferenciaEstoqueService
    {
        Task<TransferenciaEstoque> SolicitarTransferenciaAsync(
            Guid filialOrigemId,
            string filialOrigemNome,
            Guid filialDestinoId,
            string filialDestinoNome,
            List<TransferenciaEstoqueItem> itens,
            string solicitante,
            string observacoes = "");

        Task<bool> DespacharTransferenciaAsync(Guid transferenciaId, string responsavelEnvio);
        Task<bool> ConfirmarRecebimentoAsync(Guid transferenciaId, string responsavelRecebimento, Dictionary<int, int>? quantidadesConferidas = null);
        Task<bool> CancelarTransferenciaAsync(Guid transferenciaId, string motivo);
        Task<List<TransferenciaEstoque>> ListarTransferenciasAsync(Guid? filialId = null, StatusTransferenciaEstoque? status = null);
        Task<TransferenciaEstoque?> ObterPorIdAsync(Guid id);
    }

    public sealed class TransferenciaEstoqueService : ITransferenciaEstoqueService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService? _logger;

        public TransferenciaEstoqueService(DatabaseService databaseService, LoggerService? logger = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger;
        }

        public async Task<TransferenciaEstoque> SolicitarTransferenciaAsync(
            Guid filialOrigemId,
            string filialOrigemNome,
            Guid filialDestinoId,
            string filialDestinoNome,
            List<TransferenciaEstoqueItem> itens,
            string solicitante,
            string observacoes = "")
        {
            if (filialOrigemId == filialDestinoId)
                throw new InvalidOperationException("A filial de origem e de destino não podem ser as mesmas.");

            if (itens == null || itens.Count == 0)
                throw new InvalidOperationException("A transferência deve conter ao menos um item.");

            return await Task.Run(() =>
            {
                var agora = DateTime.Now;
                var numero = $"TRF-{agora:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpperInvariant()}";
                var transferencia = new TransferenciaEstoque
                {
                    Id = Guid.NewGuid(),
                    NumeroTransferencia = numero,
                    FilialOrigemId = filialOrigemId,
                    FilialOrigemNome = filialOrigemNome,
                    FilialDestinoId = filialDestinoId,
                    FilialDestinoNome = filialDestinoNome,
                    Status = StatusTransferenciaEstoque.Solicitada,
                    DataSolicitacao = agora,
                    ResponsavelSolicitacao = solicitante,
                    Observacoes = observacoes,
                    Itens = itens,
                    ValorTotalEstimado = itens.Sum(i => i.ValorTotal)
                };

                using var connection = _databaseService.GetConnection();
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"
                            INSERT INTO TransferenciasEstoque
                            (
                                Id, NumeroTransferencia, FilialOrigemId, FilialOrigemNome,
                                FilialDestinoId, FilialDestinoNome, Status, DataSolicitacao,
                                ResponsavelSolicitacao, Observacoes, ValorTotalEstimado
                            )
                            VALUES
                            (
                                @Id, @Numero, @OrigemId, @OrigemNome,
                                @DestinoId, @DestinoNome, @Status, @DataSol,
                                @RespSol, @Obs, @Total
                            );";

                        cmd.Parameters.AddWithValue("@Id", transferencia.Id.ToString());
                        cmd.Parameters.AddWithValue("@Numero", transferencia.NumeroTransferencia);
                        cmd.Parameters.AddWithValue("@OrigemId", transferencia.FilialOrigemId.ToString());
                        cmd.Parameters.AddWithValue("@OrigemNome", transferencia.FilialOrigemNome);
                        cmd.Parameters.AddWithValue("@DestinoId", transferencia.FilialDestinoId.ToString());
                        cmd.Parameters.AddWithValue("@DestinoNome", transferencia.FilialDestinoNome);
                        cmd.Parameters.AddWithValue("@Status", (int)transferencia.Status);
                        cmd.Parameters.AddWithValue("@DataSol", transferencia.DataSolicitacao.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@RespSol", transferencia.ResponsavelSolicitacao);
                        cmd.Parameters.AddWithValue("@Obs", transferencia.Observacoes ?? string.Empty);
                        cmd.Parameters.AddWithValue("@Total", (double)transferencia.ValorTotalEstimado);

                        cmd.ExecuteNonQuery();
                    }

                    foreach (var item in itens)
                    {
                        item.TransferenciaId = transferencia.Id;
                        using var itemCmd = connection.CreateCommand();
                        itemCmd.Transaction = transaction;
                        itemCmd.CommandText = @"
                            INSERT INTO TransferenciasEstoqueItens
                            (
                                Id, TransferenciaId, ProdutoId, Codigo,
                                Descricao, QuantidadeEnviada, QuantidadeRecebida, ValorUnitario
                            )
                            VALUES
                            (
                                @Id, @TransfId, @ProdId, @Codigo,
                                @Descricao, @QtdEnv, @QtdRec, @ValorUnit
                            );";

                        itemCmd.Parameters.AddWithValue("@Id", item.Id.ToString());
                        itemCmd.Parameters.AddWithValue("@TransfId", item.TransferenciaId.ToString());
                        itemCmd.Parameters.AddWithValue("@ProdId", item.ProdutoId);
                        itemCmd.Parameters.AddWithValue("@Codigo", item.Codigo);
                        itemCmd.Parameters.AddWithValue("@Descricao", item.Descricao);
                        itemCmd.Parameters.AddWithValue("@QtdEnv", item.QuantidadeEnviada);
                        itemCmd.Parameters.AddWithValue("@QtdRec", 0);
                        itemCmd.Parameters.AddWithValue("@ValorUnit", (double)item.ValorUnitario);

                        itemCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    _logger?.LogInfo($"Transferência de estoque '{transferencia.NumeroTransferencia}' solicitada de '{filialOrigemNome}' para '{filialDestinoNome}'.");
                    return transferencia;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger?.LogError($"Falha ao solicitar transferência de estoque {numero}", ex);
                    throw;
                }
            });
        }

        public async Task<bool> DespacharTransferenciaAsync(Guid transferenciaId, string responsavelEnvio)
        {
            return await Task.Run(() =>
            {
                var transf = ObterPorIdAsync(transferenciaId).Result;
                if (transf == null || transf.Status != StatusTransferenciaEstoque.Solicitada)
                    return false;

                using var connection = _databaseService.GetConnection();
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var agora = DateTime.Now;

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"
                            UPDATE TransferenciasEstoque
                            SET Status = @Status, DataEnvio = @DataEnv, ResponsavelEnvio = @RespEnv
                            WHERE Id = @Id;";

                        cmd.Parameters.AddWithValue("@Status", (int)StatusTransferenciaEstoque.EmTransito);
                        cmd.Parameters.AddWithValue("@DataEnv", agora.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@RespEnv", responsavelEnvio);
                        cmd.Parameters.AddWithValue("@Id", transferenciaId.ToString());
                        cmd.ExecuteNonQuery();
                    }

                    // Deduz do estoque físico
                    foreach (var item in transf.Itens)
                    {
                        using var baixaCmd = connection.CreateCommand();
                        baixaCmd.Transaction = transaction;
                        baixaCmd.CommandText = "UPDATE Produtos SET Estoque = MAX(0, Estoque - @Qtd) WHERE Id = @Id;";
                        baixaCmd.Parameters.AddWithValue("@Qtd", item.QuantidadeEnviada);
                        baixaCmd.Parameters.AddWithValue("@Id", item.ProdutoId);
                        baixaCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    _logger?.LogInfo($"Transferência '{transf.NumeroTransferencia}' despachada em trânsito por '{responsavelEnvio}'.");
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger?.LogError($"Falha ao despachar transferência {transferenciaId}", ex);
                    return false;
                }
            });
        }

        public async Task<bool> ConfirmarRecebimentoAsync(Guid transferenciaId, string responsavelRecebimento, Dictionary<int, int>? quantidadesConferidas = null)
        {
            return await Task.Run(() =>
            {
                var transf = ObterPorIdAsync(transferenciaId).Result;
                if (transf == null || transf.Status != StatusTransferenciaEstoque.EmTransito)
                    return false;

                using var connection = _databaseService.GetConnection();
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var agora = DateTime.Now;

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"
                            UPDATE TransferenciasEstoque
                            SET Status = @Status, DataRecebimento = @DataRec, ResponsavelRecebimento = @RespRec
                            WHERE Id = @Id;";

                        cmd.Parameters.AddWithValue("@Status", (int)StatusTransferenciaEstoque.Recebida);
                        cmd.Parameters.AddWithValue("@DataRec", agora.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@RespRec", responsavelRecebimento);
                        cmd.Parameters.AddWithValue("@Id", transferenciaId.ToString());
                        cmd.ExecuteNonQuery();
                    }

                    // Atualiza itens e dá entrada no estoque
                    foreach (var item in transf.Itens)
                    {
                        int qtdRecebida = item.QuantidadeEnviada;
                        if (quantidadesConferidas != null && quantidadesConferidas.TryGetValue(item.ProdutoId, out var conf))
                            qtdRecebida = conf;

                        using var updItem = connection.CreateCommand();
                        updItem.Transaction = transaction;
                        updItem.CommandText = "UPDATE TransferenciasEstoqueItens SET QuantidadeRecebida = @Qtd WHERE Id = @Id;";
                        updItem.Parameters.AddWithValue("@Qtd", qtdRecebida);
                        updItem.Parameters.AddWithValue("@Id", item.Id.ToString());
                        updItem.ExecuteNonQuery();

                        using var entCmd = connection.CreateCommand();
                        entCmd.Transaction = transaction;
                        entCmd.CommandText = "UPDATE Produtos SET Estoque = Estoque + @Qtd WHERE Id = @Id;";
                        entCmd.Parameters.AddWithValue("@Qtd", qtdRecebida);
                        entCmd.Parameters.AddWithValue("@Id", item.ProdutoId);
                        entCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    _logger?.LogInfo($"Transferência '{transf.NumeroTransferencia}' recebida e conferida por '{responsavelRecebimento}'.");
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger?.LogError($"Falha ao confirmar recebimento da transferência {transferenciaId}", ex);
                    return false;
                }
            });
        }

        public async Task<bool> CancelarTransferenciaAsync(Guid transferenciaId, string motivo)
        {
            return await Task.Run(() =>
            {
                var transf = ObterPorIdAsync(transferenciaId).Result;
                if (transf == null || transf.Status == StatusTransferenciaEstoque.Recebida || transf.Status == StatusTransferenciaEstoque.Cancelada)
                    return false;

                using var connection = _databaseService.GetConnection();
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Se já estava em trânsito, estorna para o estoque de origem
                    if (transf.Status == StatusTransferenciaEstoque.EmTransito)
                    {
                        foreach (var item in transf.Itens)
                        {
                            using var estCmd = connection.CreateCommand();
                            estCmd.Transaction = transaction;
                            estCmd.CommandText = "UPDATE Produtos SET Estoque = Estoque + @Qtd WHERE Id = @Id;";
                            estCmd.Parameters.AddWithValue("@Qtd", item.QuantidadeEnviada);
                            estCmd.Parameters.AddWithValue("@Id", item.ProdutoId);
                            estCmd.ExecuteNonQuery();
                        }
                    }

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"
                            UPDATE TransferenciasEstoque
                            SET Status = @Status, Observacoes = Observacoes || ' [Cancelamento: ' || @Motivo || ']'
                            WHERE Id = @Id;";

                        cmd.Parameters.AddWithValue("@Status", (int)StatusTransferenciaEstoque.Cancelada);
                        cmd.Parameters.AddWithValue("@Motivo", motivo ?? "Cancelado pelo operador");
                        cmd.Parameters.AddWithValue("@Id", transferenciaId.ToString());
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    _logger?.LogInfo($"Transferência '{transf.NumeroTransferencia}' cancelada: {motivo}");
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger?.LogError($"Falha ao cancelar transferência {transferenciaId}", ex);
                    return false;
                }
            });
        }

        public async Task<List<TransferenciaEstoque>> ListarTransferenciasAsync(Guid? filialId = null, StatusTransferenciaEstoque? status = null)
        {
            return await Task.Run(() =>
            {
                var lista = new List<TransferenciaEstoque>();

                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                var sql = "SELECT * FROM TransferenciasEstoque WHERE 1=1";

                if (filialId.HasValue && filialId.Value != Guid.Empty)
                {
                    sql += " AND (FilialOrigemId = @FilialId OR FilialDestinoId = @FilialId)";
                    cmd.Parameters.AddWithValue("@FilialId", filialId.Value.ToString());
                }

                if (status.HasValue)
                {
                    sql += " AND Status = @Status";
                    cmd.Parameters.AddWithValue("@Status", (int)status.Value);
                }

                sql += " ORDER BY DataSolicitacao DESC;";
                cmd.CommandText = sql;

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(MapearTransferencia(reader));
                }

                return lista;
            });
        }

        public async Task<TransferenciaEstoque?> ObterPorIdAsync(Guid id)
        {
            return await Task.Run(() =>
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                TransferenciaEstoque? transf = null;
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM TransferenciasEstoque WHERE Id = @Id LIMIT 1;";
                    cmd.Parameters.AddWithValue("@Id", id.ToString());

                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        transf = MapearTransferencia(reader);
                    }
                }

                if (transf != null)
                {
                    using var itemCmd = connection.CreateCommand();
                    itemCmd.CommandText = "SELECT * FROM TransferenciasEstoqueItens WHERE TransferenciaId = @Id;";
                    itemCmd.Parameters.AddWithValue("@Id", id.ToString());

                    using var itemReader = itemCmd.ExecuteReader();
                    while (itemReader.Read())
                    {
                        transf.Itens.Add(new TransferenciaEstoqueItem
                        {
                            Id = Guid.TryParse(itemReader["Id"]?.ToString(), out var itemId) ? itemId : Guid.NewGuid(),
                            TransferenciaId = id,
                            ProdutoId = Convert.ToInt32(itemReader["ProdutoId"] ?? 0),
                            Codigo = itemReader["Codigo"]?.ToString() ?? string.Empty,
                            Descricao = itemReader["Descricao"]?.ToString() ?? string.Empty,
                            QuantidadeEnviada = Convert.ToInt32(itemReader["QuantidadeEnviada"] ?? 0),
                            QuantidadeRecebida = Convert.ToInt32(itemReader["QuantidadeRecebida"] ?? 0),
                            ValorUnitario = Convert.ToDecimal(itemReader["ValorUnitario"] ?? 0)
                        });
                    }
                }

                return transf;
            });
        }

        private static TransferenciaEstoque MapearTransferencia(DbDataReader reader)
        {
            return new TransferenciaEstoque
            {
                Id = Guid.TryParse(reader["Id"]?.ToString(), out var id) ? id : Guid.NewGuid(),
                NumeroTransferencia = reader["NumeroTransferencia"]?.ToString() ?? string.Empty,
                FilialOrigemId = Guid.TryParse(reader["FilialOrigemId"]?.ToString(), out var foId) ? foId : Guid.Empty,
                FilialOrigemNome = reader["FilialOrigemNome"]?.ToString() ?? string.Empty,
                FilialDestinoId = Guid.TryParse(reader["FilialDestinoId"]?.ToString(), out var fdId) ? fdId : Guid.Empty,
                FilialDestinoNome = reader["FilialDestinoNome"]?.ToString() ?? string.Empty,
                Status = Enum.TryParse<StatusTransferenciaEstoque>(reader["Status"]?.ToString(), out var st) ? st : StatusTransferenciaEstoque.Solicitada,
                DataSolicitacao = DateTime.TryParse(reader["DataSolicitacao"]?.ToString(), out var ds) ? ds : DateTime.Now,
                DataEnvio = DateTime.TryParse(reader["DataEnvio"]?.ToString(), out var de) ? de : null,
                DataRecebimento = DateTime.TryParse(reader["DataRecebimento"]?.ToString(), out var dr) ? dr : null,
                ResponsavelSolicitacao = reader["ResponsavelSolicitacao"]?.ToString() ?? string.Empty,
                ResponsavelEnvio = reader["ResponsavelEnvio"]?.ToString(),
                ResponsavelRecebimento = reader["ResponsavelRecebimento"]?.ToString(),
                Observacoes = reader["Observacoes"]?.ToString() ?? string.Empty,
                ValorTotalEstimado = Convert.ToDecimal(reader["ValorTotalEstimado"] ?? 0)
            };
        }
    }
}
