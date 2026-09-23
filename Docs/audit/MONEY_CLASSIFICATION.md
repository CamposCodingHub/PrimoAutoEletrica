# PRIMOX Workshop — CLASSIFICAÇÃO MONEY + BLAST RADIUS

> Estágio: **CLASSIFICAÇÃO** (pipeline Money)
> Status anterior: INVENTÁRIO ✅
> Status atual: CLASSIFICAÇÃO ✅ + BLAST RADIUS ✅
> Próximo: POLÍTICA MONETÁRIA → ESTRATÉGIA DE MIGRATION → TESTES → REVIEW → AUTORIZAÇÃO → MIGRATION
>
> ⛔ **NENHUMA alteração de schema, tabela, coluna ou dado foi executada neste documento.**

---

## ACHADO POSITIVO

Os **modelos C#** (`Models/*.cs`) já usam `decimal` para todos os campos monetários.
O problema está **exclusivamente** na camada de persistência SQLite (REAL = IEEE 754 double).

```
SQLite REAL (double) → reader.GetDouble() → (decimal) cast → Model.Propriedade (decimal)
```

Isso simplifica a migration futura: **apenas a camada de leitura/escrita precisa mudar**, não a lógica de negócio nem os modelos.

---

## 1. CLASSIFICAÇÃO DAS COLUNAS REAL (82 encontradas)

### 🔴 FINANCEIRO_CRÍTICO (18 colunas)
> Afetam cobranças, pagamentos, caixa, contas a pagar/receber. Erro aqui = prejuízo financeiro real.

| Tabela | Coluna | Arquivo Schema | Linha |
|---|---|---|---|
| Vendas | Total | DatabaseService.cs | 562 |
| Vendas | Desconto | DatabaseService.cs | 564 |
| VendaItens | PrecoUnitario | DatabaseService.cs | 582 |
| VendaItens | CustoUnitario | DatabaseService.cs | 583 |
| VendaItens | Desconto | DatabaseService.cs | 584 |
| VendaItens | Subtotal | DatabaseService.cs | 585 |
| Caixas | ValorAbertura | DatabaseService.Migrations.cs | 508 |
| Caixas | ValorEsperado | DatabaseService.Migrations.cs | 509 |
| Caixas | ValorInformadoFechamento | DatabaseService.Migrations.cs | 510 |
| Caixas | TotalVendas | DatabaseService.Migrations.cs | 511 |
| Caixas | TotalSangrias | DatabaseService.Migrations.cs | 512 |
| Caixas | TotalSuprimentos | DatabaseService.Migrations.cs | 513 |
| CaixaFechamentos | ValorMovimento | DatabaseService.Migrations.cs | 528 |
| CaixaFechamentos | ValorInicial | DatabaseService.Migrations.cs | 529 |
| CaixaFechamentos | ValorFinal | DatabaseService.Migrations.cs | 530 |
| CaixaFechamentos | Sangrias | DatabaseService.Migrations.cs | 531 |
| CaixaFechamentos | Suprimentos | DatabaseService.Migrations.cs | 532 |
| CaixaFechamentos | Diferenca | DatabaseService.Migrations.cs | 533 |

### 🟡 FINANCEIRO_CALCULADO (18 colunas)
> Derivados de cálculos. Erro acumula de itens → totais.

| Tabela | Coluna | Arquivo Schema | Linha |
|---|---|---|---|
| OrdensServico | ValorTotal | DatabaseService.cs | 421 |
| OrdensServico | ValorProdutos | DatabaseService.cs | 422 |
| OrdensServicoItens | Quantidade | DatabaseService.cs | 447 |
| OrdensServicoItens | ValorUnitario | DatabaseService.cs | 448 |
| OrdensServicoItens | ValorTotal | DatabaseService.cs | 449 |
| Orcamentos | Subtotal | OrcamentoDatabaseService.cs | (schema) |
| Orcamentos | Desconto | OrcamentoDatabaseService.cs | (schema) |
| Orcamentos | DescontoPercentual | OrcamentoDatabaseService.cs | (schema) |
| Orcamentos | Acrescimo | OrcamentoDatabaseService.cs | (schema) |
| Orcamentos | Total | OrcamentoDatabaseService.cs | (schema) |
| Orcamentos | MargemLucro | OrcamentoDatabaseService.cs | (schema) |
| Orcamentos | LucroEstimado | OrcamentoDatabaseService.cs | (schema) |
| Orcamentos | ComissaoVendedor | OrcamentoDatabaseService.cs | (schema) |
| Orcamentos | ImpostosEstimados | OrcamentoDatabaseService.cs | (schema) |
| OrcamentoItens | PrecoUnitario | OrcamentoDatabaseService.cs | (schema) |
| OrcamentoItens | PrecoCusto | OrcamentoDatabaseService.cs | (schema) |
| OrcamentoItens | Subtotal | OrcamentoDatabaseService.cs | (schema) |
| OrcamentoItens | Desconto | OrcamentoDatabaseService.cs | (schema) |

