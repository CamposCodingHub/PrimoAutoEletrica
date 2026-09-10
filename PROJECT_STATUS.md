# Status do Projeto — PRIMOX Workshop 1.0.0

> **PRIMOX-I18N-05-2026-09 (10/09/2026):**  
> `Docs/qa/PRIMOX-I18N-05-BASELINE.md` · `PRIMOX-I18N-05-CONTENT-MATRIX.md` · `PRIMOX-I18N-05-COVERAGE.md` · `PRIMOX-I18N-05-GLOSSARY.md` · `PRIMOX-I18N-05-REGRESSION.md` · `Logs/qa-visual/i18n-05/`  
> **Decisão:** **YELLOW / READY WITH LIMITATIONS** — Core content IMPROVED; EN/ES ainda PARTIAL  
> **Static:** literais **1844** · bound **915** · **~33.2%** (antes ~19.6%)  
> **Residuals TRANSLATION_REQUIRED:** 507→**357** · ~382 bindings em 52 XAML  
> **User-visible strict:** PT **100%** · EN **13.3%** · ES **6.7%** (navegáveis 100%)  
> **Catálogo:** 724 keys ×3 · MISSING_EN/ES **0** · Content.cs P0/P1  
> **QA:** Loc+Fiscal **67/67** · I18n05 PASS · QaEngine **43/43** · DeepQa **6/6** · Exhaustive PASS · DB ok/FK0 · tag `v1.0.0` **intacta**  
> **Não** declarar English/Spanish complete. Help Extended CONTENT PARTIAL.  
> **PRIMOX-I18N-04-2026-09 (10/09/2026):**  
> `Docs/qa/PRIMOX-I18N-04-MANUAL-UX-AUDIT.md` · `PRIMOX-I18N-USER-VISIBLE-COVERAGE.md` · `Logs/qa-visual/i18n-04/`  
> **Decisão:** **YELLOW / READY WITH LIMITATIONS** — User-visible EN/ES **PARTIAL** (strict PASS 0%; navegáveis 100%)  
> **Métricas:** Static ~**19.6%** (2219 lit / 540 bound) · User-visible PT **100%** · EN/ES strict **0%** / usable **100%**  
> **Ferramentas:** `Audit-I18nRuntimeResiduals.ps1` · `Audit-I18nCatalog.ps1` · smoke `I18n04`  
> **P0 fixes:** SaveDraft/SaveChanges/CollapseMenu/PDV shortcuts/Funcionários actions  
> **QA:** Localization+Fiscal lote PASS · I18n04 PASS · QaEngine **43/43** · DeepQa **6/6** · Exhaustive (ver regression doc) · tag `v1.0.0` **intacta**  
> **Não** declarar multilingual complete. Help body CONTENT PARTIAL.  
> **PRIMOX-I18N-03-2026-09 (10/09/2026):**  
> `Docs/qa/PRIMOX-I18N-INTERACTION-LOCALIZATION.md` · `UiText` + `LocalizationService.Interaction.cs`  
> **Decisão:** **YELLOW** — Interaction & Dialog Localization IMPROVED / **READY WITH LIMITATIONS**  
> **Interação:** MessageBox títulos comuns (Error/Success/Warning/…) · confirms/toasts/empty selecionados · **269** `UiText.T` · ~105 chaves Interaction × pt/en/es  
> **XAML attrs:** literais **2231** · LocHelper **525** · **~19%** (inalterado vs I18N-02 — fase focou code-behind)  
> **CurrentCulture negócio:** **pt-BR** preservado · IDs de navegação/audit/paths **não** traduzidos  
> **QA I18N-03:** Build 0 · Localization **20/20** · Fiscal **46/46** · QaEngine **43/43** · DeepQa **6/6** (LongRun) · Exhaustive **1941/1941 PASS** · DB integrity **ok** · tag `v1.0.0` **intacta**  
> **Limitações:** corpos MessageBox ainda parciais · Help body CONTENT PARTIAL · walk manual pt/en/es completo **NOT EXECUTED** · **não** 100%  
> **PRIMOX-I18N-02-2026-09 (10/09/2026):**  
> `Docs/qa/PRIMOX-I18N-MODULE-COVERAGE.md` · auditoria `Scripts/Audit-I18nCoverage.ps1`  
> **Decisão:** **YELLOW** — Module Coverage IMPROVED / **READY WITH LIMITATIONS**  
> **Métrica real:** literais UI 2641→**2231** · bindings LocHelper ~55→**525** · cobertura attrs **~19%** (não 100%)  
> **Core:** indexer `Path=[Key]` · catálogo módulos · OS status display localizer · CurrentCulture negócio **pt-BR**  
> **QA I18N-02:** Localization **20/20** · Fiscal lote **34 PASS** · QaEngine **43/43** · DeepQa **6/6** · ExhaustiveUi **PASS** · tag `v1.0.0` **intacta**  
> **Ajuda conteúdo:** CONTENT PARTIAL · MessageBoxes: ainda majoritariamente pt-BR (melhorado em I18N-03)  
> **PRIMOX-I18N-2026-09 (09–10/09/2026):**  
> `Docs/qa/PRIMOX-I18N-AUDIT.md`  
> **Decisão:** **YELLOW** — `Localization Core COMPLETE` / **READY WITH LIMITATIONS**  
> **Core:** `LocalizationService` + `LocalizationHelper` · pt-BR / en-US / es-ES · persistência `language_settings.json` · fallback pt-BR · UICulture muda · CurrentCulture negócio permanece pt-BR  
> **Shell runtime:** Sidebar/Header/Login seletor/Command Palette localizados · Logout binding corrigido  
> **QA:** Build 0 erros · Unit Localization **17/17** · QaEngine **43/43** · DeepQa **6/6** (LongRun incluso) · ExhaustiveUi **PASS** (`2026-09-09_21-43-54`) · fiscal units no lote **PASS** · tag `v1.0.0` **intacta**  
> **Limitações:** módulos CRUD/PDV/OS/Help body ainda majoritariamente hardcoded pt-BR (~26 bindings vs ~1500+ literais XAML) — **não** declarar 100% i18n  
> **SCRIPT 7 — CODE SIGNING + COMMERCIAL INSTALLER (09–10/09/2026):**  
> `Docs/qa/PRIMOX-COMMERCIAL-INSTALLER-AUDIT.md` · readiness `Scripts/Check-CodeSigningReadiness.ps1`  
> **Decisão:** **YELLOW** — `READY FOR COMMERCIAL SIGNING` · assinatura **BLOCKED BY EXTERNAL CERTIFICATE**  
> **Metadata:** Company/Publisher alinhados a **CamposCodingHub** · Product **PRIMOX Workshop 1.0.0**  
> **Setup SHA256 (oficial):** `B1AAE306EE7E4108C8FDCF1622B03FA6992CFDEFA55F3151D7B275531F2799F2`  
> **Install/startup/smoke/DB:** PASS (migrations=28) · **Uninstall silent E2E:** FAIL/TIMEOUT (documentado)  
> **SmartScreen:** NOT VERIFIED · **AUTO-UPDATE:** NOT IMPLEMENTED · tag `v1.0.0` **intacta**  
> **SCRIPT 6 — FISCAL LIVE HOMOLOGATION (09/09/2026):**  
> `Docs/qa/PRIMOX-FISCAL-LIVE-HOMOLOGATION-REPORT.md` · readiness `Scripts/Check-FiscalLiveReadiness.ps1`  
> **Decisão:** **YELLOW** — `FISCAL HOMOLOGATION BLOCKED BY EXTERNAL PREREQUISITE`  
> **Live:** token ABSENT · emitente EMPTY · `LiveHttpEnabled=false` · TLS homolog REACHABLE (sem auth)  
> **Automated:** fiscal unit **46/46 PASS** · QaEngine **43/43 PASS** · Production Guard ACTIVE · Fake≠Live  
> **Prep:** payload Focus envia `serie`/`numero` quando configurados · tag `v1.0.0` **intacta** · produção **BLOQUEADA**  
> **Não** declarar emissão SEFAZ / homolog live PASS sem evidência Focus real.  
> **HELP CENTER REAL AUDIT 3.0 / Script 1 (09/09/2026):**  
> `Docs/qa/PRIMOX-HELP-CENTER-VISUAL-AUDIT-3.0.md`  
> **Problema:** índice da Ajuda ainda lia como **segunda Sidebar** (faixa escura / Surface) e seleção azul de sistema — Overnight 2.0 “Help Light CORRIGIDO” foi **só theme bind** (`SetResourceReference`), **não** layout/nav.  
> **Causa:** painel esquerdo com visual de nav primaria + TreeViewItem padrão (highlight sistema).  
> **Decisão UX:** **MODELO B** — índice compacto secundário (220, max 260), `AppBackgroundBrush`, seleção BrandSoft + barra Primary (laranja), badge “AJUDA INCLUSA”.  
> **Arquivos:** `HelpControl.xaml` · `UiSmokeTestService.DeepQa.cs` · `UiSmokeTestService.PrimoxQa.CompleteUi.cs` · docs.  
> **Testes:** BUILD 0 erros · QaEngine 43/43 (`CompleteUiHelpCenter` PASS) · DeepQa 6/6 com PNGs Help Light/Dark · Exhaustive 1933/1933 fail=0 blocked=0 · visual PNGs lidos.  
> **Decisão:** **READY WITH LIMITATIONS** · tag `v1.0.0` intacta · WIP deploy scripts preservados untracked.  
> **GLOBAL UI/UX VISUAL AUDIT 3.0 / Script 2 (09/09/2026):**  
> `Docs/qa/PRIMOX-GLOBAL-UI-UX-VISUAL-AUDIT-3.0.md`  
> **Escopo:** inventário + P0/P1 hex→DynamicResource (ListView, Veículos chips, Permissões, Agenda premium, Reset, Históricos) · fiscal = regressão only.  
> **Decisão:** **READY WITH LIMITATIONS** · Calendar Dark header = LIMITATION · residual P2 Financeiro charts.  
> **FINAL CODEBASE HARDENING 3.0 / Script 3 (09/09/2026):**  
> `Docs/qa/PRIMOX-FINAL-CODEBASE-HARDENING-3.0.md`  
> **Escopo:** inventário KEEP/KEEP-FUTURE/TEST-ONLY/SAFE-REMOVE · fiscal stack protegido · DB integrity ok / FK 0 · secrets scan limpo · SAFE-REMOVE dead services + 0-byte tests.  
> **Decisão:** **READY WITH LIMITATIONS** · tag `v1.0.0` intacta · WIP deploy untracked.  
> **PRODUCT TRUTH AUDIT 1.0 (08/09/2026)** — fonte de verdade comercial:  
> `Docs/qa/PRIMOX-PRODUCT-TRUTH-AUDIT-1.0.md` · `PRIMOX-PRODUCT-TRUTH-MATRIX.md` · `PRIMOX-PRODUCT-GAPS.md` · `PRIMOX-COMMERCIAL-READINESS.md`  
> **Decisão:** PRODUCT TRUTH VERIFIED WITH LIMITATIONS  
> **TOTAL CODEBASE + INTEGRATION AUDIT 1.0 (08/09/2026):**  
> `Docs/qa/PRIMOX-TOTAL-CODEBASE-INTEGRATION-AUDIT-1.0.md` · cleanup/integration matrices · `Docs/architecture/PRIMOX-*-ARCHITECTURE.md`  
> **Decisão:** COMMERCIAL READY WITH LIMITATIONS  
> **CODEBASE SANITIZATION 1.0 (08/09/2026):**  
> `Docs/qa/PRIMOX-CODEBASE-SANITIZATION-1.0-REPORT.md` · Help Audit/Coverage · cleanup matrix atualizada  
> **Decisão:** SANITIZED WITH LIMITATIONS · placeholders Notification/Filial neutralizados · Ajuda profissional · 7 shells removidos  
> **FISCAL PROVIDER DECISION 1.0 (08/09/2026):**  
> `Docs/qa/PRIMOX-FISCAL-PROVIDER-AUDIT-1.0.md` · `PRIMOX-FISCAL-DECISION.md` · comparison/homologation/architecture  
> **Decisão:** Focus NFe recomendado · **GO CONDICIONAL** · tag `v1.0.0` intacta  
> **FISCAL FOUNDATION 1.0 (08/09/2026):**  
> `Docs/architecture/PRIMOX-FISCAL-FOUNDATION-1.0.md` · `Docs/qa/PRIMOX-FISCAL-FOUNDATION-REPORT.md` · `PRIMOX-FISCAL-TEST-MATRIX.md`  
> **Decisão:** **FOUNDATION READY WITH LIMITATIONS** · Focus adapter preparado · Produção BLOQUEADA  
> **NF-e HOMOLOGAÇÃO 1.0 (08/09/2026):**  
> `Docs/architecture/PRIMOX-NFE-HOMOLOGATION-1.0.md` · `Docs/qa/PRIMOX-NFE-HOMOLOGATION-REPORT.md` · `PRIMOX-NFE-HOMOLOGATION-TEST-MATRIX.md`  
> **Decisão:** **NF-e HOMOLOGATION IMPLEMENTED — LIVE HOMOLOGATION PENDING** · origem Venda/PDV · produção BLOQUEADA · live Focus **NOT EXECUTED**  
> **FISCAL OPERATIONS CENTER 2.0 (08/09/2026):**  
> `Docs/architecture/PRIMOX-FISCAL-OPERATIONS-2.0.md` · `Docs/qa/PRIMOX-FISCAL-OPERATIONS-REPORT.md` · `PRIMOX-FISCAL-OPERATIONS-MATRIX.md`  
> **Decisão:** **READY WITH LIMITATIONS** · HealthCheck + Preview + StateMachine + Histórico UI · Fake cancel PASS · live Focus **NOT EXECUTED** · produção BLOQUEADA  
> **OVERNIGHT GLOBAL AUDIT 2.0 (08/09/2026):**  
> `Docs/qa/PRIMOX-OVERNIGHT-GLOBAL-AUDIT-2.0.md` · `PRIMOX-UI-PAGE-BY-PAGE-AUDIT-2.0.md` · `Docs/architecture/PRIMOX-CODEBASE-STRUCTURE-AUDIT-2.0.md`  
> **Decisão:** **READY WITH LIMITATIONS** · Ajuda Light theme **CORRIGIDO** (SetResourceReference) · Calendar Dark = LIMITATION  
> **Não** interpretar seções históricas abaixo (ROI, “98/100”, “Enterprise-ready”, “.NET 9 WPF”, “2FA DONE no login”, “multi-filial DONE”, “NF-e emissão pronta”) como estado atual sem cruzar com Truth / Total / Sanitization / Fiscal Decision / Fiscal Foundation / NF-e Homologation / Fiscal Ops 2.0 / Overnight Audit 2.0 / **Help Center Audit 3.0** / **I18N 2026-09**.  
> **Tag:** `v1.0.0` → `a4ad6fe` (intacta). **Não** liberar produção / NFC-e / SaaS automaticamente — aguardar revisão.

