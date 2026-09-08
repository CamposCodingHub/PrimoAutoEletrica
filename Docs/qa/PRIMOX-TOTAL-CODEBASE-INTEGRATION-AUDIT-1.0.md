# PRIMOX TOTAL CODEBASE + INTEGRATION AUDIT 1.0

**Data:** 2026-09-08  
**HEAD (início):** `d0e69b7eed220d46af25ea7a20111e4a519eb039`  
**Tag `v1.0.0`:** `a4ad6fe810a96914275891d294e9e585c7867e9e` — **INTACTA**  
**Branch:** `main` (ahead origin)  
**Limpeza executada:** **nenhuma remoção de código** (matriz: candidatos classificados; WIP preservado)  
**Decisão final:** **E) COMMERCIAL READY WITH LIMITATIONS** (= Product Truth VERIFIED WITH LIMITATIONS + packaging desktop 1.0.0)

Hierarquia de verdade: **runtime > código > banco > testes > docs**.

---

## 1. Executive Summary

O PRIMOX Workshop **1.0.0** é um produto desktop WPF (`net6.0-windows`) real para oficina (CRUD, OS, PDV, estoque, financeiro, relatórios, import NF-e, backup, RBAC WPF), com infraestrutura de QA forte (QaEngine / Exhaustive / DeepQa / Long Run).

**Não é:** emissão NF-e/SEFAZ, WhatsApp Cloud, Twilio SMS real, SMTP, gateway PIX, sync remoto, multi-filial persistida, SaaS, API autenticada completa.

Placeholders perigosos para venda: `NotificationService` (Delay + “Enviado”), `FilialService` (lista hardcoded), `NFeEmissaoService.cs` (0 bytes), packages JWT na API sem wiring.

**Remoções:** 0 nesta auditoria. Shells 0-byte e orphans listados como `REMOVE — SAFE*` / `DEPRECATE` **pendentes de GO humano**.

---

## 2. Repository Inventory

| Métrica | Valor (excl. `bin`/`obj`/`.git`/`node_modules`/`AutomatedTests`) |
|---------|------------------------------------------------------------------|
| Arquivos | ~1907 (inclui artefatos nativos `.dll`/`.so` em Tools/runtimes — ver nota) |
| Diretórios | ~355 |
| `.cs` | 422 |
| `.xaml` | 104 |
| `.md` | 92 |
| `.ps1` | 43 |
| `.csproj` | 13 |
| `.yml` | 7 |
| Migrations `ApplyMigration` | **27** (código) |

**Nota:** contagem bruta inclui runtimes nativos fora de `bin/`. Fonte de inventário comercial = solution + `PrimoAutoEletrica/` source + `Docs/` + `Scripts/` + `Installer/` + `.github/`.

---

## 3. Project Inventory

| Projeto | TFM | Papel | Status |
|---------|-----|-------|--------|
| `PrimoAutoEletrica` (WPF) | **net6.0-windows** | Produto | CURRENT |
| `PrimoAutoEletrica.Api` (root) | **net9.0-windows** | API minimal | REAL+PARCIAL |
| `PrimoAutoEletrica.Tests` / `Tests/...` | (test) | Unit/integration | KEEP — QA |
| `PrimoAutoEletrica.UiTests` | (test) | UI tests | KEEP — QA |
| `PrimoAutoEletrica.Simulation` (+ nested) | — | Simulação | KEEP — QA |
| `Tools/DbConfigurator` | — | Dev DB | KEEP |
| `Tools/LocalSyncSimulator` | — | LAN sync sim | KEEP — QA |
| `PrimoAutoEletrica.Maui` | empty/scaffold | — | UNKNOWN |
| Nested `PrimoAutoEletrica/Api`, `Simulation`, `Tests` | — | Duplicatas/legado | UNKNOWN / DEPRECATE candidato |
| `PrimoAutoEletrica.sln` | — | Solution oficial | CURRENT |

---

## 4. File Inventory (por finalidade)

| Finalidade | Exemplos | Decisão |
|------------|----------|---------|
| Produto WPF | Views, UserControls, Services, ViewModels | KEEP |
| Fiscal import | `NFeService`, ImportarNota | KEEP |
| Fiscal emit shell | `NFeEmissaoService.cs` 0b | KEEP — FUTURE |
| QA engine | `UiSmokeTestService*` | KEEP — QA |
| Scripts deploy WIP | `Deploy-ToInstalledApp.ps1`, `Atualizar-PrimoAuto.bat` | KEEP — WIP |
| Installer | `Installer/*.iss` | KEEP |
| CI | `.github/workflows/*` (5 root) | KEEP |
| Docs QA/arch | `Docs/qa`, `Docs/architecture` | KEEP |
| Empty services | 6× 0-byte Services | REMOVE — SAFE* (não executado) |

