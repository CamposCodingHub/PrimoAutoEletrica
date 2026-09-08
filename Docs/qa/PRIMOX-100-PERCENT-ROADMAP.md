# PRIMOX Workshop — 100% Roadmap Audit (FASE A)

**Programa:** Evolução estratégica Acessibilidade → Integrações → Fiscal → Multi-filial → Sync → SaaS  
**Tipo:** Auditoria + arquitetura (**sem implementação** das grandes features)  
**Data:** 2026-09-08  
**HEAD:** `12ded6e`  
**Tag protegida:** `v1.0.0` → `a4ad6fe` (**intacta** — não alterar)  
**Base de verdade:** Product Truth Audit 1.0  

**Decisão FASE A:** **REQUIRES PRODUCT DECISION**  
(ETAPA 1 — Acessibilidade — está **READY FOR IMPLEMENTATION** após aprovação explícita; pilares fiscais/cloud exigem escolhas de produto.)

---

## 1. Estado atual (resumo)

| Dimensão | Estado |
|----------|--------|
| Desktop oficina core | REAL + TESTADO |
| UI buttons executáveis | 1909/1909 PASS (Exhaustive 3.0) |
| P15E-015 a11y | PARTIAL |
| Integrações HTTP (Twilio/WhatsApp Cloud) | PLACEHOLDER |
| WhatsApp `wa.me` | REAL |
| NF-e import | REAL + TESTADO |
| NF-e emissão | NÃO IMPLEMENTADO (`NFeEmissaoService.cs` = 0 bytes) |
| Multi-filial | SCAFFOLD (`FilialService` hardcoded) |
| Sync remoto | NÃO IMPLEMENTADO |
| API | REAL + PARCIAL (minimal, sem JWT wired) |
| RBAC WPF | REAL; RBAC API | SCAFFOLD / NOT_USED |
| SaaS / multi-tenant / billing | FORA DO ESCOPO / NÃO IMPLEMENTADO |

**Não** existe percentual “produto = X%” oficial. Contagens objetivas: Truth Matrix (~22 REAL · ~12 PARCIAL · ~5 SCAFFOLD/PLACEHOLDER · ~3 NÃO IMPLEMENTADO).

---

## 2. Matriz central

| Área | Estado atual | Evidência | Gap | Complexidade | Dependências | Meta (critério §43) |
| ---- | ------------ | --------- | --- | ------------ | ------------ | ---- |
| Acessibilidade | PARTIAL (P15E-015) | Exhaustive ACCESSIBILITY ~30 findings; ModalClose parcialmente corrigido | AutomationName/ToolTip em icon-only + chrome AutoEletrica/OS | BAIXA–MÉDIA | Exhaustive/CompleteUi | P15E-015 **VERIFIED** |
| Integrações | Misto | `wa.me` REAL; `NotificationService` Delay+TODO | Classificar e implementar só prioritárias com config segura | MÉDIA | Secrets, provedor, LGPD consent | Catálogo REAL + 0 PLACEHOLDER “vendável” |
| NF-e | Import REAL; emissão NÃO IMPLEMENTADO | `NFeService`; `NFeEmissaoService` vazio; ESTUDO_FISCAL checklist aberto | Arquitetura + homologação SEFAZ/provedor | ALTA | Certificado, regime, CFOP/NCM, decisão A/B | Homologação REAL+TESTADO antes de produção |
| Multi-filial | SCAFFOLD | `FilialService` simulação SP/RJ; `SelecaoFilialWindow` | Persistência Empresa/Filial + isolamento | ALTA | Modelo de ownership, RBAC | Isolamento testado |
| Sync | NÃO IMPLEMENTADO | Sem `ProcessarFilaOfflineAsync` real | Outbox + API + confirmação + conflitos | ALTA | API auth, FilialId, IDs globais | E2E offline→online |
| API | REAL+PARCIAL | Minimal Program.cs | JWT, policies aplicadas, HTTP tests reais | MÉDIA–ALTA | Auth design | 401/403/200 reais |
| RBAC | WPF REAL; API SCAFFOLD | PermissionService vs policies ausentes | Aplicar policies em endpoints | MÉDIA | API | DENIED real |
| Segurança | REAL+PARCIAL | PBKDF2+lockout; 2FA não no login | Wire 2FA opcional; secrets; API harden | MÉDIA | Product policy 2FA | Sem overclaim 100% |
| Multi-tenant | NÃO IMPLEMENTADO | — | TenantId isolation | ALTA | Cloud arch, API | Testes cross-tenant DENIED |
| SaaS | FORA DO ESCOPO atual | Truth Audit | Hosting + onboarding + licença | MUITO ALTA | Multi-tenant, billing, ops | Não improvisar |
| Billing | NÃO IMPLEMENTADO | — | Planos/webhooks sandbox | ALTA | Gateway sandbox | Idempotência |

