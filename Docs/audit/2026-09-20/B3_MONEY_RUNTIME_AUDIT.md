# PRIMOX WORKSHOP — B3
## AUDITORIA EM RUNTIME DA INFRAESTRUTURA MONEY (PRECISÃO MONETÁRIA)

**Data:** 2026-09-24  
**Branch:** `audit/product-discovery-2026-09`  
**Status da Infraestrutura Money:** **APROVADA (MIGRATION REAL BLOQUEADA)**

---

### 1. Mandato e Escopo

A auditoria revalidou a infraestrutura desenvolvida nas Fases 2.x para garantir que nenhuma operação financeira sofra perdas por ponto flutuante binário (`double` ou `float`).

**Regra Absoluta:**
- A migration definitiva de schema no banco de produção `primoauto.db` **permanece 100% BLOQUEADA**.
- O banco operacional `primoauto_operacional.db` opera no modo `LegacyReal` (compatível com os dados históricos reais), enquanto a infraestrutura `MoneyIO` e `MoneyCents` encontra-se compilada, validada e pronta para o momento oportuno de transição de schema.

---

### 2. Validações da Camada MoneyIO & MoneyCents

1. **Sem tipos `double` ou `float`:** Toda manipulação financeira é feita via `decimal` ou `MoneyCents` (representação inteira em centavos com `long`).
2. **Arredondamento Bancário / Comercial:** Utilização de `MidpointRounding.AwayFromZero` em todas as conversões de centavos, evitando perdas em dízimas periódicas.
3. **Preservação de Valores Negativos:** Testado e aprovado em sangrias, despesas e devoluções (ex: `-R$ 150,00` -> `-15000L`).
4. **Preservação de Nullability:** Colunas opcionais como `Desconto` ou `PrecoCusto` preservam `null` sem forçar zeros espúrios.
5. **Rateio sem Perda de Centavos:** O algoritmo `MoneyCents.DistribuirRateio` foi testado (ex: R$ 100,01 dividido em 3 parcelas gera parcelas de 33,34, 33,34 e 33,33 com soma exata de 100,01).
6. **Detecção Automática de Modo:** O `MoneyIO.DetectMode` inspeciona o `PRAGMA user_version` da conexão para operar automaticamente em `LegacyReal` ou `CentsV1`.

---

### 3. Status dos Testes Automatizados de Precisão
- `MoneyRepositoryIoTests`: 10 testes APROVADOS.
- `MoneyPreProductionGateTests`: 10 testes APROVADOS.
- Total de asserções monetárias: 100% PASS.