---

## 5. Class Inventory (síntese)

Classificação amostral forense (não é dump de todas as 400+ classes):

| Classe | Classificação |
|--------|---------------|
| Domain services (Cliente/OS/PDV/Estoque/Financeiro/…) | REALMENTE UTILIZADA |
| `NFeService` | REAL + TESTADO |
| `NFeEmissaoService` | PLACEHOLDER / SCAFFOLD (arquivo vazio) |
| `NotificationService` | PLACEHOLDER (fake success) |
| `FilialService` | SCAFFOLD |
| `LicenseService` | SCAFFOLD / ORPHAN UI |
| `ExternalBackupService` | ORPHAN CANDIDATE |
| `DatabaseBackupService` | REAL + TESTADO |
| `FuncionariosViewModel` / `RelatoriosModernoViewModel` / `EstoqueViewModel` / `PrinterManagementViewModel` | ORPHAN UI (DI+tests; UI code-behind) — KEEP LEGACY |
| Empty service shells | ORPHAN / EMPTY |
| AccessibilityChromeHealer | REAL + TESTADO (P15E-015) |

Métodos: inventário completo método-a-método não foi gerado como CSV nesta entrega; candidatos mortos listados na Cleanup Matrix. **UNKNOWN preservados.**

---

## 6. Service Inventory

| Serviço | Status |
|---------|--------|
| Core CRUD / OS / PDV / Financeiro / Relatórios | REAL + TESTADO |
| `NFeService` | REAL + TESTADO (import) |
| `NFeEmissaoService` | NOT IMPLEMENTED |
| `NotificationService` | PLACEHOLDER |
| `FilialService` | SCAFFOLD |
| `LocalSyncService` / UDP tools | PARTIAL (LAN ≠ cloud sync) |
| `LicenseService` | SCAFFOLD |
| `DatabaseBackupService` | REAL + TESTADO |
| `ExternalBackupService` | ORPHAN |
| `PermissionService` / auth WPF | REAL |
| Empty: CpfFieldEncryption, DemandForecast, IntegracoesConfig, LgpdPortability, MarketplaceFornecedor, Telemetry | EMPTY SHELL |

---

## 7. View / XAML Inventory

- ~104 XAML: Windows, UserControls, Themes/ResourceDictionaries.
- Navegação principal via MainWindow + UserControls (code-behind dominante em vários módulos).
- Calendar / DatePicker: **KEEP** (não tratar CalendarItem como orphan).
- HelpControl + HelpTopicsCatalog: **WIP** (modificados/untracked — não commit nesta audit).
- Orphans de ViewModel não implicam XAML órfão correspondente (UI usa code-behind).

---

## 8. Test Inventory

| Suíte | Natureza | Classificação |
|-------|----------|---------------|
| QaEngine | Smoke orquestrado | REAL runtime — KEEP |
| CompleteUi | Focus/visual | REAL — KEEP |
| Exhaustive UI | Botões Light/Dark × 4 res | REAL — KEEP |
| DeepQa | Incl. a11y | REAL — KEEP |
| Long Run | 5 ciclos | REAL — KEEP |
| `PrimoAutoEletrica.Tests` | Unit | KEEP |
| API tests | Parcial / frouxos (OK\|\|NotFound) | PARCIAL — gap |
| Simulation | Simuladores | KEEP — QA |

---

## 9. Simulation / QA Inventory

**PRESERVAR:** QaEngine, CompleteUi, DeepQa, Exhaustive, Long Run, `Run-UiSmoke.ps1`, bancos isolados AutomatedTests, LocalSyncSimulator, Packaging E2E scripts.

**Não remover** por não entrarem no EXE comercial.

---

## 10. Script Inventory

| Script | Decisão |
|--------|---------|
| `Scripts/Run-UiSmoke.ps1` | REQUIRED / QA |
| Deploy WIP bat/ps1 | KEEP — WIP |
| `Run-Keycloak.ps1` (vazio) | REMOVE — SAFE* |
| Installer / release scripts | CURRENT |
| Demais `.ps1` (~43) | Mapear por doc/CI; sem purge |

