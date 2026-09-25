# PRIMOX WORKSHOP — GATE 03: MONEY ROUNDING PROOF AUDIT

Data: 2026-09-25  
Versão: 1.0.0  
Política de Arredondamento: `MidpointRounding.AwayFromZero`  

---

## 1. Justificativa da Política de Arredondamento

No contexto contábil e fiscal brasileiro (SEFAZ e NBC TG / CPC), valores monetários no meio-termo (`0.005`) devem ser arredondados para longe do zero para evitar truncamento depreciativo de faturamento e discrepâncias com o rateio de parcelas fiscais.
O C# utiliza por padrão `MidpointRounding.ToEven` (Banker's Rounding), enquanto o SQL do SQLite `ROUND()` arredonda para o mais próximo.
Para garantir paridade determinística absoluta entre o runtime C# e as queries SQLite:
1. No C#: `Math.Round(val * 100m, MidpointRounding.AwayFromZero)`.
2. No SQLite Rebuild: `CAST(CASE WHEN val >= 0 THEN ROUND(val * 100.0 + 0.0000001) ELSE ROUND(val * 100.0 - 0.0000001) END AS INTEGER)`.

---

## 2. Prova Matemática de Meio-Ponto (AwayFromZero)

| Entrada (Real) | Valor x 100 | Meio-Ponto Avaliado | Inteiro Esperado | Inteiro Obtido | Roundtrip (R$) | Divergência |
|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| 0,005 | 0,5 | 0,5 -> 1 | 1 | 1 | R$ 0,01 | R$ 0,00 |
| 0,015 | 1,5 | 1,5 -> 2 | 2 | 2 | R$ 0,02 | R$ 0,00 |
| 0,025 | 2,5 | 2,5 -> 3 | 3 | 3 | R$ 0,03 | R$ 0,00 |
| 1,005 | 100,5 | 100,5 -> 101 | 101 | 101 | R$ 1,01 | R$ 0,00 |
| 2,675 | 267,5 | 267,5 -> 268 | 268 | 268 | R$ 2,68 | R$ 0,00 |
| 10,005 | 1000,5 | 1000,5 -> 1001 | 1001 | 1001 | R$ 10,01 | R$ 0,00 |

---

## 3. Prova de Rateio de Parcelas (Exemplo Crítico R$ 100,01 em 3 vezes)

- **Total a Pagar:** R$ 100,01 (10.001 centavos).
- **Cálculo da Divisão Inteira:**
  - Base por parcela: `10001 // 3 = 3333` centavos (R$ 33,33).
  - Resto da divisão: `10001 % 3 = 2` centavos.
  - Distribuição do resto:
    - Parcela 1: 3333 + 1 = 3334 centavos (R$ 33,34).
    - Parcela 2: 3333 + 1 = 3334 centavos (R$ 33,34).
    - Parcela 3: 3333 + 0 = 3333 centavos (R$ 33,33).
- **Soma das Parcelas:** `33,34 + 33,34 + 33,33 = 100,01`.
- **Divergência Centesimal:** `0,00`. Nenhum centavo perdido, nenhum centavo inventado.

---

## 4. Conclusão

TESTE: Validação das provas matemáticas de arredondamento AwayFromZero e rateio de parcelas  
RESULTADO: Conformidade matemática estrita comprovada sem perda de centavos.  
EVIDÊNCIA: Algoritmos validados em banco de testes e asserts computacionais.  
STATUS: **PASS**
