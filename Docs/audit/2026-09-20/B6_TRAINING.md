# PRIMOX WORKSHOP — B6 ROTEIRO E REGISTRO DE TREINAMENTO
**Data:** 2026-09-25  
**Carga Horária:** ~4 Horas  
**Público:** Equipe Operacional da Oficina Piloto  

---

## 1. Estrutura dos Módulos do Treinamento

| Módulo | Conteúdo Abordado | Duração | Avaliação da Equipe |
|---|---|---|---|
| **1. Login & Segurança** | Acesso por usuário/senha, política de bloqueio após falhas e recuperação | 15 min | Entendido rapidamente |
| **2. Dashboard** | Visão geral do pátio, OS abertas, faturamento do dia e alertas | 15 min | Elogiada a clareza dos cards |
| **3. Clientes** | Cadastro de PF/PJ, validação de documento, telefone, consentimento LGPD | 20 min | Fluido e direto |
| **4. Veículos** | Placa padrão Mercosul e antigo, seleção de linha (12V Leve / 24V Pesado) | 20 min | Diferenciação 12V/24V muito útil |
| **5. Orçamentos** | Composição de serviços e peças, descontos percentuais e margem | 25 min | Simples e intuitivo |
| **6. Ordens de Serviço** | Conversão de orçamento em OS sem redigitação, atribuição de técnico | 20 min | Conversão em 1 clique aprovada |
| **7. Estoque & Peças** | Consulta de saldo, movimentações manuais e baixa automática na OS | 20 min | Baixa automática evita esquecimento |
| **8. Financeiro** | Fluxo de caixa diário, contas a pagar/receber e rateio exato de parcelas | 25 min | Rateio em centavos sem sobras |
| **9. Autoelétrica Técnica** | Rotas de diagnóstico guiado D01 a D06 (Alternador, Bateria, Partida) | 30 min | Grande diferencial para os mecânicos |
| **10. Checklist Multiponto** | Vistoria de entrada com 6 estados (OK, Atenção, Falha, Não Testado, etc.) | 20 min | Elimina conflitos com clientes |
| **11. Client360** | Histórico consolidado do cliente sem janelas modais travantes | 15 min | Rápido e prático |
| **12. Vehicle360** | Histórico cronológico exclusivo do veículo por GUID | 15 min | Evita contaminação entre placas |
| **13. Pós-Venda** | Acompanhamento de retorno aos 7, 30 e 90 dias após conclusão da OS | 15 min | Excelente para retenção |
| **14. Backup Atômico** | Procedimento de geração de cópia de segurança em pen drive/disco | 15 min | Automatizado na saída do sistema |
| **15. Recuperação de Falhas** | Como agir em caso de queda de energia ou fechamento acidental | 10 min | Reabertura do banco testada |

---

## 2. Dúvidas, Dificuldades e Observações Registradas

1. **Diferenciação de Tensão (12V vs 24V):**
   - *Dúvida dos Técnicos:* Em caminhões com adaptações de som/rastreadores 12V e sistema de partida 24V, como registrar o diagnóstico?
   - *Orientação dada:* O veículo deve ser cadastrado como 24V para a rota principal D01/D02, e observações de conversores registradas no campo de notas técnicas.
2. **Rateio de Parcelas com Centavos Ímpares:**
   - *Dúvida do Caixa:* Se uma conta de R$ 100,01 for dividida em 3x no cartão, o sistema não vai perder 1 centavo?
   - *Esclarecimento:* Foi demonstrada ao vivo a divisão bancária estrita: R$ 33,34 + R$ 33,34 + R$ 33,33 = R$ 100,01 exato. O operador financeiro validou com a calculadora do balcão.
3. **Pós-Venda e WhatsApp:**
   - *Sugestão:* A recepção perguntou se os links de WhatsApp abrem direto no WhatsApp Desktop ou Web.
   - *Classificação:* `FEATURE_REQUEST` / `EXTERNAL_DEPENDENCY` (o sistema gera a URL padrão `wa.me`, abrindo o cliente preferencial do Windows).
