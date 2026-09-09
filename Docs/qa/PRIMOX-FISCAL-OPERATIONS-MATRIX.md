# PRIMOX — Fiscal Operations Test Matrix 2.0

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
