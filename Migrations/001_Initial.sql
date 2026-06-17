-- PrimoAutoEletrica controlled migration reference.
-- Runtime migrations are applied by DatabaseService.Migrations.cs.

CREATE TABLE IF NOT EXISTS SchemaVersion
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Version TEXT NOT NULL UNIQUE,
    AppliedAt TEXT NOT NULL,
    Description TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS SchemaMigrations
(
    Id TEXT PRIMARY KEY,
    Descricao TEXT NOT NULL,
    AplicadaEm TEXT NOT NULL,
    VersaoAplicacao TEXT,
    Maquina TEXT
);

INSERT OR IGNORE INTO SchemaVersion
(
    Version,
    AppliedAt,
    Description
)
VALUES
(
    '001',
    datetime('now'),
    'Initial schema control tables'
);
