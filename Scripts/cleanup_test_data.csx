#r "nuget: Microsoft.Data.Sqlite, 9.0.0"

using Microsoft.Data.Sqlite;
using System;
using System.IO;

// =====================================================================
// Script de limpeza de dados de teste - PrimoAutoEletrica
// Preserva: admin (Id=1), cliente Oliveira Transportes, veiculos reais,
//           orcamentos reais, produtos, configuracoes, perfis
// =====================================================================

var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimoAutoEletrica", "primoauto.db");
var backupPath = dbPath + ".bak." + DateTime.Now.ToString("yyyyMMdd_HHmmss");

Console.WriteLine("=== LIMPEZA DE DADOS DE TESTE ===");
Console.WriteLine("Banco: " + dbPath);

// 1. Backup
Console.WriteLine("\n[1] Criando backup...");
File.Copy(dbPath, backupPath, overwrite: true);
Console.WriteLine("    Backup em: " + backupPath);

// 2. Conectar
var connStr = "Data Source=" + dbPath;
var conn = new SqliteConnection(connStr);
conn.Open();

// IDs a preservar
var clienteRealId = "61c59962-a312-4170-aa53-9835e8066e38"; // Oliveira Transportes
var adminId = 1; // Douglas Ciro de Campos

void ExecSql(string sql, string descricao)
{
    try
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var rows = cmd.ExecuteNonQuery();
        Console.WriteLine("    " + descricao + ": " + rows + " registros removidos");
    }
    catch (Exception ex)
    {
        Console.WriteLine("    " + descricao + ": ERRO - " + ex.Message);
    }
}

int CountRows(string table)
{
    try
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM [" + table + "]";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
    catch { return -1; }
}

// 3. Limpar tabelas dependentes primeiro (ordem de FK)
Console.WriteLine("\n[2] Removendo dados de teste...");

// --- Ordens de Servico e dependencias ---
ExecSql("DELETE FROM OrdemServicoEventos WHERE OrdemServicoId IN (SELECT Id FROM OrdensServico WHERE ClienteId != '" + clienteRealId + "')",
    "OrdemServicoEventos de teste");
ExecSql("DELETE FROM OrdemServicoItens WHERE OrdemServicoId IN (SELECT Id FROM OrdensServico WHERE ClienteId != '" + clienteRealId + "')",
    "OrdemServicoItens de teste");
ExecSql("DELETE FROM OrdensServico WHERE ClienteId != '" + clienteRealId + "'",
    "OrdensServico de teste");

// --- Orcamentos e dependencias ---
ExecSql("DELETE FROM OrcamentoItens WHERE OrcamentoId IN (SELECT Id FROM Orcamentos WHERE ClienteId != '" + clienteRealId + "')",
    "OrcamentoItens de teste");
ExecSql("DELETE FROM Orcamentos WHERE ClienteId != '" + clienteRealId + "'",
    "Orcamentos de teste");

// --- Vendas e dependencias ---
ExecSql("DELETE FROM VendaItens WHERE VendaId IN (SELECT Id FROM Vendas WHERE ClienteId != '" + clienteRealId + "')",
    "VendaItens de teste");
ExecSql("DELETE FROM Vendas WHERE ClienteId != '" + clienteRealId + "'",
    "Vendas de teste");

// --- Financeiro ---
ExecSql("DELETE FROM MovimentacoesFinanceiras WHERE Descricao LIKE '%Smoke%' OR Descricao LIKE '%Workflow%' OR Descricao LIKE '%smoke%' OR Descricao LIKE '%workflow%' OR Descricao LIKE '%teste%'",
    "MovimentacoesFinanceiras de teste (por nome)");
// Deletar movimentacoes orfas que referenciam clientes de teste
ExecSql("DELETE FROM MovimentacoesFinanceiras WHERE ClienteId IS NOT NULL AND ClienteId != '' AND ClienteId != '" + clienteRealId + "' AND ClienteId NOT IN (SELECT CAST(Id AS TEXT) FROM Clientes WHERE Id = '" + clienteRealId + "')",
    "MovimentacoesFinanceiras orfas");

// --- Caixa ---
ExecSql("DELETE FROM MovimentacoesCaixa", "MovimentacoesCaixa (todas - geradas por teste)");
ExecSql("DELETE FROM CaixaSessoes", "CaixaSessoes (todas - geradas por teste)");

// --- Contas ---
ExecSql("DELETE FROM ContasPagar WHERE Descricao LIKE '%Smoke%' OR Descricao LIKE '%Workflow%' OR Descricao LIKE '%teste%'",
    "ContasPagar de teste");
ExecSql("DELETE FROM ContasReceber WHERE Descricao LIKE '%Smoke%' OR Descricao LIKE '%Workflow%' OR Descricao LIKE '%teste%'",
    "ContasReceber de teste");

