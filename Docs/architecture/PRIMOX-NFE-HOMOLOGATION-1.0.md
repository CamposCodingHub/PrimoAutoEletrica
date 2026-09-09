# PRIMOX — NF-e Homologation 1.0

**Status:** NF-e Homologation flow implemented · Live Focus homologation **pending credentials** · Production **BLOCKED**  
**Origin:** Venda (PDV)  
**Provider:** Focus NFe via `IFiscalProvider` / `FocusNfeProvider`  
**Updated:** 2026-09-08

---

## Architecture

```text
Venda (PDV)
   ↓
VendaFiscalNFeMapper
   ↓
FiscalDocumentValidator  (no invented NCM/CFOP/CST)
   ↓
NFeHomologationService
   ↓
FiscalApplicationService (idempotency + consult-before-retry)
   ↓
IFiscalProvider → FocusNfeProvider
   ↓
Focus HTTP (Homologation only, LiveHttpEnabled opt-in)
```

## Configuration (local)

File: `%LOCALAPPDATA%\PrimoAutoEletrica\Config\fiscal-foundation.json`

- `environment`: Homologation (Production forced off)
- `liveHttpEnabled`: must be `true` for real Focus calls
- `homologationBaseUrl`: `https://homologacao.focusnfe.com.br`
- `issuer`: CNPJ, IE, CRT, endereço, IBGE, CSOSN/CST padrão, origem ICMS

Secrets:

- DPAPI file `fiscal-secrets.dpapi` via `SaveHomologationToken`, **or**
- Environment variable `PRIMOX_FOCUS_HOMOLOG_TOKEN` (never commit / never log)

## Production protection (multi-layer)

1. `FiscalProductionGuard`
2. `FiscalConfigurationService.Normalize` (clears Production / prod URL)
3. `FocusNfeProvider` refuses Production + prod URL
4. `NFeHomologationService` entry forces Homologation
5. UI confirmation text says **HOMOLOGAÇÃO**

## Idempotency

- Key: `nfe-venda-{vendaId}`
- Final states reused
- Processing/Unknown/Timeout → **Consultar** before any new emit (no blind retry)

## UI

PDV button **NF-e Homologação** with explicit confirmation. Shows Authorized / Rejected / awaiting consult — never invents success.

## Cancel

**NOT EXECUTED** — requires a real authorized homolog NF-e.

## Live homologation

Requires issuer config + token + LiveHttpEnabled. Without them: validation fails or Focus returns NotConfigured. Do **not** treat as PASS.
