# PRIMOX WORKSHOP — CERTIFICAÇÃO OPERACIONAL E METROLÓGICA (CICLO C1)
**Data de Emissão:** 20/09/2026  
**Status do Ciclo:** Homologado e Certificado com Sucesso  
**Certificação de Integridade:** ZERO Quebra de Contrato, ZERO Alucinação em Diagnóstico, 100% CentsV1

---

## 1. Escopo Certificado

### 1.1 Pilar C1.1 — PRIMOX Tools (Patrimônio e Custódia)
- **Modelos:** `Tool`, `ToolCheckout`, `ToolMaintenance`.
- **Tabelas Operacionais:** `Tools`, `ToolCheckouts`, `ToolMaintenances`.
- **Regras Operacionais:**
  - Checkouts impedidos para ferramentas que já estejam com status `IN_USE` ou `MAINTENANCE`.
  - Concorrência protegida via `RowVersion` otimista.
  - Vínculo direto de custódia com Técnico Responsável (`CurrentResponsibleUserName`), Ordem de Serviço (`WorkOrderNumber`) e Veículo (`VehiclePlate`).
  - Formato ótico padronizado `PRIMOX://TOOL/{Code}`.
  - Interface do Usuário: `FerramentasControl`, `Tool360Window`, `ToolCheckoutDialog`, `NovaFerramentaDialog`.

### 1.2 Pilar C1.2 — PRIMOX Purchasing (Compras e Reposição)
- **Modelos:** `PurchaseRequest`, `PurchaseRequestItem`.
- **Tabelas Operacionais:** `PurchaseRequests`, `PurchaseRequestItems`.
- **Regras Operacionais:**
  - Sugestões automáticas de compra geradas a partir de produtos com `QuantidadeEstoque <= QuantidadeMinima`.
  - Workflow de aprovação formal com registro de usuário e timestamp.
  - Formalização de pedido associando fornecedor parceiro cadastrado.
  - Entrada física em estoque com atualização automática das quantidades e registro correspondente no contas a pagar através do padrão `CentsV1`.
  - Interface do Usuário: `NecessidadesCompraControl`, `NovaRequisicaoCompraDialog`.

### 1.3 Pilar C1.3 — PRIMOX Knowledge (Base de Conhecimento Técnico)
- **Modelos:** `TechnicalKnowledgeEntry`, `DiagnosticCase`.
- **Tabelas Operacionais:** `TechnicalKnowledgeEntries`, `DiagnosticCases`.
- **Regras Operacionais:**
  - Acervo estruturado com classificação rigorosa por Tensão (12V e 24V Linha Pesada) e Sistema Elétrico (Partida, Carga, Injeção, Iluminação, Ar Condicionado).
  - Preservação e seed dos 17 Casos Reais de Oficina (D01 a D17).
  - Capacidade operacional de promover diagnósticos conclusivos de Ordens de Serviço finalizadas em novos casos técnicos com rastreabilidade da OS de origem.
  - Interface do Usuário: `BaseConhecimentoControl`, `CasoTecnicoDialog`.

### 1.4 Pilar C1.4 — PRIMOX Assist Foundation (Zero Alucinação)
- **Provedor:** `GroundedLocalRuleAssistantProvider` implementando `IAssistantProvider`.
- **Regras Operacionais:**
  - Operação estritamente **Fail-Closed**: na ausência de medições elétricas comprovadas, o copiloto não autoriza substituição de componentes (ex: alternador, motor de partida, bateria, ECU).
  - Emissão obrigatória de protocolo de testes não destrutivos (checklist instrumental com multímetro True RMS, alicate amperímetro DC e osciloscópio).
  - Citações obrigatórias de fontes internas do acervo grounded da oficina.
  - Interface do Usuário: Aba dedicada no `BaseConhecimentoControl` com atalhos para diagnósticos frequentes de 12V e 24V.

---

## 2. Auditoria de CentsV1 e Integridade de Dados

| Tabela | Coluna Cents | Tipo SQLite | Validação CentsV1 |
|---|---|---|---|
| `Tools` | `PurchaseValueCents` | `INTEGER NOT NULL` | ✅ Aprovado (long) |
| `ToolMaintenances` | `CostCents` | `INTEGER NOT NULL` | ✅ Aprovado (long) |
| `PurchaseRequests` | `TotalEstimatedCostCents` | `INTEGER NOT NULL` | ✅ Aprovado (long) |
| `PurchaseRequests` | `TotalActualCostCents` | `INTEGER NOT NULL` | ✅ Aprovado (long) |
| `PurchaseRequestItems` | `EstimatedUnitCostCents` | `INTEGER NOT NULL` | ✅ Aprovado (long) |
| `PurchaseRequestItems` | `ActualUnitCostCents` | `INTEGER NOT NULL` | ✅ Aprovado (long) |
| `ContasPagar` | `ValorCents` | `INTEGER NOT NULL` | ✅ Aprovado (long) |

---

## 3. Evidências de Testes e Execução

- **Suite Unitária Automatizada:** 474 testes executados e aprovados (0 falhas).
  - `ToolServiceTests`: 9 testes PASS.
  - `PurchaseServiceTests`: 7 testes PASS.
  - `KnowledgeServiceTests`: 4 testes PASS.
  - `AssistFoundationTests`: 5 testes PASS.
  - `C1UiAndNavigationTests`: 4 testes PASS.
  - Testes de Regressão e Moeda (Money/Cents): 445 testes legados PASS.
- **Auditoria de Inicialização em Desktop (Shortcut):**
  - Execução 1: PASS (Login Window carregada)
  - Execução 2: PASS (Login Window carregada)
  - Execução 3: PASS (Login Window carregada)
  - Detecção de erro "SQLite Error 8 (readonly database)": ZERO ocorrências.
- **Banco Protegido `primoauto.db`:**
  - Hash SHA-256 inicial: `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
  - Hash SHA-256 final: `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
  - Status: 100% Intacto.