## Análise histórica (arquivo vivo — pode conter trechos desatualizados)

**Data Atualização**: 10/09/2026  
**Status do Projeto**: 🟢 **PRIMOX Workshop 1.0.0** — desktop READY WITH LIMITATIONS · **I18N Core+Modules+Interaction (parcial)** · Help Center 3.0 MODELO B · Exhaustive (ver rodada atual) · Fiscal Ops 2.0 READY WITH LIMITATIONS  
**Build Status**: ✅ 0 erros  
**Testes Status**: ✅ Localization 20/20 · QaEngine 43/43 · DeepQa 6/6 · Exhaustive 1941 PASS · fiscal 46/46  
**Versão Atual**: **1.0.0** (tag `v1.0.0` → `72d85fa` points-at histórico; **não mover**)  
**Maturidade (Product Truth)**: **não usar 98/100** — ver contagens objetivas na Truth Matrix (REAL vs PARCIAL vs SCAFFOLD). Score legado “98/100” = **DOCUMENTAÇÃO INCORRETA** (retirado como métrica oficial).

### PRIMOX-I18N-03-2026-09 (10/09/2026) — Interaction & Dialog Localization IMPROVED / READY WITH LIMITATIONS

**Missão:** MessageBox / diálogos / confirms / toasts / empty / loading / error / validações via catálogo oficial.  
**Entregas:** `UiText.cs` · `LocalizationService.Interaction.cs` · merge no `BuildCatalog` · auditoria com métrica `UiText` complementar · `Docs/qa/PRIMOX-I18N-INTERACTION-LOCALIZATION.md`.  
**Métrica:** XAML ~19% inalterado · **269** `UiText.T` · títulos comuns localizados (~184 Error/Success/…).  
**QA:** `TestResults\UiSmoke\2026-09-10_06-40-49` (QaEngine) · `…_06-46-31` (DeepQa) · `…_06-49-43` (Exhaustive 1941/0).  
**Limitações:** corpos MessageBox parciais; Help body; walk manual multi-idioma completo NOT EXECUTED; não 100%.

### PRIMOX-I18N-02-2026-09 (10/09/2026) — Module Coverage IMPROVED / READY WITH LIMITATIONS

**Missão:** reduzir hardcoded nos módulos usando o core I18N-01.  
**Métrica:** literais UI 2641→2231 · LocHelper bound attrs ~55→525 · cobertura attrs **~19%**.  
**Entregas:** `LocalizationService.Modules.cs` · indexer Helper · OS status localizer · `Scripts/Audit-I18nCoverage.ps1` · `Docs/qa/PRIMOX-I18N-MODULE-COVERAGE.md`.  
**QA:** Localization 20/20 · QaEngine 43/43 · DeepQa 6/6 · Exhaustive PASS · fiscal lote PASS.  
**Limitações:** MessageBoxes/diálogos secundários; Help body CONTENT PARTIAL; não 100%.

### PRIMOX-I18N-2026-09 (09–10/09/2026) — Localization Core COMPLETE / READY WITH LIMITATIONS

**Problema:** seletor de idioma visual sem tradução efetiva da aplicação.  
**Causa:** LocalizationService parcial + Sidebar/módulos hardcoded + sem persistência + Logout binding quebrado.  
**Implementação:** catálogo pt/en/es central · persistência JSON · Shell/Sidebar/Command Palette/Login seletor · UICulture runtime · cultura de negócio pt-BR preservada.  
**QA:** `TestResults\UiSmoke\2026-09-09_21-33-06` (QaEngine 43/43) · `2026-09-09_21-41-00` (DeepQa 6/6).  
**Docs:** `Docs/qa/PRIMOX-I18N-AUDIT.md`.  
**Limitações:** conteúdo de módulos e Help body ainda majoritariamente pt-BR; não afirmar 100% i18n.

### HELP CENTER REAL AUDIT 3.0 (09/09/2026) — READY WITH LIMITATIONS

**Problema:** Central de Ajuda ainda parecia “segunda Sidebar” (faixa escura) + seleção azul de sistema; Overnight 2.0 “Help Light CORRIGIDO” = **apenas** theme bind.  
**Modelo B:** índice 220 (min 180 / max 260), `AppBackgroundBrush`, TreeViewItem custom BrandSoft + borda Primary 3px, header “ÍNDICE DA AJUDA”, badge BrandSoft “AJUDA INCLUSA”, FocusVisualStyle → Primox/SystemParameters (não `{x:Null}`).  
**QA:** `TestResults\UiSmoke\2026-09-09_07-51-26` (QaEngine) · `2026-09-09_07-47-47` (DeepQa) · `2026-09-09_07-24-47` (Exhaustive discovered=3266 tested=1933 pass=1933).  
**Visual:** PNGs `Logs\qa-visual\fase12-a11y\help-{light|dark}-{1366,1600,1920,2560}.png` lidos — índice secundário, sem segunda sidebar escura, seleção laranja/BrandSoft.  
**Limitações:** DeepQa captura o HelpControl (não necessariamente chrome completo MainWindow+Sidebar); BrandSoft no Dark pode parecer laranja mais saturado; Exhaustive desta rodada usou build pré-FocusVisual fix (CompleteUi/QaEngine pós-rebuild PASS).  
**WIP:** `Scripts/Atualizar-PrimoAuto.bat` + `Deploy-ToInstalledApp.ps1` **não** commitados.  
**Docs:** `Docs/qa/PRIMOX-HELP-CENTER-VISUAL-AUDIT-3.0.md`.

### GLOBAL UI/UX VISUAL AUDIT 3.0 (09/09/2026) — READY WITH LIMITATIONS

**Escopo:** inventário global + conversão P0/P1 de hex light-only → brushes do DS (`ListView`, chips Veículos, ConfigurarPermissões, NovoAgendamentoPremium, ResetSistema, históricos).  
**Fiscal:** regressão only — produção BLOQUEADA.  
**Visual lido:** Dashboard L/D, Veículos Dark (chip Monitorado themed), Agenda Dark (Calendar header = LIMITATION).  
**QA Script 2:** DeepQa `2026-09-09_08-05-00` 6/6 · QaEngine `2026-09-09_08-08-05` 43/43 · Exhaustive `2026-09-09_08-15-40` discovered=3276 tested=1933 pass=1933 fail=0 blocked=0.  
**Docs:** `Docs/qa/PRIMOX-GLOBAL-UI-UX-VISUAL-AUDIT-3.0.md`.

### FINAL CODEBASE HARDENING 3.0 (09/09/2026) — READY WITH LIMITATIONS

**Escopo:** classificação orphan/dead · SAFE-REMOVE (`CodeAuditService`, `ScreenshotCaptureService`, `LocalSyncMessageHandler`, 4 testes 0-byte, fase8 leftovers) · fiscal KEEP/KEEP-FUTURE/TEST-ONLY · DB isolado integrity=ok FK=0 · secrets scan sem PEM/sk_live/AKIA.  
**QA:** fiscal units 45 PASS · DeepQa `2026-09-09_08-43-02` 6/6 · QaEngine `2026-09-09_08-45-59` 43/43 · Exhaustive `2026-09-09_08-53-34` discovered=3276 tested=1933 pass=1933 fail=0 blocked=0.  
**Restart:** processo morto entre suites; DeepQa inclui `LongRunNavegacaoTema` PASS.  
**WIP:** deploy scripts untracked. **Docs:** `PRIMOX-FINAL-CODEBASE-HARDENING-3.0.md`.

### FISCAL OPERATIONS CENTER 2.0 (08/09/2026) — READY WITH LIMITATIONS

**Escopo:** HealthCheck, série emitente, preview técnico, state machine, histórico UI (`FiscalOperationsControl`), cancel com guardas (Fake PASS; Focus live cancel NOT EXECUTED).  
**Live Focus:** **NOT EXECUTED** (sem token). **Produção:** BLOQUEADA.  
**Docs:** `PRIMOX-FISCAL-OPERATIONS-2.0.md` / REPORT / MATRIX.

### OVERNIGHT GLOBAL AUDIT 2.0 (08/09/2026) — READY WITH LIMITATIONS

**Escopo:** inventário + Help Light bug (causa: FindResource snapshot) + regressão fiscal preservada + docs.  
**WIP deploy scripts:** preservados untracked.  
**Próximo:** decisão humana — **PARAR**.

### FISCAL FOUNDATION 1.0 (08/09/2026) — FOUNDATION READY WITH LIMITATIONS

**Escopo:** `IFiscalProvider`, Focus adapter preparado, contratos, idempotência, Production Guard, migration fiscal, Fake TEST ONLY — **sem** emissão real, **sem** HTTP Focus live, **sem** produção.  
**Verdade:** Import NF-e continua REAL (`NFeService`). Emissão NF-e/NFC-e/NFS-e = **NÃO IMPLEMENTADA** (foundation only).  
**Adapter:** `FocusNfeProvider` → HTTP OFF → `FISCAL-FOCUS-HTTP-OFF`. Produção → `FISCAL-PROD-BLOCKED`.  
**Banco:** `202609080001` (`FiscalOperations` / `FiscalDocuments` / `FiscalEvents`). integrity_check ok / FK 0 em DB isolado (28 migrations no código).  
**Regressão:** QaEngine 43/43 · DeepQa 6/6 · Exhaustive 1909/0/0 · Light/Dark · 4 resoluções.  
**Próximo (após revisão):** NF-e Homologation Implementation. **Não** declarar FISCAL READY.

### NF-e HOMOLOGAÇÃO 1.0 (08/09/2026) — IMPLEMENTED — LIVE PENDING

**Escopo:** fluxo end-to-end Homologação (Venda→validator→mapper→Focus HTTP opt-in) · UI PDV · produção bloqueada.  
**Live Focus:** **NOT EXECUTED** nesta sessão (sem `PRIMOX_FOCUS_HOMOLOG_TOKEN` / DPAPI).  
**Regressão:** QaEngine 43/43 · DeepQa 6/6 · Exhaustive 1915/0/0 · unit 25 PASS.  
**Docs:** `PRIMOX-NFE-HOMOLOGATION-1.0.md` / REPORT / TEST-MATRIX.

### FISCAL PROVIDER DECISION 1.0 (08/09/2026) — FISCAL ARCHITECTURE DECISION READY

**Escopo:** auditoria + comparação + arquitetura + plano de homologação — decisão de provedor.  
**Verdade:** Import NF-e REAL+TESTADA; emissão NF-e/NFC-e/NFS-e NÃO IMPLEMENTADA (agora com fundação; bridge `NFeEmissaoService`).  
**Caminho:** Opção B (provedor). **Recomendado:** Focus NFe. **Alternativa:** PlugNotas. **Evitar agora:** Nuvem Fiscal (risco continuidade).  
**Certificado:** A1. **Onda 1 futura:** NF-e homologação via `IFiscalProvider`.  
**Custo ordem:** Solo ~R$ 89,90/mês (até 100 notas) — preços públicos 08/09/2026.  
**Próximo:** aceite humano — **PARAR** (não implementar emissão automaticamente).

### CODEBASE SANITIZATION 1.0 (08/09/2026) — SANITIZED WITH LIMITATIONS

**Escopo:** limpeza SAFE da matriz + neutralização de placeholders + Central de Ajuda + regressão total.  
**Removidos:** 6 services 0-byte + `Scripts/Run-Keycloak.ps1`.  
**Neutralizado:** `NotificationService` (sem sucesso falso); `FilialService`/Login (sem SP/RJ / sem diálogo multi).  
**Ajuda:** tema Design System; cargos; limites honestos; smoke `CompleteUiHelpCenter`.  
**QA:** QaEngine **43/43** · DeepQa **6/6** · Exhaustive **1909 PASS / 0 FAIL / 0 BLOCKED** · DB isolado integrity ok (27).  
**Não** implementado: NF-e emissão, SaaS, sync, multi-filial, API.  
**Próximo:** decisão humana — **PARAR**.

### TOTAL CODEBASE + INTEGRATION AUDIT 1.0 (08/09/2026) — COMMERCIAL READY WITH LIMITATIONS

**Escopo:** inventário forense + integrações + fiscal + API + sync/filial/SaaS + limpeza **classificada** (sem remoção agressiva).  
**TFM desktop:** `net6.0-windows` · **API:** `net9.0-windows` (parcial).  
**Migrations código:** 27 CURRENT.  
**Integrações:** `wa.me` REAL; Notification/Twilio PLACEHOLDER/NÃO IMPLEMENTADO; NF-e import REAL; emissão NÃO IMPLEMENTADO; PIX interno REAL; gateway NÃO; sync remoto NÃO; multi-filial SCAFFOLD; SaaS NÃO.  
**Limpeza:** 0 arquivos removidos; candidatos em `PRIMOX-CODEBASE-CLEANUP-MATRIX.md`.  
**QA desta audit:** build reexecutado; regressão Exhaustive/QaEngine citada da evidência P15E-015 (sem remoção → sem re-run obrigatório completo).  
**WIP:** Help + Deploy scripts preservados (não commitados nesta audit).  
**Próximo:** decisão humana — **PARAR** (não implementar NF-e/SaaS/sync).

### PRODUCT TRUTH AUDIT 1.0 (08/09/2026)

| Dimensão | Resultado |
|----------|-----------|
| UI buttons (executáveis) | 1909/1909 PASS (Exhaustive 3.0) — **não** = produto 100% |
| Domínio core oficina | REAL+TESTADO (CRUD/OS/PDV/Estoque/Financeiro/Relatórios) |
| NF-e emissão SEFAZ | NÃO IMPLEMENTADO |
| Multi-filial / sync offline | SCAFFOLD / NÃO IMPLEMENTADO |
| API JWT + policies | SCAFFOLD |
| 2FA no login | PARCIAL (serviço existe; login sem desafio) |
| SaaS | FORA DO ESCOPO |
| Venda desktop | SIM COM LIMITAÇÕES |
| Venda SaaS | NÃO |

### PROGRAMA 100% — ETAPA 1 ACESSIBILIDADE (08/09/2026) — P15E-015 VERIFIED

**Escopo:** fechamento P15E-015 (icon/chrome a11y).  
**Causa:** DataGrid SelectAll + DatePicker PART_Button sem identidade.  
**Correção:** `AccessibilityChromeHealer` + template DatePicker + regressão.  
**Exhaustive:** ACCESSIBILITY rows **0**; PASS 1909; FAIL 0; BLOCKED 0; Light/Dark × 4 resoluções PASS.  
**Relatório:** `Docs/qa/PRIMOX-ACCESSIBILITY-CLOSURE-REPORT.md`  
**Não** iniciar ETAPA 2 automaticamente.

### PROGRAMA 100% — FASE A ROADMAP (08/09/2026) — SEM IMPLEMENTAÇÃO DE FEATURES

**Escopo:** auditoria + arquitetura para A11y → Integrações → NF-e → Multi-filial → Sync → SaaS.  
**Decisão:** **REQUIRES PRODUCT DECISION** (ETAPA 1 Acessibilidade = READY FOR IMPLEMENTATION após GO explícito).  
**Docs:** `Docs/qa/PRIMOX-100-PERCENT-ROADMAP.md` · `Docs/architecture/PRIMOX-*-ARCHITECTURE.md`  
**Tag `v1.0.0`:** intacta. **Não** implementar NF-e/SaaS/Sync nesta fase. **Não** iniciar Fase 16 automaticamente.

