# PRIMOX-I18N-USER-VISIBLE-SURFACE-MAP

**Missão:** PRIMOX-I18N-04 — Manual Multilingual UX Audit (Fase 1)  
**HEAD baseline:** `6036a40` (`docs(i18n): correct I18N-03 HEAD line without encoding corruption`)  
**Data:** 2026-09-10  
**Tipo de artefato:** **INVENTÁRIO** de superfícies user-visible — **não** é matriz PASS/FAIL

---

## Aviso crítico (métricas)

| Métrica | Valor | Interpretação |
|---------|------:|---------------|
| Static XAML coverage bound/(literal+bound) | **~19%** | Cobertura de **fonte estática** apenas |
| USER-VISIBLE COVERAGE | *a medir em I18N-04 runtime* | Superfícies auditadas PASS / superfícies testáveis |

**~19% NÃO é user-visible coverage.**  
Não usar o número estático como proxy de experiência multilíngue (pt-BR / en-US / es-ES).  
A métrica oficial de I18N-04 será preenchida após smoke/runtime e a matriz em `PRIMOX-I18N-04-UX-MATRIX.md`.

---

## 1. Scope & legend

**Source snapshot:** I18N-01/02/03 docs + `NavigationService` module map + `MainWindow` shell + `UserControls`/`Views` XAML.  
**Not claimed:** screenshots, live pt/en/es walk (I18N-03: NOT EXECUTED), or 100% string coverage.

| Column | Meaning |
|--------|---------|
| **Language testable** | `yes` = primary chrome/actions bound or `UiText` and expected to change with UICulture; `partial` = localized chrome mixed with hardcoded pt-BR body/labels/dialogs |
| **Criticality** | `critical` = auth, money, fiscal, OS/PDV/estoque/clientes path; `secondary` = help, kanban polish, rare admin dialogs |
| **How to access** | Sidebar `Tag` / `NavegarPara` id, or window open path |

**Stable navigation IDs** (`PrimoAutoEletrica/Services/NavigationService.cs`):  
`Dashboard`, `Clientes`, `Veiculos`, `AutoEletricaTecnica`, `Orcamentos`, `OrdensServico`, `OficinaKanban`, `PDV`, `Estoque`, `CatalogoPecas`, `ImportarNFe`, `FiscalOperacoes` (+ alias `OperacoesFiscais`), `Financeiro`, `Fornecedores`, `Funcionarios`, `Agendamentos`, `Relatorios`, `Help` (+ alias `Ajuda`).  
**Configurações** is **not** a cached module page — sidebar opens `ConfiguracoesSistemaWindow`.

---

## 2. Shell & chrome

| ID | Module | File/origin | How to access | Type | Expected visible text categories | Language testable | Criticality |
|----|--------|-------------|---------------|------|----------------------------------|-------------------|-------------|
| SURF-SHELL-01 | Shell | `MainWindow.xaml` / `.cs` | App post-login; window title `PRIMOX` | page | Window title; layout chrome | partial | critical |
| SURF-SHELL-02 | Sidebar | `MainWindow.xaml` | Always visible left rail | page | Brand "PRIMOX"; section headers (`SectionOperation`/`Registers`/`Management`/`System`/`Help`); menu labels; Session panel; collapse tooltip (literal "Recolher menu") | partial | critical |
| SURF-SHELL-03 | Header | `MainWindow.xaml` / `.cs` | Top bar | page | Brand; `CurrentModuleText` (localized module title); Back/Refresh; nav state chip; user/theme/density | partial | critical |
| SURF-SHELL-04 | Language selector | `MainWindow.xaml` (`LanguageComboBox`); also on Login | Header combo / Login combo | filter | ToolTip `SelectLanguage`; items Português / English / Español (`pt-BR`/`en-US`/`es-ES`) | yes | critical |
| SURF-SHELL-05 | Global search | `UserControls/GlobalSearchControl.xaml` + `MainWindow` | Header; Ctrl+F | filter | Placeholder/results titles from search service; clear tooltip (pt literal); empty/no-destination toast | partial | secondary |
| SURF-SHELL-06 | Command Palette | `Views/CommandPaletteWindow.xaml`; `MainWindow.AbrirCommandPalette` | Ctrl+K / Command Center button | palette | Title "COMMAND CENTER"; subtitle (pt); filter box; command titles/subtitles; Ctrl+K badge | partial | secondary |
| SURF-SHELL-07 | Shell toast | `MainWindow.xaml` (`ShellNotification*`); `ShellNotificationService` | Event-driven overlay | toast | Title/message/details/action; default titles via `UiText` (`AccessDenied`, `ModuleUnavailable`, `SearchNoDestination`, backup keys) — message bodies often still caller pt-BR | partial | critical |
| SURF-SHELL-08 | Confirm exit | `MainWindow.xaml.cs` | Close main window | messagebox | `UiText.T("ConfirmExit")` + Yes/No | yes | critical |
| SURF-SHELL-09 | Theme / Density | `MainWindow.xaml` | Header buttons | tooltip | `Theme`, `Comfort` (and related) bound labels | yes | secondary |

