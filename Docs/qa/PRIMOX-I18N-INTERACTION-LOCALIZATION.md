# PRIMOX-I18N-INTERACTION-LOCALIZATION — PRIMOX-I18N-03-2026-09

**Data:** 10/09/2026  
**Missão:** localizar textos de **interação** (MessageBox, confirmações, toasts, validações, empty/loading/error, tooltips/ações) sem segunda arquitetura.  
**HEAD inicial:** `706230b`  
**HEAD final:** `de6b72e` (`feat(i18n): localize interaction and dialog messages`)  
**Tag `v1.0.0`:** preservada (`72d85fa`) — **não movida**

---

## 1. Baseline

| Item | Valor |
|------|------:|
| HEAD inicial | `706230b` (I18N-02 docs) |
| Tag v1.0.0 | `72d85fa` |
| XAML literais UI | **2231** |
| LocHelper bound attrs | **525** |
| Cobertura XAML attrs | **~19%** |
| Idiomas core | pt-BR / en-US / es-ES |
| CurrentCulture negócio | **pt-BR** (inalterado) |

Ferramenta: `Scripts/Audit-I18nCoverage.ps1`

---

## 2. Diagnóstico

- Centenas de `MessageBox.Show` / `WindowInteractionHelper.ShowMessage` com títulos/corpos em pt-BR.
- Títulos comuns (`Erro`, `Sucesso`, `Aviso`, `Acesso negado`, `Validação`) já existiam no catálogo core mas **não** eram usados nos diálogos.
- Toasts (`ShellNotificationService`) e vários empty/loading states ainda hardcoded.
- **Classificação:** somente strings **A** (UI traduzível). Identificadores de navegação, auditoria, paths e SQL (**C/E**) **não** devem ser traduzidos.

### Incidente controlado durante a fase

Uma substituição em massa inicial atingiu identificadores (`"Clientes"`, `"Estoque"`, etc.) em serviços/repositórios.  
**Correção:** `git checkout` dos layers não-UI + reversão de chaves de módulo em code-behind de navegação/audit/paths.  
Navegação e paths permaneceram com IDs estáveis em português interno.

---

## 3. Arquitetura (reuso obrigatório)

```
UI code-behind → UiText.T(key) / UiText.Confirm(...)
                 → LocalizationService (BuildCatalog = Base + Modules + Interaction)
XAML           → LocalizationHelper (inalterado)
Persistência   → language_settings.json (inalterado)
```

Novos arquivos:

| Arquivo | Papel |
|---------|--------|
| `Helpers/UiText.cs` | Facade tipada sobre `LocalizationService` + `WindowInteractionHelper` |
| `Services/LocalizationService.Interaction.cs` | Catálogo Interaction pt/en/es (~105 chaves) |

`LocalizationService.BuildCatalog` faz merge de `Interaction*` após Modules.

**Não** foi criada segunda arquitetura / catálogo paralelo.

---

## 4. Classificação (A–H)

| Classe | Tratamento nesta fase |
|--------|------------------------|
| A UI traduzível | Migrado quando seguro (títulos comuns, confirms, empty/toasts selecionados) |
| B dado do usuário | Não traduzido |
| C código / ID módulo | Preservado (`Clientes`, `OrdensServico`, …) |
| D fiscal / técnico | Não tocado |
| E log técnico | Categorias de log preservadas |
| F teste | Smoke titles de host preservados |
| G docs/comentários | Fora de escopo |
| H fixo legítimo | Acrônimos / títulos de módulo em MessageBox title onde ID=display |

---

## 5. Strings migradas (amostra)

### Reutilizadas (core/modules)

`Error`, `Success`, `Warning`, `AccessDenied`, `Information`, `RequiredField`, `ClientDeleted`, `ClientSaved`, `VehicleDeleted`, `QuoteApproved`, `AppointmentCreated`, `SelectClientRequired`, `ConfirmDeleteClient`, `ConfirmDeleteVehicle`, `Loading`, `NoClientsRegistered`, …

### Novas (Interaction)

`ConfirmExit`, `ConfirmDeleteRecord`, `ClientNoWhatsApp`, `ContactMissing`, `SelectOsToDelete`, `DraftSaved`, `NoRecordsFound`, `CannotLoadIndicators`, `DashboardSubtitleFormat`, `UpdatedAt`, `UpdateFailed`, `SystemUpdate`, `SearchNoDestination`, `ModuleUnavailable`, `BackupDailyFailed`, `BackupNetworkFailed`, `BackupExitFailed`, `ClientRemoved`, `ProductDeleted`, `NoClientLinkedQuote`, `NoRecordsAvailable`, …

Placeholders `{0}`/`{1}`/`{2}` idênticos em pt/en/es (ex.: `DashboardSubtitleFormat`, `WorkOrderDeleted`).

---

## 6. Cobertura por tipo de interação

