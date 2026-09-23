# PRIMOX Workshop — DESENHO FINAL DA MIGRATION DE MONEY

> **Documento:** P2_MONEY_MIGRATION_DESIGN.md  
> **Data:** 23/09/2026  
> **Status:** DESENHO TÉCNICO E ARQUITETURAL COMPLETO  
> **Estado Operacional:** ⛔ **MONEY MIGRATION = BLOCKED**  
>
> ⚠️ **NENHUMA ALTERAÇÃO EXECUTADA NESTA FASE:**  
> • Sem `ALTER TABLE`  
> • Sem conversão `REAL → INTEGER`  
> • Sem alteração de schema  
> • Sem alteração de dados  
> • Sem rebuild de tabelas  
> • Sem alteração global de repositórios  
> • Branch `main` permanece 100% intocada  

---

## 1. INTRODUÇÃO E OBJETIVO

Este documento estabelece o **desenho final, determinístico e de risco mínimo** para a futura migração de campos monetários do SQLite no PRIMOX Workshop.

O sistema atualmente declara colunas monetárias como `REAL` no SQLite (armazenadas internamente como ponto flutuante de precisão dupla IEEE 754 de 64 bits). No entanto, toda a camada de modelo em C# (`Models/*.cs`) já opera com o tipo de alta precisão `decimal`.  
O objetivo da migração futura é substituir `REAL` por `INTEGER cents` (inteiro de 64 bits representando centavos) na persistência, eliminando qualquer risco de representação inexata em cálculos contábeis, fechamentos de caixa e relatórios fiscais.

---

## 2. AUDITORIA DOS 36 CAMPOS FINANCEIROS (FASE 2.01)

### 2.1 Os 18 Campos FINANCEIRO_CRÍTICO
Campos que afetam diretamente o fluxo de caixa, pagamentos de clientes, recebimentos e liquidações operacionais.

```
Tabela               Coluna                      Tipo SQLite  Propriedade C#              Tipo C#   Nullable  Default
---------------------------------------------------------------------------------------------------------------------
Vendas               Total                       REAL         Total                       decimal   NÃO       None
Vendas               Desconto                    REAL         Desconto                    decimal   NÃO       0
VendaItens           PrecoUnitario               REAL         PrecoUnitario               decimal   NÃO       None
VendaItens           CustoUnitario               REAL         CustoUnitario               decimal   NÃO       0
VendaItens           Desconto                    REAL         Desconto                    decimal   NÃO       0
VendaItens           Subtotal                    REAL         Subtotal                    decimal   NÃO       None
CaixaSessoes         ValorAbertura               REAL         ValorAbertura               decimal   NÃO       0
CaixaSessoes         ValorEsperado               REAL         ValorEsperado               decimal   NÃO       0
CaixaSessoes         ValorInformadoFechamento    REAL         ValorInformadoFechamento    decimal?  SIM       None
CaixaSessoes         TotalVendas                 REAL         TotalVendas                 decimal   NÃO       0
CaixaSessoes         TotalSangrias               REAL         TotalSangrias               decimal   NÃO       0
CaixaSessoes         TotalSuprimentos            REAL         TotalSuprimentos            decimal   NÃO       0
MovimentacoesCaixa   ValorMovimento              REAL         ValorMovimento              decimal   NÃO       0
MovimentacoesCaixa   ValorInicial                REAL         ValorInicial                decimal   NÃO       0
MovimentacoesCaixa   ValorFinal                  REAL         ValorFinal                  decimal   NÃO       0
MovimentacoesCaixa   Sangrias                    REAL         Sangrias                    decimal   NÃO       0
MovimentacoesCaixa   Suprimentos                 REAL         Suprimentos                 decimal   NÃO       0
MovimentacoesCaixa   Diferenca                   REAL         Diferenca                   decimal   NÃO       0
```

#### Ficha Técnica Detalhada dos Críticos:

1. **`Vendas.Total`**:  
   - *Onde é gravada:* `VendaRepository.InserirVenda` (linha 66).  
   - *Onde é lida:* `VendaRepository.ObterPorId` (linhas 278, 330), `RelatorioDatabaseService`.  
   - *Onde é calculada:* `PDVViewModel.AtualizarTotal` (`Subtotal - DescontoGeral`).  
   - *Fonte de verdade:* Sim (representa o valor final cobrado da transação).  
   - *Pode ser derivada:* Sim, a partir de `SUM(VendaItens.Subtotal) - Vendas.Desconto`.  

