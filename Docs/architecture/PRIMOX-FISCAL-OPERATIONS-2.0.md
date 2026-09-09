# PRIMOX — Fiscal Operations Center 2.0

**Status:** OPERATIONAL FLOW IMPLEMENTED (simulation) · LIVE HOMOLOGATION PENDING · Production **BLOCKED**  
**Updated:** 2026-09-08

## Architecture (unchanged provider boundary)

```text
Venda/PDV
  → VendaFiscalNFeMapper
  → FiscalDocumentValidator / FiscalNFePreviewBuilder
  → NFeHomologationService / FiscalOperationsCenterService
  → FiscalApplicationService (idempotency + state guards + cancel)
  → IFiscalProvider → FocusNfeProvider | FakeFiscalProvider (tests)
```

## New / extended in 2.0

| Component | Role |
|-----------|------|
| `FiscalHealthCheck` | READY / INCOMPLETE / INVALID / CREDENTIAL_MISSING / PRODUCTION_BLOCKED |
| `FiscalNFePreviewBuilder` | Pré-visualização técnica (não DANFE) |
| `FiscalStateMachine` | Transições válidas; bloqueia Rejected→Authorized etc. |
| `FiscalOperationsCenterService` | Saúde, config emitente, histórico, consulta, cancel |
| `FiscalOperationsControl` | UI: emitente, produtos pendentes, histórico |
| `FiscalIssuerProfile.SerieNFe` | Série obrigatória na validação |
| Fake scenarios | + HTTP500, Unauthorized, SlowResponse |
| Cancel guards | Somente Authorized + Homologation; Focus cancel ainda NOT EXECUTED live |

## Limitations

| Item | Status |
|------|--------|
| NF-e homologação (simulação) | IMPLEMENTED |
| NF-e live Focus | NOT EXECUTED (sem token nesta sessão) |
| Cancel live Focus | NOT EXECUTED |
| DANFE | OUT OF SCOPE (contrato futuro) |
| NFC-e / NFS-e | OUT OF SCOPE |
| Produção | BLOCKED |

## Config

`%LOCALAPPDATA%\PrimoAutoEletrica\Config\fiscal-foundation.json` + DPAPI / `PRIMOX_FOCUS_HOMOLOG_TOKEN`
