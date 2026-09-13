# PRIMOX Product Master Gap — NET10-28

**Data:** 13/09/2026 · **Branch:** `audit/product-discovery-2026-09` · **HEAD:** `a016fed`  
**Escopo:** auditoria READ-ONLY. Sem implementação.  
**Baseline CURRENT:** Build PASS · Unit **194/194** · QaEngine **43/43** · DeepQa **6/6**

> Working tree do usuário no início NET10-28: **limpo** vs HEAD. Arquivos de sessão anterior (2FA wire, PIX estático, tempos padrão, etc.) **não estão no HEAD nem no disco** neste momento — classificados como HISTORICAL/UNKNOWN (não descartados por esta auditoria).

---

## 1. Executive verdict

PRIMOX Workshop (CURRENT HEAD) é um **desktop operacional real** (oficina + PDV + estoque + Kanban + orçamento/OS) com **fiscal software-path parcial** e **gaps estruturais** que impedem Cliente/Veículo/OS 360 de nível mercado: sobretudo **ContasReceber sem ClienteId** (TEXT_MATCH por nome), **OS→Fiscal ausente**, **DVI/aprovação digital ausentes**, **pós-venda automático ausente**.

---

## 2. Master Gap Matrix

| ID | Área | Feature | Estado | Evidência | Impacto | Esforço | Dep. | Prioridade | Recomendação |
|----|------|---------|--------|-----------|---------|---------|------|------------|--------------|
| G001 | Data | ContasReceber↔Cliente por nome | WEAK/TEXT_MATCH | HistoricoClienteWindow match nome; schema `Cliente TEXT` | 5 | 3 | 2 | P0 | Add ClienteId FK |
| G002 | Data | Contas* sem FK cliente | NOT_IMPLEMENTED | FinanceiroDatabaseService schema | 5 | 3 | 1 | P0 | Migration + backfill |
| G003 | C360 | Receita 12m | POSSÍVEL/NOT UI | OS+Venda por ClienteId | 4 | 2 | G001 | P1 | Calcular em 360 |
| G004 | C360 | Ticket médio | POSSÍVEL | Totais OS/Venda | 3 | 2 | — | P1 | KPI |
| G005 | C360 | Orçamentos perdidos | PARTIAL | Contagem só dashboard orçamentos | 4 | 2 | — | P1 | Superfície 360 |
| G006 | C360 | Dívida por ClienteId | IMPOSSÍVEL modelo atual | Sem FK | 5 | 3 | G001 | P0 | Depende G001 |
| G007 | C360 | TotalGasto UI | BACKEND_ONLY | Campos no model; Historico não exibe | 2 | 1 | — | P2 | Exibir |
| G008 | V360 | Dias desde último serviço | PARTIAL | Data OS existe; KPI não | 3 | 1 | — | P1 | KPI trivial |
| G009 | V360 | Orçamentos fracos | WEAK | Via ClienteId/OS não VeiculoId | 3 | 2 | — | P1 | Persistir VeiculoId |
| G010 | OS | OS→Fiscal emissão | AUSENTE | Mapper Venda→NFe only | 5 | 4 | Fiscal live | P0/P1 | Spec NET10-29+ |
| G011 | Orç | Status alias drift | PARTIAL | Rejeitado vs Recusado | 2 | 1 | — | P2 | Normalizar |
| G012 | Orç | Lost quotes analytics | PARTIAL | Count only | 3 | 2 | — | P2 | Motivo/perda |
| G013 | Pós | Auto pós-OS | AUSENTE | NotificationService no-op | 5 | 3 | Comm | P1 | wa.me jobs |
| G014 | AutoE | Roteiros hardcoded | PARTIAL | AutoEletricaTecnicaService | 4 | 4 | — | P1 | Persistência |
| G015 | Orphan | LicenseActivationWindow | BACKEND_ONLY | Sem callers | 2 | 1 | — | P2 | Menu ou esconder |
| G016 | Orphan | AtualizacaoWindow | BACKEND_ONLY | Sem callers | 3 | 1 | CDN | P2 | Expor |
| G017 | Relat | OS report TODO | BROKEN/STUB | RelatoriosModernoViewModel | 3 | 2 | — | P1 | Terminar/ocultar |
| G018 | Comm | Notification Cloud | SCAFFOLD | Always fail | 4 | 4 | Meta | P1 | BLOCKED_EXT |
| G019 | Multi | MultiFilialDisponivel=false | FUTURE | FilialService | 4 | 5 | — | P2 | Não vender |
| G020 | Fiscal | ScaffoldNfse | SCAFFOLD | NotImplemented | 5 | 5 | Pref | P0 | Após NF-e |
| G021 | Compras | Pedido compra | AUSENTE | — | 4 | 4 | — | P2 | Spec |
| G022 | RH | Comissão settlement | PARTIAL | Campo 5% orçamento | 3 | 3 | — | P3 | — |
| G023 | Frota | Fleet ops | UI_ONLY label | Contagem veículos | 3 | 4 | — | P3 | — |
| G024 | Pátio | Box/baia | AUSENTE | — | 3 | 4 | Agenda | P3 | — |
| G025 | Fiscal | Live Focus | BLOCKED_EXTERNAL | CURRENT-TRUTH | 5 | 3 | Token | P0 | Homolog |
| G026 | Fiscal | DANFE oficial | PARTIAL informativo | IDanfeGenerator | 4 | 3 | Focus | P1 | — |
| G027 | Pay | TEF/PIX dinâmico | NOT_IMPLEMENTED | Labels PDV | 5 | 5 | Adq. | P1 | EXTERNAL |
| G028 | Comm | WA Business API | BLOCKED_EXTERNAL | ManualWhatsAppProvider | 4 | 4 | Meta | P1 | — |
| G029 | Dist | Code signing | BLOCKED_EXTERNAL | Docs commercial | 4 | 2 | Cert | P0 | — |
| G030 | DVI | DVI estruturado | NOT_IMPLEMENTED | Só fotos OS | 5 | 5 | — | P1 | NET10-29+ |
| G031 | Auth | Aprovação remota | NOT_IMPLEMENTED | Aprovar local | 5 | 4 | — | P1 | Link |
| G032 | UX | Reception wizard | AUSENTE | Cadastros separados | 4 | 3 | — | P1 | Spec |
| G033 | BI | Margem/conversão | PARTIAL | Relatórios | 4 | 3 | G001 | P1 | — |
| G034 | Sec | 2FA no login | PARTIAL | Setup existe; login gate NÃO no HEAD | 3 | 2 | — | P1 | Wire |
| G035 | Mobile | Maui | SCAFFOLD | Stub | 3 | 5 | DVI | P3 | DO NOT full |
| G036 | Cloud | SaaS sync | FUTURE | API piloto | 2 | 5 | — | P3 | DO NOT NOW |

