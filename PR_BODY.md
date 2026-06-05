# Feature: Local sync, UI smoke hardening and DB tools

This pull request implements local synchronization handlers, a dedicated `VendaRepository`, hardening for `UiSmokeTestService`, and tooling to support SQL Server integration and migration. It also adds CI workflow and documentation for manual validations.

Summary of changes:

- `PrimoAutoEletrica/Repositories/VendaRepository.cs`: new repository for venda persistence.
- `PrimoAutoEletrica/Services/LocalSyncService.cs` and `LocalSyncMessageHandler.cs`: local sync messaging and idempotent handlers.
- `PrimoAutoEletrica/Services/SynchronizationService.cs`: event idempotence checks.
- `PrimoAutoEletrica/Services/UiSmokeTestService.cs`: added timeouts, retries, and safer window handling.
- `Tools/DbConfigurator`: console tool to test SQL Server connectivity and apply `SqlServerSchema.sql`.
- `Views/DatabaseConfigurationWindow.*`: simple UI to test/save connection string.
- CI workflow: `.github/workflows/ci.yml`.
- Docs: `Docs/PDV_REIMPRESSION_VALIDATION.md`, `Docs/SQLSERVER_MIGRATION_PLAN.md`, `RELATORIO_FINAL_AUTOMATION.md`.

Why:

These changes make the application more robust in multi-station scenarios, centralize venda persistence for easier testing, and provide the tools and documentation necessary to move from SQLite to SQL Server in a controlled way.

Checklist for reviewers:

- [ ] Code compiles and tests pass in CI
- [ ] Verify LocalSync behavior in multi-station environment
- [ ] Validate PDV reprint fallback and audit records
- [ ] Review DB migration plan and tooling

Notes:

CI workflow was added; after merge, GitHub Actions will run builds and tests.
