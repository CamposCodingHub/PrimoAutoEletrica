# PRIMOX Workshop — FASE 2.1: MONEY CLASSIFICATION GATE

> **Documento:** P2_1_MONEY_CLASSIFICATION_GATE.md  
> **Data:** 23/09/2026  
> **Status:** AUDITORIA E RECLASSIFICAÇÃO SEMÂNTICA CONCLUÍDA  
> **Estado Operacional:** ⛔ **MONEY MIGRATION = BLOCKED**  
>
> ⚠️ **NENHUMA ALTERAÇÃO DE SCHEMA OU DADOS REALIZADA:**  
> • Sem `ALTER TABLE`  
> • Sem `CREATE TABLE` de migration  
> • Sem `UPDATE` financeiro  
> • Sem conversão de dados  
> • Sem backfill ou renomeação  
> • Branch `main` permanece 100% intocada  

---

## 1. OBJETIVO E REGRA PRINCIPAL DA FASE 2.1

O objetivo desta fase foi revisar e sanear a classificação dos campos com tipos flutuantes (`REAL`, `FLOAT`, `DOUBLE`, `DECIMAL`) no SQLite.

> [!IMPORTANT]
> **REGRA DE OURO SEMÂNTICA:**  
> **“REAL” no SQLite NÃO significa automaticamente “dinheiro”.**  
> Somente campos com natureza estritamente monetária (`MONEY` e `MONEY_DERIVED`) devem entrar na estratégia de conversão para `INTEGER cents`.  
> Quantidades, percentuais, coordenadas geográficas, índices e relatórios transitórios **NÃO PODEM** ser convertidos para centavos inteiros.

---

## 2. CENSO E RECLASSIFICAÇÃO SEMÂNTICA DAS 80 COLUNAS

Todas as 80 colunas não-inteiras encontradas nas 59 tabelas do schema foram rigorosamente catalogadas e classificadas em 9 categorias semânticas exclusivas:

| Categoria Semântica | Quantidade | Definição | Entra na Estratégia `INTEGER cents`? |
|---|:---:|---|:---:|
| **1. `MONEY`** | **36** | Valores monetários pontuais em moeda corrente (BRL), não derivados diretamente no registro | **SIM** |
| **2. `MONEY_DERIVED`** | **31** | Valores monetários resultantes de cálculos aritméticos ou agregações de itens | **SIM** |
| **3. `PERCENTAGE`** | **6** | Taxas percentuais (0% a 100%, margens, descontos percentuais, metas) | ❌ **NÃO** (Permanece `REAL`) |
| **4. `QUANTITY`** | **3** | Quantidades de peças, unidades discretas ou horas de serviço | ❌ **NÃO** (Permanece `REAL`) |
| **5. `COORDINATE`** | **2** | Coordenadas geográficas GPS (latitude, longitude) | ❌ **NÃO** (Permanece `REAL`) |
| **6. `TRANSIENT/REPORT`** | **2** | Snapshots transitórios de impressão ou auditoria visual | ❌ **NÃO** (Tratamento sob demanda) |
| **7. `WEIGHT`** | **0** | Pesos físicos em kg/toneladas (inexistentes no schema atual) | — |
| **8. `OTHER_MEASURE`** | **0** | Outras grandezas físicas (pressão, voltagem) | — |
| **9. `UNKNOWN`** | **0** | Todos os campos foram 100% identificados | — |
| **TOTAL GERAL** | **80** | **Total de colunas não-inteiras no SQLite** | **67 Monetários / 13 Não-Monetários** |

O inventário linha a linha completo está disponível em:  
👉 [`Docs/audit/2026-09-20/P2_1_MONEY_CLASSIFICATION.csv`](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/P2_1_MONEY_CLASSIFICATION.csv).

---

## 3. REVISÃO OBRIGATÓRIA DE CAMPOS ESPECÍFICOS (CORREÇÕES DO DESENHO)

A auditoria semântica identificou e corrigiu campos que haviam sido agrupados preliminarmente sob escopo financeiro, mas que **NÃO SÃO DINHEIRO**:

1. **`OrdemServicoItens.Quantidade`**:  
   - *Classificação:* **`QUANTITY`** (Unidade: Peças / Horas de mão de obra).  
   - *Correção:* **NÃO converter para cents.** Quantidade de 1,5 horas de serviço ou 2 peças não são R$ 1,50 nem R$ 0,02. Manter como `REAL` (ou `INTEGER` se for contagem estritamente inteira de itens).
2. **`Orcamentos.DescontoPercentual`**:  
   - *Classificação:* **`PERCENTAGE`** (Unidade: %).  
   - *Correção:* **NÃO converter para cents.** Representa taxa de 0 a 100%. Manter como `REAL`.
3. **`Orcamentos.MargemLucro`**:  
   - *Classificação:* **`PERCENTAGE`** (Unidade: %).  
   - *Correção:* **NÃO converter para cents.** Representa proporção `(Lucro / Total) * 100`. Manter como `REAL`.
