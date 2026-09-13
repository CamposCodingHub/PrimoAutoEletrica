# PRIMOX — Product Truth Audit (reconciliado)

**Atualizado:** 2026-09-13 · pós NET10-26  
**Original:** 2026-09-08 (Product Truth 1.0) — avanços daquela auditoria preservados na crônica  
**Índices:** [`Docs/CURRENT-TRUTH.md`](../CURRENT-TRUTH.md) · [`Docs/PRIMOX-ADVANCES-CHRONICLE.md`](../PRIMOX-ADVANCES-CHRONICLE.md)

---

## Verdade atual (não overclaim)

| Afirmação | Verdade |
|-----------|---------|
| É ERP desktop de oficina | **SIM** |
| Emite NF-e em produção nesta máquina sem credencial | **NÃO** |
| Tem caminho de software para NF-e (Fake + Focus homolog) | **SIM** (NET10-26) |
| NFC-e/NFS-e completos | **NÃO** (scaffold + Fake) |
| DANFE oficial SEFAZ | **NÃO** (PDF informativo) |
| WhatsApp API Cloud | **NÃO** (`wa.me` sim) |
| 2FA obrigatório no login | **NÃO** (serviço/setup existem) |
| TFM trabalho NET10 | **SIM** (`net10.0-windows`) |
| Tag 1.0.0 = net6 histórico | **SIM** |

---

## Matriz de capacidades (2026-09-13)

| Capacidade | Classificação |
|------------|---------------|
| Clientes / Veículos / OS / Orçamentos / Agenda | REAL |
| Estoque / PDV / Financeiro / Relatórios | REAL (com limitações) |
| Import NF-e | REAL |
| Fiscal foundation + Fake cycles | REAL + TESTED |
| Focus HTTP emit/consult/cancel/XML | REAL no código; LIVE BLOCKED_EXTERNAL |
| Multiempresa fiscal DB | REAL + TESTED |
| NFC-e / NFS-e / PlugNotas | SCAFFOLD |
| Certificado A1 / XmlSigner | BLOCKED_EXTERNAL |
| WhatsApp wa.me | REAL |
| IWhatsAppProvider / API | SCAFFOLD / BLOCKED_EXTERNAL |
| Multi-filial comercial / Sync / SaaS | NÃO / FUTURE |
| Code signing | BLOCKED_EXTERNAL |

---

## Avanços da auditoria original (08/09) — registro

A Truth Audit 1.0 forçou disciplina de classificação REAL/PARCIAL/SCAFFOLD e derrubou overclaims de documentação.  
Esse **método** permanece válido. Os **números/TFM/fiscal NI** daquela data estão supersedidos por NET10 + NET10-26.

---

## Regras permanentes

1. UI button PASS ≠ domínio completo.  
2. Fake PASS ≠ emissão live.  
3. PDF informativo ≠ DANFE oficial.  
4. Roadmap comercial ≠ prova técnica.
