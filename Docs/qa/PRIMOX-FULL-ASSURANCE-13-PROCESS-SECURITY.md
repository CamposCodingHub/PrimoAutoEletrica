# PRIMOX FULL ASSURANCE-13 — PROCESS SECURITY

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

**Data:** 11/09/2026  
**Baseline HEAD:** `5cd5549`

## Objetivo

Fechar residual `Process.Start` do Assurance-12 (OrdensServico, Orcamentos, ImportarNFe, Agendamentos, OficinaKanban).

## Controles

`SecureProcessLauncher`:

| API | Regras |
|---|---|
| `OpenUri` | Somente `http://` `https://` `mailto:`; rejeita `" ' ; \| \` CR LF` |
| `OpenWhatsAppLink` | Somente `http(s)://wa.me/` → `OpenUri` |
| `OpenFileOrDirectory` | Path jail (`PathSecurityHelper`) + arquivo/pasta existente |

## Migração A13

Migrados para o launcher:

- OficinaKanban WhatsApp  
- ImportarNFe pasta XML  
- Orcamentos PDF / WhatsApp / mailto  
- OrdensServico PDF / WhatsApp  
- Agendamentos WhatsApp / mailto  

Roots de open: + `MyDocuments\PrimoAutoEletrica` (+ `PDFs`).

## Gate final

| Métrica | Valor |
|---|---|
| TOTAL Process.Start (produto + launcher) | 2 (somente launcher) |
| SAFE | 0 |
| HARDENED | 2 + todos call sites |
| REVIEW | 0 |
| UNSAFE | 0 |
| Migrados nesta fase | 8 caminhos residuais |

**PROCESS SECURITY = VERIFIED** (evidência: inventário + smoke `A13Security` PASS)

## Evidência

- `TestResults/UiSmoke/a13-security-4/` — 1/1 PASS  
- Inventário: `PRIMOX-FULL-ASSURANCE-13-PROCESS-INVENTORY.md`