2. **`Vendas.Desconto`**:  
   - *Onde é gravada:* `VendaRepository.InserirVenda` (linha 68).  
   - *Onde é lida:* `VendaRepository.ObterPorId` (linhas 280, 332).  
   - *Onde é calculada:* Digitada pelo operador no PDV / `PDVViewModel.DescontoGeral`.  
   - *Fonte de verdade:* Sim (desconto concedido globalmente na venda).  
   - *Pode ser derivada:* Não.  

3. **`VendaItens.PrecoUnitario`**:  
   - *Onde é gravada:* `VendaRepository.InserirItemVenda` (linha 115).  
   - *Onde é lida:* `VendaRepository.ObterItensDaVenda`.  
   - *Onde é calculada:* Copiada de `Produto.PrecoVenda` ou editada com permissão.  
   - *Fonte de verdade:* Sim (snapshot do preço praticado no item).  
   - *Pode ser derivada:* Não.  

4. **`VendaItens.CustoUnitario`**:  
   - *Onde é gravada:* `VendaRepository.InserirItemVenda` (linha 116).  
   - *Onde é lida:* `VendaRepository.ObterItensDaVenda`, `RelatorioDatabaseService`.  
   - *Onde é calculada:* Copiada de `Produto.PrecoCompra` no momento da venda.  
   - *Fonte de verdade:* Sim (snapshot do custo contábil do item).  
   - *Pode ser derivada:* Não.  

5. **`VendaItens.Desconto`**:  
   - *Onde é gravada:* `VendaRepository.InserirItemVenda` (linha 117).  
   - *Onde é lida:* `VendaRepository.ObterItensDaVenda`.  
   - *Onde é calculada:* Informada no item no carrinho.  
   - *Fonte de verdade:* Sim.  
   - *Pode ser derivada:* Não.  

6. **`VendaItens.Subtotal`**:  
   - *Onde é gravada:* `VendaRepository.InserirItemVenda` (linha 118).  
   - *Onde é lida:* `VendaRepository.ObterItensDaVenda`.  
   - *Onde é calculada:* `ItemVenda.Subtotal` => `(PrecoUnitario * Quantidade) - Desconto`.  
   - *Fonte de verdade:* Não (é calculada).  
   - *Pode ser derivada:* Sim, exatamente por `(PrecoUnitario * Quantidade) - Desconto`.  

7. **`CaixaSessoes.ValorAbertura`**:  
   - *Onde é gravada:* `CaixaService.AbrirCaixa` (linha 85).  
   - *Onde é lida:* `CaixaService.MapearSessao` (linha 723).  
   - *Onde é calculada:* Informada pelo operador na abertura (fundo de troco).  
   - *Fonte de verdade:* Sim.  
   - *Pode ser derivada:* Não.  

8. **`CaixaSessoes.ValorEsperado`**:  
   - *Onde é gravada:* `CaixaService.AbrirCaixa`, `AtualizarSessao`.  
   - *Onde é lida:* `CaixaService.MapearSessao` (linha 724).  
   - *Onde é calculada:* `ValorAbertura + TotalVendas + TotalSuprimentos - TotalSangrias`.  
   - *Fonte de verdade:* Não.  
   - *Pode ser derivada:* Sim, por agregação de `MovimentacoesCaixa`.  

9. **`CaixaSessoes.ValorInformadoFechamento`**:  
   - *Onde é gravada:* `CaixaService.FecharCaixa` (linha 294).  
   - *Onde é lida:* `CaixaService.MapearSessao` (linha 725).  
   - *Onde é calculada:* Digitada pelo operador na contagem física do caixa.  
   - *Fonte de verdade:* Sim.  
   - *Pode ser derivada:* Não.  

10. **`CaixaSessoes.TotalVendas`**:  
    - *Onde é gravada:* `CaixaService.RegistrarVendaNaSessao` (`TotalVendas + venda.Total`).  
    - *Onde é lida:* `CaixaService.MapearSessao` (linha 726).  
    - *Onde é calculada:* Acumulador incremental.  
    - *Fonte de verdade:* Não.  
    - *Pode ser derivada:* Sim, por `SELECT SUM(Total) FROM Vendas WHERE CaixaSessaoId = ...`.  

11. **`CaixaSessoes.TotalSangrias`**:  
    - *Onde é gravada:* `CaixaService.RegistrarSangria`.  
    - *Onde é lida:* `CaixaService.MapearSessao` (linha 727).  
    - *Onde é calculada:* Acumulador incremental de retiradas.  
    - *Fonte de verdade:* Não.  
    - *Pode ser derivada:* Sim, por `SELECT SUM(ValorMovimento) FROM MovimentacoesCaixa WHERE Tipo = 'Sangria'`.  

