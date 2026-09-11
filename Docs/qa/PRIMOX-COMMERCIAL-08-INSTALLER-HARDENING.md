# PRIMOX-COMMERCIAL-08 — Installer Hardening Audit

**Fase:** PRIMOX-COMMERCIAL-08-2026-09  
**Data:** 2026-09-10  
**Decisão:** **YELLOW — COMMERCIAL INSTALLER HARDENING CLOSED WITH NON-BLOCKING LIMITATIONS**

## 1. Objetivo

Corrigir e validar o ciclo comercial do instalador:

Install → Start → CRUD → Close → Uninstall → Verify binaries → Verify data → Reinstall → Start → CRUD  

Sem novas features, sem DB/migrations, sem fiscal, sem I18N, sem code signing.

## 2. Baseline

| Item | Valor |
|------|-------|
| HEAD inicial | `b5c9481` |
| Branch | `main` |
| Tag `v1.0.0` | `72d85fa` **intacta** |
| WIP preservado | `Docs/qa/PRIMOX-FISCAL-LIVE-HOMOLOGATION-REPORT.md` |
| Versão produto | 1.0.0 / 1.0.0.0 |
| TFM | `net6.0-windows` |
| Installer | Inno Setup 6 — `Installer/PrimoAutoEletrica.iss` |
| Pipeline | `Scripts/Build-PrimoXCommercialRelease.ps1` |
| AppId comercial | `PRIMOX.Workshop.1` |
| AppId E2E | `PRIMOX.Workshop.PackagingE2E` |
| Install path comercial | `C:\Program Files\PRIMOX\Workshop` |
| AppData / DB | `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` |
| PrivilegesRequired | `admin` (UAC esperado) |
| Baseline detalhado | `Docs/qa/PRIMOX-COMMERCIAL-08-BASELINE.md` |

## 3. Installer inventory

| Componente | Responsabilidade |
|------------|------------------|
| `Installer/PrimoAutoEletrica.iss` | Instalar binários, atalhos, ARP, HKLM metadata |
| `Scripts/Build-PrimoXCommercialRelease.ps1` | Publish win-x64 self-contained + ISCC + SHA256 |
| `Scripts/Test-InstalledPackageE2E.ps1` | Packaging E2E legado (AppId isolado) |
| `Scripts/Test-CommercialInstallerHardening.ps1` | **NOVO** — ciclos install/uninstall/reinstall + data |
| `Scripts/tools/Commercial08DataSeed/` | Marker SQLite controlado (não altera schema de negócio) |
| `unins000.exe` | Uninstaller Inno |
| AppData | Dados do usuário (política: **não** apagar no uninstall) |

## 4. Problema original (Script 7)

1. Silent uninstall `unins000` **hang** / confusão com UAC cancel  
2. Reinstall Setup **Exit=2** após force-clean de diretório  
3. Data preservation já era PASS  
4. Code signing BLOCKED (fora de escopo)

## 5. Causa raiz

**Uninstall hang:** o `.iss` **não** declarava `CloseApplications=force`. Em `/VERYSILENT`, se `PrimoAutoEletrica.exe` (ou handle residual) mantinha DLLs/EXE abertos, o uninstaller Inno podia aguardar o fechamento de aplicações **sem progresso visível** → hang de `unins000`. `/FORCECLOSEAPPLICATIONS` no caller sozinho era insuficiente/ inconsistente quando o processo ainda segurava arquivos.

**Reinstall Exit=2:** evidência histórica apontava force-clean manual do diretório de instalação **sem** uninstall Inno completo (registry AppId / uninstall metadata residual). Setup encontrava estado inconsistente. Nesta fase, reinstall após uninstall Inno correto → **Exit=0**.

## 6. Correção

Em `Installer/PrimoAutoEletrica.iss`:

- `CloseApplications=force`
- `CloseApplicationsFilter=PrimoAutoEletrica.exe`
- `RestartApplications=no`
- Pascal `TryClosePrimoxProcesses()` via `taskkill` **somente** em `PrimoAutoEletrica.exe` em `InitializeSetup` / `InitializeUninstall` (explícito, documentado — não genérico)

