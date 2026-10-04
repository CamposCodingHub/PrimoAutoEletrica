# PRIMOX 3.0 — DOMAIN MODEL & CORE AGGREGATE SPECIFICATION
**Data:** 04 de Outubro de 2026  
**Versão:** 3.0.0-DOMAIN  
**Status:** ESPECIFICAÇÃO DE MODELAGEM DE DOMÍNIO (DDD)  
**Conceito Fundamental:** A Ordem de Serviço como Agregado Raiz da Oficina Inteligente.

---

## 1. O CICLO OPERACIONAL DA ORDEM DE SERVIÇO

No PRIMOX 3.0, a **Ordem de Serviço (Repair Order)** deixa de ser um mero cabeçalho de cobrança e torna-se o **agregador central de toda a inteligência e rastreabilidade da oficina**:

```
[Entrada e Cliente]
  Cliente ──► Veículo ──► Ordem de Serviço (Abertura / Recepção)
                                │
[Inspeção e Queixa]             ▼
  Queixa / Sintoma Relatado ──► Inspeção Veicular Digital (DVI 2.0)
                                │
[Diagnóstico Técnico]           ▼
  Sessão Diagnóstica ──► Testes & Medições Elétricas ──► Hipóteses & Evidências
                                │
[Solução e Causa Raiz]          ▼
  Causa Raiz Confirmada ──► Plano de Reparo (Peças de Estoque + Mão de Obra)
                                │
[Comercialização]               ▼
  Orçamento Gerado ──► Aprovação do Cliente (Termo / Assinatura Digital)
                                │
[Execução de Oficina]           ▼
  Alocação de Box / Técnico ──► Execução & Teste de Validação Final
                                │
[Faturamento e Saída]           ▼
  Pagamento (PDV / Caixa) ──► Emissão Fiscal ──► Entrega com Termo de Garantia
                                │
[Inteligência Coletiva]         ▼
  Histórico do Veículo ──► PRIMOX Repair Intelligence (Aprendizado Contínuo)
```

---

## 2. INVENTÁRIO DE ENTIDADES: ATUAL VS. ALVO

| Entidade | Status Atual | Diagnóstico Técnico no Repositório | Modelo Alvo no PRIMOX 3.0 |
| :--- | :---: | :--- | :--- |
| **Cliente** | **EXISTE** | Implementado em `Models/Cliente.cs`. Possui soft-delete e campos LGPD. | Mantido como Entidade Raiz de Relacionamento. Adicionar `TenantId` e `FilialId`. |
| **Veiculo** | **EXISTE** | Implementado em `Models/Veiculo.cs`. Vínculo direto com Cliente e histórico de KM. | Mantido como Entidade de Ativo. Adicionar identificador universal (`VIN`) normalizado. |
| **OrdemServico** | **FRAGMENTADA** | Implementado em `Models/OrdemServico.cs`, mas parte de suas regras está em `DatabaseService.OrdensServico.cs`. | **Agregado Raiz Central**. Agrupa itens, eventos, inspeção DVI e sessão de diagnóstico. |
| **Orcamento** | **EXISTE** | Implementado em `Models/Orcamento.cs` com itens e cálculo de margem/desconto. | Mantido como Proposta Comercial vinculável à Ordem de Serviço. |
| **Inspecao (DVI)** | **EXISTE** | Implementado em `Models/Dvi/InspecaoDvi.cs` com checklist semafórico e fotos. | Integrado diretamente ao ciclo de entrada da Ordem de Serviço. |
| **Sintoma / Queixa** | **FRAGMENTADA** | Atualmente campos de string soltos (`ProblemaRelatado`, `QueixaCliente`). | Entidade estruturada com categorização por sistema veicular. |
| **Diagnostico / DTC** | **FRAGMENTADA** | Strings soltas no RAG e em `OrdemServico.DiagnosticoFinal`. | Entidade rica contendo código OBD-II/ISO, congelamento de dados (Freeze Frame). |
| **TesteDiagnostico** | **NÃO EXISTE** | Não há persistência de testes executados na OS. | Nova entidade registrando tipo de teste, ferramenta utilizada e condição. |
| **Medicao** | **NÃO EXISTE** | Registros de medição não possuem tabela ou estrutura universal. | Nova entidade: valor medido, unidade (V, Ω, A, bar), faixa esperada e resultado (PASS/FAIL). |
| **Evidencia** | **NÃO EXISTE** | Evidências técnicas resumidas a comentários em texto. | Nova entidade atrelando fotos, formas de onda e leituras a hipóteses. |
| **Hipotese** | **NÃO EXISTE** | Apenas raciocínio implícito em texto da IA. | Nova entidade permitindo registrar hipóteses avaliadas, descartadas e confirmadas. |
| **CausaRaiz** | **FRAGMENTADA** | Presente como campo de texto simples em `SureTrackService`. | Entidade estruturada associada ao reparo definitivo. |
| **Reparo** | **EXISTE** | Representado pela lista de serviços executados na OS. | Mantido e enriquecido com tempo padrão e técnico responsável. |
| **Produto / Peça** | **EXISTE** | Implementado em `Models/Produto.cs` com código de barras, custo e preço de venda. | Entidade de catálogo e estoque vinculável às linhas da OS. |
| **Estoque / Movimento** | **EXISTE** | Movimentações registradas em tabela própria. | Mantido como livro-razão de estoque (StockLedger) com rastreio de lote e custo médio. |
| **Fornecedor** | **EXISTE** | Implementado em `Models/Fornecedor.cs` com vínculos operacionais. | Mantido para gestão de compras e cotações. |
| **Compra / Pedido** | **EXISTE** | Implementado em `Models/PedidoCompra.cs` e gestão anti-ruptura. | Mantido com geração automática a partir de faltas na OS. |
| **Pagamento / Caixa** | **EXISTE** | Implementado em `Models/CaixaOperacional.cs` e PDV com pagamentos mistos. | Mantido e integrado com liquidação da Ordem de Serviço. |
| **TituloReceber / Pagar** | **FRAGMENTADA** | Diluído no `FinanceiroDatabaseService.cs` sem entidades formalizadas. | **PRECISA REFACTOR**: Criar entidades formais de títulos com data de competência e vencimento. |
| **Garantia** | **FRAGMENTADA** | Controle de prazo registrado apenas em dias corridos no cabeçalho. | Entidade de termo de garantia com rastreabilidade de peças substituídas. |
| **Filial** | **FRAGMENTADA** | Tabela existe, mas não possui relacionamento nas tabelas operacionais. | **PRECISA REFACTOR**: Relacionar como escopo obrigatório em OS, Estoque e Caixa. |
| **Tenant** | **NÃO EXISTE** | Inexistente em todo o código. | **FUTURA**: Definir identificador chave para isolamento lógico em nuvem. |
| **Usuario / Funcionario** | **EXISTE** | Implementado com RBAC, perfis e permissões granulares. | Mantido. Adicionar escopo de filiais autorizadas. |
| **Box / Digital Bay** | **NÃO EXISTE** | Não há modelagem de boxes físicos da oficina. | **FUTURA**: Entidade representando baias de trabalho e ocupação em tempo real. |
| **Frota / Contrato** | **EXISTE** | Implementado em `Models/GestaoFrota.cs` com contratos corporativos B2B. | Mantido e vinculado a ordens de serviço corporativas. |

