# P1 Money inventory (read-only)

**Branch:** audit/product-discovery-2026-09
**Tip:** 34a15b9
**Data:** 2026-09-20
**Scope:** map only — **no schema migration**, no type rewrite.

## Counts by pattern
Preco|Valor|Total|Custo|Desconto|Comissao|Saldo|Receber|Pagar: 4139
decimal: 737
REAL_sql: 135
double: 124
float: 2

## Top files (any money pattern)
- 256 — `PrimoAutoEletrica\Services\FinanceiroDatabaseService.cs`
- 205 — `PrimoAutoEletrica\Services\RelatorioDatabaseService.cs`
- 182 — `PrimoAutoEletrica\ViewModels\FinanceiroViewModel.cs`
- 161 — `PrimoAutoEletrica\ViewModels\RelatoriosViewModel.cs`
- 161 — `PrimoAutoEletrica\Services\CaixaService.cs`
- 158 — `PrimoAutoEletrica\Services\DatabaseProviders\SqlServerSchema.sql`
- 140 — `PrimoAutoEletrica\Services\OrcamentoDatabaseService.cs`
- 134 — `PrimoAutoEletrica\Models\Relatorio.cs`
- 110 — `PrimoAutoEletrica\ViewModels\OrcamentosViewModel.cs`
- 95 — `PrimoAutoEletrica\UserControls\EstoqueControl.xaml.cs`
- 95 — `PrimoAutoEletrica\Views\AjusteEstoqueWindow.xaml.cs`
- 87 — `PrimoAutoEletrica\Services\OperationalWorkflowTestService.cs`
- 86 — `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs`
- 84 — `PrimoAutoEletrica\Services\FuncionarioOperationalService.cs`
- 83 — `PrimoAutoEletrica\Services\FornecedorOperationalService.cs`
- 82 — `PrimoAutoEletrica\Views\NovoOrcamentoWindow.xaml.cs`
- 75 — `PrimoAutoEletrica\Services\AgendamentoDatabaseService.cs`
- 68 — `PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs`
- 67 — `PrimoAutoEletrica\Services\DatabaseService.OrdensServico.cs`
- 65 — `PrimoAutoEletrica\Services\DatabaseService.Produtos.cs`
- 61 — `PrimoAutoEletrica\Services\VendaService.cs`
- 61 — `PrimoAutoEletrica\Services\DatabaseService.Migrations.cs`
- 59 — `PrimoAutoEletrica\ViewModels\AgendamentosViewModel.cs`
- 58 — `PrimoAutoEletrica\UserControls\OrdensServicoControl.xaml.cs`
- 58 — `PrimoAutoEletrica\Repositories\ProdutoRepository.cs`
- 57 — `PrimoAutoEletrica\Services\UiSmokeTestService.NFe.cs`
- 57 — `PrimoAutoEletrica\Services\UiSmokeTestService.Fixtures.cs`
- 56 — `PrimoAutoEletrica\Views\OrdemServicoWindow.xaml.cs`
- 51 — `PrimoAutoEletrica\Views\EditarProdutoWindow.xaml.cs`
- 51 — `PrimoAutoEletrica\Repositories\VendaRepository.cs`
- 50 — `PrimoAutoEletrica\Services\UiSmokeTestService.Financeiro.cs`
- 49 — `PrimoAutoEletrica\Services\AgendamentoReportService.cs`
- 47 — `PrimoAutoEletrica\Services\DocumentoPdfService.cs`
- 46 — `PrimoAutoEletrica\Services\AutoEletricaTecnicaService.cs`
- 46 — `PrimoAutoEletrica\Services\DatabaseService.cs`
- 44 — `PrimoAutoEletrica\Services\RelatorioExportService.cs`
- 44 — `PrimoAutoEletrica\Views\NovoProdutoWindow.xaml.cs`
- 44 — `PrimoAutoEletrica\UserControls\FinanceiroControl.xaml.cs`
- 43 — `PrimoAutoEletrica\Services\LocalizationService.Modules.cs`
- 42 — `PrimoAutoEletrica\Services\OrcamentoPdfService.cs`

