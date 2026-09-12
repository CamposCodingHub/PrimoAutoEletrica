# PRIMOX RELEASE-01 — INSTALLER

**Script:** `Installer/PrimoAutoEletrica.iss` (lido do disco)

| Campo ISS | Valor |
|---|---|
| AppName | PRIMOX Workshop |
| AppVersion | 1.0.0 |
| AppPublisher | CamposCodingHub |
| DefaultDirName | `{commonpf}\PRIMOX\Workshop` |
| OutputBaseFilename | PRIMOX-Workshop-Setup-1.0.0 |
| SetupIconFile | `PrimoAutoEletrica\icon.ico` |
| PrivilegesRequired | admin |
| ArchitecturesInstallIn64BitMode | x64compatible |
| CloseApplications | force (`PrimoAutoEletrica.exe`) |
| UninstallDisplayName | PRIMOX Workshop |
| Shortcuts | Start Menu + Desktop (task) |
| Registry | HKLM `Software\CamposCodingHub\PRIMOX Workshop` |
| Data | AppData **não** removido no uninstall |

## Artefato oficial

| Campo | Valor |
|---|---|
| File | `artifacts/installer/PRIMOX-Workshop-Setup-1.0.0.exe` |
| Size | 61 687 781 bytes (~58.8 MB) |
| SHA256 | `0BECE6AE5E7C582C51C3B81783DE8557A81F70881B82CA2505373433988607BF` |
| Signing | UNSIGNED / BLOCKED |

## E2E PackagingE2E (AppId isolado)

| Gate | Resultado |
|---|---|
| Cycles 1–3 Install/Start/CRUD/Uninstall | PASS |
| Silent / app-open uninstall | PASS |
| Reinstall + Final CRUD | PASS Exit=0 |
| Data preservation | PASS |
| fails | **0** |

Evidência: `TestResults/Commercial08/commercial-08-e2e-20260912-074302.md`

## Update simulation

Auto-update: **NOT IMPLEMENTED**.  
Over-install Inno (mesma AppId): exercitado via reinstall E2E — **PASS** (não é auto-update).
