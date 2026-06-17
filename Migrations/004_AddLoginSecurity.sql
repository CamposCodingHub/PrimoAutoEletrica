-- 004_AddLoginSecurity.sql
-- Politica de troca obrigatoria de senha para acessos temporarios.
-- O runtime aplica a migracao equivalente em DatabaseService.Migrations.cs.

ALTER TABLE Funcionarios
ADD COLUMN ExigirTrocaSenha INTEGER NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS IX_Funcionarios_ExigirTrocaSenha
ON Funcionarios (ExigirTrocaSenha, Ativo);
