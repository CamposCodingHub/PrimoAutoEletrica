# C6.12 FINAL HARDENING

**Date:** 2026-09-28 07:27:24 -03:00  
**Branch:** cycle-c6/primox-intelligence  
**Tip before this commit:** `ca8140ac876143273e81112366f0485b00eeafa3`  
**origin/main:** `bf1eb784a3ed45782487197f38d9ba15d319997e` **UNTOUCHED**  
**Protected DB SHA256:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` **unchanged**

## Suites

| Suite | Result |
|-------|--------|
| Unit full | **PASS 719/719** (was 695; +24 C6) |
| ExternalAssistant | **PASS 51/51** |
| C6 filter tests | **PASS 24/24** |
| Full App UiSmoke (Release bin) | **PASS 219/219** |
| Installed EXE (shortcut → Local App, intelligence filter) | **PASS 13/13** |
| Themes/resolutions Matrix 8 | **NOT_APPLICABLE** (no visual/UI change this cycle) |
| Provider OFF | **PASS** (default) |
| Provider ON LIVE | **LIVE_NOT_TESTED** (keys ABSENT) |
| Local model runtime | **ENVIRONMENT_DEPENDENCY** |
| Protected DB SHA | **PASS** unchanged |

## Desktop / EXE (republished via Deploy-ToInstalledApp)

| Item | Value |
|------|-------|
| Shortcut | OneDrive\Desktop\PRIMOX Workshop.lnk → Local App EXE |
| Install dir | %LOCALAPPDATA%\PrimoAutoEletrica\App |
| EXE SHA-256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| DLL SHA-256 | `00C2B9E538412178692D8E0B6AD8AB91E754C792A61D34D532E16621D3F778FC` |

## Evidence
- TestResults/C6_unit_2026-09-28_07-07-14
- TestResults/C6_extassist_2026-09-28_07-07-31
- TestResults/UiSmoke/c6_cont_fullapp_2026-09-28_07-09-02
- TestResults/UiSmoke/c6_cont_installed_2026-09-28_07-25-33
- QA_EVIDENCE/C6_CONTINUOUS/

## Decision
See `Docs/c6/C6_FINAL_DECISION.md` → formal **NOT_ENOUGH_EVIDENCE** (no vendor sold). Provisional posture = knowledge/deterministic first with optional escalation.