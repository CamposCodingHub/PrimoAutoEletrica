# PRIMOX Workshop — Compatibilidade de Schema e Feature Gate Monetário (Fase 2.3)

**Data de Emissão:** 2026-09-23  
**Status do Projeto:** `MONEY MIGRATION = BLOCKED`  
**Escopo:** Transição Segura entre Schema Atual (REAL/DECIMAL) e Schema Futuro (INTEGER cents)  
**Referência Arquitetural:** `MoneyIO.MoneyPersistenceMode`, `MoneyIO.DetectMode(DbConnection)`

---

## 1. O Problema da Transição de Schema

O banco de produção atual (`primoauto.db`) armazena valores monetários em colunas do tipo `REAL` ou afinidade numérica decimal (ex.: `123.45`).  
O schema futuro, planejado na Fase 2 e ensaiado na Fase 2.2, migrará todas as 67 colunas monetárias para `INTEGER` armazenando centavos inteiros (ex.: `12345`).

Se o código do repositório assumir cegamente que o banco já está em centavos:
- O valor `123.45` seria lido como `123L` centavos $\rightarrow$ `R$ 1,23` (erro catastrófico de divisão por 100).
- Por outro lado, se o código assumir que o banco está em reais quando já foi migrado:
- O valor `12345` seria lido como `12345m` $\rightarrow$ `R$ 12.345,00` (erro catastrófico de multiplicação por 100).

---

## 2. Princípio Fundamental: Proibição de Heurísticas de Valor

> [!CAUTION]
> **REGRA INVIOLÁVEL:**  
> É terminantemente proibido tentar inferir se uma coluna está em reais ou centavos avaliando a magnitude do número (ex.: *"se o número for maior que 1000 então é centavos"* ou *"se tiver casas decimais é reais"*).  
> Heurísticas baseadas em valor causam corrupção de dados silenciosa em valores baixos (como itens de R$ 0,50 ou R$ 1,00) ou produtos de alto valor.

A determinação da unidade armazenada deve ser **estritamente contratual e determinística**, baseada no versionamento oficial do schema do banco de dados.

---

## 3. Arquitetura do Schema Gate Oficial (`PRAGMA user_version`)

O SQLite disponibiliza um cabeçalho oficial de 4 bytes denominado `user_version`, acessível via `PRAGMA user_version`. Este registro é transacional, embutido no banco de dados e imune a discrepâncias de configuração externa.

### 3.1. Versões de Schema Definidas

| `PRAGMA user_version` | Modo de Persistência | Unidade no SQLite | Conversão em Leitura | Conversão em Escrita |
| :---: | :--- | :--- | :--- | :--- |
| `0` | `MoneyPersistenceMode.LegacyReal` | `REAL` / `NUMERIC` (Reais) | `Convert.ToDecimal(val)` | Grava `decimal` (`DbType.Decimal`) |
| `1` (ou superior) | `MoneyPersistenceMode.CentsV1` | `INTEGER` (Centavos) | `Convert.ToInt64(val) / 100m` | Grava `long` cents (`DbType.Int64`) |

### 3.2. Implementação do Gate em `MoneyIO`

```csharp
public static MoneyPersistenceMode DetectMode(DbConnection connection)
{
    try
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA user_version;";
        var result = cmd.ExecuteScalar();
        int version = Convert.ToInt32(result);
        return version >= 1 ? MoneyPersistenceMode.CentsV1 : MoneyPersistenceMode.LegacyReal;
    }
    catch
    {
        return MoneyPersistenceMode.LegacyReal;
    }
}
```

* **Comportamento Padrão no Banco Atual:**  
  Como `primoauto.db` possui `user_version = 0`, a chamada a `DetectMode` retorna `LegacyReal`. Nenhuma quebra ocorre no banco atual enquanto a migração física não for executada.
* **Comportamento no Rehearsal / Banco Migrado:**  
  A rotina de migração física define explicitamente `PRAGMA user_version = 1;` ao final da transação com sucesso. Imediatamente após isso, todas as conexões detectam `CentsV1` e passam a operar com centavos inteiros.

---

## 4. Garantia de Compatibilidade nos Repositories

Para garantir retrocompatibilidade e permitir testes em ambientes com ou sem migração:
1. `MoneyIO.DefaultMode` pode ser definido por injeção de dependência ou na inicialização do `DatabaseService`.
2. As assinaturas de `LerMoeda`, `LerMoedaNullable`, `GravarMoeda` e `ConverterAgregacao` aceitam o parâmetro opcional `MoneyPersistenceMode? mode = null`.
3. Quando omitido, utiliza `DefaultMode`, permitindo que os testes definam o modo ativo conforme o cenário.

---

## 5. Validação Automatizada em Testes

O teste `MoneyIO_DetectMode_UserVersion_Detection` em `Tests/PrimoAutoEletrica.Tests/Money/MoneyRepositoryIoTests.cs` valida:
- Banco com `user_version = 0` $\rightarrow$ `LegacyReal`
- Banco com `user_version = 1` $\rightarrow$ `CentsV1`
- Banco com `user_version = 2` $\rightarrow$ `CentsV1`

---

**Status Final:**  
Compatibilidade garantida. Risco de quebra do banco real: **ZERO**. A migração física permanece **BLOQUEADA**.
