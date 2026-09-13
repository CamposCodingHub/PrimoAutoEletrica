# PRIMOX — Crônica de avanços e melhorias

**Atualizado:** 2026-09-13  
**Branch atual:** `migration/net10` (NET10-27 TFM closure + NET10-26 fiscal)  
**Fonte de verdade técnica:** [`Docs/CURRENT-TRUTH.md`](CURRENT-TRUTH.md)

Este arquivo é o **índice narrativo** das melhorias do projeto.  
Relatórios de fase individuais permanecem como **evidência histórica**; não devem ser lidos como estado atual sem o carimbo abaixo.

---

## Como ler qualquer relatório antigo

| Seção | Significado |
|-------|-------------|
| **Estado atual (pós NET10-27 / NET10-26)** | O que vale hoje — código + testes |
| **Avanços desta fase (histórico)** | O que aquela execução entregou na época |
| **Limitações da época** | Podem ter sido fechadas depois |

---

## Linha do tempo (resumo executivo)

### Fundação comercial desktop 1.0
- Core oficina: clientes, veículos, OS, orçamentos, agenda, estoque, PDV, financeiro, relatórios
- Design System Light/Dark, Help, i18n PT/EN/ES (I18N-07 fechada com exceções explícitas)
- Installer Inno, packaging E2E, política AppData
- Tag `v1.0.0` protegida (`72d85fa`)
- **Avanço:** produto desktop piloto/comercial limitado sem emissão SEFAZ live

### Full Assurance / Master Audit / Release-01
- QA Engine 43/43, DeepQa, Exhaustive UI, security/red team baselines
- Database integrity, recovery, stress
- **Avanço:** readiness interno verificada; signing = externo

### Comercial 08–10
- Hardening installer, code signing pipeline (blocked sem cert), visual 09.5
- **Avanço:** packaging reproduzível; Calendar Dark ainda era “known limitation” **na época**

### NET10-00 … NET10-24 (migração)
- TFM → `net10.0-windows`
- Remoção LiveCharts/OpenTK (NU1701), CalendarContrastHealer
- Promotion readiness, RC gate, desktop deploy Program Files
- **Avanço:** runtime moderno + visual/calendar fechados nos gates NET10-22/23

### NET10-25 — Audit fiscal
- Inventário zero-trust: NF-e PARTIAL; NFC-e/NFS-e/DANFE/multiempresa/cert NI
- **Avanço:** mapa honesto do gap comercial fiscal

### NET10-26 — Fundação fiscal
- Multiempresa fiscal DB, Focus cancel/XML, DANFE informativo, artefatos path-safe
- NFC-e/NFS-e/PlugNotas scaffold, WhatsApp/Cert/Webhook abstrações
- Unit **194/194**, Fake/mega/stress PASS
- **Avanço:** software fiscal preparado; LIVE = BLOCKED_EXTERNAL

### NET10-27 — Superfície ativa 100% NET10 (atual TFM)
- Solution limpa (Mobile/Maui quebrados removidos da `.sln`)
- API/Tools/orphans → net10; API Swagger compatível com Swashbuckle 6.5
- Scripts/installer defaults alinhados; Unit 194/194 + build Release PASS
- **Avanço:** TFM ativo fechado; promoção `main`/tag **não** executada
- Relatório: [`Docs/qa/PRIMOX-NET10-27-FULL-TFM-MIGRATION.md`](qa/PRIMOX-NET10-27-FULL-TFM-MIGRATION.md)

---

## Tabela consolidada — estado atual vs avanço histórico

| Área | Estado atual | Último avanço relevante |
|------|--------------|-------------------------|
| TFM | **100% net10** (solution) | NET10-27 |
| Unit | 194/194 | NET10-26 (+21 fiscais) |
| QaEngine / DeepQa | 43/43 · 6/6 | mantido NET10 |
| Calendar Dark | Healer presente | NET10-22/23 |
| NF-e | PARTIAL + Fake/Focus path | NET10-26 |
| NFC-e / NFS-e | SCAFFOLD + Fake | NET10-26 |
| DANFE | Informativo ≠ oficial | NET10-26 |
| Multiempresa fiscal | DB + testes | NET10-26 |
| WhatsApp wa.me | REAL | pré-NET10 |
| WhatsApp API | Abstração blocked | NET10-26 |
| 2FA login | Setup sim; login não | Truth Audit (ainda válido) |
| Code signing | BLOCKED_EXTERNAL | Commercial 07/09 |
| SaaS | Fora do escopo imediato | Roadmap desktop-first |

---

## Próximos avanços esperados (não executados)

1. Auditoria forense anti-superdeclaração do NET10-26  
2. Promoção humana controlada `migration/net10` → `main` (quando desejado)  
3. Homologação Focus live (credencial real)  
4. NFC-e/NFS-e com contrato oficial  
5. Assinatura Authenticode  
6. DVI / inspeção visual (após fiscal estável)

---

## Índice de documentos por papel

| Papel | Documento |
|-------|-----------|
| Verdade atual | `Docs/CURRENT-TRUTH.md` |
| Fiscal detalhado | `Docs/qa/PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md` |
| TFM 100% ativo | `Docs/qa/PRIMOX-NET10-27-FULL-TFM-MIGRATION.md` |
| Roadmap produto | `PrimoAutoEletrica/Docs/ROADMAP_PRODUTO_VENDAVEL.md` |
| Roadmap estratégico | `Docs/qa/PRIMOX-100-PERCENT-ROADMAP.md` |
| Tracker | `Docs/PRIMOX-PROJECT-TRACKER.md` |
| Status executivo | `PROJECT_STATUS.md` |
| Esta crônica | `Docs/PRIMOX-ADVANCES-CHRONICLE.md` |
