import os
import re
import csv

MONEY_FIELDS = [
    # Table, Column, Category, PreviousType, CentsV1Type, Nullable, Default, Repo, Service, WritePath, ReadPath, AggPath, UIPath, Tests
    ("AgendamentoProdutos", "PrecoUnitario", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "AgendamentoProdutosRepository", "AgendamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(PrecoTotal)", "AgendamentosWindow.xaml", "AgendamentoTests"),
    ("AgendamentoProdutos", "PrecoTotal", "MONEY_DERIVED", "REAL", "INTEGER", "SIM", "NULL", "AgendamentoProdutosRepository", "AgendamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(PrecoTotal)", "AgendamentosWindow.xaml", "AgendamentoTests"),
    ("AgendamentoServicos", "Valor", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "AgendamentoServicosRepository", "AgendamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(Valor)", "AgendamentosWindow.xaml", "AgendamentoTests"),
    ("Agendamentos", "ClienteTotalGasto", "MONEY_DERIVED", "REAL", "INTEGER", "SIM", "NULL", "AgendamentosRepository", "AgendamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(ClienteTotalGasto)", "AgendamentosWindow.xaml", "AgendamentoTests"),
    ("Agendamentos", "ValorEstimado", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "AgendamentosRepository", "AgendamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(ValorEstimado)", "AgendamentosWindow.xaml", "AgendamentoTests"),
    ("Agendamentos", "ValorReal", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "AgendamentosRepository", "AgendamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(ValorReal)", "AgendamentosWindow.xaml", "AgendamentoTests"),
    ("Agendamentos", "ValorPago", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "AgendamentosRepository", "AgendamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(ValorPago)", "AgendamentosWindow.xaml", "AgendamentoTests"),
    ("Agendamentos", "ValorProdutos", "MONEY_DERIVED", "REAL", "INTEGER", "SIM", "NULL", "AgendamentosRepository", "AgendamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(ValorProdutos)", "AgendamentosWindow.xaml", "AgendamentoTests"),
    ("Agendamentos", "ValorServicos", "MONEY_DERIVED", "REAL", "INTEGER", "SIM", "NULL", "AgendamentosRepository", "AgendamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(ValorServicos)", "AgendamentosWindow.xaml", "AgendamentoTests"),
    ("CaixaSessoes", "ValorAbertura", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "CaixaSessoesRepository", "CaixaService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorAbertura)", "CaixaWindow.xaml", "CaixaTests"),
    ("CaixaSessoes", "ValorEsperado", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "CaixaSessoesRepository", "CaixaService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorEsperado)", "CaixaWindow.xaml", "CaixaTests"),
    ("CaixaSessoes", "ValorInformadoFechamento", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "CaixaSessoesRepository", "CaixaService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(ValorInformadoFechamento)", "CaixaWindow.xaml", "CaixaTests"),
    ("CaixaSessoes", "TotalVendas", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "CaixaSessoesRepository", "CaixaService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(TotalVendas)", "CaixaWindow.xaml", "CaixaTests"),
    ("CaixaSessoes", "TotalSangrias", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "CaixaSessoesRepository", "CaixaService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(TotalSangrias)", "CaixaWindow.xaml", "CaixaTests"),
    ("CaixaSessoes", "TotalSuprimentos", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "CaixaSessoesRepository", "CaixaService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(TotalSuprimentos)", "CaixaWindow.xaml", "CaixaTests"),
    ("Clientes", "TotalGasto", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "ClienteRepository", "DatabaseService.Clientes", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(TotalGasto)", "ClientesWindow.xaml", "ClienteRepositoryTests"),
    ("ContasPagar", "Valor", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "ContasPagarRepository", "FinanceiroDatabaseService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(Valor)", "FinanceiroWindow.xaml", "FinanceiroTests"),
    ("ContasReceber", "Valor", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "ContasReceberRepository", "FinanceiroDatabaseService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(Valor)", "FinanceiroWindow.xaml", "FinanceiroTests"),
    ("Fornecedores", "PedidoMinimo", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "FornecedorRepository", "FornecedorOperationalService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "MIN(PedidoMinimo)", "FornecedoresWindow.xaml", "FornecedorRepositoryTests"),
    ("Fornecedores", "TotalCompras", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "FornecedorRepository", "FornecedorOperationalService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(TotalCompras)", "FornecedoresWindow.xaml", "FornecedorRepositoryTests"),
    ("Funcionarios", "Salario", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "FuncionarioRepository", "DatabaseService.Funcionarios", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(Salario)", "FuncionariosWindow.xaml", "FuncionarioRepositoryTests"),
    ("ImportacoesItens", "ValorUnitario", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "ImportacaoRepository", "FornecedorOperationalService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorTotal)", "NFeImportWindow.xaml", "NFeImportTests"),
    ("ImportacoesItens", "ValorTotal", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "ImportacaoRepository", "FornecedorOperationalService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorTotal)", "NFeImportWindow.xaml", "NFeImportTests"),
    ("ImportacoesItens", "PrecoVendaSugerido", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "ImportacaoRepository", "FornecedorOperationalService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "AVG(PrecoVendaSugerido)", "NFeImportWindow.xaml", "NFeImportTests"),
    ("ImportacoesNFe", "ValorTotal", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "ImportacaoRepository", "FornecedorOperationalService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorTotal)", "NFeImportWindow.xaml", "NFeImportTests"),
    ("ImportacoesNFe", "ValorProdutos", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "ImportacaoRepository", "FornecedorOperationalService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorProdutos)", "NFeImportWindow.xaml", "NFeImportTests"),
    ("ImportacoesNFeExclusoes", "ValorTotal", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "ImportacaoRepository", "FornecedorOperationalService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorTotal)", "NFeImportWindow.xaml", "NFeImportTests"),
    ("Metas", "MetaValor", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "MetasRepository", "RelatorioDatabaseService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(MetaValor)", "DashboardWindow.xaml", "RelatorioTests"),
    ("Metas", "ValorAtual", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "MetasRepository", "RelatorioDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(ValorAtual)", "DashboardWindow.xaml", "RelatorioTests"),
    ("MetasFinanceiras", "ValorMeta", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "MetasFinanceirasRepository", "FinanceiroDatabaseService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorMeta)", "DashboardWindow.xaml", "FinanceiroTests"),
    ("MetasFinanceiras", "ValorAtual", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "MetasFinanceirasRepository", "FinanceiroDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(ValorAtual)", "DashboardWindow.xaml", "FinanceiroTests"),
    ("MovimentacoesCaixa", "ValorMovimento", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "MovimentacoesCaixaRepository", "CaixaService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorMovimento)", "CaixaWindow.xaml", "CaixaTests"),
    ("MovimentacoesCaixa", "ValorInicial", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "MovimentacoesCaixaRepository", "CaixaService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorInicial)", "CaixaWindow.xaml", "CaixaTests"),
    ("MovimentacoesCaixa", "ValorFinal", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "MovimentacoesCaixaRepository", "CaixaService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorFinal)", "CaixaWindow.xaml", "CaixaTests"),
    ("MovimentacoesCaixa", "Sangrias", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "MovimentacoesCaixaRepository", "CaixaService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(Sangrias)", "CaixaWindow.xaml", "CaixaTests"),
    ("MovimentacoesCaixa", "Suprimentos", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "MovimentacoesCaixaRepository", "CaixaService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(Suprimentos)", "CaixaWindow.xaml", "CaixaTests"),
    ("MovimentacoesCaixa", "Diferenca", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "MovimentacoesCaixaRepository", "CaixaService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(Diferenca)", "CaixaWindow.xaml", "CaixaTests"),
    ("MovimentacoesFinanceiras", "Valor", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "MovimentacoesFinanceirasRepository", "FinanceiroDatabaseService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(Valor)", "FinanceiroWindow.xaml", "FinanceiroTests"),
    ("OrcamentoItens", "PrecoUnitario", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "OrcamentoItensRepository", "OrcamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(Subtotal)", "OrcamentosWindow.xaml", "OrcamentoTests"),
    ("OrcamentoItens", "PrecoCusto", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "OrcamentoItensRepository", "OrcamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(PrecoCusto)", "OrcamentosWindow.xaml", "OrcamentoTests"),
    ("OrcamentoItens", "Desconto", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "OrcamentoItensRepository", "OrcamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(Desconto)", "OrcamentosWindow.xaml", "OrcamentoTests"),
    ("OrcamentoItens", "Subtotal", "MONEY_DERIVED", "REAL", "INTEGER", "SIM", "NULL", "OrcamentoItensRepository", "OrcamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(Subtotal)", "OrcamentosWindow.xaml", "OrcamentoTests"),
    ("OrcamentoItens", "LucroEstimado", "MONEY_DERIVED", "REAL", "INTEGER", "SIM", "NULL", "OrcamentoItensRepository", "OrcamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(LucroEstimado)", "OrcamentosWindow.xaml", "OrcamentoTests"),
    ("Orcamentos", "Subtotal", "MONEY_DERIVED", "REAL", "INTEGER", "SIM", "NULL", "OrcamentosRepository", "OrcamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(Subtotal)", "OrcamentosWindow.xaml", "OrcamentoTests"),
    ("Orcamentos", "Desconto", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "OrcamentosRepository", "OrcamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(Desconto)", "OrcamentosWindow.xaml", "OrcamentoTests"),
    ("Orcamentos", "Acrescimo", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "OrcamentosRepository", "OrcamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(Acrescimo)", "OrcamentosWindow.xaml", "OrcamentoTests"),
    ("Orcamentos", "Total", "MONEY_DERIVED", "REAL", "INTEGER", "SIM", "NULL", "OrcamentosRepository", "OrcamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(Total)", "OrcamentosWindow.xaml", "OrcamentoTests"),
    ("Orcamentos", "LucroEstimado", "MONEY_DERIVED", "REAL", "INTEGER", "SIM", "NULL", "OrcamentosRepository", "OrcamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(LucroEstimado)", "OrcamentosWindow.xaml", "OrcamentoTests"),
    ("Orcamentos", "ComissaoVendedor", "MONEY", "REAL", "INTEGER", "SIM", "NULL", "OrcamentosRepository", "OrcamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(ComissaoVendedor)", "OrcamentosWindow.xaml", "OrcamentoTests"),
    ("Orcamentos", "ImpostosEstimados", "MONEY_DERIVED", "REAL", "INTEGER", "SIM", "NULL", "OrcamentosRepository", "OrcamentoDatabaseService", "MoneyIO.GravarMoedaNullable", "MoneyIO.LerMoedaNullable", "SUM(ImpostosEstimados)", "OrcamentosWindow.xaml", "OrcamentoTests"),
    ("OrdemServicoItens", "ValorUnitario", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "OrdemServicoItensRepository", "DatabaseService.OrdensServico", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorUnitario)", "OrdensServicoWindow.xaml", "OrdemServicoRepositoryTests"),
    ("OrdemServicoItens", "CustoUnitario", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "OrdemServicoItensRepository", "DatabaseService.OrdensServico", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(CustoUnitario)", "OrdensServicoWindow.xaml", "OrdemServicoRepositoryTests"),
    ("OrdensServico", "ValorMaoObra", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "OrdensServicoRepository", "DatabaseService.OrdensServico", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorMaoObra)", "OrdensServicoWindow.xaml", "OrdemServicoRepositoryTests"),
    ("OrdensServico", "Desconto", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "OrdensServicoRepository", "DatabaseService.OrdensServico", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(Desconto)", "OrdensServicoWindow.xaml", "OrdemServicoRepositoryTests"),
    ("ProdutoFornecedores", "PrecoUltimaCompra", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "ProdutoFornecedoresRepository", "FornecedorOperationalService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "AVG(PrecoUltimaCompra)", "FornecedoresWindow.xaml", "FornecedorRepositoryTests"),
    ("ProdutoFornecedores", "ValorCompras", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "ProdutoFornecedoresRepository", "FornecedorOperationalService", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorCompras)", "FornecedoresWindow.xaml", "FornecedorRepositoryTests"),
    ("Produtos", "PrecoCompra", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "ProdutoRepository", "DatabaseService.Produtos", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(PrecoCompra)", "ProdutosWindow.xaml", "ProdutoRepositoryTests"),
    ("Produtos", "PrecoVenda", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "ProdutoRepository", "DatabaseService.Produtos", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(PrecoVenda)", "ProdutosWindow.xaml", "ProdutoRepositoryTests"),
    ("Produtos", "ValorTotalEstoque", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "ProdutoRepository", "DatabaseService.Produtos", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorTotalEstoque)", "ProdutosWindow.xaml", "ProdutoRepositoryTests"),
    ("Produtos", "TotalFaturado", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "ProdutoRepository", "DatabaseService.Produtos", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(TotalFaturado)", "ProdutosWindow.xaml", "ProdutoRepositoryTests"),
    ("ServicosPadrao", "ValorSugerido", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "ServicosPadraoRepository", "DatabaseService.Migrations", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(ValorSugerido)", "OrdensServicoWindow.xaml", "OrdemServicoRepositoryTests"),
    ("VendaItens", "PrecoUnitario", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "VendaItensRepository", "VendaRepository", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(PrecoUnitario)", "VendasWindow.xaml", "VendaRepositoryTests"),
    ("VendaItens", "CustoUnitario", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "VendaItensRepository", "VendaRepository", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(CustoUnitario)", "VendasWindow.xaml", "VendaRepositoryTests"),
    ("VendaItens", "Desconto", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "VendaItensRepository", "VendaRepository", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(Desconto)", "VendasWindow.xaml", "VendaRepositoryTests"),
    ("VendaItens", "Subtotal", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "VendaItensRepository", "VendaRepository", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(Subtotal)", "VendasWindow.xaml", "VendaRepositoryTests"),
    ("Vendas", "Total", "MONEY_DERIVED", "REAL", "INTEGER", "NAO", "NULL", "VendasRepository", "VendaRepository", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(Total)", "VendasWindow.xaml", "VendaRepositoryTests"),
    ("Vendas", "Desconto", "MONEY", "REAL", "INTEGER", "NAO", "NULL", "VendasRepository", "VendaRepository", "MoneyIO.GravarMoeda", "MoneyIO.LerMoeda", "SUM(Desconto)", "VendasWindow.xaml", "VendaRepositoryTests"),
]

NON_MONETARY_PROTECTED = [
    ("ProdutoFornecedores", "QuantidadeUltimaCompra", "QUANTITY", "INTEGER", "INTEGER", "NAO", "NULL", "ProdutoFornecedoresRepository", "Leitura Padrao", "Escrita Padrao", "N/A", "ZERO (Preservado inalterado)", "PASS"),
    ("Metas", "PercentualAtingido", "PERCENTAGE", "REAL", "REAL", "SIM", "NULL", "MetasRepository", "Leitura Padrao", "Escrita Padrao", "N/A", "ZERO (Preservado inalterado)", "PASS"),
    ("ImportacoesItens", "Quantidade", "QUANTITY", "REAL", "REAL", "NAO", "NULL", "ImportacoesItensRepository", "Leitura Padrao", "Escrita Padrao", "N/A", "ZERO (Preservado inalterado)", "PASS"),
    ("ImportacoesItens", "MargemAplicada", "PERCENTAGE", "REAL", "REAL", "SIM", "NULL", "ImportacoesItensRepository", "Leitura Padrao", "Escrita Padrao", "N/A", "ZERO (Preservado inalterado)", "PASS"),
    ("VendaItens", "Quantidade", "QUANTITY", "REAL", "REAL", "NAO", "NULL", "VendaItensRepository", "Leitura Padrao", "Escrita Padrao", "N/A", "ZERO (Preservado inalterado)", "PASS"),
    ("OrdemServicoItens", "Quantidade", "QUANTITY", "INTEGER", "INTEGER", "NAO", "NULL", "OrdemServicoItensRepository", "Leitura Padrao", "Escrita Padrao", "N/A", "ZERO (Preservado inalterado)", "PASS"),
    ("OrcamentoItens", "MargemLucro", "PERCENTAGE", "REAL", "REAL", "SIM", "NULL", "OrcamentoItensRepository", "Leitura Padrao", "Escrita Padrao", "N/A", "ZERO (Preservado inalterado)", "PASS"),
    ("Orcamentos", "DescontoPercentual", "PERCENTAGE", "REAL", "REAL", "SIM", "NULL", "OrcamentosRepository", "Leitura Padrao", "Escrita Padrao", "N/A", "ZERO (Preservado inalterado)", "PASS"),
    ("Orcamentos", "MargemLucro", "PERCENTAGE", "REAL", "REAL", "SIM", "NULL", "OrcamentosRepository", "Leitura Padrao", "Escrita Padrao", "N/A", "ZERO (Preservado inalterado)", "PASS"),
    ("Clientes", "Latitude", "COORDINATE", "REAL", "REAL", "SIM", "NULL", "ClientesRepository", "Leitura Padrao", "Escrita Padrao", "N/A", "ZERO (Preservado inalterado)", "PASS"),
    ("Clientes", "Longitude", "COORDINATE", "REAL", "REAL", "SIM", "NULL", "ClientesRepository", "Leitura Padrao", "Escrita Padrao", "N/A", "ZERO (Preservado inalterado)", "PASS"),
    ("Relatorios", "IndicadorCalculado", "TRANSIENT/REPORT", "REAL", "REAL", "SIM", "NULL", "RelatoriosRepository", "Leitura Padrao", "Escrita Padrao", "N/A", "ZERO (Preservado inalterado)", "PASS"),
    ("Dashboard", "IndicadorProgresso", "TRANSIENT/REPORT", "REAL", "REAL", "SIM", "NULL", "DashboardRepository", "Leitura Padrao", "Escrita Padrao", "N/A", "ZERO (Preservado inalterado)", "PASS"),
]

def generate_repository_matrix():
    out_path = r"Docs/audit/2026-09-20/B8_MONEY_REPOSITORY_MATRIX.csv"
    with open(out_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["Tabela", "Campo", "Tipo anterior", "Tipo CentsV1", "Repository", "Service", "Write path", "Read path", "Aggregate path", "UI path", "Tests", "Status"])
        for row in MONEY_FIELDS:
            # Table, Column, Category, PreviousType, CentsV1Type, Nullable, Default, Repo, Service, WritePath, ReadPath, AggPath, UIPath, Tests
            writer.writerow([
                row[0],  # Tabela
                row[1],  # Campo
                row[3],  # Tipo anterior
                row[4],  # Tipo CentsV1
                row[7],  # Repository
                row[8],  # Service
                row[9],  # Write path
                row[10], # Read path
                row[11], # Aggregate path
                row[12], # UI path
                row[13], # Tests
                "MAPPED_READY_FOR_CENTSV1"
            ])
    print(f"B8_MONEY_REPOSITORY_MATRIX.csv written ({len(MONEY_FIELDS)} fields).")

def generate_application_audit():
    # Scan all CS files in PrimoAutoEletrica
    root_dir = r"PrimoAutoEletrica"
    audit_rows = []
    
    # Patterns to match
    patterns = [
        ('ReadDecimal', re.compile(r'\bReadDecimal\s*\(')),
        ('GetDouble', re.compile(r'\bGetDouble\s*\(')),
        ('GetFloat', re.compile(r'\bGetFloat\s*\(')),
        ('Convert.ToDouble', re.compile(r'\bConvert\.ToDouble\s*\(')),
        ('Convert.ToSingle', re.compile(r'\bConvert\.ToSingle\s*\(')),
        ('Convert.ToDecimal', re.compile(r'\bConvert\.ToDecimal\s*\(')),
        ('LerDecimal', re.compile(r'\bLerDecimal\s*\(')),
        ('LerMoeda', re.compile(r'\bMoneyIO\.LerMoeda\b')),
        ('GravarMoeda', re.compile(r'\bMoneyIO\.GravarMoeda\b')),
        ('MoneyCents', re.compile(r'\bMoneyCents\b')),
        ('CAST_REAL', re.compile(r'CAST\s*\(.*?\s+AS\s+REAL\)', re.IGNORECASE)),
        ('SUM_SQL', re.compile(r'\bSUM\s*\(.*?\)', re.IGNORECASE)),
        ('AVG_SQL', re.compile(r'\bAVG\s*\(.*?\)', re.IGNORECASE)),
    ]

    for dirpath, _, filenames in os.walk(root_dir):
        for fname in filenames:
            if not fname.endswith('.cs'):
                continue
            full_path = os.path.join(dirpath, fname)
            rel_file = os.path.relpath(full_path, ".")
            
            with open(full_path, "r", encoding="utf-8", errors="ignore") as fp:
                lines = fp.readlines()

            current_method = "ClassLevel"
            method_re = re.compile(r'(?:public|private|protected|internal|static|async|\s)+\s+[\w\<\>\[\]\?]+\s+(\w+)\s*\(')

            for line_no, raw_line in enumerate(lines, 1):
                m_match = method_re.search(raw_line)
                if m_match and not any(k in raw_line for k in ['if', 'while', 'for', 'switch', 'catch', 'return']):
                    current_method = m_match.group(1)

                matched_types = []
                for p_type, p_re in patterns:
                    if p_re.search(raw_line):
                        matched_types.append(p_type)

                if not matched_types:
                    continue

                line_strip = raw_line.strip()
                for p_type in matched_types:
                    # Classification logic
                    # A = monetária e precisa conversão
                    # B = monetária já protegida
                    # C = não monetária
                    # D = falso positivo
                    
                    tabela = "N/A"
                    campo = "N/A"
                    classification = "D"
                    action = "Nenhuma"
                    status = "ANALYZED"

                    # Check for MoneyIO usage
                    if p_type in ('LerMoeda', 'GravarMoeda', 'MoneyCents'):
                        classification = "B"
                        action = "Preservar uso do MoneyIO/MoneyCents"
                        status = "PROTECTED"
                        # Try to detect field/table
                        for t, c, _, _, _, _, _, _, _, _, _, _, _, _ in MONEY_FIELDS:
                            if c in line_strip:
                                tabela = t
                                campo = c
                                break

                    elif p_type in ('ReadDecimal', 'LerDecimal'):
                        # Check if it's reading a known money field
                        is_money = False
                        is_non_money = False
                        for t, c, _, _, _, _, _, _, _, _, _, _, _, _ in MONEY_FIELDS:
                            if c in line_strip:
                                tabela = t
                                campo = c
                                is_money = True
                                break
                        if not is_money:
                            for t, c, cat, _, _, _, _, _, _, _, _, _, _ in NON_MONETARY_PROTECTED:
                                if c in line_strip:
                                    tabela = t
                                    campo = c
                                    is_non_money = True
                                    break

                        if is_money:
                            classification = "A"
                            action = f"Converter para MoneyIO.LerMoeda ({tabela}.{campo})"
                            status = "REQUIRES_CONVERSION"
                        elif is_non_money:
                            classification = "C"
                            action = f"Preservar leitura numerica padrao ({tabela}.{campo})"
                            status = "PROTECTED_NON_MONETARY"
                        else:
                            # Helper definition or generic read
                            if "private static decimal ReadDecimal" in line_strip or "private static decimal LerDecimal" in line_strip:
                                classification = "A"
                                action = "Atualizar helper para delegar a MoneyIO / CentsV1"
                                status = "REQUIRES_CONVERSION"
                            else:
                                classification = "D"
                                action = "Verificar contexto de chamada"
                                status = "FALSE_POSITIVE"

                    elif p_type in ('SUM_SQL', 'AVG_SQL'):
                        is_money = False
                        for t, c, _, _, _, _, _, _, _, _, _, _, _, _ in MONEY_FIELDS:
                            if c in line_strip:
                                tabela = t
                                campo = c
                                is_money = True
                                break
                        if is_money:
                            classification = "A"
                            action = "Garantir conversao de centavos via MoneyIO.ConverterAgregacao"
                            status = "REQUIRES_CONVERSION"
                        else:
                            classification = "C"
                            action = "Preservar agregacao nao monetaria"
                            status = "NON_MONETARY_AGGREGATE"

                    elif p_type in ('Convert.ToDecimal', 'Convert.ToDouble', 'Convert.ToSingle', 'GetDouble', 'GetFloat', 'CAST_REAL'):
                        # Check context
                        is_money = False
                        for t, c, _, _, _, _, _, _, _, _, _, _, _, _ in MONEY_FIELDS:
                            if c in line_strip:
                                tabela = t
                                campo = c
                                is_money = True
                                break
                        if is_money:
                            classification = "A"
                            action = "Substituir conversao direta por MoneyIO"
                            status = "REQUIRES_CONVERSION"
                        elif any(k in line_strip.lower() for k in ['percent', 'taxa', 'aliquota', 'margem', 'qtd', 'quantidade', 'peso', 'lat', 'lon', 'width', 'height', 'opacity']):
                            classification = "C"
                            action = "Preservar tipo double/float/decimal para geometria, percentual ou quantidade"
                            status = "NON_MONETARY_FIELD"
                        else:
                            classification = "D"
                            action = "Falso positivo / Conversao geral de dados"
                            status = "FALSE_POSITIVE"

                    audit_rows.append([
                        rel_file,
                        line_no,
                        current_method,
                        campo,
                        tabela,
                        p_type,
                        classification,
                        action,
                        status
                    ])

    out_path = r"Docs/audit/2026-09-20/B8_MONEY_APPLICATION_AUDIT.csv"
    with open(out_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["arquivo", "linha", "método", "campo", "tabela", "tipo", "classificação", "ação necessária", "status"])
        for row in audit_rows:
            writer.writerow(row)
    
    print(f"B8_MONEY_APPLICATION_AUDIT.csv written ({len(audit_rows)} occurrences).")
    counts = {}
    for r in audit_rows:
        counts[r[6]] = counts.get(r[6], 0) + 1
    print("Classification summary:")
    for k, v in sorted(counts.items()):
        print(f"  Class {k}: {v}")

if __name__ == "__main__":
    generate_repository_matrix()
    generate_application_audit()
