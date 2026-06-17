-- PrimoAutoEletrica controlled migration reference.
-- Current workflow uses OrdensServico status/timeline fields and Agendamentos.

CREATE INDEX IF NOT EXISTS IX_OrdensServico_Status_DataEntrada
ON OrdensServico (Status, DataEntrada);

CREATE INDEX IF NOT EXISTS IX_Agendamentos_Data_Status
ON Agendamentos (DataAgendamento, Status);

INSERT OR IGNORE INTO SchemaVersion
(
    Version,
    AppliedAt,
    Description
)
VALUES
(
    '003',
    datetime('now'),
    'Workshop planning indexes for OS and scheduling views'
);