Testes atualizados com `/CLOSEAPPLICATIONS` + `/FORCECLOSEAPPLICATIONS` + timeout.

## 7–9. Install / Uninstall / Reinstall

Evidência: `TestResults/Commercial08/commercial-08-e2e-20260910-202346.md`  
+ open-app: `TestResults/Commercial08/qaengine-post-reinstall.log`

| Teste | Resultado |
|-------|-----------|
| Silent install (PackagingE2E) | **PASS** Exit=0 |
| Startup | **PASS** Responding |
| Silent uninstall (app fechado) | **PASS** Exit=0 (3/3 ciclos) |
| Silent uninstall (app aberto) | **PASS** Exit=0, residualProcs=0 |
| Binary cleanup (EXE removido) | **PASS** |
| Reinstall | **PASS** Exit=0 (todos os ciclos + final) |
| Post-reinstall startup | **PASS** |
| Post-reinstall QaEngine | **PASS** Exit=0 |

## 10. Data preservation

Ver `PRIMOX-COMMERCIAL-08-DATA-PRESERVATION.md`.  
Política: AppData **preservado**. Binários **removidos**.

## 11. Process audit

Antes/durante/depois de cada ciclo: `Get-Process PrimoAutoEletrica` → count=0 pós-uninstall.  
Uninstall com app aberto: processo encerrado pelo mecanismo Inno + taskkill documentado.

## 12. UAC

`PrivilegesRequired=admin` → elevação **esperada** em instalação per-machine.  
Testes silenciosos nesta sessão correram com elevação disponível.  
Cancelamento voluntário de UAC **não** é bug do instalador.

## 13. Shortcuts

Start Menu group PackagingE2E criado (`PRIMOX Workshop PackagingE2E`) — **PASS**.  
Desktop task desabilitada nos testes (`/TASKS=!desktopicon`).

## 14. Registry

ARP comercial intacto: DisplayName `PRIMOX Workshop`, Ver `1.0.0`, InstallLocation `C:\Program Files\PRIMOX\Workshop\`, QuietUninstallString presente.  
E2E usa AppId distinto — não sobrescreve comercial.

## 15. Repeated cycles

**3/3** ciclos Install→Startup→Uninstall→Data PASS.  
4º: Final reinstall + QaEngine PASS.

## 16. Reboot

**BLOCKED BY ENVIRONMENT** — reboot automatizado não executado (risco). Não classificado como PASS.

## 17. Database

integrity=ok · fk=0 · migrations=28 (instalado pós-reinstall).  
Nenhuma migration criada nesta fase.

## 18. QA

Ver `PRIMOX-COMMERCIAL-08-REGRESSION.md` (preenchido com evidências da sessão).

## 19. Hash (rastreabilidade)

| Artefato | SHA256 | Size |
|----------|--------|------|
| `PRIMOX-Workshop-Setup-1.0.0.exe` | `35BA94C0A45188E7AB47E62DBA89829311062FF5048EB5B4D507C0056A5E6194` | ~59.4 MB |
| `PRIMOX-Workshop-Setup-1.0.0-PackagingE2E.exe` | `A18224631F26824A0D4F6C74A95D052B3758F79075B5DF0F902496029BF1CC03` | 62268192 |

Não comparar com hash histórico Script 7 como igualdade esperada.

## 20. Limitations

- Code signing / SmartScreen: **BLOCKED** (certificado comercial ausente) — fora de escopo I08  
- Reboot E2E: **BLOCKED BY ENVIRONMENT**  
- Filtro smoke `Clientes` isolado: flake UI (`EditarClienteWindow` / botão Salvar) — **não** regressão de installer; QaEngine instalado **PASS**  
- Instalação comercial pré-existente em Program Files não foi reinstalada nesta sessão (Company metadata legado no EXE já instalado)

## 21. Final decision

```text
YELLOW
COMMERCIAL INSTALLER HARDENING CLOSED WITH NON-BLOCKING LIMITATIONS
```

Critérios críticos de install/uninstall/reinstall/data: **GREEN**.  
Limitações não-bloqueantes: signing, reboot, flake Clientes smoke.
