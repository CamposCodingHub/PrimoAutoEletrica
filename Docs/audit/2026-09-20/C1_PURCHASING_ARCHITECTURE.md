# PRIMOX WORKSHOP — CICLO C1
# ARQUITETURA DO MÓDULO PRIMOX PURCHASING (C1.2)
**Data:** 2026-09-25  
**Módulo:** PRIMOX Purchasing — Gestão de Necessidades e Pedidos de Compra  
**Status Arquitetural:** IMPLEMENTATION SPEC  

---

## 1. VISÃO GERAL & PRINCÍPIO DE DOMÍNIO

O módulo **PRIMOX Purchasing** estabelece uma fronteira clara de responsabilidades operacionais na oficina:

```
┌────────────────────────────────────────────────────────┐
│                   SEPARAÇÃO DE PAPÉIS                  │
├─────────────────────────┬──────────────────────────────┤
│ ESTOQUE                 │ O que eu TENHO               │
│ COMPRAS                 │ O que eu PRECISO             │
│ FINANCEIRO              │ O que eu PAGUEI / DEVO       │
└─────────────────────────┴──────────────────────────────┘
```

Essas responsabilidades nunca devem ser misturadas. Compras não substitui a gestão de estoque nem cria um "segundo financeiro" paralelo.
O objetivo é transformar a reposição de insumos e peças de auto elétrica e mecânica em um processo controlado, auditado, com previsão de custos em **CentsV1** e aprovação humana.

---

## 2. MODELO DE DOMÍNIO & ENTIDADES

### 2.1 Entidade `PurchaseRequest` (Solicitação / Necessidade de Compra)
- `PurchaseRequestId` (`Guid`): Identificador único global.
- `Number` (`string`): Código legível sequencial (ex: `REQ-20260925-0001`).
- `RequestedByUserId` (`int`): Funcionário solicitante (`Funcionarios.Id`).
- `RequestedByUserName` (`string`): Snapshot do nome do solicitante.
- `RequestedAt` (`DateTime`): Data e hora da solicitação.
- `Priority` (`PurchasePriority`): Grau de urgência operacional.
- `Reason` (`PurchaseReason`): Justificativa operacional do pedido.
- `Status` (`PurchaseRequestStatus`): Ciclo de vida da solicitação.
- `ApprovedByUserId` (`int?`): Usuário com perfil de gestão que aprovou o pedido.
- `ApprovedByUserName` (`string?`): Snapshot do nome do aprovador.
- `ApprovedAt` (`DateTime?`): Timestamp de aprovação.
- `CancelledByUserId` (`int?`): Usuário que cancelou.
- `CancelledAt` (`DateTime?`): Timestamp de cancelamento.
- `CancellationReason` (`string?`): Justificativa obrigatória do cancelamento.
- `SupplierId` (`int?`): Fornecedor selecionado (`Fornecedores.Id`).
- `SupplierName` (`string?`): Nome do fornecedor.
- `TotalEstimatedCostCents` (`long`): Custo total estimado em centavos (CentsV1).
- `TotalActualCostCents` (`long`): Custo total efetivo da compra em centavos (CentsV1).
- `FiscalDocumentNumber` (`string?`): Número da NF-e / cupom de compra quando recebido.
- `Notes` (`string?`): Observações gerais.
- `RowVersion` (`int`): Concorrência otimista.
- `CreatedAt` (`DateTime`): Timestamp de criação.
- `UpdatedAt` (`DateTime`): Timestamp da última modificação.

### 2.2 Enumeração `PurchasePriority`
- `URGENT` (0): Risco de paralisação imediata de serviço na oficina ou cliente com veículo parado em elevador.
- `HIGH` (1): Estoque zerado ou criticamente abaixo da reserva mínima operacional.
- `NORMAL` (2): Reposição preventiva planejada conforme curva de consumo.
- `LOW` (3): Compra por conveniência, oportunidade comercial ou suprimento de longo prazo.

### 2.3 Enumeração `PurchaseReason`
- `LOW_STOCK` (0): Nível de estoque atingiu ou rompeu o mínimo.
- `OUT_OF_STOCK` (1): Item com estoque zerado na oficina.
- `DAMAGED` (2): Peça de reposição interna danificada.
- `NEW_SERVICE` (3): Novo serviço específico que exige peça sob demanda.
- `MAINTENANCE` (4): Manutenção predial ou de equipamentos da própria oficina.
- `EMPLOYEE_REQUEST` (5): Solicitação de ferramental/EPI por colaborador.
- `PREVENTIVE` (6): Reposição preventiva antes de períodos sazonais de alta demanda.
- `WORK_ORDER` (7): Demanda direta originada por Ordem de Serviço específica.
- `OTHER` (8): Outros motivos operacionais.

