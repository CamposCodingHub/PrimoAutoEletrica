# PRIMOX Workshop (PrimoAutoEletrica)

Sistema de gestão para oficina / autoelétrica — **WPF desktop** (`net10.0-windows` na branch `migration/net10`).

**Status vivo:** ver [`Docs/CURRENT-TRUTH.md`](Docs/CURRENT-TRUTH.md) · [`PROJECT_STATUS.md`](PROJECT_STATUS.md)  
**Fiscal (atual):** [`Docs/qa/PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md`](Docs/qa/PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md) — fundação implementada/testada; emissão live = **BLOCKED_EXTERNAL**  
**Versão tag comercial:** `1.0.0` (`v1.0.0` → `72d85fa`, **não mover**)  
**Acompanhamento:** [`Docs/PRIMOX-PROJECT-TRACKER.md`](Docs/PRIMOX-PROJECT-TRACKER.md) · Roadmap: [`PrimoAutoEletrica/Docs/ROADMAP_PRODUTO_VENDAVEL.md`](PrimoAutoEletrica/Docs/ROADMAP_PRODUTO_VENDAVEL.md)

> O site/marketing PRIMOX **não** faz parte deste repositório nesta fase.  
> Relatórios COMMERCIAL-*/MASTER-AUDIT-*/I18N-* / NET10-00…25 são **históricos** salvo indicação CURRENT.

---

## Visão geral

Aplicação desktop para operação diária de oficina:

- Clientes e veículos
- Ordens de serviço e dossiê técnico
- Orçamentos
- Agenda / check-in
- Estoque e fornecedores
- Financeiro
- PDV
- Relatórios (PDF/Excel)
- Kanban de oficina
- Importação NF-e + **fundação fiscal** (NF-e Focus homolog path + Fake; NFC-e/NFS-e scaffold)
- WhatsApp share manual (`wa.me`) — **não** é WhatsApp Business API
- Design System PRIMOX (Light/Dark; Calendar Dark tratado via `CalendarContrastHealer`)

---

## Requisitos

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (trabalho ativo em `migration/net10`)
- Tag `v1.0.0` / `main` históricos usavam `net6.0-windows` — **não** misturar TFMs sem ler `Docs/CURRENT-TRUTH.md`
- Visual Studio 2022 ou Cursor/VS Code

---

## Build

```powershell
dotnet build PrimoAutoEletrica/PrimoAutoEletrica.csproj -c Debug
dotnet build PrimoAutoEletrica/PrimoAutoEletrica.csproj -c Release
```

**Critério:** 0 erros.

---

## Execução

```powershell
dotnet run --project PrimoAutoEletrica/PrimoAutoEletrica.csproj -c Debug
```

Na primeira execução o SQLite é criado automaticamente (AppData / configuração da estação).

---

## Deploy / instalação

**Canal comercial oficial (Inno Setup):**

```powershell
# Pré-requisito: Inno Setup 6 (ISCC)
winget install JRSoftware.InnoSetup
.\Scripts\Build-PrimoXCommercialRelease.ps1 -Version 1.0.0
```

Saídas em `artifacts/` (não versionado).  
Validação E2E: `.\Scripts\Test-InstalledPackageE2E.ps1 -Version 1.0.0 -SkipQaEngine`  
Deploy dev NET10: `Scripts/Deploy-ToInstalledApp.ps1` · `Docs/qa/PRIMOX-NET10-DESKTOP-DEPLOY.md`

Code signing: **BLOCKED_EXTERNAL** sem certificado comercial (ver audit histórico Script 7).

---

## QA / testes automatizados

```powershell
dotnet test Tests/PrimoAutoEletrica.Tests/PrimoAutoEletrica.Tests.csproj -c Release
Scripts\Run-UiSmoke.ps1 -Configuration Release -SmokeFilter QaEngine
Scripts\Run-UiSmoke.ps1 -Configuration Release -SmokeFilter DeepQa
```

| Suite | Expectativa recente (NET10-26) |
| ----- | ------------------------------ |
| Unit | **194/194** |
| Fiscal filter | PASS (Fake + foundation) |
| QaEngine | **43/43** |
| DeepQa | **6/6** |

Evidências: `Docs/qa/` · `TestResults/`

---

## Arquitetura (resumo)

```
PrimoAutoEletrica/                 # WPF principal (net10.0-windows)
  Services/Fiscal/                # Fundação fiscal NET10-26
  Themes/ UserControls/ Views/
Scripts/ Docs/ Tests/
Docs/CURRENT-TRUTH.md             # Índice de verdade atual
PROJECT_STATUS.md
```

---

## Known issues / limitações (atuais)

1. Emissão fiscal **live** (homolog/produção) — **BLOCKED_EXTERNAL** (sem token/CNPJ/cert no ambiente)
2. DANFE gerado localmente = PDF **informativo**, não layout SEFAZ oficial
3. WhatsApp Business API — abstração pronta; envio real blocked; `wa.me` permanece
4. 2FA TOTP existe (setup), **não** é desafio obrigatório no login — reavaliar em audit dedicado
5. Multi-filial produto completo — fiscal DB multiempresa existe; isolamento comercial multi-oficina ainda futuro
6. Code signing comercial — BLOCKED_EXTERNAL
7. Cobertura 100% de todos os botões / “produto 100%” — **não** reivindicada

---

## Documentação

- [`Docs/CURRENT-TRUTH.md`](Docs/CURRENT-TRUTH.md) — **começar aqui**
- [`PROJECT_STATUS.md`](PROJECT_STATUS.md)
- [`Docs/qa/PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md`](Docs/qa/PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md)
- [`INSTALLATION.md`](INSTALLATION.md)

---

## Licença / uso

Uso interno / comercial do produto. Consulte o proprietário do repositório para licenciamento.