---

## 11. Installer Inventory

- Pipeline oficial: Inno Setup `Installer` + scripts Build-PrimoX / release workflow.
- Product: PRIMOX Workshop 1.0.0 · AppId comercial distinto de PackagingE2E.
- **Uma** pipeline oficial recomendada (já consolidada em 15D); nested/legado = UNKNOWN até prova.

---

## 12. CI/CD Inventory

Root `.github/workflows`: `ci.yml`, `release.yml`, `code-quality.yml`, `integration-tests.yml`, `performance-security.yml`.  
Nested `PrimoAutoEletrica/.github/workflows/ci.yml`: possível LEGACY/DUPLICATE — **UNKNOWN**, não apagado.

---

## 13. Database Inventory

- Runtime produto: **SQLite** (AppData / AutomatedTests isolados).
- Packages: Microsoft.Data.Sqlite (+ SqlClient presente — uso secundário/schema SQL).
- Backup/restore: REAL.
- **Não** migrar produção nesta auditoria.

---

## 14. Migration Analysis

| Fonte | Contagem | Classificação |
|-------|----------|---------------|
| Código `ApplyMigration` | **27** (20260521… → 202609060001) | CURRENT |
| AppData histórico ~32 SchemaVersion (auditorias anteriores) | HISTORICAL | NÃO REMOVER |
| Divergência | Esperada se DBs antigos acumularam IDs | Documentar; nunca apagar |

---

## 15. Documentation Audit

| Afirmação comum | Verdade |
|-----------------|---------|
| Import NF-e funciona | VERDADEIRA + TESTADA |
| Emissão NF-e pronta | INCORRETA / NÃO IMPLEMENTADA |
| Multi-filial DONE | INCORRETA (SCAFFOLD) |
| Sync offline real | INCORRETA |
| Twilio integrado | INCORRETA (PLACEHOLDER) |
| WhatsApp wa.me | VERDADEIRA |
| API enterprise JWT | PARCIAL / SCAFFOLD |
| Exhaustive 100% botões produto | PARCIAL (100% testáveis do motor ≠ 100% produto) |
| Score 98/100 | OBSOLETA / INCORRETA como métrica oficial |

---

## 16–18. Dead / Orphan / Duplicate Candidates

Ver `Docs/qa/PRIMOX-CODEBASE-CLEANUP-MATRIX.md`.

Destaques: empty services; ExternalBackupService; VMs DI órfãs; nested csproj/API/Maui; Cryptography.Xml sem uso; Notification fake.

---

## 19. Safe Removals

**Executados:** nenhum.  
**Candidatos:** 6 services 0-byte + `Run-Keycloak.ps1` vazio — só após GO + regressão completa.

---

## 20. Preserved Legacy

VMs órfãs, nested projects UNKNOWN, Calendar themes, Filial UI scaffold (até decisão).

---

## 21. Preserved QA Infrastructure

Toda a família UiSmoke / Exhaustive / QaEngine / DeepQa / Long Run / PackagingE2E / LocalSyncSimulator / AutomatedTests isolation.

---

## 22. WhatsApp

| Mecanismo | Estado |
|-----------|--------|
| `https://wa.me/...` deep link | REAL (OS/Agenda/Clientes/OficinaProfissionalService) |
| Geração de texto URL | REAL |
| WhatsApp Cloud / Meta Graph | NÃO IMPLEMENTADO |
| Templates / webhook / token | NÃO IMPLEMENTADO |
| `NotificationService.EnviarWhatsAppAsync` | PLACEHOLDER |

Arquitetura futura: `IWhatsAppProvider` → `WaMeWhatsAppProvider` / `WhatsAppCloudProvider` — **não implementado**.

---

## 23. SMS / Twilio

PLACEHOLDER / NOT IMPLEMENTED — Delay + log “sucesso”; sem package Twilio; sem SID/token real no código auditado. **Não vender.**

---

## 24. Email

- `mailto:` REAL (abertura cliente OS).  
- SMTP / MailKit / SendGrid: NÃO IMPLEMENTADO.

---

## 25–26. Payments / PIX

- PIX como **forma de pagamento interna** (PDV/Financeiro): REAL + TESTADO.  
- Gateway / QR dinâmico / webhook / conciliação: NÃO IMPLEMENTADO.

---

