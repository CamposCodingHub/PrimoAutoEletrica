# PRIMOX-I18N-07 — RESIDUALS

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

## Antes → Depois

| Item | I18N-06 | I18N-07 |
|------|--------:|--------:|
| TRANSLATION_REQUIRED | 337 | **322** |
| UNKNOWN | 1348 | **1345** |
| EN strict PASS modules | 4/15 | **14/15** |
| ES strict PASS modules | 2/15 | **14/15** |

## P0 / P1

- **P0 user-visible operacional:** 0 nos critical flows auditados (EN/ES)
- **P1 critical flows:** 0 residual classificado como PT chrome nos módulos Clientes→Relatórios

## Remanescentes documentados (não-bloqueantes)

| Item | Motivo | Categoria | Impacto | Decisão |
|------|--------|-----------|---------|--------|
| Help Extended (guias longos PT) | Conteúdo extensivo secundário | P3_HELP | Usuário opera sem Help Extended | KEEP · exceção YELLOW |
| Audit filter tokens (`Sucesso`/`Falha`/`Funcionario`…) | Valores alinhados a códigos internos de filtro | TECHNICAL | Traduzir quebraria filtro | KEEP |
| UNKNOWN ~1345 attrs | Maioria não user-visible / sem classificador forte | UNKNOWN non-UV | Não mass-translate | KEEP |
| UNUSED catalog ~317 | Chaves órfãs históricas | INTERNAL | Não remover em massa | KEEP |
| DUPLICATE_CANDIDATE ~45 | Sem evidência de consolidação segura | INTERNAL | KEEP | KEEP |
| Native Calendar Dark Header | Limitação WPF | TECHNICAL | Sem impacto i18n | KEEP |
| Dados reais / placas / nomes | Domínio | DATA | Não traduzir | KEEP |
| Termos fiscais NCM/CFOP/… | Domínio BR | FISCAL | Não traduzir | KEEP |
| Brand PRIMOX | Marca | BRAND | Não traduzir | KEEP |

## Detector

- Cognatos ES (`Editar`/`Cancelar`/`Buscar`/`Sucesso`) não contam como residual PT em `es-ES`
- Exact TECHNICAL allowlist para filtros de auditoria
- Word-boundary evita falso positivo `Configura` ⊂ `Configuración`
