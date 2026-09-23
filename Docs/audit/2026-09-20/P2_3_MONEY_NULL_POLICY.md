# PRIMOX Workshop — Política Oficial de Nulabilidade e Tratamento Monetário (Fase 2.3)

**Data de Emissão:** 2026-09-23  
**Status do Projeto:** `MONEY MIGRATION = BLOCKED`  
**Escopo:** Camada de Acesso a Dados (Repositories, Services, ORM/Micro-ORM, SQL Handlers)  
**Referência Arquitetural:** `PrimoAutoEletrica.Services.MoneyIO`, `PrimoAutoEletrica.Services.MoneyCents`

---

## 1. Visão Geral e Princípio Fail-Closed

O tratamento de valores monetários no banco SQLite e no ecossistema C# do PRIMOX Workshop deve seguir estritamente o princípio **Fail-Closed**:
1. **Colunas NOT NULL jamais aceitam ou produzem valores nulos silenciosos:** Caso o banco retorne `DBNull` em uma coluna declarada como não-nula, uma exceção controlada (`InvalidOperationException`) deve ser lançada imediatamente, impedindo a corrupção do estado de memória ou a conversão acidental para zero.
2. **Colunas NULLABLE preservam a distinção semântica entre "Ausência de Valor" (`null`) e "Valor Zero" (`0.00m`):** Converter `NULL` silenciosamente em `0.00` mascara inadimplências, valores em negociação, campos opcionais de comissão/desconto e quebra contratos de auditoria fiscal.

---

## 2. Matriz de Comportamento por Tipo de Dado

| Estado no SQLite (INTEGER cents) | Tipo no Schema | Leitura C# (`MoneyIO`) | Escrita C# (`MoneyIO`) | Semântica de Domínio | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `NULL` | `INTEGER` (NOT NULL) | Lança `InvalidOperationException` | Proibido (`decimal` obrigatório) | Corrupção/Violação de Integridade | **FAIL-CLOSED** |
| `NULL` | `INTEGER` (NULLABLE) | Retorna `(decimal?)null` | Grava `DBNull.Value` (`DbType.Int64`) | Ausência de valor monetário | **PRESERVADO** |
| `0` | `INTEGER` (NOT NULL / NULLABLE) | Retorna `0.00m` | Grava `0L` | Saldo/preço zero legítimo | **EXATO** |
| Positivo (ex: `12345`) | `INTEGER` | Retorna `123.45m` | Grava `12345L` | Valor financeiro credor/custo/preço | **EXATO** |
| Negativo (ex: `-5000`) | `INTEGER` | Retorna `-50.00m` | Grava `-5000L` | Estorno, sangria, prejuízo, saldo devedor | **EXATO** |

---

## 3. Comportamento Controlado de Leitura

### 3.1. Campos Obrigatórios (`LerMoeda`)
```csharp
public static decimal LerMoeda(DbDataReader reader, int index, MoneyPersistenceMode? mode = null)
{
    if (reader.IsDBNull(index))
        throw new InvalidOperationException($"Violação de integridade: Coluna monetária no índice {index} retornou DBNull em campo NOT NULL.");

    var activeMode = mode ?? DefaultMode;
    if (activeMode == MoneyPersistenceMode.CentsV1)
    {
        long cents = Convert.ToInt64(reader.GetValue(index));
        return cents / 100m;
    }

    return Convert.ToDecimal(reader.GetValue(index));
}
```
* **Controle:** Se houver `DBNull` em campo `NOT NULL`, falha com mensagem explicativa e rastreável.
* **Proibição:** É estritamente proibido fazer `reader.IsDBNull(index) ? 0m : ...` em campos monetários `NOT NULL`.

### 3.2. Campos Opcionais (`LerMoedaNullable`)
```csharp
public static decimal? LerMoedaNullable(DbDataReader reader, int index, MoneyPersistenceMode? mode = null)
{
    if (reader.IsDBNull(index))
        return null;

    var activeMode = mode ?? DefaultMode;
    if (activeMode == MoneyPersistenceMode.CentsV1)
    {
        long cents = Convert.ToInt64(reader.GetValue(index));
        return cents / 100m;
    }

    return Convert.ToDecimal(reader.GetValue(index));
}
```
* **Controle:** Retorna `null` puro (`decimal?`), nunca mascarando a nulabilidade.

---

## 4. Comportamento Controlado de Escrita

### 4.1. Escrita de Campos Obrigatórios (`GravarMoeda`)
* Requer parâmetro `decimal` (não-anulável).
* Executa arredondamento `MidpointRounding.AwayFromZero` ao converter para centavos inteiros (`long`).
* Associa parâmetro como `DbType.Int64` no modo `CentsV1`.

### 4.2. Escrita de Campos Opcionais (`GravarMoedaNullable`)
* Aceita `decimal?`.
* Se `!value.HasValue`, adiciona `DBNull.Value` com tipo `DbType.Int64`.
* Se `value.HasValue`, converte com exatidão para centavos `long` e adiciona parâmetro.

---

## 5. Tratamento de Valores Negativos

* Valores negativos são legítimos em contextos financeiros específicos:
  - Sangria de Caixa (`CaixaMovimentacoes.Valor`);
  - Diferença de Fechamento de Caixa (`CaixaSessoes.Diferenca`);
  - Descontos e abatimentos em orçamentos e faturamentos parciais;
  - Estornos de contas a receber e contas a pagar.
* O `MoneyIO` suporta nativamente sinais negativos:
  - Leitura: `-5000L` centavos $\rightarrow$ `-50.00m` reais.
  - Escrita: `-50.00m` reais $\rightarrow$ `-5000L` centavos.
* Proibido utilizar tipos não-assinados (`ulong`, `uint`) no armazenamento de dinheiro.

---

## 6. Cobertura de Testes Automatizados

A política de NULL e negativos é verificada por testes de regressão automatizados em `Tests/PrimoAutoEletrica.Tests/Money/MoneyRepositoryIoTests.cs`:
1. `MoneyIO_NullRules_Controlled_Behavior`: Valida que `LerMoedaNullable` retorna `null` para `DBNull` e `LerMoeda` lança `InvalidOperationException`.
2. `MoneyIO_NegativeValues_Preserved`: Valida persistência e leitura de `-50.00m` como `-5000L` sem anomalias.
3. `MoneyIO_Rounding_AwayFromZero_EdgeCases`: Valida arredondamento simétrico para valores positivos e negativos (`-0.005m` $\rightarrow$ `-1L`, `-1.005m` $\rightarrow$ `-101L`).

---

**Conclusão:**  
A camada de repositório está blindada contra vazamentos de nulos, perdas de distinção entre ausência e zero, e inversões acidentais de sinal. A migração real de dados continua **BLOQUEADA** até o cumprimento integral das fases de teste.
