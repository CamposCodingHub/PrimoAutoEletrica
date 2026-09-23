import os
import re
import csv

queries_info = [
    {
        "id": 1,
        "file": "FinanceiroDatabaseService.cs",
        "line": 1112,
        "table": "MovimentacoesFinanceiras",
        "field": "Valor",
        "sql": "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data BETWEEN @dataInicio AND @dataFim AND Tipo = 'Receita'",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "FinanceiroDatabaseService.ObterResumoFinanceiroAsync()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 2,
        "file": "FinanceiroDatabaseService.cs",
        "line": 1126,
        "table": "MovimentacoesFinanceiras",
        "field": "Valor",
        "sql": "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data BETWEEN @dataInicio AND @dataFim AND Tipo = 'Despesa'",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "FinanceiroDatabaseService.ObterResumoFinanceiroAsync()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 3,
        "file": "FinanceiroDatabaseService.cs",
        "line": 1149,
        "table": "ContasReceber",
        "field": "Valor",
        "sql": "SELECT COALESCE(SUM(Valor), 0) FROM ContasReceber WHERE Status = 'Pendente'",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "FinanceiroDatabaseService.ObterResumoFinanceiroAsync()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 4,
        "file": "FinanceiroDatabaseService.cs",
        "line": 1159,
        "table": "ContasPagar",
        "field": "Valor",
        "sql": "SELECT COALESCE(SUM(Valor), 0) FROM ContasPagar WHERE Status = 'Pendente'",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "FinanceiroDatabaseService.ObterResumoFinanceiroAsync()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 5,
        "file": "FinanceiroDatabaseService.cs",
        "line": 1169,
        "table": "ContasReceber",
        "field": "Valor",
        "sql": "SELECT COALESCE(SUM(Valor), 0) FROM ContasReceber WHERE DataVencimento < @hoje AND Status = 'Pendente'",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "FinanceiroDatabaseService.ObterResumoFinanceiroAsync()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 6,
        "file": "FinanceiroDatabaseService.cs",
        "line": 1354,
        "table": "MovimentacoesFinanceiras",
        "field": "Valor",
        "sql": "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data BETWEEN @dataInicio AND @dataFim",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "FinanceiroDatabaseService.ObterMovimentacoesPorPeriodoAsync()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 7,
        "file": "FinanceiroDatabaseService.cs",
        "line": 1635,
        "table": "MovimentacoesFinanceiras",
        "field": "Valor",
        "sql": "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Tipo = 'Entrada' AND Data = @hoje",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "FinanceiroDatabaseService.ObterFluxoCaixaDiarioAsync()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 8,
        "file": "FinanceiroDatabaseService.cs",
        "line": 1653,
        "table": "MovimentacoesFinanceiras",
        "field": "Valor",
        "sql": "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Tipo = 'Saida' AND Data = @hoje",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "FinanceiroDatabaseService.ObterFluxoCaixaDiarioAsync()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 9,
        "file": "FuncionarioOperationalService.cs",
        "line": 475,
        "table": "Vendas",
        "field": "Total",
        "sql": "SELECT COALESCE(SUM(Total), 0) FROM Vendas WHERE Usuario = @usuario AND Status = 'Concluida'",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "FuncionarioOperationalService.CalcularComissoesAsync()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 10,
        "file": "FuncionarioOperationalService.cs",
        "line": 507,
        "table": "CaixaSessoes",
        "field": "TotalVendas",
        "sql": "SELECT COALESCE(SUM(TotalVendas), 0) FROM CaixaSessoes WHERE OperadorId = @operadorId",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "FuncionarioOperationalService.ObterDesempenhoOperadorAsync()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 11,
        "file": "RelatorioDatabaseService.cs",
        "line": 294,
        "table": "MovimentacoesFinanceiras",
        "field": "Valor",
        "sql": "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data BETWEEN @dataInicio AND @dataFim AND Tipo IN ('Entrada', 'Receita')",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "RelatorioDatabaseService.ObterReceitaTotal()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 12,
        "file": "RelatorioDatabaseService.cs",
        "line": 409,
        "table": "MovimentacoesFinanceiras",
        "field": "Valor",
        "sql": "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data BETWEEN @dataInicio AND @dataFim AND Tipo IN ('Saida', 'Despesa')",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "RelatorioDatabaseService.ObterDespesaTotal()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 13,
        "file": "DashboardViewModel.cs",
        "line": 345,
        "table": "ContasReceber",
        "field": "Valor",
        "sql": "SELECT COALESCE(SUM(Valor), 0) FROM ContasReceber WHERE DataVencimento = @hoje AND Status = 'Pendente'",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "DashboardViewModel.CarregarCardsFinanceirosAsync()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 14,
        "file": "DashboardViewModel.cs",
        "line": 447,
        "table": "MovimentacoesFinanceiras",
        "field": "Valor",
        "sql": "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data >= @inicioMes AND Tipo = 'Receita'",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "DashboardViewModel.CarregarFaturamentoMensalAsync()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    },
    {
        "id": 15,
        "file": "DashboardViewModel.cs",
        "line": 456,
        "table": "MovimentacoesFinanceiras",
        "field": "Valor",
        "sql": "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data >= @inicioMes AND Tipo = 'Despesa'",
        "stored_unit": "INTEGER cents",
        "returned_unit": "long (cents)",
        "consumer": "DashboardViewModel.CarregarDespesasMensaisAsync()",
        "conversion": "Convert.ToInt64(scalar) / 100m"
    }
]

out_csv = r"Docs/audit/2026-09-20/P2_3_MONEY_SQL_AGGREGATES.csv"
with open(out_csv, "w", newline="", encoding="utf-8") as f:
    writer = csv.DictWriter(f, fieldnames=[
        "ID", "SQL_ATUAL", "CAMPO", "TABELA", "UNIDADE_ARMAZENADA", "UNIDADE_RETORNADA", "CONSUMIDOR", "CONVERSAO_NECESSARIA", "ARQUIVO", "LINHA"
    ])
    writer.writeheader()
    for q in queries_info:
        writer.writerow({
            "ID": q["id"],
            "SQL_ATUAL": q["sql"],
            "CAMPO": q["field"],
            "TABELA": q["table"],
            "UNIDADE_ARMAZENADA": q["stored_unit"],
            "UNIDADE_RETORNADA": q["returned_unit"],
            "CONSUMIDOR": q["consumer"],
            "CONVERSAO_NECESSARIA": q["conversion"],
            "ARQUIVO": q["file"],
            "LINHA": q["line"]
        })

print(f"Generated {out_csv} with 15 queries successfully.")
