# PRIMOX-I18N-INTERACTION-LOCALIZATION â€” PRIMOX-I18N-03-2026-09

**Data:** 10/09/2026  
**MissÃ£o:** localizar textos de **interaÃ§Ã£o** (MessageBox, confirmaÃ§Ãµes, toasts, validaÃ§Ãµes, empty/loading/error, tooltips/aÃ§Ãµes) sem segunda arquitetura.  
**HEAD inicial:** `706230b`  
**HEAD final:** _(preencher apÃ³s commit)_  
**Tag `v1.0.0`:** preservada (`72d85fa`) â€” **nÃ£o movida**

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
| CurrentCulture negÃ³cio | **pt-BR** (inalterado) |

Ferramenta: `Scripts/Audit-I18nCoverage.ps1`

---

## 2. DiagnÃ³stico

- Centenas de `MessageBox.Show` / `WindowInteractionHelper.ShowMessage` com tÃ­tulos/corpos em pt-BR.
- TÃ­tulos comuns (`Erro`, `Sucesso`, `Aviso`, `Acesso negado`, `ValidaÃ§Ã£o`) jÃ¡ existiam no catÃ¡logo core mas **nÃ£o** eram usados nos diÃ¡logos.
- Toasts (`ShellNotificationService`) e vÃ¡rios empty/loading states ainda hardcoded.
- **ClassificaÃ§Ã£o:** somente strings **A** (UI traduzÃ­vel). Identificadores de navegaÃ§Ã£o, auditoria, paths e SQL (**C/E**) **nÃ£o** devem ser traduzidos.

### Incidente controlado durante a fase

Uma substituiÃ§Ã£o em massa inicial atingiu identificadores (`"Clientes"`, `"Estoque"`, etc.) em serviÃ§os/repositÃ³rios.  
**CorreÃ§Ã£o:** `git checkout` dos layers nÃ£o-UI + reversÃ£o de chaves de mÃ³dulo em code-behind de navegaÃ§Ã£o/audit/paths.  
NavegaÃ§Ã£o e paths permaneceram com IDs estÃ¡veis em portuguÃªs interno.

---

## 3. Arquitetura (reuso obrigatÃ³rio)

```
UI code-behind â†’ UiText.T(key) / UiText.Confirm(...)
                 â†’ LocalizationService (BuildCatalog = Base + Modules + Interaction)
XAML           â†’ LocalizationHelper (inalterado)
PersistÃªncia   â†’ language_settings.json (inalterado)
```

Novos arquivos:

| Arquivo | Papel |
|---------|--------|
| `Helpers/UiText.cs` | Facade tipada sobre `LocalizationService` + `WindowInteractionHelper` |
| `Services/LocalizationService.Interaction.cs` | CatÃ¡logo Interaction pt/en/es (~105 chaves) |

`LocalizationService.BuildCatalog` faz merge de `Interaction*` apÃ³s Modules.

**NÃ£o** foi criada segunda arquitetura / catÃ¡logo paralelo.

---

## 4. ClassificaÃ§Ã£o (Aâ€“H)

| Classe | Tratamento nesta fase |
|--------|------------------------|
| A UI traduzÃ­vel | Migrado quando seguro (tÃ­tulos comuns, confirms, empty/toasts selecionados) |
| B dado do usuÃ¡rio | NÃ£o traduzido |
| C cÃ³digo / ID mÃ³dulo | Preservado (`Clientes`, `OrdensServico`, â€¦) |
| D fiscal / tÃ©cnico | NÃ£o tocado |
| E log tÃ©cnico | Categorias de log preservadas |
| F teste | Smoke titles de host preservados |
| G docs/comentÃ¡rios | Fora de escopo |
| H fixo legÃ­timo | AcrÃ´nimos / tÃ­tulos de mÃ³dulo em MessageBox title onde ID=display |

---

## 5. Strings migradas (amostra)

### Reutilizadas (core/modules)