12. **`CaixaSessoes.TotalSuprimentos`**:  
    - *Onde é gravada:* `CaixaService.RegistrarSuprimento`.  
    - *Onde é lida:* `CaixaService.MapearSessao` (linha 728).  
    - *Onde é calculada:* Acumulador incremental de aportes.  
    - *Fonte de verdade:* Não.  
    - *Pode ser derivada:* Sim, por `SELECT SUM(ValorMovimento) FROM MovimentacoesCaixa WHERE Tipo = 'Suprimento'`.  

13. **`MovimentacoesCaixa.ValorMovimento`**:  
    - *Onde é gravada:* `CaixaService.InserirMovimentacaoCaixa` (linha 618).  
    - *Onde é lida:* `CaixaService.MapearMovimentacao` (linha 745).  
    - *Onde é calculada:* Valor do evento ocorrido (venda, sangria ou suprimento).  
    - *Fonte de verdade:* Sim (registro imutável do evento).  
    - *Pode ser derivada:* Não.  

14. **`MovimentacoesCaixa.ValorInicial`**:  
    - *Onde é gravada:* `CaixaService.InserirMovimentacaoCaixa` (linha 619).  
    - *Onde é lida:* `CaixaService.MapearMovimentacao` (linha 746).  
    - *Onde é calculada:* Saldo anterior à movimentação.  
    - *Fonte de verdade:* Snapshot histórico.  
    - *Pode ser derivada:* Sim (saldo final da movimentação imediatamente anterior).  

15. **`MovimentacoesCaixa.ValorFinal`**:  
    - *Onde é gravada:* `CaixaService.InserirMovimentacaoCaixa` (linha 620).  
    - *Onde é lida:* `CaixaService.MapearMovimentacao` (linha 747).  
    - *Onde é calculada:* Saldo resultante após a movimentação.  
    - *Fonte de verdade:* Snapshot histórico.  
    - *Pode ser derivada:* Sim (`ValorInicial + delta`).  

16. **`MovimentacoesCaixa.Sangrias`**:  
    - *Onde é gravada:* `CaixaService.InserirMovimentacaoCaixa` (linha 621).  
    - *Onde é lida:* `CaixaService.MapearMovimentacao` (linha 748).  
    - *Onde é calculada:* Total acumulado de sangrias até o instante do evento.  
    - *Fonte de verdade:* Snapshot histórico.  
    - *Pode ser derivada:* Sim.  

17. **`MovimentacoesCaixa.Suprimentos`**:  
    - *Onde é gravada:* `CaixaService.InserirMovimentacaoCaixa` (linha 622).  
    - *Onde é lida:* `CaixaService.MapearMovimentacao` (linha 749).  
    - *Onde é calculada:* Total acumulado de suprimentos até o instante do evento.  
    - *Fonte de verdade:* Snapshot histórico.  
    - *Pode ser derivada:* Sim.  

18. **`MovimentacoesCaixa.Diferenca`**:  
    - *Onde é gravada:* `CaixaService.InserirMovimentacaoCaixa` (linha 623).  
    - *Onde é lida:* `CaixaService.MapearMovimentacao` (linha 750).  
    - *Onde é calculada:* No fechamento: `ValorInformado - SaldoEsperado`; em sangrias: `-valor`.  
    - *Fonte de verdade:* Sim (apurada no fechamento).  
    - *Pode ser derivada:* Sim (`ValorFinal - ValorInicial`).  

---

### 2.2 Os 18 Campos FINANCEIRO_CALCULADO
Campos derivados ou totalizadores de entidades comerciais (Ordens de Serviço e Orçamentos).

