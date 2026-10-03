# Manual de Operação — Gestão de Frotas B2B & Faturamento Periódico

**Módulo:** Gestão de Frotas B2B  
**Versão:** 2.1 Enterprise  
**Objetivo:** Gerenciar contratos corporativos, cadastros de frotistas e transportadoras, telemetria de KM por placa e faturamento periódico consolidado.

---

## 1. Visão Geral

O módulo de Frotas B2B do PRIMOX foi desenvolvido para oficinas que atendem empresas, locadoras, transportadoras e frotas de utilitários que demandam condições comerciais diferenciadas e faturamento agrupado (quinzenal ou mensal).

---

## 2. Cadastro de Contratos Corporativos

1. Acesse `Gestão de Frotas → Contratos → Novo Contrato`;
2. Selecione a empresa cliente cadastrada no sistema;
3. Defina as regras comerciais:
   * **Percentual de Desconto em Peças:** (ex.: 10%);
   * **Percentual de Desconto em Mão de Obra:** (ex.: 15%);
   * **Limite de Crédito Mensal:** Valor máximo para faturamento faturado sem aprovação prévia da diretoria;
   * **Ciclo de Fechamento:** Quinzenal ou Mensal;
   * **Condição de Pagamento:** (ex.: Boleto 15 dias após a emissão da fatura).
4. Clique em **Salvar Contrato**.

---

## 3. Cadastro e Vínculo de Veículos da Frota

1. Na aba de veículos do contrato, adicione as placas pertencentes àquela empresa;
2. Informe o condutor/motorista responsável (quando aplicável);
3. Defina observações específicas (ex.: "Exigir autorização por e-mail para serviços acima de R$ 1.500").

---

## 4. Atendimento e Telemetria de KM

1. Ao receber um veículo de frota na oficina, abra a Ordem de Serviço normalmente;
2. Ao selecionar a placa da frota, o sistema aplica automaticamente a tabela de preços do contrato corporativo;
3. **Obrigatório:** Informe a quilometragem atual (KM) exibida no hodômetro;
4. Durante o atendimento, as peças e serviços são apontados com base na tabela com desconto corporativo.

---

## 5. Fechamento do Ciclo e Faturamento Consolidado

1. Acesse `Gestão de Frotas → Faturamento Periódico`;
2. Selecione a empresa frotista;
3. Informe o período de apuração (ex.: `01/10/2026` a `15/10/2026`);
4. Clique em **"Buscar Ordens Concluídas"**;
5. O sistema lista todas as OS finalizadas para os veículos daquela empresa no período;
6. Revise os itens e clique em **"Gerar Faturamento Consolidado"**;
7. O sistema gera automaticamente:
   * **Títulos a Receber:** Lançados no módulo Financeiro com data de vencimento calculada;
   * **Romaneio Analítico de Frota:** Relatório detalhado por placa, condutor, data, KM e itens trocados;
8. Exporte o Romaneio em PDF e envie junto com a cobrança para o departamento financeiro da empresa parceira.
