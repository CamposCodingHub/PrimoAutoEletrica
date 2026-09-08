# PRIMOX LEGACY INSTALL CLEANUP & COMMERCIAL READINESS — Fase 15D

**Produto:** PRIMOX Workshop  
**Versão:** 1.0.0  
**Tag:** `v1.0.0` → `a4ad6fe` (**intacta**; não movida)  
**Branch:** `main`  
**HEAD inicial (início 15D):** `ad8d76b`  
**Data:** 08/09/2026  

**Decisão:** **GO**  
**Commercial readiness:** **READY** (com limitações conhecidas)

---

## 1. Escopo

Limpeza controlada de instalação legada e preparação comercial.  
**Não** inclui: novas features, redesign, schema/migrations, licença, auto-update, website, assinatura digital, mover tag.

Ordem obrigatória seguida: **IDENTIFICAR → BACKUP → VALIDAR → DECIDIR → REMOVER → TESTAR**.

---

## 2. Git (preservado)

| Item | Estado |
|------|--------|
| Branch | `main` |
| Tag `v1.0.0` | `a4ad6fe` |
| WIP preservado | `HelpControl.xaml(.cs)`, `Scripts/Deploy-ToInstalledApp.ps1`, `Scripts/Atualizar-PrimoAuto.bat` |
| Proibido | `git clean -fd`, `git reset --hard` |

---

## 3. Inventário (antes da limpeza)

Fonte: `TestResults/PackagingE2E/legacy-inventory-15d-20260908-102959.txt`

| Classe | Caminho | Versão / notas |
|--------|---------|----------------|
| **LEGACY** | `C:\Program Files\Primo Auto Elétrica` | EXE **0.0.0.0**; `unins000.exe` presente; **sem** `primoauto.db` dentro de Program Files |
| **DEVELOPMENT** | `%LOCALAPPDATA%\PrimoAutoEletrica\App` | EXE **1.0.0** / ProductName PRIMOX Workshop — fluxo `Deploy-ToInstalledApp.ps1` |
| **OFFICIAL (alvo)** | `C:\Program Files\PRIMOX\Workshop` | Ausente no inventário inicial; instalado após limpeza |
| **TEST** | `%LOCALAPPDATA%\PRIMOX-Workshop-InstallTest-15C` etc. | Harness Packaging E2E |
| **UNKNOWN** | cópias em `Backups\BeforeDeploy`, Temp publish | Histórico de deploy — não tratados como install comercial |

### Banco

| Banco | Papel |
|-------|-------|
| `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` | **Único banco de usuário** (~2.4 MB; migrations **32**) |
| Dentro do PF legado | **Nenhum** |

### Atalhos (antes)

| Tipo | Target |
|------|--------|
| LEGACY Start Menu | `C:\Program Files\Primo Auto Elétrica\PrimoAutoEletrica.exe` |
| Desktop comercial | (ausente / reparado após install oficial) |

### Registry (antes)

- ARP: `Primo Auto Elétrica` → `Primo Auto Elétrica_is1` (legado Inno)
- AppId comercial alvo: `PRIMOX.Workshop.1`

---

## 4. Backup antes da remoção

Destino:

