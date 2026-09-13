# PRIMOX Workshop 1.1.0 — NET10 (candidato)

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

**Status:** CANDIDATO — não lançado · tag `v1.1.0` **não criada**  
**Branch fonte:** `migration/net10`  
**Baseline protegida:** `main` / `v1.0.0` / `primox-net6-final` intactos

## Resumo

Migração do produto desktop WPF de **.NET 6 (`net6.0-windows`)** para **.NET 10 (`net10.0-windows`)**, com hardening visual (Calendar Dark), limpeza de dependências (remoção LiveCharts/OpenTK), alinhamento de scripts/QA ao TFM do csproj, e validação de publish/installer PackagingE2E.

## Inclui

- Migração TFM produto → `net10.0-windows`
- Compatibilidade de pacotes (Sqlite 9.0.9, SQLitePCLRaw 3.0.5, Extensions 9.0.9)
- Remoção de LiveChartsCore (não usado; gráficos via ItemsControl) → NU1701=0
- `CalendarContrastHealer` + ajustes de tema Calendar Dark
- `ClipboardHelper` (retry sob carga / ExhaustiveUi)
- Scripts/QA lendo TFM do csproj
- Validação de publish self-contained win-x64
- Installer PackagingE2E experimental `1.1.0` (AppId isolado; **não** substitui comercial 1.0.0)
- Bateria QA: Unit, QaEngine, DeepQa, Journey, A12/A13, RedTeam, DB isolado

## Não inclui / limitações externas

- **Fiscal LIVE** — requer `PRIMOX_FOCUS_HOMOLOG_TOKEN` (BLOCKED_EXTERNAL)
- **Code signing** comercial — thumbprint/certificado ausente (BLOCKED_EXTERNAL)
- **Comparação SxS .NET 6** no host — SDK/EXE net6 ausente (BLOCKED_EXTERNAL)
- **Numeração AssemblyInfo** ainda `1.0.0` até aprovação humana (`VERSION_PREPARATION_REQUIRED`)

## Rollback conceitual

Enquanto não houver merge: `main` permanece NET6 (`29b19b1`); `v1.0.0` (`a4ad6fe`) e `primox-net6-final` permanecem válidos.