```
Tabela               Coluna               Tipo SQLite  Propriedade C#       Tipo C#   Nullable  Default
------------------------------------------------------------------------------------------------------------
OrdensServico        ValorMaoObra         REAL         ValorMaoObra         decimal   NÃO       0
OrdensServico        Desconto             REAL         Desconto             decimal   NÃO       0
OrdemServicoItens    Quantidade           REAL         Quantidade           decimal   NÃO       1
OrdemServicoItens    ValorUnitario        REAL         ValorUnitario        decimal   NÃO       0
OrdemServicoItens    CustoUnitario        REAL         CustoUnitario        decimal   NÃO       0
Orcamentos           Subtotal             REAL         Subtotal             decimal   SIM       None
Orcamentos           Desconto             REAL         Desconto             decimal   SIM       None
Orcamentos           DescontoPercentual   REAL         DescontoPercentual   decimal   NÃO       0
Orcamentos           Acrescimo            REAL         Acrescimo            decimal   SIM       None
Orcamentos           Total                REAL         Total                decimal   SIM       None
Orcamentos           MargemLucro          REAL         MargemLucro          decimal   SIM       None
Orcamentos           LucroEstimado        REAL         LucroEstimado        decimal   SIM       None
Orcamentos           ComissaoVendedor     REAL         ComissaoVendedor     decimal   SIM       None
Orcamentos           ImpostosEstimados    REAL         ImpostosEstimados    decimal   SIM       None
OrcamentoItens       PrecoUnitario        REAL         PrecoUnitario        decimal   SIM       None
OrcamentoItens       PrecoCusto           REAL         PrecoCusto           decimal   SIM       None
OrcamentoItens       Desconto             REAL         Desconto             decimal   SIM       None
OrcamentoItens       Subtotal             REAL         Subtotal             decimal   SIM       None
```

#### Ficha Técnica Detalhada dos Calculados:

19. **`OrdensServico.ValorMaoObra`**:  
    - *Onde é gravada:* `DatabaseService.OrdensServico.cs` (linha 1500).  
    - *Onde é lida:* Linha 1267 (`ReadDecimal`).  
    - *Onde é calculada:* Linha 1550 (`ordem.Itens.Where(i => i.Tipo == "Servico").Sum(i => i.Total)`).  
    - *Fonte de verdade:* Não.  
    - *Pode ser derivada:* **SIM (100% derivável)** dos itens de serviço da OS.  
    - *Oportunidade:* Pode ser mantida como coluna cacheada ou gerada via view/cálculo em memória.  

20. **`OrdensServico.Desconto`**:  
    - *Onde é gravada:* `DatabaseService.OrdensServico.cs` (linha 1501).  
    - *Onde é lida:* Linha 1268.  
    - *Onde é calculada:* Informada pelo usuário na janela da OS.  
    - *Fonte de verdade:* Sim (desconto concedido na OS).  
    - *Pode ser derivada:* Não.  

21. **`OrdemServicoItens.Quantidade`**:  
    - *Onde é gravada:* `DatabaseService.OrdensServico.cs` (linha 1580).  
    - *Onde é lida:* `ReadDecimal(reader, 5)`.  
    - *Onde é calculada:* Informada no item da OS.  
    - *Fonte de verdade:* Sim.  
    - *Pode ser derivada:* Não (é quantidade).  

22. **`OrdemServicoItens.ValorUnitario`**:  
    - *Onde é gravada:* `DatabaseService.OrdensServico.cs` (linha 1581).  
    - *Onde é lida:* `ReadDecimal(reader, 6)`.  
    - *Onde é calculada:* Copiada de Produto/Serviço ou editada.  
    - *Fonte de verdade:* Sim.  
    - *Pode ser derivada:* Não.  

23. **`OrdemServicoItens.CustoUnitario`**:  
    - *Onde é gravada:* `DatabaseService.OrdensServico.cs` (linha 1582).  
    - *Onde é lida:* `ReadDecimal(reader, 7)`.  
    - *Onde é calculada:* Copiada de Produto.PrecoCompra.  
    - *Fonte de verdade:* Sim.  
    - *Pode ser derivada:* Não.  

24. **`Orcamentos.Subtotal`**:  
    - *Onde é gravada:* `OrcamentoDatabaseService.cs` (linha 726).  
    - *Onde é lida:* Linha 670 (`ReadDecimal`).  
    - *Onde é calculada:* Linha 1090 (`subtotal += item.Subtotal`).  
    - *Fonte de verdade:* Não.  
    - *Pode ser derivada:* **SIM (100% derivável)** de `SUM(OrcamentoItens.Subtotal)`.  

25. **`Orcamentos.Desconto`**:  
    - *Onde é gravada:* `OrcamentoDatabaseService.cs` (linha 727).  
    - *Onde é lida:* Linha 671.  
    - *Onde é calculada:* Informada pelo vendedor em valor nominal ou derivada de `DescontoPercentual`.  
    - *Fonte de verdade:* Sim.  
    - *Pode ser derivada:* Se for em percentual: `Subtotal * (DescontoPercentual / 100)`.  

