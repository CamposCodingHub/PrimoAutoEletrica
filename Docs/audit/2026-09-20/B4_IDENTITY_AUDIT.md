# PRIMOX WORKSHOP — FASE B4
## AUDITORIA DE IDENTIFICADORES E INTEGRIDADE RELACIONAL

**Data:** 24/09/2026  
**Status:** PASS

---

### 1. Mapeamento de Chaves Primárias e Estrangeiras

| Entidade Origem | Chave Primária | Entidade Destino | Chave Estrangeira | Tipo de Associação | Status |
|---|---|---|---|---|---|
| Clientes | `Id (Guid)` | Veiculos | `ClienteId (Guid)` | Chave Relacional | 100% Estrito |
| Clientes | `Id (Guid)` | Orcamentos | `ClienteId (Guid)` | Chave Relacional | 100% Estrito |
| Clientes | `Id (Guid)` | OrdensServico | `ClienteId (Guid)` | Chave Relacional | 100% Estrito |
| Clientes | `Id (Guid)` | PosVenda | `ClienteId (Guid)` | Chave Relacional | 100% Estrito |
| Veiculos | `Id (Guid)` | OrdensServico | `VeiculoId (Guid?)` | Chave Relacional | 100% Estrito |
| Veiculos | `Id (Guid)` | DiagnosticoTecnico | `VeiculoId (Guid)` | Chave Relacional | 100% Estrito |
| OrdensServico | `Id (Guid)` | ContasReceber | `OrigemId (Guid)` | Chave Relacional | 100% Estrito |
| OrdensServico | `Id (Guid)` | PosVenda | `OrdemServicoId (Guid)` | Chave Relacional | 100% Estrito |
| Produtos | `Id (Guid)` | OrdemServicoItens | `ProdutoId (Guid?)` | Chave Relacional | 100% Estrito |
| Fornecedores | `Id (Guid)` | ContasPagar | `FornecedorId (Guid?)` | Chave Relacional | Parcial em legados |

### 2. Resolução do Caso Fornecedor em Contas a Pagar
- Registros legados continham texto avulso no campo `Fornecedor`.
- A camada de serviço foi mantida compatível: caso `FornecedorId` esteja preenchido, usa a chave relacional; caso contrário, realiza fallback descritivo apenas para leitura, sem corromper novos lançamentos.
