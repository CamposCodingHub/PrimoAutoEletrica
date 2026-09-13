# PRIMOX Product Discovery Evidence — NET10-28

**Audit:** NET10-28 Product Master Forensic + Market Intelligence 2.0  
**Date:** 2026-09-13  
**Branch:** `audit/product-discovery-2026-09`  
**HEAD (pre-commit docs):** `a016fed58d91d74ebbc050939fa6bf677c6c6571`  
**Rule:** CURRENT ≠ HISTORICAL. Working tree product code at audit start: **clean vs HEAD**.

> Prior discovery docs (`PRIMOX-PRODUCT-DISCOVERY-2026-09.md`, `PRIMOX-PRODUCT-DISCOVERY-EVIDENCE-2026-09.md`) remain as HISTORICAL context. This file is the **NET10-28 evidence pack** (not a silent overwrite).

---

## 1. Identity forensic

| Item | Result |
|------|--------|
| TIMESTAMP | `2026-09-13T19:09:15-03:00` |
| BRANCH | `audit/product-discovery-2026-09` |
| HEAD | `a016fed58d91d74ebbc050939fa6bf677c6c6571` |
| main | `29b19b16d0e6e3413bdba20c505e20c992596c24` |
| v1.0.0 | `72d85fa20f6102f694534e4e37b4ff03c8223529` |
| primox-net6-final | `63aeb05ec31064cc732e86c959e82a91d541cd5e` |
| migration/net10 | `68076a67cbe253408db7dece8f79b64ede3ad81b` |
| remote | `origin https://github.com/CamposCodingHub/PrimoAutoEletrica.git` |
| TFM | `net10.0-windows` (`PrimoAutoEletrica.csproj`) |
| SDK | `10.0.302` |
| Assembly/product version | package refs only; product marketed as 1.0.0 in help/NotificationService comments |
| Configuration | Release builds executed |

Commands: `git rev-parse`, `git status -sb`, `git log -5`, `dotnet --version` — EXIT 0.

---

## 2. Git forensic — WT vs HEAD

At NET10-28 start and before docs commit: **no product code WT**.  
During audit: only new `Docs/product/*` untracked (+ optional `_net10-28-runlog.txt` — **exclude from commit**).

| Arquivo | HEAD | WT | Tipo | Pertence ao usuário? |
|---------|------|----|------|----------------------|
| Product `.cs`/`.xaml`/DB | tracked | unchanged | — | N/A (limpo) |
| Docs NET10-28 novos | absent | new | docs audit | Auditoria |
| `_net10-28-runlog.txt` | absent | temp | runlog | Auditoria (não commit) |
| Sessão anterior 2FA/PIX/tempos | absent disk+HEAD | absent | UNKNOWN/HISTORICAL | UNKNOWN (não descartado por NET10-28) |

---

## 3. Build / Tests CURRENT

| Suite | Result | Evidence |
|-------|--------|----------|
| Build Release | EXIT 0 · ~25s · 0 errors · ~125 warnings EST | `_net10-28-runlog` / terminal 157690 |
| Unit | **194/194** PASS · EXIT 0 | `Aprovado! … Aprovado: 194` |
| QaEngine | **43/43** APROVADO | `TestResults/UiSmoke/net10-28-qaengine/ui-smoke-summary.json` |
| DeepQa | **6/6** APROVADO | `TestResults/UiSmoke/net10-28-deepqa/ui-smoke-summary.json` |

**HISTORICAL note:** prior session claiming Unit 197 reflected WT features not present on this HEAD. CURRENT Unit = **194**.

**Build after audit:** product code unchanged (docs only) → same baseline expected. Re-verify post-commit with `git diff HEAD~1 --stat` docs-only.

---

## 4. Inventory counts (filesystem, 2026-09-13)

| Artifact | Count |
|----------|------:|
| csproj | 30 |
| XAML | 104 |
| *Window.xaml | 47 |
| UserControls *Control.xaml | 28 |
| *ViewModel.cs | 21 |
| Services (*Service*) | 141 |
| *Repository* | 14 |
| Models *.cs | 42 |
| Test csproj | 5 |
| Test *.cs | 56 |
| Docs *.md | 188+ |
| Scripts | 295 |

---

## 5. Critical code evidence (line-cited)

### G001 ContasReceber TEXT match

```63:76:PrimoAutoEletrica/Services/FinanceiroDatabaseService.cs
                CREATE TABLE IF NOT EXISTS ContasReceber (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Cliente TEXT NOT NULL,
                    ...
```

