# PRIMOX Workshop — MAPEAMENTO E COMPATIBILIDADE DE REPOSITÓRIOS E CONSULTAS SQL

> **Documento:** P2_2_MONEY_REPOSITORY_COMPATIBILITY.md  
> **Data:** 23/09/2026  
> **Escopo:** Mapeamento de Repositórios, Leitura/Escrita, Parâmetros e Consultas SQL para a Migração de Money  
> **Status:** AUDITADO E MAPEADO (Implementação Real: `BLOCKED`)  

---

## 1. O RISCO DA LEITURA DE `INTEGER CENTS` NA CAMADA DE PERSISTÊNCIA

Atualmente, o codebase do PRIMOX Workshop utiliza `REAL` (ou `DECIMAL`) no SQLite e realiza a leitura dos campos financeiros no C# através de três padrões principais:

```csharp
// Padrão A: Convert.ToDecimal(reader.GetValue(index))
return reader.IsDBNull(index) ? 0 : Convert.ToDecimal(reader.GetValue(index));

// Padrão B: Convert.ToDecimal(reader.GetDouble(index))
PrecoVenda = reader.IsDBNull(18) ? 0 : Convert.ToDecimal(reader.GetDouble(18));

// Padrão C: reader.GetDecimal(index)
Valor = reader.GetDecimal(3);
```

### 1.1 Análise de Quebra se o Schema Mudar sem Adaptação do Repositório
Quando uma coluna SQLite armazena `INTEGER cents` (exemplo: `12345` centavos representando `R$ 123,45`):

| Chamada Atual no Repositório | Retorno do SQLite Engine | O Que a Aplicação C# Recebe | Impacto no Usuário | Gravidade |
|---|---|---|---|:---:|
| `reader.GetValue(index)` | `long` (`12345L`) | `Convert.ToDecimal(12345L) = 12345m` | Produto de R$ 123,45 vira **R$ 12.345,00** | 🔴 **CRÍTICA** |
| `reader.GetDouble(index)` | `double` (`12345.0`) | `Convert.ToDecimal(12345.0) = 12345m` | Produto de R$ 123,45 vira **R$ 12.345,00** | 🔴 **CRÍTICA** |
| `reader.GetDecimal(index)` | `long` sob o capô | `12345m` | Conta de R$ 980,20 vira **R$ 98.020,00** | 🔴 **CRÍTICA** |

> [!CAUTION]
> **REGRA DE OURO:** Nenhum repositório pode continuar usando `GetDouble()`, `GetDecimal()` ou `Convert.ToDecimal(reader.GetValue())` direto sem divisão por `100m` após a migração para `INTEGER cents`.

---

## 2. BLUEPRINT DE ADAPTAÇÃO PARA OS REPOSITÓRIOS

Quando a migração definitiva for autorizada, todos os repositórios que leem ou gravam as 67 colunas monetárias devem adotar a seguinte especificação:

### 2.1 Leitura Segura de Colunas Monetárias
```csharp
// Abstração recomendada em Repositórios / Helpers:
protected static decimal LerMoeda(DbDataReader reader, int index)
{
    if (reader.IsDBNull(index)) return 0m;
    // O banco armazena centavos inteiros (INTEGER)
    long cents = Convert.ToInt64(reader.GetValue(index));
    return cents / 100m;
}

protected static decimal? LerMoedaAnulavel(DbDataReader reader, int index)
{
    if (reader.IsDBNull(index)) return null;
    long cents = Convert.ToInt64(reader.GetValue(index));
    return cents / 100m;
}
```

### 2.2 Escrita Segura de Parâmetros Monetários
Ao executar `INSERT` ou `UPDATE`, evitar passar `decimal` diretamente em `AddWithValue` se o driver puder enviar como float:
```csharp
// Incorreto após migração:
command.Parameters.AddWithValue("@PrecoVenda", produto.PrecoVenda); // enviaria 123.45

// Correto e blindado:
long cents = MoneyCents.FromDecimal(produto.PrecoVenda).Cents;
command.Parameters.Add("@PrecoVenda", SqliteType.Integer).Value = cents; // grava 12345
```

---

## 3. MAPEAMENTO DE TODAS AS 15 CONSULTAS SQL DE AGREGAÇÃO (`SUM`, `AVG`)

Foi realizado um levantamento exaustivo de todas as consultas SQL que executam agregações sobre colunas monetárias na aplicação:

