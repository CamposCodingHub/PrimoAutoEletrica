# PRIMOX Workshop — DATA INTEGRITY AUDIT

---

## 1. Relacionamentos

### 1.1 Associação por ID ✅
- `Primox360Service`: usa `ClienteId` para contas a receber, orçamentos, vendas
- `HistoricoClienteWindow`: matching por nome removido (documentado line 310-312)
- `FinanceiroDatabaseService.ResolverClienteIdPorNome`: recusa homônimos (`matches.Count == 1`)

### 1.2 Risco Residual ⚠️
- Verificar se existem outras views/services que ainda usam comparação de nome para associação de dados
- Recomendação: grep codebase por `.Nome.Contains`, `.Nome ==` em contextos de join financeiro/OS

## 2. Campos Monetários — REAL (IEEE 754)

### 2.1 Inventário de Colunas REAL

| Tabela | Colunas REAL |
|---|---|
| Orcamentos | Subtotal, Desconto, DescontoPercentual, Acrescimo, Total, MargemLucro, LucroEstimado, ComissaoVendedor, ImpostosEstimados |
| OrcamentoItens | PrecoUnitario, PrecoCusto, Subtotal, Desconto |
| Agendamentos | ClienteTotalGasto, ValorEstimado, ValorReal, ValorPago, ValorProdutos, ValorServicos |
| AgendamentoItens | PrecoUnitario, PrecoTotal |
| AgendamentoPagamentos | Valor |
| Fornecedores | PrecoUltimaCompra, QuantidadeUltimaCompra, ValorCompras |
| Caixas | ValorAbertura, ValorEsperado, ValorInformadoFechamento, TotalVendas, TotalSangrias, TotalSuprimentos |
| CaixaFechamentos | ValorMovimento, ValorInicial, ValorFinal, Sangrias, Suprimentos, Diferenca |
| ImportacoesItens | MargemAplicada, PrecoVendaSugerido |
| VendaItens | PrecoUnitario, CustoUnitario, Desconto, Subtotal |
| RelatorioIndicadores | ValorTotal |
| RelatorioMetas | MetaValor, ValorAtual, PercentualAtingido |
| RelatorioContas | Valor |

**Total: 50+ colunas monetárias como REAL.**

### 2.2 Fundação MoneyCents
- `MoneyCents` struct existe com aritmética em centavos inteiros
- `FromDecimal()`, `FromDouble()`, `ToDecimal()`, `ApplyPercentDiscount()`
- `MoneyCentsTests` + `MoneyCentsExpandedTests` cobrem edge cases
- **Migração de colunas: NÃO FEITA** (honestamente documentada no código fonte)

### 2.3 Plano de Migração Recomendado
1. Criar migration que adiciona colunas `_cents INTEGER` ao lado das existentes
2. Popular colunas cents com `CAST(valor * 100 + 0.5 AS INTEGER)`
3. Validar soma das colunas cents = esperado
4. Código passa a ler de colunas cents
5. Após período de validação, deprecar colunas REAL
6. **NÃO remover colunas REAL até confirmar rollback seguro**

## 3. Migrations

### 3.1 Cadeia de Migrations
- `DatabaseService.Migrations.cs` contém ~27 ApplyMigration com SchemaVersion
- Migrations não devem ser removidas sem plano de compatibilidade
- Cada migration é idempotente (verifica existência antes de alterar)

### 3.2 Recomendação
- Documentar versão do schema em cada release
- Criar teste que valida cadeia completa: schema 0 → schema N
- `MigrationSchemaTests.cs` existe com validação básica

## 4. DateTime

### 4.1 Situação Atual
- 100+ arquivos usam `DateTime.Now` (local timezone)
- Alguns usam `DateTime.UtcNow` (API, tokens)
- Mistura inconsistente

### 4.2 Risco
- Multi-timezone (cloud, filiais em fusos diferentes)
- Sincronização entre máquinas
- Relatórios com datas inconsistentes

### 4.3 Recomendação
- Armazenamento: UTC sempre
- Apresentação: conversão para timezone configurada da oficina
- API: UTC com ISO 8601
