# PRIMOX INSTALLATION E2E HARDENING — Fase 15C

**Produto:** PRIMOX Workshop  
**Versão:** 1.0.0  
**Tag:** `v1.0.0` → `a4ad6fe` (intacta)  
**HEAD inicial:** `aade747`  
**Branch:** `main`  
**Data:** 08/09/2026  

**Decisão:** **GO**

---

## 1. Causa raiz do smoke instalado (15B)

| Hipótese | Resultado |
|----------|-----------|
| A) App instalado não inicia | **Descartada** — startup sem args: Responding, ~131 MB |
| B) `--smoke-test` incompatível com EXE publicado | **Parcial** — smoke funciona após correção de isolamento |
| C) Smoke espera condição inexistente | **Confirmada** — path `--app-data` sem marcadores era rejeitado |
| D) Banco em outro local | Não — DB não era criado |
| E) Permissão | Não |
| F) Dependência BaseDirectory | Logs sim (corrigido) |
| G) Diferença VS vs publish | Isolamento + deadlock no handler |

**Mecanismo:** `DatabaseService` / `GarantirBancoIsoladoDoSmoke` exigiam substrings (`AutomatedTests`, etc.). `--app-data=%LOCALAPPDATA%\PRIMOX-Workshop-DataTest-*` falhava → exceção no init → `Logger`/`Audit` reentravam em `EnsureInfrastructureInitialized` sob lock → **hang** → kill → `Exit=-1` / sem DB.

**Classificação:** problema de **automação/isolamento**, não de falha do produto em uso normal.

---

## 2. Correções realizadas

1. Isolamento automatizado: rejeitar **somente** AppData de produção (`%LOCALAPPDATA%\PrimoAutoEletrica`); aceitar `--app-data` explícito fora dessa árvore.  
2. `App.IsIsolatedAutomatedAppData` — gates de Estoque / NFe / restore / QA alinhados.  
3. Logs padrão → `%LOCALAPPDATA%\PrimoAutoEletrica\Logs` (não `BaseDirectory` / Program Files).  
4. DI: `LoggerService` e `DatabaseBackupService` usam `RuntimeLogDirectory` / `RuntimeBackupDirectory`.  
5. Handlers de exceção / `OnStartup`: sem reentrada em getters de infraestrutura (evita deadlock).  
6. Script `Scripts/Test-InstalledPackageE2E.ps1` — suíte Packaging E2E.

---

## 3. Evidências Packaging E2E

Relatório: `TestResults/PackagingE2E/packaging-e2e-20260908-100141.md`

| Teste | Status |
|-------|--------|
| Setup exists | PASS |
| Setup SHA256 | `67F4DF6AA9F03F38ABC71A63219413B611A3B8D5C8F922E513C2824575C5E67B` |
| Install silencioso | PASS |
| Version 1.0.0 | PASS |
| Startup (sem smoke) | PASS (~12 s, Responding) |
| Installed smoke QaEngine | **PASS** Exit=0 (~372 s) |
| Database creation | PASS (app-data isolado) |
| Integrity / FK | ok / 0 |
| Fresh migrations | **27** |
| Uninstall | PASS |
| Data retention | PASS (marker prod + DB teste) |
| Reinstall + dados | PASS |

Ambiente: install em `%LOCALAPPDATA%\PRIMOX-Workshop-InstallTest-15C` (isolado; não Sandbox).  
Windows Sandbox: **NOT TESTABLE** (requer elevação / não habilitado nesta sessão).

---

## 4. Login / CRUD

| Item | Status | Motivo |
|------|--------|--------|
| Login UI interativo | **NOT TESTABLE** | Sem operador/Sandbox interativa |
| CRUD pós-install | **PASS** (automatizado) | QaEngine **37/37** no EXE instalado |
| Clientes / Veículos / OS / Estoque / Financeiro / Agenda / PDV / Funcionários | **PASS** | Cobertos pelo QaEngine instalado |
| Restart após smoke | **PASS** | Processo encerra Exit=0; reinstall + reopen DB ok |

---

## 5. Backup / Restore

| Item | Status |
|------|--------|
| Backup automático no startup isolado | PASS (arquivo + hash no log) |
| Restore E2E arquivo (15B) | PASS |
| Restore UI no pacote instalado | coberto no QaEngine Configuracoes (suite instalada PASS) |

---

## 6. Migrations

| Ambiente | Count | Integrity | Inventário |
|----------|------:|-----------|------------|
| Fresh install (DataTest-15C) | 27 | ok | QaEngine PASS |
| Cópia histórico LocalAppData | **32** | ok | InventarioFuncional PASS |

Divergência 32 vs 27: **KNOWN LIMITATION histórica compatível** (sem alteração de schema nesta fase).

---

## 7. Dual install / Legacy

| Instalação | Classificação | Versão | Ação |
|------------|---------------|--------|------|
| `Program Files\PRIMOX\Workshop` | OFFICIAL (ISS) | — | Não instalado em PF nesta sessão (teste `/DIR` isolado) |
| `%LOCALAPPDATA%\PrimoAutoEletrica\App` | DEVELOPMENT | 1.0.0 | Manter como tool dev |
| `Program Files\Primo Auto Elétrica` | **LEGACY** | **0.0.0.0** | **Não removido** automaticamente; banco é o AppData compartilhado — backup antes de uninstall manual |

Atalhos legados ainda podem apontar para PF `0.0.0.0` — risco de confusão do usuário (**KNOWN LIMITATION**).

---

## 8. Logs

| Antes | Depois |
|-------|--------|
| `AppContext.BaseDirectory\Logs` | `%LOCALAPPDATA%\PrimoAutoEletrica\Logs` (modo normal) |
| Smoke | `{app-data}\Logs` |

Relatórios smoke-tests em `BaseDirectory\Logs\smoke-tests` permanecem (artefato de QA).

---

## 9. QA regressão

| Suite | Resultado |
|-------|-----------|
| QaEngine (Debug) | **37/37 PASS** (`2026-09-08_10-10-55`) |
| Deep QA | **6/6 PASS** (`2026-09-08_10-15-22`) |
| Long Run | PASS (nas suites) |
| Packaging E2E | PASS |
| Regressões | **0** |

---

## 10. Conteúdo do Setup

- Publish self-contained win-x64; **sem** `*.db` / credenciais no publish inspecionado.  
- SIGNING: **NOT CONFIGURED** (SmartScreen pode avisar).  
- Upgrade A→B: **NOT TESTABLE** (só Setup 1.0.0); reinstall same-version **PASS**.

---

## 11. Limitações / NOT TESTABLE / BLOCKED

**NOT TESTABLE:** Login interativo; Windows Sandbox; upgrade versão A→B distinta; install elevado em Program Files real nesta sessão.  

**KNOWN LIMITATION:** Legacy PF 0.0.0.0 + atalhos; schema 32 vs 27; AssemblyCompany legado.  

**BLOCKED:** nenhum residual do smoke instalado após correção.