Impacto/Esforço/Dep.: **ESTIMATIVA** 1–5.

---

## 3. Classification summary (HEAD)

| Classe | Exemplos |
|--------|----------|
| IMPLEMENTED+TESTED | Clientes, OS, Orçamentos, PDV core, Estoque, Unit/Qa |
| PARTIAL | Fiscal Ops, Auto Elétrica, Financeiro, Histórico 360, wa.me |
| SCAFFOLD | NFS-e, Notification Cloud, Maui |
| BACKEND_ONLY | License/Update windows |
| FAKE_ONLY | FakeFiscal (testes) |
| NOT_IMPLEMENTED | DVI, auth digital, TEF, compras, box |
| BLOCKED_EXTERNAL | Focus live, signing, WA API |
| FUTURE | Multi-filial, SaaS |

---

## 4. Decision matrix (grandes)

| Iniciativa | Decisão |
|-----------|---------|
| ClienteId em ContasReceber | **BUILD NOW** |
| Cliente/Veículo/OS 360 KPIs | **BUILD NEXT** (NET10-29) |
| Reception wizard | **BUILD NEXT** |
| DVI + approval | **BUILD NEXT** após 360 data |
| Fiscal live | **EXTERNAL BLOCKED** |
| TEF/PIX dinâmico | **EXTERNAL BLOCKED** |
| WA API | **EXTERNAL BLOCKED** |
| Multiestação SQL | **RESEARCH** / WAIT |
| Maui full | **DO NOT BUILD** agora |
| Cloud SaaS | **DO NOT BUILD** agora |
| Auto Elétrica persistida | **BUILD NEXT** |
| Compras/Box/Frota | **WAIT** / P3 |

---

## 5. Documents related

- `PRIMOX-360-TECHNICAL-SPEC-2026-09.md` — modelo dados NET10-29  
- `PRIMOX-WORKFLOW-MASTER-2026-09.md`  
- `PRIMOX-MARKET-BENCHMARK-2026-09.md`  
- `PRIMOX-DIFFERENTIATION-STRATEGY-2026-09.md`  
- `PRIMOX-PRODUCT-ROADMAP-2026-09.md`  
- `PRIMOX-IMPLEMENTATION-ORDER-2026-09.md`  
- `PRIMOX-PRODUCT-DISCOVERY-EVIDENCE-NET10-28-2026-09.md`
