# PRIMOX FULL ASSURANCE-12 — AUTHORIZATION

## Test users (synthetic only)

| Alias | Profile | SISTEMA_CONFIGURAR |
|-------|---------|--------------------|
| ADMIN_TEST | Administrador (smoke admin) | ALLOW |
| OPERATOR_TEST | Vendedor | DENY |
| USER_TEST | Tecnico | DENY |

## Matrix (service-level, verified)

| Operation | ADMIN | OPERATOR | USER |
|-----------|-------|----------|------|
| `SystemConfigurationService.SaveAuthorized` | ALLOW | DENY | DENY |
| `CriarBackupManualAuthorized` | ALLOW | DENY | DENY |
| `RestaurarBackupAuthorized` | ALLOW | DENY | DENY |
| UI Configurações backup/restore buttons | gated via `TryGarantirPermissaoConfiguracao` + Authorized APIs | DENY | DENY |

## Evidence

- Smoke: `A12Security:Authorization:BackupRestoreServiceGate` PASS  
- Units: `BackupAuthorizationTests` PASS  
- Prior Configuracoes smoke permission check retained  

## Architecture note

Repositories do not re-check permissions. Finding only if caller can reach mutating APIs without gate. A12 closed backup/restore gap at service boundary (same pattern as config save).
