using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Repositories
{
    public sealed class PurchaseRepository : IPurchaseRepository
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly LoggerService _logger;

        public PurchaseRepository(Func<DbConnection> connectionFactory, LoggerService logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static DbParameter CreateParameter(DbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            return parameter;
        }

        public async Task<IReadOnlyList<PurchaseRequest>> ObterTodosAsync(string? busca = null, PurchasePriority? prioridade = null, PurchaseRequestStatus? status = null, int? solicitanteId = null, CancellationToken ct = default)
        {
            var list = new List<PurchaseRequest>();
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                var sql = @"
                    SELECT PurchaseRequestId, Number, RequestedByUserId, RequestedByUserName,
                           RequestedAt, Priority, Reason, Status, ApprovedByUserId, ApprovedByUserName,
                           ApprovedAt, CancelledByUserId, CancelledAt, CancellationReason, SupplierId,
                           SupplierName, TotalEstimatedCostCents, TotalActualCostCents, FiscalDocumentNumber,
                           Notes, RowVersion, CreatedAt, UpdatedAt
                    FROM PurchaseRequests
                    WHERE 1=1
                ";

                if (!string.IsNullOrWhiteSpace(busca))
                {
                    sql += " AND (Number LIKE @Busca OR SupplierName LIKE @Busca OR Notes LIKE @Busca)";
                    command.Parameters.Add(CreateParameter(command, "@Busca", $"%{busca.Trim()}%"));
                }

                if (prioridade.HasValue)
                {
                    sql += " AND Priority = @Priority";
                    command.Parameters.Add(CreateParameter(command, "@Priority", prioridade.Value.ToString()));
                }

                if (status.HasValue)
                {
                    sql += " AND Status = @Status";
                    command.Parameters.Add(CreateParameter(command, "@Status", status.Value.ToString()));
                }

                if (solicitanteId.HasValue)
                {
                    sql += " AND RequestedByUserId = @SolId";
                    command.Parameters.Add(CreateParameter(command, "@SolId", solicitanteId.Value));
                }

                sql += " ORDER BY RequestedAt DESC;";
                command.CommandText = sql;

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    list.Add(MapRequest(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao listar requisições de compra.", ex);
                throw;
            }

            return list;
        }

        public async Task<PurchaseRequest?> ObterPorIdAsync(Guid requestId, CancellationToken ct = default)
        {
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT PurchaseRequestId, Number, RequestedByUserId, RequestedByUserName,
                           RequestedAt, Priority, Reason, Status, ApprovedByUserId, ApprovedByUserName,
                           ApprovedAt, CancelledByUserId, CancelledAt, CancellationReason, SupplierId,
                           SupplierName, TotalEstimatedCostCents, TotalActualCostCents, FiscalDocumentNumber,
                           Notes, RowVersion, CreatedAt, UpdatedAt
                    FROM PurchaseRequests
                    WHERE PurchaseRequestId = @Id LIMIT 1;
                ";
                command.Parameters.Add(CreateParameter(command, "@Id", requestId.ToString()));

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    var req = MapRequest(reader);
                    req.Items = (List<PurchaseRequestItem>)await ObterItensAsync(requestId, ct).ConfigureAwait(false);
                    return req;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao obter requisição por ID {requestId}", ex);
                throw;
            }

            return null;
        }

        public async Task<PurchaseRequest?> ObterPorNumeroAsync(string numero, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(numero)) return null;

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT PurchaseRequestId, Number, RequestedByUserId, RequestedByUserName,
                           RequestedAt, Priority, Reason, Status, ApprovedByUserId, ApprovedByUserName,
                           ApprovedAt, CancelledByUserId, CancelledAt, CancellationReason, SupplierId,
                           SupplierName, TotalEstimatedCostCents, TotalActualCostCents, FiscalDocumentNumber,
                           Notes, RowVersion, CreatedAt, UpdatedAt
                    FROM PurchaseRequests
                    WHERE lower(Number) = lower(@Number) LIMIT 1;
                ";
                command.Parameters.Add(CreateParameter(command, "@Number", numero.Trim()));

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    var req = MapRequest(reader);
                    req.Items = (List<PurchaseRequestItem>)await ObterItensAsync(req.PurchaseRequestId, ct).ConfigureAwait(false);
                    return req;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao obter requisição por Número {numero}", ex);
                throw;
            }

            return null;
        }

        public async Task<bool> InserirAsync(PurchaseRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);
                await using var tx = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.Transaction = tx;
                command.CommandText = @"
                    INSERT INTO PurchaseRequests (
                        PurchaseRequestId, Number, RequestedByUserId, RequestedByUserName,
                        RequestedAt, Priority, Reason, Status, ApprovedByUserId, ApprovedByUserName,
                        ApprovedAt, CancelledByUserId, CancelledAt, CancellationReason, SupplierId,
                        SupplierName, TotalEstimatedCostCents, TotalActualCostCents, FiscalDocumentNumber,
                        Notes, RowVersion, CreatedAt, UpdatedAt
                    ) VALUES (
                        @Id, @Number, @ReqUserId, @ReqUserName, @ReqAt, @Priority, @Reason, @Status,
                        @ApprUserId, @ApprUserName, @ApprAt, @CancUserId, @CancAt, @CancReason,
                        @SupplierId, @SupplierName, @EstCost, @ActCost, @DocNum, @Notes, 1, @CreatedAt, @UpdatedAt
                    );
                ";

                AddRequestParameters(command, request);
                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                // Inserir itens se houver
                if (request.Items != null && request.Items.Count > 0)
                {
                    foreach (var item in request.Items)
                    {
                        await using var itemCmd = connection.CreateCommand();
                        itemCmd.Transaction = tx;
                        itemCmd.CommandText = @"
                            INSERT INTO PurchaseRequestItems (
                                ItemId, PurchaseRequestId, ProductId, ProductCode, ProductName,
                                RequestedQuantity, SuggestedQuantity, CurrentStock, MinimumStock, IdealStock,
                                EstimatedUnitCostCents, ActualUnitCostCents, ReceivedQuantity, Priority,
                                Reason, Notes, Status, CreatedAt
                            ) VALUES (
                                @ItemId, @PurchaseRequestId, @ProductId, @ProductCode, @ProductName,
                                @RequestedQuantity, @SuggestedQuantity, @CurrentStock, @MinimumStock, @IdealStock,
                                @EstimatedUnitCostCents, @ActualUnitCostCents, @ReceivedQuantity, @Priority,
                                @Reason, @Notes, @Status, @CreatedAt
                            );
                        ";
                        AddItemParameters(itemCmd, item, request.PurchaseRequestId);
                        await itemCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                    }
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao inserir requisição de compra {request.Number}", ex);
                throw;
            }
        }

        public async Task<bool> AtualizarAsync(PurchaseRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE PurchaseRequests SET
                        Number = @Number,
                        Priority = @Priority,
                        Reason = @Reason,
                        Status = @Status,
                        ApprovedByUserId = @ApprUserId,
                        ApprovedByUserName = @ApprUserName,
                        ApprovedAt = @ApprAt,
                        CancelledByUserId = @CancUserId,
                        CancelledAt = @CancAt,
                        CancellationReason = @CancReason,
                        SupplierId = @SupplierId,
                        SupplierName = @SupplierName,
                        TotalEstimatedCostCents = @EstCost,
                        TotalActualCostCents = @ActCost,
                        FiscalDocumentNumber = @DocNum,
                        Notes = @Notes,
                        RowVersion = RowVersion + 1,
                        UpdatedAt = @UpdatedAt
                    WHERE PurchaseRequestId = @Id AND RowVersion = @RowVersion;
                ";

                AddRequestParameters(command, request);
                command.Parameters.Add(CreateParameter(command, "@RowVersion", request.RowVersion));

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                if (affected > 0)
                {
                    request.RowVersion++;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao atualizar requisição de compra {request.Number}", ex);
                throw;
            }
        }

        public async Task<bool> AtualizarStatusAsync(Guid requestId, PurchaseRequestStatus status, int usuarioId, string? motivo = null, CancellationToken ct = default)
        {
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                if (status == PurchaseRequestStatus.APPROVED)
                {
                    command.CommandText = @"
                        UPDATE PurchaseRequests
                        SET Status = 'APPROVED',
                            ApprovedByUserId = @UserId,
                            ApprovedAt = @Now,
                            RowVersion = RowVersion + 1,
                            UpdatedAt = @Now
                        WHERE PurchaseRequestId = @Id;
                    ";
                }
                else if (status == PurchaseRequestStatus.CANCELLED)
                {
                    command.CommandText = @"
                        UPDATE PurchaseRequests
                        SET Status = 'CANCELLED',
                            CancelledByUserId = @UserId,
                            CancelledAt = @Now,
                            CancellationReason = @Motivo,
                            RowVersion = RowVersion + 1,
                            UpdatedAt = @Now
                        WHERE PurchaseRequestId = @Id;
                    ";
                    command.Parameters.Add(CreateParameter(command, "@Motivo", motivo));
                }
                else
                {
                    command.CommandText = @"
                        UPDATE PurchaseRequests
                        SET Status = @Status,
                            RowVersion = RowVersion + 1,
                            UpdatedAt = @Now
                        WHERE PurchaseRequestId = @Id;
                    ";
                    command.Parameters.Add(CreateParameter(command, "@Status", status.ToString()));
                }

                command.Parameters.Add(CreateParameter(command, "@Id", requestId.ToString()));
                command.Parameters.Add(CreateParameter(command, "@UserId", usuarioId));
                command.Parameters.Add(CreateParameter(command, "@Now", now));

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao atualizar status da requisição {requestId} para {status}", ex);
                throw;
            }
        }

        public async Task<bool> ExcluirAsync(Guid requestId, CancellationToken ct = default)
        {
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM PurchaseRequests WHERE PurchaseRequestId = @Id;";
                command.Parameters.Add(CreateParameter(command, "@Id", requestId.ToString()));

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao excluir requisição {requestId}", ex);
                throw;
            }
        }

        public async Task<IReadOnlyList<PurchaseRequestItem>> ObterItensAsync(Guid requestId, CancellationToken ct = default)
        {
            var list = new List<PurchaseRequestItem>();
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT ItemId, PurchaseRequestId, ProductId, ProductCode, ProductName,
                           RequestedQuantity, SuggestedQuantity, CurrentStock, MinimumStock, IdealStock,
                           EstimatedUnitCostCents, ActualUnitCostCents, ReceivedQuantity, Priority,
                           Reason, Notes, Status, CreatedAt
                    FROM PurchaseRequestItems
                    WHERE PurchaseRequestId = @ReqId
                    ORDER BY ProductName ASC;
                ";
                command.Parameters.Add(CreateParameter(command, "@ReqId", requestId.ToString()));

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    list.Add(MapItem(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao obter itens da requisição {requestId}", ex);
                throw;
            }

            return list;
        }

        public async Task<bool> InserirItemAsync(PurchaseRequestItem item, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(item);

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO PurchaseRequestItems (
                        ItemId, PurchaseRequestId, ProductId, ProductCode, ProductName,
                        RequestedQuantity, SuggestedQuantity, CurrentStock, MinimumStock, IdealStock,
                        EstimatedUnitCostCents, ActualUnitCostCents, ReceivedQuantity, Priority,
                        Reason, Notes, Status, CreatedAt
                    ) VALUES (
                        @ItemId, @PurchaseRequestId, @ProductId, @ProductCode, @ProductName,
                        @RequestedQuantity, @SuggestedQuantity, @CurrentStock, @MinimumStock, @IdealStock,
                        @EstimatedUnitCostCents, @ActualUnitCostCents, @ReceivedQuantity, @Priority,
                        @Reason, @Notes, @Status, @CreatedAt
                    );
                ";
                AddItemParameters(command, item, item.PurchaseRequestId);

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao inserir item de compra {item.ProductName}", ex);
                throw;
            }
        }

        public async Task<bool> AtualizarItemAsync(PurchaseRequestItem item, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(item);

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE PurchaseRequestItems SET
                        RequestedQuantity = @RequestedQuantity,
                        SuggestedQuantity = @SuggestedQuantity,
                        EstimatedUnitCostCents = @EstimatedUnitCostCents,
                        ActualUnitCostCents = @ActualUnitCostCents,
                        ReceivedQuantity = @ReceivedQuantity,
                        Priority = @Priority,
                        Reason = @Reason,
                        Notes = @Notes,
                        Status = @Status
                    WHERE ItemId = @ItemId;
                ";
                command.Parameters.Add(CreateParameter(command, "@RequestedQuantity", item.RequestedQuantity));
                command.Parameters.Add(CreateParameter(command, "@SuggestedQuantity", item.SuggestedQuantity));
                command.Parameters.Add(CreateParameter(command, "@EstimatedUnitCostCents", item.EstimatedUnitCostCents));
                command.Parameters.Add(CreateParameter(command, "@ActualUnitCostCents", item.ActualUnitCostCents));
                command.Parameters.Add(CreateParameter(command, "@ReceivedQuantity", item.ReceivedQuantity));
                command.Parameters.Add(CreateParameter(command, "@Priority", item.Priority.ToString()));
                command.Parameters.Add(CreateParameter(command, "@Reason", item.Reason));
                command.Parameters.Add(CreateParameter(command, "@Notes", item.Notes));
                command.Parameters.Add(CreateParameter(command, "@Status", item.Status.ToString()));
                command.Parameters.Add(CreateParameter(command, "@ItemId", item.ItemId.ToString()));

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao atualizar item de compra {item.ItemId}", ex);
                throw;
            }
        }

        public async Task<bool> ExcluirItemAsync(Guid itemId, CancellationToken ct = default)
        {
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM PurchaseRequestItems WHERE ItemId = @Id;";
                command.Parameters.Add(CreateParameter(command, "@Id", itemId.ToString()));

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao excluir item de compra {itemId}", ex);
                throw;
            }
        }

        public async Task<IReadOnlyList<Produto>> ObterProdutosAbaixoEstoqueMinimoAsync(CancellationToken ct = default)
        {
            var list = new List<Produto>();
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Id, Codigo, Nome, QuantidadeEstoque, QuantidadeMinima, PrecoCompra, PrecoVenda
                    FROM Produtos
                    WHERE Ativo = 1 AND QuantidadeEstoque <= QuantidadeMinima AND QuantidadeMinima > 0
                    ORDER BY (QuantidadeEstoque - QuantidadeMinima) ASC, Nome ASC;
                ";

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    list.Add(new Produto
                    {
                        Id = Guid.TryParse(reader["Id"]?.ToString(), out var pid) ? pid : Guid.NewGuid(),
                        Codigo = reader["Codigo"]?.ToString() ?? string.Empty,
                        Nome = reader["Nome"]?.ToString() ?? string.Empty,
                        QuantidadeEstoque = Convert.ToInt32(reader["QuantidadeEstoque"]),
                        QuantidadeMinima = Convert.ToInt32(reader["QuantidadeMinima"]),
                        PrecoCompra = MoneyIO.LerMoeda(reader, reader.GetOrdinal("PrecoCompra")),
                        PrecoVenda = MoneyIO.LerMoeda(reader, reader.GetOrdinal("PrecoVenda"))
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao buscar produtos abaixo do estoque mínimo.", ex);
                throw;
            }

            return list;
        }

        private static void AddRequestParameters(DbCommand command, PurchaseRequest request)
        {
            command.Parameters.Add(CreateParameter(command, "@Id", request.PurchaseRequestId.ToString()));
            command.Parameters.Add(CreateParameter(command, "@Number", request.Number));
            command.Parameters.Add(CreateParameter(command, "@ReqUserId", request.RequestedByUserId));
            command.Parameters.Add(CreateParameter(command, "@ReqUserName", request.RequestedByUserName));
            command.Parameters.Add(CreateParameter(command, "@ReqAt", request.RequestedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(CreateParameter(command, "@Priority", request.Priority.ToString()));
            command.Parameters.Add(CreateParameter(command, "@Reason", request.Reason.ToString()));
            command.Parameters.Add(CreateParameter(command, "@Status", request.Status.ToString()));
            command.Parameters.Add(CreateParameter(command, "@ApprUserId", request.ApprovedByUserId));
            command.Parameters.Add(CreateParameter(command, "@ApprUserName", request.ApprovedByUserName));
            command.Parameters.Add(CreateParameter(command, "@ApprAt", request.ApprovedAt?.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(CreateParameter(command, "@CancUserId", request.CancelledByUserId));
            command.Parameters.Add(CreateParameter(command, "@CancAt", request.CancelledAt?.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(CreateParameter(command, "@CancReason", request.CancellationReason));
            command.Parameters.Add(CreateParameter(command, "@SupplierId", request.SupplierId.HasValue ? request.SupplierId.Value.ToString() : (object)DBNull.Value));
            command.Parameters.Add(CreateParameter(command, "@SupplierName", request.SupplierName));
            command.Parameters.Add(CreateParameter(command, "@EstCost", request.TotalEstimatedCostCents));
            command.Parameters.Add(CreateParameter(command, "@ActCost", request.TotalActualCostCents));
            command.Parameters.Add(CreateParameter(command, "@DocNum", request.FiscalDocumentNumber));
            command.Parameters.Add(CreateParameter(command, "@Notes", request.Notes));
            command.Parameters.Add(CreateParameter(command, "@CreatedAt", request.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(CreateParameter(command, "@UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        private static void AddItemParameters(DbCommand command, PurchaseRequestItem item, Guid requestId)
        {
            command.Parameters.Add(CreateParameter(command, "@ItemId", item.ItemId.ToString()));
            command.Parameters.Add(CreateParameter(command, "@PurchaseRequestId", requestId.ToString()));
            command.Parameters.Add(CreateParameter(command, "@ProductId", item.ProductId.HasValue ? item.ProductId.Value.ToString() : (object)DBNull.Value));
            command.Parameters.Add(CreateParameter(command, "@ProductCode", item.ProductCode));
            command.Parameters.Add(CreateParameter(command, "@ProductName", item.ProductName));
            command.Parameters.Add(CreateParameter(command, "@RequestedQuantity", item.RequestedQuantity));
            command.Parameters.Add(CreateParameter(command, "@SuggestedQuantity", item.SuggestedQuantity));
            command.Parameters.Add(CreateParameter(command, "@CurrentStock", item.CurrentStock));
            command.Parameters.Add(CreateParameter(command, "@MinimumStock", item.MinimumStock));
            command.Parameters.Add(CreateParameter(command, "@IdealStock", item.IdealStock));
            command.Parameters.Add(CreateParameter(command, "@EstimatedUnitCostCents", item.EstimatedUnitCostCents));
            command.Parameters.Add(CreateParameter(command, "@ActualUnitCostCents", item.ActualUnitCostCents));
            command.Parameters.Add(CreateParameter(command, "@ReceivedQuantity", item.ReceivedQuantity));
            command.Parameters.Add(CreateParameter(command, "@Priority", item.Priority.ToString()));
            command.Parameters.Add(CreateParameter(command, "@Reason", item.Reason));
            command.Parameters.Add(CreateParameter(command, "@Notes", item.Notes));
            command.Parameters.Add(CreateParameter(command, "@Status", item.Status.ToString()));
            command.Parameters.Add(CreateParameter(command, "@CreatedAt", item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        private static PurchaseRequest MapRequest(DbDataReader reader)
        {
            return new PurchaseRequest
            {
                PurchaseRequestId = Guid.TryParse(reader["PurchaseRequestId"]?.ToString(), out var rid) ? rid : Guid.Empty,
                Number = reader["Number"]?.ToString() ?? string.Empty,
                RequestedByUserId = reader["RequestedByUserId"] is DBNull ? 0 : Convert.ToInt32(reader["RequestedByUserId"]),
                RequestedByUserName = reader["RequestedByUserName"]?.ToString() ?? string.Empty,
                RequestedAt = DateTime.TryParse(reader["RequestedAt"]?.ToString(), out var ra) ? ra : DateTime.Now,
                Priority = Enum.TryParse<PurchasePriority>(reader["Priority"]?.ToString(), out var pri) ? pri : PurchasePriority.NORMAL,
                Reason = Enum.TryParse<PurchaseReason>(reader["Reason"]?.ToString(), out var re) ? re : PurchaseReason.LOW_STOCK,
                Status = Enum.TryParse<PurchaseRequestStatus>(reader["Status"]?.ToString(), out var st) ? st : PurchaseRequestStatus.REQUESTED,
                ApprovedByUserId = reader["ApprovedByUserId"] is DBNull ? null : Convert.ToInt32(reader["ApprovedByUserId"]),
                ApprovedByUserName = reader["ApprovedByUserName"] is DBNull ? null : reader["ApprovedByUserName"]?.ToString(),
                ApprovedAt = reader["ApprovedAt"] is DBNull || string.IsNullOrWhiteSpace(reader["ApprovedAt"]?.ToString()) ? null : DateTime.TryParse(reader["ApprovedAt"]?.ToString(), out var aa) ? aa : null,
                CancelledByUserId = reader["CancelledByUserId"] is DBNull ? null : Convert.ToInt32(reader["CancelledByUserId"]),
                CancelledAt = reader["CancelledAt"] is DBNull || string.IsNullOrWhiteSpace(reader["CancelledAt"]?.ToString()) ? null : DateTime.TryParse(reader["CancelledAt"]?.ToString(), out var ca) ? ca : null,
                CancellationReason = reader["CancellationReason"] is DBNull ? null : reader["CancellationReason"]?.ToString(),
                SupplierId = reader["SupplierId"] is DBNull || string.IsNullOrWhiteSpace(reader["SupplierId"]?.ToString()) ? null : Guid.TryParse(reader["SupplierId"]?.ToString(), out var sid) ? sid : null,
                SupplierName = reader["SupplierName"] is DBNull ? null : reader["SupplierName"]?.ToString(),
                TotalEstimatedCostCents = reader["TotalEstimatedCostCents"] is DBNull ? 0 : Convert.ToInt64(reader["TotalEstimatedCostCents"]),
                TotalActualCostCents = reader["TotalActualCostCents"] is DBNull ? 0 : Convert.ToInt64(reader["TotalActualCostCents"]),
                FiscalDocumentNumber = reader["FiscalDocumentNumber"] is DBNull ? null : reader["FiscalDocumentNumber"]?.ToString(),
                Notes = reader["Notes"] is DBNull ? null : reader["Notes"]?.ToString(),
                RowVersion = reader["RowVersion"] is DBNull ? 1 : Convert.ToInt32(reader["RowVersion"]),
                CreatedAt = DateTime.TryParse(reader["CreatedAt"]?.ToString(), out var cr) ? cr : DateTime.Now,
                UpdatedAt = DateTime.TryParse(reader["UpdatedAt"]?.ToString(), out var up) ? up : DateTime.Now
            };
        }

        private static PurchaseRequestItem MapItem(DbDataReader reader)
        {
            return new PurchaseRequestItem
            {
                ItemId = Guid.TryParse(reader["ItemId"]?.ToString(), out var iid) ? iid : Guid.Empty,
                PurchaseRequestId = Guid.TryParse(reader["PurchaseRequestId"]?.ToString(), out var pid) ? pid : Guid.Empty,
                ProductId = reader["ProductId"] is DBNull || string.IsNullOrWhiteSpace(reader["ProductId"]?.ToString()) ? null : Guid.TryParse(reader["ProductId"]?.ToString(), out var g) ? g : null,
                ProductCode = reader["ProductCode"] is DBNull ? null : reader["ProductCode"]?.ToString(),
                ProductName = reader["ProductName"]?.ToString() ?? string.Empty,
                RequestedQuantity = reader["RequestedQuantity"] is DBNull ? 1 : Convert.ToDecimal(reader["RequestedQuantity"]),
                SuggestedQuantity = reader["SuggestedQuantity"] is DBNull ? 0 : Convert.ToDecimal(reader["SuggestedQuantity"]),
                CurrentStock = reader["CurrentStock"] is DBNull ? 0 : Convert.ToDecimal(reader["CurrentStock"]),
                MinimumStock = reader["MinimumStock"] is DBNull ? 0 : Convert.ToDecimal(reader["MinimumStock"]),
                IdealStock = reader["IdealStock"] is DBNull ? 0 : Convert.ToDecimal(reader["IdealStock"]),
                EstimatedUnitCostCents = reader["EstimatedUnitCostCents"] is DBNull ? 0 : Convert.ToInt64(reader["EstimatedUnitCostCents"]),
                ActualUnitCostCents = reader["ActualUnitCostCents"] is DBNull ? 0 : Convert.ToInt64(reader["ActualUnitCostCents"]),
                ReceivedQuantity = reader["ReceivedQuantity"] is DBNull ? 0 : Convert.ToDecimal(reader["ReceivedQuantity"]),
                Priority = Enum.TryParse<PurchasePriority>(reader["Priority"]?.ToString(), out var pri) ? pri : PurchasePriority.NORMAL,
                Reason = reader["Reason"] is DBNull ? null : reader["Reason"]?.ToString(),
                Notes = reader["Notes"] is DBNull ? null : reader["Notes"]?.ToString(),
                Status = Enum.TryParse<PurchaseItemStatus>(reader["Status"]?.ToString(), out var st) ? st : PurchaseItemStatus.PENDING,
                CreatedAt = DateTime.TryParse(reader["CreatedAt"]?.ToString(), out var ca) ? ca : DateTime.Now
            };
        }
    }
}
