# PRIMOX — Arquitetura de Integrações

**Status:** ARCHITECTURE PROPOSAL — **não implementar** nesta auditoria  
**Fonte de verdade atual:** `Docs/qa/PRIMOX-INTEGRATION-MATRIX.md` · Audit 1.0  
**Tag v1.0.0:** integrações cloud/SMS/SMTP/fiscal emissão **não** fazem parte do significado comercial 1.0.0

---

## 1. Situação atual (honesta)

```text
PRIMOX Desktop
 ├── wa.me deep links          → REAL
 ├── mailto:                   → REAL
 ├── NotificationService SMS/WA→ PLACEHOLDER (Delay + sucesso)
 ├── FilialService             → SCAFFOLD
 ├── NFe import                → REAL
 ├── NFe emissão               → NOT IMPLEMENTED
 ├── API REST                  → PARCIAL (sem auth)
 └── Sync remoto              → NOT IMPLEMENTED
```

---

## 2. Arquitetura alvo (Provider)

```text
PRIMOX Workshop (UI / Application Services)
        │
        ▼
Integration Abstractions (ports)
        │
        ├── IWhatsAppProvider
        ├── IEmailProvider
        ├── ISmsProvider
        ├── IFiscalProvider
        ├── IPaymentProvider
        └── ISyncProvider
        │
        ▼
Adapters / Providers (estratégia por ambiente)
        │
        ├── Dev / Test / Homolog / Production
        └── Secure configuration (sem secrets em log)
```

Cada abstração **só** se justifica quando houver decisão de produto para implementar o canal.

| Port | Justificativa | Estado |
|------|---------------|--------|
| `IWhatsAppProvider` | Separar wa.me de Cloud API | Futuro |
| `IEmailProvider` | mailto vs SMTP | Futuro |
| `ISmsProvider` | Evitar fake Twilio | Futuro |
| `IFiscalProvider` | Provider vs SEFAZ | Futuro P0 |
| `IPaymentProvider` | PIX interno ≠ gateway | Futuro |
| `ISyncProvider` | Evitar chamar limpeza de fila de “sync” | Futuro |

**Não** criar fakes que retornam sucesso.

---

## 3. Padrões recomendados

| Padrão | Uso |
|--------|-----|
| Provider / Strategy | Troca de fornecedor sem reescrever UI |
| Adapter | Isolar SDK/HTTP do domínio |
| Factory / DI | Seleção por ambiente |
| Retry + timeout | Integrações HTTP |
| Idempotency key | Fiscal / pagamento / sync |
| Correlation ID | Logs cross-service |
| Circuit breaker | Opcional (cloud) |
| Audit trail | Quem enviou o quê (sem PII excessiva) |
| Secure config | DPAPI / Credential Manager / secrets store — **nunca** plaintext em repo |

---

## 4. Ambientes

| Ambiente | Credenciais | Endpoints | Dados |
|----------|-------------|-----------|-------|
| DEV | sandbox / mocks explícitos | locais | sintéticos |
| TEST | QA isolado | staging | AutomatedTests |
| HOMOLOGATION | oficiais de homolog | homolog | não produção |
| PRODUCTION | produção | produção | reais |

Fiscal: **nunca** misturar homolog e produção no mesmo certificado/config sem barreira explícita.

---

## 5. Custo relativo

Ver matriz em `PRIMOX-INTEGRATION-MATRIX.md` (BAIXO/MÉDIO/ALTO).

---

## 6. Prioridade

| Prioridade | Item |
|------------|------|
| P0 | Fiscal (após GO) — compliance |
| P1 | Remover/neutralizar Notification fake; API auth; filial real |
| P2 | WhatsApp Cloud / SMTP |
| P3 | SMS / PIX gateway / SaaS |
| P4 | Limpeza de shells |

---

## 7. Anti-padrões atuais a eliminar no futuro

1. Retornar `true` após `Task.Delay` e gravar status “Enviado”.  
2. Documentar integração como REAL sem HTTP.  
3. Misturar deep link com Business API no marketing.  
4. Implementar `IFiscalProvider` com stub de autorização falsa.

---

**PARAR na documentação até decisão explícita do proprietário.**
