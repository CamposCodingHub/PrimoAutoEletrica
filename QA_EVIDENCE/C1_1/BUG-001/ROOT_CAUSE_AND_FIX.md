# BUG-001 Evidence — ModernTabControl / ModernTabItem

## Reproducao (pre-fix, documentada no Full Functional QA)
XamlParseException / ResourceReferenceKeyNotFoundException
Recursos: ModernTabControl, ModernTabItem
Telas: BaseConhecimentoControl.xaml, NecessidadesCompraControl.xaml

## Causa raiz
Estilos referenciados via StaticResource mas ausentes dos MergedDictionaries.
Themes/Tabs.xaml nao existia / nao estava mergeado em GlobalStyles.xaml.
ThemeService so troca Colors.*; GlobalStyles (e Tabs) permanecem — merge em GlobalStyles e suficiente.

## Correcao
1. Criado PrimoAutoEletrica/Themes/Tabs.xaml com x:Key ModernTabControl e ModernTabItem (Light/Dark via DynamicResource).
2. GlobalStyles.xaml: MergedDictionaries += Tabs.xaml

## Ocorrencias
Somente BaseConhecimentoControl + NecessidadesCompraControl usam esses keys.
Tabs.xaml define ambos.

## Testes
C1BugFixRegressionTests:
- Bug001_TabsResourceDictionary_DeveConterEstilosModernTab
- Bug001_BaseConhecimentoControl_DeveInicializarSemXamlParseException
- Bug001_NecessidadesCompraControl_DeveInicializarSemXamlParseException

## Status
FIXED (construcao STA + build). Validacao visual Light/Dark/resolucao: ver UiSmoke/manual.