`%LOCALAPPDATA%\PrimoAutoEletrica\Backups\LegacyCleanup-15D-20260908-103031\`

| Campo | Valor |
|-------|-------|
| Cópia DB | `primoauto-official-copy.db` |
| Tamanho | 2424832 |
| SHA256 | `A0100377E8732E49D2D517C20BF7867F8A530E4A69F7100039615EBB92C95209` |
| integrity_check | ok |
| foreign_key_check | 0 |
| SchemaMigrations | 32 |
| Configs | `business-config.json`, `database-settings.json`, `station-config.json`, temas |

**Não** houve merge nem substituição do banco oficial.  
**Não** houve migração automática de dados de versões antigas.

---

## 5. Remoção do legado

| Campo | Resultado |
|-------|-----------|
| Método | `unins000.exe /VERYSILENT` (elevado) + remoção de restos de pasta PF se necessário |
| Script | `Scripts/Cleanup-LegacyPrimoInstall.ps1` (dry-run sem `-ConfirmCleanup`) |
| Legacy removida | **SIM** |
| Dados usuário preservados | **SIM** (`primoauto.db` + AppData intactos) |
| Dev App preservado | **SIM** |
| Remoção agressiva sem uninstaller | **Não** |

---

## 6. Instalação oficial pós-limpeza

| Campo | Valor |
|-------|-------|
| Path | `C:\Program Files\PRIMOX\Workshop\PrimoAutoEletrica.exe` |
| ProductVersion | **1.0.0** |
| FileVersion | **1.0.0.0** |
| ProductName | **PRIMOX Workshop** |
| Publisher (Inno/ARP) | CamposCodingHub |
| AssemblyCompany | Primo Auto Elétrica (**KNOWN LIMITATION**) |
| AppId | `PRIMOX.Workshop.1` (`…\Uninstall\PRIMOX.Workshop.1_is1`) |
| Banco runtime | `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` |
| Logs | `%LOCALAPPDATA%\PrimoAutoEletrica\Logs` |

### Atalhos (depois)

| Atalho | Target | Classe |
|--------|--------|--------|
| Desktop público `PRIMOX Workshop.lnk` | `C:\Program Files\PRIMOX\Workshop\PrimoAutoEletrica.exe` | **OFFICIAL** |
| Start Menu `PRIMOX Workshop` | idem | **OFFICIAL** |
| Atalhos `Primo Auto Elétrica` | removidos com o legado | — |
| Dev App | sem atalho comercial apontando para ele | **DEVELOPMENT** separado |

---

## 7. Isolamento Packaging E2E × comercial (correção 15D)

**Problema encontrado:** reexecutar Packaging E2E com o **mesmo** AppId `PRIMOX.Workshop.1` em `/DIR` sob LocalAppData **hijackava** ARP/atalhos comerciais; o uninstall E2E removia links do Menu Iniciar/Desktop.

**Correção:**

| Artefato | Mudança |
|----------|---------|
| `Installer/PrimoAutoEletrica.iss` | `AppId` / `AppGroupName` / `OutputBaseFilename` injetáveis (`#ifndef`) |
| `Scripts/Build-PrimoXCommercialRelease.ps1` | `-AppId`, `-SetupNameSuffix` |
| `Scripts/Test-InstalledPackageE2E.ps1` | Setup `*-PackagingE2E.exe`, AppId `PRIMOX.Workshop.PackagingE2E`, `/TASKS=!desktopicon` |

AppId comercial **`PRIMOX.Workshop.1` preservado** (Fase 15B).

---

## 8. Setup / hashes

| Setup | SHA256 |
|-------|--------|
| Comercial `PRIMOX-Workshop-Setup-1.0.0.exe` (rebuild 15D pós-isolamento) | `6053EBFFC1028D8F97752B2F2A2F58B7C43BA000E1EF879F21843E4D323B67DC` |
| PackagingE2E `…-PackagingE2E.exe` | `982E6C812BDBBB0E04ACD69C3028042922B4F88D114EB42439C23B3FBD4F6448` |

Diferença vs hash 15C (`67F4…`): rebuild do Setup após defines Inno injetáveis / pipeline — **esperado**; não é regressão de produto.

---

## 9. Testes executados

