-- Migração 008: Integração de contas a pagar com importação NF-e.
-- O runtime também aplica esta evolução por FinanceiroDatabaseService.

ALTER TABLE ContasPagar ADD COLUMN Origem TEXT;
ALTER TABLE ContasPagar ADD COLUMN ReferenciaExterna TEXT;

CREATE UNIQUE INDEX IF NOT EXISTS IX_ContasPagar_Integracao
ON ContasPagar (Origem, ReferenciaExterna)
WHERE Origem IS NOT NULL AND ReferenciaExterna IS NOT NULL;
