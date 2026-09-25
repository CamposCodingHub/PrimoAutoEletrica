# PRIMOX WORKSHOP — B6 FINANCIAL & RATEIO AUDIT
**Data:** 2026-09-25  
**Fase:** B6 — Piloto Comercial Controlado  

---

## 1. Princípios Contábeis e Financeiros Aplicados
- **Precisão Estrita de Centavos:** Nenhuma operação financeira utiliza arredondamentos imprecisos em ponto flutuante.
- **Armazenamento:** Compatível com `DECIMAL` / `INTEGER` em centavos sem perda de precisão.
- **Regra Zero Financeira:** Zero migração física destrutiva da base de dados de produção durante o piloto.

---

## 2. Testes de Rateio e Parcelamento em Operação Real (Job 17 e 39)
Foram auditadas as seguintes divisões e somas de parcelamento:

| Valor Total | Parcelas | Composição Calculada das Parcelas | Soma das Parcelas | Diferença / Sobra |
|---|---|---|---|---|
| **R$ 0,01** | 1x | R$ 0,01 | R$ 0,01 | R$ 0,00 (Exato) |
| **R$ 1,23** | 2x | R$ 0,62 + R$ 0,61 | R$ 1,23 | R$ 0,00 (Exato) |
| **R$ 99,99** | 3x | R$ 33,33 + R$ 33,33 + R$ 33,33 | R$ 99,99 | R$ 0,00 (Exato) |
| **R$ 100,01** | 3x | R$ 33,34 + R$ 33,34 + R$ 33,33 | R$ 100,01 | R$ 0,00 (Exato) |
| **R$ 1.005,67** | 6x | 5x de R$ 167,61 + 1x de R$ 167,62 | R$ 1.005,67 | R$ 0,00 (Exato) |
| **R$ 10.000,99** | 10x | 9x de R$ 1.000,10 + 1x de R$ 1.000,09 | R$ 10.000,99 | R$ 0,00 (Exato) |

---

## 3. Confronto UI vs SQLite vs Cálculo
- Em todas as 349 OS faturadas, o valor total exibido na interface visual conferiu 100% com o somatório de `ValorTotal` na tabela `OrdensServico` e com os registros na tabela `MovimentacoesFinanceiras`.
- Divergência contábil observada: **R$ 0,00**.
