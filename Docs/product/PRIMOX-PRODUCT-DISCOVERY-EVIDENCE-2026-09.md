# PRIMOX Product Discovery — Evidence Pack

**Data:** 13/09/2026  
**Branch:** `audit/product-discovery-2026-09`  
**Método:** forense Git + inventário estático + build/unit CURRENT + pesquisa web com URLs + exploração de código (agente read-only).

---

## 1. Git baseline (CURRENT)

```
branch: audit/product-discovery-2026-09
HEAD:   68076a67cbe253408db7dece8f79b64ede3ad81b
main:   29b19b16d0e6e3413bdba20c505e20c992596c24
v1.0.0: 72d85fa20f6102f694534e4e37b4ff03c8223529
primox-net6-final: 63aeb05ec31064cc732e86c959e82a91d541cd5e
remote: https://github.com/CamposCodingHub/PrimoAutoEletrica.git
```

### Commits recentes (pré-auditoria)

```
68076a6 feat: complete NET10-27 active surface TFM migration
876b811 docs: reconcile repository truth after NET10-26
1372e11 feat: complete fiscal foundation NET10-26
114c39d audit: fiscal completeness NET10-25
...
```

### Working tree do usuário (NÃO descartado; NÃO incluído no commit de docs)

Modified (amostra):  
`ServiceExtensions.cs`, `Funcionario.cs`, `FuncionarioRepository.cs`, `BusinessConfigurationService.cs`, `ContabilExportService.cs`, `DatabaseService*.cs`, `FinanceiroControl.*`, `FiscalOperationsControl.*`, `FuncionariosControl.*`, `OrcamentosControl.*`, `PDVControl.Produtos.xaml.cs`, `LoginViewModel.cs`, `ConfiguracoesSistemaWindow.*`

Untracked (amostra):  
`ServicoPadrao.cs`, `PixStaticPayloadBuilder.cs`, `ServicoPadraoService.cs`, `PixRecebimentoWindow.*`, `SelecionarServicoPadraoWindow.*`, `TwoFactorVerifyWindow.*`, `MarketHonestWinsTests.cs`

**Decisão de branch:** criada `audit/product-discovery-2026-09` a partir de `migration/net10` HEAD, preservando dirty tree. Commit desta auditoria = **somente** `Docs/product/*`.

---

## 2. Ambiente / TFM / versão

| Item | Valor | Evidência |
|------|-------|-----------|
| SDK | 10.0.302 | `dotnet --info` |
| Runtime Desktop | 10.0.10 | idem |
| TFM | net10.0-windows | `PrimoAutoEletrica.csproj` |
| AssemblyVersion | 1.0.0.0 | `Properties/AssemblyInfo.cs` |
| OS host | Windows 10.0.26200 | user_info |

---

## 3. Contagens de inventário (CURRENT)

Comando PowerShell (13/09/2026): contagem de arquivos sob repo (excluindo obj/bin onde aplicável).

| Artefato | Contagem |
|----------|----------|
| csproj | 30 |
| *Window.xaml | 50 |
| UserControls *.xaml | 28 |
| ViewModels *.cs | 22 |
| Services *.cs | 185 |
| Repositories | 13 |
| Models | 43 |
| Docs files | 184 |
| Scripts | 161 |
| *Tests.cs | 43 |

---

## 4. Build / Unit CURRENT

### Build Release

```
dotnet build PrimoAutoEletrica\PrimoAutoEletrica.csproj -c Release
BUILD_EXIT=0
Tempo ~25s
0 Erro(s)
```

### Unit tests

```
dotnet test Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj -c Release
Aprovado: 197 / Total: 197 / Falha: 0
TEST_EXIT=0
```

**Nota CURRENT vs HISTORICAL:** `Docs/CURRENT-TRUTH.md` banner citava 194 — **CURRENT = 197** (inclui testes MarketHonestWins no working tree compilado).

---

## 5. QaEngine / DeepQa

### Histórico (NÃO CURRENT)

Docs QA citam QaEngine 43/43 e DeepQa 6/6 (NET10-26 era) — **HISTORICAL**.

### CURRENT desta auditoria

QaEngine (13/09/2026):

```
Scripts\Run-UiSmoke.ps1 -SmokeFilter QaEngine
Status: APROVADO
ExitCode: 0
TotalChecks: 43
PassedChecks: 43
FailedChecks: 0
Output: TestResults/UiSmoke/product-discovery-20260913/ui-smoke-summary.json
```

DeepQa (13/09/2026):

```
Scripts\Run-UiSmoke.ps1 -SmokeFilter DeepQa
Status: APROVADO
ExitCode: 0
TotalChecks: 6
PassedChecks: 6
FailedChecks: 0
Output: TestResults/UiSmoke/product-discovery-deepqa-20260913/ui-smoke-summary.json
```

### Histórico (NÃO CURRENT)

