# PRIMOX WORKSHOP — CICLO C1
# ESPECIFICAÇÃO DE SCHEMA & MODELO DE DADOS (C1 DATA MODEL)
**Data:** 2026-09-25  
**Ciclo:** C1 — Operational Intelligence + Tools + Purchasing + Knowledge + Assist Foundation  
**Banco de Dados:** SQLite (`primoauto_operacional.db`, `user_version = 1`, CentsV1)  
**Status:** SPECIFICATION APPROVED  

---

## 1. PRINCÍPIOS DE ENGENHARIA DE DADOS

1. **Moeda Estrita (CentsV1):** Todo campo que armazena valor financeiro novo (custo de aquisição de ferramenta, custo de reparo/aferição, custos unitários e totais de compra) é obrigatoriamente persistido como `INTEGER` representando centavos, serializado e desserializado via `MoneyIO.GravarMoeda` / `MoneyIO.LerMoeda`.
2. **Campos Não Monetários:** Quantidades físicas, estoque e horas continuam sendo números reais (`REAL`) ou inteiros de contagem.
3. **Concorrência Otimista:** Tabelas de recursos compartilhados (`Tools`, `PurchaseRequests`) incluem coluna `RowVersion INTEGER NOT NULL DEFAULT 1` para proteção atômica contra alterações simultâneas.
4. **Integridade Referencial:** Chaves estrangeiras com `FOREIGN KEY` ativas no SQLite.
5. **Índices Estruturados:** Criados para apoiar as consultas reais de interface e relatórios (sem índices redundantes).

---

## 2. DDLS DAS NOVAS TABELAS

### 2.1 Tabela `Tools`
```sql
CREATE TABLE IF NOT EXISTS Tools (
    ToolId TEXT PRIMARY KEY,
    Code TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    Category TEXT NOT NULL,
    Brand TEXT,
    Model TEXT,
    SerialNumber TEXT,
    PatrimonyNumber TEXT,
    Description TEXT,
    PhotoPath TEXT,
    LocationName TEXT NOT NULL DEFAULT 'Geral',
    CurrentResponsibleUserId INTEGER,
    CurrentResponsibleUserName TEXT,
    Status TEXT NOT NULL DEFAULT 'AVAILABLE',
    PurchaseDate TEXT,
    PurchaseValueCents INTEGER NOT NULL DEFAULT 0,
    WarrantyExpiration TEXT,
    LastMaintenanceDate TEXT,
    NextMaintenanceDate TEXT,
    Notes TEXT,
    RowVersion INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (CurrentResponsibleUserId) REFERENCES Funcionarios(Id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS IX_Tools_Status ON Tools (Status);
CREATE INDEX IF NOT EXISTS IX_Tools_Category ON Tools (Category);
CREATE INDEX IF NOT EXISTS IX_Tools_Code ON Tools (Code);
CREATE INDEX IF NOT EXISTS IX_Tools_Responsible ON Tools (CurrentResponsibleUserId);
```

### 2.2 Tabela `ToolCheckouts`
```sql
CREATE TABLE IF NOT EXISTS ToolCheckouts (
    CheckoutId TEXT PRIMARY KEY,
    ToolId TEXT NOT NULL,
    UserId INTEGER NOT NULL,
    UserName TEXT NOT NULL,
    WorkOrderId TEXT,
    WorkOrderNumber TEXT,
    VehiclePlate TEXT,
    CheckoutDate TEXT NOT NULL,
    ExpectedReturnDate TEXT,
    ReturnDate TEXT,
    ReturnedByUserId INTEGER,
    ReturnCondition TEXT,
    CheckoutNotes TEXT,
    ReturnNotes TEXT,
    Status TEXT NOT NULL DEFAULT 'OPEN',
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (ToolId) REFERENCES Tools(ToolId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Funcionarios(Id)
);

CREATE INDEX IF NOT EXISTS IX_ToolCheckouts_ToolId ON ToolCheckouts (ToolId);
CREATE INDEX IF NOT EXISTS IX_ToolCheckouts_Status ON ToolCheckouts (Status);
CREATE INDEX IF NOT EXISTS IX_ToolCheckouts_UserId ON ToolCheckouts (UserId);
```

### 2.3 Tabela `ToolMaintenances`
```sql
CREATE TABLE IF NOT EXISTS ToolMaintenances (
    MaintenanceId TEXT PRIMARY KEY,
    ToolId TEXT NOT NULL,
    MaintenanceType TEXT NOT NULL,
    Description TEXT NOT NULL,
    CostCents INTEGER NOT NULL DEFAULT 0,
    Provider TEXT,
    StartDate TEXT NOT NULL,
    CompletionDate TEXT,
    PerformedBy TEXT,
    Status TEXT NOT NULL DEFAULT 'COMPLETED',
    Notes TEXT,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (ToolId) REFERENCES Tools(ToolId) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_ToolMaintenances_ToolId ON ToolMaintenances (ToolId);
```

