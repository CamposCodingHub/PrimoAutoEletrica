# C3 RUN FINAL — C3.1→C3.12 (PRIMOX External AI Path)

**Branch:** `cycle-c3/primox-intelligence`  
**Machine:** CIRO  
**Timezone:** America/Sao_Paulo (UTC-3)  
**START:** 2026-09-27 07:22 -03:00  
**END:** 2026-09-27 07:44:38 -03:00  
**Baseline HEAD (C3.0 PASS):** `dde703222fd0f88c338f859779ca575315dded34`  
**Final HEAD:** `37e1e565fa62bbd9696ac0e8815be6df52836f8f`  
**Decision:** **PASS** (C3.8 = PASS_WITH_EXTERNAL_DEPENDENCY)

## Absolute invariants

| Check | Result |
|-------|--------|
| Protected DB SHA-256 | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` **IDENTICAL** |
| origin/main | `bf1eb784a3ed45782487197f38d9ba15d319997e` **untouched** |
| API keys / secrets in repo | **NONE** |
| Live OpenAI / external API called | **NO** (no OPENAI_API_KEY / PRIMOX_EXTERNAL_AI_KEY in env) |
| CentsV1 / Design System / grounded local Assist default | **preserved** |
| Autonomous buy/approve/pay/fiscal/stock/price/delete | **not enabled** |

## Phase results + commits

| Phase | Result | Commit |
|-------|--------|--------|
| C3.1 HTTP client skeleton | PASS | `0f9bb3f` (stack + tests) |
| C3.2 Evidence package builder | PASS | `da2ce98` (FINAL; impl in 0f9bb3f) |
| C3.3 Response grounding validator | PASS | `030f237` |
| C3.4 RBAC + finance redaction | PASS | `d9c8388` |
| C3.5 Kill-switch + settings UI | PASS | `c36895f` (+ window in C3.10) |
| C3.6 Assist routing | PASS | `c83483e` |
| C3.7 Mocked HTTP integration | PASS | `98ef1dd` |
| C3.8 Live path readiness | **PASS_WITH_EXTERNAL_DEPENDENCY** | `47d1b3e` |
| C3.9 Adversarial / eval | PASS | `33498d4` |
| C3.10 UI Assist polish | PASS | `37e1e56` |
| C3.11 Desktop publish + UiSmoke | PASS | (this commit) |
| C3.12 Hardening + FINAL | PASS | (this commit) |

### git log (C3.0..tip)

```
37e1e56 C3.10: Assist UI polish + External AI settings surface 33498d4 C3.9: phase FINAL gate doc (PASS) 47d1b3e C3.8: phase FINAL gate doc (PASS_WITH_EXTERNAL_DEPENDENCY) 98ef1dd C3.7: phase FINAL gate doc (PASS) c83483e C3.6: phase FINAL gate doc (PASS) c36895f C3.5: phase FINAL gate doc (PASS) d9c8388 C3.4: phase FINAL gate doc (PASS) 030f237 C3.3: phase FINAL gate doc (PASS) da2ce98 C3.2: phase FINAL gate doc (PASS) 0f9bb3f C3.1: HTTP external assistant client skeleton (fail-closed)
```

## Tests

| Suite | Result |
|-------|--------|
| ExternalAssistant filter (C3.0+C3.1–C3.9) | **32 PASS / 0 FAIL** |
| Full Debug unit | **601 PASS / 0 FAIL / 0 SKIP** |
| UiSmoke intelligence (Release bin) | **13/13 PASS** — `TestResults/UiSmoke/c3_11_intelligence_2026-09-27_07-39-54` |
| UiSmoke intelligence (Installed App) | **13/13 PASS** — `TestResults/UiSmoke/c3_11_installed_2026-09-27_07-41-29` |
| Filter | `Search\|MainWindow\|Tema\|BaseConhecimento\|Dashboard` |

## Desktop publish

| Item | Value |
|------|-------|
| Shortcut | `OneDrive\Desktop\PRIMOX Workshop.lnk` → Local App EXE |
| Install dir | `%LOCALAPPDATA%\PrimoAutoEletrica\App` |
| EXE SHA-256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| DLL SHA-256 | `125C0D549539A5A679FF0BC2A6EF4C929920E17AC10DBFE8620A51DD2F91B99A` |
| Deploy evidence | `QA_EVIDENCE/C3_11/deploy_log.txt` |

## Honest limitations

1. **No live external API call** this run — env keys absent; C3.8 documented as PASS_WITH_EXTERNAL_DEPENDENCY; live remains OFF by default.
2. Default Assist consult path remains **local grounded**; external Http provider is selectable when armed (enable+secret+kill-switch off) and still requires local evidence; AssistProviderRouter prefers local unless `preferExternalWhenArmed`.
3. C3.1 commit includes the integrated C3.2–C3.9 service stack so the HTTP provider can fail-closed with evidence/grounding/redaction; subsequent phase commits record FINAL gate docs (+ C3.10 UI).
4. ExhaustiveUi / full 217-check UiSmoke not re-run this cycle; intelligence + theme filter covers Assist/Search/BaseConhecimento surfaces required for C3.11.
5. Settings UI stores **env var name only** under LocalAppData `external-ai-settings.json` (gitignored patterns for secrets remain).

## External dependency (C3.8)

To enable a future minimal live smoke (still fail-closed):

- Set `PRIMOX_EXTERNAL_AI_API_KEY` (or configure `OPENAI_API_KEY` via mapped env name) in user env/secret store
- Explicit enable via settings or `PRIMOX_EXTERNAL_AI_ENABLED=1`
- Kill-switch off (`PRIMOX_EXTERNAL_AI_KILL_SWITCH` unset/0)
- Local evidence package non-empty

## GO / NO-GO

**GO for C3 close** on `cycle-c3/primox-intelligence` with honest external-deps caveat. Do **not** merge to main without separate authorization.