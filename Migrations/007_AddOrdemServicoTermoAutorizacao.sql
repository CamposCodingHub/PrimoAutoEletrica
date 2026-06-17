-- Migração 007: Termo de autorização explícito para ordens de serviço.
-- O runtime também aplica esta evolução por DatabaseService.Migrations.cs.

ALTER TABLE OrdensServico ADD COLUMN TermoAutorizacao TEXT;
