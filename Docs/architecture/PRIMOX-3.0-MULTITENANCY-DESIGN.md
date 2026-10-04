# PRIMOX 3.0 — MULTI-TENANCY & BRANCH ISOLATION DESIGN
**Data:** 04 de Outubro de 2026  
**Versão:** 3.0.0-TENANCY  
**Status:** ESPECIFICAÇÃO DE ARQUITETURA MULTI-EMPRESA E MULTI-FILIAL  
**Objetivo:** Estabelecer a estratégia formal de isolamento de dados para expansão SaaS e redes de oficinas sem vazamento de informações.

---

## 1. HIERARQUIA ORGANIZACIONAL DO PRIMOX

A segregação de dados corporativos no PRIMOX 3.0 é desenhada em quatro níveis hierárquicos rígidos:

```
┌────────────────────────────────────────────────────────┐
│                        TENANT                          │ (Empresa Contratante / Grupo Empresarial)
│             Id, Nome, CNPJ Raiz, Plano, Status         │
└───────────────────────────┬────────────────────────────┘
                            │ 1 : N
┌───────────────────────────▼────────────────────────────┐
│                    BRANCH / FILIAL                     │ (Unidade Física / Loja / Oficina de Campo)
│        Id, TenantId, Codigo, Nome, CNPJ, Inscricao      │
└───────────────────────────┬────────────────────────────┘
                            │ 1 : N
┌───────────────────────────▼────────────────────────────┐
│                        USER                            │ (Funcionário / Mecânico / Gerente)
│        Id, TenantId, Email, Nome, Cargo, Role          │
└───────────────────────────┬────────────────────────────┘
                            │ N : N (via UserBranchScope)
┌───────────────────────────▼────────────────────────────┐
│                  OPERATIONAL DATA                      │ (Dados com Escopo Estrito de Empresa/Filial)
│           OS, Clientes, Veículos, Estoque, Caixa        │
└────────────────────────────────────────────────────────┘
```

---

## 2. MAPEAMENTO DE ENTIDADES E ATRIBUTOS DE ESCOPO

No modelo atual do PRIMOX, as entidades não possuem escopo de tenant ou filial. A tabela abaixo especifica a estratégia de enriquecimento progressivo:

| Entidade de Domínio | Requer `TenantId` | Requer `FilialId` | Estratégia de Isolamento |
| :--- | :---: | :---: | :--- |
| **`OrdemServico`** | **SIM** | **SIM** | Estrito por filial física onde o serviço está sendo executado. |
| **`Cliente`** | **SIM** | *Opcional* | Compartilhado entre todas as filiais do mesmo Tenant (cadastro unificado da rede). |
| **`Veiculo`** | **SIM** | *Opcional* | Compartilhado no Tenant para histórico de manutenção entre filiais da mesma rede. |
| **`Produto / Item`** | **SIM** | *Opcional* | Catálogo mestre unificado no Tenant com regras de preço padronizadas. |
| **`SaldoEstoque`** | **SIM** | **SIM** | **Estrito por filial**. Cada loja possui seu saldo físico, prateleira e curva ABC. |
| **`MovimentacaoEstoque`** | **SIM** | **SIM** | Auditável por filial com registro de transferências inter-lojas (`TransferenciaEstoque`). |
| **`CaixaOperacional`** | **SIM** | **SIM** | **Estrito por terminal/filial**. Não há compartilhamento de fechamento de caixa entre lojas. |
| **`Orcamento`** | **SIM** | **SIM** | Vinculado à filial emissora. |
| **`AuditLogs`** | **SIM** | **SIM** | Rastreabilidade total com registro de usuário, filial de origem e IP. |

### Metadados Universais de Auditoria (Audit Shadow):
Todas as tabelas do banco receberão nas migrações futuras os seguintes atributos:
```sql
TenantId   TEXT NOT NULL,
FilialId   TEXT NOT NULL,
CreatedAt  TEXT NOT NULL, -- UTC ISO 8601
UpdatedAt  TEXT NOT NULL, -- UTC ISO 8601
CreatedBy  TEXT NOT NULL,
UpdatedBy  TEXT NOT NULL
```

---

## 3. PADRÃO DE CONSULTA COM FILTRO AUTOMÁTICO DE ESCOPO

Para impedir vazamento acidental de dados (*cross-tenant data leakage*), as consultas na camada de aplicação nunca serão executadas sem o filtro de contexto:

### 3.1 Interface de Contexto de Execução
```csharp
namespace PRIMOX.Application.Common
{
    public interface IExecutionContext
    {
        Guid TenantId { get; }
        Guid BranchId { get; }
        Guid UserId { get; }
        bool IsSystemAdministrator { get; }
    }
}
```

### 3.2 Padrão de Query SQL no Repositório
```sql
-- Exemplo de consulta segura em Repositório Dapper / ADO.NET
SELECT Id, NumeroOS, DataAbertura, Status, ValorTotal
FROM OrdensServico
WHERE TenantId = @TenantId
  AND FilialId = @FilialId
  AND DataExclusao IS NULL
ORDER BY DataAbertura DESC
LIMIT @PageSize OFFSET @Offset;
```

---

## 4. ÍNDICES DE BANCO DE DADOS PARA MULTI-TENANCY

Para manter performance de busca mesmo com dezenas de milhões de registros em base corporativa centralizada (SQL Server / PostgreSQL):

```sql
CREATE INDEX IX_OrdensServico_Tenant_Filial_Status 
    ON OrdensServico (TenantId, FilialId, Status);

CREATE INDEX IX_EstoqueSaldo_Tenant_Filial_Produto 
    ON EstoqueSaldo (TenantId, FilialId, ProdutoId);

CREATE INDEX IX_Clientes_Tenant_CpfCnpj 
    ON Clientes (TenantId, CpfCnpj);
```

---

## 5. ESTRATÉGIA DE TRANSIÇÃO E COMPATIBILIDADE

1. **Instalações Monousuário / Offline (Atual):**
   * O sistema utilizará um identificador estático padrão de migração:
     * `DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001")`
     * `DefaultBranchId = Guid.Parse("11111111-1111-1111-1111-111111111111")`
   * Dessa forma, nenhuma instalação SQLite atual terá quebra ou incompatibilidade ao atualizar.
2. **Habilitação SaaS / Nuvem:**
   * Quando o token JWT for emitido pela Web API, os claims `tenant_id` e `branch_id` alimentarão o `IExecutionContext` em cada requisição.
