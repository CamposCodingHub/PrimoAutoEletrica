# PRIMOX FULL ASSURANCE-12 — PATH SECURITY

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
