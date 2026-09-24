# PRIMOX WORKSHOP — B5.0
## SIMULAÇÃO TEMPORAL DE UMA OFICINA REAL (DIA OPERACIONAL)

**Cenário:** Autoelétrica & Mecânica Diesel/Leve com 4 Boxes de Atendimento  
**Data da Simulação:** Um dia típico de trabalho (08:00 às 18:00)  
**Status do Fluxo:** **VALIDADO (FLUXO 360° CONTÍNUO)**

---

### 1. Cronograma Operacional e Interação com o Sistema

```
08:00 | ABERTURA DA OFICINA & CONFERÊNCIA
      | O Caixa abre o PRIMOX Workshop.
      | Operação: Tela 'Caixa Operacional' -> Abertura de Turno com Suprimento inicial de R$ 200,00.
      | O Gerente visualiza o Dashboard: 3 agendamentos marcados para o período da manhã.

08:15 | CHEGADA DO CLIENTE & TRIAGEM RÁPIDA
      | Caminhão VW Constellation 24.280 chega com reclamação de 'bateria descarregando durante a noite'.
      | Atendente abre tela 'Clientes' -> Busca pelo CNPJ da transportadora (já cadastrada).
      | Seleciona o veículo no 'Client360' -> Placa já associada ao ClienteId. Zero redigitação.

08:30 | DVI (DIGITAL VEHICLE INSPECTION) & PRONTUÁRIO
      | Técnico faz a vistoria inicial pelo tablet/laptop do box:
      | - Registra fotos da lataria e nível de fluídos.
      | - Consulta o Prontuário Elétrico do veículo: Sistema 24V, 2 Baterias de 100Ah em série, alternador 28V/80A.

09:00 | DIAGNÓSTICO TÉCNICO ESTRUTURADO (AUTOELÉTRICA)
      | Técnico inicia Roteiro D03 (Corrente de Fuga / Consumo Parasita):
      | - Medição 1: Corrente de repouso inicial = 0.85A (850mA) -> CRÍTICO (Limite esperado: 0.08A).
      | - Teste sistemático por fusíveis: remoção do fusível F14 (rastreador/antena) reduz corrente para 0.04A.
      | - Causa identificada: Chicote do rastreador em curto com o chassi após a cabine.
      | - Medição 2: Teste de CCA das baterias -> Bateria A com 820 CCA (OK), Bateria B com 410 CCA (Sulfatada/Danificada).

09:30 | ELABORAÇÃO DO ORÇAMENTO
      | Atendente puxa os dados do diagnóstico técnico com 1 clique:
      | - Itens adicionados: 1 Bateria Moura 100Ah 24V (R$ 780,00) + Reparo chicote elétrico (R$ 250,00).
      | - O sistema calcula margens e sugere valor total de R$ 1.030,00.
      | - Atendente clica em 'Enviar WhatsApp' -> Mensagem com token de aprovação enviada ao gestor de frota.

10:00 | APROVAÇÃO & CONVERSÃO EM ORDEM DE SERVIÇO
      | Gestor da transportadora aprova o orçamento.
      | Atendente clica em 'Converter em OS':
      | - Ordem de Serviço #1085 gerada instantaneamente.
      | - ClienteId, VeiculoId, itens de peças e serviços transpostos com 100% de fidelidade.

10:15 | SEPARAÇÃO DE PEÇAS & DISPENSAÇÃO
      | Almoxarife consulta o painel de requisições da OS #1085:
      | - Bateria Moura 100Ah: Localização física indicada: 'Corredor B, Prateleira 1, Posição 04'.
      | - Bateria retirada e entregue ao eletricista; saldo físico decrementado no Kardex.

11:00 | EXECUÇÃO DO REPARO TÉCNICO
      | Eletricista substitui a bateria danificada e isola o chicote rompido.
      | Realiza a equalização das baterias.

14:00 | TESTE PÓS-REPARO COM CÁLCULO DE DELTA (AUTOELÉTRICA 2.0)
      | Eletricista executa o teste de validação final:
      | - Corrente de fuga antes: 0.85A | Corrente de fuga depois: 0.04A | Delta: -0.81A (CONFORME).
      | - Tensão de carga do alternador com faróis ligados: 28.35V | Delta: +0.20V (CONFORME).
      | - Sistema grava laudo técnico estruturado no histórico da OS e no Vehicle360.

15:00 | CONCLUSÃO TÉCNICA DA OS
      | Eletricista clica em 'Concluir Serviço' no PRIMOX.
      | Sistema atualiza o status da OS para 'Pronto para Retirada' e notifica o balcão.

15:10 | FATURAMENTO & RECEBIMENTO NO CAIXA
      | Motorista comparece para retirar o caminhão:
      | - Caixa abre 'Recebimento de OS #1085':
      | - Condição: 1 parcela à vista no Pix (R$ 500,00) + 1 boleto a 30 dias para a transportadora (R$ 530,00).
      | - Caixa registra recebimento do Pix; Contas a Receber gera título a prazo vinculado ao ClienteId.
      | - Baixa final da OS disparada: movimentação de estoque efetivada em definitivo.

15:20 | ATUALIZAÇÃO DOS CADASTROS 360°
      | - No 'Vehicle360': Registrado histórico completo da troca da bateria e laudo de fuga.
      | - No 'Client360': Registrada movimentação financeira e histórico de pontualidade.

15:30 | AGENDAMENTO AUTOMÁTICO DE PÓS-VENDA
      | O serviço de 'Pós-Venda' gera automaticamente:
      | 1. Lembrete de Follow-up (D+7): Ligação para conferir se o veículo manteve partida imediata.
      | 2. Garantia da Bateria (12 meses): Alerta ativo caso o caminhão retorne com problema elétrico.
      | 3. Revisão Preventiva (180 dias): Lembrete automático para teste de alternador.

18:00 | FECHAMENTO DO DIA & AUDITORIA
      | Caixa encerra o turno: total de entradas em dinheiro/pix bate perfeitamente com o relatório do PRIMOX.
      | Administrador aciona o 'Backup Automático' -> Banco de dados validado via integridade física e copiado com sucesso.
```

---

### 2. Identificação de Fricções e Oportunidades de Melhoria

1. **Aprimoramento Visual do Checklist (Backlog B5-002):** A execução do checklist multiponto atualmente é feita dentro da aba de diagnóstico; a criação de uma tela visual dedicada otimizará o tempo do eletricista no box.
2. **Integração Direta com WhatsApp Oficial (Backlog B8):** O envio atual via link `wa.me` requer que o operador confirme o envio na interface web; no futuro, o envio via API oficial fará o disparo automático sem intervenção manual.