### EXHAUSTIVE UI AUDIT 3.0 (08/09/2026) — PASS WITH KNOWN LIMITATIONS

**Escopo:** fechar 102 BLOCKED, modais P15E-012, a11y P15E-015, Light/Dark × 4 resoluções, regressão.  
**102 BLOCKED:** todos `Host disposed mid-queue` → **QA_ENGINE_BUG**; após correção **BLOCKED=0**.  
**Reteste Exhaustive:** discovered 3220 · tested **1909** · PASS **1909** · FAIL **0** · BLOCKED **0** · tested/discovered **59,29%** · tested/executable **100%**.  
**Crashes corrigidos no motor:** Login `CloseButton`→Shutdown; native dismiss `Abrir caixa` (HwndWrapper).  
**P15E-012:** VERIFIED (runtime modals; 35 windows).  
**P15E-015:** PARTIAL (30 findings: AutoEletrica chrome + OrdensServico icons).  
**Relatórios:** `Docs/qa/PRIMOX-EXHAUSTIVE-UI-CLOSURE-REPORT.md`, `Docs/qa/PRIMOX-EXHAUSTIVE-UI-REPORT.md`  
**Tag `v1.0.0`:** intacta (`a4ad6fe`).  
**Próximo passo:** decisão humana — **não** iniciar Fase 16.

### EXHAUSTIVE UI AUDIT 2.0 (08/09/2026) — PASS WITH KNOWN LIMITATIONS (superseded by 3.0)

**Escopo:** reteste completo do motor ExhaustiveUi (SCAN→FREEZE→EXEC, recursão de janelas, Light/Dark × 4 resoluções), classificação honesta de FAIL vs falso positivo, regressão QaEngine/DeepQa/Long Run.  
**Motor:** `UiSmokeTestService.ExhaustiveUi.cs` — commit `9856f23`.  
**Reteste:** discovered 3216 · tested 1502 · PASS 1502 · FAIL 0 · BLOCKED 102 · coverage tested/discovered **46,70%** · tested/executable **93,64%** · pass rate **100%**.  
**Falsos positivos Pass 1:** ~797 eliminados (classificador).  
**Bugs produto novos (FAIL):** 0.  
**Limitações:** native file/print, disabled/hidden, depth/loop guards, ~16 a11y icon-only (P15E-015 OPEN), BLOCKED mid-queue.  
**Relatório:** `Docs/qa/PRIMOX-EXHAUSTIVE-UI-REPORT.md`  
**Checklist:** `Docs/qa/PRIMOX-MELHORIAS.md`  
**Tag `v1.0.0`:** protegida (`a4ad6fe`).  
**Próximo passo:** decisão humana — **não** iniciar Fase 16.

### FASE 15E — COMPLETE UI INTERACTION / VISUAL QA (08/09/2026) — VALIDATED

**Escopo:** FocusVisualStyle global, Dark inputs, placeholders i18n, layouts Login/PDV/Funcionários, motor CompleteUi.  
**Decisão:** **GO WITH KNOWN LIMITATIONS** (sem declarar 100% de todos os botões/diálogos).  
**Checklist:** `Docs/qa/PRIMOX-MELHORIAS.md`  
**Relatório:** `Docs/qa/PRIMOX-COMPLETE-UI-AUDIT-15E-REPORT.md`  
**Tag `v1.0.0`:** protegida (`a4ad6fe`).

### FASE 15D — LEGACY INSTALL CLEANUP & COMMERCIAL READINESS (08/09/2026) — GO

**Escopo:** inventário → backup → uninstall legado 0.0.0.0 → atalhos oficiais → isolamento PackagingE2E AppId; sem features/schema/tag move.

| Campo | Resultado |
|-------|-----------|
| Legado PF `Primo Auto Elétrica` 0.0.0.0 | **REMOVIDO** (via `unins000`; dados AppData preservados) |
| Oficial | `Program Files\PRIMOX\Workshop` — PV **1.0.0** |
| Development | `%LOCALAPPDATA%\PrimoAutoEletrica\App` **mantido**; sem atalho comercial |
| Backup pré-limpeza | `LegacyCleanup-15D-20260908-103031` SHA256 `A0100377…C95209` integrity ok |
| Atalhos comerciais | Desktop + Start Menu → EXE oficial |
| AppId comercial | `PRIMOX.Workshop.1` (preservado) |
| Packaging E2E AppId | `PRIMOX.Workshop.PackagingE2E` (isolado) |
| Setup SHA256 (comercial 15D) | `6053EBFFC1028D8F97752B2F2A2F58B7C43BA000E1EF879F21843E4D323B67DC` |
| QaEngine / DeepQa / Long Run | **37/37** / **6/6** / **PASS** |
| Sandbox / login CRUD manual | NOT TESTABLE |
| Relatório | `Docs/qa/PRIMOX-LEGACY-CLEANUP-REPORT.md` |
| Guia usuário legado | `INSTALLATION.md` |

**Próximo passo:** decisão humana — **não** iniciar Fase 16 / website / licença / signing automaticamente.

### FASE 15C — INSTALLATION E2E HARDENING (08/09/2026) — GO

**Escopo:** provar Setup→Install→EXE→DB→CRUD→uninstall/reinstall; corrigir isolamento smoke instalado + logs AppData.

| Campo | Resultado |
|-------|-----------|
| Causa smoke Exit=-1 | Isolamento `--app-data` + deadlock handler (não falha de startup normal) |
| Installed QaEngine | **37/37 PASS** (~372 s) |
| Fresh DB migrations | **27** integrity ok |
| Histórico 32 migs | startup/inventário PASS (cópia isolada) |
| Uninstall / retenção / reinstall | PASS |
| Login interativo | NOT TESTABLE |
| Sandbox | NOT TESTABLE |
| Legacy PF 0.0.0.0 | KNOWN — não removido auto |
| Setup SHA256 (rebuild 15C) | `67F4DF6AA9F03F38ABC71A63219413B611A3B8D5C8F922E513C2824575C5E67B` |
| Relatório | `Docs/qa/PRIMOX-INSTALLATION-E2E-REPORT.md` |

**Próximo passo:** decisão humana — **não** iniciar Fase 16 / website / licença automaticamente.

### FASE 15B — COMMERCIAL PACKAGING & DEPLOYMENT (08/09/2026) — GO

**Escopo:** infraestrutura/distribuição — sem novos módulos/redesign/schema destrutivo.

| Campo | Resultado |
|-------|-----------|
| Pipeline oficial | `Scripts/Build-PrimoXCommercialRelease.ps1` → `artifacts/` |
| TFM | **net6.0-windows** alinhado (ISS/scripts/CI) |
| Publish | win-x64 **self-contained** (sem SingleFile/Trim) |
| Inno | 6.7.3 → `PRIMOX-Workshop-Setup-1.0.0.exe` |
| SHA256 | `A62388AE547B6863363825DA984A7532061F5EF7548CCB66BEB5615D35735EB3` |
| Install silencioso + startup | PASS (processo Responding) |
| Uninstall + retenção AppData | PASS |
| Migrations 32 vs 27 | Explicado (5 extras classe **B**); SCHEMA DIVERGENT KNOWN |
| Restore E2E (cópia isolada) | PASS (arquivo) |
| Update comercial completo | NOT IMPLEMENTED / upgrade E2E NOT TESTABLE |
| Assinatura | NOT CONFIGURED |
| Tag `v1.0.0` | **intacta** (`a4ad6fe`) |
| Relatório | `Docs/qa/PRIMOX-COMMERCIAL-PACKAGING-REPORT.md` |
| Instalação usuário | `INSTALLATION.md` |

**Próximo passo:** decisão humana após o relatório — **não** iniciar Fase 16 / website / licença SaaS automaticamente.

### FASE 15A — COMMERCIAL PACKAGING AUDIT (08/09/2026) — CONCLUÍDA

**Escopo:** auditoria somente — baseline antes da 15B. Relatório: `Docs/qa/PRIMOX-COMMERCIAL-PACKAGING-AUDIT.md`.
### RELEASE GATE — 1.0.0 (08/09/2026) — GO

| Campo | Valor |
|-------|-------|
| RC analisado | `1.0.0-rc.1` |
| HEAD analisado | `b184501` |
| Build | PASS (0 erros) |
| QaEngine | **37/37 PASS** (`2026-09-08_08-33-29`) |
| Deep QA | **6/6 PASS** (`2026-09-08_08-40-28`) |
| Long Run | **5 ciclos PASS** (+ operacional 3) |
| Regressões | **0** |
| Decisão | **GO** → promovido para **1.0.0** |
| Relatório | `Docs/qa/PRIMOX-RELEASE-GATE-1.0.0.md` |

**Limitações (não bloqueiam GO):** Calendar Dark header (KNOWN); NF-e real (NOT TESTABLE); ícone de fase (NOT FOUND); deploy instalado RC (CONDITIONAL — exe instalado ainda 0.0.0.0); FuncionariosViewModel (ORPHAN RETAINED).

**WIP preservado:** HelpControl; Scripts deploy (fora do commit de release).

### PRIMOX — Redesign controlado (reconstrução)

Redesign anterior **não recuperável** via Git/stash/reflog (opção B confirmada). Reconstrução faseada.

| Fase | Escopo | Status |
|------|--------|--------|
| 1 | Design System | **VALIDADO** (`6817d0f`) |
| 2 | Application Shell | **VALIDADO** (`a691e9b`) |
| 3 | Centro de Operações | **VALIDADO** (`5a41821`) |
| 4 | Componentes Globais | **VALIDADO** (`827c3dd`) |
| 5 | Ordens de Serviço / Dossiê Técnico | **VALIDADO** (`d274993`) |
| 6 | Clientes + Veículos | **VALIDADO** (`0cf595a`) |
| 7 | Agenda / Central de Agendamentos | **VALIDADO** (`9da7d63`) |
| 8 | Estoque / Central de Peças | **VALIDADO** (`e962bb9`) |
| 9 | Financeiro / Central Financeira | **VALIDADO** (`25b1a5c`) |
| 10 | Relatórios / Central de Inteligência | **VALIDADO** (`af3257e`) |
| 11 | Dark Mode / Deep QA | **VALIDADO** (`36aef40`) |
| 12 | Accessibility + Interaction Hardening | **VALIDADO** (`62aecc9`) |
| 12B | PRIMOX QA Engine (funcional/persistência) | **VALIDADO** (`8511c48`) |
| 13 | PRIMOX QA Coverage Expansion | **VALIDADO** (`7fa1f63`) |
| 14 | Finalization / Release Candidate Audit | **VALIDADO** (`df25dc4` + `fbf211d` + docs `71a305a`) |
| 15A | Commercial Packaging Audit | **CONCLUÍDA** (docs only) |
| 15B | Commercial Packaging & Deployment | **GO** (Inno + pipeline; ver relatório) |
| 15C | Installation E2E Hardening | **GO** (`PRIMOX-INSTALLATION-E2E-REPORT.md`) |
| 15D | Legacy Install Cleanup & Commercial Readiness | **GO** (`PRIMOX-LEGACY-CLEANUP-REPORT.md`) |
| 15E | Complete UI Interaction / Visual QA | **VALIDATED** — GO WITH KNOWN LIMITATIONS (`PRIMOX-COMPLETE-UI-AUDIT-15E-REPORT.md`) |
| 16+ | Site / licença / auto-update comercial | **NÃO INICIADO** (decisão humana) |

#### Fase 14 — PRIMOX Finalization / Release Candidate (08/09/2026) — VALIDADO

**Objetivo:** fechar o produto tecnicamente (auditar, evidenciar, documentar) — **sem novos módulos**.

**Congelamento**
- Branch: `main`
- HEAD pré-fase: `844ce25`
- WIP preservado (não commitado nesta fase): `HelpControl.xaml(.cs)`, `Scripts/Atualizar-PrimoAuto.bat`, `Scripts/Deploy-ToInstalledApp.ps1`

**Inventário real**
- Módulos canônicos: **17**
- Windows: **51**
- UserControls: **28**
- Click handlers: **392**
- Botões runtime (módulos): **277**
- Ações inventariadas: **419**
- Matriz: **537** linhas → `Docs/qa/primox-coverage-matrix-fase14.md`

**QaEngine:** **37/37 PASS** (`2026-09-08_07-16-02` regressão pós-versão; baseline finalização `2026-09-08_07-04-16`)  
Novos checks Finalization:
- InventarioCompleto, WindowAudit, ButtonAuditSafe, AccessibilityFormal
- HardcodedColorAudit, VersionAndPhaseIcon, LongRun5Ciclos, CoverageMatrix

**Deep QA:** **6/6 PASS** (`2026-09-08_07-21-52` regressão final; anterior `2026-09-08_07-10-03`)

**Long Run:** **5 ciclos** (~109 s / 75 navegações) + LongRun operacional 3 ciclos (Fase 13)

**Versão**
- `Properties/AssemblyInfo.cs`: AssemblyVersion `1.0.0.0`, InformationalVersion **`1.0.0-rc.1`**
- Recomendação comercial: manter RC até confirmação de GA

**Classificações definitivas**
| Item | Classificação |
|------|---------------|
| Calendar Dark header nativo | 🟡 KNOWN LIMITATION |
| Cores hardcoded (~345 hex / ~71 RGB) | 🟡 KNOWN LIMITATION (sem mass-replace) |
| NF-e emissão real | 🔵 NOT TESTABLE |
| Exclusão real produção | 🔵 NOT TESTABLE (dialog-only / DB isolado) |
| FuncionariosViewModel | ORPHAN CANDIDATE — RETAINED |
| Indicador/ícone de fase | NOT FOUND / mecanismo inexistente |
| Clique 100% botões execução real | **não reivindicado** |
| Site PRIMOX | 🟠 OUT OF SCOPE |

**Bugs críticos novos:** **0**  
**SQL/Schema:** NÃO ALTERADO  
**Regras de negócio:** NÃO ALTERADAS  
**Redesign:** NÃO

**Arquivos**
- `UiSmokeTestService.PrimoxQa.Finalization.cs` (novo)
- `Properties/AssemblyInfo.cs` (versão RC)
- `Docs/qa/FASE14-RELEASE-CANDIDATE-REPORT.md`
- `README.md` (atualizado)

**Recomendação:** **RELEASE CANDIDATE**  
**Próximo passo:** revisão humana — **PARAR** (não iniciar Fase 15 / site).

#### Fase 13 — PRIMOX QA Coverage Expansion (08/09/2026) — VALIDADO

**O QaEngine foi expandido para cobertura funcional profunda dos módulos prioritários.**

**Cobertura anterior (Fase 12B):** 14 checks QaEngine  
**Cobertura atual:** **29/29 PASS** (`2026-09-08_06-50-33`)

