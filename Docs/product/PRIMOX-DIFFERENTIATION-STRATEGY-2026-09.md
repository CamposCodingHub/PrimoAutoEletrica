# PRIMOX Differentiation Strategy — NET10-28

**HEAD CURRENT** · 13/09/2026 · Honestidade comercial obrigatória.

---

## 1. Product North Star — opções

1. **PRIMOX ajuda a autoelétrica e oficina a fechar o ciclo cliente→OS→caixa sem perder o histórico no WhatsApp.**  
2. PRIMOX ajuda o dono a enxergar lucro por cliente e veículo sem depender de planilha.  
3. PRIMOX ajuda o técnico elétrico a diagnosticar com prontuário, sem reinventar o roteiro a cada carro.

**Escolhida:** (1) — cobre recepção, execução e retenção; diferencia vs Excel/WhatsApp e vs ERP genérico sem oficina.

---

## 2. Classificação de recursos

| Recurso | Classe |
|---------|--------|
| OS / Orçamento / Estoque / PDV | COMMODITY / PARIDADE |
| Kanban oficina | PARIDADE |
| Fiscal Focus path | PARIDADE se live; hoje PARTIAL |
| Offline desktop | DIFERENCIAL (vs SaaS) se confiável |
| Auto elétrica / prontuário | DIFERENCIAL / OPORTUNIDADE |
| Linha pesada | OPORTUNIDADE (não forte hoje) |
| DVI + approval | PARIDADE mercado US; OPORTUNIDADE BR |
| Cliente/Veículo/OS 360 | PARIDADE mínima; hoje PARTIAL |
| QA / smoke / evidence culture | DIFERENCIAL FORTE interno→confiança |
| Fiscal honesto (não vender live sem token) | DIFERENCIAL ético |
| i18n PT/EN/ES | PARIDADE / leve diferencial |
| Multiestação SQL | FUTURE |
| Mobile full | COMMODITY mercado; hoje SCAFFOLD |

---

## 3. “Não trabalho sem isso”

### Autoelétrica
| Tipo | Item |
|------|------|
| Commodity | OS, orçamento, estoque básico |
| Impacto operacional | Histórico elétrico do veículo, fotos, tempos |
| Diferencial técnico | Sintoma→teste→causa aprendido; medições |

### Oficina mecânica
| Tipo | Item |
|------|------|
| Commodity | Fluxo OS+peças+caixa |
| Impacto | Orçamento→aprovação rápido; estoque |
| Diferencial | DVI + link aprovação |

### Linha pesada
| Tipo | Item |
|------|------|
| Commodity | OS multi-dia, frota |
| Impacto | Capacidade/box, custo real |
| Diferencial | Frota PJ + telemetria (FUTURE) |

---

## 4. Por que PRIMOX?

| Alternativa | Por que escolher PRIMOX | Fraqueza da resposta |
|-------------|-------------------------|----------------------|
| Excel/WhatsApp | Histórico estruturado, estoque, OS | Ainda usa wa.me manual |
| Software barato | Desktop local + módulos oficina | Fiscal/mobile podem ser mais maduros no rival |
| SaaS | Offline, dado local, custo previsível | Sem DVI/cloud sync |
| ERP tradicional | Menos peso; foco oficina/autoelétrica | Menos “tudo fiscal/RH” |

Se a resposta parece fraca: **registrar** — hoje falta DVI, approval, pós-venda e fiscal live para “matar” SaaS US.

---

## 5. Por que NÃO PRIMOX? (obrigatório)

1. **Fiscal live** ainda depende Focus/certificado — risco comercial.  
2. **Sem TEF/PIX dinâmico** — PDV moderno incompleto.  
3. **Sem mobile/DVI** — técnicos em pátio.  
4. **Sem WA Business API** — comunicação manual.  
5. **Multiestação** não vendável (`MultiFilialDisponivel=false`).  
6. **Implantação** + onboarding zero-data ainda fricção.  
7. **Assinatura código** BLOCKED_EXTERNAL para trust Windows.  
8. **360 incompleto** (dívida por nome) — gestor desconfia do BI.

---

## 6. Competitive positioning

| Dimensão | PRIMOX CURRENT | ERP BR genérico | SaaS oficina BR | Shopmonkey/Tekmetric |
|----------|----------------|-----------------|-----------------|----------------------|
| Offline | FORTE (desktop) | Variável | FRACO | FRACO |
| Auto elétrica | PARCIAL único | FRACO | FRACO | FRACO |
| DVI | AUSENTE | RARO | CLAIM | FORTE (CLAIM) |
| Fiscal BR | PARTIAL | FORTE | CLAIM | N/A |
| Mobile | SCAFFOLD | CLAIM | CLAIM | FORTE |
| Preço/modelo | Desktop licença | ERP | Assinatura | Assinatura USD |
| Honestidade gaps | Alta (docs) | Baixa mkt | Mkt | Mkt |

---

## 7. Commercial truth

**Pode vender hoje:** gestão local de clientes, veículos, OS, orçamentos, estoque, PDV, Kanban, financeiro operacional, histórico parcial, Auto Elétrica assistida, fiscal software-path (homolog/fake conforme config), i18n parcial, wa.me manual.

**Pode prometer:** roadmap 360/DVI com IDs; QA contínuo.

**NÃO prometer:** Focus produção sem credencial; DANFE oficial SEFAZ; TEF; PIX dinâmico; WA API; multi-loja; SaaS; “IA diagnóstica”; margem/funcionário 100% confiável; DVI; aprovação remota.

**Depende de:** CNPJ, certificado A1, token Focus, Meta, adquirente, CDN update, cert assinatura, SQL Server (multiestação), hardware.

---

## 8. DO NOT BUILD (agora)

| Item | Justificativa |
|------|---------------|
| SaaS multi-tenant | Desvia do offline-first; esforço 5 |
| Mobile ERP completo | Dependências DVI/sync; Maui stub |
| SPED completo | Fiscal live primeiro |
| IA generativa diagnóstico | Risco + sem dados SINTOMA→CAUSA |
| Box/pátio sofisticado | Sem capacidade model |
| Frota 100 veículos UX | Modelo OK contagem; ops não |
