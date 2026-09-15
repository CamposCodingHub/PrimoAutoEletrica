# PRIMOX — ComboBox seleção / IsEditable audit (2026-09-15)

## Problema reportado
Em **Veículos → Novo Veículo**, Marca/Modelo selecionados **não apareciam** no campo (Light e Dark).

## Causa raiz
`Themes/Inputs.xaml` → `PremiumComboBox` **não tinha `PART_EditableTextBox`**.  
Com `IsEditable="True"`, o WPF exige esse PART; sem ele a seleção/dropdown não popula o texto visível.

## Escopo afetado (6 controles)
| Tela | Controles |
|------|-----------|
| NovoVeiculoWindow | MarcaComboBox, ModeloComboBox |
| NovoProdutoWindow | CategoriaComboBox, UnidadeMedidaComboBox |
| EditarProdutoWindow | CategoriaComboBox, UnidadeMedidaComboBox |

## Inventário
- ~84 ComboBoxes no app; maioria Premium implícito
- Correções adicionais: RelatorioComboBox → BasedOn Premium; Fornecedores filtros sem override InverseTextBrush; OperationalListBox Selected Foreground

## Correções
1. Template PremiumComboBox com PART_EditableTextBox + trigger IsEditable
2. PremiumComboBoxItem MultiTrigger Selected+Highlighted
3. Relatorios/Fornecedores alinhados ao Design System
4. Smoke `DefinirComboBoxTexto` valida Text após seleção em editáveis

## Teste
- Unit + Build Release
- Smoke filter `Veiculos` (usa Marca/Modelo editáveis)
