# PRIMOX RELEASE-01 — SIGNING READINESS

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

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