`Error`, `Success`, `Warning`, `AccessDenied`, `Information`, `RequiredField`, `ClientDeleted`, `ClientSaved`, `VehicleDeleted`, `QuoteApproved`, `AppointmentCreated`, `SelectClientRequired`, `ConfirmDeleteClient`, `ConfirmDeleteVehicle`, `Loading`, `NoClientsRegistered`, â€¦

### Novas (Interaction)

`ConfirmExit`, `ConfirmDeleteRecord`, `ClientNoWhatsApp`, `ContactMissing`, `SelectOsToDelete`, `DraftSaved`, `NoRecordsFound`, `CannotLoadIndicators`, `DashboardSubtitleFormat`, `UpdatedAt`, `UpdateFailed`, `SystemUpdate`, `SearchNoDestination`, `ModuleUnavailable`, `BackupDailyFailed`, `BackupNetworkFailed`, `BackupExitFailed`, `ClientRemoved`, `ProductDeleted`, `NoClientLinkedQuote`, `NoRecordsAvailable`, â€¦

Placeholders `{0}`/`{1}`/`{2}` idÃªnticos em pt/en/es (ex.: `DashboardSubtitleFormat`, `WorkOrderDeleted`).

---

## 6. Cobertura por tipo de interaÃ§Ã£o

| Tipo | Status | EvidÃªncia |
|------|--------|-----------|
| MessageBox tÃ­tulos comuns | **FORTE** | ~184 usos `UiText.T("Error\|Success\|â€¦")`; 237 `MessageBox.Show` totais |
| MessageBox corpos | **PARCIAL** | Sucessos/confirms/empty selecionados; muitos corpos ainda pt-BR |
| ConfirmaÃ§Ãµes | **PARCIAL** | `ConfirmExit`, delete keys; CriticalActionDialog textos ainda mistos |
| Toasts | **PARCIAL** | Default title + AccessDenied/ModuleUnavailable/Backup titles |
| Validation | **PARCIAL** | `RequiredField`, quantity/image/attachment keys |
| Empty | **PARCIAL** | Quote sem cliente, NF-e sem registro, keys No* no catÃ¡logo |
| Loading | **PARCIAL** | keys + Dashboard UpdatedAt/UpdateFailed |
| Error states | **PARCIAL** | titles + `CannotLoadIndicators` |
| ToolTips / AutomationProperties | **HERDADO I18N-02** | XAML bindings; poucos ToolTips novos nesta fase |
| AÃ§Ãµes CRUD commons | **HERDADO I18N-02** | botÃµes XAML |

MÃ©trica complementar (nÃ£o infla % XAML):

| MÃ©trica | Valor |
|---------|------:|
| `UiText.T(` calls | **269** |
| Interaction keys (pt) | **~105** |
| XAML literal attrs | **2231** (igual I18N-02) |
| XAML bound attrs | **525** (igual I18N-02) |
| % XAML | **~19%** |

Honestidade: esta fase melhorou **interaÃ§Ã£o em code-behind**; o scanner XAML permanece ~19%.

---

## 7. MÃ³dulos (UiText calls aproximados)

| MÃ³dulo | Status | Notas |
|--------|--------|-------|
| Dashboard | PARTIALâ†’OK | subtÃ­tulo / erro / updated |
| Clientes | FORTE tÃ­tulos | Error/Success/AccessDenied/WhatsApp |
| VeÃ­culos | PARCIAL | tÃ­tulos |
| OS | PARCIAL | tÃ­tulos + select delete |
| OrÃ§amentos | PARCIAL | empty client + tÃ­tulos |
| PDV | PARCIAL | tÃ­tulos/helpers |
| Estoque | FORTE relativo | tÃ­tulos + ProductDeleted |
| Financeiro | PARCIAL | tÃ­tulos |
| Fornecedores | PARCIAL | SupplierDeleted + tÃ­tulos |
| FuncionÃ¡rios | PARCIAL | tÃ­tulos |
| Agenda | PARCIAL | tÃ­tulos |
| RelatÃ³rios | PARCIAL | tÃ­tulos |
| CatÃ¡logo | PARCIAL | tÃ­tulos MessageBox (IDs estÃ¡veis) |
| NF-e | PARCIAL | empty + tÃ­tulos |
| Config | PARCIAL | tÃ­tulos |
| Ajuda | NÃƒO (conteÃºdo) | Help body CONTENT PARTIAL (I18N-02) |
| Login | PARCIAL | LoginFailed / tÃ­tulos |
| Shell | OK | ConfirmExit + toast titles |