**Novos checks (persistência real UI→repo/DB)**
- OS: create/emitir + update/repeat + cancel não persiste
- Orçamento: update + aprovar + converter OS + PDF + idempotência
- Agenda: confirmar + check-in + converter OS
- Estoque: entrada/saída + histórico
- Financeiro: baixa conta receber + reconsulta
- PDV: venda + cancelamento com estorno de estoque (DB isolado)
- Fornecedores: edit + cancel
- Kanban: avanço StatusKanban + evento
- NFe: importação simulada + rollback (sem transmissão real)
- Relatórios: PDF/Excel gerados (tamanho > 0)
- Clientes: CREATE + veículo vinculado + cancel edit
- Veículos: CREATE + persistência
- Relacionamentos Cliente↔Veículo↔OS↔Orçamento
- Negativos: pesquisa sem resultado
- LongRun: **3 ciclos** + Orcamentos + Kanban

**Preservados:** todos os checks Fase 12, incl. `QaEngine:FuncionarioSalarioZeroEdicao`

**Deep QA:** **6/6 PASS** (`2026-09-08_06-53-01`) — não reduzido

**Arquivos:** `UiSmokeTestService.PrimoxQa.Coverage.cs` (novo); `UiSmokeTestService.PrimoxQa.cs` (wire + LongRun 3)

**SQL / Schema:** NÃO ALTERADO  
**Regras de negócio:** NÃO ALTERADAS

**PENDENTE**
- Clique destrutivo botão-a-botão exaustivo ainda parcial
- Cobertura 100% de todos os botões de todos os módulos: **não reivindicada**
- HelpControl / Scripts deploy: WIP local fora do commit

#### Fase 12B — PRIMOX QA Engine (08/09/2026) — VALIDADO

**O Deep QA da Fase 11 foi preservado e expandido para uma infraestrutura permanente de testes funcionais do PRIMOX.**

**Motivação:** edição de Funcionário falhava na operação real (salário 0 bloqueava UPDATE) enquanto a tela “abria” — smoke insuficiente.

**Arquitetura**
- `PrimoxQaEngine` — inventário reflection + relatório de cobertura
- `UiSmokeTestService.PrimoxQa.cs` — runner funcional (filtro `QaEngine` / `FunctionalQa` / `PrimoxQa`)
- Banco isolado `ui-smoke-test-*` (nunca produção)
- Validação em dois níveis: UI + re-leitura repository/DB
- DeepQa Fase 11 intacto (`UiSmokeTestService.DeepQa.cs`)

**Bug encontrado e corrigido (escopo seguro)**
- **CRÍTICO/ALTO:** `EditarFuncionarioWindow` + `FuncionarioRepository.PrepararEValidarFuncionario` rejeitavam `Salario = 0` (`permitirZero: false`), impedindo salvar qualquer alteração em colaboradores sem salário cadastrado
- Correção: permitir zero na edição/repositório; parse de salário culture-aware; `x:Name="SalvarButton"` para automação
- Smoke `Funcionarios:CadastroEdicaoPelaTela` agora valida **Nome + Telefone** persistidos

**QaEngine — execução** (`2026-09-08_03-03-21`): **14/14 PASS**
- Inventário, descoberta de botões, salário-zero, CRUD completo + repetição + bloqueio/reativação, negativo, cliente/veículo persistência, navegação, teclado, Light/Dark, resize 1366–2560, destrutivo só-dialog, LongRun 2 ciclos, relatório

**Regressão**
- Funcionarios: **3/3 PASS** (`2026-09-08_03-05-22`)
- DeepQa: **6/6 PASS** (`2026-09-08_03-05-37`) — InventarioPermanente, LongRun, A11y, Capturas, Botoes, Funcionarios

**SQL / Schema:** NÃO ALTERADO  
**Regras de negócio:** NÃO ALTERADAS (apenas validação de salário zero alinhada a dados legados)

**PENDENTE / LIMITAÇÕES**
- Cobertura CRUD UI exaustiva de OS/Orçamentos/PDV/NFe ainda via smokes de módulo (não todos reescritos no QaEngine)
- Exclusão real só em DB isolado; produção nunca tocada
- Clique destrutivo exaustivo botão-a-botão: parcial (dialog-only)

#### Fase 12 — Accessibility + Interaction Hardening (08/09/2026) — VALIDADO

**Deep QA tornou-se infraestrutura permanente de qualidade do projeto.**

**Baseline Deep QA (pré-mudanças):** PASS (`2026-09-08_01-45-30`)  
**Deep QA final:** PASS — 6 checks (InventarioPermanente, LongRun, A11y, Capturas multi-res, BotoesEnumeracao, Funcionarios)

**Permanência**
- `INavigationService.GetCanonicalModuleNames()` + implementação em `NavigationService`
- DeepQa/Tema consomem o inventário canônico (novos módulos no mapa entram automaticamente; alias `Ajuda` excluído)
- Cobertura **não reduzida** vs Fase 11 (≥16 módulos; Help/Dashboard/Funcionarios obrigatórios)

**Acessibilidade / interação corrigidas**
- Focus ring Primox em CheckBox/RadioButton (`NativeChrome`) + borda `IsKeyboardFocused`
- FocusVisualStyle Primox em ComboBox/DatePicker (`Inputs`)
- CalendarDayButton: PrimoxFocusVisual (CalendarItem **permanece BLOQUEADO**)
- ToolTip + AutomationProperties: PDV `+/-/X`, GlobalSearch limpar, Catálogo ações, Theme/Density

**QA visual:** `Logs/qa-visual/fase12-a11y` — Light/Dark × 1366 (12 módulos) + subset em 1600/1920/2560

**Arquivos alterados:** NativeChrome, Inputs, Calendar, PDV, GlobalSearch, CatalogoPecas, MainWindow, INavigationService, NavigationService, UiSmokeTestService(.DeepQa/.Theme/.cs), PROJECT_STATUS  
**Criados:** nenhum (DeepQa expandido in-place)  
**Removidos:** nenhum

**Evidências:** Build 0 erros · DeepQa + Tema + regressão completa PASS · SQL/schema **NÃO ALTERADO** · regras **NÃO ALTERADAS**

**Indicador visual:** deploy desktop após commit. Ícone de fase dedicado: **não localizado**.

**PENDENTE**
- Calendar header/semana nativo Dark (pré-existente; CalendarItem bloqueado)
- Hex remanescente OS print / Veículos chips
- `AccessibilityService` ainda sem wire no shell (helper órfão)
- Tooltips em todos os botões textuais de Orçamentos/Kanban (baixa prioridade — já têm Content)
- `FuncionariosViewModel` órfão
- Clique destrutivo exaustivo (não executado de propósito)

#### Fase 11 — Dark Mode / Deep QA (07/09/2026) — VALIDADO

**Objetivo:** auditoria profunda do sistema + refinamento Dark Mode — sem alterar schema nem regras de negócio.

**Auditoria — inventário real**
- **17 módulos navegáveis:** Dashboard, Agendamentos, Orcamentos, OrdensServico, OficinaKanban, PDV, ImportarNFe, Clientes, Veiculos, AutoEletricaTecnica, Estoque, CatalogoPecas, Fornecedores, Funcionarios, Financeiro, Relatorios, Help
- **~49 Windows** em `Views/` + MainWindow; Configurações via janela (F12)
- **Infraestrutura de teste criada:** `UiSmokeTestService.DeepQa.cs` (long-run, capturas Light/Dark 1366×768, Funcionários especial); Tema expandido para 17 módulos; timeout DeepQa 10 min

**Problema crítico corrigido — Funcionários**
- XAML com atributos fora das tags + code-behind incompleto (botões sem Click, `UltimoPainelOperacional` nunca preenchido, grade `FuncionarioListItem` vs smoke `Funcionario`)
- **Correção:** restaurada UI operacional real (handlers, painel, bloquear/reativar, busca) a partir do checkpoint funcional — **sem** alterar regras/repository

**Dark Mode — correções**
- `OrcamentoStatusControl` / `OrcamentoAlertasControl` / preço em `OrcamentoProdutosPanelControl` → tokens/badges PRIMOX
- `GlobalSearchControl` ícones de tipo → card brushes dinâmicos + encoding `Veículo`
- `Badges.xaml` textos semânticos → `Success/Warning/Danger/InfoBrush` (melhor contraste Dark)

**Long run / DeepQa**
- 2 ciclos × 17 módulos + retorno Dashboard; Light e Dark na mesma sessão (~30s)
- 24 capturas PNG em `Logs/qa-visual/fase11-deep/*-{Light|Dark}-1366x768.png`
- Funcionários: painel operacional + botões + busca empty/restore PASS

**Matriz (resumo):** todos os 17 módulos Abrir/Carregar/Dark/Light PASS via DeepQa+Tema; CRUD profundo coberto pelos smokes dedicados onde existem (Clientes, Veículos, OS, Estoque, Financeiro, Relatórios, Funcionários, PDV, Orçamentos, Fornecedores, Kanban, NFe, Configurações, Agenda). Clicks destrutivos em massa **não** automatizados de propósito.

**Arquivos alterados:** FuncionariosControl.xaml(.cs), OrcamentoStatus/Alertas/ProdutosPanel, GlobalSearchControl, Badges.xaml, UiSmokeTestService.cs/.Theme.cs, PROJECT_STATUS.md  
**Arquivos criados:** `Services/UiSmokeTestService.DeepQa.cs`  
**Removidos:** nenhum

**Evidências:** Build 0 erros · DeepQa + Tema + regressão completa PASS · SQL/schema **NÃO ALTERADO** · regras **NÃO ALTERADAS** · CalendarItem **BLOQUEADO** (intacto)

**Indicador visual:** deploy desktop após commit. Ícone de fase dedicado: **não localizado**.

**PENDENTE**
- Contraste header/semana Calendar nativo Dark (pré-existente; CalendarItem bloqueado)
- Hex hardcoded remanescente em `OrdensServicoControl.xaml.cs` (print/preview) e chips em `VeiculosControl.xaml.cs`
- Capturas 1600/1920/2560 (apenas 1366 nesta fase)
- `FuncionariosViewModel` permanece registrado mas a tela restaurada não o usa (órfão pré-migração)
- Clique exaustivo de 100% dos botões (risco CRUD) — DeepQa enumera; smokes dedicados cobrem fluxos críticos

#### Fase 10 — Relatórios / Central de Inteligência Operacional (07/09/2026) — VALIDADO

**Conceito:** Relatórios = consumo read-only de dados reais (OS, vendas, estoque, financeiro, auditoria) com período explícito — sem KPIs inventados.

**Domínio encontrado (ativo)**
- UI: `UserControls/RelatoriosControl.xaml(.cs)` + `ViewModels/RelatoriosViewModel.cs` (navegação `"Relatorios"`)
- Consultas: `Services/RelatorioDatabaseService.cs` (agregações reais SQLite)
- Exportação pré-existente: `Services/RelatorioExportService.cs` (PDF PdfSharpCore, Excel/CSV EPPlus, pacote evidências)
- DTOs: `Models/Relatorio.cs`
- Smoke: `Services/UiSmokeTestService.Relatorios.cs`
- Permissões: `RELATORIOS_VER` / `EXPORTAR` / `IMPRIMIR`
- Órfão (não usado nesta fase): `RelatoriosModernoViewModel` (DI, TODOs, fora da navegação)

**Fontes de dados (métricas reais)**
- **Faturamento / ticket médio:** tabela `Vendas` via `ObterFaturamentoTotal` / `ObterTicketMedio` (AVG vendas concluídas — **não** faturamento÷qtd OS)
- **DRE / conciliação / inadimplência:** domínio financeiro Fase 9 (`MovimentacoesFinanceiras`, `ContasReceber`, etc.) via mesmas consultas do serviço de relatório
- **OS:** abertas / finalizadas / por técnico / serviços / lucro por serviço (StatusKanban / itens reais)
- **Estoque:** curva ABC, margem produto, produtos parados (≥90 dias) — saldo **não** recalculado; usa `QuantidadeEstoque` existente
- **Clientes / auditoria / consistência operacional:** consultas já existentes no snapshot

**UI PRIMOX nesta fase**
- `ModulePageHeader` + `PageActionBar` + OpsPulse (faturamento vendas, ticket médio vendas, OS abertas/finalizadas, clientes cadastro)
- Período rápido: Hoje / Ontem / 7 dias / 30 dias / Mês atual / Mês anterior → define `DataInicio`/`DataFim` + `AplicarFiltrosAsync` (mesmas queries)
- Loading / Empty / Error + conteúdo carregado; F5 atualiza
- Grades nomeadas preservadas para smoke; exportações reais reutilizadas

**Métricas deliberadamente não inventadas / pendentes**
- Ticket médio como faturamento÷OS (**não** implementado — definição correta é AVG vendas)
- Drill-down Resumo→OS/Cliente/Produto (**PENDENTE** — sem navegação fictícia)
- Novos gráficos decorativos (**não** criados)
- Relatório dedicado de Agenda na UI (**não** expandido além do que o serviço já agrega; sem mock)
- Dualidade pré-existente: header faturamento = Vendas vs DRE = MovimentacoesFinanceiras (documentada; regras **não** unificadas nesta fase)

**Arquivos alterados:** `UserControls/RelatoriosControl.xaml(.cs)`, `ViewModels/RelatoriosViewModel.cs`, `Services/RelatorioExportService.cs` (EPPlus 8 License API), `PROJECT_STATUS.md`

**Arquivos criados / removidos:** nenhum

