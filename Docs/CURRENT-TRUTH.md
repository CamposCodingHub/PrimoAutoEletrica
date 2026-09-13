# PRIMOX — Fonte de verdade atual (CURRENT TRUTH)

**Atualizado:** 2026-09-13  
**Branch de trabalho:** `migration/net10`  
**HEAD de referência:** `1372e11` (`feat: complete fiscal foundation NET10-26`)  
**TFM:** `net10.0-windows`  
**Tag comercial protegida:** `v1.0.0` → `72d85fa` (**não mover**)  
**main / primox-net6-final:** protegidos — sem merge/push/tag desta fase

**Crônica de melhorias:** [`Docs/PRIMOX-ADVANCES-CHRONICLE.md`](PRIMOX-ADVANCES-CHRONICLE.md)  
**Política de docs:** relatórios de fase foram reescritos em **duas camadas** (Estado atual + Avanços históricos). Não misturar as camadas.

---

## O que usar como verdade

| Tema | Documento canônico | Não usar como verdade atual |
|------|--------------------|-----------------------------|
| Índice de docs | [`Docs/DOCUMENTATION-INDEX.md`](DOCUMENTATION-INDEX.md) | — |
| Crônica de melhorias | [`Docs/PRIMOX-ADVANCES-CHRONICLE.md`](PRIMOX-ADVANCES-CHRONICLE.md) | — |
| Fiscal (código + classificação) | [`Docs/qa/PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md`](qa/PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md) | NET10-25 audit, Product Truth 1.0, Commercial Readiness fiscal rows, architecture “não implementado” pré-NET10 |
| Baseline pré-NET10-26 | [`Docs/qa/PRIMOX-NET10-26-BASELINE.md`](qa/PRIMOX-NET10-26-BASELINE.md) | Contagens Unit 173 de banners antigos |
| Status vivo do projeto | [`PROJECT_STATUS.md`](../PROJECT_STATUS.md) (banner NET10-26 no topo) | Banners NET10-25 “próximos: cancel/XML” como backlog atual |
| Roadmap produto | [`PrimoAutoEletrica/Docs/ROADMAP_PRODUTO_VENDAVEL.md`](../PrimoAutoEletrica/Docs/ROADMAP_PRODUTO_VENDAVEL.md) | Roadmap 100% (FASE A 08/09) sem banner |
| Roadmap estratégico 100% | [`Docs/qa/PRIMOX-100-PERCENT-ROADMAP.md`](qa/PRIMOX-100-PERCENT-ROADMAP.md) | Versões FASE A pré-rewrite |
| Tracker | [`Docs/PRIMOX-PROJECT-TRACKER.md`](PRIMOX-PROJECT-TRACKER.md) | Snapshot I18N-07 / net6 |
| Arquitetura fiscal | [`Docs/architecture/PRIMOX-FISCAL-ARCHITECTURE.md`](architecture/PRIMOX-FISCAL-ARCHITECTURE.md) | Foundation 1.0 / Operations 2.0 textos pré-HTTP sem delta |
| Decisão provedor | [`Docs/qa/PRIMOX-FISCAL-DECISION.md`](qa/PRIMOX-FISCAL-DECISION.md) | Texto “Implementar agora? NÃO” só como histórico |
| Comercial / installer (histórico Script 7) | [`Docs/qa/PRIMOX-COMMERCIAL-INSTALLER-AUDIT.md`](qa/PRIMOX-COMMERCIAL-INSTALLER-AUDIT.md) | Usar só para signing/packaging daquela sessão |
| Desktop deploy NET10 | [`Docs/qa/PRIMOX-NET10-DESKTOP-DEPLOY.md`](qa/PRIMOX-NET10-DESKTOP-DEPLOY.md) | — |

---

## Snapshot técnico (2026-09-13)

| Área | Estado atual | Produção / live |
|------|--------------|-----------------|
| Desktop core (clientes, OS, orçamento, PDV, estoque, financeiro…) | REAL + TESTADO | OK para uso interno |
| TFM | `net10.0-windows` | — |
| Unit | **194/194** | — |
| QaEngine | **43/43** | — |
| DeepQa | **6/6** (baseline NET10-26) | — |
| NF-e foundation + Focus HTTP (emit/consult/cancel/XML) | PARTIAL + TESTADO (Fake + stubs HTTP) | LIVE = **BLOCKED_EXTERNAL** |
| NFC-e / NFS-e | SCAFFOLD + FAKE_ONLY | BLOCKED_EXTERNAL |
| DANFE | PDF **informativo** (não layout SEFAZ oficial) | BLOCKED_EXTERNAL para oficial |
| Multiempresa fiscal (DB) | IMPLEMENTED + TESTADO | UI produto multi-filial ainda futura |
| Certificado A1 / assinatura XML | Abstração + bloqueado | BLOCKED_EXTERNAL |
| PlugNotas | SCAFFOLD | BLOCKED_EXTERNAL |
| WhatsApp `wa.me` | REAL (manual) | — |
| WhatsApp Business API | Abstração + blocked | BLOCKED_EXTERNAL |
| 2FA TOTP | Serviço + setup existem; **não** no fluxo de login | Reavaliar em audit dedicado |
| Calendar Dark | `CalendarContrastHealer` — fechado NET10-22/23 | Não citar “KNOWN LIMITATION” antigo |
| Code signing | BLOCKED_EXTERNAL | Cert comercial ausente |
| SaaS / sync cloud | Fora do escopo imediato | Desktop-first |

---

## Estratégia de produto (inalterada e coerente)

1. **Desktop Workshop** → fiscal completo testável → instalação → segurança → WhatsApp API → aprovação digital → DVI → estoque inteligente  
2. Depois mobile complementar  
3. Depois cloud / multi-loja  

**Não** tratar “não pode vender SaaS nem emissão SEFAZ” como verdicto eterno: recalcular após evidência. Hoje: **software fiscal preparado**; **emissão homolog/produção = BLOCKED_EXTERNAL** sem CNPJ/token/cert.

---

## Reforma tributária (IBS/CBS)

Preparar **arquitetura extensível** de tributos/itens. **Não** implementar regras IBS/CBS neste repo sem schema/fonte oficial comprovada.

---

## Regra para agentes

1. Descobrir estado no **código + testes**.  
2. Consultar este índice + NET10-26.  
3. Documentos de fase (NET10-00…25, COMMERCIAL-08…10, FULL-ASSURANCE, I18N) = **HISTÓRICOS** salvo indicação explícita de CURRENT.  
4. Nunca copiar roadmap comercial antigo para `PROJECT_STATUS` sem revalidação.
