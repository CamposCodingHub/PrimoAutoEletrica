# PRIMOX Workshop — MECÂNICA TÉCNICA DE MIGRAÇÃO NO SQLITE (FASE 2.1)

> **Documento:** P2_1_SQLITE_MIGRATION_MECHANICS.md  
> **Data:** 23/09/2026  
> **Status:** BLUEPRINT TÉCNICO CONCLUÍDO (Nenhuma execução)  
> **Aviso Fundamental:** SQLite **NÃO SUPORTA** `ALTER TABLE ALTER COLUMN TYPE`. Qualquer migração de tipo exige o procedimento formal de reconstrução de tabela (12-Step Table Rebuild).  

---

## 1. O PROBLEMA DA TIPAGEM NO SQLITE

Em bancos cliente/servidor como PostgreSQL ou SQL Server, uma migração de tipo pode ser realizada via:
```sql
-- PostgreSQL:
ALTER TABLE Vendas ALTER COLUMN Total TYPE BIGINT USING (ROUND(Total * 100)::BIGINT);

-- SQL Server:
ALTER TABLE Vendas ALTER COLUMN Total BIGINT;
```

No **SQLite**, o comando `ALTER TABLE` é estritamente limitado a:
1. `RENAME TABLE`
2. `RENAME COLUMN`
3. `ADD COLUMN`
4. `DROP COLUMN` (apenas em versões muito recentes e com severas restrições de constraints/FKs)

**Conclusão:** Para alterar o tipo físico de uma coluna de `REAL` para `INTEGER` (e garantir tipagem estrita de afinidade e integridade de constraints), é **obrigatório** adotar o procedimento de recriação de tabela com preservação de dados e chaves estrangeiras.

---

## 2. PROCEDIMENTO PADRÃO EM 12 PASSOS (SQLITE TABLE REBUILD)

Para cada tabela que sofrer migração monetária, o script deverá executar estritamente os seguintes 12 passos atômicos:

```
Passo 01: PRAGMA foreign_keys = OFF;
Passo 02: BEGIN TRANSACTION;
Passo 03: CREATE TABLE _shadow_pre_money_[tabela] AS SELECT * FROM [tabela];
Passo 04: CREATE TABLE [tabela]_new ( ... com INTEGER cents ... );
Passo 05: INSERT INTO [tabela]_new (...) SELECT ..., CAST(ROUND(col * 100) AS INTEGER), ... FROM [tabela];
Passo 06: Validar contagem e somas: COUNT(*), SUM(cents) == ROUND(SUM(real) * 100);
Passo 07: DROP TABLE [tabela];
Passo 08: ALTER TABLE [tabela]_new RENAME TO [tabela];
Passo 09: Recriar todos os índices originais em [tabela];
Passo 10: PRAGMA foreign_key_check([tabela]);
Passo 11: COMMIT;
Passo 12: PRAGMA foreign_keys = ON; PRAGMA integrity_check;
```

---

## 3. MAPEAMENTO DETALHADO POR TABELA

Abaixo, a definição exata dos artefatos técnicos necessários para cada tabela monetária:

### 3.1 Tabela `Vendas`
- **Colunas a Converter:** `Total` (REAL $\to$ INTEGER), `Desconto` (REAL $\to$ INTEGER).
- **Mecanismo:** Nova Tabela + Swap.
- **Foreign Keys de Entrada:** `VendaItens.VendaId` referencia `Vendas.Id`.
- **Índices a Recriar:**
  - `CREATE INDEX IF NOT EXISTS IX_Vendas_ClienteId ON Vendas(ClienteId);`
  - `CREATE INDEX IF NOT EXISTS IX_Vendas_Data ON Vendas(Data);`
  - `CREATE INDEX IF NOT EXISTS IX_Vendas_CaixaSessaoId ON Vendas(CaixaSessaoId);`
- **Validação Específica:** `SUM(Total_new) == ROUND(SUM(Total_old) * 100)`.

