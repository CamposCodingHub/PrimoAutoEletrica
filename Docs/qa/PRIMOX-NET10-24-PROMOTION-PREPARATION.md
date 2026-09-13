# PRIMOX NET10-24 — Controlled Promotion Preparation

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

**Data/hora:** 2026-09-13 13:33–13:56 (America/Sao_Paulo)  
**Fase:** Preparação formal de promoção (sem merge/push/tag)  
**Verdict:** **READY FOR HUMAN PROMOTION WITH EXTERNAL LIMITATIONS**

---

## Proteção absoluta (GATE 00 / 25)

| Ref | Valor | Esperado | Status |
|-----|-------|----------|--------|
| BRANCH | `migration/net10` | migration/net10 | PASS |
| HEAD (início) | `31c6877` | doc tip | PASS |
| MAIN | `29b19b1` | 29b19b1 | PASS |
| v1.0.0 | `a4ad6fe` | a4ad6fe | PASS |
| primox-net6-final | `29b19b1` | 29b19b1 | PASS |

MERGE=NO · PUSH=NO · TAG=NO

---

## Identidade (GATE 01)

| Item | Valor |
|------|-------|
| merge-base(main,HEAD) | `29b19b1` |
| main...HEAD left-right | `0 30` (0 só main · 30 só NET10) |
| Commits exclusivos NET10 | 30 |
| Docs | maioria `docs(net10)` / qa docs |
| Funcional/migração | `dce6d27` feat TFM · `cccbb43`/`ec4b8d8` hardening · Calendar/Clipboard · csproj packages |
| Infra/QA/scripts | Run-UiSmoke TFM · Assurance/Commercial scripts · SecurityRedTeam |
| Segurança | A12/A13 harness + RedTeam path fix |

Histórico **não** reescrito.

---

## Diff main...HEAD (GATE 02)

- **63 files** · **+2681 / −46**
- Predominância: `Docs/qa/PRIMOX-NET10-*.md` + `PROJECT_STATUS.md`
- Produto: `PrimoAutoEletrica.csproj` (TFM/deps) · `CalendarContrastHealer` · `ClipboardHelper` · Theme/Calendar · smoke helpers · App.xaml.cs hook
- Scripts: TFM dinâmico
- Tools: `Net10CompatProbe`
- Testes/csproj TFM net10

### Classificação

| Classe | Exemplos |
|--------|----------|
| NET10 migration | csproj TFM, package bumps |
| compatibility | scripts TFM, smoke helpers |
| bug fix | clipboard, localized tabs, SQLitePCLRaw |
| security | RedTeam paths (sem mudança de superfície de ameaça nova) |
| UI | Calendar Dark healer, Calendar.xaml |
| installer/scripts | Build/Deploy/New-WindowsInstaller/Test-Installed |
| tests/QA | UiTests/Tests TFM, smoke filters |
| documentation | NET10-00…23 + desktop deploy |
| unrelated/suspicious | **nenhum** após revisão do name-status |

---

## Regressão funcional (GATE 03)

`NavigationService` module map **idêntico** main ↔ HEAD (Dashboard…Ajuda/Help, Fiscal, PDV, etc.).

| Capacidade | NET6 | NET10 | Resultado |
|------------|------|-------|-----------|
| Login | PRESENTE | PRESENTE | PRESERVADA (Journey+Desktop) |
| Dashboard | PRESENTE | PRESENTE | PRESERVADA |
| Centro de Operações / CommandCenter | PRESENTE | PRESENTE | PRESERVADA (mapa/shell; sem remoção) |
| Clientes | PRESENTE | PRESENTE | PRESERVADA |
| Veículos | PRESENTE | PRESENTE | PRESERVADA |
| OS | PRESENTE | PRESENTE | PRESERVADA |
| Agenda | PRESENTE | PRESENTE | PRESERVADA |
| Estoque | PRESENTE | PRESENTE | PRESERVADA |
| Financeiro | PRESENTE | PRESENTE | PRESERVADA |
| Relatórios | PRESENTE | PRESENTE | PRESERVADA |
| Configurações | PRESENTE | PRESENTE | PRESERVADA |
| Funcionários | PRESENTE | PRESENTE | PRESERVADA (mapa) |
| Orçamentos | PRESENTE | PRESENTE | PRESERVADA (mapa) |
| PDV | PRESENTE | PRESENTE | PRESERVADA (mapa+QaEngine) |
| Produtos/Catálogo/Auto Elétrica/Ajuda | PRESENTE | PRESENTE | PRESERVADA (mapa) |
| Tema Light/Dark | PRESENTE | PRESENTE | PRESERVADA (Tema smoke) |
| Busca/componentes/notificações | PRESENTE | PRESENTE | PRESERVADA (Components+QaEngine) |
| Persistência | PRESENTE | PRESENTE | PRESERVADA (DB+Config) |

