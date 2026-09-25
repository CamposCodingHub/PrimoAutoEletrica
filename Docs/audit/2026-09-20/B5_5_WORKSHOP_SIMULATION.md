# PRIMOX Workshop — Fase B5.5: Simulação de Oficina Real

**Data:** 2026-09-24  
**Fase:** B5.5 — Mega Homologação Operacional de Oficina Real  
**Escopo:** Simulação de Ciclo Completo de Atendimento Comercial e Técnico  

---

## 1. Fluxo Operacional Simulado

A simulação reproduziu a rotina diária de uma oficina de autoelétrica moderna:

1. **08:00 — Recepção e Triagem (Cliente 1 Chega):**
   - Cadastro do cliente `B5_5_QA_CLIENTE_001` (João da Silva, CPF `555.001.001-55`).
   - Cadastro do veículo `B5_5_QA_VEICULO_001` (Fiat Strada 1.4, Placa `B5501QA`).
   - Registro de sintoma: *"Partida pesada e oscilação no circuito de carga"*.

2. **08:30 — Inspeção Inicial e Orçamento:**
   - Criação do orçamento `B5_5_QA_ORC_001`.
   - Adição de peças (`B5_5_QA_PRODUTO_001` - Alternador Bosch 80A, R$ 690,00).
   - Adição de serviços (`B5_5_QA_SERVICO_001` - Diagnóstico & Reparo Especializado, R$ 135,00).
   - Aplicação de desconto promocional autorizado de R$ 10,00. Subtotal: R$ 825,00, Total: R$ 815,00.
   - Salvo, reaberto, editado e aprovado com inspeção visual DVI.

3. **09:15 — Aprovação e Conversão para Ordem de Serviço:**
   - Conversão em `B5_5_QA_OS_001`.
   - Herança de snapshots de cliente, placa e modelo sem perda de integridade.
   - Vinculação estrita de chaves estrangeiras (`ClienteId`, `VeiculoId`, `OrcamentoId`).

4. **10:00 — Diagnóstico Pericial Autoelétrica:**
   - Roteiro `D02` (Alternador e Sistema de Carga) executado.
   - Medição inicial: 14.2V com ondulação excessiva. Causa: regulador avariado.
   - Ação corretiva: substituição do regulador e retificação de bornes.
   - Teste pós-reparo: 13.8V estabilizado sob carga (SAE J1113). Resultado: **APROVADO**.

5. **11:00 — Checklist Multiponto e Saída:**
   - Preenchimento dos 6 itens do checklist técnico (`OK`, `ATENCAO`, `FALHA`, `NAO_TESTADO`, `NAO_DISPONIVEL`, `NA`).
   - Conclusão do laudo técnico pericial pelo eletricista responsável.

6. **11:30 — Fechamento Financeiro e Pós-Venda:**
   - Pagamento via PIX registrado no Caixa Diário.
   - Movimentação de estoque confirmada (-1 un alternador).
   - Registro de acompanhamento pós-venda gerado por GUIDs (`NotaSatisfacaoNPS = 10`).

---

## 2. Matriz de Validação da Simulação

| Etapa | Operação | Status | Evidência / Registro |
| :--- | :--- | :---: | :--- |
| **Recepção** | Cadastro Cliente e Veículo | **PASS** | `B5_5_QA_CLIENTE_001`, `B5_5_QA_VEICULO_001` |
| **Orçamento** | Criação, Edição, Desconto e DVI | **PASS** | `B5_5_QA_ORC_001` (R$ 815,00) |
| **Conversão** | Orçamento -> OS com Snapshots | **PASS** | `B5_5_QA_OS_001` gerada e persistida |
| **Estoque** | Baixa de produto por consumo em OS | **PASS** | Estoque atualizado de 21 para 20 un |
| **Autoelétrica** | Diagnóstico técnico D01-D06 12V/24V | **PASS** | Laudos periciais salvos em JSON pericial |
| **Checklist** | 6 status operacionais validados | **PASS** | Checklist concluído com bloqueio de edição |
| **Pós-Venda** | Vínculo estrito por GUIDs | **PASS** | `Comercial/pos-venda.json` registrado |
| **Financeiro** | Recebimento, sessão de caixa e fechamento | **PASS** | Caixa diário balanceado e fechado |
