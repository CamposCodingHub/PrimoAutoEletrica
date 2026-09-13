# PRIMOX FULL ASSURANCE-12 — PATH SECURITY

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

## Sandbox layout

```
PRIMOX_SECURITY_SANDBOX/CANARY_INSIDE.txt
PRIMOX_SECURITY_OUTSIDE/CANARY_OUTSIDE.txt
```

## Product enforcement

`DatabaseBackupService.RestaurarBackup` / custom backup destino now call `PathSecurityHelper.RequireUnderAnyRoot` against:

- service backup directory  
- `App.RuntimeAppDataPath` (+ Backups)  
- optional network backup dir  
- known PackagingE2E data roots  

Escape via `..`, mixed separators, absolute outside paths → `UnauthorizedAccessException`.

## Results

| Probe | Result |
|-------|--------|
| Dot-dot variants | REJECT (unit + smoke) |
| Absolute outside canary | REJECT |
| Junction to outside | CREATED in sim; product `GetFullPath` resolves outside → REJECT when root is sandbox/AppData |
| ZIP slip | NOT APPLICABLE (no zip extract in backup path) |

Evidence: `A12Security:PathTraversal:CanaryReject`, `PathSecurityHelperTests`, `Invoke-PathTraversalSimulation.ps1`.