### 3.2 Tabela `VendaItens`
- **Colunas a Converter:** `PrecoUnitario`, `CustoUnitario`, `Desconto`, `Subtotal` (todos REAL $\to$ INTEGER).
- **Mecanismo:** Nova Tabela + Swap.
- **Foreign Keys de Saída:** `FOREIGN KEY (VendaId) REFERENCES Vendas(Id)`.
- **Índices a Recriar:**
  - `CREATE INDEX IF NOT EXISTS IX_VendaItens_VendaId ON VendaItens(VendaId);`
  - `CREATE INDEX IF NOT EXISTS IX_VendaItens_ProdutoId ON VendaItens(ProdutoId);`
- **Validação Específica:** Verificar que `Subtotal = (PrecoUnitario * Quantidade) - Desconto` em cada registro.

### 3.3 Tabela `CaixaSessoes`
- **Colunas a Converter:** `ValorAbertura`, `ValorEsperado`, `ValorInformadoFechamento`, `TotalVendas`, `TotalSangrias`, `TotalSuprimentos`.
- **Mecanismo:** Nova Tabela + Swap.
- **Foreign Keys de Entrada:** `MovimentacoesCaixa.CaixaSessaoId` referencia `CaixaSessoes.Id`.
- **Índices a Recriar:**
  - `CREATE INDEX IF NOT EXISTS IX_CaixaSessoes_OperadorId ON CaixaSessoes(OperadorId);`
  - `CREATE INDEX IF NOT EXISTS IX_CaixaSessoes_DataAbertura ON CaixaSessoes(DataAbertura);`
- **Tratamento Especial:** `ValorInformadoFechamento` é NULL quando o caixa está aberto (`CASE WHEN col IS NULL THEN NULL ELSE CAST(...) END`).

### 3.4 Tabela `MovimentacoesCaixa`
- **Colunas a Converter:** `ValorMovimento`, `ValorInicial`, `ValorFinal`, `Sangrias`, `Suprimentos`, `Diferenca`.
- **Mecanismo:** Nova Tabela + Swap.
- **Foreign Keys de Saída:** `FOREIGN KEY (CaixaSessaoId) REFERENCES CaixaSessoes(Id)`.
- **Índices a Recriar:**
  - `CREATE INDEX IF NOT EXISTS IX_MovimentacoesCaixa_SessaoId ON MovimentacoesCaixa(CaixaSessaoId);`
  - `CREATE INDEX IF NOT EXISTS IX_MovimentacoesCaixa_Data ON MovimentacoesCaixa(Data);`
- **Tratamento Especial:** `Diferenca` aceita valores negativos (estornos / faltas de caixa).

### 3.5 Tabela `OrdensServico`
- **Colunas a Converter:** `ValorMaoObra` (REAL $\to$ INTEGER), `Desconto` (REAL $\to$ INTEGER).
- **Colunas NÃO Convertidas:** Todas as demais colunas mantêm tipos originais.
- **Mecanismo:** Nova Tabela + Swap.
- **Foreign Keys de Entrada:** `OrdemServicoItens.OrdemServicoId`, `OrdemServicoEventos.OrdemServicoId`.
- **Índices a Recriar:**
  - `CREATE UNIQUE INDEX IF NOT EXISTS UQ_OrdensServico_Numero ON OrdensServico(Numero);`
  - `CREATE INDEX IF NOT EXISTS IX_OrdensServico_ClienteId ON OrdensServico(ClienteId);`
  - `CREATE INDEX IF NOT EXISTS IX_OrdensServico_Status ON OrdensServico(Status);`

### 3.6 Tabela `OrdemServicoItens`
- **Colunas a Converter:** `ValorUnitario`, `CustoUnitario` (ambas REAL $\to$ INTEGER).
- **Coluna PRESERVADA:** `Quantidade` **PERMANECE REAL** (unidades/horas fracionadas).
- **Mecanismo:** Nova Tabela + Swap.
- **Foreign Keys de Saída:** `FOREIGN KEY (OrdemServicoId) REFERENCES OrdensServico(Id)`.
- **Índices a Recriar:**
  - `CREATE INDEX IF NOT EXISTS IX_OrdemServicoItens_OSId ON OrdemServicoItens(OrdemServicoId);`

