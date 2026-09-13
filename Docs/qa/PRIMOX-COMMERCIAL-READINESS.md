# PRIMOX Workshop — Commercial Readiness (reconciliado)

**Produto:** PRIMOX Workshop desktop  
**Atualizado:** 2026-09-13 · pós NET10-26  
**Tag:** `v1.0.0` → `72d85fa`  
**Índices:** [`Docs/CURRENT-TRUTH.md`](../CURRENT-TRUTH.md) · [`Docs/PRIMOX-ADVANCES-CHRONICLE.md`](../PRIMOX-ADVANCES-CHRONICLE.md)

Legenda: `READY` | `READY WITH LIMITATION` | `PILOT ONLY` | `NOT READY` | `BLOCKED_EXTERNAL` | `FUTURE`

---

## Matriz comercial atual

| Área | Classificação | Nota |
|------|---------------|------|
| Produto desktop oficina | READY WITH LIMITATION | Core real |
| TFM branch trabalho | READY WITH LIMITATION | `net10.0-windows` em `migration/net10`; tag 1.0.0 histórica net6 |
| Instalação / packaging | READY WITH LIMITATION | Inno/E2E; signing ausente |
| Backup / Restore | READY | |
| Banco SQLite | READY WITH LIMITATION | integrity OK; migrations evoluem |
| Segurança login | READY WITH LIMITATION | PBKDF2+lockout; 2FA **não** no login |
| CRUD / OS / Orçamentos / Agenda / Estoque / Financeiro / PDV | READY WITH LIMITATION | Limitações de domínio documentadas em audits históricos |
| Relatórios | READY WITH LIMITATION | |
| NF-e import | READY | |
| NF-e software path (Fake/Focus) | READY WITH LIMITATION | Testado; **não** = live |
| NF-e emissão live homolog/prod | BLOCKED_EXTERNAL | Sem token/CNPJ/cert |
| NFC-e / NFS-e | NOT READY / SCAFFOLD | Fake only |
| DANFE oficial SEFAZ | NOT READY | PDF informativo existe |
| WhatsApp wa.me | READY | |
| WhatsApp Business API | BLOCKED_EXTERNAL | Abstração pronta |
| Code signing | BLOCKED_EXTERNAL | |
| Auto-update | NOT READY | |
| Multi-filial real | NOT READY | Fiscal DB multiempresa ≠ multi-oficina completa |
| Cloud / SaaS | FUTURE | Desktop-first |

---

## Pode vender?

| Pergunta | Resposta (2026-09-13) |
|----------|----------------------|
| Instalar em oficina própria / piloto? | **SIM** |
| Vender como desktop com limitações? | **SIM — COM LIMITAÇÕES** |
| Vender como “emite NF-e em produção”? | **NÃO** (BLOCKED_EXTERNAL) |
| Vender como SaaS? | **NÃO** (fora do escopo atual) |
| Afirmar DANFE oficial / XML SEFAZ validado? | **NÃO** |

Separar sempre:

- **Software fiscal implementado/testado**  
- **Homologação live**  
- **Produção SEFAZ**

---

## Avanços desde o Commercial Readiness 1.0 (2026-09-08)

| Avanço | Fase |
|--------|------|
| I18N fechada | I18N-07 |
| NET10 + calendar healer + deploy | NET10-20…24 |
| Fundação fiscal completa (cancel/XML/multiempresa/DANFE info/scaffolds) | NET10-26 |
| Unit 173 → 194 | NET10-26 |

O relatório original de 08/09 (“NF-e emissão NOT READY / NÃO IMPLEMENTADO”) era correto **na época**. Hoje o software path existe; live permanece externo.
