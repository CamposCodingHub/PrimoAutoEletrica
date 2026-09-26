# C2.2 FINAL — Deterministic PRIMOX Intelligent Search

**Branch:** `cycle-c2/primox-intelligence`  
**Date (America/Sao_Paulo):** 2026-09-26  
**C2.3 started:** **NO**

## Decision

**PASS**

Absence of OpenAI is **not** a failure. Protected SHA unchanged. `origin/main` untouched. No P0/P1.

## Objective

"Buscar no PRIMOX": normalize → retrieve → deterministic relevance → group → evidence → open source. Never invent results. Deterministic retrieval — not "IA".

## Git / baseline

| Item | Value |
|------|-------|
| Branch | `cycle-c2/primox-intelligence` |
| C2.1 HEAD (start) | `18f807dbc3aeb63b4485eee44b3ea187148cdf35` |
| origin/main | `bf1eb784a3ed45782487197f38d9ba15d319997e` (not altered) |
| C2.2 commit | `c77b844824c8364c147bbed2e73d2fd06a87e5d5` |
| Commit message | `C2.2: implement deterministic PRIMOX search` |

## Protected DB

| Check | Before | After |
|-------|--------|-------|
| Path | `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` | same |
| SHA-256 | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` | **C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B** (identical) |
| ReadOnly | True | True |
| Size | 20201472 | 20201472 |
| integrity_check | ok | ok |
| foreign_key_check | OK | OK |

## Architecture delivered

UI (`PrimoxKnowledgeSearchControl` tab in `BaseConhecimentoControl`) → `KnowledgeSearchViewModel` → `KnowledgeSearchService` → `KnowledgeRetrievalService` → adapters/index/repos.  
DI in `DependencyInjection/ServiceExtensions.cs`. No SQL in XAML/code-behind. No OpenAI/embeddings/vector DB.

## Contracts / ranking

Documented in `Docs/c2/C2_2_SEARCH.md`. Relevance = retrieval ranking only. Multi-token AND filter prevents incidental false hits (e.g. `XYZ_NAO_EXISTE_PRIMOX_999`). Groups only when non-empty.

## D01–D17 proof

- Unit `D01_to_D17_IndividualSearches_17of17` → 17/17 Found, distinct codes.
- Desktop UiSmoke `Search:D01` / `Search:D17` PASS (installed EXE + App suite).

## Tests

| Suite | Result | Notes |
|-------|--------|-------|
| Unit Debug | **537 PASS / 0 FAIL** | C2.1 baseline 519; delta **+18** C2.2 search tests |
| Unit Release | **537 PASS / 0 FAIL** | |
| UiSmoke Search (installed Desktop EXE) | **9/9 PASS** | Empty, NoResults, D01, D17, Knowledge, DiagnosticCase, OpenSource, Clear, BaseConhecimentoHost |
| UiSmoke App (Release bin via Run-UiSmoke.ps1; same C2.2 bits) | **217/217 PASS** | prior C1 baseline 206/206; delta includes +9 Search:* and other suite growth |
| Builds Debug/Release | **0 errors** | pre-existing warnings only |

## Desktop certification

| Item | Evidence |
|------|----------|
| Install | `%LOCALAPPDATA%\PrimoAutoEletrica\App\PrimoAutoEletrica.exe` |
| Shortcut | `OneDrive\Desktop\PRIMOX Workshop.lnk` → that EXE |
| EXE SHA-256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| DLL SHA-256 | `D6A646969A46871ABA8CB6BB24596AF7F4DD865877E35CA2EB7D16ADAE9B420C` |
| Search flow on installed EXE | PASS (dedicated Search filter) |
| Theme | Search UI uses Design System `DynamicResource` brushes (Light/Dark). Dedicated screenshot matrix not automated; no theme regressions in App UiSmoke. |

## Performance samples (real)

See `QA_EVIDENCE/C2_2/perf_samples_final.md`. Desktop Search UiSmoke examples: D01=143ms, D17=130ms, queda de tensão=185ms, NoResults=170ms (installed EXE sandbox).

## RBAC

Service-layer filter for PURCHASE/FINANCIAL + `GetBySourceId` denial without finance permission. Unit tests PASS.

## Bugs / limitations

1. Assist UI still constructs `AssistantService` in code-behind (pre-existing).  
2. Checklist/measurement adapters still reserved (C2.1).  
3. In-memory index only (no FTS5 / protected migration).  
4. Multi-token AND may reduce recall for partial phrases — intentional anti-false-positive.  
5. Light/Dark visual screenshot matrix for search cards not separately automated.  
6. Full App UiSmoke certified on Release bin EXE (Run-UiSmoke.ps1); Search dedicated suite certified on installed App EXE via shortcut target.  
7. **C2.3 not started.**

## Truth matrix

`Docs/c2/C2_TRUTH_MATRIX.md` — Token Retrieval IMPLEMENTED; OpenAI/Embedding/Vector NOT_IMPLEMENTED; Knowledge Search IMPLEMENTED; D01–D17 IMPLEMENTED; RBAC IMPLEMENTED.

## Git audit

No C2.3 / OpenAI / protected migration / `main` contamination.

## Status

**PASS**