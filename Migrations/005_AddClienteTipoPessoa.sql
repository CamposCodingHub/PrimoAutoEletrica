-- Adds explicit PF/PJ classification for customer registrations.
-- Runtime application is controlled by DatabaseService.Migrations.cs version 202606110002.

ALTER TABLE Clientes ADD COLUMN TipoPessoa TEXT NOT NULL DEFAULT 'Fisica';

UPDATE Clientes
SET TipoPessoa = CASE
    WHEN length(replace(replace(replace(replace(COALESCE(CPF, ''), '.', ''), '/', ''), '-', ''), ' ', '')) > 11 THEN 'Juridica'
    ELSE 'Fisica'
END
WHERE trim(COALESCE(TipoPessoa, '')) = '';

CREATE INDEX IF NOT EXISTS IX_Clientes_TipoPessoa_Status
ON Clientes (TipoPessoa, Ativo, Nome);