| Teste | Status | Evidência |
|-------|--------|-----------|
| Inventário | PASS | `legacy-inventory-15d-20260908-102959.txt` |
| Backup pré-uninstall | PASS | `LegacyCleanup-15D-20260908-103031` |
| Uninstall legado | PASS | PF legado ausente; DB prod permanece |
| Install oficial | PASS | PF `PRIMOX\Workshop` + ARP |
| Startup atalho/EXE oficial | PASS | Process Path = Program Files; Responding |
| Processo órfão pós-fecho | PASS | nenhum |
| Database path | PASS | LocalAppData `primoauto.db` |
| CRUD (QaEngine Cliente/Veículo/…) | PASS | `2026-09-08_10-52-44` 37/37 |
| CRUD login interativo “Cliente E2E 15D” | **NOT TESTABLE** | requer sessão manual |
| Backup UI oficial | **PARTIAL** | cópia controlada + integrity PASS; UI login NOT TESTABLE |
| Restore isolado | PASS (herdado 15C) | cópia isolada; sem tocar produção |
| Uninstall / retenção / reinstall | PASS | Packaging E2E `20260908-104340` |
| Package E2E (AppId isolado) | PASS | idem |
| QaEngine | **37/37 PASS** | `2026-09-08_10-52-44` (reexecução isolada após floco 35/37 concorrente) |
| Deep QA | **6/6 PASS** | `2026-09-08_10-50-20` |
| Long Run 5 | **PASS** | `2026-09-08_10-59-13` |
| Windows Sandbox / máquina zero | **NOT TESTABLE** | Sandbox indisponível nesta estação |
| Cliente com legado em VM limpa | **NOT TESTABLE** | cenário executado nesta máquina de desenvolvimento (equivalente parcial) |
| Assinatura / SmartScreen | **KNOWN** | SIGNING NOT CONFIGURED — alerta possível, não bug do installer |

Regressões de produto: **0** (falha QaEngine 35/37 foi floco `FocusVisualStyle` UnsetValue sob concorrência com install; reexecução limpa 37/37).

---

## 10. Estado final da máquina (comercial)

**ANTES**

- Primo Auto Elétrica **0.0.0.0** em Program Files  
- Atalhos legados  
- Dev App em LocalAppData  
- Banco em AppData  

**DEPOIS**

- **PRIMOX Workshop 1.0.0** em `Program Files\PRIMOX\Workshop`  
- Atalhos → EXE oficial  
- Dados + backup LegacyCleanup preservados  
- Development App mantido, **sem** atalho comercial  
- Legado **0.0.0.0** removido  

---

## 11. Procedimento para clientes legados

1. Identificar versão antiga (EXE ProductVersion / pasta `Primo Auto Elétrica`).  
2. Localizar banco em `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` (não confiar em Program Files).  
3. Backup (cópia + SHA256 + integrity).  
4. Confirmar integridade.  
5. Instalar `PRIMOX-Workshop-Setup-1.0.0.exe`.  
6. Confirmar banco ainda no AppData.  
7. Confirmar atalho → `Program Files\PRIMOX\Workshop`.  
8. Testar startup.  
9. Validar dados.  
10. Remover legado **somente** via desinstalador (`unins000` / Apps & Features), após confirmação.  

**Não** apagar `C:\Program Files\Primo Auto Elétrica` à mão sem backup.  
Ops: `Scripts/Cleanup-LegacyPrimoInstall.ps1 -ConfirmCleanup`.

Detalhe usuário: `INSTALLATION.md`.

---

## 12. Critérios de sucesso (checklist)

- [x] Instalações inventariadas  
- [x] Oficial / legado / development identificados  
- [x] Banco legado (usuário) identificado + protegido  
- [x] Backup antes da remoção  
- [x] Atalhos inventariados; comercial → oficial  
- [x] 0.0.0.0 removida  
- [x] Development separado  
- [x] Registry / AppId verificados  
- [x] Versão oficial 1.0.0; banco/logs AppData  
- [x] Package E2E / Uninstall / retenção / reinstall PASS  
- [x] QaEngine / Deep QA / Long Run PASS  
- [x] Documentação atualizada  
- [ ] Sandbox clean machine — NOT TESTABLE  
- [ ] Login CRUD manual completo — NOT TESTABLE  

---

## 13. Riscos / limitações

| Item | Classe |
|------|--------|
| AssemblyCompany legado no EXE | KNOWN LIMITATION |
| SIGNING NOT CONFIGURED / SmartScreen | KNOWN LIMITATION |
| Update A→B comercial | NOT IMPLEMENTED |
| Packaging E2E com AppId comercial na mesma máquina | risco documentado — mitigado com AppId PackagingE2E |
| Migrations 32 vs 27 no DB histórico | KNOWN (classe B; sem purge) |
| Windows Sandbox | NOT TESTABLE |

---

## 14. Decisão

**GO** — commercial path limpo nesta estação; legado controlado; dados preservados; regressão QA verde; tag intacta.

**PARAR** — não iniciar Fase 16 / website / licença / auto-update / signing sem revisão humana.
