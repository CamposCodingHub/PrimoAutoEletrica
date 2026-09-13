# PRIMOX NET10 — PROMOTION COMMIT AUDIT

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

**Timestamp:** 2026-09-13  
**Branch:** `migration/net10`  
**HEAD:** `19587e7`  
**Base:** `main` = `29b19b1` (= `primox-net6-final`)  
**Evidence:** `TestResults/Net10-Overnight/20260913/NET10-Promotion-Readiness/`

## Ancestry

- `migration/net10` is a **fast-forward descendant** of `main` (`29b19b1`).
- Commits only on NET10 (`main..HEAD`): **27**
- Commits only on main (`HEAD..main`): **0**

## Commit classification (`main..HEAD`)

| Class | Count | Examples |
|---|---:|---|
| docs | 19 | NET10-00…23 reports, comparison, final decision |
| fix | 5 | SQLitePCLRaw NU1903, smoke tabs i18n, SecurityRedTeam paths, Calendar/LiveCharts hardening |
| feat | 1 | `dce6d27` migrate TFM to `net10.0-windows` |
| test | 1 | `505435b` forensic promotion gate |
| chore | 1 | `7ad65e9` establish protected migration baseline |

## Functional vs documentation

| Kind | Commits |
|---|---|
| Functional / product | `dce6d27`, `7a1243b`, `c5411a1`, `509f54a`, `ec4b8d8`, `cccbb43`, + clipboard helper in forensic gate |
| Docs / status | majority of branch tip commits |
| Tools | `Net10CompatProbe`, Commercial08DataSeed TFM bump |

## Potentially sensitive areas (reviewed via diff name-status)

| Area | Assessment |
|---|---|
| csproj TFM + package bumps | EXPECTED migration |
| Scripts TFM defaults → csproj-driven | EXPECTED / QA infra |
| ClipboardHelper + callers | bug fix (clipboard under load) |
| CalendarContrastHealer | UI hardening Dark |
| LiveCharts removal | dependency cleanup (unused) |
| Installer scripts | PackagingE2E / TFM read — not commercial AppId change by default |
| No updater / credentials / registry commercial AppId swap in product | no suspicious commercial overwrite path in product code |

## Diff summary vs `main`

- **60 files** · **+2209 / −46**
- Heavy documentation addition; product delta concentrated in TFM, packages, Calendar, clipboard, smoke harness, scripts.

## Verdict (commit/diff)

No commits exclusive to main missing from NET10.  
No suspicious force rewrite of commercial installer AppId in default product path.  
Ready for further live gates (this document is forensic inventory only).