---

## 3. Auth & bootstrap windows

| ID | Module | File/origin | How to access | Type | Expected visible text categories | Language testable | Criticality |
|----|--------|-------------|---------------|------|----------------------------------|-------------------|-------------|
| SURF-LOGIN-01 | Login | `Views/LoginWindow.xaml`; `ViewModels/LoginViewModel.cs` | App start | login | Brand; "Entrar"; Email/Senha; Remember; Show password; footer; language combo; errors via `UiText` (`LoginFailed` etc.) — most chrome still pt literals | partial | critical |
| SURF-AUTH-02 | First run | `Views/PrimeiraExecucaoWindow.xaml` | First install / setup | form | Initial config title/labels/buttons (pt) | partial | critical |
| SURF-AUTH-03 | License | `Views/LicenseActivationWindow.xaml` | License gate | form | Activation title/fields/actions (pt) | partial | critical |
| SURF-AUTH-04 | Branch select | `Views/SelecaoFilialWindow.xaml` | Multi-branch login path | modal | "Selecionar Filial"; list/actions | partial | secondary |
| SURF-AUTH-05 | Forced password | `Views/TrocarSenhaObrigatoriaWindow.xaml` | Temp password policy | form | Title/fields/confirm (pt) | partial | critical |
| SURF-AUTH-06 | 2FA setup | `Views/TwoFactorSetupWindow.xaml` | Security setup | form | 2FA title/instructions/buttons (pt) | partial | secondary |
| SURF-AUTH-07 | System update | `Views/AtualizacaoWindow.xaml` | Update check | modal | Update title/status; some `UiText` titles | partial | secondary |
| SURF-AUTH-08 | Friendly error | `Views/FriendlyErrorWindow.xaml` | Unhandled/friendly error path | modal | "Erro registrado"; message/details | partial | secondary |

---

## 4. Navigable module pages