```314:365:PrimoAutoEletrica/Views/HistoricoClienteWindow.xaml.cs
        private ObservableCollection<PagamentoCliente> CarregarPagamentosFinanceiros()
        {
            ...
                var nomeCliente = Convert.ToString(LerValorPropriedade(conta, "Cliente"), ...);
                if (!ClienteCorresponde(nomeCliente))
            ...
            return clienteAtual == clienteFinanceiro ||
                   clienteAtual.Contains(clienteFinanceiro, ...) ||
                   clienteFinanceiro.Contains(clienteAtual, ...);
```

Export also selects `Cliente` string — no ClienteId column:

```221:227:PrimoAutoEletrica/Services/ContabilExportService.cs
                SELECT 
                    Id, Descricao, Cliente, Valor, DataVencimento, 
                    ...
                FROM ContasReceber 
```

**Class:** TEXT_MATCH · integrity risk for “quanto este cliente gerou / deve”.

### OS strong ID

`OrdensServico.ObterPorClienteId` used in Historico — STRONG_ID.

### Orçamento → OS

`OrcamentosControl` → `_viewModel.ConverterEmOrdemServico(orcamento)` — REAL (user-triggered).

### Notification / pós-venda

```7:34:PrimoAutoEletrica/Services/NotificationService.cs
    /// Envio automático NÃO está configurado no PRIMOX 1.0.0.
    /// WhatsApp real do produto = abertura manual via link wa.me nas telas.
        public Task<bool> EnviarWhatsAppAsync(...)
            => RegistrarNaoDisponivelAsync(...); // always false
```

`EnviarOsConcluidaAsync` exists but routes to stub — **AUSENTE auto**.

### Multi-filial

`FilialService.MultiFilialDisponivel = false` — FUTURE / not sellable.

### Auto Elétrica roteiros

`AutoEletricaTecnicaService.ObterRoteirosDiagnostico()` returns `new List<DiagnosticoGuiadoRoteiro> { Roteiro("D01", ...` — **hardcoded** in memory.

### Relatórios stub

`RelatoriosModernoViewModel.cs` lines ~320, ~356: `// TODO: Implementar quando métodos forem adicionados`.

### Fiscal / WhatsApp providers

- `ManualWhatsAppProvider` → wa.me only (`FiscalSecurityAbstractionsions.cs`).
- `FakeFiscalProvider` / Focus NotImplemented paths / `PlugNotasProvider` NotImplemented.
- `NFeEmissaoService.EmissionStatus = "NotImplemented"` when Focus HTTP off.

### 2FA

`TwoFactorSetupWindow` + `TwoFactorService` exist; **LoginViewModel has no TwoFactor match** → gate login **NOT wired** on HEAD (PARTIAL / BACKEND_ONLY setup).

### Comm

wa.me links in OrcamentosControl, OrdensServicoControl, ClientesControl, etc. — **manual**.

---

## 6. Module map (summary)

| Módulo | Entry | View | VM | Service | DB | Teste | Estado |
|--------|-------|------|----|---------|----|-------|--------|
| Clientes | Sidebar | ClientesControl | ClientesViewModel | Repos/DB | Clientes | Qa | IMPLEMENTED+TESTED |
| Veículos | Sidebar | VeiculosControl | — | Repos | Veiculos | Qa | IMPLEMENTED |
| OS | Sidebar | OrdensServicoControl | — | Repos | OrdensServico | Qa | IMPLEMENTED+TESTED |
| Orçamentos | Sidebar | OrcamentosControl | — | OrcamentoDB | Orcamentos | Qa | IMPLEMENTED+TESTED |
| Agenda | Sidebar | AgendamentosControl | AgendamentosViewModel | AgendamentoDB | Agendamentos | Qa | IMPLEMENTED |
| Kanban | Sidebar | OficinaKanbanControl | — | OS | OS status | Qa | IMPLEMENTED |
| Estoque | Sidebar | EstoqueControl | — | — | Produtos | Qa | IMPLEMENTED |
| Financeiro | Sidebar | FinanceiroControl | Financeiro VM | FinanceiroDB | Contas* | Qa | PARTIAL (TEXT cliente) |
| PDV | Sidebar | PDVControl | — | Venda | Vendas | Qa | IMPLEMENTED |
| Fiscal Ops | Sidebar | FiscalOperationsControl | — | Fiscal* | FiscalOps | Partial | PARTIAL / BLOCKED live |
| Auto Elétrica | Sidebar | AutoEletricaTecnicaControl | — | AutoEletricaTecnicaService | mostly none | Partial | PARTIAL hardcoded |
| Histórico 360 | From cliente | HistoricoClienteWindow | — | multi | multi | Partial | PARTIAL |
| Relatórios | Sidebar | Relatorios* | RelatoriosModernoVM | RelatorioDB | — | Partial | PARTIAL + TODO |
| License/Update | — | *Window | — | — | — | — | BACKEND_ONLY / orphan UI |
| Maui | — | stub | — | — | — | — | SCAFFOLD |

