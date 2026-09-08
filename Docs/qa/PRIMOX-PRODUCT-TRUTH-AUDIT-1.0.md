# PRIMOX Workshop — Product Truth Audit 1.0

## 1. Executive Summary

PRIMOX Workshop **1.0.0** is a **real WPF desktop** workshop management product with validated core operations (clients, vehicles, OS, quotes, stock, POS, finance, reports, NF-e **import**, packaging/install).  

It is **not** a complete enterprise ERP, **not** SaaS, **not** SEFAZ-emitting fiscal software, and **not** multi-filial with remote sync.

**Final decision:** **PRODUCT TRUTH VERIFIED WITH LIMITATIONS**

UI Exhaustive 3.0 evidence (1909/1909 executable PASS) remains valid and is **orthogonal** to domain completeness.

---

## 2. Scope

Truth audit of code × documentation × functionality × evidence × commercial readiness.  
**No** Fase 16, **no** SaaS, **no** redesign, **no** architecture rewrite, **no** tag move.

---

## 3. Methodology

Hierarchy: executed code → real tests → schema/migrations → runtime evidence → wired services/UI → documentation.  
Documentation alone is never proof.

---

## 4. Git Baseline

| Item | Value |
|------|-------|
| Branch | `main` |
| HEAD (audit start) | `1f3af7e` |
| Tag `v1.0.0` | annotated tag; **`v1.0.0^{commit}` = `a4ad6fe`** (**intact**) |
| WIP preserved | HelpControl, HelpTopicsCatalog, Deploy scripts |

---

## 5. Product Inventory

Desktop modules present in UI navigation (~17 pages): Dashboard, Clientes, Veículos, OS, Orçamentos, Agenda, Estoque, Financeiro, Relatórios, PDV, Funcionários, Fornecedores, NF-e import, Kanban, Catálogo, Auto Elétrica, Configurações/Help (Help WIP).

Separate: `PrimoAutoEletrica.Api` (minimal ASP.NET, net9) — pilot surface only.

---

## 6. Functional Matrix

See `Docs/qa/PRIMOX-PRODUCT-TRUTH-MATRIX.md`.

Objective counts (matrix rows): ~22 REAL/REAL+TESTADO · ~12 PARCIAL · ~5 SCAFFOLD/PLACEHOLDER · ~3 NÃO IMPLEMENTADO · orphans/WIP/SaaS separated.

---

## 7. Code vs Documentation

| Claim | Reality | Classification |
|-------|---------|----------------|
| Maturity 98/100 | Inflated vs gaps | DOCUMENTAÇÃO INCORRETA (corrected) |
| 2FA DONE wired login | TwoFactorService exists; Login has **no** TOTP challenge | DOCUMENTAÇÃO INCORRETA |
| Multi-filial done | FilialService mock | SCAFFOLD / DOC INCORRETA |
| NF-e complete | Import REAL; emission empty | PARTIAL vs NOT_IMPLEMENTED |
| Paginação SQL product-wide | No `ObterPaginado`; Estoque CLIENT_SIDE | DOC/assumptions false |
| API JWT + OS routes | Minimal API; no JWT wired; no OS map | DOC DESATUALIZADA |
| ARCHITECTURE .NET 9 WPF | Shipping TFM **net6.0-windows** | DOC DESATUALIZADA |
| Exhaustive 100% executable | True for executable subset | REAL (keep; do not generalize) |

---

## 8. Security

| Control | Status |
|---------|--------|
| PBKDF2 passwords | REAL+TESTADO |
| Login lockout 5/15min | REAL |
| Soft delete LGPD | REAL |
| SqlIdentifierGuard | REAL |
| Audit trail services | REAL |
| 2FA TOTP library + setup UI | PARCIAL (not enforced at login) |
| API JWT / named policies | SCAFFOLD / NOT_USED |
| Rate limiter (claimed DONE) | Verify carefully — not treated as proven commercial API hardening |

---

## 9. Database

| Item | Status |
|------|--------|
| Live integrity (smoke/AppData) | OK historically |
| CODE_MIGRATIONS | **27** `ApplyMigration` IDs in `DatabaseService.Migrations.cs` |
| HISTORICAL_DB_MIGRATIONS | AppData `SchemaVersion` **~32** rows (mixed legacy IDs) |
| Action | **Do not delete** historical migrations; document divergence |

