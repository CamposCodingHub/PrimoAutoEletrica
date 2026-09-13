# PRIMOX FULL ASSURANCE-11 — Database

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

## Probe (VERIFIED)

| Check | Result |
|-------|--------|
| PRAGMA integrity_check | **ok** |
| PRAGMA foreign_key_check | **0** violations |
| SchemaMigrations count | **28** |
| Tool | `Scripts/QA/Invoke-DatabaseIntegrity.ps1` |
| Backup/restore file-level | **PASS** (hash match) |

## Cross-check

Seed/verify via `Commercial08DataSeed` on PackagingE2E dataDir during installer cycles — markers preserved across uninstall/reinstall.

## Findings

No orphan/FK corruption detected in probed DBs. No migration rewrite performed.
