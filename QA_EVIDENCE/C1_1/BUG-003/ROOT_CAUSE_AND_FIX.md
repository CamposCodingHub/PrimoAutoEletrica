# BUG-003 Evidence — FerramentasControl NRE

## Reproducao
NullReferenceException em AplicarFiltros / FiltroCombo_SelectionChanged durante InitializeComponent

## Causa raiz
SelectionChanged do Combo dispara durante parse XAML, antes de InitializeComponent concluir e antes de FerramentasDataGrid existir.
_isInitialized so era true apos InitializeComponent — handlers precisam ignorar eventos pre-init.

## Correcao
1. Flag _isInitialized = false; true somente apos InitializeComponent.
2. FiltroCombo_SelectionChanged / BuscaTextBox_TextChanged: return se !_isInitialized.
3. AplicarFiltros: guard !_isInitialized || FerramentasDataGrid == null.
4. Carregamento de dados no Loaded (apos arvore pronta).

Nao e catch vazio; e ordenacao correta de ciclo de vida WPF.

## Testes
Bug003_FerramentasControl_DeveInicializarSemNullReferenceException (STA)

## Status
FIXED
