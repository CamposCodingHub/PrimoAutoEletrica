# PRIMOX WORKSHOP — MATRIZ DE VERDADE COMERCIAL & OPERACIONAL (CICLO C1)
**Data de Emissão:** 20/09/2026  
**Ciclo:** C1 — Inteligência Operacional, Ferramentas, Compras, Conhecimento & Assist Foundation  
**Branch:** `cycle-c1/operational-intelligence`  
**Base de Código Integrada:** PRIMOX Workshop Desktop (WPF .NET 10.0-windows)  
**Banco de Dados Operacional:** `primoauto_operacional.db` (CentsV1, `user_version = 1`)  
**Banco Protegido:** `primoauto.db` (SHA-256: `C7420D18...`, 20.201.472 bytes, Somente-Leitura Intacto)

---

## 1. Classificação de Funcionalidades

| Domínio | Funcionalidade | Status | Implementação Concreta | Limitações & Próximos Passos |
|---|---|---|---|---|
| **C1.1 Tools** | Cadastro e Patrimônio de Ferramentas | **IMPLEMENTADO** | `Tool`, `ToolRepository`, `ToolService`, `NovaFerramentaDialog` | Suporta serial, marca, modelo, localização e valor patrimonial em CentsV1. |
| **C1.1 Tools** | Custódia e Check-in / Checkout | **IMPLEMENTADO** | `ToolCheckout`, `IToolRepository.RegistrarRetiradaAsync`, `RegistrarDevolucaoAsync`, `ToolCheckoutDialog` | Vínculo com usuário/técnico, OS e placa. Concorrência tratada via RowVersion. |
| **C1.1 Tools** | Visão Tool360 | **IMPLEMENTADO** | `Tool360Window.xaml` + `.cs` | Histórico completo de movimentações e calibrações. Ações rápidas de devolução e aferição. |
| **C1.1 Tools** | QR Code & Identificador Ótico | **IMPLEMENTADO** | Formato canônico `PRIMOX://TOOL/{Code}` exibido na Tool360 e Grid. | Geração de etiqueta física de impressão gráfica reservada para melhoria posterior. |
| **C1.1 Tools** | Histórico de Calibrações e Manutenção | **IMPLEMENTADO** | `ToolMaintenance`, `IToolRepository.InserirManutencaoAsync`, `ObterManutencoesAsync` | Custos registrados estritamente em centavos inteiros (`CostCents`). |
| **C1.2 Purchasing** | Requisições de Compra | **IMPLEMENTADO** | `PurchaseRequest`, `PurchaseRepository`, `PurchaseService`, `NovaRequisicaoCompraDialog` | Status: REQUESTED, APPROVED, ORDERED, RECEIVED, CANCELLED. |
| **C1.2 Purchasing** | Sugestões por Estoque Mínimo | **IMPLEMENTADO** | `PurchaseService.ObterSugestoesEstoqueMinimoAsync`, `NecessidadesCompraControl` (Aba 2) | Dispara automaticamente com base na margem de segurança configurada em `Produtos.QuantidadeMinima`. |
| **C1.2 Purchasing** | Formalização de Pedidos | **IMPLEMENTADO** | `PurchaseService.FormalizarPedidoAsync` | Associa fornecedor, registra prazo e atualiza status para ORDERED. |
| **C1.2 Purchasing** | Recebimento Físico & Integração Estoque | **IMPLEMENTADO** | `PurchaseService.ReceberMercadoriaAsync` | Credita estoque físico via `IProdutoRepository.AdicionarEstoque` e gera registro de contas a pagar (CentsV1). |
| **C1.3 Knowledge** | Boletins Técnicos Estruturados | **IMPLEMENTADO** | `TechnicalKnowledgeEntry`, `KnowledgeRepository`, `KnowledgeService`, `CasoTecnicoDialog` | Classificação por sistema, tensão (12V/24V), sintomas e procedimentos de medição. |
| **C1.3 Knowledge** | Casos Reais de Oficina (D01-D17) | **IMPLEMENTADO** | `DiagnosticCase`, `DatabaseService.C1.cs` (Seed D01-D17) | Casos reais de bancada preservados integralmente com evidências e causas comprovadas. |
| **C1.3 Knowledge** | Promoção de OS para Caso Técnico | **IMPLEMENTADO** | `KnowledgeService.CriarCasoAPartirDeOSAsync`, `BaseConhecimentoControl` | Transforma histórico de OS concluída em documentação técnica com rastreabilidade. |
| **C1.4 Assist** | Copiloto Técnico Grounded (Fail-Closed) | **IMPLEMENTADO** | `IAssistantService`, `AssistantService`, `GroundedLocalRuleAssistantProvider`, `BaseConhecimentoControl` (Aba 3) | Zero alucinação: recusa pedidos de troca sem medição prévia. Respostas fundamentadas no acervo. |
| **C1.4 Assist** | Provedores Externos LLM / Cloud | **FUTURO** | Arquitetura preparada via `IAssistantProvider` | Provedor local implementado e ativo. Provedor de API externa (OpenAI/Anthropic/Gemini) planejado para ciclo futuro. |

---

## 2. Garantias Comerciais e de Integridade

1. **Proteção Total do Banco Legado:**
   - O banco `primoauto.db` permaneceu em modo Somente-Leitura (`IsReadOnly = True`), com tamanho inalterado de 20.201.472 bytes e hash SHA-256 `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`.
2. **Moeda CentsV1 Obrigatória:**
   - 100% dos novos campos monetários (`PurchaseValueCents`, `CostCents`, `EstimatedUnitCostCents`, `ActualUnitCostCents`, `TotalEstimatedCostCents`, `TotalActualCostCents`) foram criados como `INTEGER` e operados via tipo estrito `MoneyCents`.
3. **Branch e Código:**
   - A branch `main` permanece intacta no commit `29b19b16d0e6e3413bdba20c505e20c992596c24`.
   - Todas as alterações estão contidas na branch `cycle-c1/operational-intelligence`.
4. **Resultados de Testes Verificados:**
   - Suite completa de testes unitários: 474/474 aprovados (0 falhas).
   - Teste de inicialização do executável da área de trabalho: 3/3 aprovados (0 erros, 0 exceções no log).