| ID | Module | File/origin | How to access | Type | Expected visible text categories | Language testable | Criticality |
|----|--------|-------------|---------------|------|----------------------------------|-------------------|-------------|
| SURF-DASH-01 | Dashboard | `UserControls/DashboardControl.xaml`; `ViewModels/DashboardViewModel.cs` | Sidebar `Dashboard` | page | `OperationsCenter`; Refresh; loading/error keys; KPI labels; shortcut buttons (`AppointmentsTitle`, `PdvTitle`, …); subtitle/`UpdatedAt` via `UiText` (I18N-03) — some pulse/detail still pt | partial | critical |
| SURF-CLI-01 | Clientes | `UserControls/ClientesControl.xaml` / `.cs` | Sidebar `Clientes` | page | Localized title/subtitle/actions/KPIs/grid headers/empty; filters; MessageBox via `UiText` (stronger than most modules) | yes | critical |
| SURF-VEI-01 | Veículos | `UserControls/VeiculosControl.xaml` / `.cs` | Sidebar `Veiculos` | page | Page title/subtitle/actions **pt literals**; some common buttons bound; loading/empty/error **pt**; MessageBox titles partial | partial | critical |
| SURF-TEC-01 | Auto elétrica técnica | `UserControls/AutoEletricaTecnicaControl.xaml` | Sidebar `AutoEletricaTecnica` | page | Section titles/labels mostly **pt**; header module name localized in shell only | partial | secondary |
| SURF-ORC-01 | Orçamentos | `UserControls/OrcamentosControl.xaml` (+ nested `Orcamento*Control.xaml`) | Sidebar `Orcamentos` | page | Action bar mostly pt; CRUD commons bound (`NewQuote`, `Approve`, …); status badges pt; empty client via `UiText`; nested panels mixed | partial | critical |
| SURF-OS-01 | Ordens de serviço | `UserControls/OrdensServicoControl.xaml` / `.cs` | Sidebar `OrdensServico` | page | Title/subtitle/actions **pt**; `NewOs` etc. bound; status filter Tag internal + localized display (I18N-02); MessageBox partial | partial | critical |
| SURF-KAN-01 | Oficina Kanban | `UserControls/OficinaKanbanControl.xaml` | Sidebar `OficinaKanban` | page | "Kanban da oficina" + board labels **pt**; shell title localized | partial | secondary |
| SURF-PDV-01 | PDV | `UserControls/PDVControl.xaml` (+ `.Pagamento`/`.Produtos`/`.Helpers`.cs) | Sidebar `PDV` | page | Cart/payment/cash buttons bound; many labels (CLIENTE/CAIXA/SUBTOTAL) **pt**; MessageBox partial | partial | critical |
| SURF-EST-01 | Estoque | `UserControls/EstoqueControl.xaml` / `.cs` | Sidebar `Estoque` | page | `InventoryTitle` + common actions/headers bound; loading/empty titles often **pt**; `ProductDeleted` etc. via `UiText` | partial | critical |
| SURF-CAT-01 | Catálogo de peças | `UserControls/CatalogoPecasControl.xaml` | Sidebar `CatalogoPecas` | page | Title/subtitle/import actions **pt**; MessageBox titles often module-ID style | partial | secondary |
| SURF-NFE-01 | Importar NF-e | `UserControls/ImportarNFeControl.xaml` / `.cs` | Sidebar `ImportarNFe` | page | Titles/KPIs/history **pt**; empty keys partial (`UiText`); opens import window | partial | critical |
| SURF-FIS-01 | Operações fiscais | `UserControls/FiscalOperationsControl.xaml` | Sidebar `FiscalOperacoes` | page | "Operações Fiscais (Homologação)" + emitente form **pt**; some field labels bound (`Cnpj`, `Refresh`); fiscal acronyms intentional | partial | critical |
| SURF-FIN-01 | Financeiro | `UserControls/FinanceiroControl.xaml` / `.cs` | Sidebar `Financeiro` | page | `FinanceTitle` + some commons; action/loading/empty mostly **pt**; MessageBox titles partial | partial | critical |
| SURF-FOR-01 | Fornecedores | `UserControls/FornecedoresControl.xaml` / `.cs` | Sidebar `Fornecedores` | page | Action bar **pt**; commons/buttons mixed; `SupplierDeleted` + titles via `UiText` | partial | secondary |
| SURF-FUN-01 | Funcionários | `UserControls/FuncionariosControl.xaml` / `.cs` | Sidebar `Funcionarios` | page | "Equipe e acessos" and detail panels **pt**; MessageBox/critical confirms mixed | partial | secondary |
| SURF-AGE-01 | Agenda | `UserControls/AgendamentosControl.xaml`; `ViewModels/AgendamentosViewModel.cs` | Sidebar `Agendamentos` | page | `AppointmentsTitle` + KPIs/commons bound; many filters/calendar/empty/loading **pt**; `UiText` for some confirms | partial | critical |
| SURF-REL-01 | Relatórios | `UserControls/RelatoriosControl.xaml` | Sidebar `Relatorios` | page | `ReportsTitle` + some headers; large report bodies/filters often **pt** | partial | secondary |
| SURF-CFG-01 | Configurações | `Views/ConfiguracoesSistemaWindow.xaml` / `.cs` | Sidebar `Configuracoes` (modal, not `Navigate`) | modal | Window title pt; tabs: `General`/`Backup` bound, others literal ("Comercial…", "Operacao", "Banco de Dados", "Multiusuario…"); forms mixed | partial | critical |
| SURF-HELP-01 | Ajuda | `UserControls/HelpControl.xaml` | Sidebar `Help` / `Ajuda` | page | `HelpTitle` + module nav tree bound; **help body CONTENT PARTIAL (pt-BR)** | partial | secondary |