### 🟠 AGENDAMENTO (9 colunas)
> Valores financeiros de agendamentos. Afetam previsão e cobranças se vinculados a OS.

| Tabela | Coluna | Arquivo Schema | Linha |
|---|---|---|---|
| Agendamentos | ClienteTotalGasto | AgendamentoDatabaseService.cs | 62 |
| Agendamentos | ValorEstimado | AgendamentoDatabaseService.cs | 82 |
| Agendamentos | ValorReal | AgendamentoDatabaseService.cs | 83 |
| Agendamentos | ValorPago | AgendamentoDatabaseService.cs | 84 |
| Agendamentos | ValorProdutos | AgendamentoDatabaseService.cs | 94 |
| Agendamentos | ValorServicos | AgendamentoDatabaseService.cs | 95 |
| AgendamentoItens | PrecoUnitario | AgendamentoDatabaseService.cs | 127 |
| AgendamentoItens | PrecoTotal | AgendamentoDatabaseService.cs | 128 |
| AgendamentoPagamentos | Valor | AgendamentoDatabaseService.cs | 142 |

### 🔵 ESTOQUE_PREÇO (6 colunas)
> Preços de compra/venda. Afetam margem e custos.

| Tabela | Coluna | Arquivo Schema | Linha |
|---|---|---|---|
| Produtos | PrecoCompra | DatabaseService.cs | 376 |
| Produtos | PrecoVenda | DatabaseService.cs | 377 |
| Produtos | MargemLucro | DatabaseService.cs | 378 |
| Produtos | ValorTotalEstoque | DatabaseService.cs | 379 |
| Clientes | TotalGasto | DatabaseService.cs | 335 |
| OrdensServico | TotalFaturado | DatabaseService.cs | 403 |

### 🟣 FORNECEDORES (5 colunas)
> Rastreamento de compras. Afetam custo.

| Tabela | Coluna | Arquivo Schema | Linha |
|---|---|---|---|
| Fornecedores | PedidoMinimo | DatabaseService.cs | 487 |
| Fornecedores | TotalCompras | DatabaseService.cs | 495 |
| FornecedoresProdutos | PrecoUltimaCompra | DatabaseService.cs | 528 |
| FornecedoresProdutos | QuantidadeUltimaCompra | DatabaseService.cs | 529 |
| FornecedoresProdutos | ValorCompras | DatabaseService.cs | 532 |

### ⬜ RELATÓRIO (muitas colunas)
> Valores usados apenas para exibição/relatório. Não alteram dados de negócio.
> Modelos C# já usam `decimal`. Risco de imprecisão existe mas não corrompe dados persistidos.

| Tabela | Colunas |
|---|---|
| RelatorioIndicadores | ValorTotal |
| RelatorioMetas | MetaValor, ValorAtual, PercentualAtingido |
| RelatorioContas | Valor |

### ⬜ OPERACIONAL (3 colunas)
> Valores operacionais.

| Tabela | Coluna | Arquivo Schema | Linha |
|---|---|---|---|
| Funcionarios | Salario | DatabaseService.cs | 258 |
| ImportacoesItens | MargemAplicada | DatabaseService.Migrations.cs | 720 |
| ImportacoesItens | PrecoVendaSugerido | DatabaseService.Migrations.cs | 721 |

### ⬜ QUANTIDADE (2 colunas)
> Quantidades — não são monetárias. REAL pode ser aceitável, mas INTEGER é mais seguro.

| Tabela | Coluna | Nota |
|---|---|---|
| OrdensServicoItens | Quantidade | Pode ter frações (0.5 litros) |
| FornecedoresProdutos | QuantidadeUltimaCompra | Pode ter frações |

---

## 2. BLAST RADIUS POR DOMÍNIO

### 2.1 VENDAS (🔴 Crítico — 6 colunas)

| Camada | Arquivo | Impacto |
|---|---|---|
| Schema | DatabaseService.cs:562-585 | CREATE TABLE |
| CRUD | VendaService.cs:125,249,411,467 | INSERT, UPDATE, SELECT |
| Repositório | VendaRepository.cs | Leitura/escrita |
| Relatório | RelatorioDatabaseService.cs:278,323,388,657,764,818,892,956,1010 | 9 queries SELECT |
| Fiscal | ContabilExportService.cs:147 | Exportação |
| Smoke | UiSmokeTestService.Funcionarios.cs:461 | Seed |

**Blast radius: 6 arquivos, ~20 pontos de contato**

### 2.2 CAIXA (🔴 Crítico — 12 colunas)

| Camada | Arquivo | Impacto |
|---|---|---|
| Schema | DatabaseService.Migrations.cs:508-533 | CREATE TABLE (2 tabelas) |
| Service | CaixaService.cs | Abertura, fechamento, sangria, suprimento |
| Relatório | RelatorioDatabaseService.cs | Relatório de caixa |

**Blast radius: ~3 arquivos, ~15 pontos de contato**