---

## 8. Runtime / persistÃªncia / fallback / cultura

| Item | Resultado | EvidÃªncia |
|------|-----------|-----------|
| pt/en/es catalogs | PASS | InteractionPt/En/Es + unit Localization **20/20** |
| Runtime switch | PASS (unit + core) | `LocalizationServiceTests` |
| PersistÃªncia | PASS (core) | `language_settings.json` |
| Fallback invÃ¡lido â†’ pt-BR | PASS (core) | unit |
| CurrentCulture negÃ³cio pt-BR | **PASS** | `FormatCulture` forÃ§ado em `SetLanguage` |
| Walk manual mÃ³duloÃ—idioma | **NOT EXECUTED** (completo) | Exhaustive/DeepQa em pt-BR default |

---

## 9. Visual

| Item | Resultado |
|------|-----------|
| Light / Dark | **PASS** DeepQa `CapturasVisuaisLightDark` + Exhaustive 8 rounds |
| 1366 / 1600 / 1920 / 2560 | **PASS** Exhaustive rounds planned=executed |
| Sidebar Brand / Gold Hover | nÃ£o alterados nesta fase |

---

## 10. QA

| Suite | Resultado | Artefato |
|-------|-----------|----------|
| Build Release | **0 errors** | local |
| Localization unit | **20/20 PASS** | `dotnet test â€¦~Localization` |
| Fiscal lote | **46/46 PASS** | filter Fiscal\|Nfe\|Focus |
| QaEngine (+ CompleteUi checks) | **43/43 PASS** | `TestResults\UiSmoke\2026-09-10_06-40-49` |
| DeepQa (LongRun incluso) | **6/6 PASS** | `â€¦\2026-09-10_06-46-31` |
| ExhaustiveUi | **PASS** discovered 3286 Â· tested **1941** Â· PASS **1941** Â· FAIL **0** | `â€¦\2026-09-10_06-49-43` + `exhaustive-summary.md` |
| DB integrity_check | **ok** | smoke DB Exhaustive |
| DB foreign_key_check | **0 rows** | idem |
| Tag v1.0.0 | **intacta** `72d85fa` | `git rev-parse v1.0.0` |

---

## 11. LimitaÃ§Ãµes (honestas)

1. **NÃ£o Ã© 100% i18n** â€” XAML ~19%; muitos corpos de MessageBox ainda pt-BR.
2. TÃ­tulos de MessageBox com nome de mÃ³dulo frequentemente permanecem no ID interno PT (`Clientes`, `Estoque`) para nÃ£o quebrar navegaÃ§Ã£o/audit.
3. `CriticalActionDialogService` / diÃ¡logos premium ainda com textos mistos.
4. Help content body continua CONTENT PARTIAL.
5. Walk manual completo pt/en/es por mÃ³dulo: **NOT EXECUTED**.
6. Scanner XAML nÃ£o conta `UiText` (mÃ©trica complementar adicionada sem inflar %).

---

## 12. DecisÃ£o

**YELLOW** â€” Interaction & Dialog Localization **IMPROVED** / **READY WITH LIMITATIONS**

CritÃ©rios GREEN plenos (100% interaÃ§Ã£o + walk manual) **nÃ£o** atingidos.  
Sem regressÃ£o de build/QA/fiscal/cultura.

---

## 13. STOP

Fase **PRIMOX-I18N-03** encerrada.  
**NÃ£o** iniciar I18N-04 / Installer / Code Signing / Fiscal Live / SaaS / Auto-update automaticamente.
