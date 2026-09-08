# SECURITY.md

Estado alinhado à Product Truth Audit 1.0 (`Docs/qa/PRIMOX-PRODUCT-TRUTH-AUDIT-1.0.md`).

## Implementado e em uso (WPF)

- Senhas: PBKDF2 (`PasswordHasherService`)
- Login lockout: 5 falhas / 15 min (`LoginTentativasSeguranca`)
- Soft delete LGPD (`IsDeleted` + `SoftDeleteService`)
- Reset sistema: senha do admin logado (sem hardcoded)
- SQL dinâmico: `SqlIdentifierGuard`
- RBAC de módulos na UI: `PermissionService` (não confundir com policies ASP.NET)

## Parcial / não exigir como “completo”

- **2FA TOTP:** biblioteca + `TwoFactorSetupWindow` existem (`TwoFactorService` / OtpNet).  
  **O fluxo de Login atual não exige código TOTP.** Não declarar “2FA DONE no login” até wiring + persistência + reteste.
- **API:** CORS configurável (`Cors:AllowedOrigins`). JWT/Keycloak/policies nomeadas (`AdminOnly`, etc.) **não** estão efetivamente aplicados nos endpoints mínimos.

## Fora / futuro

- Emissão fiscal com certificado A1/A3
- Hardening API multi-tenant / SaaS