### 3.7 Tabela `Orcamentos`
- **Colunas a Converter:** `Subtotal`, `Desconto`, `Acrescimo`, `Total`, `LucroEstimado`, `ComissaoVendedor`, `ImpostosEstimados` (todos REAL $\to$ INTEGER).
- **Colunas PRESERVADAS:** `DescontoPercentual` e `MargemLucro` **PERMANECEM REAL** (são taxas percentuais, não valores monetários).
- **Mecanismo:** Nova Tabela + Swap.
- **Foreign Keys de Entrada:** `OrcamentoItens.OrcamentoId`.
- **Índices a Recriar:**
  - `CREATE INDEX IF NOT EXISTS IX_Orcamentos_ClienteId ON Orcamentos(ClienteId);`
  - `CREATE INDEX IF NOT EXISTS IX_Orcamentos_Numero ON Orcamentos(Numero);`

### 3.8 Tabela `OrcamentoItens`
- **Colunas a Converter:** `PrecoUnitario`, `PrecoCusto`, `Desconto`, `Subtotal`, `LucroEstimado` (todos REAL $\to$ INTEGER).
- **Coluna PRESERVADA:** `MargemLucro` **PERMANECE REAL** (percentual de margem).
- **Mecanismo:** Nova Tabela + Swap.
- **Foreign Keys de Saída:** `FOREIGN KEY (OrcamentoId) REFERENCES Orcamentos(Id)`.
- **Índices a Recriar:**
  - `CREATE INDEX IF NOT EXISTS IX_OrcamentoItens_OrcamentoId ON OrcamentoItens(OrcamentoId);`

### 3.9 Tabela `Produtos`
- **Colunas a Converter:** `PrecoCompra`, `PrecoVenda`, `ValorTotalEstoque`, `TotalFaturado` (todos REAL $\to$ INTEGER).
- **Coluna PRESERVADA:** `MargemLucro` **PERMANECE REAL** (percentual de lucro).
- **Mecanismo:** Nova Tabela + Swap.
- **Foreign Keys de Entrada:** Diversas tabelas referenciam `Produtos.Id`.
- **Atenção Redobrada:** O `PRAGMA foreign_keys = OFF` durante a recriação é estritamente indispensável para evitar que drops temporários acionem `ON DELETE CASCADE`.

---

## 4. ORDEM DE EXECUÇÃO E RECONEXÃO DE CHAVES ESTRANGEIRAS

A ordem de execução deve respeitar a hierarquia de dependência para minimizar janelas de foreign keys desativadas:

```
[Nível 1: Tabelas Dependentes (Folhas)]
  ├── OrdemServicoItens (referencia OrdensServico)
  ├── OrcamentoItens (referencia Orcamentos)
  └── VendaItens (referencia Vendas)

[Nível 2: Tabelas Mestres de Transação]
  ├── OrdensServico
  ├── Orcamentos
  ├── Vendas
  └── MovimentacoesCaixa (referencia CaixaSessoes)

[Nível 3: Tabelas Mestres Operacionais]
  ├── CaixaSessoes
  ├── Produtos
  └── Fornecedores / Clientes
```

---

## 5. RECONSTRUÇÃO E VALIDAÇÃO DE INTEGRIDADE

Imediatamente após a conclusão do swap de cada tabela:
1. Executar `PRAGMA foreign_key_check;` para verificar se alguma referência ficou órfã.
2. Executar `PRAGMA integrity_check;` para garantir que as páginas B-Tree e os índices recriados estão 100% íntegros.
3. Se qualquer verificação retornar erro, o rollback do nível da transação ou shadow table é acionado de imediato.
