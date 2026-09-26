# PRIMOX WORKSHOP — CICLO C1
# MAPA DE INTEGRAÇÃO OPERACIONAL E CONEXÃO ENTRE MÓDULOS
**Data:** 2026-09-25  
**Ciclo:** C1 — Operational Intelligence + Tools + Purchasing + Knowledge + Assist Foundation  
**Status:** ARCHITECTURAL INTEGRATION SPEC  

---

## 1. O VERDADEIRO DIFERENCIAL DO PRIMOX

A inovação do PRIMOX não reside em adicionar botões ou formulários isolados. A inovação consiste em **compreender e orquestrar as relações orgânicas** entre todos os elementos operacionais da oficina:

```
                         PRIMOX WORKSHOP
                                │
             ┌──────────────────┼──────────────────┐
             │                  │                  │
         OPERAÇÃO            RECURSOS         CONHECIMENTO
             │                  │                  │
         Cliente             Estoque          Base Técnica
         Veículo             Compras          Diagnóstico
         Orçamento           Ferramentas      Casos Reais
         OS
             │                  │                  │
             └──────────────────┼──────────────────┘
                                │
                            HISTÓRICO
                                │
                                ▼
                         PRIMOX ASSIST
                                │
                                ▼
                       INTELIGÊNCIA REAL
                                │
                                ▼
                         DECISÃO HUMANA
```

---

## 2. FLUXO OPERACIONAL COMPLETO: DIAGNÓSTICO PESADO (MERCEDES-BENZ ACTROS)

### Cenário Operacional de Referência:
1. **Entrada e Identificação:**
   - O veículo Mercedes-Benz Actros 2651 entra na oficina com falha de alimentação elétrica intermitente.
   - O técnico abre o **Vehicle 360**, identifica sistema elétrico **24V** e histórico de ordens anteriores.
2. **Abertura da OS:**
   - A Ordem de Serviço é criada com sintoma reportado: *"Perda de carga intermitente em viagem noturna"*.
3. **Controle de Ferramentas (PRIMOX Tools):**
   - Técnico requisita o *Osciloscópio Automotivo* e *Alicate Amperimétrico DC 600A*.
   - Realiza o Checkout na tela de Ferramentas, vinculando a OS e Placa.
   - Status da ferramenta atualiza imediatamente para `IN_USE` com responsabilidade do técnico.
4. **Execução Técnica (Auto Elétrica Técnica D01-D06):**
   - Técnico executa roteiro D01 (Balanço Energético) e D02 (Queda de Tensão).
   - Medições registradas: Tensão B+ no alternador = 28,4V; Tensão no borne de bateria = 26,1V (Queda excessiva de 2,3V em 24V).
5. **Diagnóstico & Demanda de Peça:**
   - Diagnosticado: Cabo principal rompido parcialmente e terminal oxidado.
   - Necessidade: 1 Cabo 50mm² 24V especial e 2 Terminais Olha Reforçados.
6. **Integração Estoque -> Compras (PRIMOX Purchasing):**
   - Estoque do terminal = 0 (Abaixo do estoque mínimo = 5).
   - O PRIMOX detecta o rompimento do estoque mínimo e **sugere a solicitação de compra**.
   - O gestor revisa na tela de Necessidades de Compra com prioridade `URGENT` e aprova a compra.
7. **Recebimento & Integração Financeira:**
   - O fornecedor local entrega a peça com nota fiscal.
   - O almoxarife dá o recebimento físico:
     - `Produtos.EstoqueAtual` é incrementado atomicamente.
     - `FinanceiroDatabaseService` cria obrigação a pagar (`ContaPagar`) em **CentsV1**, vinculando o fornecedor sem duplicar dados.
8. **Aplicação e Fechamento da OS:**
   - As peças são baixadas na Ordem de Serviço.
   - Teste final: Queda de tensão cai para 0,18V (Padrão de fábrica atendido).
   - Ferramenta é devolvida com condição `OK` (retorna para `AVAILABLE`).
9. **Geração de Conhecimento (PRIMOX Knowledge):**
   - Ao concluir a OS, o técnico seleciona *"Salvar como caso técnico"*.
   - Caso `CASO-20260925-001` é registrado na base técnica com fotos, medições e causa confirmada.
   - O caso passa a enriquecer o histórico do **Vehicle 360** e a base RAG do **PRIMOX Assist**.
10. **Futuro Acionamento via PRIMOX Assist:**
   - Quando outro caminhão Mercedes Actros der entrada com queixa similar, o PRIMOX Assist indicará as evidências históricas deste caso comprovado.

---

## 3. FLUXO OPERACIONAL DE FERRAMENTAS E PREVENÇÃO DE AVARIAS

```
FERRAMENTAS
    ↓
Tool 360
    ↓
Retirar Ferramenta (Checkout)
    ↓
Responsável = Técnico / OS = 1234
    ↓
Status = IN_USE
    ↓
[Uso na Oficina]
    ↓
Devolver Ferramenta (Checkin)
    ├── Se Condição = OK ──→ Status = AVAILABLE (Liberada)
    └── Se Condição = DAMAGED ──→ Status = DAMAGED (Bloqueada)
                                      ↓
                                 Manutenção / Calibração
                                      ↓
                                 Registro de Custo (CentsV1)
                                      ↓
                                 Retorno Aferido (AVAILABLE)
```
