# C3 PREFLIGHT — Post Night-Run GO/NO-GO

**Machine:** CIRO (`de411c5d-4243-4085-bf33-8c3d241d7f2f`)  
**Timezone:** America/Sao_Paulo (UTC-3)  
**Date:** 2026-09-26  
**Base tip:** `ee39fad` (`cycle-c2/primox-intelligence` Night Run final)  
**Decision:** **GO** (no P0/P1; protected SHA + origin/main intact)

## Times

| | Value |
|--|--|
| START | 2026-09-26 21:01 -03:00 |
| END | 2026-09-26 21:35 -03:00 |

## Git / main

| Check | Value |
|-------|-------|
| Start branch | `cycle-c2/primox-intelligence` @ `ee39fad06d743d4afc5641390c91536cce8d08aa` |
| origin/main | `bf1eb784a3ed45782487197f38d9ba15d319997e` **unchanged** |
| Working tree product dirty | none (untracked QA_EVIDENCE/TestData noise only) |

## Protected DB

| Check | Value |
|-------|-------|
| Path | `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` |
| SHA-256 | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` **IDENTICAL** (before + after) |
| ReadOnly | True |
| Mutations | **NONE** (UiSmoke used isolated `--app-data`) |

## Desktop cert

| Item | Value |
|------|-------|
| Shortcut | `OneDrive\Desktop\PRIMOX Workshop.lnk` → Local App EXE |
| App folder | `%LOCALAPPDATA%\PrimoAutoEletrica\App` (+ Desktop `App` junction → same) |
| EXE SHA-256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| DLL SHA-256 | `26859BB3001C49503CAE66C24ECEEE2901A792B673C729B2486F122B36BF0324` |

## UiSmoke

| Suite | Result | Evidence |
|-------|--------|----------|
| Full App (no filter, Release bin) | **217/217 PASS** | `TestResults/UiSmoke/preflight_c3_2026-09-26_21-01-34` |
| Light/Dark + intelligence core (`Search\|MainWindow\|Tema\|BaseConhecimento\|Dashboard\|Calendar`) | **17/17 PASS** | `.../preflight_c3_theme_2026-09-26_21-15-55` |
| Resolutions + theme engine (`QaEngine\|Dvi\|Tema\|Search\|BaseConhecimento`) | **56/56 PASS** incl. `QaEngine:ResponsividadeResolucoes` (1366/1600/1920/2560) + `Dvi:*1280` Light/Dark + `Tema:*` | `.../preflight_c3_res_2026-09-26_21-28-11` |

## Unit

| Suite | Result |
|-------|--------|
| Debug | **569 PASS / 0 FAIL / 0 SKIP** |

## Honest limitations (non-P0)

1. ExhaustiveUi / DeepQa full screenshot inventories not re-run (opt-in long suites); QaEngine resolution + Tema + Dvi 1280 + Search cover critical + intelligence surfaces.
2. Desktop `App` was missing as a folder name on Desktop; created **junction** to Local App for cert clarity (shortcut already pointed at Local App).
3. Installed App bits match Night Run deploy; preflight UiSmoke certified Release **bin** path (same tip). Re-deploy not required for GO.
4. OpenAI / live external AI still **NOT_IMPLEMENTED** (by design until C3 enables stub path only).

## Gate

**START C3** on new branch `cycle-c3/primox-intelligence` from `ee39fad` without further user prompt (continuous authorization).
