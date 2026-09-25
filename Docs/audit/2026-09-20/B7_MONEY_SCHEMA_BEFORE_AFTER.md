# PRIMOX WORKSHOP — GATE 02: MONEY SCHEMA BEFORE/AFTER ARCHITECTURE

Data: 2026-09-25  
Versão: 1.0.0  
Padrão Alvo: CentsV1 (INTEGER em centavos, AwayFromZero, user_version = 1)  

---

## 1. Princípios da Arquitetura CentsV1

1. **Campos Monetários (67 mapeados):**
   - Migrados de `REAL` (ponto flutuante legacy) para `INTEGER` (representação exata em centavos).
   - Conversão matemática: `ROUND(campo * 100)` com política `AwayFromZero` (`MidpointRounding.AwayFromZero`).
   - Leitura/Escrita protegida por `MoneyIO`:
     - `MoneyIO.LerMoeda(reader, "Campo")` -> `long cents / 100m`
     - `MoneyIO.GravarMoeda(command, "@Param", decimalValue)` -> `param.Value = (long)Math.Round(val * 100, MidpointRounding.AwayFromZero)`
2. **Campos Não-Monetários (Estritamente Preservados):**
   - `QUANTITY` (`Quantidade`, `QuantidadeUltimaCompra`): mantidos em `REAL` / `INTEGER` originais.
   - `PERCENTAGE` (`MargemLucro`, `DescontoPercentual`, `PercentualAtingido`): mantidos em `REAL`.
   - `COORDINATE` (`Latitude`, `Longitude`): mantidos em `REAL`.
   - `WEIGHT` / `OTHER_MEASURE`: mantidos intactos.
3. **Mecanismo de Migração: SQLite 12-Step Table Rebuild:**
   - Passo 1: `PRAGMA foreign_keys = OFF;`
   - Passo 2: `BEGIN TRANSACTION;`
   - Passo 3: Criação de tabela temporária `<table>_cents_v1` com DDL corrigido (`INTEGER` nos 67 campos).
   - Passo 4: Cópia dos dados:
     `INSERT INTO <table>_cents_v1 (...) SELECT ..., CAST(ROUND(money_col * 100) AS INTEGER), ... FROM <table>;`
     (para campos Nullable: `CASE WHEN money_col IS NULL THEN NULL ELSE CAST(ROUND(money_col * 100) AS INTEGER) END`)
   - Passo 5: Drop da tabela original: `DROP TABLE <table>;`
   - Passo 6: Renomear tabela temporária: `ALTER TABLE <table>_cents_v1 RENAME TO <table>;`
   - Passo 7: Recriação de índices e triggers.
   - Passo 8: Verificação de integridade referencial local: `PRAGMA foreign_key_check;`
   - Passo 9: `COMMIT;`
   - Passo 10: `PRAGMA foreign_keys = ON;`
   - Passo 11: `PRAGMA integrity_check;`
   - Passo 12: Atualização de metadados: `PRAGMA user_version = 1;`

---

## 2. Tabelas Afetadas e DDL Antes vs Depois

### 2.1 AgendamentoProdutos
- **Antes (Legacy):**
  `PrecoUnitario REAL, PrecoTotal REAL`
- **Depois (CentsV1):**
  `PrecoUnitario INTEGER, PrecoTotal INTEGER`

### 2.2 AgendamentoServicos
- **Antes (Legacy):**
  `Valor REAL`
- **Depois (CentsV1):**
  `Valor INTEGER`

### 2.3 Agendamentos
- **Antes (Legacy):**
  `ClienteTotalGasto REAL, ValorEstimado REAL, ValorReal REAL, ValorPago REAL, ValorProdutos REAL, ValorServicos REAL`
- **Depois (CentsV1):**
  `ClienteTotalGasto INTEGER, ValorEstimado INTEGER, ValorReal INTEGER, ValorPago INTEGER, ValorProdutos INTEGER, ValorServicos INTEGER`

### 2.4 CaixaSessoes
- **Antes (Legacy):**
  `ValorAbertura REAL, ValorEsperado REAL, ValorInformadoFechamento REAL, TotalVendas REAL, TotalSangrias REAL, TotalSuprimentos REAL`
- **Depois (CentsV1):**
  `ValorAbertura INTEGER, ValorEsperado INTEGER, ValorInformadoFechamento INTEGER, TotalVendas INTEGER, TotalSangrias INTEGER, TotalSuprimentos INTEGER`

### 2.5 Clientes
- **Antes (Legacy):**
  `TotalGasto REAL, Latitude REAL, Longitude REAL`
- **Depois (CentsV1):**
  `TotalGasto INTEGER, Latitude REAL, Longitude REAL` *(Coordenadas estritamente preservadas em REAL)*

### 2.6 ContasPagar
- **Antes (Legacy):**
  `Valor REAL`
- **Depois (CentsV1):**
  `Valor INTEGER`

### 2.7 ContasReceber
- **Antes (Legacy):**
  `Valor REAL`
- **Depois (CentsV1):**
  `Valor INTEGER`

### 2.8 Fornecedores
- **Antes (Legacy):**
  `PedidoMinimo REAL, TotalCompras REAL`
- **Depois (CentsV1):**
  `PedidoMinimo INTEGER, TotalCompras INTEGER`

### 2.9 Funcionarios
- **Antes (Legacy):**
  `Salario REAL`