4. **`OrcamentoItens.MargemLucro`**:  
   - *Classificação:* **`PERCENTAGE`** (Unidade: %).  
   - *Correção:* **NÃO converter para cents.** Manter como `REAL`.
5. **`Produtos.MargemLucro`**:  
   - *Classificação:* **`PERCENTAGE`** (Unidade: %).  
   - *Correção:* **NÃO converter para cents.** Manter como `REAL`.
6. **`ImportacoesItens.MargemAplicada`**:  
   - *Classificação:* **`PERCENTAGE`** (Unidade: %).  
   - *Correção:* **NÃO converter para cents.** Manter como `REAL`.
7. **`ImportacoesItens.Quantidade`**:  
   - *Classificação:* **`QUANTITY`** (Unidade: Unidades do XML).  
   - *Correção:* **NÃO converter para cents.** Manter como `REAL`.
8. **`Metas.PercentualAtingido`**:  
   - *Classificação:* **`PERCENTAGE`** (Unidade: %).  
   - *Correção:* **NÃO converter para cents.** Manter como `REAL`.
9. **`Filiais.Latitude` e `Filiais.Longitude`**:  
   - *Classificação:* **`COORDINATE`** (Unidade: Graus decimais GPS).  
   - *Correção:* **NÃO converter para cents.** Manter como `REAL`.

---

## 4. TABELAS QUE REALMENTE EXIGEM MIGRATION MONETÁRIA

Somente tabelas que contêm colunas `MONEY` ou `MONEY_DERIVED` participarão da futura migração de schema:

### Tabelas Que Exigem Migração (15 tabelas):
1. `Vendas` (`Total`, `Desconto`)
2. `VendaItens` (`PrecoUnitario`, `CustoUnitario`, `Desconto`, `Subtotal`)
3. `CaixaSessoes` (`ValorAbertura`, `ValorEsperado`, `ValorInformadoFechamento`, `TotalVendas`, `TotalSangrias`, `TotalSuprimentos`)
4. `MovimentacoesCaixa` (`ValorMovimento`, `ValorInicial`, `ValorFinal`, `Sangrias`, `Suprimentos`, `Diferenca`)
5. `OrdensServico` (`ValorMaoObra`, `Desconto`)
6. `OrdemServicoItens` (`ValorUnitario`, `CustoUnitario`)
7. `Orcamentos` (`Subtotal`, `Desconto`, `Acrescimo`, `Total`, `LucroEstimado`, `ComissaoVendedor`, `ImpostosEstimados`)
8. `OrcamentoItens` (`PrecoUnitario`, `PrecoCusto`, `Desconto`, `Subtotal`, `LucroEstimado`)
9. `Produtos` (`PrecoCompra`, `PrecoVenda`, `ValorTotalEstoque`, `TotalFaturado`)
10. `Clientes` (`TotalGasto`)
11. `Fornecedores` (`PedidoMinimo`, `TotalCompras`)
12. `ProdutoFornecedores` (`PrecoUltimaCompra`, `ValorCompras`)
13. `Funcionarios` (`Salario`)
14. `ServicosPadrao` (`ValorSugerido`)
15. `Agendamentos`, `AgendamentoProdutos`, `AgendamentoServicos` (`ValorEstimado`, `ValorReal`, `ValorPago`, etc.)

### Tabelas Que NÃO Exigem Migração Monetária:
1. `Filiais` (apenas latitude/longitude - permanecem `REAL`).
2. `Metas` (apenas percentuais e metas de volume).
3. `Usuarios`, `Veiculos`, `LogsAuditoria`, etc. (sem dados monetários).

---

## 5. TABELAS SEM DADOS REAIS — PROTOCOLO DE VALIDAÇÃO SINTÉTICA

Conforme apurado na auditoria física do banco de dados `primoauto.db`:
- `Vendas`: 0 registros
- `VendaItens`: 0 registros
- `CaixaSessoes`: 0 registros
- `MovimentacoesCaixa`: 0 registros

> [!CAUTION]
> **Declaração Formal de Limitação:**  
> A conversão de dados destas 4 tabelas **NÃO FOI VALIDADA COM DADOS REAIS DE PRODUÇÃO**.  
> Para garantir a validade técnica sem tocar no banco do cliente, foi desenvolvido o teste automatizado `TabelasSemDados_ValidacaoEmBancoSinteticoIsolado_RoundTripExato` em banco SQLite in-memory isolado (`:memory:`), validando criação de schema, inserção de dados sintéticos, migração mecânica e round-trip exato.

---

## 6. REVISÃO TÉCNICA DA POLÍTICA DE ARREDONDAMENTO

Em estrita consonância com as diretrizes do gate, a redação foi ajustada para um tom tecnicamente conservador:

> **Parecer de Arredondamento:**  
> “A política atual do domínio utiliza `MidpointRounding.AwayFromZero`. A adequação fiscal deverá ser validada por tipo de documento e regra tributária específica antes da entrada em produção.”

