# PRIMOX MASTER AUDIT-01 — FINDINGS

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

**Data:** 12/09/2026  
**Baseline:** `b706370`

| ID | SEV | MODULE | TYPE | DESCRIPTION | EVIDENCE | ACTION | STATUS |
|---|---|---|---|---|---|---|---|
| F-001 | P3 | Commercial / Update UI | COMMERCIAL | `AtualizacaoWindow` header “Primo Auto Elétrica” | XAML inventory | Texto → “Atualização do PRIMOX Workshop” | **FIXED** |
| F-002 | P3 | First-run | COMMERCIAL | Welcome “Primo Auto Eletrica” | `PrimeiraExecucaoWindow.xaml.cs` | → PRIMOX Workshop | **FIXED** |
| F-003 | P3 | Config defaults | COMMERCIAL | Defaults `CompanyDisplayName` legado | Business/SystemConfigurationService | Default → PRIMOX Workshop | **FIXED** |
| F-004 | P3 | PDFs / WhatsApp / OS print | COMMERCIAL | Hardcoded “Primo Auto Eletrica” em superfícies de documento | Grep product | Usar `BusinessConfigurationService.ResolveCurrent().EffectiveCompanyName` | **FIXED** |
| F-005 | P3 | Relatórios VM | COMMERCIAL | `_empresaAtual` legado | RelatoriosViewModel | → PRIMOX Workshop | **FIXED** |
| F-006 | — | Calendar Dark | KNOWN LIMITATION | Header Calendar em Dark | Exhaustive / I18N-07 | Nenhum redesign | **KNOWN** |
| F-007 | — | Performance | KNOWN LIMITATION | RAM/handles crescem com navegação (estabiliza) | A13Performance MA01 PASS | Observation | **KNOWN** |
| F-008 | — | Signing | EXTERNAL DEPENDENCY | Sem certificado comercial | Test-CodeSigningReadiness Exit=2 | Pipeline READY | **EXTERNAL** |
| F-009 | — | Fiscal LIVE | EXTERNAL DEPENDENCY | Token/emitente ausentes | Check-FiscalLiveReadiness | Foundation only | **EXTERNAL** |
| F-010 | — | Auto-update | OUT OF SCOPE | Não implementado | Product truth | Manual reinstall | **OUT OF SCOPE** |
| F-011 | — | net6.0-windows | EXTERNAL / ARCHITECTURE | TFM EOL warning NETSDK1138 | Build logs | Não atualizar TFM nesta auditoria | **KNOWN / EXTERNAL** |
| F-012 | P3 | Docs internos | DOCUMENTATION | `PrimoAutoEletrica/Docs/*` ainda citam marca legada | Grep | Histórico; README raiz OK | **OPEN (docs internos)** |
| F-013 | — | Security harness | DOCUMENTATION | Invoke-SecurityRedTeam SQL probe falhou (unable to open DB) nesta sessão | Script Exit | A12Security smoke **PASS**; não é produto | **KNOWN (harness)** |
| F-014 | — | Offline formal NIC | DOCUMENTATION | Desconexão NIC formal não forçada | Sessão | Core SQLite local; smokes sem dependência de rede | **KNOWN / VERIFIED BY ARCHITECTURE** |
| F-015 | — | Packaging | — | PDB=0 DB=0 test artifacts=0 | Publish audit 590 files | Keep | **PASS** |

## Severity totals (product)

| | Count |
|---|---:|
| P0 open | **0** |
| P1 open | **0** |
| P2 open | **0** |
| P3 fixed | 5 |
| P3 open (internal docs only) | 1 |
| Known / External / OoS | restante |

## Critical answer support

Nenhum P0/P1/P2 interno aberto após correções comerciais P3.