### 2.4 Tabela `PurchaseRequests`
```sql
CREATE TABLE IF NOT EXISTS PurchaseRequests (
    PurchaseRequestId TEXT PRIMARY KEY,
    Number TEXT NOT NULL UNIQUE,
    RequestedByUserId INTEGER NOT NULL,
    RequestedByUserName TEXT NOT NULL,
    RequestedAt TEXT NOT NULL,
    Priority TEXT NOT NULL DEFAULT 'NORMAL',
    Reason TEXT NOT NULL DEFAULT 'LOW_STOCK',
    Status TEXT NOT NULL DEFAULT 'REQUESTED',
    ApprovedByUserId INTEGER,
    ApprovedByUserName TEXT,
    ApprovedAt TEXT,
    CancelledByUserId INTEGER,
    CancelledAt TEXT,
    CancellationReason TEXT,
    SupplierId INTEGER,
    SupplierName TEXT,
    TotalEstimatedCostCents INTEGER NOT NULL DEFAULT 0,
    TotalActualCostCents INTEGER NOT NULL DEFAULT 0,
    FiscalDocumentNumber TEXT,
    Notes TEXT,
    RowVersion INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (RequestedByUserId) REFERENCES Funcionarios(Id),
    FOREIGN KEY (SupplierId) REFERENCES Fornecedores(Id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS IX_PurchaseRequests_Status ON PurchaseRequests (Status);
CREATE INDEX IF NOT EXISTS IX_PurchaseRequests_Priority ON PurchaseRequests (Priority);
CREATE INDEX IF NOT EXISTS IX_PurchaseRequests_RequestedAt ON PurchaseRequests (RequestedAt);
```

### 2.5 Tabela `PurchaseRequestItems`
```sql
CREATE TABLE IF NOT EXISTS PurchaseRequestItems (
    ItemId TEXT PRIMARY KEY,
    PurchaseRequestId TEXT NOT NULL,
    ProductId INTEGER NOT NULL,
    ProductCode TEXT,
    ProductName TEXT NOT NULL,
    RequestedQuantity REAL NOT NULL DEFAULT 1,
    SuggestedQuantity REAL NOT NULL DEFAULT 0,
    CurrentStock REAL NOT NULL DEFAULT 0,
    MinimumStock REAL NOT NULL DEFAULT 0,
    IdealStock REAL NOT NULL DEFAULT 0,
    EstimatedUnitCostCents INTEGER NOT NULL DEFAULT 0,
    ActualUnitCostCents INTEGER NOT NULL DEFAULT 0,
    ReceivedQuantity REAL NOT NULL DEFAULT 0,
    Priority TEXT NOT NULL DEFAULT 'NORMAL',
    Reason TEXT,
    Notes TEXT,
    Status TEXT NOT NULL DEFAULT 'PENDING',
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (PurchaseRequestId) REFERENCES PurchaseRequests(PurchaseRequestId) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Produtos(Id)
);

CREATE INDEX IF NOT EXISTS IX_PurchaseRequestItems_ReqId ON PurchaseRequestItems (PurchaseRequestId);
CREATE INDEX IF NOT EXISTS IX_PurchaseRequestItems_ProductId ON PurchaseRequestItems (ProductId);
```

### 2.6 Tabela `TechnicalKnowledgeEntries`
```sql
CREATE TABLE IF NOT EXISTS TechnicalKnowledgeEntries (
    KnowledgeId TEXT PRIMARY KEY,
    Code TEXT NOT NULL UNIQUE,
    Title TEXT NOT NULL,
    System TEXT NOT NULL,
    VehicleCategory TEXT NOT NULL DEFAULT 'Universal',
    Voltage TEXT NOT NULL DEFAULT '12V',
    Symptom TEXT NOT NULL,
    PossibleCauses TEXT NOT NULL,
    DiagnosticProcedure TEXT NOT NULL,
    RecommendedMeasurements TEXT,
    Solution TEXT NOT NULL,
    Warnings TEXT,
    Tags TEXT,
    SourceType TEXT NOT NULL DEFAULT 'FIELD_EXPERIENCE',
    CreatedByUserId INTEGER NOT NULL,
    CreatedByUserName TEXT NOT NULL,
    Status TEXT NOT NULL DEFAULT 'PUBLISHED',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (CreatedByUserId) REFERENCES Funcionarios(Id)
);

CREATE INDEX IF NOT EXISTS IX_Knowledge_System ON TechnicalKnowledgeEntries (System);
CREATE INDEX IF NOT EXISTS IX_Knowledge_Voltage ON TechnicalKnowledgeEntries (Voltage);
CREATE INDEX IF NOT EXISTS IX_Knowledge_Status ON TechnicalKnowledgeEntries (Status);
```

### 2.7 Tabela `DiagnosticCases`
```sql
CREATE TABLE IF NOT EXISTS DiagnosticCases (
    CaseId TEXT PRIMARY KEY,
    Code TEXT NOT NULL UNIQUE,
    Title TEXT NOT NULL,
    VehicleId TEXT,
    VehicleModel TEXT NOT NULL,
    VehiclePlate TEXT,
    WorkOrderId TEXT,
    WorkOrderNumber TEXT,
    TechnicianId INTEGER,
    TechnicianName TEXT,
    System TEXT NOT NULL,
    Voltage TEXT NOT NULL DEFAULT '12V',
    DtcCodes TEXT,
    Symptom TEXT NOT NULL,
    Measurements TEXT,
    InitialHypotheses TEXT,
    ConfirmedCause TEXT NOT NULL,
    Solution TEXT NOT NULL,
    PartsUsed TEXT,
    TestResult TEXT,
    FinalResult TEXT NOT NULL DEFAULT 'RESOLVED',
    KnowledgeEntryId TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (TechnicianId) REFERENCES Funcionarios(Id) ON DELETE SET NULL,
    FOREIGN KEY (KnowledgeEntryId) REFERENCES TechnicalKnowledgeEntries(KnowledgeId) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS IX_DiagnosticCases_VehicleId ON DiagnosticCases (VehicleId);
CREATE INDEX IF NOT EXISTS IX_DiagnosticCases_WorkOrderId ON DiagnosticCases (WorkOrderId);
CREATE INDEX IF NOT EXISTS IX_DiagnosticCases_System ON DiagnosticCases (System);
```