---

## 3. Acessibilidade

| Item | Status |
|------|--------|
| FocusVisualStyle global | REAL+TESTADO (P15E-001) |
| ModalClose Name/ToolTip | REAL (parcial cobertura) |
| Icon-only / UNIDENTIFIED | PARTIAL — AutoEletrica chrome + OS outside-window |
| Meta ETAPA 1 | Inventário completo → Name/ToolTip → CompleteUi + Exhaustive Light/Dark × 4 resoluções → P15E-015 VERIFIED |

**Pronto para implementar após GO explícito:** SIM (ETAPA 1).

---

## 4. Integrações (inventário)

| Integração | Classificação | Nota |
|------------|---------------|------|
| WhatsApp `wa.me` | REAL | Deep link; não é Cloud API |
| WhatsApp Cloud API | NÃO IMPLEMENTADA | — |
| Twilio / SMS HTTP | PLACEHOLDER | Retorna sucesso após `Task.Delay` |
| E-mail SMTP | PARCIAL / NÃO AUDITADO A FUNDO | Não tratar como Cloud mail |
| PDF/Excel export | REAL+PARCIAL | Export interno; pickers nativos NOT_TESTABLE |
| Impressão Windows | NÃO TESTÁVEL (ambiente) | PrintDialog |
| XML NF-e import | REAL+TESTADO | — |
| SEFAZ emissão | NÃO IMPLEMENTADO | — |
| PIX/gateway/cartão/boleto | NÃO IMPLEMENTADO / FORA (desktop 1.0) | — |

---

## 5–12. Pilares (estado + proposta)

Ver documentos:

- `Docs/architecture/PRIMOX-FISCAL-ARCHITECTURE.md`
- `Docs/architecture/PRIMOX-SYNC-ARCHITECTURE.md`
- `Docs/architecture/PRIMOX-CLOUD-ARCHITECTURE.md`

| Pilar | Agora | Próximo degrau |
|-------|-------|----------------|
| NF-e | Import only | Decisão Opção A vs B → homologação |
| Multi-filial | Mock | Schema Empresa/Filial + ownership |
| Sync | Ausente | Outbox após API+Filial |
| API/RBAC | Minimal | Auth real + policies aplicadas |
| Multi-tenant/SaaS/Billing | Ausente | Só após sync/API |

---

## 13–14. Arquitetura atual vs proposta

### Atual

```text
WPF net6.0-windows ── SQLite local (AppData)
         │
         ├── PermissionService (UI)
         ├── NFeService (import)
         ├── FilialService (mock)
         └── NotificationService (stub)
PrimoAutoEletrica.Api (net9 minimal, open endpoints)
```

### Proposta (gradual — não big-bang)

```text
PRIMOX Desktop (modo local SQLite) ── permanece
        │
        └── modo conectado (opt-in)
                │
                ▼
             API autenticada
                │
         ┌──────┴──────┐
         ▼             ▼
    Outbox Sync    Fiscal Adapter
         │             │
         ▼             ▼
   PostgreSQL      SEFAZ ou Provedor
   (multi-tenant)
```

**Regra:** não abandonar SQLite local; cloud é camada adicional.

---

## 15. Dependências externas (não contratar automaticamente)

