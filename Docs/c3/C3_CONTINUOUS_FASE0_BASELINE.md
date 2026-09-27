# C3 CONTINUOUS - FASE 0 Baseline Re-check

**Machine:** CIRO (de411c5d-4243-4085-bf33-8c3d241d7f2f)
**Timezone:** America/Sao_Paulo (UTC-3)
**START:** 2026-09-27 09:57:30 -03:00
**Branch:** `cycle-c3/primox-intelligence`
**HEAD tip:** `b8d21cbec2b02bb5257eac2f052d8afc70783c41`
**origin/main:** `bf1eb784a3ed45782487197f38d9ba15d319997e` **UNTOUCHED**

## Absolute invariants

| Check | Result |
|-------|--------|
| Branch | cycle-c3/primox-intelligence |
| HEAD matches expected tip | YES `b8d21cbec2b02bb5257eac2f052d8afc70783c41` |
| origin/main untouched | YES `bf1eb784a3ed45782487197f38d9ba15d319997e` |
| Protected DB path | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db` |
| Protected SHA-256 | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` |
| Official SHA match | IDENTICAL |
| ReadOnly | True |
| Operational DB | primoauto_operacional.db EXISTS |
| Installed EXE | `%LOCALAPPDATA%\PrimoAutoEletrica\App\PrimoAutoEletrica.exe` |
| EXE SHA-256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| DLL SHA-256 | `91529A1B7A368266A9E98F13E6F07B200172EAABF85149C36615A17F02DABA5F` |

## API keys (Process / User / Machine)

| Variable | Status |
|----------|--------|
| OPENAI_API_KEY | ABSENT |
| PRIMOX_EXTERNAL_AI_KEY | ABSENT |
| PRIMOX_EXTERNAL_AI_API_KEY | ABSENT |

**Implication:** LIVE HTTP = LIVE_NOT_TESTED / EXTERNAL_DEPENDENCY. Do not invent live calls.

## Build

| Item | Result |
|------|--------|
| `dotnet build PrimoAutoEletrica.sln -c Release` | **0 Error(s)** (warnings only CA1416/CS8632) |
| Evidence | `QA_EVIDENCE/C3_CONTINUOUS/build_release_2026-09-27_09-56-52.txt` |

## Prior stack (already on tip)

C3.0..C3.24 closed earlier today with `PASS_WITH_EXTERNAL_DEPENDENCY` (live not exercised). This continuous run re-verifies baseline and re-documents C3.13 LIVE honestly, then continues hardening / C4.0 Discovery if gates OK.

## Working tree note

Untracked noise only: QA_EVIDENCE/*, TestData/Catalogos PDFs, smoke patches. No product source dirty from this baseline step.