---

## 5. Major dialogs / windows (`Views/`)

Grouped by originating module. Titles below are **XAML window titles as shipped** (mostly pt literals → language walk = **partial** unless noted).

### 5.1 Clientes / veículos / OS / orçamentos

| ID | Module | File/origin | How to access | Type | Expected visible text categories | Language testable | Criticality |
|----|--------|-------------|---------------|------|----------------------------------|-------------------|-------------|
| SURF-DLG-CLI-01 | Clientes | `Views/Clientes/NovoClienteWindow.xaml` | Clientes → Novo | form | Title "Novo Cliente"; fields/buttons; validation MessageBox | partial | critical |
| SURF-DLG-CLI-02 | Clientes | `Views/Clientes/EditarClienteWindow.xaml` | Clientes → Editar | form | "Editar Cliente"; fields; critical confirm | partial | critical |
| SURF-DLG-CLI-03 | Clientes | `Views/Clientes/VisualizarClienteWindow.xaml` | Clientes → visualizar | modal | Relationship profile title; history panels | partial | secondary |
| SURF-DLG-CLI-04 | Clientes | `Views/HistoricoClienteWindow.xaml` | Cliente history action | table | History title; grids | partial | secondary |
| SURF-DLG-VEI-01 | Veículos | `Views/NovoVeiculoWindow.xaml` | Veículos → Novo | form | "Novo Veiculo"; form labels | partial | critical |
| SURF-DLG-VEI-02 | Veículos | `Views/VisualizarVeiculoWindow.xaml` | Veículos → visualizar | modal | "Prontuário Técnico — Veículo" | partial | secondary |
| SURF-DLG-OS-01 | OS | `Views/OrdemServicoWindow.xaml` | OS → Nova/abrir dossiê | form | "Dossiê Técnico — Ordem de Serviço"; tabs/fields/actions | partial | critical |
| SURF-DLG-ORC-01 | Orçamentos | `Views/NovoOrcamentoWindow.xaml` | Orcamentos → Novo | form | "Novo Orcamento"; nested panels | partial | critical |
| SURF-DLG-ORC-02 | Orçamentos | `Views/SelecionarOrcamentoWindow.xaml` | Carteira completa | table | "Carteira completa de orcamentos"; filters/grid | partial | secondary |
| SURF-DLG-ORC-03 | Orçamentos | `Views/OrcamentosView.xaml` | Alternate/central quotes host | page | "Orcamentos - Central Inteligente" | partial | secondary |
| SURF-DLG-AGE-01 | Agenda | `Views/NovoAgendamentoPremiumWindow.xaml` | Agenda → Novo | form | "Novo Agendamento"; form + VM `UiText` | partial | critical |
| SURF-DLG-SIG-01 | OS/Docs | `Views/AssinaturaDigitalWindow.xaml` | Signature capture flows | modal | "Assinatura digital" | partial | secondary |

### 5.2 PDV / estoque / catálogo / fornecedores / funcionários

