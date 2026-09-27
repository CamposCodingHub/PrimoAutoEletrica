# C4.12 FINAL HARDENING

**Date:** 2026-09-27 20:50:45 -03 | **Branch:** cycle-c4/primox-intelligence | **Tip before this commit:** `1782e65481a4631df720bfecff1423bc184433a8`

## Suites

| Suite | Result |
|-------|--------|
| Unit full | **PASS 655/655** |
| ExternalAssistant | **PASS 51/51** |
| C4 filter tests | **PASS 35/35** |
| Full App UiSmoke (Release bin) | **PASS 219/219** |
| Installed EXE (shortcut → Local App, intelligence filter) | **PASS 13/13** |
| Themes/resolutions 8 | **NOT_APPLICABLE** this cycle (no visual/UI change); Tema covered in installed filter |
| Provider OFF | **PASS** (default) |
| Provider ON LIVE | **LIVE_NOT_TESTED** (keys ABSENT) |
| Protected DB SHA | **PASS** unchanged `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` |
| Audit durable | **NOT_IMPLEMENTED** (in-memory only) |

## Desktop / EXE (republished via Deploy-ToInstalledApp)

| Item | Value |
|------|-------|
| Shortcut | OneDrive\Desktop\PRIMOX Workshop.lnk → Local App EXE |
| Install dir | %LOCALAPPDATA%\PrimoAutoEletrica\App |
| EXE SHA-256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| DLL SHA-256 | `D2075BBC720F5B500C09E17EAF92A0C0B9C351C3649124A95FC22543C8AE6E55` (updated this cycle) |

## Evidence
- `TestResults/UiSmoke/c4_cont_fullapp_2026-09-27_20-25-35`
- `TestResults/UiSmoke/c4_cont_installed_2026-09-27_20-39-43`
- `QA_EVIDENCE/C4_CONTINUOUS/`

## Decision
**C4.12 = PASS** with honesty: LIVE_NOT_TESTED; Audit NOT_IMPLEMENTED.