26. **`Orcamentos.DescontoPercentual`**:  
    - *Onde é gravada:* `OrcamentoDatabaseService.cs` (linha 729).  
    - *Onde é lida:* Linha 673.  
    - *Onde é calculada:* Informada pelo vendedor.  
    - *Fonte de verdade:* Sim.  
    - *Pode ser derivada:* Não.  

27. **`Orcamentos.Acrescimo`**:  
    - *Onde é gravada:* `OrcamentoDatabaseService.cs` (linha 730).  
    - *Onde é lida:* Linha 674.  
    - *Onde é calculada:* Informada pelo vendedor (ex: frete, taxa extra).  
    - *Fonte de verdade:* Sim.  
    - *Pode ser derivada:* Não.  

28. **`Orcamentos.Total`**:  
    - *Onde é gravada:* `OrcamentoDatabaseService.cs` (linha 731).  
    - *Onde é lida:* Linha 675.  
    - *Onde é calculada:* Linha 1091 (`Math.Max(0m, Subtotal - Desconto + Acrescimo)`).  
    - *Fonte de verdade:* Não.  
    - *Pode ser derivada:* **SIM (100% derivável)** de `Subtotal - Desconto + Acrescimo`.  

29. **`Orcamentos.MargemLucro`**:  
    - *Onde é gravada:* `OrcamentoDatabaseService.cs` (linha 732).  
    - *Onde é lida:* Linha 676.  
    - *Onde é calculada:* Linha 1093 (`Total > 0 ? (LucroEstimado / Total) * 100m : 0m`).  
    - *Fonte de verdade:* Não.  
    - *Pode ser derivada:* **SIM (100% derivável)**.  

30. **`Orcamentos.LucroEstimado`**:  
    - *Onde é gravada:* `OrcamentoDatabaseService.cs` (linha 733).  
    - *Onde é lida:* Linha 677.  
    - *Onde é calculada:* Linha 1092 (`SUM(OrcamentoItens.LucroEstimado)`).  
    - *Fonte de verdade:* Não.  
    - *Pode ser derivada:* **SIM (100% derivável)**.  

31. **`Orcamentos.ComissaoVendedor`**:  
    - *Onde é gravada:* `OrcamentoDatabaseService.cs` (linha 734).  
    - *Onde é lida:* Linha 678.  
    - *Onde é calculada:* Informada ou calculada sobre `Total`.  
    - *Fonte de verdade:* Sim (snapshot da comissão negociada).  
    - *Pode ser derivada:* Pode ser calculada caso haja percentual fixo, mas é recomendável manter snapshot.  

32. **`Orcamentos.ImpostosEstimados`**:  
    - *Onde é gravada:* `OrcamentoDatabaseService.cs` (linha 735).  
    - *Onde é lida:* Linha 679.  
    - *Onde é calculada:* Estimativa baseada em alíquota fiscal.  
    - *Fonte de verdade:* Não.  
    - *Pode ser derivada:* **SIM (100% derivável)** a partir da alíquota fiscal configurada.  

33. **`OrcamentoItens.PrecoUnitario`**:  
    - *Onde é gravada:* `OrcamentoDatabaseService.cs` (linha 626).  
    - *Onde é lida:* Linha 701.  
    - *Onde é calculada:* Informada ou carregada do produto.  
    - *Fonte de verdade:* Sim.  
    - *Pode ser derivada:* Não.  

34. **`OrcamentoItens.PrecoCusto`**:  
    - *Onde é gravada:* `OrcamentoDatabaseService.cs` (linha 627).  
    - *Onde é lida:* Linha 702.  
    - *Onde é calculada:* Carregada do produto.  
    - *Fonte de verdade:* Sim (snapshot do custo no momento do orçamento).  
    - *Pode ser derivada:* Não.  

35. **`OrcamentoItens.Desconto`**:  
    - *Onde é gravada:* `OrcamentoDatabaseService.cs` (linha 628).  
    - *Onde é lida:* Linha 703.  
    - *Onde é calculada:* Informada no item.  
    - *Fonte de verdade:* Sim.  
    - *Pode ser derivada:* Não.  

36. **`OrcamentoItens.Subtotal`**:  
    - *Onde é gravada:* `OrcamentoDatabaseService.cs` (linha 629).  
    - *Onde é lida:* Linha 704.  
    - *Onde é calculada:* Linha 1136 (`(Quantidade * PrecoUnitario) - Desconto`).  
    - *Fonte de verdade:* Não.  
    - *Pode ser derivada:* **SIM (100% derivável)** de `(Quantidade * PrecoUnitario) - Desconto`.  