| Arquivo | Linha | Consulta SQL / Snippet | Coluna Afetada | Comportamento Pós-Migração |
|---|:---:|---|---|---|
| `FinanceiroDatabaseService.cs` | 1112 | `SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras ... Tipo = 'Receita'` | `MovimentacoesFinanceiras.Valor` | `SUM(Valor)` retornará centavos inteiros. Consumidor deve dividir por `100m`. |
| `FinanceiroDatabaseService.cs` | 1126 | `SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras ... Tipo = 'Despesa'` | `MovimentacoesFinanceiras.Valor` | Idem |
| `FinanceiroDatabaseService.cs` | 1149 | `SELECT COALESCE(SUM(Valor), 0) FROM ContasReceber WHERE Status = 'Pendente'` | `ContasReceber.Valor` | Idem |
| `FinanceiroDatabaseService.cs` | 1159 | `SELECT COALESCE(SUM(Valor), 0) FROM ContasPagar WHERE Status = 'Pendente'` | `ContasPagar.Valor` | Idem |
| `FinanceiroDatabaseService.cs` | 1169 | `SELECT COALESCE(SUM(Valor), 0) FROM ContasReceber WHERE DataVencimento < @hoje` | `ContasReceber.Valor` | Idem |
| `FinanceiroDatabaseService.cs` | 1354 | `SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras ...` | `MovimentacoesFinanceiras.Valor` | Idem |
| `FinanceiroDatabaseService.cs` | 1635 | `SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras ...` | `MovimentacoesFinanceiras.Valor` | Idem |
| `FinanceiroDatabaseService.cs` | 1653 | `SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras ...` | `MovimentacoesFinanceiras.Valor` | Idem |
| `FuncionarioOperationalService.cs` | 475 | `SELECT COALESCE(SUM(Total), 0) FROM Vendas WHERE Usuario = @usuario` | `Vendas.Total` | `SUM(Total)` retornará centavos. Consumidor deve dividir por `100m`. |
| `FuncionarioOperationalService.cs` | 507 | `SELECT COALESCE(SUM(TotalVendas), 0) FROM CaixaSessoes WHERE OperadorId = @id` | `CaixaSessoes.TotalVendas` | Idem |
| `RelatorioDatabaseService.cs` | 294 | `SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras ...` | `MovimentacoesFinanceiras.Valor` | Idem |
| `RelatorioDatabaseService.cs` | 409 | `SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras ...` | `MovimentacoesFinanceiras.Valor` | Idem |
| `DashboardViewModel.cs` | 345 | `SELECT COALESCE(SUM(Valor), 0) FROM ContasReceber ...` | `ContasReceber.Valor` | Idem |
| `DashboardViewModel.cs` | 447 | `SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras ...` | `MovimentacoesFinanceiras.Valor` | Idem |
| `DashboardViewModel.cs` | 456 | `SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras ...` | `MovimentacoesFinanceiras.Valor` | Idem |

> [!IMPORTANT]
> **ATENÇÃO À AGREGAÇÃO SQL:**
> A query `SELECT SUM(Valor) FROM ContasReceber` executada sobre uma coluna de centavos retornará, por exemplo, `98020` (centavos).
> **NÃO** alterar as queries para `SELECT SUM(Valor) / 100.0` em SQL sem sincronizar com o consumidor, pois se o consumidor aplicar `/ 100m`, o valor final ficaria dividido por 10.000!
> A política arquitetural do PRIMOX define que o SQL agrega centavos (`long`) e o mapeamento C# converte `Convert.ToDecimal(scalar) / 100m`.

---

## 4. MAPEAMENTO DE RELATÓRIOS E DASHBOARD (SEM IMPACTO VISUAL NA UI)

### 4.1 Contrato da Camada de Apresentação (UI / ViewModels)
A interface do usuário do PRIMOX Workshop (WPF / XAML) consome estritamente propriedades do tipo `decimal` nos ViewModels e DTOs:
* `ProdutoModel.PrecoVenda: decimal`
* `OrcamentoModel.Total: decimal`
* `OrdemServicoModel.ValorMaoObra: decimal`
* `VendaModel.Total: decimal`
* `CaixaSessaoModel.ValorAbertura: decimal`

A formatação para exibição ao usuário final utiliza conversores de cultura `pt-BR` ou strings de formatação padrão:
```csharp
// Exibição na View:
Text="{Binding Total, StringFormat='C2'}" // Renderiza: R$ 1.234,56
```

### 4.2 Garantia de Invariância Visual
Como a camada de serviço/repositório entrega `decimal` (`1234.56m`), a UI Desktop nunca recebe centavos inteiros (`123456`) e nunca exibe frações distorcidas (`R$ 12,35`). A experiência visual e operacional do operador da oficina permanece **100% idêntica e indistinguível**.

---

## 5. AUDITORIA DE CLAUSULAS `WHERE` E `ORDER BY`

* **Cláusulas `WHERE`:** Não foram encontradas cláusulas SQL raw com filtros literais monetários (ex: `WHERE Valor > 10.50`). Todos os filtros são efetuados por IDs, status, datas ou via in-memory LINQ em objetos de domínio já convertidos para `decimal`.
* **Cláusulas `ORDER BY`:** Ordenações por colunas monetárias (ex: `ORDER BY PrecoVenda DESC`) operam com ainda maior performance e precisão em `INTEGER` do que em `REAL`.

---

## 6. STATUS E POLÍTICA DE CONTENÇÃO

Em cumprimento à instrução número 23:
* **NENHUMA alteração definitiva foi realizada nos repositórios da aplicação nesta fase.**
* Este documento serve como **catálogo formal de impacto e plano de execução** para quando o gate de migração real for deliberadamente aprovado.
