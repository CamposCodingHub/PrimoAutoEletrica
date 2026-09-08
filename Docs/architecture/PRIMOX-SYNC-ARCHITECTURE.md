# PRIMOX — Arquitetura de Sincronização (FASE A)

**Status:** ARCHITECTURE PROPOSAL — **NÃO IMPLEMENTADO**  
**Código atual:** não há engine de sync remoto; não declarar fila local como sync.

---

## 1. Problema

Desktop SQLite funciona offline por natureza.  
“Sincronização” só existe se houver **servidor autoritativo** + confirmação.

---

## 2. Pré-requisitos (ordem)

1. Identidade de dispositivo / instalação  
2. API autenticada  
3. Modelo Empresa/Filial (ou Tenant/Filial) estável  
4. IDs globais (Guid) nas entidades sincronizáveis  
5. Outbox local  

**Não** implementar sync antes de API auth + multi-filial ownership.

---

## 3. Fluxo alvo

```text
LOCAL change
  → OUTBOX (PENDING)
  → SYNC ENGINE (SENDING)
  → API
  → SERVER persist + ack
  → OUTBOX (SYNCED)
  → PULL delta
  → apply local (version check)
```

Estados: `PENDING | SENDING | SYNCED | FAILED | CONFLICT | RETRY`

Nunca apagar outbox sem ack.

---

## 4. Outbox (proposta)

| Campo | Papel |
|-------|-------|
| Id | Guid |
| EntityType / EntityId | alvo |
| Operation | Insert/Update/Delete |
| Payload | JSON |
| FilialId / TenantId | escopo |
| Version / UpdatedAt | conflito |
| Status / Attempts / LastError | retry |
| CorrelationId | observabilidade |

---

## 5. Conflitos

**Não** defaultar “last write wins” globalmente.

| Entidade | Estratégia sugerida |
|----------|---------------------|
| Produto.estoque (qty) | Operações comutativas / ledger; não overwrite cego |
| Cliente cadastro | Version check → CONFLICT → revisão |
| OS status | Regras de domínio; bloqueio se divergente |
| Financeiro liquidado | Imutável pós-baixa; só eventos compensatórios |

Toda resolução → audit log.

---

## 6. O que NÃO é sync

- Limpar lista em memória  
- `Task.FromResult(true)`  
- UDP discovery sem ack  
- Export/import CSV manual (pode ser ferramenta, não sync)

---

## 7. Critério REAL

offline → alteração → fila → online → envio → confirmação → pull → persistência local, com retry, duplicidade, conflito, falha e recuperação — testado com `QA_SYNC_`.

---

## 8. Documento relacionado

Cloud/multi-tenant: `PRIMOX-CLOUD-ARCHITECTURE.md`.  
Integrações: `PRIMOX-INTEGRATION-ARCHITECTURE.md`.

---

## 9. Atualização — TOTAL AUDIT 1.0 (2026-09-08)

### CURRENT STATE

| Item | Estado |
|------|--------|
| SQLite local offline-capable | REAL (desktop) |
| `ProcessarFilaOfflineAsync` / outbox remoto | **NÃO IMPLEMENTADO** (sem transporte remoto) |
| Local UDP / LocalSyncSimulator | PARCIAL (LAN) — **não** é sync multi-loja |
| FilialId isolation | SCAFFOLD (`FilialService` hardcoded) |
| API como hub de sync | PARCIAL (endpoints mínimos, sem auth) |

### TARGET STATE

Outbox → API autenticada → ack → pull delta → conflitos versionados → idempotência.

### GAPS

Ver `Docs/qa/PRIMOX-INTEGRATION-GAPS.md` (IG-H01, IG-H05).  
**Nunca** classificar limpeza de fila local como sincronização.
