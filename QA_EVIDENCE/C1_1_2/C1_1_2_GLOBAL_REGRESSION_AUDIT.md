# C1.1.2 Global Regression Audit

Data: 2026-09-26 07:46:46 America/Sao_Paulo
Branch: cycle-c1/operational-intelligence
Commit (working tree dirty until final commit): 83b3ffa39203b327342c269f78acc149e11aa2b7

## BUG-001 ModernTabControl / ModernTabItem
- Themes/Tabs.xaml presente no fonte
- GlobalStyles.xaml merge: ResourceDictionary Source="Tabs.xaml"
- DLL instalada pos-deploy contem string ModernTabControl: SIM
- Tabs.xaml solto na pasta App: NAO (esperado se Resource/embedded no assembly)
- Controles afetados historicamente: BaseConhecimentoControl.xaml, NecessidadesCompraControl.xaml
- Busca StaticResource ModernTab*: revisar outros XAML que usam o estilo

## BUG-002 TemPermissao vs TemPermissaoCodigo
- CatalogoPecasViewModel: TemPermissaoCodigo("ESTOQUE_CRIAR") — OK
- TemPermissao("...") com codigo de permissao: nenhuma ocorrencia de ESTOQUE_* em TemPermissao(
- TemPermissao("Financeiro") em UiSmoke Login = API de modulo (esperado)

## BUG-003 Ferramentas NRE
- _isInitialized guard em AplicarFiltros e SelectionChanged presente no fonte e na DLL instalada

## BUG-004 MoneyIO DBNull (Orçamentos / Kanban)
- Causa raiz: MaterializarOrcamento index 16 = Acrescimo; coluna INTEGER NULLABLE com ~300/325 NULL no operacional
- Desconto/LucroEstimado/ComissaoVendedor/ImpostosEstimados tambem NULLABLE com muitos NULL
- Dominio C#: decimal nao-anulavel; ausencia = 0
- Correcao: MoneyIO.LerMoedaOpcionalOuZero + ReadOptionalMoney nesses campos; Subtotal/Total permanecem LerMoeda fail-closed
- Justificativa semantica alinhada a P2_3_MONEY_NULL_POLICY para colunas NULLABLE mapeadas a decimal de dominio

## Protected DB
- SHA esperado: C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B
- Pos-deploy: CONFIRMADO intacto