| ID | Module | File/origin | How to access | Type | Expected visible text categories | Language testable | Criticality |
|----|--------|-------------|---------------|------|----------------------------------|-------------------|-------------|
| SURF-DLG-PDV-01 | PDV | `Views/SelecionarClientePDVWindow.xaml` | PDV F5 / buscar cliente | filter | "Selecionar cliente"; search/grid | partial | critical |
| SURF-DLG-PDV-02 | PDV | `Views/SelecionarProdutoPDVWindow.xaml` | PDV F1 / produtos | filter | "Selecionar produto" | partial | critical |
| SURF-DLG-PDV-03 | PDV | `Views/PagamentoMistoWindow.xaml` | PDV mixed payment | form | "Pagamento misto" | partial | critical |
| SURF-DLG-PDV-04 | PDV | `Views/OperacaoCaixaWindow.xaml` | Open/close cash | form | "Operacao de caixa" | partial | critical |
| SURF-DLG-PDV-05 | PDV | `Views/SelecionarVendaWindow.xaml` | Suspended/recall sale | table | "Selecionar venda" | partial | secondary |
| SURF-DLG-EST-01 | Estoque | `Views/NovoProdutoWindow.xaml` | Estoque → Novo | form | "Novo Produto"; validation `UiText` titles | partial | critical |
| SURF-DLG-EST-02 | Estoque | `Views/EditarProdutoWindow.xaml` | Estoque → Editar | form | "Editar Produto"; critical confirm | partial | critical |
| SURF-DLG-EST-03 | Estoque | `Views/AjusteEstoqueWindow.xaml` | Stock adjust | form | "Ajuste de Estoque" | partial | critical |
| SURF-DLG-EST-04 | Estoque | `Views/HistoricoEstoqueWindow.xaml` | Stock history | table | "Historico de estoque" | partial | secondary |
| SURF-DLG-CAT-01 | Catálogo | `Views/ImportarCatalogoPecasWindow.xaml` | Catalogo → Importar | form | "Importar Catalogo de Pecas" | partial | secondary |
| SURF-DLG-CAT-02 | Catálogo | `Views/RevisarCatalogoPecaWindow.xaml` | Catalog item review | form | "Revisar Item do Catalogo" | partial | secondary |
| SURF-DLG-FOR-01 | Fornecedores | `Views/NovoFornecedorWindow.xaml` | Fornecedores → Novo | form | "Novo Fornecedor" | partial | secondary |
| SURF-DLG-FOR-02 | Fornecedores | `Views/EditarFornecedorWindow.xaml` | Editar | form | "Editar Fornecedor" | partial | secondary |
| SURF-DLG-FOR-03 | Fornecedores | `Views/VisualizarFornecedorWindow.xaml` | Visualizar | modal | "Visualizar Fornecedor" | partial | secondary |
| SURF-DLG-FOR-04 | Fornecedores | `Views/AdicionarFornecedorDialog.xaml` | NF-e / missing supplier | modal | "Fornecedor nao cadastrado" | partial | secondary |
| SURF-DLG-FUN-01 | Funcionários | `Views/NovoFuncionarioWindow.xaml` | Funcionarios → Novo | form | "Novo Funcionario…" | partial | secondary |
| SURF-DLG-FUN-02 | Funcionários | `Views/EditarFuncionarioWindow.xaml` | Editar | form | "Editar Funcionario…" | partial | secondary |
| SURF-DLG-FUN-03 | Funcionários | `Views/GerenciarPerfisWindow.xaml` | Profiles admin | table | "Gerenciar Perfis…" | partial | secondary |
| SURF-DLG-FUN-04 | Funcionários | `Views/NovoPerfilWindow.xaml` | New profile | form | "Novo Perfil…" | partial | secondary |
| SURF-DLG-FUN-05 | Funcionários | `Views/ConfigurarPermissoesWindow.xaml` | Permissions | form | "Configurar Permissoes…"; critical deletes | partial | critical |
| SURF-DLG-FUN-06 | Funcionários | `Views/UsuariosOnlineWindow.xaml` | Online users | table | "Usuarios Online…" | partial | secondary |

### 5.3 Fiscal / config / system dialogs

