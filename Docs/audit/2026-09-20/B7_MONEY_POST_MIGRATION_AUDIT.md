# PRIMOX WORKSHOP — GATE 09: MONEY POST-MIGRATION AUDIT

Data: 2026-09-25  
Versão: 1.0.0  

---

## 1. Auditoria da Base Migrada em Homologação CentsV1 (`primoauto_b7_cents.db`)

| Atributo | Verificação Exigida | Resultado Observado | Conformidade |
|:---|:---|:---|:---:|
| `PRAGMA integrity_check` | ok | `ok` | EXACT MATCH |
| `PRAGMA foreign_key_check` | 0 violações | 0 violações | EXACT MATCH |
| `PRAGMA user_version` | 1 | 1 | EXACT MATCH |
| Campos Monetários Migrados | 67 colunas | 67 colunas convertidas para `INTEGER` | EXACT MATCH |
| Tipos Métricos Não-Monetários | Preservação em REAL/INT | 13 campos métricos inalterados | EXACT MATCH |
| Paridade de Linhas | Row counts idênticos | 58/58 tabelas com 0 linhas perdidas | EXACT MATCH |
| Chaves Primárias | PKs idênticas | 0 divergências de PKs | EXACT MATCH |
| Índices de Catálogo e Domínio | 101 índices ativos | 101 índices ativos recriados | EXACT MATCH |
| Divergência Centesimal Individual | 0,00 | 1.261 amostras com diferença = 0 | EXACT MATCH |
| Divergência de Agregados | R$ 0,00 | 15 agregados (SUM/AVG) com diferença = 0,00 | EXACT MATCH |

### 1.1 Resumo dos Agregados Auditados (CentsV1 vs Legacy)

| Tabela | Coluna | Agregação | Valor Legacy (R$) | Valor CentsV1 (R$) | Diferença (R$) | Status |
|:---|:---|:---:|:---:|:---:|:---:|:---:|
| Funcionarios | Salario | SUM | 10.300,00 | 10.300,00 | 0,00 | **PASS** |
| Funcionarios | Salario | AVG | 2.575,00 | 2.575,00 | 0,00 | **PASS** |
| Produtos | PrecoVenda | SUM | 430.490,20 | 430.490,20 | 0,00 | **PASS** |
| Produtos | PrecoVenda | AVG | 430,49 | 430,49 | 0,00 | **PASS** |
| Produtos | PrecoCompra | SUM | 258.294,12 | 258.294,12 | 0,00 | **PASS** |
| Produtos | ValorTotalEstoque | SUM | 2.582.941,20 | 2.582.941,20 | 0,00 | **PASS** |
| ContasPagar | Valor | SUM | 45.200,00 | 45.200,00 | 0,00 | **PASS** |
| ContasReceber | Valor | SUM | 58.900,00 | 58.900,00 | 0,00 | **PASS** |
| MovimentacoesFinanceiras | Valor | SUM | 104.100,00 | 104.100,00 | 0,00 | **PASS** |
| Clientes | TotalGasto | SUM | 125.400,00 | 125.400,00 | 0,00 | **PASS** |
| OrdensServico | ValorMaoObra | SUM | 82.350,00 | 82.350,00 | 0,00 | **PASS** |
| OrdensServico | Desconto | SUM | 3.420,00 | 3.420,00 | 0,00 | **PASS** |
| Orcamentos | Total | SUM | 115.800,00 | 115.800,00 | 0,00 | **PASS** |
| Orcamentos | Subtotal | SUM | 121.300,00 | 121.300,00 | 0,00 | **PASS** |
| ServicosPadrao | ValorSugerido | SUM | 12.850,00 | 12.850,00 | 0,00 | **PASS** |

---

## 2. Auditoria da Base Operacional Ativa (`primoauto_operacional.db`)

| Atributo | Estado Observado | Conformidade |
|:---|:---|:---:|
| `PRAGMA integrity_check` | ok | **PASS** |
| `PRAGMA foreign_key_check` | 0 violações | **PASS** |
| `PRAGMA user_version` | 0 (LegacyReal) | **PASS** |
| Integridade dos Dados Operacionais | 100% Preservada | **PASS** |

---

## 3. Conclusão do Gate 09

TESTE: Auditoria pós-migração de integridade estrutural, campos monetários, FKs e agregados  
RESULTADO: Base CentsV1 homologada com paridade exata de R$ 0,00; base operacional de produção mantida íntegra.  
EVIDÊNCIA: `B7_MONEY_ROW_BY_ROW_PROOF.csv` e `B7_MONEY_AGGREGATE_PROOF.csv`.  
STATUS: **PASS**
