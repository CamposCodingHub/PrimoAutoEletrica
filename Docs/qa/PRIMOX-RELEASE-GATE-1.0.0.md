# PRIMOX Workshop — Release Gate 1.0.0

**Versão analisada:** `1.0.0-rc.1`  
**HEAD analisado:** `b184501` (`b184501088270b91941fd1ef58128e5e43c9f07d`)  
**Branch:** `main`  
**Data:** 2026-09-08  

---

## Build

| Item | Valor |
|------|-------|
| Comando | `dotnet clean` + `restore` + `build` |
| Configuração | Debug |
| Framework | `net6.0-windows` |
| Duração | ~23.8 s |
| Erros | **0** |
| Warnings | ~130 (nullable/CA1416 pré-existentes) — **WARNING**, não bloqueadores |
| Resultado | **PASS** |

Assembly no build RC:
- AssemblyVersion: `1.0.0.0`
- InformationalVersion: `1.0.0-rc.1` (ProductVersion do exe Debug)

---

## QA regression

| Suite | Evidência | Resultado |
|-------|-----------|-----------|
| QaEngine | `TestResults/UiSmoke/2026-09-08_08-33-29` | **37/37 PASS** |
| Deep QA | `TestResults/UiSmoke/2026-09-08_08-40-28` | **6/6 PASS** |
| Long Run 5 | `QaEngine:FinalizationLongRun5Ciclos` (~110 s) | **PASS** |
| Long Run 3 | `QaEngine:LongRunOperacional` (~65 s) | **PASS** |
| Light/Dark | `QaEngine:TemaLightDarkRoundTrip` + DeepQa | **PASS** |
| Visual multi-res | `DeepQa:CapturasVisuaisLightDark` + `ResponsividadeResolucoes` | **PASS** |
| Startup / Shell / Dashboard / navegação | QaEngine + DeepQa (banco isolado) | **PASS** |

Nenhuma regressão vs Fase 14.

---

## Cobertura (fonte oficial Fase 14)

| Métrica | Valor | Classificação |
|---------|-------|---------------|
| Matriz | **537** linhas | inventário + auditoria |
| Módulos | **17/17** | VALIDADOS (navegação) |
| Windows | **51** | inventariadas; subset parameterless + CONDITIONAL |
| UserControls | **28** | inventariados |
| Click handlers | **392** | inventariados |
| Botões runtime | **277** | inventariados; subset seguro EXECUTADO |
| Execução real 100% de todos os botões | **NÃO** | não reivindicado |

Distinção mantida: INVENTARIADO ≠ EXECUTADO ≠ VALIDADO.

---

## Limitações revalidadas

| Item | Resultado Gate |
|------|----------------|
| Calendar Dark header | **KNOWN LIMITATION** (CalendarItem não alterado) |
| NF-e emissão real | **NOT TESTABLE** |
| Ícone de fase | **NOT FOUND** |
| Deploy smoke instalado | **CONDITIONAL**: atalho existe (`OneDrive\Desktop\Primo Auto Eletrica.lnk` → `AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe`), mas binário instalado ainda `ProductVersion=0.0.0.0` (não é o RC). Smoke completo da cópia instalada RC **não executado** (risco de dados reais). Startup RC via smoke isolado **PASS**. |
| FuncionariosViewModel | **ORPHAN CANDIDATE — RETAINED** (DI + UITests) |

---

## Dados / schema

| Item | Resultado |
|------|-----------|
| Schema alterado neste Gate | **Não** |
| Migrations novas | **Não** |
| Smoke DB | isolado `AutomatedTests/ui-smoke-test-*` |
| DB produção AppData | presente; **não tocado** pelo Gate |
| Corrupção / massa em produção | **não observada** (smokes isolados) |
| Resultado | **PASS** (integridade operacional do Gate) |

---

## Segurança operacional (sem pentest)

| Item | Resultado |
|------|-----------|
| Crash em fluxos smoke | nenhum |
| Connection string com Password hardcoded (scan rápido) | nenhum match óbvio no escopo anterior |
| Pentest / hardening amplo | **fora do escopo** |
| Classificação | **PASS** com escopo limitado |

---

## Git audit

| Item | Valor |
|------|-------|
| Branch | `main` |
| HEAD Gate | `b184501` |
| Commits Fase 14 | `df25dc4`, `fbf211d`, `71a305a`, `b184501` — **presentes** |
| WIP preservado | HelpControl.xaml(.cs); Scripts deploy (untracked) |
| Diff acidental de produto | **nenhum** no Gate (só WIP conhecido) |

### DOCUMENTATION ISSUE (menor)

`Docs/qa/FASE14-RELEASE-CANDIDATE-REPORT.md` registra corretamente o HEAD **pré-fase** (`844ce25`), não o HEAD final do RC (`b184501`). Não é inconsistência de métricas; esclarecido neste relatório.

---

## Regressões encontradas

**0**

## Correções realizadas neste Gate

**Nenhuma** (código de produto não alterado para “passar” no Gate).

---

## Decisão

# GO

O RC `1.0.0-rc.1` em `b184501` está tecnicamente pronto para promoção a **PRIMOX Workshop 1.0.0**.

Limitações conhecidas **não** são bloqueadores de uso comercial pretendido (demo/instalação/operação), desde que documentadas.

---

## Promoção (pós-GO)

| Campo | Valor |
|-------|-------|
| Versão promovida | **1.0.0** |
| Commit de release | *(preenchido após commit)* |
| Tag | `v1.0.0` |
| Publicação externa | **não** (aguarda confirmação explícita) |

**Não iniciado:** Fase 15, website, novos módulos.