## Candidate monetary fields (type + name heuristic)
| File | Line | Snippet |
|------|------|---------|
| `PrimoAutoEletrica.Api\Configuration\SwaggerConfiguration.cs` | 158 | `["valor"] = new Microsoft.OpenApi.Any.OpenApiDouble(1500.00),` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 47 | `ValorTotal REAL NOT NULL DEFAULT 0,` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 48 | `ValorProdutos REAL NOT NULL DEFAULT 0,` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 71 | `ValorUnitario REAL NOT NULL DEFAULT 0,` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 72 | `ValorTotal REAL NOT NULL DEFAULT 0,` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 80 | `MargemAplicada REAL NOT NULL DEFAULT 0,` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 81 | `PrecoVendaSugerido REAL NOT NULL DEFAULT 0,` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 104 | `ValorTotal REAL NOT NULL DEFAULT 0,` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 150 | `EnsureColumnExists(connection, "ImportacoesItens", "MargemAplicada", "ALTER TABLE ImportacoesItens ADD COLUMN MargemAplicada REAL NOT NULL DEFAULT 0;");` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 151 | `EnsureColumnExists(connection, "ImportacoesItens", "PrecoVendaSugerido", "ALTER TABLE ImportacoesItens ADD COLUMN PrecoVendaSugerido REAL NOT NULL DEFAULT 0;");` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 327 | `ValorTotal = ReadDecimal(reader, 6),` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 328 | `ValorProdutos = ReadDecimal(reader, 7),` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 381 | `ValorTotal = ReadDecimal(reader, 6),` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 382 | `ValorProdutos = ReadDecimal(reader, 7),` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 441 | `ValorUnitario = ReadDecimal(reader, 5),` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 442 | `ValorTotal = ReadDecimal(reader, 6),` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 450 | `MargemAplicada = ReadDecimal(reader, 14),` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 451 | `PrecoVendaSugerido = ReadDecimal(reader, 15),` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 893 | `PrecoCompra = ReadDecimal(reader, 3),` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 894 | `PrecoVenda = ReadDecimal(reader, 4),` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 895 | `MargemLucro = ReadDecimal(reader, 5),` |
| `PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs` | 896 | `ValorTotalEstoque = ReadDecimal(reader, 6),` |
| `PrimoAutoEletrica\Helpers\CadastroValidationHelper.cs` | 192 | `public static string? ValidarDecimal(decimal valor, string descricao, bool permitirZero = true, bool permitirNegativo = false)` |
| `PrimoAutoEletrica\Helpers\ComercialValidationHelper.cs` | 20 | `public static void GarantirValorMaiorQueZero(decimal valor, string descricao)` |
| `PrimoAutoEletrica\Helpers\ComercialValidationHelper.cs` | 28 | `public static void GarantirValorMaiorOuIgualZero(decimal valor, string descricao)` |
| `PrimoAutoEletrica\Helpers\ComercialValidationHelper.cs` | 52 | `public static void GarantirDescontoValido(decimal desconto, decimal baseCalculo, string descricao)` |
| `PrimoAutoEletrica\Helpers\ComercialValidationHelper.cs` | 76 | `public static decimal CalcularSubtotal(decimal quantidade, decimal valorUnitario, decimal desconto)` |
| `PrimoAutoEletrica\Helpers\SqlIdentifierGuard.cs` | 49 | `public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);` |
| `PrimoAutoEletrica\Models\Agendamento.cs` | 31 | `public decimal ClienteTotalGasto { get; set; }` |
| `PrimoAutoEletrica\Models\Agendamento.cs` | 59 | `public decimal ValorEstimado { get; set; }` |
| `PrimoAutoEletrica\Models\Agendamento.cs` | 60 | `public decimal ValorReal { get; set; }` |
| `PrimoAutoEletrica\Models\Agendamento.cs` | 61 | `public decimal ValorPago { get; set; }` |
| `PrimoAutoEletrica\Models\Agendamento.cs` | 76 | `public decimal ValorProdutos { get; set; }` |
| `PrimoAutoEletrica\Models\Agendamento.cs` | 80 | `public decimal ValorServicos { get; set; }` |
| `PrimoAutoEletrica\Models\Agendamento.cs` | 124 | `public decimal PrecoUnitario { get; set; }` |
| `PrimoAutoEletrica\Models\Agendamento.cs` | 125 | `public decimal PrecoTotal { get; set; }` |
| `PrimoAutoEletrica\Models\Agendamento.cs` | 135 | `public decimal Valor { get; set; }` |
| `PrimoAutoEletrica\Models\Agendamento.cs` | 237 | `public decimal TotalGasto { get; set; }` |
| `PrimoAutoEletrica\Models\Agendamento.cs` | 263 | `public decimal Valor { get; set; }` |
| `PrimoAutoEletrica\Models\AutoEletricaTecnica.cs` | 59 | `public decimal ValorTotal { get; set; }` |
| `PrimoAutoEletrica\Models\AutoEletricaTecnica.cs` | 75 | `public decimal ValorPadrao { get; set; }` |
| `PrimoAutoEletrica\Models\AutoEletricaTecnica.cs` | 88 | `public decimal ValorMaoObra { get; set; }` |
| `PrimoAutoEletrica\Models\CaixaOperacional.cs` | 14 | `public decimal ValorAbertura { get; set; }` |
| `PrimoAutoEletrica\Models\CaixaOperacional.cs` | 15 | `public decimal ValorEsperado { get; set; }` |
| `PrimoAutoEletrica\Models\CaixaOperacional.cs` | 16 | `public decimal? ValorInformadoFechamento { get; set; }` |
| `PrimoAutoEletrica\Models\CaixaOperacional.cs` | 17 | `public decimal TotalVendas { get; set; }` |
| `PrimoAutoEletrica\Models\CaixaOperacional.cs` | 18 | `public decimal TotalSangrias { get; set; }` |
| `PrimoAutoEletrica\Models\CaixaOperacional.cs` | 19 | `public decimal TotalSuprimentos { get; set; }` |
| `PrimoAutoEletrica\Models\CaixaOperacional.cs` | 40 | `public decimal ValorMovimento { get; set; }` |
| `PrimoAutoEletrica\Models\CaixaOperacional.cs` | 41 | `public decimal ValorInicial { get; set; }` |
| `PrimoAutoEletrica\Models\CaixaOperacional.cs` | 42 | `public decimal ValorFinal { get; set; }` |
| `PrimoAutoEletrica\Models\CatalogoImportacaoPreview.cs` | 17 | `public int TotalSemNomeReal { get; set; }` |
| `PrimoAutoEletrica\Models\Cliente.cs` | 36 | `public decimal TotalGasto { get; set; }` |
| `PrimoAutoEletrica\Models\Fornecedor.cs` | 51 | `public decimal TotalCompras { get; set; } = 0;` |
| `PrimoAutoEletrica\Models\HistoricoServico.cs` | 13 | `public decimal Valor { get; set; }` |
| `PrimoAutoEletrica\Models\ItemVenda.cs` | 12 | `public decimal PrecoUnitario { get; set; }` |
| `PrimoAutoEletrica\Models\ItemVenda.cs` | 13 | `public decimal CustoUnitario { get; set; }` |
| `PrimoAutoEletrica\Models\ItemVenda.cs` | 14 | `public decimal Desconto { get; set; }` |
| `PrimoAutoEletrica\Models\ItemVenda.cs` | 19 | `public decimal Subtotal => (PrecoUnitario * Quantidade) - Desconto;` |
| `PrimoAutoEletrica\Models\NotaFiscalImportada.cs` | 14 | `public decimal ValorTotal { get; set; }` |
| `PrimoAutoEletrica\Models\NotaFiscalImportada.cs` | 15 | `public decimal ValorProdutos { get; set; }` |
| `PrimoAutoEletrica\Models\OficinaProfissional.cs` | 23 | `public decimal TotalEstimado => Cards.Sum(card => card.ValorEstimado);` |
| `PrimoAutoEletrica\Models\OficinaProfissional.cs` | 43 | `public decimal ValorEstimado { get; set; }` |
| `PrimoAutoEletrica\Models\OficinaProfissional.cs` | 55 | `public decimal? Valor { get; set; }` |
| `PrimoAutoEletrica\Models\Orcamento.cs` | 24 | `public decimal Subtotal { get; set; }` |
| `PrimoAutoEletrica\Models\Orcamento.cs` | 25 | `public decimal Desconto { get; set; }` |
| `PrimoAutoEletrica\Models\Orcamento.cs` | 27 | `public decimal DescontoPercentual { get; set; }` |
| `PrimoAutoEletrica\Models\Orcamento.cs` | 29 | `public decimal Total { get; set; }` |
| `PrimoAutoEletrica\Models\Orcamento.cs` | 30 | `public decimal MargemLucro { get; set; }` |
| `PrimoAutoEletrica\Models\Orcamento.cs` | 31 | `public decimal LucroEstimado { get; set; }` |
| `PrimoAutoEletrica\Models\Orcamento.cs` | 32 | `public decimal ComissaoVendedor { get; set; }` |
| `PrimoAutoEletrica\Models\OrcamentoItem.cs` | 19 | `public decimal PrecoUnitario { get; set; }` |
| `PrimoAutoEletrica\Models\OrcamentoItem.cs` | 20 | `public decimal PrecoCusto { get; set; }` |
| `PrimoAutoEletrica\Models\OrcamentoItem.cs` | 21 | `public decimal Desconto { get; set; }` |
| `PrimoAutoEletrica\Models\OrcamentoItem.cs` | 22 | `public decimal Subtotal { get; set; }` |
| `PrimoAutoEletrica\Models\OrcamentoItem.cs` | 23 | `public decimal LucroEstimado { get; set; }` |
| `PrimoAutoEletrica\Models\OrcamentoItem.cs` | 24 | `public decimal MargemLucro { get; set; }` |
| `PrimoAutoEletrica\Models\OrdemServico.cs` | 53 | `public decimal ValorMaoObra { get; set; }` |
| `PrimoAutoEletrica\Models\OrdemServico.cs` | 54 | `public decimal Desconto { get; set; }` |
| `PrimoAutoEletrica\Models\OrdemServicoItem.cs` | 14 | `public decimal ValorUnitario { get; set; }` |
| `PrimoAutoEletrica\Models\OrdemServicoItem.cs` | 15 | `public decimal CustoUnitario { get; set; }` |
| `PrimoAutoEletrica\Models\OrdemServicoItem.cs` | 20 | `public decimal Total => Quantidade * ValorUnitario;` |
| `PrimoAutoEletrica\Models\OrdemServicoItem.cs` | 21 | `public decimal CustoTotal => Quantidade * CustoUnitario;` |
| `PrimoAutoEletrica\Models\PagamentoCliente.cs` | 8 | `public decimal Valor { get; set; }` |
| `PrimoAutoEletrica\Models\Primox360Models.cs` | 17 | `public decimal ReceitaOsTotal { get; init; }` |
| `PrimoAutoEletrica\Models\Primox360Models.cs` | 18 | `public decimal ReceitaVendasTotal { get; init; }` |
| `PrimoAutoEletrica\Models\Primox360Models.cs` | 19 | `public decimal ReceitaTotal => ReceitaOsTotal + ReceitaVendasTotal;` |
| `PrimoAutoEletrica\Models\Primox360Models.cs` | 27 | `public decimal ValorPerdidoOrcamentos { get; init; }` |
| `PrimoAutoEletrica\Models\Primox360Models.cs` | 30 | `public decimal? TotalGastoCadastro { get; init; }` |
| `PrimoAutoEletrica\Models\Primox360Models.cs` | 84 | `public decimal TotalItens { get; init; }` |
| `PrimoAutoEletrica\Models\Primox360Models.cs` | 97 | `public decimal Valor { get; init; }` |
| `PrimoAutoEletrica\Models\Primox360Models.cs` | 116 | `public decimal Custo { get; init; }` |
| `PrimoAutoEletrica\Models\Primox360Models.cs` | 117 | `public decimal Preco { get; init; }` |
| `PrimoAutoEletrica\Models\Primox360Models.cs` | 118 | `public decimal MargemPercentual { get; init; }` |
| `PrimoAutoEletrica\Models\Primox360Models.cs` | 121 | `public decimal ValorUsadoEmOs { get; init; }` |
| `PrimoAutoEletrica\Models\Produto.cs` | 33 | `public decimal PrecoCompra { get; set; }` |
| `PrimoAutoEletrica\Models\Produto.cs` | 34 | `public decimal PrecoVenda { get; set; }` |
| `PrimoAutoEletrica\Models\Produto.cs` | 35 | `public decimal MargemLucro { get; set; }` |
| `PrimoAutoEletrica\Models\Produto.cs` | 36 | `public decimal ValorTotalEstoque { get; set; }` |
| `PrimoAutoEletrica\Models\Produto.cs` | 72 | `public decimal TotalFaturado { get; set; }` |
| `PrimoAutoEletrica\Models\Produto.cs` | 87 | `public decimal ReceitaEstimadaTotal { get; set; }` |
| `PrimoAutoEletrica\Models\ProdutoFornecedor.cs` | 14 | `public decimal PrecoUltimaCompra { get; set; }` |
| `PrimoAutoEletrica\Models\ProdutoFornecedor.cs` | 18 | `public decimal ValorCompras { get; set; }` |
| `PrimoAutoEletrica\Models\ProdutoImportacaoSnapshot.cs` | 16 | `public decimal PrecoCompra { get; set; }` |
| `PrimoAutoEletrica\Models\ProdutoImportacaoSnapshot.cs` | 17 | `public decimal PrecoVenda { get; set; }` |
| `PrimoAutoEletrica\Models\ProdutoImportacaoSnapshot.cs` | 18 | `public decimal MargemLucro { get; set; }` |
| `PrimoAutoEletrica\Models\ProdutoImportacaoSnapshot.cs` | 19 | `public decimal ValorTotalEstoque { get; set; }` |
| `PrimoAutoEletrica\Models\ProdutoImportado.cs` | 16 | `private decimal _valorUnitario;` |
| `PrimoAutoEletrica\Models\ProdutoImportado.cs` | 17 | `private decimal _valorTotal;` |
| `PrimoAutoEletrica\Models\ProdutoImportado.cs` | 25 | `private decimal _margemAplicada = 200m;` |
| `PrimoAutoEletrica\Models\ProdutoImportado.cs` | 26 | `private decimal _precoVendaSugerido;` |
| `PrimoAutoEletrica\Models\ProdutoImportado.cs` | 70 | `public decimal ValorUnitario` |
| `PrimoAutoEletrica\Models\ProdutoImportado.cs` | 76 | `public decimal ValorTotal` |
| `PrimoAutoEletrica\Models\ProdutoImportado.cs` | 162 | `public decimal MargemAplicada` |
| `PrimoAutoEletrica\Models\ProdutoImportado.cs` | 174 | `public decimal PrecoVendaSugerido` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 17 | `public decimal ValorTotal { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 30 | `public decimal Valor { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 41 | `public decimal ContasReceberPendentes { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 42 | `public decimal ContasPagarPendentes { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 45 | `public decimal ResultadoProjetado => ResultadoOperacional + ContasReceberPendentes - ContasPagarPendentes;` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 53 | `public decimal LucroOrdensServico { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 54 | `public decimal LucroProdutos { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 55 | `public decimal LucroServicos { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 59 | `public decimal SaldoCaixaOperadores { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 71 | `public decimal Custo { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 72 | `public decimal LucroBruto { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 73 | `public decimal MargemPercentual { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 81 | `public decimal Valor { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 90 | `public decimal Valor { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 100 | `public decimal Saldo { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 101 | `public decimal TotalVendas { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 104 | `public decimal TicketMedio => QuantidadeVendas > 0 ? TotalVendas / QuantidadeVendas : 0m;` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 124 | `public decimal ValorTotal { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 125 | `public decimal Desconto { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 126 | `public decimal Lucro { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 138 | `public decimal ReceitaTotal { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 139 | `public decimal CustoTotal { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 140 | `public decimal LucroBruto { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 141 | `public decimal MargemPercentual { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 160 | `public decimal Valor { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 180 | `public decimal TotalVendas { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 182 | `public decimal Diferenca => TotalVendas - EntradasFinanceiras;` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 201 | `public decimal ValorTotal { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 202 | `public decimal LucroBruto { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 213 | `public decimal ValorTotal { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 214 | `public decimal LucroBruto { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 216 | `public decimal TicketMedio => TotalOrdens > 0 ? ValorTotal / TotalOrdens : 0m;` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 224 | `public decimal ReceitaTotal { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 225 | `public decimal CustoTotal { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 226 | `public decimal LucroBruto => ReceitaTotal - CustoTotal;` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 227 | `public decimal MargemPercentual => ReceitaTotal > 0 ? LucroBruto / ReceitaTotal * 100m : 0m;` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 241 | `public decimal ValorUnitario { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 242 | `public decimal ValorTotal { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 258 | `public decimal TotalCompras { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 276 | `public decimal ValorTotal { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 288 | `public decimal ValorInicial { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 289 | `public decimal ValorFinal { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 302 | `public decimal MetaValor { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 303 | `public decimal ValorAtual { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 349 | `public decimal? Valor { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 358 | `public decimal ValorAtual { get; set; }` |
| `PrimoAutoEletrica\Models\Relatorio.cs` | 359 | `public decimal ValorAnterior { get; set; }` |
| `PrimoAutoEletrica\Models\ServicoCliente.cs` | 11 | `public decimal Valor { get; set; }` |
| `PrimoAutoEletrica\Models\Venda.cs` | 12 | `public decimal Total { get; set; }` |
| `PrimoAutoEletrica\Models\Venda.cs` | 14 | `public decimal Desconto { get; set; }` |
| `PrimoAutoEletrica\Repositories\ClienteRepository.cs` | 760 | `TotalGasto = ReadDecimal(reader, 17),` |
| `PrimoAutoEletrica\Repositories\ClienteRepository.cs` | 1165 | `decimal totalGasto = 0;` |
| `PrimoAutoEletrica\Repositories\ClienteRepository.cs` | 1215 | `totalGasto = ReadDecimal(reader, 0);` |
| `PrimoAutoEletrica\Repositories\FornecedorRepository.cs` | 573 | `TotalCompras = ReadDecimal(reader, 29)` |
| `PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs` | 89 | `decimal PrecoCompra,` |
| `PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs` | 1013 | `decimal totalGasto = 0;` |
| `PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs` | 1063 | `totalGasto = ReadDecimal(reader, 0);` |
| `PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs` | 1197 | `ValorMaoObra = ReadDecimal(reader, 39),` |
| `PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs` | 1198 | `Desconto = ReadDecimal(reader, 40),` |
| `PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs` | 1243 | `ValorUnitario = ReadDecimal(reader, 6),` |
| `PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs` | 1244 | `CustoUnitario = ReadDecimal(reader, 7),` |
| `PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs` | 1357 | `private static (string Codigo, string Nome, decimal PrecoCompra, int EstoqueAtual, int ReservadoNoAgendamento, int ReservasAtivas) ObterEstoqueAtual(` |
| `PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs` | 1411 | `private static (string Codigo, string Nome, decimal PrecoCompra, int EstoqueAtual, int ReservasAtivas) ObterEstoqueResumo(` |
| `PrimoAutoEletrica\Repositories\ProdutoRepository.cs` | 341 | `PrecoCompra = decimal.TryParse(partes[4], out var precoCompra) ? precoCompra : 0,` |
| `PrimoAutoEletrica\Repositories\ProdutoRepository.cs` | 342 | `PrecoVenda = decimal.TryParse(partes[5], out var precoVenda) ? precoVenda : 0,` |
| `PrimoAutoEletrica\Repositories\ProdutoRepository.cs` | 376 | `var auditorias = new List<(Guid ProdutoId, string Codigo, string Nome, decimal PrecoCompra, int EstoqueAnterior, int EstoqueNovo, int ReservadoAnterior, int Res` |
| `PrimoAutoEletrica\Repositories\ProdutoRepository.cs` | 469 | `private static (string Codigo, string Nome, decimal PrecoCompra, int EstoqueAtual, int ReservadoNoAgendamento, int ReservasAtivas) ObterEstoqueAtual(` |
| `PrimoAutoEletrica\Repositories\ProdutoRepository.cs` | 541 | `PrecoCompra = ReadDecimal(reader, 18),` |
| `PrimoAutoEletrica\Repositories\ProdutoRepository.cs` | 542 | `PrecoVenda = ReadDecimal(reader, 19),` |
| `PrimoAutoEletrica\Repositories\ProdutoRepository.cs` | 543 | `MargemLucro = ReadDecimal(reader, 20),` |
| `PrimoAutoEletrica\Repositories\ProdutoRepository.cs` | 544 | `ValorTotalEstoque = ReadDecimal(reader, 21),` |
| `PrimoAutoEletrica\Repositories\ProdutoRepository.cs` | 568 | `TotalFaturado = ReadDecimal(reader, 45),` |
| `PrimoAutoEletrica\Repositories\VendaRepository.cs` | 174 | `PrecoCompra = ReadDecimal(readerLocal, 6)` |
| `PrimoAutoEletrica\Repositories\VendaRepository.cs` | 178 | `PrecoUnitario = ReadDecimal(readerLocal, 5),` |
| `PrimoAutoEletrica\Repositories\VendaRepository.cs` | 179 | `CustoUnitario = ReadDecimal(readerLocal, 6),` |
| `PrimoAutoEletrica\Repositories\VendaRepository.cs` | 180 | `Desconto = ReadDecimal(readerLocal, 7)` |
| `PrimoAutoEletrica\Repositories\VendaRepository.cs` | 221 | `PrecoCompra = ReadDecimal(reader, 6)` |
| `PrimoAutoEletrica\Repositories\VendaRepository.cs` | 225 | `PrecoUnitario = ReadDecimal(reader, 5),` |
| `PrimoAutoEletrica\Repositories\VendaRepository.cs` | 226 | `CustoUnitario = ReadDecimal(reader, 6),` |
| `PrimoAutoEletrica\Repositories\VendaRepository.cs` | 227 | `Desconto = ReadDecimal(reader, 7)` |
| `PrimoAutoEletrica\Repositories\VendaRepository.cs` | 278 | `Total = ReadDecimal(readerLocal, 4),` |
| `PrimoAutoEletrica\Repositories\VendaRepository.cs` | 280 | `Desconto = ReadDecimal(readerLocal, 6),` |
| `PrimoAutoEletrica\Repositories\VendaRepository.cs` | 330 | `Total = ReadDecimal(reader, 4),` |
| `PrimoAutoEletrica\Repositories\VendaRepository.cs` | 332 | `Desconto = ReadDecimal(reader, 6),` |
| `PrimoAutoEletrica\Repositories\VendaRepository.cs` | 389 | `Total = ReadDecimal(reader, 4),` |

## Notes / risks (draft)
- Raw dump: `Docs/audit/2026-09-20/P1_MONEY_INVENTORY_RAW.csv` (5137 rows).
- double/float on money fields = precision risk; decimal preferred for BRL.
- SQLite REAL is IEEE float — confirm which money columns use REAL vs INTEGER cents vs TEXT.
- **No migrate in this step.** Next after review: propose strategy (cents int vs decimal) + blast radius.

## Honest gaps
- Heuristic scan only (regex); may miss cryptic names and include false positives (e.g. TotalCount).
- Does not yet classify UI binding vs DB column vs DTO.

## Refined property declarations (name + numeric type)

- `decimal` money-ish props (heuristic): **231**
- `double`/`float` money-ish props (heuristic): **0**

### double/float money-ish (blast radius hot list)
```

```

### SQL / schema REAL-ish money (sample)
```
PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs:47:                    ValorTotal REAL NOT NULL DEFAULT 0,
PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs:48:                    ValorProdutos REAL NOT NULL DEFAULT 0,
PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs:71:                    ValorUnitario REAL NOT NULL DEFAULT 0,
PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs:72:                    ValorTotal REAL NOT NULL DEFAULT 0,
PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs:81:                    PrecoVendaSugerido REAL NOT NULL DEFAULT 0,
PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs:104:                    ValorTotal REAL NOT NULL DEFAULT 0,
PrimoAutoEletrica\Data\Repositories\ImportacaoRepository.cs:151:            EnsureColumnExists(connection, "ImportacoesItens", "PrecoVendaSugerido", "ALTER TABLE ImportacoesItens ADD COLUMN PrecoVendaSugerido REAL NOT NULL DEFAULT 0;");
PrimoAutoEletrica\Helpers\ComercialValidationHelper.cs:20:        public static void GarantirValorMaiorQueZero(decimal valor, string descricao)
PrimoAutoEletrica\Helpers\ComercialValidationHelper.cs:28:        public static void GarantirValorMaiorOuIgualZero(decimal valor, string descricao)
PrimoAutoEletrica\Helpers\ComercialValidationHelper.cs:52:        public static void GarantirDescontoValido(decimal desconto, decimal baseCalculo, string descricao)
PrimoAutoEletrica\Helpers\ComercialValidationHelper.cs:76:        public static decimal CalcularSubtotal(decimal quantidade, decimal valorUnitario, decimal desconto)
PrimoAutoEletrica\Helpers\CadastroValidationHelper.cs:192:        public static string? ValidarDecimal(decimal valor, string descricao, bool permitirZero = true, bool permitirNegativo = false)
PrimoAutoEletrica\Views\AjusteEstoqueWindow.xaml.cs:47:        public decimal PrecoVenda { get; set; }
PrimoAutoEletrica\Views\AjusteEstoqueWindow.xaml.cs:48:        public decimal NovoPreco { get; set; }
PrimoAutoEletrica\Views\AjusteEstoqueWindow.xaml.cs:137:        private decimal CalcularNovoPreco(decimal precoAtual)
PrimoAutoEletrica\Views\AjusteEstoqueWindow.xaml.cs:139:            if (!decimal.TryParse(PrecoValorTextBox.Text, out var valorAjuste) || valorAjuste == 0)
PrimoAutoEletrica\Views\AjusteEstoqueWindow.xaml.cs:152:                return decimal.Round(Math.Max(0, novoPrecoPercentual), 2);
PrimoAutoEletrica\Views\AjusteEstoqueWindow.xaml.cs:159:            return decimal.Round(Math.Max(0, novoPrecoValor), 2);
PrimoAutoEletrica\Views\AjusteEstoqueWindow.xaml.cs:162:        private decimal CalcularDiferenca(decimal precoAtual)
PrimoAutoEletrica\Views\AjusteEstoqueWindow.xaml.cs:511:            if (!decimal.TryParse(PrecoValorTextBox.Text, out var valorAjuste) || valorAjuste == 0)
PrimoAutoEletrica\Views\AjusteEstoqueWindow.xaml.cs:778:        private static string FormatarMoedaComSinal(decimal valor)
PrimoAutoEletrica\Views\AjusteEstoqueWindow.xaml.cs:889:            decimal precoAnterior,
PrimoAutoEletrica\Views\AjusteEstoqueWindow.xaml.cs:890:            decimal precoNovo,
PrimoAutoEletrica\Views\AjusteEstoqueWindow.xaml.cs:893:            decimal valorInformado,
PrimoAutoEletrica\Views\ComissaoSettlementWindow.cs:151:            public decimal TotalServicos { get; set; }
PrimoAutoEletrica\Views\ComissaoSettlementWindow.cs:152:            public decimal TotalPecas { get; set; }
PrimoAutoEletrica\Views\ComissaoSettlementWindow.cs:155:            public decimal Comissao { get; set; }
PrimoAutoEletrica\Models\Agendamento.cs:31:        public decimal ClienteTotalGasto { get; set; }
PrimoAutoEletrica\Models\Agendamento.cs:59:        public decimal ValorEstimado { get; set; }
PrimoAutoEletrica\Models\Agendamento.cs:60:        public decimal ValorReal { get; set; }
PrimoAutoEletrica\Models\Agendamento.cs:61:        public decimal ValorPago { get; set; }
PrimoAutoEletrica\Models\Agendamento.cs:76:        public decimal ValorProdutos { get; set; }
PrimoAutoEletrica\Models\Agendamento.cs:80:        public decimal ValorServicos { get; set; }
PrimoAutoEletrica\Models\Agendamento.cs:124:        public decimal PrecoUnitario { get; set; }
PrimoAutoEletrica\Models\Agendamento.cs:125:        public decimal PrecoTotal { get; set; }
PrimoAutoEletrica\Models\Agendamento.cs:135:        public decimal Valor { get; set; }
PrimoAutoEletrica\Models\Agendamento.cs:237:        public decimal TotalGasto { get; set; }
PrimoAutoEletrica\Models\Agendamento.cs:263:        public decimal Valor { get; set; }
PrimoAutoEletrica\Models\AutoEletricaTecnica.cs:59:        public decimal ValorTotal { get; set; }
PrimoAutoEletrica\Models\AutoEletricaTecnica.cs:75:        public decimal ValorPadrao { get; set; }
PrimoAutoEletrica\Models\AutoEletricaTecnica.cs:88:        public decimal ValorMaoObra { get; set; }
PrimoAutoEletrica\Repositories\ClienteRepository.cs:1165:            decimal totalGasto = 0;
PrimoAutoEletrica\Models\CaixaOperacional.cs:14:        public decimal ValorAbertura { get; set; }
PrimoAutoEletrica\Models\CaixaOperacional.cs:15:        public decimal ValorEsperado { get; set; }
PrimoAutoEletrica\Models\CaixaOperacional.cs:16:        public decimal? ValorInformadoFechamento { get; set; }
PrimoAutoEletrica\Models\CaixaOperacional.cs:17:        public decimal TotalVendas { get; set; }
PrimoAutoEletrica\Models\CaixaOperacional.cs:18:        public decimal TotalSangrias { get; set; }
PrimoAutoEletrica\Models\CaixaOperacional.cs:19:        public decimal TotalSuprimentos { get; set; }
PrimoAutoEletrica\Models\CaixaOperacional.cs:40:        public decimal ValorMovimento { get; set; }
PrimoAutoEletrica\Models\CaixaOperacional.cs:41:        public decimal ValorInicial { get; set; }
PrimoAutoEletrica\Models\CaixaOperacional.cs:42:        public decimal ValorFinal { get; set; }
PrimoAutoEletrica\Views\EditarFuncionarioWindow.xaml.cs:225:                ShowError("O salario deve ser um valor numerico valido.");
PrimoAutoEletrica\Views\EditarProdutoWindow.xaml.cs:419:            var precoCompra = TryParseDecimalOrZero(PrecoCompraTextBox.Text, out var pc) ? pc : 0m;
PrimoAutoEletrica\Views\EditarProdutoWindow.xaml.cs:420:            var precoVenda = TryParseDecimalOrZero(PrecoVendaTextBox.Text, out var pv) ? pv : 0m;
PrimoAutoEletrica\Views\EditarProdutoWindow.xaml.cs:446:            var precoCompra = TryParseDecimalOrZero(PrecoCompraTextBox.Text, out var compra) ? compra : 0m;
PrimoAutoEletrica\Views\EditarProdutoWindow.xaml.cs:447:            var precoVenda = TryParseDecimalOrZero(PrecoVendaTextBox.Text, out var venda) ? venda : 0m;
PrimoAutoEletrica\Views\EditarProdutoWindow.xaml.cs:508:            if (!TryParseDecimalOrZero(PrecoCompraTextBox.Text, out precoCompra) || precoCompra < 0)
PrimoAutoEletrica\Views\EditarProdutoWindow.xaml.cs:513:            if (!TryParseDecimalOrZero(PrecoVendaTextBox.Text, out precoVenda) || precoVenda < 0)
PrimoAutoEletrica\Views\EditarProdutoWindow.xaml.cs:555:        private bool PrecoFoiAlterado(decimal precoCompra, decimal precoVenda)
PrimoAutoEletrica\Views\EditarProdutoWindow.xaml.cs:650:        private static decimal CalcularMargem(decimal precoCompra, decimal precoVenda)
PrimoAutoEletrica\Views\EditarProdutoWindow.xaml.cs:671:            public decimal PrecoCompra { get; set; }
PrimoAutoEletrica\Views\EditarProdutoWindow.xaml.cs:672:            public decimal PrecoVenda { get; set; }
PrimoAutoEletrica\Views\ImportarCatalogoPecasWindow.xaml.cs:82:                TotalSemNomeRealText.Text = _previewAtual.TotalSemNomeReal.ToString();
PrimoAutoEletrica\Repositories\ProdutoRepository.cs:341:                        PrecoCompra = decimal.TryParse(partes[4], out var precoCompra) ? precoCompra : 0,
PrimoAutoEletrica\Repositories\ProdutoRepository.cs:342:                        PrecoVenda = decimal.TryParse(partes[5], out var precoVenda) ? precoVenda : 0,
PrimoAutoEletrica\Repositories\ProdutoRepository.cs:376:            var auditorias = new List<(Guid ProdutoId, string Codigo, string Nome, decimal PrecoCompra, int EstoqueAnterior, int EstoqueNovo, int ReservadoAnterior, int ReservadoNovo, int Quantidade)>();
PrimoAutoEletrica\Repositories\ProdutoRepository.cs:469:        private static (string Codigo, string Nome, decimal PrecoCompra, int EstoqueAtual, int ReservadoNoAgendamento, int ReservasAtivas) ObterEstoqueAtual(
PrimoAutoEletrica\Views\HistoricoClienteWindow.xaml.cs:335:        private static decimal ConverterDecimal(object? valor)
PrimoAutoEletrica\Views\HistoricoClienteWindow.xaml.cs:343:                ? Convert.ToDecimal(valor, CultureInfo.InvariantCulture)
PrimoAutoEletrica\Views\HistoricoClienteWindow.xaml.cs:653:            public decimal Total { get; set; }
PrimoAutoEletrica\Views\HistoricoClienteWindow.xaml.cs:662:            public decimal Total { get; set; }
PrimoAutoEletrica\Models\Cliente.cs:36:        public decimal TotalGasto { get; set; }
PrimoAutoEletrica\Models\Fornecedor.cs:51:        public decimal TotalCompras { get; set; } = 0;
PrimoAutoEletrica\ViewModels\FinanceiroViewModel.cs:26:        public decimal Valor { get; set; }
PrimoAutoEletrica\ViewModels\FinanceiroViewModel.cs:39:        public decimal Valor { get; set; }
PrimoAutoEletrica\ViewModels\FinanceiroViewModel.cs:59:        private decimal _valor = 0;
PrimoAutoEletrica\ViewModels\FinanceiroViewModel.cs:60:        public decimal Valor
PrimoAutoEletrica\ViewModels\FinanceiroViewModel.cs:100:        public decimal Valor { get; set; }
PrimoAutoEletrica\ViewModels\FinanceiroViewModel.cs:122:        public decimal Valor { get; set; }
PrimoAutoEletrica\ViewModels\FinanceiroViewModel.cs:190:        private decimal _totalEntradas = 0;
```

## Recommended next (still no migrate)
1. Classify each double/float money field: UI-only vs persisted.
2. List SQLite column types for Precos/Valores (`.schema` dump).
3. Choose strategy: `decimal` CLR + INTEGER cents SQLite vs REAL stay + rounding policy.
4. Present risk map before any ALTER.
