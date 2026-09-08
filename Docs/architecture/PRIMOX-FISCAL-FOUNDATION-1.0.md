# PRIMOX — Fiscal Foundation 1.0

**Status:** Fiscal Foundation implemented · Focus adapter prepared · Production emission **not** implemented  
**Product version:** 1.0.0 (tag `v1.0.0` → `a4ad6fe` intact — post-1.0 work on `main`)  
**Updated:** 2026-09-08

---

## 1. Goal

Establish a safe, testable fiscal architecture for future Focus NFe homologation **without** emitting NF-e/NFC-e/NFS-e in production.

```text
PRIMOX
   │
   ├── FiscalApplicationService
   │
   ├── IFiscalProvider
   │
   └── FocusNfeProvider (HTTP live OFF)
          │
          └── PRODUCTION = BLOCKED
```

---

## 2. What exists

| Layer | Types |
|-------|--------|
| Contracts | `IFiscalProvider`, `FiscalEmissionRequest`, `FiscalCancellationRequest`, `FiscalProviderResult`, `FiscalOperation`, `FiscalDocumentRecord` |
| Enums | `FiscalDocumentType`, `FiscalDocumentStatus`, `FiscalEnvironment`, `FiscalProviderKind`, `FiscalErrorKind` |
| Guard | `FiscalProductionGuard` — Production denied by default |
| Config | `FiscalConfigurationService` — JSON non-secrets + DPAPI secret file |
| Store | `FiscalOperationStore` + migration `202609080001` |
| App | `FiscalApplicationService` — idempotency + audit category `Fiscal` |
| Adapter | `FocusNfeProvider` — prepared; returns `NotImplemented` / `NotConfigured` / `ProductionBlocked` |
| Bridge | `NFeEmissaoService` — KEEP FUTURE facade over foundation (was 0-byte) |
| Test only | `FakeFiscalProvider` — never registered in commercial DI |

---

## 3. Decisions

| DECISION | REASON | IMPACT |
|----------|--------|--------|
| Keep `NFeEmissaoService` as bridge, not delete | Prior KEEP—FUTURE; avoid orphan name | Callers can migrate later to `FiscalApplicationService` |
| Focus HTTP live forced OFF in config normalize | Prevent accidental real calls this phase | Homologation HTTP = next phase |
| No `HttpClient` factory yet | No HTTP infra in WPF app today | Add factory when live HTTP lands |
| Secrets via DPAPI file under AppData `Config/` | Reuse `CryptoService`; no Git/appsettings secrets | Tokens never logged |
| Fake provider only in tests | Avoid commercial “success” path | DI registers `FocusNfeProvider` only |
| Lean tables: Operations / Documents / Events | Enough for idempotency + XML paths later | No premature 50-column schema |
| No fiscal UI screen | Contract allows conceptual env only | Settings UI = FUTURE |
| Document ≠ Finance auto-create | Explicit rule | Finance link = FUTURE after commercial origin |

---

## 4. Idempotency

- `FiscalOperation.Id` = internal operation id (independent of NF number / SEFAZ key / provider id)
- `IdempotencyKey` UNIQUE on `FiscalOperations`
- Retry with same key reuses the same operation (no second emission)
- After timeout: **Consultar** before any new emit decision

---

## 5. Environments

| Env | Meaning | Status |
|-----|---------|--------|
| Development | Local/dev | Allowed for foundation plumbing |
| Homologation | Provider sandbox | Target for next phase |
| Production | Real SEFAZ | **BLOCKED** (`FISCAL-PROD-BLOCKED`) |

Homologation ≠ Production (enforced in guard + tests).

---

## 6. Origin mapping (future — not wired in modules)

| Origin | Field on operation |
|--------|--------------------|
| OS | `OrdemServicoId` |
| PDV / Venda | `VendaId` |
| Orçamento | `OrcamentoId` |
| Module name | `OriginModule` |

PDV / OS / Orçamentos **not** modified to emit in this phase.

---

## 7. Webhook vs polling

```text
Future (API/SaaS):
Focus → Webhook → PRIMOX API → FiscalOperation → DB → Desktop/SaaS

Desktop now (documented strategy):
Emit → timeout/unknown → Consultar (polling) → update FiscalOperation
```

Webhook server: **not implemented** (FUTURE).

---

## 8. XML / DANFE / Cancel

- DB columns for `XmlEnviadoPath` / `XmlAutorizadoPath` prepared
- `ObterXmlAsync` / `ObterDanfeAsync` / `CancelarAsync` = contracts; Focus returns NotImplemented
- No fake PDF / no hand-built fiscal XML

---

## 9. Observability (future metrics — not a dashboard)

Counts to collect later: emissions, authorized, rejected, timeouts, errors, average duration.  
Logs may include OperationId, provider, environment, status, providerCode — **never** tokens/certs.

---

## 10. Rate limit / timeout / retry

- Live HTTP must use explicit timeout + `CancellationToken` (when implemented)
- Never blind re-emit; consult first
- Adapter should treat HTTP 429 / Retry-After when live path exists (documented; not coded)

---

## 11. Explicitly NOT implemented

- Real NF-e / NFC-e / NFS-e emission
- Production unlock
- Focus live HTTP
- Homologation credentials collection UI
- SaaS / multi-filial / PostgreSQL migration / billing / WhatsApp Cloud
- Installer / version bump

**Next step after review:** `PRIMOX NF-e HOMOLOGATION IMPLEMENTATION` (Focus + NF-e + Homologation only).