### Evidências Documentais no Código:
- Em `FiscalEmpresaModels.cs` (linha 28), o cálculo de impostos e totais fiscais adota explicitamente `MidpointRounding.AwayFromZero`.
- Em `ProdutoImportacaoService.cs` (linhas 303 e 441), o cálculo de margem e preço de venda sugerido adota `MidpointRounding.AwayFromZero`.
- Em `MoneyCents.cs` (linhas 16 e 30), a conversão de decimais e cálculo de descontos percentuais utiliza `MidpointRounding.AwayFromZero`.

---

## 7. ANÁLISE DE IMPACTO NOS REPOSITORIES

O mapeamento de todos os Repositories e Services que manipulam colunas monetárias revelou:

1. **Estado Atual do Código (Pré-Migration):**  
   - O código em C# assume que o banco armazena **reais flutuantes** (`REAL = reais`).  
   - As leituras utilizam `Convert.ToDecimal(reader.GetValue(i))` ou `reader.GetDouble(i)`.  
   - As gravações passam o valor em reais diretamente nos parâmetros (`command.Parameters.AddWithValue("@Total", venda.Total)`).  
   - Consultas agregadas em SQL utilizam `SUM(Total)` e comparam diretamente com números em reais (`WHERE Total > 100`).
2. **Impacto Quando a Migração Ocorrer (Pós-Migration):**  
   - O código dos Repositories precisará ser atualizado sincronizadamente com a migração:  
     - **Gravação:** `command.Parameters.AddWithValue("@Total", MoneyCents.FromDecimal(venda.Total).Cents);`  
     - **Leitura:** `venda.Total = new MoneyCents(reader.GetInt64(i)).ToDecimal();`  
     - **Queries SQL:** `SUM(Total) / 100.0 AS TotalFaturado` ou comparação em centavos (`WHERE Total > 10000`).

---

## 8. MECÂNICA TÉCNICA NO SQLITE (12-STEP TABLE REBUILD)

Documentada detalhadamente em [P2_1_SQLITE_MIGRATION_MECHANICS.md](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/P2_1_SQLITE_MIGRATION_MECHANICS.md).  
SQLite não suporta `ALTER COLUMN TYPE`. Para cada tabela monetária, será executada a reconstrução de tabela com preservação de integridade referencial:
`PRAGMA foreign_keys = OFF` $\to$ `CREATE shadow` $\to$ `CREATE new com INTEGER` $\to$ `INSERT SELECT com ROUND * 100` $\to$ `DROP old` $\to$ `RENAME new` $\to$ `Recriar índices` $\to$ `PRAGMA foreign_key_check` $\to$ `COMMIT` $\to$ `PRAGMA foreign_keys = ON` $\to$ `PRAGMA integrity_check`.

---

## 9. SUÍTE DE TESTES MATEMÁTICOS E VALIDAÇÃO

Criada em [`Tests/PrimoAutoEletrica.Tests/Money/MoneyClassificationGateMathTests.cs`](file:///c:/Projetos/PrimoAutoEletrica/Tests/PrimoAutoEletrica.Tests/Money/MoneyClassificationGateMathTests.cs) e documentada em [P2_1_MONEY_MATH_TESTS.md](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/P2_1_MONEY_MATH_TESTS.md).

Resultados dos testes:
- Transformação `decimal -> cents -> decimal` com todos os valores de prova exigidos: 100% PASS.
- Separação entre comportamento matemático e regra de negócio validada: 100% PASS.
- Cálculos de Subtotal, Total, Margem percentual e Rateio misto sem perda de centavos: 100% PASS.
- Simulação mecânica em banco in-memory sintético: 100% PASS.

---

## 10. NON-NEGOTIABLE SAFETY GATE

### Compilação e Execução
- **Comando:** `dotnet build --configuration Release`
  - Projetos Compilados: 6 projetos (`PrimoAutoEletrica`, `PrimoAutoEletrica.Api`, `PrimoAutoEletrica.Tests`, `PrimoAutoEletrica.UiTests`, `Tools/DbConfigurator`, `Tools/LocalSyncSimulator`).
  - Erros: **0 erros**.
  - Avisos (Warnings): ~76 avisos de plataforma Windows CA1416 e warnings informativos de anotação de nullable CS8632. Nenhum warning impede a compilação ou execução.
- **Testes Unitários:** `dotnet test Tests/PrimoAutoEletrica.Tests`
  - Total: **320 executados**
  - Passed: **320 APROVADOS**
  - Failed: **0**
  - Skipped: **0**
- **Testes de UI Desktop:** Permanecem em categoria separada (`PrimoAutoEletrica.UiTests`), excluídos da execução padrão sem display interativo desktop, conforme desenhado na infraestrutura de CI.

---

## 11. CONFIRMAÇÃO DE STATUS FINAL

> [!IMPORTANT]
> **STATUS DO GATE:**  
> **MONEY MIGRATION = BLOCKED**  
>  
> • Nenhum `ALTER TABLE` executado.  
> • Nenhum schema do banco alterado.  
> • Nenhum dado real de produção ou homologação alterado.  
> • Nenhuma tabela financeira modificada.  
> • A branch `main` permanece **100% intocada**.  