// --- Agendamentos ---
ExecSql("DELETE FROM AgendamentoIntegracoes", "AgendamentoIntegracoes");
ExecSql("DELETE FROM AgendamentoProdutos", "AgendamentoProdutos");
ExecSql("DELETE FROM AgendamentoServicos", "AgendamentoServicos");
ExecSql("DELETE FROM AgendamentoTimeline", "AgendamentoTimeline");
ExecSql("DELETE FROM Agendamentos", "Agendamentos");
ExecSql("DELETE FROM AlertasAgendamento", "AlertasAgendamento");

// --- Fiscal ---
ExecSql("DELETE FROM FiscalEvents", "FiscalEvents");
ExecSql("DELETE FROM FiscalDocuments", "FiscalDocuments");
ExecSql("DELETE FROM FiscalOperations", "FiscalOperations");

// --- Importacoes NF-e ---
ExecSql("DELETE FROM ImportacoesItens", "ImportacoesItens");
ExecSql("DELETE FROM ImportacoesNFeExclusoes", "ImportacoesNFeExclusoes");
ExecSql("DELETE FROM ImportacoesNFeRollbacks", "ImportacoesNFeRollbacks");
ExecSql("DELETE FROM ImportacoesNFe", "ImportacoesNFe");

// --- Catalogo ---
ExecSql("DELETE FROM CatalogoImportacaoErros", "CatalogoImportacaoErros");
ExecSql("DELETE FROM CatalogoImportacoes", "CatalogoImportacoes");
ExecSql("DELETE FROM CatalogoPecas", "CatalogoPecas");

// --- Veiculos de teste ---
ExecSql("DELETE FROM Veiculos WHERE ClienteId != '" + clienteRealId + "'",
    "Veiculos de teste");

// --- Clientes de teste ---
ExecSql("DELETE FROM Clientes WHERE Id != '" + clienteRealId + "'",
    "Clientes de teste");

// --- Funcionarios de teste (manter apenas admin Id=1) ---
ExecSql("DELETE FROM Funcionarios WHERE Id != " + adminId,
    "Funcionarios de teste");

// --- Logs e auditoria ---
ExecSql("DELETE FROM AuditLogs", "AuditLogs");
ExecSql("DELETE FROM Auditoria", "Auditoria");
ExecSql("DELETE FROM Timeline", "Timeline");
ExecSql("DELETE FROM Alertas", "Alertas");
ExecSql("DELETE FROM LoginTentativasSeguranca", "LoginTentativasSeguranca");
ExecSql("DELETE FROM UserSessions", "UserSessions");
ExecSql("DELETE FROM RecordLocks", "RecordLocks");
ExecSql("DELETE FROM RegistroBloqueios", "RegistroBloqueios");
ExecSql("DELETE FROM Relatorios", "Relatorios");
ExecSql("DELETE FROM Metas", "Metas");
ExecSql("DELETE FROM MetasFinanceiras", "MetasFinanceiras");

// --- Fornecedores de teste ---
ExecSql("DELETE FROM ContatosFornecedor", "ContatosFornecedor");
ExecSql("DELETE FROM ProdutoFornecedores", "ProdutoFornecedores");
ExecSql("DELETE FROM Fornecedores WHERE RazaoSocial LIKE '%Smoke%' OR RazaoSocial LIKE '%Workflow%' OR RazaoSocial LIKE '%Teste%'",
    "Fornecedores de teste");

// --- Database Backups registry (meta apenas, nao os arquivos) ---
ExecSql("DELETE FROM DatabaseBackups", "DatabaseBackups (registros de backup)");

// 4. VACUUM para compactar
Console.WriteLine("\n[3] Compactando banco...");
var vacCmd = conn.CreateCommand();
vacCmd.CommandText = "VACUUM";
vacCmd.ExecuteNonQuery();
Console.WriteLine("    VACUUM concluido.");

// 5. Verificacao final
Console.WriteLine("\n[4] Verificacao final:");
string[] tablesCheck = new string[] { "Clientes", "Veiculos", "Funcionarios", "Produtos", "OrdensServico", "OrdemServicoItens", "Orcamentos", "OrcamentoItens", "Fornecedores", "MovimentacoesFinanceiras", "Vendas", "VendaItens", "AuditLogs", "Agendamentos" };
foreach (var t in tablesCheck)
{
    Console.WriteLine("    " + t + ": " + CountRows(t) + " registros");
}

conn.Close();

Console.WriteLine("\n=== LIMPEZA CONCLUIDA COM SUCESSO ===");
Console.WriteLine("Backup disponivel em: " + backupPath);
Console.WriteLine("Se algo deu errado, restaure com:");
Console.WriteLine("  Copy-Item '" + backupPath + "' '" + dbPath + "' -Force");