| Tipo | Status | Evidência |
|------|--------|-----------|
| MessageBox títulos comuns | **FORTE** | ~184 usos `UiText.T("Error\|Success\|…")`; 237 `MessageBox.Show` totais |
| MessageBox corpos | **PARCIAL** | Sucessos/confirms/empty selecionados; muitos corpos ainda pt-BR |
| Confirmações | **PARCIAL** | `ConfirmExit`, delete keys; CriticalActionDialog textos ainda mistos |
| Toasts | **PARCIAL** | Default title + AccessDenied/ModuleUnavailable/Backup titles |
| Validation | **PARCIAL** | `RequiredField`, quantity/image/attachment keys |
| Empty | **PARCIAL** | Quote sem cliente, NF-e sem registro, keys No* no catálogo |
| Loading | **PARCIAL** | keys + Dashboard UpdatedAt/UpdateFailed |
| Error states | **PARCIAL** | titles + `CannotLoadIndicators` |
| ToolTips / AutomationProperties | **HERDADO I18N-02** | XAML bindings; poucos ToolTips novos nesta fase |
| Ações CRUD commons | **HERDADO I18N-02** | botões XAML |

Métrica complementar (não infla % XAML):

| Métrica | Valor |
|---------|------:|
| `UiText.T(` calls | **269** |
| Interaction keys (pt) | **~105** |
| XAML literal attrs | **2231** (igual I18N-02) |
| XAML bound attrs | **525** (igual I18N-02) |
| % XAML | **~19%** |

Honestidade: esta fase melhorou **interação em code-behind**; o scanner XAML permanece ~19%.

---

## 7. Módulos (UiText calls aproximados)

| Módulo | Status | Notas |
|--------|--------|-------|
| Dashboard | PARTIAL→OK | subtítulo / erro / updated |
| Clientes | FORTE títulos | Error/Success/AccessDenied/WhatsApp |
| Veículos | PARCIAL | títulos |
| OS | PARCIAL | títulos + select delete |
| Orçamentos | PARCIAL | empty client + títulos |
| PDV | PARCIAL | títulos/helpers |
| Estoque | FORTE relativo | títulos + ProductDeleted |
| Financeiro | PARCIAL | títulos |
| Fornecedores | PARCIAL | SupplierDeleted + títulos |
| Funcionários | PARCIAL | títulos |
| Agenda | PARCIAL | títulos |
| Relatórios | PARCIAL | títulos |
| Catálogo | PARCIAL | títulos MessageBox (IDs estáveis) |
| NF-e | PARCIAL | empty + títulos |
| Config | PARCIAL | títulos |
| Ajuda | NÃO (conteúdo) | Help body CONTENT PARTIAL (I18N-02) |
| Login | PARCIAL | LoginFailed / títulos |
| Shell | OK | ConfirmExit + toast titles |

---

## 8. Runtime / persistência / fallback / cultura

| Item | Resultado | Evidência |
|------|-----------|-----------|
| pt/en/es catalogs | PASS | InteractionPt/En/Es + unit Localization **20/20** |
| Runtime switch | PASS (unit + core) | `LocalizationServiceTests` |
| Persistência | PASS (core) | `language_settings.json` |
| Fallback inválido → pt-BR | PASS (core) | unit |
| CurrentCulture negócio pt-BR | **PASS** | `FormatCulture` forçado em `SetLanguage` |
| Walk manual módulo×idioma | **NOT EXECUTED** (completo) | Exhaustive/DeepQa em pt-BR default |

---

## 9. Visual

| Item | Resultado |
|------|-----------|
| Light / Dark | **PASS** DeepQa `CapturasVisuaisLightDark` + Exhaustive 8 rounds |
| 1366 / 1600 / 1920 / 2560 | **PASS** Exhaustive rounds planned=executed |
| Sidebar Brand / Gold Hover | não alterados nesta fase |

---

## 10. QA

| Suite | Resultado | Artefato |
|-------|-----------|----------|
| Build Release | **0 errors** | local |
| Localization unit | **20/20 PASS** | `dotnet test …~Localization` |
| Fiscal lote | **46/46 PASS** | filter Fiscal\|Nfe\|Focus |
| QaEngine (+ CompleteUi checks) | **43/43 PASS** | `TestResults\UiSmoke\2026-09-10_06-40-49` |
| DeepQa (LongRun incluso) | **6/6 PASS** | `…\2026-09-10_06-46-31` |
| ExhaustiveUi | **PASS** discovered 3286 · tested **1941** · PASS **1941** · FAIL **0** | `…\2026-09-10_06-49-43` + `exhaustive-summary.md` |
| DB integrity_check | **ok** | smoke DB Exhaustive |
| DB foreign_key_check | **0 rows** | idem |
| Tag v1.0.0 | **intacta** `72d85fa` | `git rev-parse v1.0.0` |

---

## 11. Limitações (honestas)

1. **Não é 100% i18n** — XAML ~19%; muitos corpos de MessageBox ainda pt-BR.
2. Títulos de MessageBox com nome de módulo frequentemente permanecem no ID interno PT (`Clientes`, `Estoque`) para não quebrar navegação/audit.
3. `CriticalActionDialogService` / diálogos premium ainda com textos mistos.
4. Help content body continua CONTENT PARTIAL.
5. Walk manual completo pt/en/es por módulo: **NOT EXECUTED**.
6. Scanner XAML não conta `UiText` (métrica complementar adicionada sem inflar %).

---

## 12. Decisão

**YELLOW** — Interaction & Dialog Localization **IMPROVED** / **READY WITH LIMITATIONS**

Critérios GREEN plenos (100% interação + walk manual) **não** atingidos.  
Sem regressão de build/QA/fiscal/cultura.

---

## 13. STOP

Fase **PRIMOX-I18N-03** encerrada.  
**Não** iniciar I18N-04 / Installer / Code Signing / Fiscal Live / SaaS / Auto-update automaticamente.
