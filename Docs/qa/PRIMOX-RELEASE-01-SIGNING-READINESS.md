# PRIMOX RELEASE-01 — SIGNING READINESS

**Data:** 12/09/2026  

| Check | Resultado |
|---|---|
| SignTool | READY (Windows Kit x64) |
| Timestamp RFC3161 | READY (`http://timestamp.digicert.com` default) |
| `PRIMOX_CODESIGN_THUMBPRINT` | ABSENT |
| Commercial cert in store | BLOCKED |
| Localhost cert | presente / **rejeitado** (não comercial) |
| Sign execution | **NOT EXECUTED** |
| Verification | **NOT EXECUTED** |

**RESULT:** SIGNING PIPELINE READY · CERTIFICATE = **BLOCKED**

Não comprar certificado. Não assinar com localhost.
