# Design System - Primo Auto Eletrica

Documento oficial do design system do ERP Primo Auto Eletrica. Este guia consolida as regras de tema, componentes e experiencia de uso usadas pelas telas WPF.

## Direcao visual

- Identidade: oficina tecnica, eletrica automotiva, operacao rapida e confiavel.
- Paleta principal: laranja operacional (`PrimaryBrush`) sobre base slate/azul escuro.
- Tema claro: fundo `AppBackgroundBrush`, cards brancos, texto `PrimaryTextBrush`.
- Tema escuro: fundo `#07111F`, cards `#0F1B2E`, texto claro `#F8FAFC`.
- Regra de contraste: texto nunca deve usar cor fixa sobre fundo dinamico; preferir `PrimaryTextBrush`, `SecondaryTextBrush`, `InputForegroundBrush`, `AccentButtonTextBrush` e `InverseTextBrush`.

## Tokens

- Cores: `Themes/Colors.Light.xaml` e `Themes/Colors.Dark.xaml`.
- Tipografia: `Themes/Typography.xaml`, fonte padrao via `DefaultFontFamily`.
- Espacamento: `SpacingXS`, `SpacingSM`, `SpacingMD`, `SpacingLG`, `SpacingXL`, `Spacing2XL`, `Spacing3XL`.
- Radius: `CornerRadiusSM`, `CornerRadiusMD`, `CornerRadiusLG`, `CornerRadiusXL`, `CornerRadiusFull`.
- Sombras: `CardShadowEffect`, `SoftShadowEffect`, `MediumShadowEffect`, `StrongShadowEffect`, `PanelShadowEffect`.
- Densidade: `Themes/Density.xaml`, com `DisplayDensityService` alternando `Compact` e `Comfortable`.

## Componentes

- Headers: usar `ShellHeader`, `ShellHeaderTitle`, `ShellHeaderSubtitle`, `ShellHeaderInfoChip`, `PageActionBar`.
- Botoes: usar `PagePrimaryActionButton`, `PageSecondaryActionButton`, `PageSuccessActionButton`, `PageWarningActionButton`, `PageDangerActionButton`, `PageInfoActionButton`, `Compact*ActionButton`.
- Inputs: usar `PremiumTextBox`, `PremiumPasswordBox`, `PremiumComboBox`, `PremiumDatePicker`, `SearchBox`, `MultiLineTextBox`.
- Cards: usar `PremiumCard`, `MetricCard`, cards semanticos de info/sucesso/alerta/perigo.
- Tabelas: usar `PremiumDataGrid`, `ComfortableDataGrid` ou `CompactDataGrid`; grades de consulta devem ser `IsReadOnly=True`.
- Modais: usar estilos de `Themes/Modal.xaml`, com titulos, labels e botoes dinamicos.
- Badges/status: usar brushes de status e fundos semanticos (`InfoCardBackgroundBrush`, `SuccessCardBackgroundBrush`, `WarningCardBackgroundBrush`, `DangerCardBackgroundBrush`).

## Contraste

- Login: email/senha usam `PremiumTextBox` e `PremiumPasswordBox`; card, rodape e alertas usam recursos dinamicos.
- Placeholders e textos auxiliares devem usar `InputPlaceholderBrush` ou `MutedTextBrush`.
- TextBox, PasswordBox, ComboBox e DatePicker devem manter `InputForegroundBrush` sobre `InputBackgroundBrush`.
- DataGrid deve manter `PrimaryTextBrush` sobre `CardBackgroundBrush` ou `SurfaceAltBrush`; selecao usa `PrimaryBrush` e `InverseTextBrush`.
- Botoes secundarios usam `PrimaryTextBrush` sobre `SurfaceAltBrush`, com borda `BorderBrush`.

## Padrao de tela

- Titulo claro no topo, usando `ShellHeaderTitle` ou equivalente.
- Subtitulo quando houver contexto operacional.
- Acao principal a direita, usando botao primario.
- Acoes secundarias ao lado, usando botao secundario/compacto.
- Filtros dentro de `PageActionBar` ou card equivalente.
- Conteudo principal em cards ou grades com espacamento consistente.

## Modulos prioritarios

- Dashboard: cards metricos modernos, alertas importantes, atalhos rapidos e leitura executiva em poucos segundos.
- PDV: busca grande, carrinho evidente, total em destaque, botoes grandes e caminho curto para pagamento.
- OS: resumo do cliente/veiculo, status destacado, itens, servicos, totais, timeline e acoes rapidas.
- Orcamentos: leitura comercial clara, itens/servicos/totais, validade, aprovacao e compartilhamento.
- Relatorios: secoes com resumo executivo antes das grades e exportacoes rastreaveis.

## Densidade

- Modo confortavel: padrao premium, mais respiro visual, altura de controle `40`, linha de grade `38`.
- Modo compacto: mais dados por tela, altura de controle `34`, linha de grade `30`, padding menor.
- O shell possui botao `DensityToggleButton`, que alterna e persiste a preferencia em `density_settings.json`.
- Novas telas devem consumir `PageCardPadding`, `PageToolbarPadding`, `DensityControlHeight` e `DensityTableRowHeight` quando possivel.

## Regras de implementacao

- Evitar cores fixas em XAML de telas; excecao apenas para arte, graficos ou PDFs.
- Preferir `DynamicResource` para recursos visuais que mudam com tema/densidade.
- Novos controles de tela devem receber `x:Name` quando participarem de smoke tests.
- Toda nova grade operacional deve ter teste de carregamento, contraste/tema e, se for consulta, somente leitura.
- Exportacoes devem refletir as secoes principais mostradas na tela.
