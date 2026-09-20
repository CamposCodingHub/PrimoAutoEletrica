# E — SECURITY_AUDIT

## Confirmado

1. API anônima em endpoints de negócio (P0-01)
2. PermissionService fail-open pré-fix → fail-closed Unavailable (P0-02 mitigado)
3. License local SHA256 (P0-04)
4. PBKDF2 100k → 600k (P1-01 mitigado)
5. ProblemDetails com ex.Message
6. CI security mascarado → GATE/INFO (P0-07 mitigado)

## Padrões honestos a preservar

- `NotificationService` / status `NaoConfigurado` (não finge envio)
- `FilialService.MultiFilialDisponivel = false`

## Não afirmado

- Pen-test RUNTIME não executado neste executor (sem machineId Windows)
- “SaaS ready” / “NF-e ready” / “sync ready” = **falso**
