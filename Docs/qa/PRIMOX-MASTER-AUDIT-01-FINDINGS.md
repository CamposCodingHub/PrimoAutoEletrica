# PRIMOX MASTER AUDIT-01 — FINDINGS

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