### 2.3 ORÇAMENTOS (🟡 Calculado — 14 colunas)

| Camada | Arquivo | Impacto |
|---|---|---|
| Schema | OrcamentoDatabaseService.cs | CREATE TABLE (2 tabelas) |
| CRUD | OrcamentoDatabaseService.cs:351,432,480,513,571,869,935,962,981,985 | 10 queries |
| Relatório | RelatorioDatabaseService.cs:1401,1448,1462,1508,1523 | 5 queries |
| 360 | Primox360Service.cs | Agregações |
| Smoke | UiSmokeTestService.Assurance13.cs:611 | COUNT |

**Blast radius: ~5 arquivos, ~18 pontos de contato**

### 2.4 ORDENS DE SERVIÇO (🟡 Calculado — 5 colunas)

| Camada | Arquivo | Impacto |
|---|---|---|
| Schema | DatabaseService.cs:421-449 | CREATE TABLE (2 tabelas) |
| Service | DatabaseService.OrdensServico.cs | CRUD completo |
| 360 | Primox360Service.cs | Agregações |
| Relatório | RelatorioDatabaseService.cs | Múltiplas queries |

**Blast radius: ~4 arquivos, ~15 pontos de contato**

### 2.5 AGENDAMENTOS (🟠 Médio — 9 colunas)

| Camada | Arquivo | Impacto |
|---|---|---|
| Schema | AgendamentoDatabaseService.cs:62-142 | CREATE TABLE (3 tabelas) |
| CRUD | AgendamentoDatabaseService.cs | CRUD completo |

**Blast radius: ~1 arquivo concentrado**

### 2.6 PRODUTOS/ESTOQUE (🔵 Preço — 4 colunas)

| Camada | Arquivo | Impacto |
|---|---|---|
| Schema | DatabaseService.cs:376-379 | CREATE TABLE |
| CRUD | DatabaseService.Produtos.cs | CRUD |
| Relatório | RelatorioDatabaseService.cs | Margem, estoque |
| Importação | ImportacaoService.cs | Importação de catálogos |
| Persistence Test | DatabasePersistenceTestService.cs:193 | Test seed |

**Blast radius: ~5 arquivos**

---

## 3. ORDEM DE MIGRATION RECOMENDADA

> [!CAUTION]
> Esta é apenas uma recomendação. A execução requer aprovação formal nos estágios subsequentes.

| Ordem | Domínio | Colunas | Risco | Justificativa |
|---|---|---|---|---|
| 1 | Vendas + VendaItens | 6 | 🔴 ALTO | Impacto financeiro direto, primeiro a corrigir |
| 2 | Caixas + CaixaFechamentos | 12 | 🔴 ALTO | Caixa precisa bater 100% |
| 3 | Orçamentos + OrcamentoItens | 14 | 🟡 MÉDIO | Volume alto, muitas queries |
| 4 | OrdensServico + OrdensServicoItens | 5 | 🟡 MÉDIO | Core do negócio |
| 5 | Agendamentos + AgendamentoItens + Pagamentos | 9 | 🟠 MÉDIO | Concentrado em 1 service |
| 6 | Produtos | 4 | 🔵 BAIXO | Preços — menor frequência de escrita |
| 7 | Fornecedores | 5 | 🟣 BAIXO | Histórico de compras |
| 8 | Funcionários (Salário) | 1 | ⬜ BAIXO | Single column |
| 9 | Relatórios | ~5 | ⬜ MÍNIMO | Read-only, não persiste |
| 10 | Importações | 2 | ⬜ MÍNIMO | Scaffold |

---

## 4. RESUMO

| Métrica | Valor |
|---|---|
| Total de colunas REAL | **82** |
| FINANCEIRO_CRÍTICO | **18** (22%) |
| FINANCEIRO_CALCULADO | **18** (22%) |
| AGENDAMENTO | **9** (11%) |
| ESTOQUE_PREÇO | **6** (7%) |
| FORNECEDORES | **5** (6%) |
| RELATÓRIO/OPERACIONAL | **~24** (29%) |
| QUANTIDADE (não monetário) | **2** (3%) |
| Arquivos de service afetados | **~15** |
| Modelos C# impactados | **0** (já usam decimal ✅) |
| Lógica de negócio impactada | **0** (MoneyCents opera em decimal ✅) |
| Pontos de contato (CRUD + relatório) | **~80-100 queries** |

---

## 5. PRÓXIMO ESTÁGIO: POLÍTICA MONETÁRIA

Definir antes da migration:
1. **Arredondamento**: MidpointRounding.AwayFromZero confirmado ✅
2. **Centavos vs milésimos**: centavos (2 casas) — adequado para BRL
3. **Tipo de coluna destino**: INTEGER (centavos) no SQLite
4. **Dual-write ou single-write**: dual-write (REAL + INTEGER) durante transição
5. **Rollback**: manter colunas REAL por N releases após migration
6. **Validação**: soma(INTEGER) / 100 == soma(REAL) ± 0.01 por tabela
