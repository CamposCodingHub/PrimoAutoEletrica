using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Serviço para exportação de dados contábeis em formatos compatíveis com sistemas contábeis
    /// </summary>
    public class ContabilExportService
    {
        private readonly LoggerService _logger;
        private readonly DatabaseService _database;

        public ContabilExportService(LoggerService logger, DatabaseService database)
        {
            _logger = logger;
            _database = database;
        }

        /// <summary>
        /// Exporta vendas para formato CSV compatível com sistemas contábeis
        /// </summary>
        public async Task<string> ExportarVendasCsvAsync(DateTime dataInicio, DateTime dataFim, string caminhoArquivo)
        {
            try
            {
                var vendas = await ObterVendasPeriodoAsync(dataInicio, dataFim);
                var csv = GerarCsvVendas(vendas);
                
                await File.WriteAllTextAsync(caminhoArquivo, csv, Encoding.UTF8);
                _logger.LogInfo($"Exportação de vendas CSV concluída: {caminhoArquivo}");
                
                return caminhoArquivo;
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao exportar vendas para CSV", ex);
                throw;
            }
        }

        /// <summary>
        /// Exporta contas a pagar para formato CSV
        /// </summary>
        public async Task<string> ExportarContasPagarCsvAsync(DateTime dataInicio, DateTime dataFim, string caminhoArquivo)
        {
            try
            {
                var contas = await ObterContasPagarPeriodoAsync(dataInicio, dataFim);
                var csv = GerarCsvContasPagar(contas);
                
                await File.WriteAllTextAsync(caminhoArquivo, csv, Encoding.UTF8);
                _logger.LogInfo($"Exportação de contas a pagar CSV concluída: {caminhoArquivo}");
                
                return caminhoArquivo;
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao exportar contas a pagar para CSV", ex);
                throw;
            }
        }

        /// <summary>
        /// Exporta contas a receber para formato CSV
        /// </summary>
        public async Task<string> ExportarContasReceberCsvAsync(DateTime dataInicio, DateTime dataFim, string caminhoArquivo)
        {
            try
            {
                var contas = await ObterContasReceberPeriodoAsync(dataInicio, dataFim);
                var csv = GerarCsvContasReceber(contas);
                
                await File.WriteAllTextAsync(caminhoArquivo, csv, Encoding.UTF8);
                _logger.LogInfo($"Exportação de contas a receber CSV concluída: {caminhoArquivo}");
                
                return caminhoArquivo;
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao exportar contas a receber para CSV", ex);
                throw;
            }
        }

        /// <summary>
        /// Exporta dados para formato OFX (Open Financial Exchange)
        /// </summary>
        public async Task<string> ExportarOfxAsync(DateTime dataInicio, DateTime dataFim, string caminhoArquivo)
        {
            try
            {
                var vendas = await ObterVendasPeriodoAsync(dataInicio, dataFim);
                var ofx = GerarOfx(vendas, dataInicio, dataFim);
                
                await File.WriteAllTextAsync(caminhoArquivo, ofx, Encoding.UTF8);
                _logger.LogInfo($"Exportação OFX concluída: {caminhoArquivo}");
                
                return caminhoArquivo;
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao exportar para OFX", ex);
                throw;
            }
        }

        /// <summary>
        /// Exporta dados para formato SPED (Sistema Público de Escrituração Digital)
        /// </summary>
        public async Task<string> ExportarSpedAsync(DateTime dataInicio, DateTime dataFim, string caminhoArquivo)
        {
            try
            {
                // TODO: Implementar formato SPED completo (SPED Fiscal, SPED Contábil, etc.)
                // Este é um esboço simplificado
                
                var vendas = await ObterVendasPeriodoAsync(dataInicio, dataFim);
                var sped = GerarSpedSimplificado(vendas, dataInicio, dataFim);
                
                await File.WriteAllTextAsync(caminhoArquivo, sped, Encoding.UTF8);
                _logger.LogInfo($"Exportação SPED concluída: {caminhoArquivo}");
                
                return caminhoArquivo;
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao exportar para SPED", ex);
                throw;
            }
        }

        private async Task<List<VendaExport>> ObterVendasPeriodoAsync(DateTime dataInicio, DateTime dataFim)
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var sql = @"
                SELECT 
                    Id, Data, ClienteId, ClienteNome, ClienteCpfCnpj, 
                    ValorTotal, FormaPagamento, Status
                FROM Vendas 
                WHERE Data >= @DataInicio AND Data <= @DataFim
                ORDER BY Data";

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@DataInicio", dataInicio);
            command.Parameters.AddWithValue("@DataFim", dataFim);

            var vendas = new List<VendaExport>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                vendas.Add(new VendaExport
                {
                    Id = reader.GetGuid(0),
                    Data = reader.GetDateTime(1),
                    ClienteId = reader.GetGuid(2),
                    ClienteNome = reader.GetString(3),
                    ClienteCpfCnpj = reader.IsDBNull(4) ? null : reader.GetString(4),
                    ValorTotal = reader.GetDecimal(5),
                    FormaPagamento = reader.GetString(6),
                    Status = reader.GetString(7)
                });
            }

            return vendas;
        }

        private async Task<List<ContaPagarExport>> ObterContasPagarPeriodoAsync(DateTime dataInicio, DateTime dataFim)
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var sql = @"
                SELECT 
                    Id, Descricao, Fornecedor, Valor, DataVencimento, 
                    DataPagamento, Status, Categoria
                FROM ContasPagar 
                WHERE DataVencimento >= @DataInicio AND DataVencimento <= @DataFim
                ORDER BY DataVencimento";

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@DataInicio", dataInicio);
            command.Parameters.AddWithValue("@DataFim", dataFim);

            var contas = new List<ContaPagarExport>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                contas.Add(new ContaPagarExport
                {
                    Id = reader.GetGuid(0),
                    Descricao = reader.GetString(1),
                    Fornecedor = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Valor = reader.GetDecimal(3),
                    DataVencimento = reader.GetDateTime(4),
                    DataPagamento = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    Status = reader.GetString(6),
                    Categoria = reader.IsDBNull(7) ? null : reader.GetString(7)
                });
            }

            return contas;
        }

        private async Task<List<ContaReceberExport>> ObterContasReceberPeriodoAsync(DateTime dataInicio, DateTime dataFim)
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var sql = @"
                SELECT 
                    Id, Descricao, Cliente, Valor, DataVencimento, 
                    DataRecebimento, Status, Categoria
                FROM ContasReceber 
                WHERE DataVencimento >= @DataInicio AND DataVencimento <= @DataFim
                ORDER BY DataVencimento";

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@DataInicio", dataInicio);
            command.Parameters.AddWithValue("@DataFim", dataFim);

            var contas = new List<ContaReceberExport>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                contas.Add(new ContaReceberExport
                {
                    Id = reader.GetGuid(0),
                    Descricao = reader.GetString(1),
                    Cliente = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Valor = reader.GetDecimal(3),
                    DataVencimento = reader.GetDateTime(4),
                    DataRecebimento = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    Status = reader.GetString(6),
                    Categoria = reader.IsDBNull(7) ? null : reader.GetString(7)
                });
            }

            return contas;
        }

        private string GerarCsvVendas(List<VendaExport> vendas)
        {
            var sb = new StringBuilder();
            
            // Cabeçalho
            sb.AppendLine("ID;Data;ClienteID;ClienteNome;ClienteCPF/CNPJ;ValorTotal;FormaPagamento;Status");
            
            // Linhas
            foreach (var venda in vendas)
            {
                sb.AppendLine($"{venda.Id};{venda.Data:yyyy-MM-dd};{venda.ClienteId};{venda.ClienteNome};{venda.ClienteCpfCnpj ?? ""};{venda.ValorTotal};{venda.FormaPagamento};{venda.Status}");
            }
            
            return sb.ToString();
        }

        private string GerarCsvContasPagar(List<ContaPagarExport> contas)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("ID;Descricao;Fornecedor;Valor;DataVencimento;DataPagamento;Status;Categoria");
            
            foreach (var conta in contas)
            {
                sb.AppendLine($"{conta.Id};{conta.Descricao};{conta.Fornecedor ?? ""};{conta.Valor};{conta.DataVencimento:yyyy-MM-dd};{conta.DataPagamento?.ToString("yyyy-MM-dd") ?? ""};{conta.Status};{conta.Categoria ?? ""}");
            }
            
            return sb.ToString();
        }

        private string GerarCsvContasReceber(List<ContaReceberExport> contas)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("ID;Descricao;Cliente;Valor;DataVencimento;DataRecebimento;Status;Categoria");
            
            foreach (var conta in contas)
            {
                sb.AppendLine($"{conta.Id};{conta.Descricao};{conta.Cliente ?? ""};{conta.Valor};{conta.DataVencimento:yyyy-MM-dd};{conta.DataRecebimento?.ToString("yyyy-MM-dd") ?? ""};{conta.Status};{conta.Categoria ?? ""}");
            }
            
            return sb.ToString();
        }

        private string GerarOfx(List<VendaExport> vendas, DateTime dataInicio, DateTime dataFim)
        {
            var sb = new StringBuilder();
            
            // Cabeçalho OFX
            sb.AppendLine("OFXHEADER:100");
            sb.AppendLine("DATA:OFXHEADER");
            sb.AppendLine("VERSION:102");
            sb.AppendLine("SECURITY:NONE");
            sb.AppendLine("ENCODING:UTF-8");
            sb.AppendLine("CHARSET:1252");
            sb.AppendLine("COMPRESSION:NONE");
            sb.AppendLine("OLDFILEUID:NONE");
            sb.AppendLine("NEWFILEUID:NONE");
            sb.AppendLine("");
            sb.AppendLine("SIGNONMSGSRSV1");
            sb.AppendLine("SONRS");
            sb.AppendLine("STATUS");
            sb.AppendLine("CODE:0");
            sb.AppendLine("SEVERITY:INFO");
            sb.AppendLine("MESSAGE:Sucesso");
            sb.AppendLine("");
            sb.AppendLine("BANKMSGSRSV1");
            sb.AppendLine("STMTTRNRS");
            sb.AppendLine("STATUS");
            sb.AppendLine("CODE:0");
            sb.AppendLine("SEVERITY:INFO");
            sb.AppendLine("");
            sb.AppendLine("STMTRS");
            sb.AppendLine($"DTSTART:{dataInicio:yyyyMMdd}");
            sb.AppendLine($"DTEND:{dataFim:yyyyMMdd}");
            sb.AppendLine("BANKTRANLIST");
            
            // Transações
            foreach (var venda in vendas)
            {
                sb.AppendLine("STMTTRN");
                sb.AppendLine($"TRNTYPE:DEBIT");
                sb.AppendLine($"DTPOSTED:{venda.Data:yyyyMMdd}");
                sb.AppendLine($"TRNAMT:{venda.ValorTotal}");
                sb.AppendLine($"FITID:{venda.Id}");
                sb.AppendLine($"NAME:{venda.ClienteNome}");
                sb.AppendLine($"MEMO:{venda.FormaPagamento}");
                sb.AppendLine("");
            }
            
            return sb.ToString();
        }

        private string GerarSpedSimplificado(List<VendaExport> vendas, DateTime dataInicio, DateTime dataFim)
        {
            var sb = new StringBuilder();
            
            // Cabeçalho SPED simplificado
            sb.AppendLine("|0000|PRIMO AUTO ELETRICA LTDA|");
            sb.AppendLine($"|0001|0|"); // Indicador de atividade
            sb.AppendLine($"|0002|{DateTime.Now:yyyyMMdd}|"); // Data início
            sb.AppendLine($"|0003|{DateTime.Now:yyyyMMdd}|"); // Data final
            sb.AppendLine("");
            
            // Registro de vendas
            foreach (var venda in vendas)
            {
                sb.AppendLine($"|C100|{venda.Data:yyyyMMdd}|{venda.Id}|{venda.ValorTotal}|{venda.ClienteCpfCnpj ?? ""}|");
            }
            
            return sb.ToString();
        }
    }

    /// <summary>
    /// Classe auxiliar para exportação de vendas
    /// </summary>
    public class VendaExport
    {
        public Guid Id { get; set; }
        public DateTime Data { get; set; }
        public Guid ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string? ClienteCpfCnpj { get; set; }
        public decimal ValorTotal { get; set; }
        public string FormaPagamento { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Classe auxiliar para exportação de contas a pagar
    /// </summary>
    public class ContaPagarExport
    {
        public Guid Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string? Fornecedor { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataVencimento { get; set; }
        public DateTime? DataPagamento { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Categoria { get; set; }
    }

    /// <summary>
    /// Classe auxiliar para exportação de contas a receber
    /// </summary>
    public class ContaReceberExport
    {
        public Guid Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string? Cliente { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataVencimento { get; set; }
        public DateTime? DataRecebimento { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Categoria { get; set; }
    }
}