---

## 3. O AGREGADO RAIZ: `OrdemServicoAggregate`

Abaixo está o contrato conceitual do agregado principal do PRIMOX 3.0:

```csharp
namespace PRIMOX.Domain.Aggregates
{
    public class RepairOrderAggregate
    {
        public Guid Id { get; private set; }
        public string OrderNumber { get; private set; }
        public Guid CustomerId { get; private set; }
        public Guid VehicleId { get; private set; }
        public Guid BranchId { get; private set; }
        public Guid? TenantId { get; private set; }

        public RepairOrderStatus Status { get; private set; }
        public DateTimeOffset OpenedAt { get; private set; }
        public DateTimeOffset? ClosedAt { get; private set; }

        // Queixa e Inspeção
        public string CustomerComplaint { get; private set; }
        public Guid? DviInspectionId { get; private set; }

        // Diagnóstico e Evidências
        public DiagnosticSession? DiagnosticSession { get; private set; }

        // Itens de Reparo (Peças e Mão de Obra)
        private readonly List<RepairOrderItem> _items = new();
        public IReadOnlyCollection<RepairOrderItem> Items => _items.AsReadOnly();

        // Financeiro e Aprovação
        public Money TotalParts { get; private set; }
        public Money TotalLabor { get; private set; }
        public Money Discount { get; private set; }
        public Money TotalOrder { get; private set; }
        public bool IsApprovedByCustomer { get; private set; }
        public DateTimeOffset? ApprovedAt { get; private set; }

        // Garantia
        public int WarrantyDays { get; private set; }
        public string? WarrantyTerms { get; private set; }

        // Métodos de Invariante
        public void AddPartItem(Guid productId, string description, int quantity, Money unitPrice);
        public void AddLaborItem(string description, decimal hours, Money hourlyRate, Guid? technicianId);
        public void AttachDiagnosticSession(DiagnosticSession session);
        public void ApproveByCustomer(string signatureHash, DateTimeOffset timestamp);
        public void FinalizeRepair(string rootCauseSummary);
    }
}
```

---

## 4. DIRETRIZES DE TRANSIÇÃO DO DOMÍNIO

1. **Preservação de Dados Existentes:** A tabela `OrdensServico` continuará sendo a tabela de persistência no SQLite. Novas entidades (`DiagnosticSession`, `DiagnosticMeasurement`) serão ligadas por chaves estrangeiras com migrações seguras.
2. **Encapsulamento Progressivo:** Cálculos de descontos, impostos e totais da OS migrarão gradualmente do code-behind XAML e dos scripts SQL para métodos do domínio.
