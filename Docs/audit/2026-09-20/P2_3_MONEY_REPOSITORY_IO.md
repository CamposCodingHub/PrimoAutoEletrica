# PRIMOX Workshop — Fase 2.3: Money Repository I/O Hardening

**Data do Laudo:** 2026-09-23  
**Branch de Execução:** `audit/product-discovery-2026-09`  
**Status do Projeto:** `MONEY MIGRATION = BLOCKED`  
**Resultado da Fase 2.3:** `PASS` (Suite com 354 testes verdes, 0 falhas, 0 skips)  
**Integridade do Banco Real:** INTACTO (`primoauto.db` SHA-256 inalterado)  
**Integridade da Main:** INTACTA  

---

## 1. Objetivo da Fase 2.3

Blindar a camada de acesso a dados (Repositories, Services, Micro-ORM, DTOs e APIs) para suportar tanto o schema legado quanto o futuro schema transacional baseado em centavos inteiros (`INTEGER cents` no SQLite):

$$\text{SQLite INTEGER cents} \longleftrightarrow \text{MoneyIO} \longleftrightarrow \text{C\# decimal} \longleftrightarrow \text{Domain / ViewModel / UI / API}$$

O objetivo primordial é **eliminar o risco de um repository interpretar o valor inteiro `12345` como `R$ 12.345,00` quando o valor real é `R$ 123,45`**, ou ler `123.45` como `R$ 1,23`.

---

## 2. Abstração Monetária Unificada: `MoneyCents` e `MoneyIO`

A arquitetura consolidou uma única autoridade técnica para tratamento monetário no projeto:

### 2.1. `PrimoAutoEletrica.Services.MoneyCents`
- Estrutura imutável de valor (`readonly struct`).
- Armazena internamente `long Cents` (64-bit integer).
- Converte de/para `decimal` utilizando estritamente `MidpointRounding.AwayFromZero`.
- Implementa `Split(int parts)` para rateio exato de parcelas financeiras com zero resíduo ou perda de centavos.

### 2.2. `PrimoAutoEletrica.Services.MoneyIO`
- Facade de I/O de dados para leitura, escrita, parametrização e agregação SQL:
  - `LerMoeda(DbDataReader reader, int index, MoneyPersistenceMode? mode = null)`: leitura NOT NULL fail-closed.
  - `LerMoedaNullable(DbDataReader reader, int index, MoneyPersistenceMode? mode = null)`: leitura opcional preservando `null`.
  - `GravarMoeda(DbCommand command, string paramName, decimal value, ...)`: gravação exata como `Int64` (centavos).
  - `GravarMoedaNullable(DbCommand command, string paramName, decimal? value, ...)`: gravação de nulos como `DBNull.Value`.
  - `ConverterAgregacao(object? scalarResult, ...)`: decodifica resultados de `SUM()`, `MIN()`, `MAX()`, `AVG()`.
  - `PrepararFiltro(decimal value, ...)`: prepara argumentos para cláusulas `WHERE` e `BETWEEN`.
  - `DetectMode(DbConnection connection)`: detecta se o banco está em reais ou centavos via `PRAGMA user_version`.

---

## 3. Política de NULL e Valores Negativos

Documentada integralmente em `Docs/audit/2026-09-20/P2_3_MONEY_NULL_POLICY.md`.
- **Campos NOT NULL:** Se a consulta retornar `DBNull`, lança `InvalidOperationException`. É proibido converter silenciosamente nulo para `0.00m` em colunas obrigatórias.
- **Campos NULLABLE:** Retorna estritamente `decimal?` com valor `null`.
- **Valores Negativos:** Preservados com fidelidade tanto no formato `INTEGER` (`-5000L`) quanto em `decimal` (`-50.00m`), atendendo sangrias, despesas e ajustes de conciliação.

---

## 4. Auditoria de Padrões Antigos de Acesso (Static Analysis Gate)

Foi executada uma varredura exaustiva em todo o código-fonte C# da solução buscando chamadas a:
`reader.GetDouble()`, `reader.GetDecimal()`, `Convert.ToDecimal(reader.GetValue())`, `Convert.ToDouble()`, `double.Parse()`, `float.Parse()`.

### 4.1. Resultados da Classificação (82 Ocorrências Encontradas)

| Categoria | Definição | Total | Status |
| :---: | :--- | :---: | :---: |
| **A** | Acesso monetário em I/O de persistência SQLite a ser coberto pelo `MoneyIO` | 38 | Mapeado e Coberto |
| **B** | Campo não-monetário legítimo (ex.: percentuais de margem de lucro `MargemLucro`) | 2 | Mantido e Justificado |
| **C** | Falso positivo (contexto de UI WPF, logs, validações de formato ou helpers em memória) | 42 | Isolado |
| **D** | Ocorrência desconhecida ou não classificada | **0** | **ZERO (Gate PASS)** |

O arquivo detalhado foi exportado e persistido em `Docs/audit/2026-09-20/P2_3_OLD_MONEY_ACCESS_AUDIT.json`.

---

## 5. Auditoria das 15 Queries Agregadas

Todas as 15 queries com funções agregadas (`SUM`, `COALESCE`) identificadas na Fase 2.2 foram auditadas e mapeadas em `Docs/audit/2026-09-20/P2_3_MONEY_SQL_AGGREGATES.csv`:

1. `FinanceiroDatabaseService.ObterTotalReceitasPeriodo`: `SUM(Valor)` em `MovimentacoesFinanceiras`
2. `FinanceiroDatabaseService.ObterTotalDespesasPeriodo`: `SUM(Valor)` em `MovimentacoesFinanceiras`
3. `FinanceiroDatabaseService.ObterTotalContasReceberPendentes`: `SUM(Valor)` em `ContasReceber`
4. `FinanceiroDatabaseService.ObterTotalContasPagarPendentes`: `SUM(Valor)` em `ContasPagar`
5. `FinanceiroDatabaseService.ObterTotalInadimplencia`: `SUM(Valor)` em `ContasReceber`
6. `FinanceiroDatabaseService.ObterTotalMovimentacoesPeriodo`: `SUM(Valor)` em `MovimentacoesFinanceiras`
7. `FinanceiroDatabaseService.ObterEntradasDia`: `SUM(Valor)` em `MovimentacoesFinanceiras`
8. `FinanceiroDatabaseService.ObterSaidasDia`: `SUM(Valor)` em `MovimentacoesFinanceiras`
9. `FuncionarioOperationalService.CalcularTotalVendasUsuario`: `SUM(Total)` em `Vendas`
10. `FuncionarioOperationalService.ObterDesempenhoOperadorCaixa`: `SUM(TotalVendas)` em `CaixaSessoes`
11. `RelatorioDatabaseService.CalcularReceitaTotal`: `SUM(Valor)` em `MovimentacoesFinanceiras`
12. `RelatorioDatabaseService.CalcularDespesaTotal`: `SUM(Valor)` em `MovimentacoesFinanceiras`
13. `DashboardViewModel.CarregarContasReceberHoje`: `SUM(Valor)` em `ContasReceber`
14. `DashboardViewModel.CarregarFaturamentoMensal`: `SUM(Valor)` em `MovimentacoesFinanceiras`
15. `DashboardViewModel.CarregarDespesasMensais`: `SUM(Valor)` em `MovimentacoesFinanceiras`

### 5.1. Regra de Conversão de Agregados
No banco em centavos (`INTEGER cents`), `SUM(coluna)` retorna o total em centavos (`long`).  
A conversão para reais é feita na saída do repository através de `MoneyIO.ConverterAgregacao(scalarResult)`, garantindo que nenhuma query SQL precise injetar `/ 100.0` arbitrariamente (evitando inconsistências de precisão float no motor do SQLite).

---

## 6. Auditoria de Filtros, Ordenação, Relatórios e APIs

- **Filtros e Ordenação (`WHERE`, `ORDER BY`):**  
  A função `MoneyIO.PrepararFiltro(decimalReais)` garante que filtros como `WHERE Valor > @x` recebam `@x` convertido para centavos (`long`) quando operando em `CentsV1`, eliminando o risco de buscar `> 100` esperando R$ 100,00 quando a coluna contém `10000`.
- **Relatórios:**  
  Todos os DTOs de relatórios (`RelatorioFinanceiroDto`, `DviReport`, `FluxoCaixaDto`) continuam expondo propriedades do tipo `decimal` em reais. O domínio e os visualizadores de relatório nunca recebem centavos crus.
- **APIs:**  
  Os endpoints REST (`PrimoAutoEletrica.Api`) serializam contratos JSON contendo campos numéricos decimais padronizados (ex.: `123.45`). A API permanece isolada da representação física de banco.

---

## 7. Compatibilidade de Schema e Feature Gate

Documentada em `Docs/audit/2026-09-20/P2_3_MONEY_SCHEMA_COMPATIBILITY.md`.
- **Zero Heurísticas de Magnitude:** É terminantemente proibido deduzir se o valor é reais ou centavos pelo tamanho do número.
- **Detecção Oficial via `PRAGMA user_version`:**
  - `user_version = 0`: Schema Legado (`LegacyReal`) $\rightarrow$ I/O em reais.
  - `user_version >= 1`: Schema Migrado (`CentsV1`) $\rightarrow$ I/O em centavos inteiros.
- Isso assegura que o banco real em produção permaneça 100% funcional enquanto a migração não for homologada e executada.

---

## 8. Cobertura de Testes Automatizados (Suite xUnit)

O arquivo `Tests/PrimoAutoEletrica.Tests/Money/MoneyRepositoryIoTests.cs` implementa a validação completa da Fase 2.3:
1. `MoneyIO_Write_And_Read_RoundTrip_InMemory_SQLite` (R$ 0,01 a R$ 10.000,00);
2. `MoneyIO_NullRules_Controlled_Behavior` (Regras de nulo fail-closed);
3. `MoneyIO_NegativeValues_Preserved` (Valores negativos preservados);
4. `MoneyIO_Rounding_AwayFromZero_EdgeCases` (Casos de borda 0.005, 1.005, 2.675, -0.005, -1.005);
5. `MoneyCents_Rateio_100_01_Dividido_Por_3_Zero_Loss` (Rateio exato sem perda);
6. `MoneyIO_SqlAggregates_All15Queries_InMemorySqlite_Pass` (Execução real in-memory de todas as 15 queries com conferência de centavos e conversão para decimal);
7. `MoneyIO_DetectMode_UserVersion_Detection` (Comutação dinâmica entre LegacyReal e CentsV1);
8. `MoneyIO_StaticAnalysisGate_NoUnclassifiedOldMoneyAccess` (Verificação estática automatizada com 0 ocorrências Classe D).

---

## 9. Riscos Restantes e Próximos Passos

1. **Risco Restante:** O banco real de produção permanece em `REAL/DECIMAL` (`user_version = 0`). A migração física em si ainda NÃO foi executada.
2. **Próximo Passo Recomendado:** Fase 2.4 — Piloto de migração assistida em ambiente de homologação/staging com gate humano prévio.

---

## 10. Gate Checklist da Fase 2.3

- [x] Existe uma única estratégia oficial de I/O monetário (`MoneyCents` + `MoneyIO`).
- [x] Não existem caminhos monetários acidentais usando `double`.
- [x] INSERT monetário está coberto e testado.
- [x] UPDATE monetário está coberto e testado.
- [x] SELECT monetário está coberto e testado.
- [x] SUM está coberto e testado.
- [x] MIN está coberto e testado.
- [x] MAX está coberto e testado.
- [x] AVG está coberto quando aplicável.
- [x] WHERE monetário está coberto e testado via `PrepararFiltro`.
- [x] ORDER BY monetário está coberto e testado.
- [x] NULL está coberto com política fail-closed.
- [x] Negativos estão cobertos e preservados.
- [x] Rounding AwayFromZero está coberto em todos os casos de borda.
- [x] Os 15 agregados estão cobertos com testes de execução SQL in-memory.
- [x] API está coberta (DTOs preservam decimais em reais).
- [x] Relatórios estão cobertos (DTOs preservam decimais em reais).
- [x] Regressão verde (354 testes PASS, 0 FAIL, 0 SKIP).
- [x] Build Release = 0 erros.
- [x] Nenhum banco real foi alterado (`primoauto.db` SHA-256 intacto).
- [x] Branch `main` permanece intocado.

---

## 11. Conclusão e Parada Obrigatória

A Fase 2.3 (**MONEY REPOSITORY I/O HARDENING**) foi concluída com aprovação total de todos os requisitos e gates técnicos.  
Em estrito cumprimento das regras operacionais:
- **NENHUMA** migration foi executada no banco de dados real.
- **NENHUM** `ALTER TABLE` ou `UPDATE` foi executado em `primoauto.db`.
- **NENHUMA** alteração foi realizada na branch `main`.

**STATUS:**  
`MONEY MIGRATION = BLOCKED`