### 2.4 Enumeração `PurchaseRequestStatus`
- `DRAFT` (0): Rascunho em edição pelo solicitante.
- `REQUESTED` (1): Enviada para análise da diretoria / gerência.
- `APPROVED` (2): Aprovada tecnicamente e financeiramente.
- `QUOTING` (3): Em processo de cotação com múltiplos fornecedores.
- `ORDERED` (4): Pedido formalizado e aguardando entrega do fornecedor.
- `PARTIALLY_RECEIVED` (5): Entrega parcial conferida no estoque.
- `RECEIVED` (6): Totalmente entregue, conferida e integrada ao estoque e financeiro.
- `CANCELLED` (7): Recusada ou cancelada operacionalmente com motivo registrado.

### 2.5 Entidade `PurchaseRequestItem` (Itens da Solicitação)
- `ItemId` (`Guid`): Identificador único do item da requisição.
- `PurchaseRequestId` (`Guid`): Referência da requisição pai.
- `ProductId` (`int`): ID do produto cadastrado (`Produtos.Id`).
- `ProductCode` (`string?`): Código de barras ou código interno do produto.
- `ProductName` (`string`): Descrição oficial do item.
- `RequestedQuantity` (`decimal`): Quantidade solicitada pelo usuário.
- `SuggestedQuantity` (`decimal`): Quantidade calculada automaticamente pelo sistema com base no estoque ideal.
- `CurrentStock` (`decimal`): Snapshot do estoque no momento da solicitação.
- `MinimumStock` (`decimal`): Snapshot do estoque mínimo cadastrado.
- `IdealStock` (`decimal`): Snapshot do estoque máximo/ideal.
- `EstimatedUnitCostCents` (`long`): Custo unitário estimado em centavos (CentsV1).
- `ActualUnitCostCents` (`long`): Custo unitário faturado pelo fornecedor em centavos (CentsV1).
- `ReceivedQuantity` (`decimal`): Quantidade efetivamente entregue no recebimento físico.
- `Priority` (`PurchasePriority`): Prioridade do item individual.
- `Notes` (`string?`): Observações técnicas (ex: fabricante alternativo permitido).
- `Status` (`PurchaseItemStatus`): `PENDING`, `ORDERED`, `RECEIVED`, `CANCELLED`.

---

## 3. ARQUITETURA DE INTEGRAÇÃO OPERACIONAL

### 3.1 Detecção & Sugestão Automática de Reposição (Estoque -> Compras)
- O serviço `PurchaseService` monitora a condição:
  `Produtos.EstoqueAtual <= Produtos.EstoqueMinimo`
- Gera sugestões de compra com a quantidade necessária para atingir `Produtos.EstoqueMaximo` (ou estoque ideal).
- **Diretriz de Transparência:** NUNCA cria pedido automaticamente com envio ao fornecedor. A sugestão é apresentada na tela de Compras com badge informativo. O usuário revisa, ajusta quantidades e formaliza a solicitação.

### 3.2 Recebimento Físico & Integração com Estoque (Compras -> Estoque)
Ao confirmar o recebimento do pedido (`Status = RECEIVED` ou `PARTIALLY_RECEIVED`):
1. Incrementa o saldo do produto no estoque:
   `Produtos.EstoqueAtual += ReceivedQuantity`
2. Registra o histórico operacional da movimentação no `EstoqueOperationalService` com tipo `ENTRADA_COMPRA` e referência ao `PurchaseRequest.Number`.
3. Atualiza o custo de aquisição do produto se fornecido valor mais recente, utilizando CentsV1.

### 3.3 Integração com Financeiro (Compras -> Contas a Pagar)
Ao receber o pedido de compras com documento/obrigação:
1. Comunica-se diretamente com o `FinanceiroDatabaseService`.
2. Cria lançamento de `ContaPagar` com:
   - Fornecedor vinculado (`FornecedorId`).
   - Valor em centavos inteiros via `MoneyIO.GravarMoeda` / `CentsV1`.
   - Data de vencimento e categoria `Compras / Peças`.
   - Número do documento fiscal / requisição.
3. Não duplica registros financeiros e respeita rigorosamente as regras de conciliação existentes.

---

## 4. DESIGN SYSTEM & INTERFACE

- Tela de **Necessidades de Compra**:
  - Filtros rápidos no topo: *🔴 Urgentes*, *🟠 Alta*, *Normal*, *Aguardando Aprovação*, *Em Pedido*.
  - Tabela limpa com colunas: *Código*, *Item*, *Estoque Atual*, *Mínimo*, *Qtd. Solicitada*, *Prioridade*, *Status*, *Ações*.
  - Ações contextuais de aprovação, cancelamento e recebimento com diálogos informativos.
  - Alinhamento pleno ao Design System (Light/Dark, 1280x720, zero ruído visual).
