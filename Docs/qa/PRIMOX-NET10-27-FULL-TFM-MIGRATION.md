# PRIMOX NET10-27 — Full TFM Migration (active surface)

**Data:** 2026-09-13  
**Branch:** `migration/net10`  
**Veredito:** **ACTIVE SURFACE 100% NET10 — COMPLETE WITH EXPLICIT OUT-OF-SCOPE**

---

## Objetivo

Fechar a migração **de todos os projetos e scripts ativos** para .NET 10, sem promover `main` / `v1.0.0` / `primox-net6-final`.

---

## O que “100% NET10” significa aqui

| Incluído | Não incluído |
|----------|--------------|
| Todos os `*.csproj` na solution `PrimoAutoEletrica.sln` | Promoção para `main` / tag comercial |
| Tools oficiais (`DbConfigurator`, `LocalSyncSimulator`) | Version bump comercial `1.1.0` |
| API (`PrimoAutoEletrica.Api` → `net10.0-windows`) | Code signing Authenticode |
| Tests oficiais + UiTests | Homologação fiscal live |
| Scripts de validação/installer alinhados a TFM net10 | Stub vazio `PrimoAutoEletrica.Maui` (fora da solution) |
| Orphans nested (`PrimoAutoEletrica\Api|Simulation|Tests`) | Docs históricos que citam net6/net9 como evidência de fase |

---

## Mudanças desta fase

1. **Solution** — removidos projetos quebrados `PrimoAutoEletrica.Mobile` / Maui da `.sln` (build deixava de carregar).
2. **TFM bumps** — API, Tools, orphans e root tests → `net10.0` / `net10.0-windows`.
3. **API** — `net10.0-windows` (necessário para ProjectReference ao WPF); JwtBearer 10; Swashbuckle 6.5; removidos `MapOpenApi` / `.WithOpenApi()` incompatíveis com o stack Swashbuckle; `SwaggerConfiguration` ajustada (`OrderActionsBy`, UI Filter).
4. **Scripts / ISS** — defaults e probes ativos alinhados a net10; comentário ISS atualizado.
5. **Docs vivos** — `ARCHITECTURE.md`, `Installer/README_INSTALADOR.md`, CURRENT-TRUTH / crônica / PROJECT_STATUS.

---

## Inventário TFM (fonte)

| Projeto | TFM | Na solution? |
|---------|-----|--------------|
| `PrimoAutoEletrica` (WPF) | `net10.0-windows` | SIM |
| `PrimoAutoEletrica.Api` | `net10.0-windows` | SIM |
| `Tests/PrimoAutoEletrica.Tests` | `net10.0-windows` | SIM |
| `PrimoAutoEletrica.UiTests` | `net10.0` | SIM |
| `Tools/DbConfigurator` | `net10.0` | SIM |
| `Tools/LocalSyncSimulator` | `net10.0` | SIM |
| Nested Api/Simulation/Tests | `net10.0(-windows)` | NÃO (orphan) |
| `Scripts/tools/Commercial08DataSeed` | `net10.0` | NÃO |
| `Tools/Net10CompatProbe` | `net10.0-windows` | NÃO |
| `PrimoAutoEletrica.Maui.App.csproj` | **vazio** | NÃO — OUT_OF_SCOPE |

Hits `net6.0` / `net9.0` restantes em `*.csproj` ativos: **0**.

---

## Evidência de gates

| Gate | Resultado |
|------|-----------|
| `dotnet build PrimoAutoEletrica.sln -c Release` | **PASS** (0 erros; avisos NU1510/NU1603/NU1701 FlaUI) |
| `dotnet test Tests\PrimoAutoEletrica.Tests -c Release` | **194/194 PASS** |
| Protected refs (`main` / `v1.0.0` / `primox-net6-final`) | **intactos** |
| Push / merge / tag | **NÃO executados** |

---

## Limitações explícitas

- **Assembly comercial** permanece `1.0.0` (promotion humana).
- **FlaUI** em UiTests: NU1701 (netfx packages) — conhecido, não bloqueia TFM.
- **Maui / Mobile:** stub vazio / ausente — fora do produto desktop.
- **Histórico documental** (COMMERCIAL-08, MASTER-AUDIT, etc.) continua citando net6/net9 na camada histórica — correto.

---

## Decisão

**ACTIVE SURFACE 100% NET10 — COMPLETE WITH EXPLICIT OUT-OF-SCOPE**

Próximo passo humano (não desta fase): promoção controlada / PR para `main` quando desejado, mantendo proteções de tag.
