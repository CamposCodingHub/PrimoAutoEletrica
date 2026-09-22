# PRIMOX Workshop — LACUNAS DE MERCADO E PRODUTO

> Funcionalidades necessárias para competir como produto comercial de alto valor no mercado brasileiro de oficinas automotivas.

---

## 1. MERCADO-ALVO

| Segmento | Tamanho Estimado (BR) | PRIMOX Atual |
|---|---|---|
| Oficinas pequenas (1-3 técnicos) | ~200.000 | PARCIAL |
| Oficinas médias (4-10 técnicos) | ~40.000 | PARCIAL |
| Oficinas grandes (11+ técnicos) | ~8.000 | NÃO ATENDE |
| Linha pesada (caminhões/ônibus) | ~15.000 | NÃO ATENDE |
| Frotistas | ~5.000 | NÃO ATENDE |
| Concessionárias | ~6.000 | NÃO ATENDE |

## 2. CONCORRENTES DIRETOS

| Concorrente | Pontos Fortes | PRIMOX Diferencial |
|---|---|---|
| Ultracar | Market share, integração fiscal | Diagnóstico técnico estruturado |
| Car Clin | Simplicidade, preço baixo | Prontuário elétrico, DVI |
| GestorCar | Funcionalidades completas | Linha pesada, inteligência |
| Oficina Integrada | Cloud-first | Desktop offline-first |

## 3. FUNCIONALIDADES AUSENTES PARA COMPETIR

### 3.1 Portal do Cliente ⭐⭐⭐⭐⭐
- Cliente visualiza seus veículos, OS ativas, diagnósticos, fotos, orçamentos
- Aprovação online de orçamento
- Acompanhamento do status da OS em tempo real
- Histórico de manutenções
- Próxima manutenção recomendada
- **Impacto**: ALTO — diferenciador competitivo direto

### 3.2 Inspeção Digital (DVI) Completa ⭐⭐⭐⭐⭐
- Check-in com fotos antes/depois
- Checklist por tipo de veículo/serviço
- Classificação de defeitos por gravidade (vermelho/amarelo/verde)
- Geração automática de orçamento a partir do DVI
- Envio para cliente via WhatsApp/email com fotos
- Assinatura digital de aprovação
- **Impacto**: ALTO — funcionalidade mais desejada por oficinas modernas
- **Status PRIMOX**: FUNDAÇÃO EXISTENTE (DviChecklistService, DviOrcamentoOsInheritance)

### 3.3 WhatsApp Cloud API ⭐⭐⭐⭐
- Templates aprovados pela Meta
- Webhook para respostas
- Fila de envio com retry
- Opt-in/opt-out (LGPD)
- Rastreamento de entrega/leitura
- **Impacto**: ALTO — canal principal de comunicação com clientes
- **Status PRIMOX**: SCAFFOLD (WhatsAppService existe)

### 3.4 PIX Dinâmico ⭐⭐⭐⭐
- QR Code dinâmico por OS/orçamento
- Link de pagamento
- Webhook de confirmação
- Conciliação automática
- Baixa automática no financeiro
- **Impacto**: ALTO — reduz inadimplência e acelera recebimento

### 3.5 Gestão de Frotas ⭐⭐⭐⭐
- Empresa → Frota → Veículo → Motorista
- Custo por km, por veículo, por frota
- Manutenção preventiva por km/horas
- Alertas de manutenção vencida
- Relatório para frotista
- **Impacto**: ALTO — segmento com ticket médio elevado

### 3.6 BI Operacional / DRE ⭐⭐⭐
- Demonstrativo de Resultados do Exercício
- Margem por serviço, por técnico, por tipo de OS
- Custo de mão de obra
- Giro de estoque (curva ABC)
- Fluxo de caixa projetado
- **Impacto**: MÉDIO — decisivo para oficinas médias/grandes

### 3.7 Interface Mobile para Técnico ⭐⭐⭐
- Consulta de OS atribuída
- Registro de horas
- Fotos de diagnóstico direto pelo celular
- Check-in/check-out de serviços
- **Impacto**: MÉDIO — produtividade do técnico

### 3.8 Motor de Garantia/Retorno ⭐⭐⭐
- Período de garantia por tipo de serviço/peça
- Alerta automático de retorno (cliente voltou com mesmo problema)
- Rastreamento de retorno vs. novo serviço
- Custo de garantia
- **Impacto**: MÉDIO — qualidade e reputação

### 3.9 Agendamento Online ⭐⭐
- Link público para cliente agendar
- Disponibilidade por box/técnico
- Confirmação automática
- Lembrete automático (WhatsApp/SMS)
- **Impacto**: MÉDIO — conveniência

### 3.10 Integração Fiscal Completa ⭐⭐⭐⭐
- NF-e, NFC-e, NFS-e
- Certificado A1
- Cancelamento, inutilização
- DANFE
- Contingência offline
- Eventos fiscais
- **Impacto**: ALTO — requisito obrigatório
- **Status PRIMOX**: FUNDAÇÃO EXISTENTE (IFiscalProvider, FocusNfe, validadores, testes)

## 4. PRIORIZAÇÃO RECOMENDADA

1. **Fiscal completo** — obrigatório para venda
2. **DVI completo** — diferenciador #1
3. **WhatsApp Cloud API** — comunicação
4. **Portal do cliente** — diferenciador #2
5. **PIX dinâmico** — receita
6. **Gestão de frotas** — ticket alto
7. **BI/DRE** — decisão
8. **Mobile técnico** — produtividade
9. **Garantia/retorno** — qualidade
10. **Agendamento online** — conveniência