---

## 10. API

REAL minimal host: health, orcamentos, estoque/produtos, financeiro.  
SCAFFOLD: JWT packages, Keycloak empty file, authorization without policies.  
Tests: `WebApplicationFactory` HTTP-shaped (**REAL+PARCIAL**), not source-only Assert.Contains — but surface incomplete / fragile.

---

## 11. Fiscal

| Capability | Status |
|------------|--------|
| XML NF-e import → stock/AP | REAL+TESTADO |
| Draft/emission/SEFAZ/DANFE/cancel | NÃO IMPLEMENTADO (`NFeEmissaoService.cs` empty) |

---

## 12. LGPD

Consent fields, soft delete, restore paths: REAL+PARCIAL.  
Not a full DPO/SaaS compliance program.

---

## 13. Multi-filial

UI selection after login: SCAFFOLD (hardcoded Matriz/Filial).  
No durable multi-tenant branch model.

---

## 14. Offline

No `ProcessarFilaOfflineAsync`. Remote sync: NÃO IMPLEMENTADO.  
Local desktop use without sync: normal.

---

## 15. Notifications

Twilio/SMS HTTP: PLACEHOLDER (`Task.Delay` + TODO).  
WhatsApp `wa.me` deep links: REAL.

---

## 16. Backup / Restore

REAL+TESTADO (Config UI + DatabaseBackupService + Installation E2E evidence).

---

## 17. Update

UpdateService + AtualizacaoWindow exist but window is **orphan** (no navigation consumer found).  
Commercial auto-update: NOT READY. Deploy/installer scripts are the practical path.

---

## 18. UI Evidence

Preserved from Exhaustive UI Audit 3.0:

- Discovered 3220 · Tested 1909 · PASS 1909 · FAIL 0 · BLOCKED 0  
- tested/executable **100%** · tested/discovered **59.29%**  
- P15E-012 VERIFIED runtime · P15E-015 PARTIAL  

---

## 19. QA Evidence

| Suite | Result |
|-------|--------|
| Exhaustive 3.0 | PASS WITH KNOWN LIMITATIONS |
| QaEngine | 42/42 (latest Exhaustive cycle) |
| CompleteUi | PASS |
| Deep QA | 6/6 |
| Long Run | 5 ciclos / 90 nav |
| Packaging 15B / Install 15C | GO / PASS (known limits) |

---

## 20. Documentation Accuracy

Corrected in this audit:

- PROJECT_STATUS header: Product Truth banner; maturity reclassified  
- SECURITY.md: qualify 2FA  
- ARCHITECTURE.md: net6 shipping + API note  
- New matrices + commercial readiness  

Older body sections of PROJECT_STATUS may still contain historical ROI/.NET 9 narrative — treat as **archive**, not truth.

---

## 21. Product Gaps

See `Docs/qa/PRIMOX-PRODUCT-GAPS.md`.

---

## 22. Commercial Readiness

See `Docs/qa/PRIMOX-COMMERCIAL-READINESS.md`.

---

## 23. Pilot Readiness

**YES** — suitable for own workshop / pilot with documented limits (no SEFAZ emission, no multi-filial sync, manual update, unsigned installer).

---

## 24. SaaS Readiness

**NO** — explicitly out of scope; no multi-tenant cloud product.

---

## 25. Known Limitations

Native print/file dialogs · CLIENT_SIDE paging · API unauthenticated · 2FA not in login · NF-e emission absent · filial mock · notification stubs · orphan VMs · Help WIP · code signing absent · migration count divergence.

---

## 26. Corrective Actions

| Action | Done in this audit? |
|--------|---------------------|
| Truth matrices + commercial doc | YES |
| Fix exaggerated PROJECT_STATUS header | YES |
| Fix SECURITY / ARCHITECTURE false framing | YES |
| Implement SEFAZ / sync / SaaS | **NO** (forbidden) |
| Delete orphans / migrations | **NO** (RETAIN) |

---

## 27. Final Decision

# PRODUCT TRUTH VERIFIED WITH LIMITATIONS

The product **is** a commercially installable desktop workshop system for pilot/limited sale, with honest boundaries.  
UI button execution coverage ≠ product completeness ≠ SaaS readiness.