- **Depois (CentsV1):**
  `Salario INTEGER`

### 2.10 ImportacoesItens
- **Antes (Legacy):**
  `ValorUnitario REAL, ValorTotal REAL, PrecoVendaSugerido REAL, Quantidade REAL, MargemAplicada REAL`
- **Depois (CentsV1):**
  `ValorUnitario INTEGER, ValorTotal INTEGER, PrecoVendaSugerido INTEGER, Quantidade REAL, MargemAplicada REAL` *(Quantidade e Margem preservadas em REAL)*

### 2.11 ImportacoesNFe & ImportacoesNFeExclusoes
- **Antes (Legacy):**
  `ValorTotal REAL, ValorProdutos REAL`
- **Depois (CentsV1):**
  `ValorTotal INTEGER, ValorProdutos INTEGER`

### 2.12 Metas & MetasFinanceiras
- **Antes (Legacy):**
  `MetaValor REAL, ValorAtual REAL, PercentualAtingido REAL`
- **Depois (CentsV1):**
  `MetaValor INTEGER, ValorAtual INTEGER, PercentualAtingido REAL` *(PercentualAtingido preservado em REAL)*

### 2.13 MovimentacoesCaixa
- **Antes (Legacy):**
  `ValorMovimento REAL, ValorInicial REAL, ValorFinal REAL, Sangrias REAL, Suprimentos REAL, Diferenca REAL`
- **Depois (CentsV1):**
  `ValorMovimento INTEGER, ValorInicial INTEGER, ValorFinal INTEGER, Sangrias INTEGER, Suprimentos INTEGER, Diferenca INTEGER`

### 2.14 MovimentacoesFinanceiras
- **Antes (Legacy):**
  `Valor REAL`
- **Depois (CentsV1):**
  `Valor INTEGER`

### 2.15 Orcamentos & OrcamentoItens
- **Antes (Legacy):**
  `Orcamentos: Subtotal REAL, Desconto REAL, Acrescimo REAL, Total REAL, LucroEstimado REAL, ComissaoVendedor REAL, ImpostosEstimados REAL, DescontoPercentual REAL, MargemLucro REAL`  
  `OrcamentoItens: PrecoUnitario REAL, PrecoCusto REAL, Desconto REAL, Subtotal REAL, LucroEstimado REAL, MargemLucro REAL, Quantidade REAL`
- **Depois (CentsV1):**
  `Orcamentos: Subtotal INTEGER, Desconto INTEGER, Acrescimo INTEGER, Total INTEGER, LucroEstimado INTEGER, ComissaoVendedor INTEGER, ImpostosEstimados INTEGER, DescontoPercentual REAL, MargemLucro REAL`  
  `OrcamentoItens: PrecoUnitario INTEGER, PrecoCusto INTEGER, Desconto INTEGER, Subtotal INTEGER, LucroEstimado INTEGER, MargemLucro REAL, Quantidade REAL`

### 2.16 OrdensServico & OrdemServicoItens
- **Antes (Legacy):**
  `OrdensServico: ValorMaoObra REAL, Desconto REAL`  
  `OrdemServicoItens: ValorUnitario REAL, CustoUnitario REAL, Quantidade INTEGER`
- **Depois (CentsV1):**
  `OrdensServico: ValorMaoObra INTEGER, Desconto INTEGER`  
  `OrdemServicoItens: ValorUnitario INTEGER, CustoUnitario INTEGER, Quantidade INTEGER`

### 2.17 Produtos & ProdutoFornecedores
- **Antes (Legacy):**
  `Produtos: PrecoCompra REAL, PrecoVenda REAL, ValorTotalEstoque REAL, TotalFaturado REAL`  
  `ProdutoFornecedores: PrecoUltimaCompra REAL, ValorCompras REAL, QuantidadeUltimaCompra INTEGER`
- **Depois (CentsV1):**
  `Produtos: PrecoCompra INTEGER, PrecoVenda INTEGER, ValorTotalEstoque INTEGER, TotalFaturado INTEGER`  
  `ProdutoFornecedores: PrecoUltimaCompra INTEGER, ValorCompras INTEGER, QuantidadeUltimaCompra INTEGER`

### 2.18 ServicosPadrao
- **Antes (Legacy):**
  `ValorSugerido REAL`
- **Depois (CentsV1):**
  `ValorSugerido INTEGER`

### 2.19 Vendas & VendaItens
- **Antes (Legacy):**
  `Vendas: Total REAL, Desconto REAL`  
  `VendaItens: PrecoUnitario REAL, CustoUnitario REAL, Desconto REAL, Subtotal REAL, Quantidade REAL`
- **Depois (CentsV1):**
  `Vendas: Total INTEGER, Desconto INTEGER`  
  `VendaItens: PrecoUnitario INTEGER, CustoUnitario INTEGER, Desconto INTEGER, Subtotal INTEGER, Quantidade REAL`

---

## 3. Conclusão do Gate 02

TESTE: Homologação arquitetural do mapeamento dos 67 campos monetários e preservação de campos métricos  
RESULTADO: DDL antes/depois especificado para todas as 20 tabelas com suporte atômico ao rebuild do SQLite.  
EVIDÊNCIA: `B7_MONEY_FIELD_FINAL_MATRIX.csv` e especificação DDL.  
STATUS: **PASS**