## 27. NF-e

| Capacidade | Estado |
|------------|--------|
| Importação XML | REAL + TESTADO |
| Rascunho emissão / XML geração fiscal saída | NÃO IMPLEMENTADO |
| Assinatura / transmissão / autorização / cancel / inutilização / DANFE / homolog / produção | NÃO IMPLEMENTADO |
| `NFeEmissaoService.cs` | 0 bytes |

**Import ≠ emissão.**

---

## 28–29. NFC-e / NFS-e

NÃO IMPLEMENTADO. Necessidade futura tipicamente: NFC-e balcão B2C; NFS-e mão de obra (municipal). Fora do 1.0.0.

---

## 30. Certificates

Sem uso real de X509/PFX/A1/A3 para fiscal. Package `System.Security.Cryptography.Xml` presente **sem SignedXml usage** → ORPHAN package. Senhas/tokens: **não impressos** neste relatório.

---

## 31. API

Endpoints reais mínimos: `/api/health`, orçamentos, estoque produtos, financeiro resumo/orçamento.  
JWT packages presentes; **AddAuthentication/RequireAuthorization não evidenciado** como wired.  
Testes que aceitam NotFound ≠ API testada. Classificação: **REAL + PARCIAL**.

---

## 32. RBAC

- WPF `PermissionService` / perfis: REAL.  
- Policies API (`AdminOnly`, etc.): NÃO IMPLEMENTADO / gap.  
- Não implementar nesta etapa.

---

## 33–35. Multi-filial / Offline / Sync

- `FilialService`: SCAFFOLD (SP/RJ hardcoded, Guid novos).  
- Offline desktop SQLite: natural.  
- Sync remoto / outbox ack / `ProcessarFilaOfflineAsync`: **NÃO IMPLEMENTADO** (símbolo ausente / sem transporte).  
- Sync LAN UDP: PARCIAL — **não** chamar sincronização multi-loja.

Ver `PRIMOX-SYNC-ARCHITECTURE.md`.

---

## 36. SaaS

NÃO IMPLEMENTADO. Sem TenantId isolation / billing / subscription real. LicenseService local = SCAFFOLD. Cloud = architecture only.

---

## 37. Security (síntese)

| Tema | Achado |
|------|--------|
| Auth desktop | REAL (senha; 2FA serviço PARCIAL no login) |
| Secrets em docs/logs | Risco operacional se Notification fake + PII em logs |
| API | Sem auth efetiva |
| SQL | Dapper predominante; sem purge |
| Backups | REAL; não apagar AppData |
| Certificados | N/A fiscal |

---

## 38. Performance

Padrões load-all / ToList em módulos legados possíveis; AppCache parcial. **Somente mapa** — sem otimização nesta audit.

---

## 39–42. Architectures

Documentos:

- `Docs/architecture/PRIMOX-INTEGRATION-ARCHITECTURE.md` (**novo**)  
- `Docs/architecture/PRIMOX-FISCAL-ARCHITECTURE.md` (atualizado)  
- `Docs/architecture/PRIMOX-SYNC-ARCHITECTURE.md` (atualizado)  
- `Docs/architecture/PRIMOX-CLOUD-ARCHITECTURE.md` (atualizado)  

---

## 43. Commercial Readiness

Desktop oficina **pode ser vendido** com limitações honestas (sem NF-e emissão, sem Cloud WA, sem multi-filial real, sem SaaS).  
Packaging 15D + Exhaustive PASS suportam **COMMERCIAL READY WITH LIMITATIONS**.

---

## 44. Technical Debt

Placeholders de integração; VMs órfãs; nested projects; migrations histórico vs código; API frouxa; empty shells; docs históricas contraditórias.

---

## 45. Risks

1. Vender emissão fiscal / Twilio / sync / multi-filial.  
2. NotificationService registrar “Enviado”.  
3. API aberta se exposta.  
4. Confundir Exhaustive 100% executable com produto 100%.

---

## 46. Product Decisions Required

1. NF-e: Provider (B) vs SEFAZ (A).  
2. Comunicar: manter wa.me vs Cloud/SMS.  
3. Prioridade: API auth vs fiscal vs filial.  
4. Remover shells 0-byte?  
5. Horizonte SaaS.

---

## 47. Recommended Roadmap (sem implementar)