LiveCharts: **removido** do csproj; uso em produto = 0 → sem regressão de gráficos (ItemsControl).

---

## TFM (GATE 04)

| Projeto ACTIVE produto | TFM |
|------------------------|-----|
| `PrimoAutoEletrica/PrimoAutoEletrica.csproj` | **net10.0-windows** |

- ACTIVE `net6.0-windows` no produto = **0**
- Testes ACTIVE: `Tests/...` net10.0-windows · UiTests/Commercial08DataSeed net10.0
- Satélites net9 (Api/Simulation/DbConfigurator legados): **não** são o desktop comercial ACTIVE
- Menções net6 em docs históricos / probe temporário em script E2E: classificação documental/tooling

---

## Dependências (GATE 05)

- `dotnet restore` Exit=0  
- `dotnet list package` Exit=0  
- **NU1701 = 0** (sem matches)  
- NU1605 / NU1900–1905 = 0 nesta varredura  
- NU1510 (prune hints) informativo  
- OpenTK/LiveCharts = **0** no publish

---

## Build (GATE 06)

| Config | Exit | Tempo |
|--------|------|-------|
| Debug | **0** | ~24.6 s |
| Release | **0** | ~16.5 s |

0 errors.

---

## Testes (GATE 07)

| Suite | Resultado | Evidência |
|-------|-----------|-----------|
| Unit | **173/173** Exit=0 | `gate07-unit.log` 13:35 |
| QaEngine | **43/43** APROVADO | `smoke-QaEngine` 13:41 |
| DeepQa | **6/6** APROVADO | `smoke-DeepQa` 13:43 |
| Journey | **fails=0** (12 módulos) | `gate07-journey.json` |

---

## Banco (GATE 08)

| Check | Resultado |
|-------|-----------|
| A13Database | PASS 1/1 (isolado) |
| Configurações backup/restore | PASS 2/2 |
| Banco comercial usuário | **não usado** |

---

## Segurança (GATE 09)

| Gate | Resultado |
|------|-----------|
| A12Security | **3/3 PASS** (retry filtro correto) |
| A13Security | **1/1 PASS** |
| RedTeam | Critical=**0** High=**0** (YELLOW informativo) |

Nota: filtros `Assurance12`/`Assurance13` sozinhos não disparam checks (harness); aliases `A12Security`/`A13Security` são os canônicos.

---

## Publish (GATE 10)

| Campo | Valor |
|-------|-------|
| Comando | `dotnet publish -c Release -r win-x64 --self-contained true` |
| Exit | 0 |
| Files | 473 |
| EXE SHA256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| OpenTK/LiveCharts | 0 |
| PDB | **1** (raw publish) — PackagingE2E exclui PDBs |
| Startup | PASS |

**LIMITATION:** PDB≠0 no publish cru; pacote installer E2E cumpre exclusão.

---

## Installer + reinstall (GATE 11–12)

| Campo | Valor |
|-------|-------|
| Nome | `PRIMOX-Workshop-Setup-1.1.0-PackagingE2E.exe` |
| SHA256 | `6FE6C6FE9890358F297EB5F342D685528849D226B3D842A8AC8DF425949142AB` |
| Size | 54 196 380 |
| Cycles 1–3 | Install/Start/Smoke/Uninstall **PASS** |
| Data preservation | **PASS** |
| fails | **0** |
| Comercial 1.0.0 SHA | `9A08494D…A9C5` **INTACTO** |
| FileVersion no pacote | ainda **1.0.0** (AssemblyInfo) |

