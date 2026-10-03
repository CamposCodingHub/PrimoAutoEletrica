# PRIMOX Workshop 2.0 — Manual do Usuário
## 01. Visão Geral da Expansão Estratégica

---

### Introdução ao PRIMOX 2.0

O **PRIMOX Workshop 2.0** foi concebido para elevar oficinas mecânicas e auto elétricas ao mais alto nível de eficiência operacional, rastreabilidade técnica e rentabilidade. 

Esta expansão introduziu **três pilares estratégicos de engenharia** que atacam diretamente os maiores gargalos do dia a dia das oficinas:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           PRIMOX WORKSHOP 2.0                           │
├─────────────────────┬─────────────────────┬─────────────────────────────┤
│      PILAR 1        │       PILAR 2       │           PILAR 3           │
│   COMPRAS E GESTÃO  │ CONTROLE FERRAMENTAL│      COPILOT DE IA          │
│    ANTI-RUPTURA     │    ANTI-EXTRAVIO    │        DUAL-ENGINE          │
│                     │                     │                             │
│ • Cálculo ROP       │ • Armários/Gavetas  │ • Google Gemini Cloud       │
│ • Histórico 60 dias │ • Bipagem QR/Barra  │ • Motor Local Offline       │
│ • Cotação WhatsApp  │ • Trava de Baixa OS │ • RAG Elétrico Automotivo   │
│ • Pedido PDF Oficial│ • Alertas de Atraso │ • Function Calling no ERP   │
│ • Entrada XML NF-e  │ • Calibração & Log  │ • Flyout Retrátil (Ctrl+I)  │
└─────────────────────┴─────────────────────┴─────────────────────────────┘
```

---

### Perfis de Acesso e Responsabilidades

| Perfil | Módulos Principais | Responsabilidade Primária |
| :--- | :--- | :--- |
| **Gerente / Administrador** | Todos os módulos, Configurações de IA, Auditoria, Compras | Gestão estratégica, precificação, aprovação de pedidos e auditoria. |
| **Comprador / Estoquista** | Estoque, Compras & Falta, Entrada de NF-e XML | Evitar ruptura de estoque, cotar via WhatsApp e emitir pedidos. |
| **Eletricista / Mecânico Chefe** | Ordens de Serviço, Ferramentas, Copilot IA, Módulo Técnico | Diagnósticos avançados, retirada e devolução rápida de ferramental. |
| **Técnico Operacional** | Ordens de Serviço, Bipagem de Ferramental | Execução de serviços com garantia de checklist anti-extravio. |

---

### Navegação Rápida no Sistema

- **Barra Lateral do ERP:**
  - `Ferramentas`: Gestão visual de armários, bancadas e empréstimos ativos.
  - `Compras / Falta`: Matriz de ressuprimento dinâmico e pedidos em aberto.
  - `Importar NF-e`: Associação inteligente de XML aos pedidos de compra.
  - `Configurações`: Aba "Inteligência Artificial (Copilot)" para parametrização do Gemini.

- **Atalhos Globais:**
  - `Ctrl + I`: Abre/fecha o **PRIMOX Copilot** de qualquer tela do sistema.
  - `Esc`: Fecha modais e o flyout do Copilot.
  - `F5`: Atualiza a tela ou tabela atual.

---

### Política de Tolerância a Falhas e Trabalho Offline

O PRIMOX foi arquitetado sob o princípio de **Resiliência Máxima**:
1. Se a conexão de internet cair, o sistema continuará operando 100% das suas funções locais.
2. O **Copilot de IA** possui um motor determinístico local que responde a esquemas elétricos, cálculos de queda de tensão, pinagens de relés e tolerâncias mesmo em galpões sem sinal de internet.
3. As movimentações de ferramental e pedidos de compra são persistidos atomicamente no banco de dados local com isolamento transacional.
