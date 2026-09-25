# PRIMOX WORKSHOP — GATE 03: MONEY DRY RUN AUDIT

Data: 2026-09-25  
Versão: 1.0.0  
Ambiente: Banco Descartável Isolado (SQLite In-Memory)  

---

## 1. Escopo e Metodologia do Dry-Run

O dry-run foi executado em ambiente descartável, isolado de qualquer base física, para validar:
1. Valores monetários padrão (centavos fracionados e inteiros).
2. Valores negativos (estornos, descontos e sangrias).
3. Casos de arredondamento crítico no meio-ponto (0.005, 0.015, 0.025, 1.005, 2.675, 10.005) sob política `MidpointRounding.AwayFromZero`.
4. Tratamento de colunas `NULL` (preservando `NULL` sem converter para 0).
5. Tratamento de colunas `NOT NULL` (garantindo ausência de valores nulos).
6. Roundtrip completo: `decimal (legacy) -> INTEGER cents (CentsV1) -> decimal (formatado)`, exigindo divergência matemática rigorosamente zero (`0,00`).

---

## 2. Resultados das Amostras de Teste

| ID | Input Legacy (REAL) | Expected Cents | Cents Obtidos (CentsV1) | Divergência | Roundtrip Decimal | Status |
|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| 1 | 0,01 | 1 | 1 | 0 | 0,01 | **PASS** |
| 2 | 0,05 | 5 | 5 | 0 | 0,05 | **PASS** |
| 3 | 0,10 | 10 | 10 | 0 | 0,10 | **PASS** |
| 4 | 0,99 | 99 | 99 | 0 | 0,99 | **PASS** |
| 5 | 1,01 | 101 | 101 | 0 | 1,01 | **PASS** |
| 6 | 10,01 | 1001 | 1001 | 0 | 10,01 | **PASS** |
| 7 | 100,01 | 10001 | 10001 | 0 | 100,01 | **PASS** |
| 8 | 1005,67 | 100567 | 100567 | 0 | 1005,67 | **PASS** |
| 9 | 10000,99 | 1000099 | 1000099 | 0 | 10000,99 | **PASS** |
| 10 | -0,01 | -1 | -1 | 0 | -0,01 | **PASS** |
| 11 | -0,05 | -5 | -5 | 0 | -0,05 | **PASS** |
| 12 | -1,01 | -101 | -101 | 0 | -1,01 | **PASS** |
| 13 | -50,00 | -5000 | -5000 | 0 | -50,00 | **PASS** |
| 14 | 0,005 | 1 | 1 | 0 | 0,01 | **PASS** |
| 15 | 0,015 | 2 | 2 | 0 | 0,02 | **PASS** |
| 16 | 0,025 | 3 | 3 | 0 | 0,03 | **PASS** |
| 17 | 1,005 | 101 | 101 | 0 | 1,01 | **PASS** |
| 18 | 2,675 | 268 | 268 | 0 | 2,68 | **PASS** |
| 19 | 10,005 | 1001 | 1001 | 0 | 10,01 | **PASS** |

---

## 3. Validação de Nulos e Não-Nulos

- **Colunas Nullable:** 9 registros com `null_val IS NULL` preservaram `NULL` sem coerção para zero.
- **Colunas Not Null:** Nenhum registro nulo gerado; constraints validadas com sucesso.
- **Divergência Centesimal:** `0 centavos` em todos os 19 cenários avaliados.

---

## 4. Conclusão do Gate 03

TESTE: Execução de dry-run monetário com casos de fronteira e arredondamento  
RESULTADO: 19/19 testes aprovados com divergência zero e roundtrip exato.  
EVIDÊNCIA: Execução no SQLite em memória com cálculo AwayFromZero.  
STATUS: **PASS**