**Evidências:** Build 0 erros · smokes Dashboard/Tema/Calendar/Sidebar/CommandCenter/Components/OS/Clientes/Veiculos/Agendamentos/Estoque/Financeiro/**Relatorios** PASS · SQL/schema **NÃO ALTERADO** · regras de negócio **NÃO ALTERADAS**

**Visual / a11y / performance (honestidade)**
- Light/Dark: tokens PRIMOX + smoke `Tema` PASS (sem screenshots dedicados Relatórios nesta sessão)
- Responsive: shell com ScrollViewer como Fases 8–9; matriz 1366–2560 **sem** captura visual dedicada nesta sessão
- Accessibility: labels textuais no pulse, F5, focus tokens globais; auditoria formal a11y **não** instrumentada
- Performance: snapshot agregado em `Task.Run` preservado; sem polling novo de relatórios

**Indicador visual de área de trabalho:** atalho instalado atualizado via `Scripts/Deploy-ToInstalledApp.ps1` após o commit (scripts permanecem fora do Git). Mecanismo dedicado de “ícone de fase” separado: **não localizado**.

**PENDENTE**
- Drill-down real para OS/Cliente/Produto
- Unificar definição de faturamento (Vendas vs DRE) em produto futuro, se desejado
- Screenshots QA visual Relatórios Light/Dark × resoluções
- Filtro Operador na UI ainda não propagado às queries (limitação pré-existente)

#### Fase 9 — Financeiro / Central Financeira (07/09/2026) — VALIDADO

**Conceito:** Financeiro = Central Financeira (entradas, saídas, vencimentos, atrasos, baixas e origem real)

**Mapa do domínio (somente dados reais)**
- **Entidades:** `ContaPagar` / `ContaReceber` (classes na `FinanceiroViewModel`) + tabelas `ContasPagar`, `ContasReceber`, `MovimentacoesFinanceiras`, `MetasFinanceiras`, `CaixaSessoes`, `MovimentacoesCaixa`
- **Campos reais:** valor, vencimento, status, descrição, fornecedor/cliente, forma pagamento, origem/referenciaExterna, observações
- **Operações reais:** `FinanceiroDatabaseService.Adicionar*` / `BaixarContaPagar` / `BaixarContaReceber` / movimentações; UI chama `RegistrarPagamentoContaPagar` / `RegistrarRecebimentoContaReceber`
- **Status:** Pagar → Pendente/Paga · Receber → Pendente/Pago/Parcial/Cancelado (validação service)
- **Origens existentes:** OS, Orçamento, Agendamento, NF-e, liquidação de contas, PDV/Caixa (movimentações)
- **Filtros reais:** todas / vencidas / hoje / semana (+ busca textual por campos existentes)
- **KPIs/Pulse:** a receber, a pagar, vencendo hoje, vencidas, saldo do período (derivados das coleções/serviço existentes) + alertas `AlertasDivergencia` + plano executivo já existente
- **Create/edit/cancel/delete contas na tela:** PENDENTE (não inventado)
- **MetasFinanceiras na UI:** PENDENTE
- **Navegação profunda OS/Cliente/Fornecedor a partir da ficha:** PENDENTE (origem/referência exibidas)

**UI**
- `ModulePageHeader` + `PageActionBar` + OpsPulse + Loading/Empty/Error
- Contas com busca, filtros, badges de status, coluna Origem, ficha da seleção
- Baixas preservadas via `CriticalActionDialog` + service de domínio
- Export/PDF/Imprimir preservados

**Arquivos alterados:** `UserControls/FinanceiroControl.xaml(.cs)`, `ViewModels/FinanceiroViewModel.cs`, `PROJECT_STATUS.md`

**Arquivos criados / removidos:** nenhum

**Evidências:** Build 0 erros · smokes Dashboard/Tema/Calendar/Sidebar/CommandCenter/Components/OS/Clientes/Veiculos/Agendamentos/Estoque/**Financeiro** PASS · SQL/schema **NÃO ALTERADO** · regras financeiras **NÃO ALTERADAS**

**Indicador visual de área de trabalho:** atalho instalado atualizado via `Scripts/Deploy-ToInstalledApp.ps1` após o commit (scripts permanecem fora do Git). Mecanismo dedicado de “ícone de fase” separado: **não localizado**.

**PENDENTE**
- CRUD de contas pela UI Financeiro
- MetasFinanceiras na UI
- Navegação para OS/Cliente/Fornecedor a partir de Origem/ReferenciaExterna
- Align VM Entrada/Saida com tipos Receita/Despesa nos cards (limitação pré-existente)

#### Fase 8 — Estoque / Central de Peças (07/09/2026) — VALIDADO

**Conceito:** Estoque = Central de Peças e Materiais (o que tenho / quanto / onde / custo / o que acaba / o que foi usado)

**Mapa do domínio (somente dados reais)**
- **PRODUTO (`Models/Produto.cs`):** Codigo, Nome/Descricao, Categoria, Marca/Modelo, Fornecedor(+Id/CNPJ/contato), QuantidadeEstoque/Minima/Maxima, Localizacao/Prateleira/Gaveta, PrecoCompra/PrecoVenda/MargemLucro/ValorTotalEstoque, UnidadeMedida, CodigoBarras/SKU/NCMS/CEST/CFOP, Ativo, perecível/validade, TotalVendas/VendasUltimoMes, QuantidadeReservada, QuantidadeDisponivel (calculado)
- **ESTOQUE:** não há entidade separada — saldo vive no Produto (`QuantidadeEstoque` + reservas)
- **MOVIMENTAÇÕES:** `EstoqueOperationalService.RegistrarMovimentacaoManual` (Entrada/Saida + audit `EntradaEstoqueDedicada`/`SaidaEstoqueDedicada`), `RegistrarInventario`, ajuste via `AjusteEstoqueWindow`; histórico via `ObterHistoricoProduto` (AuditLogs — sem tabela MovimentacaoEstoque)
- **FORNECEDOR:** campos no Produto; filtro/combo existentes; Fornecedores **não** redesenhados
- **OS:** baixa real em `OrdemServicoRepository.AplicarBaixaEstoqueSeNecessaria` (itens Tipo=`Peca` + ProdutoId) — **UI de utilização em OS nesta tela: PENDENTE** (sem alterar OS)
- **VENDA/PDV:** baixa via `VendaService` existente — preservada; sem novo fluxo

**UI**
- `ModulePageHeader` + `PageActionBar` + OpsPulse (total / valor / abaixo do mínimo / zerados / mais vendido — métricas reais)
- Loading / Loaded / Empty / Error
- DataGrid global + busca (codigo/nome/SKU/barras) + filtros categoria/fornecedor/status operacional (Estoque Baixo/Alto, Parados, Sem Codigo/SKU, Sem Preco, Sem Fornecedor, Margem Baixa, Curva A/B/C, Mais Vendidos, etc.)
- Badges de status (OK / Baixo / Zerado) + painel de insights + ficha do produto selecionado
- Ações reais: Novo/Editar/Entrada/Saida/Ajuste/Inventario/Etiqueta/Historico/Excluir (ConfirmationDialog)
- Quantidade **não** editada direto na UI — só via serviços/operações existentes

**Arquivos alterados:** `UserControls/EstoqueControl.xaml(.cs)`, `PROJECT_STATUS.md`

**Arquivos criados / removidos:** nenhum

**Evidências:** Build 0 erros · smokes Dashboard/Tema/Calendar/Sidebar/CommandCenter/Components/OS/Clientes/Veiculos/Agendamentos/**Estoque** PASS · Light/Dark tokens PRIMOX · SQL/schema **NÃO ALTERADO** · regras de estoque **NÃO ALTERADAS**

**PENDENTE**
- Painel de utilização do produto em OS/Vendas (relação existe no domínio; UI da central ainda não lista OS/vendas por produto)
- ModulePageHeader nos demais módulos fora do escopo (Financeiro/PDV = fases futuras)

#### Fase 7 — Agenda / Central de Agendamentos (07/09/2026) — VALIDADO

**Conceito:** Agenda = Central de Compromissos Operacionais

**Mapa de dados (domínio real — `Models/Agendamento.cs`)**
- Identidade: Id, Numero, DataAgendamento, HoraInicio/HoraTermino, DuracaoEstimada/Real
- Status reais: Agendado, Confirmado, Aguardando Cliente, Em Andamento, Aguardando Peça, Pausado, Finalizado, Cancelado, Entregue
- Cliente: ClienteId + snapshots · Veículo: VeiculoId + placa/modelo/marca/**VeiculoQuilometragem** · Técnico · OS: OrdemServicoId/NumeroOS · Observacoes
- Serviço: TipoServico, DescricaoServico, Prioridade · Valores estimados/reais
- **Calendar:** `calendarControl` preservado — **sem** DisplayDateStart/End · **sem** CalendarItem custom · apenas DayButton/Button theming

**UI**
- `ModulePageHeader` + `PageActionBar` + OpsPulse (Hoje/Pendentes/Em andamento/Concluídos/Cancelados — contagens reais)
- Loading / Error (full) + Empty da lista no período (calendário permanece)
- Detalhe: DataAgendamento corrigido, km, duração, OS, navegação Cliente/Veículo/OS quando IDs válidos
- Removidos percentuais inventados dos cards legados (`AtualizarDashboardCards`)

**Arquivos alterados:** `UserControls/AgendamentosControl.xaml(.cs)`, `ViewModels/AgendamentosViewModel.cs`, `Services/UiSmokeTestService.Agendamentos.cs`, `PROJECT_STATUS.md`

**Arquivos criados / removidos:** nenhum · **Themes/Calendar.xaml:** NÃO alterado

**Evidências:** Build 0 erros · smokes Dashboard/Tema/**Calendar**/Sidebar/CommandCenter/Components/OS/Clientes/Veiculos/**Agendamentos** PASS · SQL/schema **NÃO ALTERADO** · regras **NÃO ALTERADAS**

**PENDENTE**
- Validação de conflito de horário (domínio não possui)
- Contrast Dark do header nativo CalendarItem (sem custom template)
- Novo agendamento com ClienteId/VeiculoId reais no fluxo stub
- ModulePageHeader nos demais módulos fora do escopo

#### Fase 6 — Clientes + Veículos (07/09/2026) — VALIDADO

**Conceitos:** Cliente = Perfil de Relacionamento · Veículo = Prontuário Técnico

**Mapa de dados (domínio real)**
- **Cliente:** Nome, TipoPessoa, CPF/RG, contatos (Telefone/WhatsApp/Email), endereço, VIP/Ativo, TotalGasto/TotalServicos/Pontos, LGPD, Observacoes, mídia, Veiculos, UltimaVisita
- **Veículo:** ClienteId, Marca/Modelo/Ano/Cor/Placa, Chassi/Renavam, Tipo, sistema elétrico, baterias/testes, **Quilometragem (existe)**, HistoricoTecnico, observações técnicas, datas retorno/garantia/revisão, FotosTecnicas
- **Relações:** Cliente ↔ Veiculos; OS via ClienteId (`ObterPorClienteId`) e VeiculoId/PlacaSnapshot (filtro em memória); Eventos via OS; Agendamentos/Orcamentos por vínculo existente
- Quilometragem em OS: **PENDENTE** (Fase 5; sem schema nesta fase)
- `ObterPorVeiculoId` no repositório: **PENDENTE** (UI usa filtro seguro VeiculoId ‖ PlacaSnapshot)

**UI**
- `ClientesControl` / `VeiculosControl`: `ModulePageHeader` + `PageActionBar` + `OpsPulseCard` + Loading/Empty/Error
- Perfil rápido do cliente selecionado (frota + OS reais)
- `VisualizarClienteWindow`: Perfil de Relacionamento (min size 1366-friendly)
- `VisualizarVeiculoWindow`: Prontuário Técnico + resumo OS + histórico enriquecido + **Timeline técnica** (OrdemServicoEventos das OS) + abrir proprietário; cache de OS (sem N+1)

**Arquivos alterados:** `UserControls/ClientesControl.xaml(.cs)`, `UserControls/VeiculosControl.xaml(.cs)`, `Views/Clientes/VisualizarClienteWindow.xaml`, `Views/VisualizarVeiculoWindow.xaml(.cs)`, `Services/UiSmokeTestService.Clientes.cs`, `Services/UiSmokeTestService.Veiculos.cs`, `PROJECT_STATUS.md`

**Arquivos criados / removidos:** nenhum

**Evidências:** Build 0 erros · smokes Dashboard/Tema/Calendar/Sidebar/CommandCenter/Components/OrdensServico/Clientes/Veiculos PASS · SQL/schema **NÃO ALTERADO** · regras **NÃO ALTERADAS**

**PENDENTE**
- Quilometragem no domínio OS
- `ObterPorVeiculoId` dedicado (hoje filtro em memória)
- ModulePageHeader nos demais módulos fora do escopo
- Timeline unificada Audit+OS+Financeiro (cliente)

#### Fase 5 — Ordens de Serviço / Dossiê Técnico (07/09/2026) — VALIDADO

**Conceito:** OS = Dossiê Técnico (leitura operacional rápida + editor existente).

**Mapa de dados (somente domínio real)**
- `OrdemServico`: Cliente/Veículo snapshots, Status, Prioridade, ProblemaRelatado, Diagnostico*, Observacoes*, checklists, fotos, assinatura, aprovação, datas, TecnicoId, OrcamentoId, ValorMaoObra, Desconto, Itens, Eventos
- `StatusKanban` (`OficinaProfissionalService`): Agendado→…→Entregue/Cancelado — **não alterado**; UI de lista reutiliza progresso/transições já existentes em `OrdensServicoControl`
- `OrdemServicoEventos`: Titulo, Descricao, Tipo, Usuario, DataEvento
- Relacionamentos: ClienteId + snapshots; VeiculoId + snapshots; OrcamentoId; Itens (Peca/Servico ↔ Produto); financeiro via operação existente “Gerar financeiro”
- Quilometragem dedicada: **PENDENTE** (não existe no modelo)
- Unificação Status lista OS vs StatusKanban strings: **PENDENTE** (sem inventar máquina nova)

**UI**
- `ModulePageHeader` + `PageActionBar` + pulse (`OpsPulseCard`)
- Master-detail: identidade (cliente/veículo/técnico), queixa/diagnóstico, financeiro real, itens, eventos, evidências/checklists já suportados
- Estados explícitos: Loading / Loaded / Empty / Error
- `OrdemServicoWindow`: título dossiê + MinWidth/MinHeight adequados a 1366×768 (sem mudar lógica de save/status)
- Badges/status com brushes de tema (Light/Dark)

**Arquivos alterados:** `UserControls/OrdensServicoControl.xaml(.cs)`, `Views/OrdemServicoWindow.xaml(.cs)`, `Services/UiSmokeTestService.OrdensServico.cs`, `PROJECT_STATUS.md`

**Arquivos criados / removidos:** nenhum

**Evidências:** Build 0 erros · smokes Dashboard/Tema/Calendar/Sidebar/CommandCenter/Components/OrdensServico PASS · SQL/schema **NÃO ALTERADO** · regras de negócio **NÃO ALTERADAS**

**PENDENTE**
- Quilometragem no domínio OS
- Alinhar nomenclatura de status lista OS ↔ StatusKanban sem segunda máquina de estados
- Empty state por seção (itens) mais rico; skeleton avançado
- Aplicar `ModulePageHeader` em massa nos demais módulos (Clientes/Veículos tratados na Fase 6)

#### Fase 4 — Componentes Globais (07/09/2026) — VALIDADO

**Consolidados / criados (estilos & recursos — sem mudar domínio)**
- Page Header: `ModulePageHeader` (+ Dashboard `PageHeader` preservado)
- Buttons: Primary / Secondary / Ghost·Tertiary / Danger / Success / Outline / Icon + focus
- Inputs: Focus / ReadOnly / Validation.HasError + `FormFieldLabel` / Helper / Error + `InputError`
- Badges: `StatusBadge*` Success/Warning/Danger/Info/Neutral (+ texto)
- Toast: surfaces `Toast*Surface` + ShellNotification usa brushes Toast* (Light/Dark)
- Empty / Loading / Error: `PrimoxEmptyState*` / `LoadingStatePanel` / `ErrorStatePanel`
- Dialog: `ConfirmationDialogSurface`, `ConfirmDangerButton`, `ConfirmCancelButton`
- Tooltip global + Focus `PrimoxFocusVisual`
- DataGrid: seleção via `TableSelectedBrush` (não fill Brand total) + focus cell
- Density: alturas via `DensityControlHeight`

**Arquivos novos:** `Themes/Badges.xaml`, `Themes/Feedback.xaml`, `Services/UiSmokeTestService.Components.cs`

**Evidências:** Build 0 erros · smokes Dashboard/Tema/Calendar/Sidebar/CommandCenter/Components PASS · CalendarItem custom permanece BLOQUEADO

**PENDENTE:** aplicar `ModulePageHeader` em massa nos módulos (Fase 5+); skeleton avançado

#### Fase 3 — Centro de Operações (07/09/2026) — VALIDADO

**Arquitetura**
- Page header no conteúdo (`DashboardPageHeader` / alias `PageHeader` em `Themes/Dashboard.xaml`)
- Workshop Pulse · Attention Center · Fluxo operacional (Kanban real) · Atividade recente · Ações rápidas
- Estados: Loading / Loaded / Error / Empty (atenção e atividade)

**Dados reais utilizados**
- `OrdensServico` (abertas, andamento, aguardando, atrasadas, GROUP BY Status)
- `Orcamentos` (pendentes)
- `Vendas` (faturamento do mês + 7 dias)
- `Clientes`, `Produtos` (ativos / estoque baixo)
- `Agendamentos` (hoje / atrasados)
- `OrdemServicoEventos` (timeline recente)
- Fluxo alinhado a `OficinaProfissionalService` StatusKanban (sem inventar estágios de negócio)

**PENDENTE / BLOQUEADO (sem fonte inventada)**
- Ticket médio / % conversão / faturamento projetado sem tabela: **não implementados**
- Timeline unificada AuditLogs + OS + Financeiro: **PENDENTE** (hoje só eventos de OS)

**Evidências**
- Build 0 erros
- Smoke Dashboard PASS · Tema PASS · Calendar 4/4 · Sidebar PASS · Command Center PASS
- Viewport 1366×768 exercitado no smoke Dashboard

#### Fase 2 — Application Shell PRIMOX (07/09/2026) — VALIDADO

**Implementado + validado**
- Command Bar **56px** (`PrimoxCommandBar`)
- Sidebar expandida **240px** / compacta **68px** + tooltips + reflow
- `SidebarLayoutService` + `sidebar_settings.json` (persistência / fallback seguro)
- Grupos: OPERAÇÃO · CADASTROS · GESTÃO · SISTEMA · AJUDA (destinos reais; Help no menu)
- Estado ativo: fundo + texto + indicador Brand (Light/Dark/compacto)
- Command Center (Ctrl+K): visual/agrupamento/foco; lógica de itens preservada
- Atalhos preservados: Ctrl+K, F1–F6, F12
- Login identidade PRIMOX (tokens Fase 1)
- Smokes novos: `Sidebar`, `CommandCenter`

**Não implementado (intencional)**
- Dashboard Centro de Operações
- PageHeader aplicado aos módulos
- Redesign de páginas

**Evidências**
- Build Debug: 0 erros
- Smoke Dashboard PASS · Tema 2/2 · Calendar 4/4 · Sidebar PASS · Command Center PASS  
  Logs: `Logs/smoke-tests/` (sessão 07/09/2026 ~19:00)

#### Fase 1 — Design System PRIMOX (07/09/2026) — VALIDADO

**Implementado**
- Paleta Light: Brand `#F97316`, Brand Soft `#FFF7ED`, Navy `#0B1220`, Background `#F5F7FA`, Surface `#FFFFFF` / `#F8FAFC`, texto/borda/semântico + aliases `Brand*` / `Navy*` / `TechnicalInfo*` / `Toast*`
- Paleta Dark própria (não inversão): Background `#0B1220`, Surface `#111827` / `#172033`, Border `#263247`
- Tipografia Segoe UI + aliases Display/Page/Section/Subsection/Body/Caption
- Spacing 4…32 (+ extensão), ControlHeight SM–XL, CornerRadius SM…Full / cards 10–12
- Elevation 0–3; motion ~150/220 ms (sem pulse infinito)
- Focus ring global Brand (`PrimoxFocusVisual` / `ButtonKeyboardFocusVisual`)
- `StandardTheme.xaml` e `Colors.xaml` marcados **LEGADO** (não mergeados em `App.xaml`; arquivos mantidos)

**Não implementado nesta fase (intencional)**
- Command Bar / Sidebar compacta / SidebarLayoutService / Command Center
- Dashboard Centro de Operações / PageHeader aplicado a módulos

**Evidências**
- Build Debug: 0 erros
- Smoke: `Dashboard` PASS · `Tema` 2/2 PASS · `Calendar` 4/4 PASS  
  Logs: `Logs/smoke-tests/ui-smoke-2026-09-07-18-46-32-*` (Dashboard), `…18-46-57-*` (Tema), `…18-47-06-*` (Calendar)
- CalendarItem custom: permanece **BLOQUEADO** (comportamento preservado)

### Fase 9 / 9.5 — Encerramento (07/09/2026)

| Item | Resultado |
|------|-----------|
| Command System (Ctrl+K, F5 refresh, F6 Estoque) | Recuperado e commitado |
| SQLite smoke isolado | PASS (não toca AppData de produção) |
| Calendar interação | Corrigido (sem CalendarItem custom; sem DisplayDateStart/End no filtro) |
| QA visual Light (Agendamentos Calendar) | VALIDADO (screenshot + smoke) |
| QA visual Dark (dias/seleção/today) | VALIDADO |
| QA visual Dark (header/semana) | PENDÊNCIA: baixo contraste do CalendarItem nativo |
| Commits locais | `db8b337`, `fc5f2fe`, `bce46ac` (+ QA visual) |

Evidências: `PrimoAutoEletrica/bin/Debug/net6.0-windows/Logs/qa-visual/agendamentos-calendar-{light,dark}.png`

---

## 🎯 EXECUTIVE SUMMARY (TL;DR)

| Aspecto | Status | Score | Ação |
|---------|--------|-------|------|
| **Arquitetura** | ✅ Sólida | 80/100 | Manutenção |
| **Segurança** | 🟡 EM PROGRESSO | 60/100 | Continuar |
| **Funcionalidades** | 🟡 Incompletas | 75/100 | 3 semanas |
| **UX/UI** | 🟡 Melhorando | 65/100 | 6 semanas |
| **Documentação** | 🟡 Em Progresso | 60/100 | 2 semanas |
| **Dark Mode** | 🟡 Corrigido (3/4) | 75/100 | Verificar ComboBox |
| **Performance** | 🟡 Otimizável | 70/100 | 6 semanas |
| **Testes** | ✅ Completos | 80/100 | Manutenção |
| **LGPD/Compliance** | 🟡 Parcial | 40/100 | Continuar |

**Conclusão**: Projeto viável com **investimento de R$ 87.000 em 16 semanas** para atingir **95+/100 e conformidade enterprise**.

---

## � DOCUMENTOS EXTERNOS DE REFERÊNCIA

### Documentos em `C:\Users\campo\Downloads\files`

**SUMÁRIO EXECUTIVO & ROADMAP** (`SUMARIO_EXECUTIVO_E_ROADMAP.md`)
- Roadmap completo de 16 semanas para transformação enterprise
- Análise financeira e ROI esperado (+3.900% em 12 meses)
- Investimento total estimado: R$ 87.000 (580 horas)
- Priorização de tarefas por impacto comercial

**GUIA PRÁTICO DE IMPLEMENTAÇÃO** (`GUIA_IMPLEMENTACAO_PRATICA.md`)
- Código pronto para usar para cada funcionalidade
- Passo-a-passo detalhado com exemplos XAML e C#
- Implementação de segurança, dashboard, help, RBAC
- Referência técnica para desenvolvimento

**RELATÓRIO COMPLETO DE ANÁLISE** (`RELATORIO_ANALISE_COMPLETA_PRIMO.md`)
- Análise profunda de cada área do sistema
- Detalhes de bugs, vulnerabilidades e arquitetura
- Recomendações específicas por componente
- Diagnóstico completo de gaps funcionais

**CHECKLIST RÁPIDO** (`CHECKLIST_ACOES_RAPIDAS.md`)
- Lista de tarefas priorizada por urgência
- Ordem de execução recomendada
- Métricas de progresso semanal
- Referência rápida para desenvolvimento diário

---

## 🧪 RESULTADOS DA SIMULAÇÃO GERAL (06/09/2026)

### Status da Simulação: 86.3% SUCESSO (44/51 operações)

**Funcionalidades Verificadas:**
- ✅ **Veículos**: 8/8 operações (100%) - **NÃO HÁ ERRO NO CADASTRO DE VEÍCULOS**
- ✅ **Clientes**: 5/5 operações (100%) - CPF/CNPJ validados
- ✅ **Produtos**: 6/6 operações (100%) - CRUD completo
- ✅ **Ordens de Serviço**: 7/7 operações (100%) - Funcionando
- ✅ **Vendas/PDV**: 3/3 operações (100%) - Funcionando
- ✅ **Funcionários**: 1/1 operação (100%) - Leitura OK
- ✅ **Fornecedores**: 3/4 operações (75%) - CRUD básico OK
- ✅ **Integridade Referencial**: 5/5 operações (100%) - Validações funcionando
- ✅ **Segurança**: 5/5 operações (100%) - Hash de senha OK
- 🟡 **Orçamentos**: 1/6 operações (17%) - Requer vinculação com produtos

**Erros Identificados:**
- Orçamentos exigem itens vinculados a produtos válidos
- Alguns campos de modelo foram renomeados (refatoração recente)

**Conclusão da Simulação:**
- **O erro relatado pelo usuário em "adicionar novos veículos" NÃO existe no código**
- Todos os 8 testes de veículos passaram com sucesso
- O sistema está funcional para as operações principais
- Os erros são de validação de dados (CPF/CNPJ, itens de orçamento)

---

---


## Progresso do Roadmap (auditoria real — 2026-09-07)

O trecho anterior (v1.2.x) estava **desatualizado e inflado**. Status abaixo confrontado com o codigo.

| Categoria | Status real | Notas |
|-----------|-------------|-------|
| Curto prazo (seguranca/compliance) | **~85%** | Hash, lockout, audit, CORS, soft delete, 2FA login, rate limit API, LGPD anonimizar |
| Medio prazo (UX/modulos) | **~70%** | Dashboard KPIs, Help F1, RBAC, dark theme tokens; smart scheduling ainda basico |
| Longo prazo (externo) | **~15%** | SEFAZ emissao, MAUI, cloud sync, IdP OAuth real — fora do escopo in-repo |
| UX/UI modernizacao | **~75%** | Design system claro/escuro; Calendar/ComboBox/DataGrid ok; placeholder wired |

### Seguranca — checklist vs codigo

| # | Item | Status | Evidencia |
|---|------|--------|-----------|
| 1 | Senhas em texto plano | **DONE** | `PasswordHasherService` PBKDF2 |
| 2 | Lockout / forca bruta | **DONE** | `LoginTentativasSeguranca` 5 falhas / 15 min |
| 3 | 2FA TOTP | **PARCIAL** (Truth Audit 1.0) | `TwoFactorService` + setup UI; **Login sem desafio TOTP** — não marcar DONE no login |
| 4 | SQL injection | **PARTIAL** | Params na maioria; `SqlIdentifierGuard` em soft-delete |
| 5 | Criptografia CPF em repouso | **PARTIAL** | DPAPI para segredos/SQL pwd; CPF ainda plaintext (trade-off busca) |
| 6 | Auditoria | **DONE** | `AuditLogService` / `AuditTrailService` |
| 7 | CORS API | **DONE** | `RestrictiveCors` |
| 8 | Secrets hardcoded | **DONE** | Sem secrets em appsettings |
| 9 | Validacao input | **PARTIAL** | MaxLength XAML + helpers; sem DataAnnotations em Models |
| 10 | LGPD | **PARTIAL→melhor** | Consentimento + soft delete + **AnonimizarCliente** (direito ao esquecimento) |
| 11 | Rate limiting API | **DONE** (2026-09-07) | `UseRateLimiter` global 120/min |
| 12 | Security event logging | **DONE** | Login/logout/permissoes/deletes |

### Dark mode — bugs declarados

| Bug | Status |
|-----|--------|
| Calendar invisivel | **Corrigido** (`Themes/Calendar.xaml`) |
| ComboBox popup | **Corrigido** (`Themes/Inputs.xaml`) |
| DataGrid header | **Corrigido** (`Themes/DataGrid.xaml`) |
| Placeholder contraste | **Corrigido** (2026-09-07) — `InputPlaceholderBrush` no template |

### Funcionalidades — gaps declarados

| # | Funcionalidade | Status real |
|---|----------------|-------------|
| 1 | Dashboard KPIs | **DONE** |
| 2 | RBAC granular | **REAL WPF** (`PermissionService`) — API policies nomeadas **NÃO** wired |
| 3 | NF-e/Contabil | **PARTIAL** — import NF-e + export CSV; sem emissao SEFAZ |
| 4 | 2FA | **PARCIAL** — serviço existe; **não** wired no Login (Truth Audit 1.0) |
| 5 | Auditoria completa | **REAL** (serviços) — não confundir com auditoria fiscal SEFAZ |
| 6 | Help/Tutorial F1 | **PARCIAL / WIP** |
| 7 | Notificacoes avancadas | **PARTIAL** — stubs SMS/WhatsApp |
| 8 | Agendamento inteligente | **MISSING** — CRUD apenas |
| 9 | Soft delete | **DONE** (+ restore + anonimizar) |
| 10 | Mobile/Cloud sync | **MISSING** — so LAN UDP |

### Residuais honestos (nao marcar 100%)

- Emissao NF-e SEFAZ + certificado
- App MAUI / cloud sync
- OAuth2 IdP real
- Criptografia coluna-a-coluna de CPF (impacto em busca)
- Pen-test externo
---

## 📈 Progresso Geral do Roadmap (REVISADO)

### ✅ Melhorias Ativas Concluídas

- ✅ MVVM finalizado para UserControls críticos
- ✅ Suporte completo a múltiplos idiomas (PT-BR, EN)
- ✅ Integração de impressoras e diagnósticos de hardware
- ✅ Relatórios PDF/Excel funcionando
- ✅ Limpeza de código morto e auditoria estrutural
- ✅ Histórico de alterações com audit trail (COMPLETO - AuditLogService + AuditTrailService)
- ✅ Acessibilidade reforçada com atalhos e validação
- ✅ Build em Release validado sem erros de compilação
- ✅ Pipeline de CI/CD com GitHub Actions
- ✅ Backup automático do banco
- ✅ **Hash de senha com PBKDF2** (PasswordHasherService.cs - 100K iterações, salt, timing-safe)
- ✅ **Criptografia DPAPI** (CryptoService.cs - ProtectedData.Protect/Unprotect)
- ✅ **Auditoria completa** (AuditLogService.cs - 17 campos, correlationId, severidade)
- ✅ **Importação NF-e** (NFeService.cs + ImportarNFeControl.xaml - validação, conferência, conta a pagar automática)
- ✅ **2FA com TOTP** (TwoFactorService.cs - compatível com Google Authenticator) (NOVO 05/09)
- ✅ **Help/Tutorial integrado** (HelpControl.xaml - TreeView + conteúdo estruturado) (NOVO 05/09)
- ✅ **Calendar dark mode fix** (Calendar.xaml - DynamicResource) (NOVO 05/09)
- ✅ **Dashboard expandido** (DashboardViewModel.cs - 6+ KPIs, estoque baixo) (NOVO 05/09)

### 🔜 Próximas Melhorias Ativas (PRIORIZADO)

1. **🔴 CRÍTICA (Semanas 1-2)**: Segurança + Dark Mode Fixes
   - Implementar hash de senha (Argon2)
   - Implementar 2FA (TOTP)
   - Corrigir Calendar, ComboBox, DataGrid bugs
   - Criar AuditLog
   - Implementar criptografia DPAPI

2. **🟠 ALTA (Semanas 2-4)**: Dashboard + Help
   - Dashboard com KPIs em tempo real
   - Help/Tutorial integrado (F1)
   - Gráficos com LiveCharts2

3. **🟡 MÉDIA (Semanas 5-12)**: RBAC + Integrações
   - RBAC granular completo
   - NF-e / Integração contábil
   - Performance optimization

4. **🟢 BAIXA (Semanas 13-16)**: UI/UX + Mobile Prep
   - Modernização de UI
   - Responsividade
   - Preparação para mobile

---

## 🎯 Roadmap - Status por Prioridade (NOVO!)

### 1️⃣ Curto Prazo (✅ <= 2 semanas) - PARCIALMENTE COMPLETO

**Status**: 6/12 completados (50%) - **AÇÕES CRÍTICAS NECESSÁRIAS**

| Área | Item | Status | Benefício | Horas | Custo |
|------|------|--------|-----------|-------|-------|
| **SEGURANÇA** | Implementar Hash Senha | ⏳ PRÓXIMO | Protege credenciais | 20 | R$3k |
| **SEGURANÇA** | Implementar 2FA (TOTP) | ⏳ PRÓXIMO | Requer 2º fator | 15 | R$2.25k |
| **SEGURANÇA** | Criar AuditLog | ⏳ PRÓXIMO | Trail de ações | 16 | R$2.4k |
| **SEGURANÇA** | SQL Injection Fix | ⏳ PRÓXIMO | Parametrizar queries | 12 | R$1.8k |
| **DARK MODE** | Corrigir Calendar | ⏳ PRÓXIMO | Calendário visível | 5 | R$750 |
| **DARK MODE** | Corrigir ComboBox | ⏳ PRÓXIMO | Dropdown visível | 8 | R$1.2k |
| **DARK MODE** | Corrigir DataGrid | ⏳ PRÓXIMO | Header visível | 7 | R$1.05k |
| **DARK MODE** | TextBox Placeholder | ⏳ PRÓXIMO | Placeholder visível | 3 | R$450 |
| Navegação | Refatorar NavigationService | ✅ Concluído | Reduz bugs | - | - |
| Permissões | Centralizar PermissionService | ✅ Concluído | Segurança | - | - |
| UI/UX | Padronizar estilos | ✅ Concluído | Consistência | - | - |
| Documentação | Atualizar README | ✅ Concluído | Onboarding | - | - |

**Progresso Curto Prazo**: 6/12 itens = 50% ✅🔜  
**Ações Urgentes**: 8 itens críticos de segurança + dark mode  
**Custo Adicional**: ~R$ 12.9k | 86h  
**Prazo Recomendado**: SEMANA 1-2

---

### 2️⃣ Médio Prazo (⏳ 1‑3 meses) - EM ANDAMENTO + EXPANSÃO

**Status**: 8/18 completados (44%) - **EXPANSÃO NECESSÁRIA COM 10 NOVOS ITENS**

| Área | Item | Status | Benefício | Horas | Custo | Prazo |
|------|------|--------|-----------|-------|-------|-------|
| **NOVO** | Dashboard com KPIs | ⏳ CRÍTICA | Revenue tracking | 48 | R$7.2k | Sem 3-4 |
| **NOVO** | Help/Tutorial (F1) | ⏳ CRÍTICA | -80% support tickets | 40 | R$6.0k | Sem 2-3 |
| **NOVO** | RBAC Granular | ⏳ CRÍTICA | Enterprise feature | 62 | R$9.3k | Sem 5-8 |
| **NOVO** | Integração NF-e | ⏳ CRÍTICA | Automação fiscal | 80 | R$12.0k | Sem 9-12 |
| **NOVO** | Criptografia DPAPI | ⏳ CRÍTICA | LGPD compliance | 15 | R$2.25k | Sem 1-2 |
| **NOVO** | Rate Limiting Login | ⏳ ALTA | Anti-brute force | 8 | R$1.2k | Sem 1 |
| **NOVO** | Soft Delete DB | ⏳ ALTA | Reversível delete | 20 | R$3.0k | Sem 5-6 |
| **NOVO** | Performance Cache | ⏳ ALTA | 5x+ faster | 12 | R$1.8k | Sem 3-4 |
| Arquitetura | Migrar MVVM completo | 🟡 Parcial | Testabilidade | - | - | - |
| Injeção Dep. | Microsoft.Extensions.DI | ✅ Concluído | Flexibilidade | - | - | - |
| Logging | Microsoft.Extensions.Logging | ✅ Concluído | Estruturado | - | - | - |
| Relatórios | PDF/Excel export | ✅ Concluído | Automatização | - | - | - |
| Backup | Backup automático | ✅ Concluído | Data safety | - | - | - |
| Multi-idioma | PT-BR + EN | 🟡 Parcial | Localização | - | - | - |
| Testes UI | White + Appium | ✅ Concluído | Automação | - | - | - |
| **NOVO** | Security Logging | ⏳ ALTA | Forensics | 12 | R$1.8k | Sem 1-2 |
| **NOVO** | CORS Restrictivo | ⏳ ALTA | API Security | 4 | R$600 | Sem 1 |
| **NOVO** | Input Validation | ⏳ ALTA | Sanitização | 10 | R$1.5k | Sem 1-2 |

**Progresso Médio Prazo**: 8/18 = 44% ✅ + 10 NOVOS = 18/28 TOTAL  
**Custo Adicional**: R$ 45.75k | ~300h  
**Prazo Recomendado**: SEMANAS 2-12

---

### 3️⃣ Longo Prazo (📆 > 3 meses) - ESTRATÉGICO

**Status**: 1/23 completados (4%) - **NOVO ROADMAP EXPANDIDO**

| Área | Item | Status | Benefício | Horas | Custo | Prazo |
|------|------|--------|-----------|-------|-------|-------|
| **NOVO** | UI Modernização | ⏳ MÉDIA | Material Design 3 | 35 | R$5.25k | Sem 13-14 |
| **NOVO** | Acessibilidade WCAG | ⏳ MÉDIA | ADA Compliant | 25 | R$3.75k | Sem 13-15 |
| **NOVO** | Mobile Responsivo | ⏳ MÉDIA | Tablet support | 40 | R$6.0k | Sem 13-16 |
| **NOVO** | Agendamento Smart | ⏳ MÉDIA | AI allocation | 40 | R$6.0k | Sem 9-10 |
| **NOVO** | Analytics/Telemetria | ⏳ BAIXA | Usage metrics | 30 | R$4.5k | Sem 15-16 |
| Plataforma | Portar .NET 8 | ⏳ Pendente | Futuro suporte | 20 | R$3.0k | TBD |
| Web API | ASP.NET Core REST | 🟡 Parcial | Mobile API | 40 | R$6.0k | Sem 9-10 |
| Mobile | MAUI App | ⏳ Pendente | App móvel | 120 | R$18.0k | Sem 17-20 |
| Analytics | Application Insights | ⏳ Pendente | Métricas | 25 | R$3.75k | Sem 15-16 |
| ML | Previsão demanda | ⏳ Pendente | Estoque IA | 60 | R$9.0k | Sem 18-20 |
| Marketplace | Integração fornecedores | ⏳ Pendente | Auto-purchase | 50 | R$7.5k | Sem 19-22 |
| Design System | Biblioteca controles | ⏳ Pendente | Reutilização | 40 | R$6.0k | Sem 17-18 |
| Segurança | OAuth2 + OpenID | ⏳ Pendente | SSO | 35 | R$5.25k | Sem 16-18 |
| Segurança | Pen Testing | ⏳ Pendente | Audit de seg. | 40 | R$6.0k | Sem 18-19 |
| Cloud | Migrar para Azure | ⏳ Pendente | Escalabilidade | 60 | R$9.0k | Sem 20-22 |
| CI/CD | GitHub Actions Pro | ✅ Concluído | Automação | - | - | - |
| DevOps | Docker + Kubernetes | ⏳ Pendente | Containerização | 45 | R$6.75k | Sem 19-21 |
| Compliance | GDPR Audit | ⏳ Pendente | EU compliance | 30 | R$4.5k | Sem 18-20 |
| Performance | Profiling completo | ⏳ Pendente | Otimização | 25 | R$3.75k | Sem 15-16 |

**Progresso Longo Prazo**: 1/23 = 4% (era 0%)  
**Novo Roadmap Expandido**: 23 itens totais  
**Custo Adicional**: R$ 124.5k | ~750h  
**Prazo**: SEMANAS 13+ (paralelo com fases anteriores)

---

## ✅ Tarefas Recentes Concluídas (Session Anterior)

### CRÍTICAS ✅
- ✅ Resolver duplicação de métodos no App.xaml.cs
- ✅ Remover dependência conflitante do projeto Simulation
- ✅ Corrigir erros de compilação em BackupSettingsWindow.xaml.cs
- ✅ Corrigir erro de compilação em DatabaseBackupService.cs
- ✅ Resolver conflitos de versão do System.Text.Json

### ALTA PRIORIDADE ✅
- ✅ Testar build completo do projeto WPF
- ✅ Executar todos os testes unitários (92/92 aprovados - ATUALIZADO)
- ✅ Integrar StandardTheme.xaml no App.xaml
- ✅ Integrar ViewModels nos UserControls principais com DI
- ✅ Integrar funcionalidade multi-filial no LoginWindow

### MÉDIA PRIORIDADE ✅
- ✅ Implementar logging estruturado em serviços principais
- ✅ Criar MigrationService para inicializar tabelas
- ✅ Verificar métodos reais nos serviços de API
- ✅ Completar MVVM para todos os UserControls principais (9 controles atualizados)
- ✅ Concluir integração de LocalizationService

### BAIXA PRIORIDADE ✅
- ✅ Criar documentação de arquitetura (ARCHITECTURE.md)

---

## ⏳ Tarefas Pendentes - PRIORIZAÇÃO CRÍTICA (NOVO!)

### 🔴 IMEDIATO (SEMANA 1-2) - CRÍTICO!

#### Segurança - PARCIALMENTE IMPLEMENTADO
- [x] Implementar hash senha → ✅ PasswordHasherService.cs (PBKDF2, 100K iterações)
- [x] Implementar 2FA TOTP → ✅ TwoFactorService.cs + TwoFactorSetupWindow.xaml
- [x] Criar AuditLog system → ✅ AuditLogService.cs + AuditTrailService.cs
- [ ] SQL Injection fixes (12h | R$ 1.8k) - PENDENTE: auditar queries com concatenação
- [x] Criptografia DPAPI → ✅ CryptoService.cs (ProtectedData)
- [ ] Rate limiting (8h | R$ 1.2k) - PENDENTE
- [x] Security logging → ✅ AuditLogService.RegistrarLogin()

#### Dark Mode Fixes - PARCIALMENTE CORRIGIDO
- [x] Corrigir Calendar → ✅ DynamicResource aplicado (05/09)
- [ ] Corrigir ComboBox popup (8h | R$ 1.2k) - VERIFICAR
- [x] Corrigir DataGrid → ✅ Já usava DynamicResource
- [x] Corrigir TextBox → ✅ Inputs.xaml já correto

**Subtotal**: 121h | R$ 18.150

---

### 🟠 SEMANAS 3-4 - ALTA PRIORIDADE

#### Dashboard com KPIs - 48h | R$ 7.200
- [ ] ViewModel com KPIs (12h)
- [ ] Cards de métricas (12h)
- [ ] Gráficos LiveCharts (20h)
- [ ] Filtros por período (4h)

#### Help/Tutorial - 40h | R$ 6.000
- [ ] HelpControl XAML (15h)
- [ ] Conteúdo estruturado (20h)
- [ ] Videos linkados (5h)

**Subtotal**: 88h | R$ 13.200

---

### 🟡 SEMANAS 5-8 - MÉDIA PRIORIDADE

#### RBAC Completo - 62h | R$ 9.300
- [ ] Database schema (12h)
- [ ] RBAC Service (25h)
- [ ] Admin interface (15h)
- [ ] Testes (10h)

#### Performance - 27h | R$ 4.050
- [ ] Caching (12h)
- [ ] Lazy loading (10h)
- [ ] Índices BD (5h)

**Subtotal**: 89h | R$ 13.350

---

### 🟢 SEMANAS 9-12 - INTEGRAÇÕES

#### NF-e & ERP - 80h | R$ 12.000
- [ ] NF-e integration (40h)
- [ ] ERP mapping (25h)
- [ ] Testes (15h)

---

### 🎨 SEMANAS 13-16 - UI/UX & POLISHING

#### Modernização - 80h | R$ 12.000
- [ ] Material Design 3 (35h)
- [ ] Performance tuning (25h)
- [ ] QA & Polish (20h)

---

## 📊 Métricas de Qualidade (EXPANDIDO!)

### Status Atual vs. Target Enterprise

| Métrica | Atual | Target | Gap | Prioridade |
|---------|-------|--------|-----|-----------|
| Build Time | 2:30min | <1:30min | 1:00min | 🟡 |
| Test Coverage | 60% | 85% | +25% | 🟠 |
| **Security Vulns** | **12** | **0** | **-12** | **🔴** |
| Dark Mode Bugs | 4 | 0 | -4 | 🔴 |
| Avg Help Time | N/A | <2min | TBD | 🟠 |
| Dashboard Load | N/A | <1sec | TBD | 🟡 |
| LGPD Compliance | 20% | 100% | +80% | 🔴 |
| Code Quality | B+ | A | +1 level | 🟡 |
| Uptime | 99.5% | 99.99% | +0.49% | 🟢 |
| User Satisfaction | 6.5/10 | 9/10 | +2.5 | 🟠 |

### Segurança & Compliance (NOVO!)

| Aspecto | Atual | Recomendado | Status |
|---------|-------|-------------|--------|
| Encryption at Rest | ❌ | ✅ DPAPI/AES | 🔴 |
| Encryption in Transit | ✅ HTTPS | ✅ TLS 1.3 | 🟡 |
| Authentication | ⚠️ Básica | ✅ 2FA Required | 🔴 |
| Authorization | 🟡 Simples | ✅ RBAC Granular | 🔴 |
| Audit Trail | ❌ | ✅ Completo | 🔴 |
| LGPD Compliance | ❌ | ✅ 100% | 🔴 |
| PEN Testing | ❌ | ✅ Anual | 🔴 |
| DPO (Data Officer) | ❌ | ✅ Designado | 🔴 |

**Segurança Score**: 40/100 (CRÍTICO) → Target: 95/100

---

## 💰 INVESTIMENTO & ROI (NOVO!)

### Cenários de Implementação
#### OPÇÃO 1: Mínimo Viável (8 semanas) - R$ 22.500
```
Escopo:
✅ Segurança básica (80h)
✅ Dark Mode fixes (20h)
✅ Help básico (20h)
✅ Dashboard simples (30h)

Benefício:
+ Segurança operacional
+ UX melhorada
+ Support reduzido
- Sem RBAC
- Sem integrações
- Sem compliance completa

ROI: +150% em 6 meses
```

#### OPÇÃO 2: COMPLETO (16 semanas) ⭐ RECOMENDADO - R$ 87.000
```
Escopo:
✅ Tudo acima +
✅ RBAC completo (60h)
✅ NF-e/Integrações (80h)
✅ UI modernização (80h)
✅ LGPD compliance

Benefício:
+ Enterprise-ready
+ Marketplace competitivo
+ Segurança LGPD
+ RBAC granular
+ Automação 60%

ROI: +3.900% em 12 meses
Preço Novo: R$ 150-200k/licença (vs R$ 50k)
```

#### OPÇÃO 3: PREMIUM (20+ semanas) - R$ 120.000+
```
Escopo:
✅ Tudo acima +
✅ Mobile App MAUI (120h)
✅ Machine Learning (60h)
✅ Marketplace (50h)
✅ DevOps/Kubernetes

Benefício:
+ Eco-sistema completo
+ Múltiplas plataformas
+ Inteligência artificial

ROI: +5.000%+ em 12 meses
Potencial Mercado: R$ 10M+/ano
```

### Análise de Retorno

```
ANTES:
├─ Preço: R$ 50.000/licença
├─ Conversão: 20%
├─ Retenção: 60% (churn 5%/mês)
└─ Potencial: R$ 500k/ano

↓ INVESTIMENTO R$ 87.000 ↓

DEPOIS:
├─ Preço: R$ 175.000/licença (média)
├─ Conversão: 60%
├─ Retenção: 95% (churn 0.5%/mês)
└─ Potencial: R$ 5M+/ano

RESULTADO 12 MESES:
├─ 20 licenças × R$ 175k = R$ 3.5M
├─ Custo operação: R$ 80k
├─ Lucro bruto: R$ 3.42M
├─ ROI: 3,931% 📈
└─ Break-even: Mês 2-3 ✅
```

---

## 🔧 Configurações e Setup (REVISADO)

### Build
- **Framework**: .NET 9.0
- **Build Command**: `dotnet build PrimoAutoEletrica/PrimoAutoEletrica.csproj --configuration Release`
- **Status**: ✅ Compilando sem erros
- **Build Time**: 2:30min (Target: <1:30min)
- **Warnings**: 54 (Target: <10)

### Testes
- **Framework**: xUnit
- **Test Command**: `dotnet test Tests/PrimoAutoEletrica.Tests/PrimoAutoEletrica.Tests.csproj --configuration Release`
- **Status**: ✅ 101/101 aprovados (100%) (atualizado 05/09)
- **Coverage**: ~65% (Target: 85%)
- **Execution Time**: ~2 segundos

### API
- **Framework**: ASP.NET Core 9.0
- **Start Command**: `dotnet run --project PrimoAutoEletrica.Api/PrimoAutoEletrica.Api.csproj`
- **Swagger**: http://localhost:5000/swagger
- **Status**: ✅ Funcional
- **Response Time**: ~500ms (Target: <200ms)

### Database
- **Engine**: SQLite (Dev) / SQL Server (Prod)
- **Migrations**: Via MigrationService (✅ Implementado)
- **Backup**: Automático diário (✅ Implementado)
- **Encryption**: ❌ NÃO (Target: DPAPI)

### Security
- **HTTPS**: ✅ Implementado
- **Authentication**: ⚠️ Básica (Target: 2FA)
- **Authorization**: 🟡 Simples (Target: RBAC Granular)
- **Encryption Rest**: ❌ NÃO (Target: DPAPI)
- **Audit Trail**: ❌ NÃO (Target: Completo)

---

## 📁 Arquivos e Componentes Importantes (EXPANDIDO)

### Documentação Projeto
- ✅ `ARCHITECTURE.md` - Arquitetura completa
- ✅ `README.md` - Documentação inicial
- ✅ `PROJECT_STATUS.md` - **ESTE ARQUIVO (EXPANDIDO)**
- ✅ `CHANGELOG.md` - Histórico versões
- 🟡 `SECURITY.md` - **NOVO: Políticas de segurança** (Falta)
- 🟡 `INSTALLATION.md` - **NOVO: Guia instalação completa** (Falta)
- 🟡 `USER_MANUAL.md` - **NOVO: Manual do usuário** (Falta)
- 🟡 `DEVELOPER_GUIDE.md` - **NOVO: Guia para devs** (Falta)

### Serviços Principais
- ✅ `Services/NavigationService.cs` - Navegação com cache LRU
- ✅ `Services/PermissionService.cs` - Permissões centralizadas
- ✅ `Services/LoggerService.cs` - Logging estruturado
- ✅ `Services/MigrationService.cs` - Migrações BD
- ✅ `Services/NotificationService.cs` - Notificações SMS/WhatsApp
- ✅ `Services/ContabilExportService.cs` - Exportação contábil
- ✅ `Services/DatabaseService.cs` - Gerenciamento banco
- ✅ `Services/OrcamentoDatabaseService.cs` - Orçamentos
- ✅ `Services/EstoqueOperationalService.cs` - Estoque operacional
- ✅ `Services/FinanceiroDatabaseService.cs` - Financeiro
- ✅ `Services/PasswordHasherService.cs` - Hash de senha PBKDF2 (IMPLEMENTADO)
- ✅ `Services/TwoFactorService.cs` - 2FA TOTP com Otp.NET (IMPLEMENTADO 05/09)
- ✅ `Services/AuditLogService.cs` - Auditoria completa 17 campos (IMPLEMENTADO)
- ✅ `Services/AuditTrailService.cs` - Trail histórico com estatísticas (IMPLEMENTADO)
- ✅ `Services/CryptoService.cs` - Criptografia DPAPI (IMPLEMENTADO)
- ✅ `Services/NFeService.cs` - Importação NF-e XML (IMPLEMENTADO)
- 🟡 `Services/RBACService.cs` - **RBAC Granular** (Falta - PermissionService é parcial)
- 🟡 `Services/DashboardService.cs` - **Dashboard com gráficos** (Falta - ViewModel existe)

### UserControls & ViewModels
- ✅ `UserControls/EstoqueControl.xaml.cs` + ViewModel
- ✅ `UserControls/FuncionariosControl.xaml.cs` + ViewModel
- ✅ `UserControls/ClientesControl.xaml.cs`
- ✅ `UserControls/OrcamentosControl.xaml.cs`
- ✅ `UserControls/FinanceiroControl.xaml.cs`
- ✅ `UserControls/DashboardControl.xaml.cs`
- 🟡 `UserControls/HelpControl.xaml` - **NOVO: Help integrado** (Falta)
- 🟡 `UserControls/SecuritySettings.xaml` - **NOVO: Configurações seg.** (Falta)
- 🟡 `UserControls/RBACManagement.xaml` - **NOVO: Admin RBAC** (Falta)

### Themes & Styles
- ✅ `Themes/StandardTheme.xaml` - Theme padronizado
- ✅ `Themes/Colors.xaml` - Palheta de cores
- 🟡 `Themes/Components/Calendar.xaml` - **BUG: Corrigir dark mode** (Falta fix)
- 🟡 `Themes/Components/ComboBox.xaml` - **BUG: Corrigir dark mode** (Falta fix)
- 🟡 `Themes/Components/DataGrid.xaml` - **BUG: Corrigir dark mode** (Falta fix)
- 🟡 `Themes/Components/TextBox.xaml` - **BUG: Corrigir dark mode** (Falta fix)

### API REST
- ✅ `Api/Program.cs` - API com endpoints
- ✅ `Api/Controllers/OrcamentosController.cs`
- ✅ `Api/Controllers/EstoqueController.cs`
- ✅ `Api/Controllers/FinanceiroController.cs`
- 🟡 `Api/Controllers/SecurityController.cs` - **NOVO** (Falta)
- 🟡 `Api/Middleware/JwtAuthMiddleware.cs` - **NOVO** (Falta)

### Testes
- ✅ `Tests/PrimoAutoEletrica.Tests/` - 74+ testes unitários
- ✅ `Tests/PrimoAutoEletrica.Tests/PermissionServiceTests.cs`
- ✅ `Tests/PrimoAutoEletrica.Tests/NavigationServiceTests.cs`
- ✅ `Tests/PrimoAutoEletrica.Tests/OrcamentoDatabaseServiceTests.cs`
- 🟡 `Tests/SecurityServiceTests.cs` - **NOVO** (Falta)
- 🟡 `Tests/AuditServiceTests.cs` - **NOVO** (Falta)
- 🟡 `Tests/RBACServiceTests.cs` - **NOVO** (Falta)

### Models (Atualizar para LGPD)
- ✅ `Models/Cliente.cs` - Adicionar soft delete
- ✅ `Models/Veiculo.cs` - Adicionar soft delete
- ✅ `Models/Orcamento.cs` - Adicionar soft delete
- ✅ `Models/Usuario.cs` - Adicionar campos seg.
- 🟡 `Models/AuditLog.cs` - **NOVO** (Falta)
- 🟡 `Models/SecurityEvent.cs` - **NOVO** (Falta)
- 🟡 `Models/PermissionPolicy.cs` - **NOVO** (Falta)

---

## 🚀 Próximos Passos Imediatos (ATUALIZADO)

### ESTA SEMANA (Crítico!)
- [ ] Revisar relatórios de análise completa
- [ ] Reunião executiva com stakeholders
- [ ] Decisão: Qual opção de investimento?
- [ ] Aprovação de orçamento R$ 87.000 (mínimo)
- [ ] Contratação de especialista em segurança (consultoria 20h)

### SEMANA 1 (Segurança + Dark Mode)
```
Objetivos:
1. Implementar hash de senha
2. Implementar 2FA
3. Corrigir 4 bugs dark mode
4. Criar AuditLog
5. Implementar rate limiting

Resultado: +50 pontos de segurança
```

### SEMANA 2 (Continuação Segurança)
```
Objetivos:
1. Implementar criptografia DPAPI
2. SQL Injection fixes
3. Security logging
4. CORS restrictivo
5. Input validation

Resultado: LGPD compliance 60%
```

### SEMANA 3-4 (Dashboard + Help)
```
Objetivos:
1. Dashboard com KPIs
2. Gráficos Live
3. Help integrado (F1)
4. Tutorial estruturado

Resultado: -80% support tickets
```

### SEMANA 5-12 (RBAC + Integrações)
```
Objetivos:
1. RBAC granular
2. NF-e integration
3. Performance optimization
4. Testes completos

Resultado: Enterprise-ready
```

### SEMANA 13-16 (UI/UX + Polish)
```
Objetivos:
1. Modernização UI
2. Performance final
3. QA completo
4. Release v2.0

Resultado: Produto pronto para venda
```

---

## 📈 Sucesso Esperado (v2.0 Enterprise)

### Transformação Prevista

```
ANTES (v1.2.1)          DEPOIS (v2.0)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Status: 70/100          Status: 95+/100 ✅
Segurança: 40/100   →   Segurança: 95/100 ✅
Dark Mode: 40/100   →   Dark Mode: 100/100 ✅
UX: 60/100          →   UX: 90/100 ✅
Docs: 50/100        →   Docs: 95/100 ✅
LGPD: 20/100        →   LGPD: 100/100 ✅

Mercado: Nichado         Mercado: Escalável ✅
Preço: R$ 50k           Preço: R$ 150-200k ✅
Potencial: R$ 500k/ano  Potencial: R$ 5M+/ano ✅

ROI: +3.900% em 12 meses
```

---

## 📞 Contato & Referência

**Análise Realizada**: 04/09/2026  
**Analista**: Especialista em Arquitetura Enterprise  
**Documentos de Referência**:
- `RELATORIO_ANALISE_COMPLETA_PRIMO.md` - Análise detalhada
- `GUIA_IMPLEMENTACAO_PRATICA.md` - Código + implementação
- `SUMARIO_EXECUTIVO_E_ROADMAP.md` - Visão executiva
- `CHECKLIST_ACOES_RAPIDAS.md` - Ações prioritárias

---

## ⚠️ Importante: PRÓXIMA AÇÃO

**NÃO PROCEEDER COM VENDAS SEM:**
1. ✅ Implementar segurança (2FA, hash, auditoria)
2. ✅ Corrigir dark mode bugs
3. ✅ Compliance LGPD mínimo
4. ✅ Dashboard com KPIs
5. ✅ Help/Tutorial integrado

**Risco Legal**: Multas LGPD até R$ 50M  
**Risco Comercial**: Churn >50% sem segurança  
**Timeline Recomendado**: 16 semanas com R$ 87.000

---

**Status Final**: ⚠️ PRONTO PARA TRANSFORMAÇÃO  
**Próxima Revisão**: Após implementação Fase 1 (Semana 2)  
**Aprovação Requerida**: Executiva
**DOCUMENTO CRÍTICO - NÃO COMPARTILHAR COM PÚBLICO**

---

## PRIMOX Icon System and Premium Sidebar (2026-09-09)

| Campo | Valor |
|-------|--------|
| Tecnologia | PathGeometry 24x24 + Path Stroke (herda Foreground do Button) |
| Familia visual | Outline tecnico enterprise (estilo Lucide/Fluent; geometrias originais) |
| Licenca | Original PRIMOX — sem dependencia externa / sem download runtime |
| Arquivos | `Themes/Icons.xaml`, `Themes/Sidebar.xaml`, `MainWindow.xaml(.cs)` |
| Estados | Normal / Hover (+2px slide 140ms) / Selected (Brand + barra 3px) / Focus / Compact 20px / Expanded 18px |
| Sidebar | Sempre Navy; icones recoloriveis; Ajuda=Geo.Help; Fiscal=Geo.Fiscal |
| Tag v1.0.0 | Intacta |

### Script 5 — Premium Sidebar Hover Gold (2026-09-09)

| Campo | Valor |
|-------|--------|
| Token | `SidebarHoverGoldBrush` `#C9A227` (Light + Dark) |
| Escopo | SOMENTE estilos `SidebarItem` / Path da sidebar |
| Normal | `SidebarItemForegroundBrush` (cinza tecnico) |
| Hover | `SidebarHoverGoldBrush` (texto + icone juntos) |
| Selected | `BrandBrush` #F97316 (nao fica dourado) |
| Animacao | slide 2px ~150ms; sem glow/neon/pulse |
| Nao aplicado | botoes de pagina, cards, grids, headers, Help, Command Center |


