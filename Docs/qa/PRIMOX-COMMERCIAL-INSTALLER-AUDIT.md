# PRIMOX — COMMERCIAL INSTALLER AUDIT (Script 7)

**Data:** 2026-09-09 / 2026-09-10  
**HEAD baseline:** `38a86b4`  
**Tag:** `v1.0.0` → `72d85fa` (**PRESERVADA / não movida**)  
**Branch:** `main`  
**Decisão:** **YELLOW** — `READY FOR COMMERCIAL SIGNING` · `CODE SIGNING BLOCKED BY EXTERNAL CERTIFICATE`

```text
NÃO SIMULAR CODE SIGNING
Certificate fake / localhost ≠ assinatura comercial
```

---

## Baseline

| Item | Valor |
|------|-------|
| Worktree início | clean |
| Deploy scripts | PRESENT (rastreados) |
| Installer | `Installer/PrimoAutoEletrica.iss` |
| Pipeline oficial | `Scripts/Build-PrimoXCommercialRelease.ps1` |

---

## Identidade / metadata

| Campo | Antes | Depois (Script 7) |
|-------|-------|-------------------|
| ProductName | PRIMOX Workshop | PRIMOX Workshop |
| ProductVersion | 1.0.0 | 1.0.0 |
| FileVersion | 1.0.0.0 | 1.0.0.0 |
| InformationalVersion | 1.0.0 | 1.0.0 |
| AssemblyCompany | Primo Auto Elétrica | **CamposCodingHub** |
| Copyright | © Primo Auto Elétrica | **Copyright (C) 2026 CamposCodingHub** |
| AppPublisher (Inno) | CamposCodingHub | CamposCodingHub |
| AppName | PRIMOX Workshop | PRIMOX Workshop |
| Shortcut | PRIMOX Workshop | PRIMOX Workshop |
| DefaultDir | `{commonpf}\PRIMOX\Workshop` | inalterado |
| Dados usuário | `%LOCALAPPDATA%\PrimoAutoEletrica` | inalterado |

**Evidência publish:** `Company=CamposCodingHub; Product=PRIMOX Workshop; PV=1.0.0; FV=1.0.0.0`

**Nota:** Marca de oficina “Primo Auto Elétrica” permanece no domínio do produto; publisher/company Windows alinhados a CamposCodingHub (sem inventar CNPJ).

**OriginalFilename=.dll:** comportamento conhecido do apphost .NET — documentado, não tratado como falha de packaging.

---

## Ícone

| Superfície | Fonte | Status |
|------------|-------|--------|
| EXE | `PrimoAutoEletrica/icon.ico` (~41 KB) | PASS |
| Setup | `SetupIconFile=..\PrimoAutoEletrica\icon.ico` | PASS |
| UninstallDisplayIcon | `{app}\PrimoAutoEletrica.exe` | PASS |
| Atalhos | apontam para EXE | PASS |

---

## Instalador Inno

Endurecido:

- `VersionInfoVersion=1.0.0.0` (`AppVersionInfo`)
- `VersionInfoProductVersion=1.0.0`
- URL GitHub case-correct `CamposCodingHub`
- Comentário explícito: signing só pós-ISCC com cert comercial

Dados: uninstall **não** remove AppData (política preservada).

---

## Publish / build reproduzível

```powershell
$env:DOTNET_ROLL_FORWARD='LatestMajor'
.\Scripts\Build-PrimoXCommercialRelease.ps1 -Version 1.0.0
```

| Etapa | Resultado |
|-------|-----------|
| restore/build | PASS |
| publish win-x64 self-contained | PASS |
| ISCC | PASS |
| manifest `PRIMOX-PUBLISH-MANIFEST.json` | PASS |
| signing hook | BLOCKED (sem thumbprint) |

---

## Code signing

| Item | Status |
|------|--------|
| signtool.exe | PRESENT (Windows Kit 10 x64) |
| Cert comercial Code Signing | **ABSENT** |
| Private key no store | 1 (Subject=`localhost`, EKU=Server Auth) — **rejeitado** |
| `PRIMOX_CODESIGN_THUMBPRINT` | ABSENT |
| Authenticode EXE/Setup | **NotSigned** |
| Timestamp | NOT APPLICABLE |
| SmartScreen reputation | **NOT VERIFIED** |

