# PRIMOX WORKSHOP — CICLO C1
# ARQUITETURA DO MÓDULO PRIMOX TOOLS (C1.1)
**Data:** 2026-09-25  
**Módulo:** PRIMOX Tools — Controle de Ferramentas e Patrimônio Operacional  
**Status Arquitetural:** IMPLEMENTATION SPEC  

---

## 1. VISÃO GERAL & PRINCÍPIO DE DOMÍNIO

O módulo **PRIMOX Tools** não é um mero cadastro de itens. Ele representa o controle do **patrimônio operacional e técnico da oficina**.
Em uma oficina mecânica e de auto elétrica de alta performance (linha leve e pesada), o extravio, falta de calibração ou quebra de ferramentas especializadas (scanners, osciloscópios, alicates amperimétricos, multímetros automotivos, fontes de bancada, testadores de bateria e chicote) acarreta parada de linha, diagnóstico errôneo e prejuízo financeiro.

### Responsabilidade Operacional:
1. **Identidade e Patrimônio:** Rastreamento unívoco de cada ferramenta física (código, patrimônio, marca, número de série).
2. **Custos e Investimento:** Aquisição e reparos registrados obrigatoriamente no padrão **CentsV1** (`INTEGER cents` via `MoneyIO`).
3. **Custódia em Tempo Real:** Quem retirou, quando, com qual motivo e previsão de devolução.
4. **Ciclo de Manutenção & Calibração:** Preventiva, corretiva, aferição metrológica e registro de avaria/descarte.
5. **Prevenção de Race Conditions:** Controle de concorrência otimista (`RowVersion`) para evitar dupla retirada concorrente.
6. **Conectividade de Identificação:** Estrutura preparada para identificação rápida via QR Code padrão (`PRIMOX://TOOL/{Code}`).

---

## 2. MODELO DE DOMÍNIO & ENTIDADES

### 2.1 Entidade `Tool` (Ferramenta)
- `ToolId` (`Guid`): Identificador único global.
- `Code` (`string`): Código operacional da ferramenta (ex: `F001`, `F002`, `OSC-01`).
- `Name` (`string`): Nome da ferramenta (ex: `Osciloscópio Automotivo 4 Canais`).
- `Category` (`string`): Categoria operacional (`Diagnóstico`, `Auto Elétrica`, `Mecânica`, `Carga e Bateria`, `Instrumentação`, `Pneumática`, `Especial`).
- `Brand` (`string?`): Fabricante / Marca (ex: `Hantek`, `Raven`, `Fluke`, `Bosch`).
- `Model` (`string?`): Modelo (ex: `2D82 Auto`).
- `SerialNumber` (`string?`): Número de série do fabricante.
- `PatrimonyNumber` (`string?`): Número do patrimônio interno da oficina.
- `Description` (`string?`): Descrição e características técnicas.
- `PhotoPath` (`string?`): Caminho relativo ou URL da foto da ferramenta.
- `LocationName` (`string`): Localização na oficina (ex: `Bancada Diagnóstico`, `Armário 02`, `Gaveta 04`).
- `CurrentResponsibleUserId` (`int?`): ID do funcionário atualmente em posse (`Funcionarios.Id`).
- `CurrentResponsibleUserName` (`string?`): Snapshot do nome do responsável.
- `Status` (`ToolStatus`): Enumeração estrita de status operacional.
- `PurchaseDate` (`DateTime?`): Data da compra.
- `PurchaseValueCents` (`long`): Valor de compra em centavos inteiros (CentsV1).
- `WarrantyExpiration` (`DateTime?`): Vencimento da garantia.
- `LastMaintenanceDate` (`DateTime?`): Data da última manutenção/calibração.
- `NextMaintenanceDate` (`DateTime?`): Data da próxima manutenção programada.
- `Notes` (`string?`): Observações gerais.
- `RowVersion` (`int`): Versão do registro para concorrência otimista.
- `CreatedAt` (`DateTime`): Timestamp de criação.
- `UpdatedAt` (`DateTime`): Timestamp da última alteração.

### 2.2 Enumeração `ToolStatus`
Centralizada no modelo de domínio, sem strings soltas:
- `AVAILABLE` (0): Disponível para retirada na oficina.
- `IN_USE` (1): Em uso interno na oficina por um técnico.
- `BORROWED` (2): Emprestada externamente ou em socorro/campo.
- `MAINTENANCE` (3): Em processo de manutenção, calibração ou revisão.
- `DAMAGED` (4): Danificada, aguardando laudo ou conserto.
- `LOST` (5): Extraviada / não localizada.
- `RETIRED` (6): Aposentada / descartada do inventário ativo.