*(Nota adicional: `OrcamentoItens` também possui `LucroEstimado` e `MargemLucro`, que seguem exatamente a mesma regra: são 100% deriváveis).*

---

## 3. RELATÓRIO DE INSPEÇÃO DOS DADOS REAIS (FASE 2.02)

A inspeção executada diretamente no banco de dados ativo `primoauto.db` (20 MB, 59 tabelas) com conexões estritamente read-only comprovou:

- **Anomalias de Ponto Flutuante:** **“Nenhuma ocorrência encontrada.”**
- **Casas Decimais:** Todos os registros reais possuem no máximo 2 casas decimais.
- **Valores Negativos:** Nenhuma ocorrência em campos estritamente positivos.
- **Resíduos:** Zero resíduos de conversão binária IEEE 754.
- Evidência completa e detalhamento numérico documentados em [P2_MONEY_DATA_PROOF.md](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/P2_MONEY_DATA_PROOF.md).

---

## 4. MAPEAMENTO DE CÁLCULOS E OPERAÇÕES (FASE 2.03)

O levantamento de todas as operações aritméticas e conversões no código revelou:

### 4.1 Operações Aritméticas Básicas
1. **Adição (`+`)**:  
   - Soma de itens para Subtotal: `subtotal = Itens.Sum(i => i.Subtotal);`  
   - Acréscimo em orçamentos: `Total = Subtotal - Desconto + Acrescimo;`  
   - Depósito de suprimento no caixa: `saldoDepois = saldoAntes + valor;`  
2. **Subtração (`-`)**:  
   - Aplicação de desconto: `Total = Subtotal - Desconto;`  
   - Sangria no caixa: `saldoDepois = saldoAntes - valor;`  
   - Cálculo de diferença de conferência: `diferenca = valorInformado - saldoEsperado;`  
   - Margem bruta nominal: `lucro = subtotal - custo;`  
3. **Multiplicação (`*`)**:  
   - Subtotal bruto do item: `valorBruto = quantidade * precoUnitario;`  
   - Aplicação de percentual de desconto: `desconto = subtotal * (percentual / 100m);`  
   - Comissão: `comissao = total * percentualComissao;`  
4. **Divisão (`/`)**:  
   - Percentual de margem de lucro: `(lucro / total) * 100m;`  
   - Preço sugerido por markup: `custo / (1m - margemDesejada);`  

### 4.2 Métodos de Arredondamento e Conversão
- `Math.Round(valor, 2, MidpointRounding.AwayFromZero)`: Utilizado em módulos fiscais e importação de NF-e (`FiscalEmpresaModels.cs:28`, `ProdutoImportacaoService.cs:303,441`).  
- `Math.Round(valor, 2)`: Utilizado sem parâmetro em `ComissaoSettlementService.cs:59` e `PagamentoMistoWindow.xaml.cs:21,48` (usa implicitamente `ToEven` do .NET).  
- `Convert.ToDecimal(reader.GetValue(ordinal))`: Utilizado em `VendaRepository.cs:532`, `CaixaService.cs:788`, `DatabaseService.OrdensServico.cs`.  

### 4.3 Regras de Negócio Existentes
- **Desconto:** Nunca pode ultrapassar o Subtotal (`DescontoGeral > Subtotal ? Subtotal : DescontoGeral`).  
- **Total:** Nunca pode ser negativo (`Math.Max(0m, Total)`).  
- **Rateio Misto:** A soma das parcelas de dinheiro, PIX e cartão deve fechar exatamente com o total da venda (`decimal.Round(totalInformado - totalVenda, 2) == 0`).  
- **Juros e Parcelamento:** Inexistentes na versão desktop atual.  

---

## 5. REVISÃO DO MONEYCENTS E POLÍTICA DE ARREDONDAMENTO (FASE 2.04)

