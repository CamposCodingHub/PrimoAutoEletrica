# C3.11 FINAL — Desktop publish + UiSmoke

**Decision:** **PASS**  
**Date:** 2026-09-27 (America/Sao_Paulo)

## Deploy

- Target: `%LOCALAPPDATA%\PrimoAutoEletrica\App` via `Scripts/Deploy-ToInstalledApp.ps1`
- Shortcut updated: PRIMOX Workshop.lnk
- Protected DB SHA unchanged: `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
- EXE SHA: `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B`
- DLL SHA: `125C0D549539A5A679FF0BC2A6EF4C929920E17AC10DBFE8620A51DD2F91B99A`

## UiSmoke

| Target | Result | Path |
|--------|--------|------|
| Release bin | 13/13 PASS | TestResults/UiSmoke/c3_11_intelligence_2026-09-27_07-39-54 |
| Installed App | 13/13 PASS | TestResults/UiSmoke/c3_11_installed_2026-09-27_07-41-29 |

Filter: Search|MainWindow|Tema|BaseConhecimento|Dashboard