Scripts:

- `Scripts/Check-CodeSigningReadiness.ps1` → `RESULT: BLOCKED`
- Pipeline assina **somente** se thumbprint + EKU Code Signing + não-localhost

---

## SHA256 (artefato oficial desta sessão)

| Artefato | SHA256 |
|----------|--------|
| `PRIMOX-Workshop-Setup-1.0.0.exe` | `B1AAE306EE7E4108C8FDCF1622B03FA6992CFDEFA55F3151D7B275531F2799F2` |
| `PRIMOX-Workshop-Setup-1.0.0-PackagingE2E.exe` (rebuild S7) | `9B742BA201E1D333970ECB5CB6D6A481D5EA1AB96677FB4B248295242579B557` |

Arquivo: `artifacts/checksums/PRIMOX-Workshop-Setup-1.0.0.sha256.txt`

---

## Install / startup / QA instalado

| Teste | Resultado | Evidência |
|-------|-----------|-----------|
| Silent install (PackagingE2E) | PASS | Exit=0 |
| EXE + versão | PASS | PV=1.0.0 FV=1.0.0.0 Company=CamposCodingHub |
| Startup | PASS | Responding ~12s |
| Installed QaEngine smoke | PASS | Exit=0 (~7 min) |
| DB creation | PASS | AppData teste |
| integrity_check | PASS | ok |
| foreign_key_check | PASS | 0 |
| migrations fresh | PASS | **28** (inclui `202609080001` fiscal) |
| Uninstall silent | **FAIL / TIMEOUT** | `unins000` hang; elevação UAC cancelada no retry |
| Data preservation (política) | PASS | DB teste + marker AppData preservados |
| Reinstall (após force-clean dir) | **FAIL** | Setup Exit=2 (possível conflito AppId/registry E2E); não inventar PASS |
| AUTO-UPDATE | NOT IMPLEMENTED | fora do escopo |

**Uninstall:** limitação real do ambiente E2E silencioso nesta sessão (processo `unins000` hang). Não inventar PASS. Pipeline E2E atualizado com `/FORCECLOSEAPPLICATIONS` + timeout + kill pré-uninstall.

---

## Segurança

| Scan | Resultado |
|------|-----------|
| PEM / PRIVATE KEY / PFX no Git | não introduzido |
| Token fiscal em logs de packaging | não |
| Cert localhost usado como comercial | **bloqueado no pipeline** |

---

## QA regressão

| Suite | Resultado |
|-------|-----------|
| Fiscal unit | PASS 46/46 |
| Build Release packaging | PASS 0 errors |
| QaEngine (workspace) | **PASS 43/43** (`TestResults/UiSmoke/2026-09-09_21-12-46`) |
| QaEngine (instalado PackagingE2E) | PASS (smoke Exit=0) |
| Deep QA / Exhaustive / Long Run | **NOT RE-RUN** nesta sessão (sem mudança de UI de negócio); baseline 09/09 documentada |

---

## Como assinar quando o certificado existir

1. Instalar certificado Authenticode OV/EV no store (CurrentUser ou LocalMachine `\My`).  
2. `$env:PRIMOX_CODESIGN_THUMBPRINT = '<thumbprint>'` (não commitar).  
3. Opcional: `$env:PRIMOX_CODESIGN_TIMESTAMP_URL = 'http://timestamp.digicert.com'`  
4. `.\Scripts\Check-CodeSigningReadiness.ps1` → READY_TO_SIGN  
5. `.\Scripts\Build-PrimoXCommercialRelease.ps1 -Version 1.0.0`  
6. Confirmar `signtool verify /pa` e Authenticode `Valid`

---

## Decisão

```text
YELLOW
READY FOR COMMERCIAL SIGNING
CODE SIGNING BLOCKED BY EXTERNAL CERTIFICATE
v1.0.0 PRESERVADA
Production fiscal permanece fora deste script
AUTO-UPDATE = NOT IMPLEMENTED
```

**STOP** — não iniciar Script 8.
