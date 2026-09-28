# C5.12 FINAL HARDENING

**Date:** 2026-09-28 06:47:13 -03  
**Branch:** cycle-c5/primox-intelligence  
**Tip before this commit:** `5c504e5e3389c61e60db3b4722af94b6503dc03e`

## Suites

| Suite | Result |
|-------|--------|
| Unit full | **PASS 695/695** |
| ExternalAssistant | **PASS 51/51** |
| C5 filter tests | **PASS 40/40** |
| Full App UiSmoke (Release bin) | **PASS 219/219** |
| Installed EXE (shortcut â†’ Local App, intelligence filter) | **PASS 13/13** |
| Themes/resolutions 8 | **NOT_APPLICABLE** (no visual/UI change this cycle) |
| Provider OFF | **PASS** (default) |
| Provider ON LIVE | **LIVE_NOT_TESTED** (keys ABSENT) |
| Protected DB SHA | **PASS** unchanged `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` |
| Audit durable reopen | **PASS** (Unit C51_CRITICAL_Write_DisposeReopen_ReadBack_FromSqlite PASS (included in 695/695)) |

## Desktop / EXE (republished via Deploy-ToInstalledApp)

| Item | Value |
|------|-------|
| Shortcut | OneDrive\Desktop\PRIMOX Workshop.lnk â†’ Local App EXE |
| Install dir | %LOCALAPPDATA%\PrimoAutoEletrica\App |
| EXE SHA-256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| DLL SHA-256 | `E9D6EEEB6A1ECEC32346F1B174EFCF1295080EACB59A3BBDDDF9C64E2247577A` |

## Evidence
- TestResults/UiSmoke/c5_cont_fullapp_2026-09-28_06-31-35
- TestResults/UiSmoke/c5_cont_installed_2026-09-28_06-45-43
- QA_EVIDENCE/C5_CONTINUOUS/

## Decision
**C5.12 = PASS** with honesty: LIVE_NOT_TESTED (keys ABSENT). Durable audit **PASS**.
