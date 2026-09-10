# PRIMOX-I18N-04-BASELINE — Pre-flight (Fase 0)

**Missão:** PRIMOX-I18N-04 — Manual Multilingual UX Audit  
**Data/hora:** 2026-09-10 07:19 (America/Sao_Paulo)  
**Escopo desta fase:** somente registro de baseline — **nenhuma alteração de código**

---

## 1. Identificação Git

| Item | Valor |
|------|-------|
| Branch | `main` |
| HEAD | `6036a40` (`docs(i18n): correct I18N-03 HEAD line without encoding corruption`) |
| HEAD esperado | `6036a40` — **CONFIRMADO** |
| Tag `v1.0.0` | `72d85fa` — **INTACTA** (`git rev-parse v1.0.0`) |
| Tag points-at HEAD? | **Não** (esperado; tag permanece em release histórico) |
| Remote | `main...origin/main` **ahead 10** (commits I18N locais ainda não pushed) |

### Worktree

| Estado | Detalhe |
|--------|---------|
| Limpo para I18N? | Quase — **1 arquivo modificado não relacionado** |
| WIP preservado | `Docs/qa/PRIMOX-FISCAL-LIVE-HOMOLOGATION-REPORT.md` (M) — **NÃO tocar nesta fase** |
| Scripts WIP | `Scripts/Atualizar-PrimoAuto.bat` **existe** · `Scripts/Deploy-ToInstalledApp.ps1` **existe** — **preservar** |

---

## 2. Cadeia I18N conhecida

| Fase | Conteúdo | HEAD / artefato |
|------|----------|-----------------|
| I18N-01 | Localization Core | `d73257f` + audit |
| I18N-02 | Module chrome | `c358d5c` + module coverage |
| I18N-03 | Interaction / dialogs | `de6b72e` + interaction doc |
| I18N-04 | Manual UX + user-visible coverage | **INÍCIO** (este baseline) |

---

## 3. Build baseline

| Item | Resultado |
|------|-----------|
| Comando | `dotnet build PrimoAutoEletrica/PrimoAutoEletrica.csproj -c Release` |
| Errors | **0** |
| Warnings | 73 (pré-existentes / CA1416 etc.) — não bloqueantes |
| Status | **PASS** |

---

## 4. Localization tests baseline

| Item | Resultado |
|------|-----------|
| Comando | `dotnet test … --filter FullyQualifiedName~Localization -c Release` |
| Passed | **20 / 20** |
| Failed | **0** |
| Status | **PASS** |

---

## 5. Static source coverage (NÃO = user-visible)

Fonte: `Scripts/Audit-I18nCoverage.ps1` · JSON `TestResults/I18n/i18n-coverage-20260910-071931.json`

| Métrica | Valor | Uso nesta fase |
|---------|------:|----------------|
| XAML files | 104 | referência |
| Literal UI attrs | **2231** | STATIC SOURCE only |
| Bound LocalizationHelper attrs | **525** | STATIC SOURCE only |
| Coverage bound/(literal+bound) | **~19%** | STATIC SOURCE only |
| UiText.T calls (code-behind) | **269** | complementar I18N-03 |
| MessageBox.Show calls | **237** | complementar |
| Localized common titles | **184** | complementar |

**Regra de verdade I18N-04:** estes números **não** afirmam experiência multilíngue real.  
A métrica oficial desta fase será:

> **USER-VISIBLE COVERAGE** = superfícies auditadas PASS / superfícies auditadas testáveis

separada por idioma (PT / EN / ES) e por criticidade (critical / secondary).

---

## 6. Cultura de negócio (inalterada)

| Item | Esperado |
|------|----------|
| UICulture | muda com idioma |
| CurrentCulture / formatação | permanece **pt-BR** |
| Fiscal / dinheiro / CPF-CNPJ | não traduzidos como regra de negócio |

---

## 7. Decisão do Pre-flight

| Check | Status |
|-------|--------|
| HEAD = 6036a40 | PASS |
| Tag v1.0.0 intacta | PASS |
| WIP fiscal doc preservado | PASS (não alterar) |
| Deploy scripts preservados | PASS |
| Build 0 errors | PASS |
| Localization 20/20 | PASS |
| Código alterado na Fase 0? | **NÃO** |

**Pre-flight: GO** para Fases 1+ (mapa de superfícies / matriz UX / execução real).

---

## 8. STOP parcial da Fase 0

Fase 0 concluída. Próximo passo obrigatório: inventário user-visible (Fase 1) **sem** conversão em massa de XAML.
