# PRIMOX — Fiscal Operations Test Matrix 2.0

> **DOCUMENTO REESCRITO EM CAMADAS — 2026-09-13**
>
> | Camada | Uso |
> |--------|-----|
> | **Estado atual** | Fonte operacional hoje · ver também `Docs/CURRENT-TRUTH.md` e NET10-26 |
> | **Avanços desta fase (histórico)** | Registro do que esta execução entregou — **não** sobrescrever mentalmente o estado atual |
>
> HEAD de referência pós-NET10-26: `1372e11` · TFM `net10.0-windows` · Branch `migration/net10`
> Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md`

## Estado atual (pós NET10-26 · 2026-09-13)

| Item | Valor |
|------|-------|
| Branch | `migration/net10` |
| HEAD fiscal foundation | `1372e11` |
| TFM | `net10.0-windows` |
| Unit | **194/194** |
| QaEngine | **43/43** |
| DeepQa | **6/6** (baseline NET10-26) |
| Fiscal LIVE / WhatsApp API / Code signing | **BLOCKED_EXTERNAL** |
| Calendar Dark | Mitigado (`CalendarContrastHealer`) — não citar KNOWN LIMITATION antigo como atual |
| NF-e | PARTIAL + TESTED (Focus path + Fake) |
| NFC-e / NFS-e | SCAFFOLD + FAKE_ONLY |
| DANFE | PDF informativo (≠ SEFAZ oficial) |
| Multiempresa fiscal | IMPLEMENTED + TESTED (DB) |

**Claims abaixo sobre net6, “emissão NÃO IMPLEMENTADO”, Unit 173, DANFE/cancel NI, Calendar Dark KNOWN LIMITATION, etc. pertencem ao registro histórico da fase.**

---

## Avanços desta fase (registro histórico — preservar)

| Teste | Resultado | Notas |
|-------|-----------|-------|
| Emitente incompleto | PASS | Health + Validator |
| Série ausente | PASS | FISCAL-EMITENTE-SERIE |
| NCM/CFOP ausente | PASS | Validator (pré-existente) |
| Preview sem HTTP | PASS | FiscalNFePreviewBuilder |
| Fake Authorized | PASS | |
| Fake Rejected | PASS | |
| Fake Timeout | PASS | |
| Fake Network / 503 / 500 / 401 / Invalid / Slow | PASS | |
| Idempotência / concorrência | PASS | EmitCount ≤ 2, mesma OpId |
| Cancel Rejected | PASS | STATE-BLOCKED |
| Cancel Authorized (Fake) | PASS | Cancelled |
| Production Guard | PASS | |
| Live Focus emit | NOT EXECUTED | Sem token |
| Live Focus cancel | NOT EXECUTED | |
| Restart recovery | PASS | via suíte NFe prévia + store |
| QaEngine | PASS | 43/43 |
