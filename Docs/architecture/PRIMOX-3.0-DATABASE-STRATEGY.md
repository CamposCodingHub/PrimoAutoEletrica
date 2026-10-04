# PRIMOX 3.0 — DATABASE PERSISTENCE & PAGINATION STRATEGY
**Data:** 04 de Outubro de 2026  
**Versão:** 3.0.0-DATABASE  
**Status:** ESPECIFICAÇÃO DE PERSISTÊNCIA, OTIMIZAÇÃO DE QUERIES E PAGINAÇÃO SERVER-SIDE  
**Objetivo:** Eliminar o gargalo de memória de `ObterTodos()` e `SELECT *`, assegurando estabilidade para oficinas com mais de 100.000 registros.

---

## 1. O DIAGNÓSTICO DAS CONSULTAS ATUAIS

A auditoria do código revelou práticas de consulta que colocam o software em risco severo de crash (`OutOfMemoryException`) à medida que a oficina cresce:

* **160 métodos `ObterTodos()` em memória:** Ao abrir a tela de Estoque, o sistema tenta ler toda a tabela de produtos para montar uma `List<Produto>` em memória RAM, incluindo descrição técnica, fornecedores e histórico.
* **90 ocorrências de `SELECT *`:** Tabelas com dezenas de colunas e dados binários (fotos, comprovantes de assinatura) são baixadas integralmente mesmo quando a tela necessita apenas de `Id`, `Nome` e `Preco`.
* **Acesso Direto ao Banco em Code-Behinds:** Diversas janelas XAML instanciam comandos SQL diretamente em vez de consultar a camada de Repositórios.

---

## 2. A ESTRATÉGIA DE BANCO DE DADOS: LOCAL VS. NUVEM

```
┌────────────────────────────────────────┐       ┌────────────────────────────────────────┐
│             ESTAÇÃO LOCAL              │       │          SERVIDOR CENTRAL / NUVEM      │
│  SQLite (primoauto.db)                 │       │  PostgreSQL 16+ / SQL Server 2022      │
├────────────────────────────────────────┤       ├────────────────────────────────────────┤
│ • Zero configuração para a oficina     │       │ • Concorrência multi-conexão ilimitada │
│ • 100% offline-first                   │ ◄───► │ • Backup contínuo (WAL archiving)      │
│ • Transações ACID locais ultrarrápidas │       │ • Relatórios analíticos e IA de rede   │
│ • Armazenamento embarcado seguro       │       │ • Multi-filial consolidado             │
└────────────────────────────────────────┘       └────────────────────────────────────────┘
```

---

## 3. PADRÃO UNIVERSAL DE PAGINAÇÃO SERVER-SIDE

Todas as consultas de listagem operacional adotarão o contrato genérico `PagedResult<T>`:

```csharp
namespace PRIMOX.Application.Common.Pagination
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items ?? Array.Empty<T>();
            TotalCount = totalCount;
            PageNumber = Math.Max(1, pageNumber);
            PageSize = Math.Max(1, pageSize);
        }

        public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 25) =>
            new(Array.Empty<T>(), 0, pageNumber, pageSize);
    }

    public record PaginationParams(int PageNumber = 1, int PageSize = 25, string? SearchTerm = null);
}
```

---

## 4. PADRONIZAÇÃO DE CONSULTAS: DIRETRIZES DE REATORAÇÃO

### 4.1 Substituição de `SELECT *` por Projeções Dedicadas
Toda listagem utilizará DTOs específicos de projeção (`ClienteListItemDto`, `ProdutoListItemDto`):

```sql
-- ANTES (Inseguro e Ineficiente):
SELECT * FROM Produtos WHERE DataExclusao IS NULL;

-- DEPOIS (Otimizado com Paginação):
SELECT 
    p.Id, 
    p.Codigo, 
    p.Nome, 
    p.EstoqueAtual, 
    p.PrecoVenda, 
    p.Categoria
FROM Produtos p
WHERE p.DataExclusao IS NULL
  AND (@SearchTerm IS NULL OR p.Nome LIKE @SearchTerm OR p.Codigo LIKE @SearchTerm)
ORDER BY p.Nome ASC
LIMIT @PageSize OFFSET @Offset;
```

### 4.2 Query de Contagem Associada
```sql
SELECT COUNT(*) 
FROM Produtos 
WHERE DataExclusao IS NULL
  AND (@SearchTerm IS NULL OR Nome LIKE @SearchTerm OR Codigo LIKE @SearchTerm);
```

---

## 5. TELAS COM PRIORIDADE MÁXIMA DE MIGRAÇÃO PARA PAGINAÇÃO

1. **Ordens de Serviço (`OrdensServicoControl.xaml`):**
   * Volume esperado: 20.000 a 100.000 OSs.
   * Filtro padrão: Apenas OSs abertas nos últimos 30 dias por padrão. Paginação por lote de 50.
2. **Estoque de Produtos (`EstoqueControl.xaml`):**
   * Volume esperado: 5.000 a 40.000 itens (peças elétricas, fusíveis, lâmpadas, conectores).
   * Paginação server-side por lote de 30 com pesquisa textual indexada.
3. **Cadastro de Clientes (`ClientesControl.xaml`):**
   * Volume esperado: 10.000 a 50.000 clientes.
   * Busca por CPF/CNPJ, Telefone e Nome com paginação de 25 registros.
4. **Histórico de Veículos (`VeiculosControl.xaml`):**
   * Busca por Placa normalizada com histórico de OS paginado.
5. **Livro Caixa e Vendas PDV (`FinanceiroControl.xaml`):**
   * Paginação estrita por período de data e turno de caixa.

---

## 6. ABSTRAÇÃO DE TEMPO (`ITimeProvider`)

Para encerrar o espalhamento de 686 ocorrências de `DateTime.Now`, introduz-se a porta de relógio de domínio:

```csharp
namespace PRIMOX.Application.Common
{
    public interface ITimeProvider
    {
        DateTimeOffset UtcNow { get; }
        DateTimeOffset LocalNow { get; }
        TimeZoneInfo LocalTimeZone { get; }
    }
}
```

O banco armazenará sempre `DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")`, eliminando ambiguidades em relatórios fiscais e sincronizações entre filiais de diferentes estados.
