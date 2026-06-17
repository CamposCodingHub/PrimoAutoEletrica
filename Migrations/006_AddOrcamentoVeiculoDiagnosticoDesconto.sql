-- Migração 006: Orçamentos com veículo, diagnóstico técnico e desconto percentual.
-- O runtime também aplica esta evolução por DatabaseService.Migrations.cs e OrcamentoDatabaseService.

ALTER TABLE Orcamentos ADD COLUMN VeiculoId TEXT;
ALTER TABLE Orcamentos ADD COLUMN Diagnostico TEXT;
ALTER TABLE Orcamentos ADD COLUMN DescontoTipo TEXT NOT NULL DEFAULT 'Valor';
ALTER TABLE Orcamentos ADD COLUMN DescontoPercentual REAL NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS IX_Orcamentos_Cliente_Veiculo_Status
ON Orcamentos (ClienteId, VeiculoId, Status);