---

## Desktop (GATE 13)

| Campo | Valor |
|-------|-------|
| Path | `C:\Program Files\PRIMOX\Workshop` |
| TFM | net10.0 |
| EXE SHA | `05D103E9…775B` |
| Smoke | Login/PreCheck/Tema/Clientes/Dashboard **PASS** |
| Calendar QaVisualDarkLight | **FAIL ACL** gravar `Program Files\...\Logs\qa-visual` |

Funcional Calendar (interação/tema) PASS nos checks 1–3; captura PNG = LIMITATION ambiental.

---

## Visual (GATE 14)

| Item | Resultado |
|------|-----------|
| Journey Tema | PASS |
| Journey Calendar (incl. QaVisualDarkLight em app-data gravável) | **4/4 PASS** |
| Components | PASS |
| Desktop modules + Light/Dark via Tema | PASS |

---

## Performance (GATE 15)

| Métrica | Valor |
|---------|-------|
| n | 10 |
| min/max/avg ms | 1532 / 1590 / **1556.6** |
| fails | 0 |
| NET6_COMPARE | **BLOCKED_EXTERNAL** |

Método: processo Responding ≥1.5s (não compara cold UI completo com NET6).

---

## Fiscal (GATE 16)

| Item | Resultado |
|------|-----------|
| Fake/unit `Fiscal\|FakeFiscal\|Homolog` | **46/46 PASS** |
| LIVE | **BLOCKED_EXTERNAL** (sem `PRIMOX_FOCUS_HOMOLOG_TOKEN`) |

---

## Signing (GATE 17)

| Item | Resultado |
|------|-----------|
| SignTool no PATH | False |
| Cert code-signing comercial | ausente |
| Status | **BLOCKED_EXTERNAL** |

---

## Versão 1.1.0 (GATE 18)

| Campo | Valor atual | Alvo |
|-------|-------------|------|
| AssemblyVersion | 1.0.0.0 | 1.1.0.0 |
| FileVersion | 1.0.0.0 | 1.1.0.0 |
| InformationalVersion | 1.0.0 | 1.1.0 |

**VERSION_PREPARATION_REQUIRED** — não alterado nesta fase (protege linha comercial até aprovação humana).

---

## Artefatos docs (GATE 19–22)

| Doc | Status |
|-----|--------|
| `Docs/release/PRIMOX-Workshop-1.1.0-NET10.md` | CREATED |
| `Docs/release/RELEASE-NOTES-1.1.0.md` | CREATED |
| `Docs/release/PRIMOX-NET10-PROMOTION-PR-BODY.md` | CREATED |
| PR GitHub real | **PR_CREATION_NOT_EXECUTED** (`gh auth` ausente) |

---

## Rollback lógico (GATE 24)

Sem alterar refs: abandonar PR / não mergear deixa `main`/`v1.0.0`/`primox-net6-final` inalterados. **Reversível por não-ação.**

---

## Decisão

**READY FOR HUMAN PROMOTION WITH EXTERNAL LIMITATIONS**

### TECHNICAL LIMITATIONS

1. AssemblyInfo ainda 1.0.0 (`VERSION_PREPARATION_REQUIRED`)
2. Publish cru PDB=1 (installer E2E sem PDB)
3. Desktop Calendar captura visual ACL em Program Files

### EXTERNAL LIMITATIONS

1. Fiscal LIVE  
2. Code Signing  
3. NET6 SxS compare  

### CRITICAL BLOCKERS

**Nenhum** Critical/High de segurança · **nenhuma** regressão funcional demonstrada que bloqueie promoção humana.

---

## Evidência

`TestResults/Net10-Overnight/20260913/NET10-24-Promotion-Prep/`

## Git protection final

main=`29b19b1` · v1.0.0=`a4ad6fe` · primox-net6-final=`29b19b1`  
MERGE=NO · PUSH=NO · TAG=NO

### Commits desta preparação

| SHA | Mensagem |
|-----|----------|
| `9af0f8f` | `docs: prepare net10 1.1.0 promotion` |
| `0cd807f` | `docs: add net10 1.1.0 release candidate notes and PR body` |

**HEAD final:** `0cd807f`
