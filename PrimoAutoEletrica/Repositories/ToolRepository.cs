using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Repositories
{
    public sealed class ToolRepository : IToolRepository
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly LoggerService _logger;

        public ToolRepository(Func<DbConnection> connectionFactory, LoggerService logger)
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

        public async Task<IReadOnlyList<Tool>> ObterTodosAsync(string? busca = null, string? categoria = null, ToolStatus? status = null, int? responsavelId = null, CancellationToken ct = default)
        {
            var list = new List<Tool>();
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                var sql = @"
                    SELECT ToolId, Code, Name, Category, Brand, Model, SerialNumber, PatrimonyNumber,
                           Description, PhotoPath, LocationName, CurrentResponsibleUserId, CurrentResponsibleUserName,
                           Status, PurchaseDate, PurchaseValueCents, WarrantyExpiration, LastMaintenanceDate,
                           NextMaintenanceDate, Notes, RowVersion, CreatedAt, UpdatedAt
                    FROM Tools
                    WHERE 1=1
                ";

                if (!string.IsNullOrWhiteSpace(busca))
                {
                    sql += " AND (Code LIKE @Busca OR Name LIKE @Busca OR Brand LIKE @Busca OR SerialNumber LIKE @Busca OR PatrimonyNumber LIKE @Busca)";
                    command.Parameters.Add(CreateParameter(command, "@Busca", $"%{busca.Trim()}%"));
                }

                if (!string.IsNullOrWhiteSpace(categoria) && !string.Equals(categoria, "Todas", StringComparison.OrdinalIgnoreCase))
                {
                    sql += " AND Category = @Categoria";
                    command.Parameters.Add(CreateParameter(command, "@Categoria", categoria.Trim()));
                }

                if (status.HasValue)
                {
                    sql += " AND Status = @Status";
                    command.Parameters.Add(CreateParameter(command, "@Status", status.Value.ToString()));
                }

                if (responsavelId.HasValue)
                {
                    sql += " AND CurrentResponsibleUserId = @RespId";
                    command.Parameters.Add(CreateParameter(command, "@RespId", responsavelId.Value));
                }

                sql += " ORDER BY Code ASC;";
                command.CommandText = sql;

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    list.Add(MapTool(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao consultar ferramentas no banco de dados.", ex);
                throw;
            }

            return list;
        }

        public async Task<Tool?> ObterPorIdAsync(Guid toolId, CancellationToken ct = default)
        {
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT ToolId, Code, Name, Category, Brand, Model, SerialNumber, PatrimonyNumber,
                           Description, PhotoPath, LocationName, CurrentResponsibleUserId, CurrentResponsibleUserName,
                           Status, PurchaseDate, PurchaseValueCents, WarrantyExpiration, LastMaintenanceDate,
                           NextMaintenanceDate, Notes, RowVersion, CreatedAt, UpdatedAt
                    FROM Tools
                    WHERE ToolId = @ToolId LIMIT 1;
                ";
                command.Parameters.Add(CreateParameter(command, "@ToolId", toolId.ToString()));

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    return MapTool(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao obter ferramenta por Id: {toolId}", ex);
                throw;
            }

            return null;
        }

        public async Task<Tool?> ObterPorCodigoAsync(string codigo, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT ToolId, Code, Name, Category, Brand, Model, SerialNumber, PatrimonyNumber,
                           Description, PhotoPath, LocationName, CurrentResponsibleUserId, CurrentResponsibleUserName,
                           Status, PurchaseDate, PurchaseValueCents, WarrantyExpiration, LastMaintenanceDate,
                           NextMaintenanceDate, Notes, RowVersion, CreatedAt, UpdatedAt
                    FROM Tools
                    WHERE lower(Code) = lower(@Code) LIMIT 1;
                ";
                command.Parameters.Add(CreateParameter(command, "@Code", codigo.Trim()));

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    return MapTool(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao obter ferramenta por Código: {codigo}", ex);
                throw;
            }

            return null;
        }

        public async Task<bool> InserirAsync(Tool tool, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(tool);

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Tools (
                        ToolId, Code, Name, Category, Brand, Model, SerialNumber, PatrimonyNumber,
                        Description, PhotoPath, LocationName, CurrentResponsibleUserId, CurrentResponsibleUserName,
                        Status, PurchaseDate, PurchaseValueCents, WarrantyExpiration, LastMaintenanceDate,
                        NextMaintenanceDate, Notes, RowVersion, CreatedAt, UpdatedAt
                    ) VALUES (
                        @ToolId, @Code, @Name, @Category, @Brand, @Model, @SerialNumber, @PatrimonyNumber,
                        @Description, @PhotoPath, @LocationName, @CurrentResponsibleUserId, @CurrentResponsibleUserName,
                        @Status, @PurchaseDate, @PurchaseValueCents, @WarrantyExpiration, @LastMaintenanceDate,
                        @NextMaintenanceDate, @Notes, 1, @CreatedAt, @UpdatedAt
                    );
                ";

                AddToolParameters(command, tool);
                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao inserir ferramenta {tool.Code}", ex);
                throw;
            }
        }

        public async Task<bool> AtualizarAsync(Tool tool, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(tool);

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Tools SET
                        Code = @Code,
                        Name = @Name,
                        Category = @Category,
                        Brand = @Brand,
                        Model = @Model,
                        SerialNumber = @SerialNumber,
                        PatrimonyNumber = @PatrimonyNumber,
                        Description = @Description,
                        PhotoPath = @PhotoPath,
                        LocationName = @LocationName,
                        CurrentResponsibleUserId = @CurrentResponsibleUserId,
                        CurrentResponsibleUserName = @CurrentResponsibleUserName,
                        Status = @Status,
                        PurchaseDate = @PurchaseDate,
                        PurchaseValueCents = @PurchaseValueCents,
                        WarrantyExpiration = @WarrantyExpiration,
                        LastMaintenanceDate = @LastMaintenanceDate,
                        NextMaintenanceDate = @NextMaintenanceDate,
                        Notes = @Notes,
                        RowVersion = RowVersion + 1,
                        UpdatedAt = @UpdatedAt
                    WHERE ToolId = @ToolId AND RowVersion = @RowVersion;
                ";

                AddToolParameters(command, tool);
                command.Parameters.Add(CreateParameter(command, "@RowVersion", tool.RowVersion));

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                if (affected > 0)
                {
                    tool.RowVersion++;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao atualizar ferramenta {tool.Code}", ex);
                throw;
            }
        }

        public async Task<bool> ExcluirAsync(Guid toolId, CancellationToken ct = default)
        {
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Tools WHERE ToolId = @ToolId;";
                command.Parameters.Add(CreateParameter(command, "@ToolId", toolId.ToString()));

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao excluir ferramenta {toolId}", ex);
                throw;
            }
        }

        public async Task<bool> RegistrarRetiradaAsync(ToolCheckout checkout, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(checkout);

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);
                await using var tx = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

                // 1. Atualizar ferramenta apenas se AVAILABLE
                await using var updateCmd = connection.CreateCommand();
                updateCmd.Transaction = tx;
                updateCmd.CommandText = @"
                    UPDATE Tools
                    SET Status = 'IN_USE',
                        CurrentResponsibleUserId = @UserId,
                        CurrentResponsibleUserName = @UserName,
                        RowVersion = RowVersion + 1,
                        UpdatedAt = @Now
                    WHERE ToolId = @ToolId AND Status = 'AVAILABLE';
                ";
                updateCmd.Parameters.Add(CreateParameter(updateCmd, "@UserId", checkout.UserId));
                updateCmd.Parameters.Add(CreateParameter(updateCmd, "@UserName", checkout.UserName));
                updateCmd.Parameters.Add(CreateParameter(updateCmd, "@Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                updateCmd.Parameters.Add(CreateParameter(updateCmd, "@ToolId", checkout.ToolId.ToString()));

                var updated = await updateCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                if (updated == 0)
                {
                    await tx.RollbackAsync(ct).ConfigureAwait(false);
                    return false; // Conflito ou ferramenta indisponível
                }

                // 2. Inserir ToolCheckout
                await using var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = tx;
                insertCmd.CommandText = @"
                    INSERT INTO ToolCheckouts (
                        CheckoutId, ToolId, UserId, UserName, WorkOrderId, WorkOrderNumber,
                        VehiclePlate, CheckoutDate, ExpectedReturnDate, CheckoutNotes, Status, CreatedAt
                    ) VALUES (
                        @CheckoutId, @ToolId, @UserId, @UserName, @WorkOrderId, @WorkOrderNumber,
                        @VehiclePlate, @CheckoutDate, @ExpectedReturnDate, @CheckoutNotes, 'OPEN', @CreatedAt
                    );
                ";
                insertCmd.Parameters.Add(CreateParameter(insertCmd, "@CheckoutId", checkout.CheckoutId.ToString()));
                insertCmd.Parameters.Add(CreateParameter(insertCmd, "@ToolId", checkout.ToolId.ToString()));
                insertCmd.Parameters.Add(CreateParameter(insertCmd, "@UserId", checkout.UserId));
                insertCmd.Parameters.Add(CreateParameter(insertCmd, "@UserName", checkout.UserName));
                insertCmd.Parameters.Add(CreateParameter(insertCmd, "@WorkOrderId", checkout.WorkOrderId?.ToString()));
                insertCmd.Parameters.Add(CreateParameter(insertCmd, "@WorkOrderNumber", checkout.WorkOrderNumber));
                insertCmd.Parameters.Add(CreateParameter(insertCmd, "@VehiclePlate", checkout.VehiclePlate));
                insertCmd.Parameters.Add(CreateParameter(insertCmd, "@CheckoutDate", checkout.CheckoutDate.ToString("yyyy-MM-dd HH:mm:ss")));
                insertCmd.Parameters.Add(CreateParameter(insertCmd, "@ExpectedReturnDate", checkout.ExpectedReturnDate?.ToString("yyyy-MM-dd HH:mm:ss")));
                insertCmd.Parameters.Add(CreateParameter(insertCmd, "@CheckoutNotes", checkout.CheckoutNotes));
                insertCmd.Parameters.Add(CreateParameter(insertCmd, "@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                await insertCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                await tx.CommitAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao registrar retirada da ferramenta {checkout.ToolId}", ex);
                throw;
            }
        }

        public async Task<bool> RegistrarDevolucaoAsync(Guid toolId, Guid checkoutId, DateTime dataDevolucao, int usuarioId, ToolCondition condicao, string? observacoes, CancellationToken ct = default)
        {
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);
                await using var tx = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

                // 1. Atualizar checkout
                await using var updateCheckoutCmd = connection.CreateCommand();
                updateCheckoutCmd.Transaction = tx;
                updateCheckoutCmd.CommandText = @"
                    UPDATE ToolCheckouts
                    SET ReturnDate = @ReturnDate,
                        ReturnedByUserId = @ReturnedByUserId,
                        ReturnCondition = @Condition,
                        ReturnNotes = @Notes,
                        Status = 'RETURNED'
                    WHERE CheckoutId = @CheckoutId AND Status = 'OPEN';
                ";
                updateCheckoutCmd.Parameters.Add(CreateParameter(updateCheckoutCmd, "@ReturnDate", dataDevolucao.ToString("yyyy-MM-dd HH:mm:ss")));
                updateCheckoutCmd.Parameters.Add(CreateParameter(updateCheckoutCmd, "@ReturnedByUserId", usuarioId));
                updateCheckoutCmd.Parameters.Add(CreateParameter(updateCheckoutCmd, "@Condition", condicao.ToString()));
                updateCheckoutCmd.Parameters.Add(CreateParameter(updateCheckoutCmd, "@Notes", observacoes));
                updateCheckoutCmd.Parameters.Add(CreateParameter(updateCheckoutCmd, "@CheckoutId", checkoutId.ToString()));

                var checkoutUpdated = await updateCheckoutCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                if (checkoutUpdated == 0)
                {
                    await tx.RollbackAsync(ct).ConfigureAwait(false);
                    return false;
                }

                // 2. Determinar novo status da ferramenta com base na condição
                var novoStatus = condicao switch
                {
                    ToolCondition.OK => ToolStatus.AVAILABLE,
                    ToolCondition.DAMAGED => ToolStatus.DAMAGED,
                    ToolCondition.NEEDS_CALIBRATION => ToolStatus.MAINTENANCE,
                    _ => ToolStatus.AVAILABLE
                };

                await using var updateToolCmd = connection.CreateCommand();
                updateToolCmd.Transaction = tx;
                updateToolCmd.CommandText = @"
                    UPDATE Tools
                    SET Status = @NovoStatus,
                        CurrentResponsibleUserId = NULL,
                        CurrentResponsibleUserName = NULL,
                        RowVersion = RowVersion + 1,
                        UpdatedAt = @Now
                    WHERE ToolId = @ToolId;
                ";
                updateToolCmd.Parameters.Add(CreateParameter(updateToolCmd, "@NovoStatus", novoStatus.ToString()));
                updateToolCmd.Parameters.Add(CreateParameter(updateToolCmd, "@Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                updateToolCmd.Parameters.Add(CreateParameter(updateToolCmd, "@ToolId", toolId.ToString()));

                await updateToolCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                await tx.CommitAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao registrar devolução da ferramenta {toolId}", ex);
                throw;
            }
        }

        public async Task<IReadOnlyList<ToolCheckout>> ObterHistoricoMovimentacoesAsync(Guid toolId, CancellationToken ct = default)
        {
            var list = new List<ToolCheckout>();
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT CheckoutId, ToolId, UserId, UserName, WorkOrderId, WorkOrderNumber,
                           VehiclePlate, CheckoutDate, ExpectedReturnDate, ReturnDate, ReturnedByUserId,
                           ReturnCondition, CheckoutNotes, ReturnNotes, Status, CreatedAt
                    FROM ToolCheckouts
                    WHERE ToolId = @ToolId
                    ORDER BY CheckoutDate DESC;
                ";
                command.Parameters.Add(CreateParameter(command, "@ToolId", toolId.ToString()));

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    list.Add(MapCheckout(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao obter histórico de movimentações da ferramenta {toolId}", ex);
                throw;
            }

            return list;
        }

        public async Task<ToolCheckout?> ObterCheckoutAtivoAsync(Guid toolId, CancellationToken ct = default)
        {
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT CheckoutId, ToolId, UserId, UserName, WorkOrderId, WorkOrderNumber,
                           VehiclePlate, CheckoutDate, ExpectedReturnDate, ReturnDate, ReturnedByUserId,
                           ReturnCondition, CheckoutNotes, ReturnNotes, Status, CreatedAt
                    FROM ToolCheckouts
                    WHERE ToolId = @ToolId AND Status = 'OPEN'
                    ORDER BY CheckoutDate DESC LIMIT 1;
                ";
                command.Parameters.Add(CreateParameter(command, "@ToolId", toolId.ToString()));

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    return MapCheckout(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao obter checkout ativo da ferramenta {toolId}", ex);
                throw;
            }

            return null;
        }

        public async Task<bool> InserirManutencaoAsync(ToolMaintenance manutencao, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(manutencao);

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);
                await using var tx = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.Transaction = tx;
                command.CommandText = @"
                    INSERT INTO ToolMaintenances (
                        MaintenanceId, ToolId, MaintenanceType, Description, CostCents,
                        Provider, StartDate, CompletionDate, PerformedBy, Status, Notes, CreatedAt
                    ) VALUES (
                        @MaintenanceId, @ToolId, @MaintenanceType, @Description, @CostCents,
                        @Provider, @StartDate, @CompletionDate, @PerformedBy, @Status, @Notes, @CreatedAt
                    );
                ";
                command.Parameters.Add(CreateParameter(command, "@MaintenanceId", manutencao.MaintenanceId.ToString()));
                command.Parameters.Add(CreateParameter(command, "@ToolId", manutencao.ToolId.ToString()));
                command.Parameters.Add(CreateParameter(command, "@MaintenanceType", manutencao.MaintenanceType.ToString()));
                command.Parameters.Add(CreateParameter(command, "@Description", manutencao.Description));
                command.Parameters.Add(CreateParameter(command, "@CostCents", manutencao.CostCents));
                command.Parameters.Add(CreateParameter(command, "@Provider", manutencao.Provider));
                command.Parameters.Add(CreateParameter(command, "@StartDate", manutencao.StartDate.ToString("yyyy-MM-dd HH:mm:ss")));
                command.Parameters.Add(CreateParameter(command, "@CompletionDate", manutencao.CompletionDate?.ToString("yyyy-MM-dd HH:mm:ss")));
                command.Parameters.Add(CreateParameter(command, "@PerformedBy", manutencao.PerformedBy));
                command.Parameters.Add(CreateParameter(command, "@Status", manutencao.Status.ToString()));
                command.Parameters.Add(CreateParameter(command, "@Notes", manutencao.Notes));
                command.Parameters.Add(CreateParameter(command, "@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                // Atualizar LastMaintenanceDate da ferramenta
                await using var updateCmd = connection.CreateCommand();
                updateCmd.Transaction = tx;
                updateCmd.CommandText = @"
                    UPDATE Tools
                    SET LastMaintenanceDate = @LastMaint,
                        RowVersion = RowVersion + 1,
                        UpdatedAt = @Now
                    WHERE ToolId = @ToolId;
                ";
                updateCmd.Parameters.Add(CreateParameter(updateCmd, "@LastMaint", manutencao.StartDate.ToString("yyyy-MM-dd HH:mm:ss")));
                updateCmd.Parameters.Add(CreateParameter(updateCmd, "@Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                updateCmd.Parameters.Add(CreateParameter(updateCmd, "@ToolId", manutencao.ToolId.ToString()));

                await updateCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                await tx.CommitAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao inserir manutenção da ferramenta {manutencao.ToolId}", ex);
                throw;
            }
        }

        public async Task<IReadOnlyList<ToolMaintenance>> ObterManutencoesAsync(Guid toolId, CancellationToken ct = default)
        {
            var list = new List<ToolMaintenance>();
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT MaintenanceId, ToolId, MaintenanceType, Description, CostCents,
                           Provider, StartDate, CompletionDate, PerformedBy, Status, Notes, CreatedAt
                    FROM ToolMaintenances
                    WHERE ToolId = @ToolId
                    ORDER BY StartDate DESC;
                ";
                command.Parameters.Add(CreateParameter(command, "@ToolId", toolId.ToString()));

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    list.Add(MapMaintenance(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao obter manutenções da ferramenta {toolId}", ex);
                throw;
            }

            return list;
        }

        private static void AddToolParameters(DbCommand command, Tool tool)
        {
            command.Parameters.Add(CreateParameter(command, "@ToolId", tool.ToolId.ToString()));
            command.Parameters.Add(CreateParameter(command, "@Code", tool.Code));
            command.Parameters.Add(CreateParameter(command, "@Name", tool.Name));
            command.Parameters.Add(CreateParameter(command, "@Category", tool.Category));
            command.Parameters.Add(CreateParameter(command, "@Brand", tool.Brand));
            command.Parameters.Add(CreateParameter(command, "@Model", tool.Model));
            command.Parameters.Add(CreateParameter(command, "@SerialNumber", tool.SerialNumber));
            command.Parameters.Add(CreateParameter(command, "@PatrimonyNumber", tool.PatrimonyNumber));
            command.Parameters.Add(CreateParameter(command, "@Description", tool.Description));
            command.Parameters.Add(CreateParameter(command, "@PhotoPath", tool.PhotoPath));
            command.Parameters.Add(CreateParameter(command, "@LocationName", tool.LocationName));
            command.Parameters.Add(CreateParameter(command, "@CurrentResponsibleUserId", tool.CurrentResponsibleUserId));
            command.Parameters.Add(CreateParameter(command, "@CurrentResponsibleUserName", tool.CurrentResponsibleUserName));
            command.Parameters.Add(CreateParameter(command, "@Status", tool.Status.ToString()));
            command.Parameters.Add(CreateParameter(command, "@PurchaseDate", tool.PurchaseDate?.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(CreateParameter(command, "@PurchaseValueCents", tool.PurchaseValueCents));
            command.Parameters.Add(CreateParameter(command, "@WarrantyExpiration", tool.WarrantyExpiration?.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(CreateParameter(command, "@LastMaintenanceDate", tool.LastMaintenanceDate?.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(CreateParameter(command, "@NextMaintenanceDate", tool.NextMaintenanceDate?.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(CreateParameter(command, "@Notes", tool.Notes));
            command.Parameters.Add(CreateParameter(command, "@CreatedAt", tool.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(CreateParameter(command, "@UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        private static Tool MapTool(DbDataReader reader)
        {
            var tool = new Tool
            {
                ToolId = Guid.TryParse(reader["ToolId"]?.ToString(), out var tid) ? tid : Guid.Empty,
                Code = reader["Code"]?.ToString() ?? string.Empty,
                Name = reader["Name"]?.ToString() ?? string.Empty,
                Category = reader["Category"]?.ToString() ?? "Geral",
                Brand = reader["Brand"] is DBNull ? null : reader["Brand"]?.ToString(),
                Model = reader["Model"] is DBNull ? null : reader["Model"]?.ToString(),
                SerialNumber = reader["SerialNumber"] is DBNull ? null : reader["SerialNumber"]?.ToString(),
                PatrimonyNumber = reader["PatrimonyNumber"] is DBNull ? null : reader["PatrimonyNumber"]?.ToString(),
                Description = reader["Description"] is DBNull ? null : reader["Description"]?.ToString(),
                PhotoPath = reader["PhotoPath"] is DBNull ? null : reader["PhotoPath"]?.ToString(),
                LocationName = reader["LocationName"]?.ToString() ?? "Geral",
                CurrentResponsibleUserId = reader["CurrentResponsibleUserId"] is DBNull ? null : Convert.ToInt32(reader["CurrentResponsibleUserId"]),
                CurrentResponsibleUserName = reader["CurrentResponsibleUserName"] is DBNull ? null : reader["CurrentResponsibleUserName"]?.ToString(),
                Status = Enum.TryParse<ToolStatus>(reader["Status"]?.ToString(), out var st) ? st : ToolStatus.AVAILABLE,
                PurchaseDate = reader["PurchaseDate"] is DBNull || string.IsNullOrWhiteSpace(reader["PurchaseDate"]?.ToString()) ? null : DateTime.TryParse(reader["PurchaseDate"]?.ToString(), out var pd) ? pd : null,
                PurchaseValueCents = reader["PurchaseValueCents"] is DBNull ? 0 : Convert.ToInt64(reader["PurchaseValueCents"]),
                WarrantyExpiration = reader["WarrantyExpiration"] is DBNull || string.IsNullOrWhiteSpace(reader["WarrantyExpiration"]?.ToString()) ? null : DateTime.TryParse(reader["WarrantyExpiration"]?.ToString(), out var we) ? we : null,
                LastMaintenanceDate = reader["LastMaintenanceDate"] is DBNull || string.IsNullOrWhiteSpace(reader["LastMaintenanceDate"]?.ToString()) ? null : DateTime.TryParse(reader["LastMaintenanceDate"]?.ToString(), out var lmd) ? lmd : null,
                NextMaintenanceDate = reader["NextMaintenanceDate"] is DBNull || string.IsNullOrWhiteSpace(reader["NextMaintenanceDate"]?.ToString()) ? null : DateTime.TryParse(reader["NextMaintenanceDate"]?.ToString(), out var nmd) ? nmd : null,
                Notes = reader["Notes"] is DBNull ? null : reader["Notes"]?.ToString(),
                RowVersion = reader["RowVersion"] is DBNull ? 1 : Convert.ToInt32(reader["RowVersion"]),
                CreatedAt = DateTime.TryParse(reader["CreatedAt"]?.ToString(), out var ca) ? ca : DateTime.Now,
                UpdatedAt = DateTime.TryParse(reader["UpdatedAt"]?.ToString(), out var ua) ? ua : DateTime.Now
            };

            return tool;
        }

        private static ToolCheckout MapCheckout(DbDataReader reader)
        {
            return new ToolCheckout
            {
                CheckoutId = Guid.TryParse(reader["CheckoutId"]?.ToString(), out var cid) ? cid : Guid.Empty,
                ToolId = Guid.TryParse(reader["ToolId"]?.ToString(), out var tid) ? tid : Guid.Empty,
                UserId = reader["UserId"] is DBNull ? 0 : Convert.ToInt32(reader["UserId"]),
                UserName = reader["UserName"]?.ToString() ?? string.Empty,
                WorkOrderId = reader["WorkOrderId"] is DBNull || string.IsNullOrWhiteSpace(reader["WorkOrderId"]?.ToString()) ? null : Guid.TryParse(reader["WorkOrderId"]?.ToString(), out var wid) ? wid : null,
                WorkOrderNumber = reader["WorkOrderNumber"] is DBNull ? null : reader["WorkOrderNumber"]?.ToString(),
                VehiclePlate = reader["VehiclePlate"] is DBNull ? null : reader["VehiclePlate"]?.ToString(),
                CheckoutDate = DateTime.TryParse(reader["CheckoutDate"]?.ToString(), out var cd) ? cd : DateTime.Now,
                ExpectedReturnDate = reader["ExpectedReturnDate"] is DBNull || string.IsNullOrWhiteSpace(reader["ExpectedReturnDate"]?.ToString()) ? null : DateTime.TryParse(reader["ExpectedReturnDate"]?.ToString(), out var erd) ? erd : null,
                ReturnDate = reader["ReturnDate"] is DBNull || string.IsNullOrWhiteSpace(reader["ReturnDate"]?.ToString()) ? null : DateTime.TryParse(reader["ReturnDate"]?.ToString(), out var rd) ? rd : null,
                ReturnedByUserId = reader["ReturnedByUserId"] is DBNull ? null : Convert.ToInt32(reader["ReturnedByUserId"]),
                ReturnCondition = reader["ReturnCondition"] is DBNull || string.IsNullOrWhiteSpace(reader["ReturnCondition"]?.ToString()) ? null : Enum.TryParse<ToolCondition>(reader["ReturnCondition"]?.ToString(), out var cond) ? cond : null,
                CheckoutNotes = reader["CheckoutNotes"] is DBNull ? null : reader["CheckoutNotes"]?.ToString(),
                ReturnNotes = reader["ReturnNotes"] is DBNull ? null : reader["ReturnNotes"]?.ToString(),
                Status = Enum.TryParse<ToolCheckoutStatus>(reader["Status"]?.ToString(), out var st) ? st : ToolCheckoutStatus.OPEN,
                CreatedAt = DateTime.TryParse(reader["CreatedAt"]?.ToString(), out var ca) ? ca : DateTime.Now
            };
        }

        private static ToolMaintenance MapMaintenance(DbDataReader reader)
        {
            return new ToolMaintenance
            {
                MaintenanceId = Guid.TryParse(reader["MaintenanceId"]?.ToString(), out var mid) ? mid : Guid.Empty,
                ToolId = Guid.TryParse(reader["ToolId"]?.ToString(), out var tid) ? tid : Guid.Empty,
                MaintenanceType = Enum.TryParse<ToolMaintenanceType>(reader["MaintenanceType"]?.ToString(), out var mt) ? mt : ToolMaintenanceType.PREVENTIVE,
                Description = reader["Description"]?.ToString() ?? string.Empty,
                CostCents = reader["CostCents"] is DBNull ? 0 : Convert.ToInt64(reader["CostCents"]),
                Provider = reader["Provider"] is DBNull ? null : reader["Provider"]?.ToString(),
                StartDate = DateTime.TryParse(reader["StartDate"]?.ToString(), out var sd) ? sd : DateTime.Now,
                CompletionDate = reader["CompletionDate"] is DBNull || string.IsNullOrWhiteSpace(reader["CompletionDate"]?.ToString()) ? null : DateTime.TryParse(reader["CompletionDate"]?.ToString(), out var cd) ? cd : null,
                PerformedBy = reader["PerformedBy"] is DBNull ? null : reader["PerformedBy"]?.ToString(),
                Status = Enum.TryParse<MaintenanceStatus>(reader["Status"]?.ToString(), out var st) ? st : MaintenanceStatus.COMPLETED,
                Notes = reader["Notes"] is DBNull ? null : reader["Notes"]?.ToString(),
                CreatedAt = DateTime.TryParse(reader["CreatedAt"]?.ToString(), out var ca) ? ca : DateTime.Now
            };
        }
    }
}