| ID | Module | File/origin | How to access | Type | Expected visible text categories | Language testable | Criticality |
|----|--------|-------------|---------------|------|----------------------------------|-------------------|-------------|
| SURF-DLG-NFE-01 | NF-e | `Views/ImportarNotaWindow.xaml` | ImportarNFe → Nova importação | form | "Importar NF-e"; wizard steps; cancel via critical dialog | partial | critical |
| SURF-DLG-CFG-02 | Configurações | `Views/Configuracoes/ResetSistemaWindow.xaml` | Settings → reset data | modal | "Zerar Dados do Sistema"; destructive confirm | partial | critical |
| SURF-DLG-SYS-01 | Shell | `Views/ConfirmacaoCriticaWindow.xaml`; `CriticalActionDialogService` | Deletes/destructive actions across modules | modal | "Confirmacao critica"; title/body/confirm often **mixed pt** (I18N-03 limitation) | partial | critical |
| SURF-DLG-SYS-02 | Shell | `Views/AtalhosTecladoWindow.xaml` | Shortcuts help | modal | "Atalhos do teclado" | partial | secondary |
| SURF-DLG-SYS-03 | Shell | `Views/CommandPaletteWindow.xaml` | See SURF-SHELL-06 | palette | (same) | partial | secondary |

---

## 6. Interaction surfaces (I18N-03 inventory points)

Not separate "screens", but **user-visible** interaction classes to audit per language.

| ID | Module | File/origin | How to access | Type | Expected visible text categories | Language testable | Criticality |
|----|--------|-------------|---------------|------|----------------------------------|-------------------|-------------|
| SURF-INT-01 | Cross-cutting | `Helpers/UiText.cs` + `LocalizationService.Interaction.cs`; ~269 `UiText.T(`; ~237 `MessageBox.Show` | CRUD/validation/errors in modules | messagebox | Common titles `Error`/`Success`/`Warning`/`AccessDenied`/`Information` **FORTE** (~184); many **bodies still pt-BR** | partial | critical |
| SURF-INT-02 | Cross-cutting | Same + module code-behinds | Delete/approve/exit confirms | messagebox | `ConfirmExit`, `ConfirmDelete*`, `ConfirmDeleteRecord`; module-ID titles often remain PT IDs | partial | critical |
| SURF-INT-03 | Shell | `MainWindow` + `ShellNotificationService` | Permission/nav/search/backup toasts | toast | Localized default titles; message/details often caller-owned pt | partial | critical |
| SURF-INT-04 | Modules | Empty-state panels in Controls (Clientes bound; Agenda/Estoque/Financeiro/Veículos often literal) | Empty datasets | empty | `NoClientsRegistered`, `NoRecordsFound`, quote-without-client, NF-e empty — **catalog partial**; many XAML empties still pt | partial | secondary |
| SURF-INT-05 | Modules | Loading panels (Dashboard bound; Agenda/Estoque/Financeiro/Veículos literal) | Slow loads | loading | `Loading`, `LoadingOperationalData`, `UpdatedAt`, `UpdateFailed` vs literal "Carregando…" | partial | secondary |
| SURF-INT-06 | Modules | Validation MessageBoxes / field errors | Save forms | form | `RequiredField`, qty/image/attachment keys **partial** | partial | critical |
| SURF-INT-07 | Shell/modules | XAML `ToolTip` / `AutomationProperties` | Hover / a11y | tooltip | Many shell/module tooltips bound (I18N-02); plenty of literal tooltips remain (e.g. OS New, search clear) | partial | secondary |
| SURF-INT-08 | Destructive UX | `CriticalActionDialogService` / `ConfirmacaoCriticaWindow` | High-risk confirms | modal | Texts **still mixed** (documented I18N-03 limitation) | partial | critical |

### Representative `UiText` hotspots (by area)