### 2.3 Entidade `ToolCheckout` (Retirada e Devolução)
- `CheckoutId` (`Guid`): Identificador único da movimentação.
- `ToolId` (`Guid`): Referência da ferramenta movimentada.
- `UserId` (`int`): Funcionário que realizou a retirada (`Funcionarios.Id`).
- `UserName` (`string`): Snapshot do nome do funcionário.
- `WorkOrderId` (`Guid?`): Vínculo opcional com a Ordem de Serviço em andamento.
- `WorkOrderNumber` (`string?`): Número da OS vinculada.
- `VehiclePlate` (`string?`): Placa do veículo atendido (quando aplicável).
- `CheckoutDate` (`DateTime`): Data e hora da retirada.
- `ExpectedReturnDate` (`DateTime?`): Previsão de devolução acordada.
- `ReturnDate` (`DateTime?`): Data e hora efetiva da devolução.
- `ReturnedByUserId` (`int?`): Funcionário que recebeu a ferramenta de volta.
- `ReturnCondition` (`ToolCondition?`): Estado físico e operacional da ferramenta na entrega.
- `CheckoutNotes` (`string?`): Observações da retirada / motivo.
- `ReturnNotes` (`string?`): Observações da devolução.
- `Status` (`ToolCheckoutStatus`): `OPEN` (em aberto) ou `RETURNED` (concluída).
- `CreatedAt` (`DateTime`): Timestamp de auditoria.

### 2.4 Enumeração `ToolCondition`
- `OK` (0): Em perfeito estado funcional e estético.
- `DAMAGED` (1): Avariada ou com quebra identificada.
- `NEEDS_CALIBRATION` (2): Necessita aferição ou calibração.
- `DIRTY` (3): Entregue suja / necessita limpeza.
- `MISSING_ACCESSORIES` (4): Faltando pontas de prova, cabos ou adaptadores.

### 2.5 Entidade `ToolMaintenance` (Manutenção e Calibração)
- `MaintenanceId` (`Guid`): Identificador único do evento.
- `ToolId` (`Guid`): Referência da ferramenta.
- `MaintenanceType` (`ToolMaintenanceType`): `PREVENTIVE`, `CORRECTIVE`, `CALIBRATION`, `INSPECTION`, `DAMAGE_REPAIR`, `DISPOSAL`.
- `Description` (`string`): Escopo do serviço ou calibração.
- `CostCents` (`long`): Custo total do reparo/aferição em centavos (CentsV1).
- `Provider` (`string?`): Fornecedor ou laboratório responsável pela calibração/manutenção.
- `StartDate` (`DateTime`): Data de envio / início.
- `CompletionDate` (`DateTime?`): Data de retorno / conclusão.
- `PerformedBy` (`string?`): Técnico ou técnico terceirizado.
- `Status` (`MaintenanceStatus`): `SCHEDULED`, `IN_PROGRESS`, `COMPLETED`, `CANCELLED`.
- `Notes` (`string?`): Observações e laudos técnicos.
- `CreatedAt` (`DateTime`): Timestamp de auditoria.

---

## 3. ARQUITETURA DE SERVIÇO & PERSISTÊNCIA

### 3.1 Padrão de Repositório (`IToolRepository`)
- Métodos assíncronos e síncronos de consulta paginada, busca textual (`Code`, `Name`, `SerialNumber`, `Brand`), filtro por status e categoria.
- Transação atômica em SQLite para checkout e checkin com verificação estrita de concorrência (`RowVersion`).

### 3.2 Regras de Negócio do Serviço (`IToolService`)
1. **Regra de Retirada (Checkout):**
   - Uma ferramenta só pode ser retirada se seu status for rigorosamente `AVAILABLE`.
   - Se outro usuário retirar simultaneamente, o `RowVersion` detecta a colisão e lança exceção de negócio amigável: `"A ferramenta '...' já foi retirada por outro usuário. Atualize a listagem."`.
   - Ao retirar, a ferramenta passa para `IN_USE` (ou `BORROWED`), o `CurrentResponsibleUserId` é gravado e um registro de `ToolCheckout` com status `OPEN` é persistido.
2. **Regra de Devolução (Checkin):**
   - O checkout aberto é encerrado com `ReturnDate`, `ReturnedByUserId` e `ReturnCondition`.
   - Se `ReturnCondition == OK`, status retorna a `AVAILABLE` e `CurrentResponsibleUserId = null`.
   - Se `ReturnCondition == DAMAGED`, status muda automaticamente para `DAMAGED`.
   - Se `ReturnCondition == NEEDS_CALIBRATION`, status muda para `MAINTENANCE`.
3. **Auditoria Integrada:**
   - Todo checkout, checkin e alteração gera registro de auditoria via `AuditTrailService` (`App.Audit.RegistrarOperacao`).

---

## 4. DESIGN SYSTEM & INTERFACE (FERRAMENTAS + TOOL 360)

1. **FerramentasControl (Tela Principal):**
   - Busca em tempo real, filtros por Categoria, Status, Localização e Responsável.
   - Cards executivos no topo: *Total de Ferramentas*, *Disponíveis*, *Em Uso*, *Em Manutenção/Avaria*.
   - DataGrid moderno com badges de status coloridos segundo o Design System.
   - Ações: *Nova Ferramenta*, *Retirar Ferramenta*, *Devolver*, *Abrir Tool 360*.
2. **Tool360Window (Visão Integral 360):**
   - Cabeçalho: Identidade visual, código, patrimônio, marca, número de série e badge de status.
   - Bloco Situação: Responsável atual, local físico, última data de saída.
   - Bloco Manutenção: Última e próxima calibração/manutenção programada, custo acumulado em CentsV1.
   - Histórico de Movimentações: Linha do tempo com todas as retiradas, devoluções, avarias e apontamentos de OS.
3. **Compatibilidade Visual:**
   - Suporte completo a Light e Dark themes via `DynamicResource`.
   - Grid e layout adaptados para 1280x720 e 1920x1080.