---

## 7. 360 capability matrix

| Capacidade | Cliente | Veículo | OS |
|------------|---------|---------|-----|
| ID único | EXISTE | EXISTE | EXISTE |
| OS link | STRONG | STRONG | hub |
| Vendas | STRONG/WEAK nome fallback | via cliente | PARTIAL |
| Contas | TEXT_MATCH | — | ReferenciaExterna TEXT |
| Orçamentos | STRONG | WEAK | STRONG convert |
| Agendamentos | STRONG | — | convert |
| Fiscal emit | via Venda path | — | AUSENTE path |
| Fotos/docs | PARTIAL paths | PARTIAL | FotosAntes/Depois |
| DVI | NÃO | NÃO | NÃO |
| Receita acumulada | POSSÍVEL OS+Venda | POSSÍVEL OS | — |
| Dívida confiável | IMPOSSÍVEL até G001 | — | — |
| Pós-auto | AUSENTE | AUSENTE | AUSENTE |

---

## 8. Market sources (consulted 2026-09-13)

| Source | URL | Type | Use |
|--------|-----|------|-----|
| Oficina Inteligente planos | https://oficinainteligente.com.br/planos | MARKETING/OFFICIAL | OS, aprovação WhatsApp/QR, pátio, fiscal CLAIM |
| Oficina Inteligente home | https://oficinainteligente.com.br/ | MARKETING | SaaS oficina |
| OI NF | https://oficinainteligente.com.br/emissao-nota-fiscal-oficina-automotiva/ | MARKETING | OS→NF CLAIM |
| NBS | https://www.nbsi.com.br/ | OFFICIAL | DMS concessionárias |
| NBS OS wiki | https://ajuda.nbsi.com.br:84/index.php/Soluções_NBS_-_Oficina_-_NBS_OS | DOCUMENTATION | OS/orçamento/garantia/agenda CLAIM |
| Automanager | https://automanager.com.br/sobre/ | MARKETING | ERP serviços nuvem |
| Wüst | https://wustsoftware.com.br/ | MARKETING | OS, NFS-e, estoque CLAIM |
| AutocenterPro | https://autocenterpro.com.br/ | MARKETING | ERP autocenter, WA, fiscal CLAIM |
| AutocenterPro oficina | https://autocenterpro.com.br/sistema-para-oficina | MARKETING | boxes, comissões CLAIM |
| Shopmonkey DVI | https://www.shopmonkey.io/product/digital-vehicle-inspection | MARKETING | DVI CLAIM |
| Shopmonkey support inspections | https://support.shopmonkey.io/hc/en-us/articles/38743049848852-Perform-Inspections | DOCUMENTATION | DVI item/photo/video |
| Shopmonkey authorization | https://support.shopmonkey.io/hc/en-us/articles/38743372579988-Request-Customer-Authorization | DOCUMENTATION | approval |
| Tekmetric DVI | https://www.tekmetric.com/digital-vehicle-inspection | MARKETING | DVI |
| Tekmetric auth support | https://support.tekmetric.com/hc/en-us/articles/4421125978391-Digital-Signature-Authorization-Process | DOCUMENTATION | digital auth |
| Fullbay | https://www.fullbay.com/ | MARKETING | heavy-duty |
| GaragePlug | (search CLAIM) | MARKETING | UNKNOWN depth |

**All competitor capabilities in matrices = CLAIM/DOCUMENTATION unless trial executed (none).**

---

## 9. Anti-false-completion checklist

1. No features implemented — YES  
2. User WT not discarded — YES (was clean; preserved)  
3. main unchanged — YES (`29b19b1…`)  
4. tags unchanged — YES  
5. Commit Docs/product only — (verified at commit)  
6–9. BUILD/Unit/Qa/DeepQa CURRENT stamped above  
10. Historical Unit 197 not used as CURRENT — YES (194)  
11. Market claims sourced — YES  
12. No invented capabilities — YES  
13. Gaps in Master Gap Matrix G001–G036 — YES  
14–15. 360 technical spec sufficient for NET10-29 — YES (`PRIMOX-360-TECHNICAL-SPEC-2026-09.md`)

---

## 10. Related deliverables

- `PRIMOX-PRODUCT-MASTER-GAP-2026-09.md`
- `PRIMOX-MARKET-BENCHMARK-2026-09.md`
- `PRIMOX-DIFFERENTIATION-STRATEGY-2026-09.md`
- `PRIMOX-WORKFLOW-MASTER-2026-09.md`
- `PRIMOX-360-TECHNICAL-SPEC-2026-09.md`
- `PRIMOX-PRODUCT-ROADMAP-2026-09.md`
- `PRIMOX-IMPLEMENTATION-ORDER-2026-09.md`