| Area | Primary files (examples) | Notes for language audit |
|------|--------------------------|--------------------------|
| Shell | `MainWindow.xaml.cs` | ConfirmExit; AccessDenied/ModuleUnavailable/SearchNoDestination toasts |
| Login | `LoginViewModel.cs` | LoginFailed / titles |
| Dashboard | `DashboardViewModel.cs` | Subtitle format; UpdatedAt; CannotLoadIndicators |
| Clientes | `ClientesControl.xaml.cs`; Novo/Editar cliente windows | Stronger title coverage; WhatsApp/contact keys |
| Estoque | `EstoqueControl.xaml.cs`; Novo/Editar/Ajuste produto | ProductDeleted; validation titles |
| OS / Orçamentos / PDV / Agenda / Fiscal / Config | respective `*.xaml.cs` | Titles often OK; bodies/dialogs still pt-heavy |

---

## 7. Coverage matrix (minimum modules requested)

| Required surface | Surface IDs | Nav / access key | Overall language testable |
|------------------|-------------|------------------|---------------------------|
| Shell | SURF-SHELL-01 | MainWindow | partial |
| Sidebar | SURF-SHELL-02 | menu Tags | **yes** (labels); collapse tooltip partial |
| Header | SURF-SHELL-03 | top bar | partial |
| Language selector | SURF-SHELL-04 | `LanguageComboBox` | **yes** |
| Command Palette | SURF-SHELL-06 | Ctrl+K | partial |
| Login | SURF-LOGIN-01 | startup | partial |
| Dashboard | SURF-DASH-01 | `Dashboard` | partial |
| Clientes | SURF-CLI-01 + DLG-CLI-* | `Clientes` | **yes** (page chrome); dialogs partial |
| Veiculos | SURF-VEI-01 + DLG-VEI-* | `Veiculos` | partial |
| OS | SURF-OS-01 + DLG-OS-01 | `OrdensServico` | partial |
| Orcamentos | SURF-ORC-01 + DLG-ORC-* | `Orcamentos` | partial |
| PDV | SURF-PDV-01 + DLG-PDV-* | `PDV` | partial |
| Estoque | SURF-EST-01 + DLG-EST-* | `Estoque` | partial |
| Financeiro | SURF-FIN-01 | `Financeiro` | partial |
| Fornecedores | SURF-FOR-01 + DLG-FOR-* | `Fornecedores` | partial |
| Funcionarios | SURF-FUN-01 + DLG-FUN-* | `Funcionarios` | partial |
| Agenda | SURF-AGE-01 + DLG-AGE-01 | `Agendamentos` | partial |
| Relatorios | SURF-REL-01 | `Relatorios` | partial |
| Catalogo | SURF-CAT-01 + DLG-CAT-* | `CatalogoPecas` | partial |
| NF-e / Fiscal | SURF-NFE-01, SURF-FIS-01, DLG-NFE-01 | `ImportarNFe` / `FiscalOperacoes` | partial |
| Configuracoes | SURF-CFG-01 + DLG-CFG-02 | sidebar → window | partial |
| Help | SURF-HELP-01 | `Help` | partial (nav yes / body no) |

**Also present in sidebar (include in full audit):** `AutoEletricaTecnica` (SURF-TEC-01), `OficinaKanban` (SURF-KAN-01).

---

## 8. Honest counts (inventory, not PASS/FAIL)

| Bucket | Approx. surfaces listed | Language testable = yes | = partial |
|--------|------------------------:|------------------------:|----------:|
| Shell / chrome | 9 | 3 | 6 |
| Auth / bootstrap | 8 | 0 | 8 |
| Module pages | 18 | 1 (Clientes chrome) | 17 |
| Major dialogs | ~40 | 0 | ~40 |
| Interaction classes | 8 | 1 (ConfirmExit-class titles) | 7 |

**Interpretation for I18N-04 USER-VISIBLE COVERAGE:** treat almost all module **bodies** and **Views/** dialogs as **partial**; only shell menu labels, language control, and a few module chromes (notably Clientes + Dashboard header metrics) are realistically **yes** without expecting residual pt-BR text.

---

## 9. Próximo passo

Preencher `Docs/qa/PRIMOX-I18N-04-UX-MATRIX.md` após I18n04 smoke/runtime (pt-BR / en-US / es-ES).  
Consultar glossário oficial: `Docs/qa/PRIMOX-I18N-TRANSLATION-GLOSSARY.md`.