Docs QA citam QaEngine 43/43 e DeepQa 6/6 (NET10-26 era) — **HISTORICAL**; CURRENT reconfirmou os mesmos números nesta data.

---

## 6. Arquivos / superfícies analisadas (amostra)

### Navegação
- `PrimoAutoEletrica/MainWindow.xaml` (+.cs)
- `PrimoAutoEletrica/Services/NavigationService.cs`

### Cliente / Veículo / OS
- `UserControls/ClientesControl.*`
- `Views/HistoricoClienteWindow.*`
- `UserControls/VeiculosControl.*`
- `Views/VisualizarVeiculoWindow.*`
- `UserControls/OrdensServicoControl.*`
- `Views/OrdemServicoWindow.*`
- `UserControls/OficinaKanbanControl.*`

### Fiscal / pagamentos / comunicação
- `Services/Fiscal/*` (Focus, Fake, ScaffoldNfse, ManualWhatsAppProvider)
- `UserControls/FiscalOperationsControl.*` (working tree: XML/DANFE/WhatsApp)
- `Services/NotificationService.cs`
- `Services/PixStaticPayloadBuilder.cs` (WT)
- `Services/FilialService.cs` (`MultiFilialDisponivel = false`)

### Órfãos / quick wins
- `Services/LicenseService.cs` + `Views/LicenseActivationWindow.*` (sem callers `new`)
- `Services/UpdateService.cs` + `Views/AtualizacaoWindow.*` (sem callers `new`)
- `Services/ContabilExportService.cs`
- `ViewModels/RelatoriosModernoViewModel.cs` (TODO OS)

### Segurança
- `Services/TwoFactorService.cs`
- `ViewModels/LoginViewModel.cs` (WT: gate 2FA)
- `Views/TwoFactorVerifyWindow.*` (WT)

### Persistência
- `Services/DatabaseService.cs`
- `Services/DatabaseService.Migrations.cs` (incl. ServicosPadrao migration WT)
- Domain services: Agendamento*, Orcamento*, Financeiro*

---

## 7. Tabelas (evidência estática)

Bootstrap + migrations + domain services — lista consolidada no inventário do agente explore (13/09/2026):  
Funcionarios, Clientes, Produtos, Veiculos, OrdensServico*, Orcamentos*, Agendamentos*, Vendas*, ContasPagar/Receber, FiscalOperations/Documents/Events/Empresas, CatalogoPecas*, Caixa*, ServicosPadrao (WT), ConfiguracoesSistema, AuditLogs, LoginTentativasSeguranca, etc.

`NotificacoesEnviadas`: INSERT em NotificationService — **create site UNKNOWN** nas migrations grep.

---

## 8. Fontes de mercado (URLs)

| Fonte | URL |
|-------|-----|
| Oficina Inteligente planos | https://oficinainteligente.com.br/planos |
| NBS | https://www.nbsi.com.br/ |
| NBS OS wiki | https://ajuda.nbsi.com.br:84/index.php/Solu%C3%A7%C3%B5es_NBS_-_Oficina_-_NBS_OS |
| Automanager BR | https://automanager.com.br/ |
| AutocenterPro | https://autocenterpro.com.br/software-para-oficina |
| Wüst | https://wustsoftware.com.br/software-gestao-oficina-mecanica |
| MecPro (marketing) | https://oficina.saas.magoweb.com.br/ |
| Shopmonkey DVI | https://www.shopmonkey.io/product/digital-vehicle-inspection |
| Shopmonkey auth | https://support.shopmonkey.io/hc/en-us/articles/38743372579988-Request-Customer-Authorization |
| Tekmetric auth | https://tours.tekmetric.com/answers/digital-signature-authorization-process-tekmetric |
| GaragePlug reviews | https://www.techjockey.com/reviews/garageplug-workshop-management |
| RAMP reviews | https://www.softwaresuggest.com/ramp-auto-care/reviews |

Claims de marketing de terceiros **não** foram verificados por trial.

---

## 9. Classificações usadas

IMPLEMENTED / IMPLEMENTED+TESTED / PARTIAL / SCAFFOLD / PLACEHOLDER / FAKE_ONLY / UI_ONLY / BACKEND_ONLY / BROKEN / DUPLICATED / NOT_IMPLEMENTED / BLOCKED_EXTERNAL / FUTURE / UNKNOWN  

Critério: UI+lógica+dados+fluxo+persistência (+teste quando aplicável). Botão/tabela/TODO sozinhos ≠ REAL.

---

## 10. Limitações

1. Sem observação presencial em oficina.  
2. Cliques/tempo são estimativas de fluxo de código.  
3. DeepQa overnight não rodado nesta sessão.  
4. QaEngine CURRENT: verificar summary JSON.  
5. Working tree features classificadas como CURRENT-disco, não como commit HEAD.  
6. Não foram usadas credenciais fiscais/WhatsApp/adquirente.

---

## 11. Relatório principal

`Docs/product/PRIMOX-PRODUCT-DISCOVERY-2026-09.md`
