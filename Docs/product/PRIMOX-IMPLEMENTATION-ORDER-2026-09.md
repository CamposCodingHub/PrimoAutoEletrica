# PRIMOX Implementation Order — NET10-28

**Não implementar nesta auditoria.** Ordem por valor × risco × dependência × reuso × mercado × diferenciação.

---

## Ordem recomendada

| # | Iniciativa | Decisão | Depende de | Esforço EST | Valor |
|---|------------|---------|------------|-------------|-------|
| 1 | ContasReceber.ClienteId (+ OS/Venda) + testes | BUILD NOW | — | 3 | 5 |
| 2 | Cliente360Service + UI KPIs | BUILD NEXT | #1 | 3 | 5 |
| 3 | Veiculo360 KPIs (dias, receita) | BUILD NEXT | — | 2 | 4 |
| 4 | Os360 painel ligações REAL/PARTIAL | BUILD NEXT | #2 | 3 | 4 |
| 5 | Reception wizard | BUILD NEXT | #2–4 | 3 | 4 |
| 6 | Wire 2FA login | BUILD NOW/NEXT | Setup existente | 2 | 3 |
| 7 | Pós-venda wa.me templates | BUILD NEXT | OS status | 2 | 4 |
| 8 | Normalizar status orçamento + lost quotes | BUILD NEXT | — | 2 | 3 |
| 9 | Auto Elétrica persistência | BUILD NEXT | — | 4 | 4 |
| 10 | DVI data model + captura | BUILD NEXT | Fotos OS | 5 | 5 |
| 11 | Digital approval token | BUILD NEXT | #10 preferível | 4 | 5 |
| 12 | OS→Fiscal software path | BUILD NEXT | Focus config | 4 | 5 |
| 13 | Relatórios stub cleanup | BUILD NEXT | — | 2 | 2 |
| 14 | BI Owner v1 | BUILD NEXT | #1–2 | 3 | 4 |
| 15 | Compras sugeridas | WAIT | Estoque+OS | 4 | 3 |
| 16 | Comissão | WAIT | #14 | 3 | 2 |
| 17 | Fiscal live Focus | EXTERNAL BLOCKED | Token/CNPJ | 3 | 5 |
| 18 | TEF / PIX dinâmico | EXTERNAL BLOCKED | Adquirente | 5 | 5 |
| 19 | WA Business API | EXTERNAL BLOCKED | Meta | 4 | 4 |
| 20 | Multiestação SQL pilot | RESEARCH | Infra | 5 | 4 |
| 21 | Box/pátio | WAIT | Agenda | 4 | 2 |
| 22 | Maui full ERP | DO NOT BUILD | DVI+sync | 5 | 2 |
| 23 | Cloud SaaS | DO NOT BUILD | — | 5 | 1 |
| 24 | AI diagnóstico | RESEARCH | #9 data | 5 | 3 |
| 25 | Linha pesada pack | WAIT | Frota UX | 4 | 2 |

---

## Quick wins (ESTIMATIVA)

| Item | Classe | Tempo EST |
|------|--------|-----------|
| Exibir TotalGasto / KPIs já calculáveis | wiring UI | &lt;1 dia |
| Dias desde último serviço | cálculo | &lt;1 dia |
| Entrada menu Update/License | menu | &lt;1 dia |
| Ocultar TEF se não integrado | UI label | 1–3 dias |
| Wire 2FA login | wiring | 1–3 dias |
| Status orçamento alias | regra | 1–3 dias |
| ClienteId ContasReceber | migration+UI | 3–7 dias |
| Reception wizard | feature | &gt;7 dias |
| DVI | feature | &gt;7 dias |

---

## Top 25 product bets (resumo)

Ver Master Gap + Differentiation. Destaques: 360 IDs, reception, DVI, approval, pós-venda, auto elétrica learn, fiscal honest, offline, QA, BI, compras, garantia recurrence, frota, box, comissão, multiestação, TEF, WA API, i18n polish, a11y, onboarding zero-data, pacote contador, signing, multi-filial OFF, AI later.

---

## Top 10 executive

1. Corrigir integridade financeira ClienteId (G001).  
2. Entregar Cliente/Veículo/OS 360 (NET10-29).  
3. Reception wizard — menos cliques.  
4. Não vender fiscal live / TEF / WA API até EXTERNAL.  
5. Pós-venda mínimo (wa.me pós-OS).  
6. DVI + approval como aposta BR.  
7. Auto elétrica como diferencial — persistir.  
8. OS→Fiscal fechar ciclo.  
9. Manter offline-first; não pivotar SaaS.  
10. Code signing + Focus homolog em paralelo comercial.
