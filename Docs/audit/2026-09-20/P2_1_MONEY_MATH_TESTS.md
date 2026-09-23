# PRIMOX Workshop — TESTES MATEMÁTICOS E ISOLADOS DE MONEY (FASE 2.1)

> **Documento:** P2_1_MONEY_MATH_TESTS.md  
> **Data:** 23/09/2026  
> **Status:** EXECUTADO E 100% APROVADO  
> **Arquivo de Testes:** [`Tests/PrimoAutoEletrica.Tests/Money/MoneyClassificationGateMathTests.cs`](file:///c:/Projetos/PrimoAutoEletrica/Tests/PrimoAutoEletrica.Tests/Money/MoneyClassificationGateMathTests.cs)  
> **Garantia de Isolamento:** Nenhum dado do banco real do usuário foi acessado ou modificado nestes testes.  

---

## 1. RESUMO DA EXECUÇÃO DOS TESTES

| Métrica | Resultado |
|---|---|
| **Suíte de Testes** | `PrimoAutoEletrica.Tests` (.NET 10.0-windows) |
| **Total de Testes Executados** | **320** |
| **Testes Aprovados (Passed)** | **320** (100%) |
| **Falhas (Failed)** | **0** |
| **Ignorados (Skipped)** | **0** |
| **Duração da Execução** | ~15 segundos |

---

## 2. VALIDAÇÃO MATEMÁTICA DA TRANSFORMAÇÃO DECIMAL ↔ CENTS

Testado rigorosamente através de `Transformacao_Decimal_Para_Cents_E_Volta_Exata`:

| Valor Decimal Entrada | Centavos Esperados (`long`) | Valor Decimal Retornado | Arredondamento Aplicado | Resultado do Teste |
|---|:---:|:---:|---|:---:|
| `0,01` | `1` | `0,01` | Exato | ✅ PASS |
| `0,05` | `5` | `0,05` | Exato | ✅ PASS |
| `0,10` | `10` | `0,10` | Exato | ✅ PASS |
| `1,23` | `123` | `1,23` | Exato | ✅ PASS |
| `99,99` | `9.999` | `99,99` | Exato | ✅ PASS |
| `100,01` | `10.001` | `100,01` | Exato | ✅ PASS |
| `0,005` | `1` | `0,01` | `AwayFromZero` (+0,5¢ sobe para 1¢) | ✅ PASS |
| `1,005` | `101` | `1,01` | `AwayFromZero` (+0,5¢ sobe para 1¢) | ✅ PASS |
| `2,675` | `268` | `2,68` | `AwayFromZero` (+0,5¢ sobe para 1¢) | ✅ PASS |
| `-0,01` | `-1` | `-0,01` | Exato (matemático negativo) | ✅ PASS |
| `-1,005` | `-101` | `-1,01` | `AwayFromZero` (-0,5¢ desce para -1¢) | ✅ PASS |

### 2.1 Verificação da Política `MidpointRounding.AwayFromZero`
Confirmada através do teste `PoliticaArredondamento_MidpointRounding_AwayFromZero_Confirmada`:
- `0,0050m` $\to$ `1` centavo (+0,5 centavo é promovido para a unidade superior em módulo).
- `-0,0050m` $\to$ `-1` centavo (-0,5 centavo é promovido para a unidade inferior em módulo).
- `0,0049m` $\to$ `0` centavos (fração menor que meio centavo trunca para zero).
- `-0,0049m` $\to$ `0` centavos.

---

## 3. SEPARAÇÃO ENTRE MATEMÁTICA PURA E REGRA DE NEGÓCIO

Confirmada através do teste `Separacao_Matematica_Vs_RegraDeNegocio_ValoresNegativos`:

1. **Camada Matemática (`MoneyCents`):**  
   - Aceita valores negativos (`-50,00` $\to$ `-5000` cents $\to$ `-50,00`).  
   - Necessário para representar deltas financeiros, sangrias, estornos e saldos devedores contábeis sem corromper a representação inteira.
2. **Camada de Regra de Negócio (`ComercialValidationHelper`):**  
   - Bloqueia valores negativos em operações de venda, preços de produtos e cadastros:  
     `ComercialValidationHelper.GarantirValorMaiorOuIgualZero(-10.00m, "preço")` lança `InvalidOperationException`.  
     `ComercialValidationHelper.GarantirValorMaiorQueZero(0m, "valor")` lança `InvalidOperationException`.  

---

## 4. TESTES DE CASOS DE CÁLCULO E ARREDONDAMENTO INTERMEDIÁRIO

### 4.1 Subtotal: $\text{Quantidade} \times \text{Preço} - \text{Desconto}$
- **Cenário:** 3 unidades $\times$ R$ 19,99 com R$ 5,00 de desconto.
- **Resultado Decimal:** R$ 54,97.
- **Resultado em Cents:** 5.497 centavos.
- **Conclusão:** Sem qualquer divergência intermediária entre cálculo flutuante e cálculo em centavos inteiros (`Assert.Equal(subtotalDecimal, subtotalCents / 100m)`).

### 4.2 Total: $\text{Subtotal} - \text{Desconto} + \text{Acréscimo}$
- **Cenário:** Subtotal R$ 150,00, Desconto R$ 15,50, Acréscimo R$ 7,25.
- **Resultado em Cents:** `15000 - 1550 + 725 = 14175` centavos.
- **Resultado Decimal:** R$ 141,75.
- **Conclusão:** Fechamento exato na unidade de centavo.

### 4.3 Margem de Lucro: $\frac{\text{Lucro}}{\text{Total}} \times 100$
- **Regra Fundamental:** Margem é classificada como **`PERCENTAGE`**, NÃO sendo tratada como centavos monetários.
- **Cenário:** Lucro de R$ 40,00 sobre Total de R$ 200,00.
- **Cálculo:** `((decimal)lucro.Cents / total.Cents) * 100m = 20,00%`.
- **Conclusão:** O percentual é mantido como taxa contínua sem truncamento indevido de centavos.

### 4.4 Rateio Misto de Pagamentos: $\sum(\text{Partes}) = \text{Total}$
- **Cenário:** Venda de R$ 100,01 dividida em 3 formas de pagamento:
  - Parte 1 (Dinheiro): R$ 33,34 (3.334 cents)
  - Parte 2 (PIX): R$ 33,34 (3.334 cents)
  - Parte 3 (Cartão): R$ 33,33 (3.333 cents)
- **Soma das Partes:** `3334 + 3334 + 3333 = 10001` cents (R$ 100,01).
- **Conclusão:** A conservação do valor total é 100% exata, sem "centavo perdido" de arredondamento.

---

## 5. PROVA EM TABELAS SEM DADOS REAIS (AMBIENTE SINTÉTICO)

Conforme identificado na Fase 2, as tabelas:
- `Vendas`
- `VendaItens`
- `CaixaSessoes`
- `MovimentacoesCaixa`

possuem **zero registros reais** no banco local de desenvolvimento/auditoria.

> [!IMPORTANT]
> **Declaração Formal de Validação:**  
> A conversão de dados dessas quatro tabelas **NÃO FOI VALIDADA COM DADOS REAIS DE PRODUÇÃO**, uma vez que tais registros inexistem no banco inspecionado.

### Protocolo de Validação Sintética Realizado:
O teste `TabelasSemDados_ValidacaoEmBancoSinteticoIsolado_RoundTripExato` executou:
1. Criação do schema idêntico ao de produção em banco SQLite in-memory (`Data Source=:memory:`).
2. Inserção de dados sintéticos estruturados (Vendas com itens e sessões de caixa com movimentação).
3. Execução da migração mecânica para `INTEGER cents` via `CAST(ROUND(coluna * 100) AS INTEGER)`.
4. Comparação registro a registro entre a tabela temporária e a tabela convertida.
5. Validação de equivalência exata (`originalReal == migratedCents / 100.0`).
6. Destruição imediata da conexão e do banco em memória após o encerramento do teste (`using`).
