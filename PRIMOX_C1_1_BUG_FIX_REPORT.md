# PRIMOX C1.1 — Bug Fix Report

## Identificação
Ciclo C1.1 | Branch cycle-c1/operational-intelligence | Base 8a397fe

## Baseline
QA Full Functional: 199/206 UI checks PASS, 7 FAIL → 3 bugs nomeados.

## BUG-001
**Sintoma:** XamlParseException / ResourceReferenceKeyNotFoundException ModernTabControl/ModernTabItem em BaseConhecimento e NecessidadesCompra.
**Causa:** estilos ausentes do Design System merge (Tabs.xaml inexistente/não mergeado).
**Correção:** Themes/Tabs.xaml + GlobalStyles MergedDictionaries.
**Testes:** C1BugFixRegressionTests Bug001_*; UiSmoke Controle/Interacao BaseConhecimento + NecessidadesCompra PASS.
**Status:** FIXED

## BUG-002
**Sintoma:** Admin negado ESTOQUE_CRIAR (Tipo=Modulo) no CatalogoPecasViewModel.
**Causa:** TemPermissao = módulo; ESTOQUE_CRIAR = código de ação → API errada no call site.
**Correção:** TemPermissaoCodigo("ESTOQUE_CRIAR").
**Testes:** Admin true / Visualizador false / TemPermissao("ESTOQUE_CRIAR") false / VM PodeCriarProduto.
**Status:** FIXED (fail-closed preservado)

## BUG-003
**Sintoma:** NRE AplicarFiltros durante InitializeComponent (SelectionChanged).
**Causa:** evento de filtro antes da árvore completa.
**Correção:** _isInitialized + early return; load no Loaded.
**Testes:** Bug003 STA init; UiSmoke FerramentasControl PASS.
**Status:** FIXED

## Ocorrências semelhantes
Ver seção Final §7.

## Arquivos alterados
Tabs.xaml, GlobalStyles.xaml, CatalogoPecasViewModel.cs, FerramentasControl.xaml.cs, testes C1, docs/evidências.

## Resultado
3/3 FIXED | Build PASS | xUnit PASS | UiSmoke 206 PASS | E2E 42 PASS | Protected DB intact