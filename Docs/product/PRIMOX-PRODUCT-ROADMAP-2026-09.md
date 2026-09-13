# PRIMOX Product Roadmap — NET10-28

**Não é compromisso comercial hard.** Prioridades P0–P3; separar bloqueador técnico de “importante”.

---

## North Star

PRIMOX ajuda a autoelétrica e oficina a fechar o ciclo cliente→OS→caixa sem perder o histórico no WhatsApp.

---

## 0–30 dias

| ID | Item | P | Tipo |
|----|------|---|------|
| G001–G002 | ClienteId (+ OS/Venda Id) em ContasReceber + backfill | P0 | Bloqueador 360 |
| G034 | 2FA no login (wire existente) | P1 | Segurança |
| G007/G008 | KPIs 360 triviais (exibir TotalGasto, dias serviço) | P1 | Product |
| G011 | Normalizar status orçamento | P2 | Debt |
| G015–G016 | Menu License/Update ou esconder | P2 | UX |
| Docs/commercial truth | Alinhar vendas ao CURRENT | P0 | Go-to-market |

**External (paralelo, não código produto):** Focus homolog, code signing.

---

## 30–60 dias

| ID | Item | P |
|----|------|---|
| NET10-29 | Cliente360 / Veiculo360 / Os360 services + UI | P0 |
| G032 | Reception wizard (Cliente+Veículo+Sintoma) | P1 |
| G013 | Pós-OS wa.me templates (manual assistido→semi-auto) | P1 |
| G017 | Relatórios OS stub | P1 |
| G033 | BI Owner v1 (receita, ticket, inativos) | P1 |

---

## 60–90 dias

| ID | Item | P |
|----|------|---|
| G010 | OS→Fiscal path (software) | P1 |
| G014 | Auto Elétrica persistência roteiros/resultados | P1 |
| G030 | DVI modelo dados + captura foto por item | P1 |
| G031 | Aprovação digital (link local/token) | P1 |
| G026 | DANFE informativo→oficial quando Focus live | P1 |

---

## 3–6 meses

| Item | P |
|------|---|
| Compras sugeridas por OS | P2 |
| Comissão settlement | P2 |
| Multiestação RESEARCH→pilot | P2 |
| PIX dinâmico / TEF se parceiro | P1 EXTERNAL |
| WA Business se Meta | P1 EXTERNAL |
| Garantia recorrência “mesmo problema” | P2 |

---

## 6–12 meses

| Item | P |
|------|---|
| Box/pátio capacidade | P3 |
| Frota PJ ops | P3 |
| Mobile técnico (após DVI) | P3 |
| Cloud sync opcional | P3 / DO NOT default |
| Linha pesada pack | P3 |
| AI assist (após dados SINTOMA→CAUSA) | RESEARCH |

---

## Dependency graph (texto)

```
Fiscal live (EXTERNAL)
  ↓
OS→Fiscal / DANFE oficial

ClienteId ContasReceber
  ↓
Cliente 360 / BI / CRM debt
  ↓
Workflow reception + pós-venda

DVI
  ↓
Digital Approval
  ↓
Mobile técnico

Auto Elétrica persistida
  ↓
Aprendizado sintoma→causa
  ↓
AI (RESEARCH)

Multiestação SQL
  ↓
Multi-loja (FUTURE)
```

---

## Personas (resumo)

1. **Autoeletricista pequeno** — diagnóstico rápido, histórico bateria/alternador.  
2. **Oficina mecânica** — OS+peças+caixa.  
3. **Linha pesada** — frota, tempo parado.  
4. **Oficina média** — multi-usuário, BI.  
5. **Dono/gestor** — margem, inadimplência.  
6. **Recepcionista** — cadastro rápido, agenda.  
7. **Técnico** — Kanban, checklist, fotos.

## JTBD (resumo)

Recepção: “Quando o cliente chega, quero abrir OS sem redigitar.”  
Diagnóstico: “Quero saber o que já falhou neste carro.”  
Orçamento: “Quero aprovar rápido.”  
Execução: “Quero status sem papel.”  
Estoque: “Quero não faltar peça.”  
Pagamento/Fiscal: “Quero fechar e emitir sem retrabalho.”  
Pós-venda: “Quero lembrar garantia sem planilha.”  
Gestão: “Quero saber quem gera lucro.”
