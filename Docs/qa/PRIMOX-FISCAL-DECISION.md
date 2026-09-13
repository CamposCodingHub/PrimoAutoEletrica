# PRIMOX — Fiscal Decision Record

**Decisão de produto (válida):** Opção B — provedor · Focus principal · PlugNotas alternativa  
**Status de implementação:** **GO EXECUTADO** na fundação (NET10-11…26) · live homolog ainda **CONDICIONAL**  
**Atualizado:** 2026-09-13  
**Canônico:** [`PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md`](PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md)

---

## Decisão (permanece)

| Item | Escolha |
|------|---------|
| SEFAZ direta vs Provedor | **Provedor (Opção B)** |
| Provedor principal | **Focus NFe** |
| Alternativa | **TecnoSpeed PlugNotas** |
| Certificado | **A1 eCNPJ** (quando aplicável; hoje token Focus + DPAPI) |
| Onda 1 | NF-e homologação |
| Onda 2 | NFC-e |
| Onda 3 | NFS-e |
| Produção SEFAZ nesta fase | **NÃO** |

---

## Status vs decisão original (08/09/2026)

| Item original | Agora |
|---------------|-------|
| Implementar agora? **NÃO** | Fundação **implementada** (NET10-26) |
| Início de código bloqueado | Código fiscal ativo em `Services/Fiscal/` |
| GO CONDICIONAL homolog | Ainda CONDICIONAL (token/cert/empresa) |

---

## GO / NO-GO atual

### GO para usar fundação + Fake / testes
Sempre — coberto por unit/mega tests.

### GO CONDICIONAL para homolog live Focus
1. Token homolog no secret store DPAPI  
2. Dados emitente reais de teste (não inventar)  
3. Ambiente só `homologacao.focusnfe.com.br`  
4. Produção continua bloqueada por guard  

### NO-GO
- Produção SEFAZ  
- Inventar schema/endpoints  
- Declarar PRODUCTION READY  

---

## Avanços desde a decisão

Focus HTTP emit/consult/cancel/XML · Fake · multiempresa DB · DANFE informativo · scaffolds NFC-e/NFS-e/PlugNotas · abstrações cert/WhatsApp/webhook.
