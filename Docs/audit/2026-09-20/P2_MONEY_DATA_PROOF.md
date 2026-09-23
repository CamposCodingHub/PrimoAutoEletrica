# PRIMOX Workshop — PROVA DE DADOS REAIS (MONEY AUDIT)

> **Documento:** P2_MONEY_DATA_PROOF.md  
> **Data:** 23/09/2026  
> **Status:** AUDITORIA READ-ONLY FINALIZADA  
> **Escopo:** Inspeção física dos dados reais no banco ativo SQLite (`primoauto.db`)  
> **Garantia:** NENHUM dado alterado. Conexão estritamente `mode=ro` (read-only).  

---

## 1. AMBIENTE E BANCO ANALISADO

| Item | Detalhe |
|---|---|
| **Caminho do Banco** | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db` |
| **Modo de Abertura** | `file:C:/Users/campo/AppData/Local/PrimoAutoEletrica/primoauto.db?mode=ro` |
| **Tamanho do Arquivo** | 20.013.056 bytes (~19.08 MB) |
| **Total de Tabelas** | 59 tabelas |
| **Engine de Consulta** | Python 3.14.6 + sqlite3 (PRAGMA table_info e SELECTs analíticos) |

---

## 2. RESULTADOS DA INSPEÇÃO DOS 36 CAMPOS FINANCEIROS

### 2.1 Resumo Geral das Anomalias

| Verificação | Critério | Ocorrências Encontradas | Amostra / Observação |
|---|---|:---:|---|
| **Valores com mais de 2 casas decimais** | `len(dec_part) > 2` com resíduo real | **0** | Nenhuma ocorrência encontrada. |
| **Resíduos de Ponto Flutuante (IEEE 754)** | `abs(val - round(val, 2)) > 1e-7` | **0** | Nenhuma ocorrência encontrada. |
| **Valores Negativos Inválidos** | `val < 0` em campos estritamente positivos | **0** | Nenhuma ocorrência encontrada. |
| **Valores Excessivamente Grandes** | `abs(val) > 1.000.000,00` | **0** | Nenhuma ocorrência encontrada. |
| **Valores Próximos de Zero Inválidos** | `0 < abs(val) < 0,005` (fração de centavo) | **0** | Nenhuma ocorrência encontrada. |
| **NULL em colunas NOT NULL** | Violação de constraint | **0** | Nenhuma ocorrência encontrada. |

> [!NOTE]
> **Declaração Formal de Ausência de Anomalias:**  
> **“Nenhuma ocorrência encontrada.”**  
> Todos os valores reais armazenados nas tabelas ativas do SQLite possuem no máximo 2 casas decimais e representam centavos inteiros quando multiplicados por 100.

---

## 3. AUDITORIA DETALHADA POR CAMPO

### 🔴 18 CAMPOS FINANCEIRO_CRÍTICO

| Tabela | Coluna | Tipo SQLite | Qtd Reg. | Min | Max | Média | Qtd NULL | Qtd Zero | Qtd Neg. | Max Decimais | Resíduos FP |
|---|---|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| `Vendas` | Total | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `Vendas` | Desconto | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `VendaItens` | PrecoUnitario | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `VendaItens` | CustoUnitario | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `VendaItens` | Desconto | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `VendaItens` | Subtotal | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `CaixaSessoes` | ValorAbertura | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `CaixaSessoes` | ValorEsperado | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `CaixaSessoes` | ValorInformadoFechamento | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `CaixaSessoes` | TotalVendas | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `CaixaSessoes` | TotalSangrias | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `CaixaSessoes` | TotalSuprimentos | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `MovimentacoesCaixa` | ValorMovimento | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `MovimentacoesCaixa` | ValorInicial | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `MovimentacoesCaixa` | ValorFinal | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `MovimentacoesCaixa` | Sangrias | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `MovimentacoesCaixa` | Suprimentos | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |
| `MovimentacoesCaixa` | Diferenca | REAL | 0 | — | — | — | 0 | 0 | 0 | 0 | 0 |

*Nota de auditoria:* As tabelas de `Vendas`, `VendaItens`, `CaixaSessoes` e `MovimentacoesCaixa` estão criadas e estruturadas no banco SQLite, mas atualmente não contêm linhas persistidas no ambiente de desenvolvimento/homologação local. Também foram verificados 10 bancos de backup em `AppData/Local/PrimoAutoEletrica/Backups/`, todos confirmando ausência de linhas remanescentes nessas tabelas específicas.

---

### 🟡 18 CAMPOS FINANCEIRO_CALCULADO

| Tabela | Coluna | Tipo SQLite | Qtd Reg. | Min | Max | Média | Qtd NULL | Qtd Zero | Qtd Neg. | Max Decimais | Resíduos FP |
|---|---|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| `OrdensServico` | ValorMaoObra | REAL | 9 | 0,00 | 220,00 | 104,44 | 0 | 1 | 0 | 2 | 0 |
| `OrdensServico` | Desconto | REAL | 9 | 0,00 | 0,00 | 0,00 | 0 | 9 | 0 | 0 | 0 |
| `OrdemServicoItens` | Quantidade | REAL | 16 | 1,00 | 4,00 | 1,31 | 0 | 0 | 0 | 0 | 0 |
| `OrdemServicoItens` | ValorUnitario | REAL | 16 | 1,20 | 150,00 | 39,95 | 0 | 0 | 0 | 2 | 0 |
| `OrdemServicoItens` | CustoUnitario | REAL | 16 | 0,00 | 17,98 | 1,92 | 0 | 14 | 0 | 2 | 0 |
| `Orcamentos` | Subtotal | REAL | 20 | 80,00 | 480,00 | 213,50 | 0 | 0 | 0 | 2 | 0 |
| `Orcamentos` | Desconto | REAL | 20 | 0,00 | 0,00 | 0,00 | 0 | 20 | 0 | 0 | 0 |
| `Orcamentos` | DescontoPercentual | REAL | 20 | 0,00 | 0,00 | 0,00 | 0 | 20 | 0 | 0 | 0 |
| `Orcamentos` | Acrescimo | REAL | 20 | 0,00 | 0,00 | 0,00 | 0 | 20 | 0 | 0 | 0 |
| `Orcamentos` | Total | REAL | 20 | 80,00 | 480,00 | 213,50 | 0 | 0 | 0 | 2 | 0 |
| `Orcamentos` | MargemLucro | REAL | 20 | 100,00 | 100,00 | 100,00 | 0 | 0 | 0 | 0 | 0 |
| `Orcamentos` | LucroEstimado | REAL | 20 | 80,00 | 480,00 | 213,50 | 0 | 0 | 0 | 2 | 0 |
| `Orcamentos` | ComissaoVendedor | REAL | 20 | 0,00 | 6,00 | 0,75 | 0 | 16 | 0 | 2 | 0 |
| `Orcamentos` | ImpostosEstimados | REAL | 20 | 0,00 | 21,60 | 2,70 | 0 | 16 | 0 | 2 | 0 |
| `OrcamentoItens` | PrecoUnitario | REAL | 39 | 0,00 | 480,00 | 109,49 | 0 | 4 | 0 | 2 | 0 |
| `OrcamentoItens` | PrecoCusto | REAL | 39 | 0,00 | 0,00 | 0,00 | 0 | 39 | 0 | 0 | 0 |
| `OrcamentoItens` | Desconto | REAL | 39 | 0,00 | 0,00 | 0,00 | 0 | 39 | 0 | 0 | 0 |
| `OrcamentoItens` | Subtotal | REAL | 39 | 0,00 | 960,00 | 133,08 | 0 | 4 | 0 | 2 | 0 |

---

### 🟢 DADOS COMPLEMENTARES POPULADOS

Para validação cruzada do comportamento dos dados no banco ativo:

| Tabela | Coluna | Tipo SQLite | Qtd Reg. | Min | Max | Média | Observação |
|---|---|---|:---:|:---:|:---:|:---:|---|
| `Produtos` | PrecoCompra | REAL | 55 | 0,00 | 450,00 | 32,85 | Preços de custo cadastrados |
| `Produtos` | PrecoVenda | REAL | 55 | 0,00 | 720,00 | 56,12 | Preços de venda ao consumidor |
| `ContasPagar` | Valor | DECIMAL (NUMERIC) | 2 | 5.412,75 | 5.412,75 | 5.412,75 | Lançamento financeiro real |
| `ContasReceber` | Valor | DECIMAL (NUMERIC) | 6 | 80,00 | 380,20 | 213,40 | Lançamentos de clientes |
| `MovimentacoesFinanceiras` | Valor | DECIMAL (NUMERIC) | 9 | 80,00 | 5.412,75 | 870,41 | Histórico financeiro consolidado |

---

## 4. DISTRIBUIÇÃO E CONVERSÃO DOS TIPOS NO SQLITE

No SQLite, colunas declaradas como `REAL` armazenam `float` (IEEE 754 de 64 bits).  
Ao consultar `typeof([coluna])`, foi verificado:
- Todos os registros numéricos em tabelas `REAL` retornam `real` ou `integer` (quando SQLite otimiza 0 ou inteiros).
- Nenhum campo continha representação em `text` ou `blob` corrompido.
- Todas as conversões de teste `CAST(ROUND(coluna * 100) AS INTEGER)` produziram valores inteiros exatos sem perda de precisão e sem truncamento.
