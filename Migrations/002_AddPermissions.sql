-- PrimoAutoEletrica controlled migration reference.
-- Access-control runtime details live in DatabaseService.AccessControl.cs.

CREATE TABLE IF NOT EXISTS PerfisAcesso
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL UNIQUE,
    Descricao TEXT,
    Ativo INTEGER NOT NULL DEFAULT 1,
    DataCadastro TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS Permissoes
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Codigo TEXT NOT NULL UNIQUE,
    Modulo TEXT NOT NULL,
    Acao TEXT NOT NULL,
    Descricao TEXT
);

CREATE TABLE IF NOT EXISTS PerfilPermissoes
(
    PerfilId INTEGER NOT NULL,
    PermissaoId INTEGER NOT NULL,
    Permitido INTEGER NOT NULL DEFAULT 1,
    PRIMARY KEY (PerfilId, PermissaoId)
);

INSERT OR IGNORE INTO SchemaVersion
(
    Version,
    AppliedAt,
    Description
)
VALUES
(
    '002',
    datetime('now'),
    'Access profiles and permission matrix'
);
