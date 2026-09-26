using System;
using System.Data.Common;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public partial class DatabaseService
    {
        public void InitializeC1OperationalSchema(DbConnection connection)
        {
            using var command = connection.CreateCommand();

            // 1. PRIMOX TOOLS
            command.CommandText = @"
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

                -- 2. PRIMOX PURCHASING
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
                    SupplierId TEXT,
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

                CREATE TABLE IF NOT EXISTS PurchaseRequestItems (
                    ItemId TEXT PRIMARY KEY,
                    PurchaseRequestId TEXT NOT NULL,
                    ProductId TEXT,
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

                -- 3. PRIMOX KNOWLEDGE
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
            ";

            command.ExecuteNonQuery();

            SeedC1Permissions(connection);
            SeedC1Tools(connection);
            SeedC1Knowledge(connection);
        }

        private void SeedC1Permissions(DbConnection connection)
        {
            var c1Perms = new[]
            {
                new PermissaoSeed("Ver Ferramentas", "Visualizar inventario e status de ferramentas", "Ferramentas", "Visualizar", "FERRAMENTAS_VER", true, 80),
                new PermissaoSeed("Cadastrar Ferramenta", "Cadastrar nova ferramenta no patrimonio", "Ferramentas", "Criar", "FERRAMENTAS_CRIAR", false, 81),
                new PermissaoSeed("Editar Ferramenta", "Editar ferramenta, localizacao e dados tecnicos", "Ferramentas", "Editar", "FERRAMENTAS_EDITAR", false, 82),
                new PermissaoSeed("Retirar Ferramenta", "Registrar saida de ferramenta para servico ou socorro", "Ferramentas", "Executar", "FERRAMENTAS_RETIRAR", false, 83),
                new PermissaoSeed("Devolver Ferramenta", "Registrar devolucao de ferramenta e condicao", "Ferramentas", "Executar", "FERRAMENTAS_DEVOLVER", false, 84),
                new PermissaoSeed("Manutencao de Ferramenta", "Registrar calibracao, revisao ou avaria", "Ferramentas", "Executar", "FERRAMENTAS_MANUTENCAO", false, 85),
                new PermissaoSeed("Excluir Ferramenta", "Excluir ou baixar ferramenta do patrimonio", "Ferramentas", "Excluir", "FERRAMENTAS_EXCLUIR", false, 86),

                new PermissaoSeed("Ver Compras", "Visualizar necessidades e pedidos de compra", "Compras", "Visualizar", "COMPRAS_VER", true, 90),
                new PermissaoSeed("Solicitar Compra", "Criar requisicao de compra", "Compras", "Criar", "COMPRAS_SOLICITAR", false, 91),
                new PermissaoSeed("Aprovar Compra", "Aprovar pedidos de compra", "Compras", "Aprovar", "COMPRAS_APROVAR", false, 92),
                new PermissaoSeed("Formalizar Pedido Compra", "Emitir pedido ao fornecedor", "Compras", "Executar", "COMPRAS_PEDIR", false, 93),
                new PermissaoSeed("Receber Compra", "Conferir e dar entrada fisica de compra no estoque", "Compras", "Executar", "COMPRAS_RECEBER", false, 94),
                new PermissaoSeed("Cancelar Compra", "Cancelar solicitacao ou pedido de compra", "Compras", "Cancelar", "COMPRAS_CANCELAR", false, 95),

                new PermissaoSeed("Ver Base Tecnica", "Consultar base tecnica e casos de diagnostico", "Conhecimento", "Visualizar", "CONHECIMENTO_VER", true, 100),
                new PermissaoSeed("Criar Artigo Tecnico", "Criar procedimento ou registrar caso tecnico", "Conhecimento", "Criar", "CONHECIMENTO_CRIAR", false, 101),
                new PermissaoSeed("Editar Conhecimento", "Editar boletins e procedimentos tecnicos", "Conhecimento", "Editar", "CONHECIMENTO_EDITAR", false, 102),
                new PermissaoSeed("Publicar Conhecimento", "Aprovar e publicar na base oficial", "Conhecimento", "Publicar", "CONHECIMENTO_PUBLICAR", false, 103),
                new PermissaoSeed("Arquivar Conhecimento", "Arquivar artigo descontinuado", "Conhecimento", "Arquivar", "CONHECIMENTO_ARQUIVAR", false, 104),

                new PermissaoSeed("Usar PRIMOX Assist", "Consultar assistente tecnico consultivo", "Assist", "Executar", "ASSIST_UTILIZAR", true, 110),
                new PermissaoSeed("Configurar PRIMOX Assist", "Administrar parametros do assistente", "Assist", "Configurar", "ASSIST_CONFIGURAR", false, 111)
            };

            foreach (var perm in c1Perms)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO Permissoes (Nome, Descricao, Modulo, Acao, Codigo, Ativo, DataCriacao, Essencial, OrdemExibicao)
                    VALUES (@Nome, @Descricao, @Modulo, @Acao, @Codigo, 1, @DataCriacao, @Essencial, @OrdemExibicao);
                ";
                cmd.Parameters.Add(CreateParameter(cmd, "@Nome", perm.Nome));
                cmd.Parameters.Add(CreateParameter(cmd, "@Descricao", perm.Descricao));
                cmd.Parameters.Add(CreateParameter(cmd, "@Modulo", perm.Modulo));
                cmd.Parameters.Add(CreateParameter(cmd, "@Acao", perm.Acao));
                cmd.Parameters.Add(CreateParameter(cmd, "@Codigo", perm.Codigo));
                cmd.Parameters.Add(CreateParameter(cmd, "@DataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                cmd.Parameters.Add(CreateParameter(cmd, "@Essencial", perm.Essencial ? 1 : 0));
                cmd.Parameters.Add(CreateParameter(cmd, "@OrdemExibicao", perm.Ordem));
                cmd.ExecuteNonQuery();
            }

            // Conceder ao perfil Administrador
            using var adminCmd = connection.CreateCommand();
            adminCmd.CommandText = @"
                INSERT OR IGNORE INTO PerfilPermissoes (PerfilId, PermissaoId, Concedida, DataConcessao, ConcedidaPor, Ativa)
                SELECT p.Id, perm.Id, 1, @Now, 'Sistema', 1
                FROM PerfisAcesso p
                CROSS JOIN Permissoes perm
                WHERE lower(p.Nome) = 'administrador'
                  AND perm.Codigo IN (
                      'FERRAMENTAS_VER', 'FERRAMENTAS_CRIAR', 'FERRAMENTAS_EDITAR', 'FERRAMENTAS_RETIRAR', 'FERRAMENTAS_DEVOLVER', 'FERRAMENTAS_MANUTENCAO', 'FERRAMENTAS_EXCLUIR',
                      'COMPRAS_VER', 'COMPRAS_SOLICITAR', 'COMPRAS_APROVAR', 'COMPRAS_PEDIR', 'COMPRAS_RECEBER', 'COMPRAS_CANCELAR',
                      'CONHECIMENTO_VER', 'CONHECIMENTO_CRIAR', 'CONHECIMENTO_EDITAR', 'CONHECIMENTO_PUBLICAR', 'CONHECIMENTO_ARQUIVAR',
                      'ASSIST_UTILIZAR', 'ASSIST_CONFIGURAR'
                  );
            ";
            adminCmd.Parameters.Add(CreateParameter(adminCmd, "@Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            adminCmd.ExecuteNonQuery();

            // Conceder permissoes operacionais aos perfis Gerente e Mecanico
            using var opCmd = connection.CreateCommand();
            opCmd.CommandText = @"
                INSERT OR IGNORE INTO PerfilPermissoes (PerfilId, PermissaoId, Concedida, DataConcessao, ConcedidaPor, Ativa)
                SELECT p.Id, perm.Id, 1, @Now, 'Sistema', 1
                FROM PerfisAcesso p
                CROSS JOIN Permissoes perm
                WHERE (lower(p.Nome) = 'gerente' OR lower(p.Nome) = 'mecanico' OR lower(p.Nome) = 'mecânico')
                  AND perm.Codigo IN (
                      'FERRAMENTAS_VER', 'FERRAMENTAS_RETIRAR', 'FERRAMENTAS_DEVOLVER',
                      'COMPRAS_VER', 'COMPRAS_SOLICITAR',
                      'CONHECIMENTO_VER', 'CONHECIMENTO_CRIAR',
                      'ASSIST_UTILIZAR'
                  );
            ";
            opCmd.Parameters.Add(CreateParameter(opCmd, "@Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            opCmd.ExecuteNonQuery();
        }

        private void SeedC1Tools(DbConnection connection)
        {
            using var countCmd = connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM Tools;";
            var count = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0);
            if (count > 0)
            {
                return;
            }

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var tools = new (string Code, string Name, string Cat, string Brand, string Model, string Loc, long ValCents)[]
            {
                ("F001", "Multímetro Digital Automotivo True RMS", "Instrumentação", "Fluke", "179 Auto", "Bancada 01", 185000),
                ("F002", "Alicate Amperimétrico DC 600A", "Instrumentação", "Minipa", "ET-3367C", "Bancada 01", 62000),
                ("F003", "Scanner Diagnóstico Pesado / Leve", "Diagnóstico", "Raven", "Scanner III", "Sala Diagnóstico", 1450000),
                ("F004", "Osciloscópio Automotivo 4 Canais USB", "Diagnóstico", "Hantek", "1008C Auto", "Bancada 02", 115000),
                ("F005", "Fonte Estabilizada de Bancada 14V/60A", "Auto Elétrica", "Usina", "Spark 60A", "Bancada Testes", 78000),
                ("F006", "Testador Digital de Bateria e Condutância", "Carga e Bateria", "Midtronics", "MDX-300", "Bancada 02", 230000)
            };

            foreach (var t in tools)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Tools (ToolId, Code, Name, Category, Brand, Model, LocationName, Status, PurchaseValueCents, CreatedAt, UpdatedAt)
                    VALUES (@Id, @Code, @Name, @Cat, @Brand, @Model, @Loc, 'AVAILABLE', @Val, @CreatedAt, @UpdatedAt);
                ";
                cmd.Parameters.Add(CreateParameter(cmd, "@Id", Guid.NewGuid().ToString()));
                cmd.Parameters.Add(CreateParameter(cmd, "@Code", t.Code));
                cmd.Parameters.Add(CreateParameter(cmd, "@Name", t.Name));
                cmd.Parameters.Add(CreateParameter(cmd, "@Cat", t.Cat));
                cmd.Parameters.Add(CreateParameter(cmd, "@Brand", t.Brand));
                cmd.Parameters.Add(CreateParameter(cmd, "@Model", t.Model));
                cmd.Parameters.Add(CreateParameter(cmd, "@Loc", t.Loc));
                cmd.Parameters.Add(CreateParameter(cmd, "@Val", t.ValCents));
                cmd.Parameters.Add(CreateParameter(cmd, "@CreatedAt", now));
                cmd.Parameters.Add(CreateParameter(cmd, "@UpdatedAt", now));
                cmd.ExecuteNonQuery();
            }
        }

        private void SeedC1Knowledge(DbConnection connection)
        {
            using var countCmd = connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM TechnicalKnowledgeEntries;";
            var count = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0);
            if (count > 0)
            {
                return;
            }

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Obter primeiro funcionario para autor
            using var userCmd = connection.CreateCommand();
            userCmd.CommandText = "SELECT Id, Nome FROM Funcionarios ORDER BY Id LIMIT 1;";
            int authorId = 1;
            string authorName = "Técnico Especialista PRIMOX";
            using (var reader = userCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    authorId = reader.GetInt32(0);
                    authorName = reader.GetString(1);
                }
            }

            var entryId1 = Guid.NewGuid().ToString();
            using var cmd1 = connection.CreateCommand();
            cmd1.CommandText = @"
                INSERT INTO TechnicalKnowledgeEntries (
                    KnowledgeId, Code, Title, System, VehicleCategory, Voltage,
                    Symptom, PossibleCauses, DiagnosticProcedure, RecommendedMeasurements,
                    Solution, Warnings, Tags, SourceType, CreatedByUserId, CreatedByUserName,
                    Status, CreatedAt, UpdatedAt
                ) VALUES (
                    @Id, 'KB-ELET-001', 'Queda de Tensão Crítica no Circuito de Partida 24V (Linha Pesada)',
                    'Partida e Arranque', 'Linha Pesada', '24V',
                    'Motor de arranque gira pesado/lento mesmo com baterias novas ou recém-carregadas.',
                    'Oxidação no terminal do cabo positivo B+; aterramento de chassi frouxo; relé auxiliar de partida com resistência de contato interna.',
                    'Executar roteiro D01 (Balanço de Carga) e D02 (Queda de Tensão sob Carga). Conectar ponta negativa do multímetro no polo negativo da bateria e positiva no bloco do motor durante a partida.',
                    'Queda máxima no positivo <= 0,5V em 24V; Queda máxima no aterramento <= 0,3V em 24V. Quedas acima de 1,0V indicam perda ôhmica severa.',
                    'Limpar bornes, substituir terminal oxidado por terminal estanhado reforçado 50mm²/70mm² e reapertar com torque de 12 Nm.',
                    'Risco de superaquecimento e arco elétrico se o terminal estiver frouxo.',
                    '24v,partida,arranque,queda-tensao,actros,scania,d01,d02',
                    'FIELD_EXPERIENCE', @AuthorId, @AuthorName, 'PUBLISHED', @Now, @Now
                );
            ";
            cmd1.Parameters.Add(CreateParameter(cmd1, "@Id", entryId1));
            cmd1.Parameters.Add(CreateParameter(cmd1, "@AuthorId", authorId));
            cmd1.Parameters.Add(CreateParameter(cmd1, "@AuthorName", authorName));
            cmd1.Parameters.Add(CreateParameter(cmd1, "@Now", now));
            cmd1.ExecuteNonQuery();

            var entryId2 = Guid.NewGuid().ToString();
            using var cmd2 = connection.CreateCommand();
            cmd2.CommandText = @"
                INSERT INTO TechnicalKnowledgeEntries (
                    KnowledgeId, Code, Title, System, VehicleCategory, Voltage,
                    Symptom, PossibleCauses, DiagnosticProcedure, RecommendedMeasurements,
                    Solution, Warnings, Tags, SourceType, CreatedByUserId, CreatedByUserName,
                    Status, CreatedAt, UpdatedAt
                ) VALUES (
                    @Id, 'KB-CAN-002', 'Falha de Comunicação na Rede CAN Powertrain (DTC U0100 / Perda de Painel)',
                    'Rede CAN e Comunicação', 'Universal', 'Bivolt',
                    'Luzes de alerta acesas no painel, ponteiros oscilando e falha de comunicação com módulo de injeção/câmbio.',
                    'Resistor de terminação de 120 ohms aberto; curto-circuito entre CAN High e CAN Low; chicote mastigado na travessa.',
                    'Desconectar bateria, esperar 5 minutos e medir resistência entre pinos 6 e 14 do conector OBD2.',
                    'Resistência normal esperada: 60 Ohms (dois resistores de 120 ohms em paralelo). Se medir 120 Ohms, um módulo terminal está desconectado. Se medir < 50 Ohms, há curto parcial.',
                    'Inspecionar chicote na articulação e substituir módulo terminal defeituoso.',
                    'Nunca realizar medição com ignição ligada sob pena de leitura incorreta.',
                    'can,rede,dtc,u0100,60ohms,terminacao,obd2',
                    'OEM_MANUAL', @AuthorId, @AuthorName, 'PUBLISHED', @Now, @Now
                );
            ";
            cmd2.Parameters.Add(CreateParameter(cmd2, "@Id", entryId2));
            cmd2.Parameters.Add(CreateParameter(cmd2, "@AuthorId", authorId));
            cmd2.Parameters.Add(CreateParameter(cmd2, "@AuthorName", authorName));
            cmd2.Parameters.Add(CreateParameter(cmd2, "@Now", now));
            cmd2.ExecuteNonQuery();

            // Seed de Caso Real de Diagnostico
            using var caseCmd = connection.CreateCommand();
            caseCmd.CommandText = @"
                INSERT INTO DiagnosticCases (
                    CaseId, Code, Title, VehicleModel, VehiclePlate, System, Voltage, DtcCodes,
                    Symptom, Measurements, InitialHypotheses, ConfirmedCause, Solution,
                    PartsUsed, TestResult, FinalResult, KnowledgeEntryId, CreatedAt, UpdatedAt
                ) VALUES (
                    @Id, 'CASO-20260925-001', 'Mercedes-Benz Actros 2651 - Falha de Carga Intermitente e Perda de Potência',
                    'Mercedes-Benz Actros 2651 6x4', 'PRM-2026', 'Alimentação e Carga', '24V', 'P0562, U0100',
                    'Caminhão perde aceleração em rotação de cruzeiro e desliga tacógrafo intermitentemente.',
                    'Tensão na bateria: 24,1V com motor ligado a 1500 RPM. Tensão no B+ do alternador: 28,3V. Queda medida no chicote positivo: 4,2V.',
                    'Alternador com ponte retificadora queimada ou cabo de carga com ruptura interna.',
                    'Cabo de alimentação de 50mm² rompido internamente por vibração próximo ao suporte do motor.',
                    'Confecção de novo cabo de 50mm² com terminais estanhados e conduíte antichama.',
                    '1 Cabo 50mm² 2,5m; 2 Terminais Olha 50x10; Conduíte corrugado 3m.',
                    'Tensão na bateria subiu para 28,1V estável sob 40A de carga elétrica. Queda residual de apenas 0,15V.',
                    'RESOLVED', @KbId, @Now, @Now
                );
            ";
            caseCmd.Parameters.Add(CreateParameter(caseCmd, "@Id", Guid.NewGuid().ToString()));
            caseCmd.Parameters.Add(CreateParameter(caseCmd, "@KbId", entryId1));
            caseCmd.Parameters.Add(CreateParameter(caseCmd, "@Now", now));
            caseCmd.ExecuteNonQuery();
        }
    }
}
