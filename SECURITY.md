# SECURITY.md

- Senhas: PBKDF2 (`PasswordHasherService`)
- Login lockout: 5 falhas / 15 min
- 2FA TOTP (`TwoFactorService`)
- Soft delete LGPD (`IsDeleted` + `SoftDeleteService`)
- Reset sistema: senha do admin logado (sem hardcoded)
- API: CORS restritivo (`Cors:AllowedOrigins`)
- SQL: `SqlIdentifierGuard` para identificadores dinâmicos