P0 futuro compliance: fiscal provider + cert.  
P1: desligar fake Notification; filial persistida; API JWT.  
P2: WhatsApp Cloud / SMTP.  
P3: PIX gateway / SaaS.  
P4: limpeza orphans após GO.

---

## 48. Test Evidence

**Nenhuma remoção → regressão completa QA não reexecutada nesta audit**; evidência recente (P15E-015, 2026-09-08):

| Teste | Resultado | Evidência |
|-------|-----------|-----------|
| Build | PASS 0 errors / 64 warnings | `dotnet build` net6.0-windows Debug (esta audit) |
| QaEngine | 42/42 | Accessibility closure |
| CompleteUi | PASS | … |
| Exhaustive | 1909 PASS / 0 FAIL / 0 BLOCKED / a11y 0 | `exhaustive-summary-latest.md` 18:11:31 |
| DeepQa | 6/6 | … |
| Long Run | 5 ciclos PASS | … |
| Light/Dark × 4 res | PASS | Exhaustive 8/8 |

---

## 49. Git Evidence

- Tag `v1.0.0` → `a4ad6fe` intacta.  
- WIP Help + Deploy scripts **não** commitados nesta audit.  
- Entregáveis docs sob `Docs/qa` e `Docs/architecture`.  
- Sem reset/rebase/force.

---

## 50. Final Decision

### **E) COMMERCIAL READY WITH LIMITATIONS**

Equivalente comercial a Product Truth VERIFIED WITH LIMITATIONS: desktop operacional testado; integrações avançadas / fiscal emissão / SaaS / sync remoto **fora** do que pode ser prometido.

---

## O QUE O PRIMOX JÁ É

- Sistema desktop de oficina (clientes, veículos, OS, orçamentos, PDV, estoque, financeiro, relatórios).  
- Importação de NF-e de compra.  
- WhatsApp via deep link `wa.me`.  
- PIX como forma de pagamento **interna**.  
- Backup/restore SQLite.  
- RBAC no desktop.  
- Instalador comercial 1.0.0 + QA regressivo forte.

## O QUE O PRIMOX AINDA NÃO É

- Emissor NF-e/NFC-e/NFS-e homologado.  
- WhatsApp Business Cloud / SMS Twilio real.  
- SMTP corporativo.  
- Gateway de pagamento / PIX dinâmico.  
- Multi-filial persistida.  
- Sincronização remota multi-loja.  
- SaaS multi-tenant.  
- API autenticada completa.

## O QUE PODE SER VENDIDO HOJE

- Licença desktop oficina com as capacidades REAIS acima.  
- Import NF-e + operação local SQLite.  
- Comunicação manual via WhatsApp/email do SO.

## O QUE NÃO PODE SER PROMETIDO AO CLIENTE

- “Emite NF-e”.  
- “Integração Twilio/WhatsApp API”.  
- “Multi-filial / sync na nuvem”.  
- “SaaS / assinatura cloud”.  
- “API enterprise pronta”.  
- “100% do produto” só porque Exhaustive passou 1909 botões.

## O QUE DEVE SER FEITO PRIMEIRO

1. Decisão A/B fiscal.  
2. Neutralizar `NotificationService` fake.  
3. Roadmap: API auth **ou** filial **ou** fiscal (uma prioridade).  
4. GO humano para limpeza 0-byte.

## O QUE PODE ESPERAR

- NFC-e/NFS-e, PIX gateway, SaaS, Cloud WhatsApp.

## O QUE PODE SER REMOVIDO

- Somente após GO: shells 0-byte listados + script Keycloak vazio. **Nada removido agora.**

## O QUE DEVE SER PRESERVADO

- QA engines, smoke scripts, installer oficial, WIP deploy, tag v1.0.0, import NF-e, backup, Help WIP.

---

## Checklist final (§75)

- [x] Árvore percorrida (inventário)  
- [x] Projetos inventariados  
- [x] C# / XAML / Services / scripts / testes / workflows / installer / DB / migrations amostrados com evidência  
- [x] Integrações / fiscal / API / RBAC / filial / sync / SaaS / segurança / performance  
- [x] Arquitetura futura documentada  
- [x] QA / WIP / tag preservados  
- [x] Build executado nesta audit  
- [x] QaEngine/Exhaustive/etc. citados da evidência recente (sem remoção → sem re-run obrigatório completo)  
- [x] Nenhuma feature futura implementada  
- [x] Documentação atualizada  

**PARAR.** Aguardar decisão do proprietário.
