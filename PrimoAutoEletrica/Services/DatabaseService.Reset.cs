using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public partial class DatabaseService
    {
        public async Task ZerarSistemaAsync(bool limparClientes, bool limparVeiculos, bool limparEstoque)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var isSqlServer = IsSqlServerConnection(connection);

                if (limparClientes || limparVeiculos || limparEstoque)
                {
                    // Limpar Históricos que dependem dessas tabelas primárias
                    await ExecuteDeleteAsync(connection, transaction, "OrdemServicoEventos");
                    await ExecuteDeleteAsync(connection, transaction, "OrdemServicoItens");
                    await ExecuteDeleteAsync(connection, transaction, "OrdensServico");
                    
                    await ExecuteDeleteAsync(connection, transaction, "OrcamentoItens");
                    await ExecuteDeleteAsync(connection, transaction, "Orcamentos");

                    await ExecuteDeleteAsync(connection, transaction, "AgendamentoTimeline");
                    await ExecuteDeleteAsync(connection, transaction, "AgendamentoProdutos");
                    await ExecuteDeleteAsync(connection, transaction, "AgendamentoServicos");
                    await ExecuteDeleteAsync(connection, transaction, "Agendamentos");
                }

                if (limparVeiculos)
                {
                    await ExecuteDeleteAsync(connection, transaction, "Veiculos");
                }

                if (limparClientes)
                {
                    await ExecuteDeleteAsync(connection, transaction, "Clientes");
                }

                if (limparEstoque)
                {
                    await ExecuteDeleteAsync(connection, transaction, "ProdutoFornecedores");
                    await ExecuteDeleteAsync(connection, transaction, "Produtos");
                }

                var logCommand = connection.CreateCommand();
                logCommand.Transaction = transaction;
                logCommand.CommandText = @"
                    INSERT INTO AuditLogs (Id, DataHora, Categoria, Acao, Detalhes, Severidade, Sucesso)
                    VALUES (@Id, @DataHora, 'Sistema', 'Wipe', @Detalhes, 'Alto', 1)";
                
                logCommand.Parameters.Add(CreateParameter(logCommand, "@Id", Guid.NewGuid().ToString()));
                logCommand.Parameters.Add(CreateParameter(logCommand, "@DataHora", DateTime.Now));
                logCommand.Parameters.Add(CreateParameter(logCommand, "@Detalhes", $"Sistema zerado. Clientes: {limparClientes}, Veículos: {limparVeiculos}, Estoque: {limparEstoque}"));
                
                await logCommand.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Erro ao zerar o sistema: {ex.Message}", ex);
                throw;
            }
        }

        private async Task ExecuteDeleteAsync(DbConnection connection, DbTransaction transaction, string tabela)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"DELETE FROM {tabela}";
            await command.ExecuteNonQueryAsync();
        }
        
        private DbParameter CreateParameter(DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            return parameter;
        }
    }
}