### 5.1 Análise da Política Atual (`MidpointRounding.AwayFromZero`)
A estrutura `MoneyCents` implementa:
```csharp
public static MoneyCents FromDecimal(decimal amount) =>
    new(decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero)));
```
- **Conformidade Contábil Brasileira:** O arredondamento `AwayFromZero` (onde `0,005` arredonda para `0,01`) é a norma mandatória da legislação brasileira (ABNT NBR 5891) e das regras de validação da SEFAZ para notas fiscais eletrônicas (NF-e/NFC-e).
- **Adequação:** Todos os casos reais do blast radius (soma de itens, rateio de pagamentos, comissões e descontos percentuais) são perfeitamente atendidos por essa política.
- **Recomendação de Harmonização:** Substituir chamadas soltas de `Math.Round(x, 2)` (que usam Banker's rounding / `ToEven`) por `MidpointRounding.AwayFromZero` para unificar todo o sistema no padrão comercial brasileiro.

---

## 6. ESTRATÉGIA DE PERSISTÊNCIA EM CAMADAS (FASE 2.05)

A migração NÃO converterá o tipo `decimal` globalmente no C#. A estratégia preserva a ergonomia do domínio e isola a conversão exclusivamente na fronteira de persistência:

```
┌────────────────────────────────────────────────────────┐
│                        UI (WPF)                        │
│   Exibição formatada em R$ (TextBoxes, DataGrids)     │
└───────────────────────────▲────────────────────────────┘
                            │ decimal
┌───────────────────────────▼────────────────────────────┐
│                    ViewModel / DTO                     │
│           Propriedades `decimal` (ou `MoneyCents`)     │
└───────────────────────────▲────────────────────────────┘
                            │ decimal
┌───────────────────────────▼────────────────────────────┐
│                 Domain / Services                      │
│     Lógica de negócio operando em `decimal` / cents    │
└───────────────────────────▲────────────────────────────┘
                            │ decimal (ou MoneyCents)
┌───────────────────────────▼────────────────────────────┐
│               Repositórios / Data Access               │
│  Mapeamento de leitura: reader.GetInt64(i) → decimal  │
│  Mapeamento de escrita: decimal → command.Add(cents)   │
└───────────────────────────▲────────────────────────────┘
                            │ long (INTEGER cents)
┌───────────────────────────▼────────────────────────────┐
│                    SQLite Database                     │
│                Colunas INTEGER (centavos)              │
└────────────────────────────────────────────────────────┘
```

**Ganhos desta arquitetura:**
1. Zero impacto em XAML, Bindings e ViewModels.
2. Zero quebra de regras de negócio em Services.
3. Máximo isolamento do blast radius (apenas comandos SQL nos Repositories são modificados).

---

## 7. PLANO DE MIGRAÇÃO TABELA POR TABELA (FASE 2.06)

O detalhamento completo coluna por coluna está documentado no arquivo anexo:  
👉 [P2_MONEY_TABLE_PLAN.csv](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/P2_MONEY_TABLE_PLAN.csv)

### Ordem Recomendada de Migração (10 Estágios Isolados):
1. **Estágio 1 (Isolado/Zero Risco):** `Agendamentos`, `AgendamentoProdutos`, `AgendamentoServicos` (sem dependência financeira de fechamento).
2. **Estágio 2 (Preços de Catálogo):** `ServicosPadrao`.
3. **Estágio 3 (Fornecedores):** `Fornecedores`, `ProdutoFornecedores`.
4. **Estágio 4 (Cadastros Básicos):** `Funcionarios` (`Salario`), `Clientes` (`TotalGasto`).
5. **Estágio 5 (Estoque):** `Produtos` (`PrecoCompra`, `PrecoVenda`, `ValorTotalEstoque`, `TotalFaturado`).
6. **Estágio 6 (Orçamentos):** `Orcamentos`, `OrcamentoItens`.
7. **Estágio 7 (Ordens de Serviço):** `OrdensServico`, `OrdemServicoItens`.
8. **Estágio 8 (PDV / Vendas):** `Vendas`, `VendaItens`.
9. **Estágio 9 (Operações de Caixa):** `CaixaSessoes`, `MovimentacoesCaixa`.
10. **Estágio 10 (Módulo Financeiro Central):** `ContasPagar`, `ContasReceber`, `MovimentacoesFinanceiras`.

---

## 8. ESTRATÉGIA DE VALIDAÇÃO PRÉ/PÓS COM TOLERÂNCIA ZERO (FASE 2.07)

Para certificar que **nenhum centavo seja alterado ou corrompido**, a validação de cada tabela seguirá o protocolo de tolerância zero:

1. **Contagem Idêntica de Registros**:
   $$\text{COUNT}(*)_{\text{novo}} = \text{COUNT}(*)_{\text{antigo}}$$
2. **Contagem Idêntica de Nulos**:
   $$\text{COUNT}(\text{coluna IS NULL})_{\text{novo}} = \text{COUNT}(\text{coluna IS NULL})_{\text{antigo}}$$
3. **Equivalência Monetária Exata**:
   $$\sum \text{cents}_{\text{novo}} = \text{ROUND}\left(\sum \text{real}_{\text{antigo}} \times 100\right)$$
4. **Conferência Registro a Registro**:
   Query comparativa direta entre a tabela convertida e a shadow table prévia:
   ```sql
   SELECT t.Id, t.ValorCents, s.ValorReal
   FROM T t JOIN _shadow_pre_money_T s ON t.Id = s.Id
   WHERE t.ValorCents != CAST(ROUND(s.ValorReal * 100) AS INTEGER);
   ```
   **Resultado esperado:** Exatamente 0 linhas.
5. **Integridade Estrutural**:
   `PRAGMA integrity_check` e `PRAGMA foreign_key_check` retornando 0 erros.

---

## 9. ESTRATÉGIA DE ROLLBACK REAL (FASE 2.08)

O desenho completo de reversibilidade está documentado em:  
👉 [P2_MONEY_ROLLBACK.md](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/P2_MONEY_ROLLBACK.md)

Compreende:
- **Nível 1:** Rollback transacional imediato em falhas in-flight.
- **Nível 2:** Restauração atômica a partir de shadow tables (`_shadow_pre_money_*`).
- **Nível 3:** Restauração do arquivo físico pré-migration com validação de hash criptográfico SHA-256.

---

## 10. IMPACTO FUNCIONAL POR MÓDULO (FASE 2.09)

Mapeamento do impacto funcional decorrente da migração de persistência:

| Módulo | Criticidade | O que muda na migração | O que permanece inalterado |
|---|:---:|---|---|
| **PDV / Vendas** | 🔴 ALTO | `VendaRepository` grava/lê `Total`, `Desconto`, itens em centavos | Telas, cálculo de troco, atalhos, impressão de comprovante |
| **Caixa** | 🔴 ALTO | `CaixaService` manipula `ValorAbertura`, `ValorEsperado`, movimentações em cents no DB | Fechamento de caixa, fluxo de sangria/suprimento, UI |
| **Financeiro** | 🔴 ALTO | `FinanceiroDatabaseService` lê/grava `ContasPagar`, `ContasReceber` em cents | Relatórios de DRE, filtros por vencimento, baixa de títulos |
| **Orçamentos** | 🟡 MÉDIO | `OrcamentoDatabaseService` persiste itens e totais em cents | Aprovação, conversão em OS, impressão de PDF |
| **Ordens de Serviço** | 🟡 MÉDIO | `DatabaseService.OrdensServico.cs` persiste mão de obra e itens em cents | Fluxo de status, fotos antes/depois, checklist |
| **Estoque / Compras** | 🟡 MÉDIO | `DatabaseService.Produtos.cs` persiste preços de compra e venda em cents | Gestão de saldo, curva ABC, histórico |
| **Relatórios / BI** | 🟢 BAIXO | Queries de agregação no DB usam `SUM(cents) / 100.0` | Gráficos, métricas, layout de impressão |
| **API REST** | 🟢 BAIXO | Serialização JSON continua expondo `decimal` (ex: `150.00`) | Contratos externos, autenticação JWT, documentação Swagger |
| **Importação NF-e** | 🟡 MÉDIO | Gravação de XML importado mapeia itens diretamente para cents | Parser do XML, mapeamento de impostos, cálculo de markup |

---

## 11. CONFIRMAÇÃO DE GATES

```
[x] 18 FINANCEIRO_CRÍTICO detalhados
[x] 18 FINANCEIRO_CALCULADO detalhados
[x] Dados reais analisados em modo read-only
[x] Ausência de anomalias comprovada ("Nenhuma ocorrência encontrada")
[x] Cálculos existentes mapeados
[x] Política de arredondamento revisada (AwayFromZero validada)
[x] Tabelas que precisam de migration identificadas
[x] Campos deriváveis identificados como oportunidades
[x] Estratégia de persistência DB ↔ Repo ↔ Domain ↔ UI documentada
[x] Estratégia de migration table-by-table desenhada
[x] Estratégia de validação pré/pós tolerância zero definida
[x] Estratégia de rollback em 3 níveis documentada
[x] Impacto funcional mapeado em 9 módulos
[x] 4 documentos formais gerados em Docs/audit/2026-09-20/
```

> [!IMPORTANT]
> **DECLARAÇÃO DE CONFORMIDADE OBRIGATÓRIA:**  
> **MONEY MIGRATION = BLOCKED**  
> Nenhum `ALTER TABLE` foi executado.  
> Nenhum schema de banco de dados foi alterado.  
> Nenhum dado financeiro foi modificado ou convertido.  
> Nenhuma tabela financeira foi alterada.  
> A branch `main` permanece intacta.  
