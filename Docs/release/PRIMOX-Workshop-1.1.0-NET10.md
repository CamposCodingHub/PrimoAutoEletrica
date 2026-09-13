# PRIMOX Workshop 1.1.0 — NET10 (candidato)

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