| Serviço | Finalidade | Custo relativo | Alternativas | Dependência |
| ------- | ----------- | -------------: | ------------ | ----------- |
| Provedor fiscal / SEFAZ direta | NF-e | Alto (manutenção schemas) | Focus NFe, Tecnospeed, Bling, etc. vs Direct | Certificado A1 |
| WhatsApp Cloud / Twilio | Mensagens | Médio/alto | Manter `wa.me` | Meta Business / conta |
| E-mail (SendGrid/SES/SMTP) | Notificações | Baixo–médio | SMTP próprio | DNS/SPF |
| Cloud (Azure/AWS) | SaaS host | Alto ops | Self-host VPS | Multi-tenant pronto |
| Gateway pagamento | Billing | Médio + % | Stripe/Pagar.me/Asaas sandbox | Webhooks |

---

## 16. Riscos

| Risco | Severidade | Mitigação |
|-------|------------|-----------|
| Declarar NF-e “pronta” sem SEFAZ | CRITICAL | Critério §45; só homologação primeiro |
| SaaS improvisado sobre WPF | CRITICAL | Arquitetura cloud antes de código |
| Migrations 27 vs ~32 | HIGH | Inventário antes de novas migrations |
| Stub Notification marcar “Enviado” | HIGH | Não mentir status no DB |
| Filial mock Guid novo a cada load | MEDIUM | Persistência real |
| Quebrar v1.0.0 / AppData | CRITICAL | Tag intacta; backups; sem reset |
| Escopo paralelo (NF-e+SaaS+Sync) | HIGH | Ordem §38; uma etapa por vez |

---

## 17. Complexidade técnica relativa (ordem de magnitude)

```text
A11y  <  Integrações prioritárias  <  API/RBAC  <  Multi-filial
      <  Sync  <  NF-e homologação  <  NF-e produção
      <  Multi-tenant  <  SaaS+Billing+Ops
```

---

## 18. Ordem recomendada (inalterável sem revisão)

1. Acessibilidade  
2. Integrações prioritárias (classificar + só as aprovadas)  
3. NF-e homologação  
4. NF-e produção (após evidência)  
5. Multi-filial  
6. Sync  
7. API/RBAC endurecido  
8. Multi-tenant  
9. SaaS foundation  
10. Billing  

---

## 19. NÃO implementar ainda

- Emissão SEFAZ produção  
- Cobrança real  
- Contratar provedores pagos  
- PostgreSQL cutover destruindo SQLite  
- Reescrever WPF em web  
- Multi-tenant sem API auth  
- Sync que só limpa fila local  
- Qualquer alteração à tag `v1.0.0`  
- Apagar WIP Help/Deploy  
- Remover migrations históricas  

---

## 20. Critérios de aceite (por etapa)

Conforme instrução §43 + critérios específicos §45–48.  
Após cada etapa: BUILD → QA → docs → commit dedicado → **PARAR**.

---

## Decisões de produto pendentes (bloqueiam pilares 3+)

1. **NF-e:** Opção A (SEFAZ direta) vs Opção B (provedor) — ver Fiscal Architecture.  
2. **Integrações:** manter só `wa.me` vs investir Cloud API/SMS.  
3. **2FA:** obrigatório no login ou opcional por perfil.  
4. **Cloud host / região / LGPD DPA** antes de multi-tenant.  
5. **Modelo comercial:** desktop forever vs desktop+cloud hybrid vs SaaS-only futuro.

---

## Decisão FASE A

# REQUIRES PRODUCT DECISION

- **ETAPA 1 (Acessibilidade):** técnica e QA-ready → pode receber **GO** isolado.  
- **ETAPAS 3–10:** bloqueadas até decisões acima.  
- **Não** iniciar implementação das grandes features nesta resposta.

---

## Próximo passo humano

Responder com:

1. `GO ETAPA 1` (a11y), e/ou  
2. Escolhas: NF-e A/B · WhatsApp strategy · 2FA policy · horizonte SaaS (sim/não/quando).
