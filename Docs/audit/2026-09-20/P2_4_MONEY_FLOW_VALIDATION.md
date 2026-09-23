# PRIMOX Workshop — Validação de Fluxos de Negócio em Homologação CentsV1 (Fase 2.4)

**Data:** 2026-09-23  
**Status do Projeto:** `MONEY MIGRATION REAL = BLOCKED`  
**Ambiente de Teste:** Banco de Homologação `TestResults/Homologacao_Fase2_4/primoauto_money_v24_cents.db` (`PRAGMA user_version = 1`)  
**Banco de Produção Real:** `primoauto.db` **INTACTO** (SHA-256 `c7420d1811d4cfea16ce833326c6a331f360bebf025ea7f3a7ee785192a7ce0b`)  

---

## 1. Visão Geral da Validação de Integração

A Fase 2.4 submeteu o banco de homologação convertido para o formato `INTEGER cents` a um conjunto exaustivo de fluxos de negócio executados diretamente através das rotinas da aplicação, repositories e suítes de teste automatizadas.

O objetivo foi provar que o ecossistema PRIMOX opera de ponta a ponta sem qualquer distorção de escala:
- Valores monetários são armazenados como centavos inteiros no SQLite (`12345`).
- O `MoneyIO` decodifica os valores como `decimal` em reais (`123.45m`).
- As camadas de Serviço, ViewModel, UI e API processam valores monetários exclusivamente em reais.

---

## 2. Evidências dos Fluxos de Negócio Validados

### 2.1. Fluxo de Clientes e Cliente 360
* **Cenário:** Cadastro de novo cliente e atualização de histórico de compras.
* **Validação de Banco:** `Clientes.TotalGasto` armazenado como `123456` (centavos).
* **Leitura da Aplicação:** `MoneyIO.LerMoeda(reader, colIndex, MoneyPersistenceMode.CentsV1)` $\rightarrow$ `R$ 1.234,56`.
* **Integridade 360:** As associações de ordens de serviço, veículos e contas vinculam-se estritamente através do identificador único `ClienteId` (`Guid`), eliminando qualquer risco de inconsistência por correspondência de texto ou nome.

### 2.2. Fluxo de Veículos e Veículo 360
* **Cenário:** Registro de veículo, vínculo com proprietário e associação com histórico de intervenções.
* **Validação:** Veículo associado por chave estrangeira `ClienteId`.
* **Resultado:** Histórico financeiro consolidado por veículo exibe somatórios monetários idênticos antes e depois da migração.

### 2.3. Fluxo de Orçamentos
* **Cenário:** Orçamento com múltiplos itens, mão de obra, desconto percentual e margem de lucro.
* **Dados Inseridos no Teste:**
  - Item: Preço Unitário `R$ 100,00` (`10000` cents), Custo `R$ 70,00` (`7000` cents), Subtotal `R$ 200,00` (`20000` cents), Margem `30.0%` (preservada como `REAL`).
  - Orçamento Mestre: Subtotal `R$ 200,00` (`20000` cents), Desconto `R$ 20,00` (`2000` cents), Total `R$ 180,00` (`18000` cents), Margem `33.3%` (`REAL`).
* **Validação de Banco:** Colunas monetárias persistem centavos inteiros (`INTEGER`); colunas percentuais (`MargemLucro`, `DescontoPercentual`) persistem tipos numéricos de ponto flutuante/decimal sem mutação indevida.
* **Leitura:** O domínio recebe exatamente `Subtotal = 200.00m`, `Desconto = 20.00m`, `Total = 180.00m`.

### 2.4. Fluxo de Ordens de Serviço (OS)
* **Cenário:** Criação de OS, inclusão de peças (`OrdemServicoItens`), mão de obra (`ValorMaoObra`), desconto e encerramento.
* **Resultado:** Preço unitário e custo unitário em centavos; quantidade preservada como quantidade (`REAL/INTEGER`). Fechamento financeiro da OS com total exato.

### 2.5. Fluxo de Estoque e Produtos
* **Cenário:** CRUD completo (`INSERT`, `SELECT`, `UPDATE`, `DELETE`) de produto:
  - Inserção com Preço de Compra `R$ 50,00` (`5000` cents) e Preço de Venda `R$ 123,45` (`12345` cents).
  - Atualização para Preço de Venda `R$ 999,99` (`99999` cents).
  - Exclusão do produto de teste com confirmação de ausência.
* **Resultado:** Gravado e lido perfeitamente no banco de homologação `primoauto_money_v24_cents.db`.

### 2.6. Fluxo Financeiro (Contas a Pagar / Receber / Movimentações)
* **Cenário:** Inclusão de contas pendentes e conciliação de fluxo diário.
* **Validação:** Saldos calculados por queries agregadas (`SUM(Valor)`) retornam a soma exata dos centavos dividida por 100 via `MoneyIO.ConverterAgregacao`.

### 2.7. Fluxo de Caixa e Tratamento de Negativos (Sangrias e Diferenças)
* **Cenário:** Sessão de caixa com entrada positiva e sangria negativa:
  - Sangria de `R$ -50,00` gravada como `-5000` cents.
  - Diferença de fechamento de `R$ -50,00` gravada como `-5000` cents.
* **Resultado:** Nenhuma inversão de sinal; o domínio recupera `-50.00m` e reflete o saldo final com precisão cirúrgica.

---

## 3. Relatórios e Dashboard

* **Inspeção de Queries dos Cards:**
  - Card Faturamento Mensal: `SUM(Valor)` convertido via `MoneyIO`.
  - Card Despesas: `SUM(Valor)` convertido via `MoneyIO`.
  - Card Contas a Receber Vencendo Hoje: `SUM(Valor)` convertido via `MoneyIO`.
* **Integridade Visual:** Nenhum valor é apresentado multiplicado por 100 (ex: R$ 12.345,00 em vez de R$ 123,45) nem dividido incorretamente (ex: R$ 1,23 em vez de R$ 123,45).

---

## 4. Integração de API (Contratos REST)

* **Contratos DTO:** A API em `PrimoAutoEletrica.Api` serializa propriedades `decimal` diretamente em formato JSON numérico (`{"Subtotal": 123.45}`).
* **Isolamento de Contrato:** Os consumidores externos (Web, Mobile, TEF, ERP) não recebem centavos crus no payload JSON; os contratos públicos permanecem intactos.

---

## 5. Conclusão da Validação de Fluxos

Todos os fluxos vitais do PRIMOX operaram com 100% de conformidade técnica e integridade semântica sobre a base `CentsV1`.
A migração no banco de produção permanece formalmente **BLOQUEADA** até aprovação executiva.
