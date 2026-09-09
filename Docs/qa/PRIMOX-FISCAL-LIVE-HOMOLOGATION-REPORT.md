# PRIMOX — FISCAL LIVE HOMOLOGATION REPORT (Script 6)

**Data:** 2026-09-09  
**HEAD sessão:** `5d3768b` (baseline) → commit desta fase (ver git log)  
**Tag:** `v1.0.0` → `72d85fa` / objeto tag preservado (**NÃO movido**)  
**Branch:** `main`  
**Decisão:** **YELLOW** — `FISCAL HOMOLOGATION BLOCKED BY EXTERNAL PREREQUISITE`

```text
FAKE ≠ LIVE
UNIT TEST ≠ HOMOLOGAÇÃO REAL
HTTP 200 ≠ DOCUMENTO FISCAL AUTORIZADO
```

---

## Missão

Levar o estado de *“homologation technically implemented, live not executed”* para evidência live **ou** bloqueio externo preciso com preparação técnica completa.

**Resultado:** preparação técnica reforçada + live **BLOCKED** (sem token / emitente vazio / LiveHttpEnabled=false).

---

## Baseline (Script §1)

| Item | Valor |
|------|-------|
| Branch | `main` |
| HEAD baseline | `5d3768b` |
| Worktree | clean no início |
| Tag `v1.0.0` | PRESERVADA (não aponta para HEAD; **não alterada**) |
| Deploy scripts | rastreados no repo (não apagados) |

---

## Inventário fiscal (§2)

| Item | Estado |
|------|--------|
| IFiscalProvider | REAL |
| FocusNfeProvider | REAL (HTTP opt-in) · **não live-proven** |
| FakeFiscalProvider | REAL · **TEST-ONLY** |
| Homologação HTTP | REAL no código · **LIVE BLOCKED** nesta sessão |
| Token | **AUSENTE** (env=false, DPAPI=false) |
| Configuração emitente | **INCOMPLETA** (todos campos EMPTY no `fiscal-foundation.json` local) |
| Certificado A1 local | NÃO IMPLEMENTADO (Focus guarda cert no provedor) |
| XML | PARCIAL (JSON→Focus; `ObterXmlAsync` = PENDING; colunas path sem escrita local) |
| Consulta | REAL no código · LIVE BLOCKED |
| Persistência | REAL (FiscalOperations/Documents/Events) · unit restart PASS |
| Idempotência | REAL · unit PASS |
| Restart recovery | REAL · unit PASS |
| Rejeição | TEST (Fake) PASS · LIVE BLOCKED |
| Produção | **BLOQUEADA** |
| Cancelamento Focus | NÃO EXECUTADO / stub `FISCAL-FOCUS-CANCEL-NOT-EXECUTED` |

---

## Pesquisa documental Focus (§3)

Fontes oficiais usadas:

| Tópico | Fonte |
|--------|--------|
| Índice | https://doc.focusnfe.com.br/llms.txt |
| Ambiente | https://doc.focusnfe.com.br/reference/ambiente.md — Homolog `https://homologacao.focusnfe.com.br` · Prod `https://api.focusnfe.com.br` · prefixo `/v2` |
| Auth | https://doc.focusnfe.com.br/reference/autenticacao.md — HTTP Basic `token:` (senha vazia) |
| Emitir NF-e | https://doc.focusnfe.com.br/reference/emitir_nfe.md — fluxo assíncrono típico; consulta/webhook para status; resposta com `status`/`status_sefaz`/`numero`/`serie`/`caminho_xml_nota_fiscal` |

**Código vs docs:** Basic Auth + URL homolog + POST/GET `/v2/nfe` alinhados. Gap corrigido nesta fase: payload agora envia `serie`/`numero` quando configurados no emitente (antes validados e omitidos do JSON).

---

## Configuração segura (§4–6)

| Mecanismo | Status |
|-----------|--------|
| Env `PRIMOX_FOCUS_HOMOLOG_TOKEN` | ABSENT |
| DPAPI `%LOCALAPPDATA%\PrimoAutoEletrica\Config\fiscal-secrets.dpapi` | ABSENT |
| `fiscal-foundation.json` | PRESENT · `liveHttpEnabled=false` · issuer EMPTY |
| Segredo no Git | NÃO (scan sem PEM/`sk_live_`/`AKIA` literais de produção) |

Script novo (sem vazar segredo): `Scripts/Check-FiscalLiveReadiness.ps1` → **RESULT: BLOCKED**.

---

## Testes LIVE (§7–15)

| Teste | Tipo | Resultado | Evidência |
|-------|------|-----------|-----------|
| Provider connectivity (auth) | LIVE | **BLOCKED** | Token ABSENT; não inventado PASS |
| Network TLS homolog | AUDIT | REACHABLE | HTTP 404 em `/v2/` ~147ms (sem auth) |
| Send homologation | LIVE | **BLOCKED** | Pré-requisito externo |
| Status query | LIVE | **BLOCKED** | Idem |
| Persistence | REAL/UNIT | **PASS** | Unit store + restart |
| Restart | REAL/UNIT | **PASS** | `Restart_RecuperaOperacaoDoBanco` |
| Idempotency | REAL/UNIT | **PASS** | Consult-before-retry |
| Rejection | TEST | **PASS** | FakeFiscalProvider Rejected |
| Invalid credential | TEST | **PASS** | `FocusProvider_SemToken_NaoAutoriza` |
| Production guard | TEST | **PASS** | `FISCAL-PROD-BLOCKED`; HTTP count=0 |
| XML | AUDIT | **PARTIAL** | JSON enviado; XML path/DANFE pending |
| PDV integration | REAL/CODE+UNIT | **PASS** (fluxo) · live BLOCKED | PDV → Homolog service; venda não destruída por falha fiscal (design) |

### Separação de evidência

| Classe | Itens |
|--------|-------|
| VERIFIED LIVE | *(nenhum nesta sessão)* |
| VERIFIED AUTOMATED | 46 fiscal/homolog unit tests PASS; Production Guard; Fake reject/timeout/idempotency/restart |
| SIMULATED | FakeFiscalProvider scenarios (explicitamente TEST-ONLY) |
| BLOCKED | Live emit/consult/cancel Focus |
| NOT IMPLEMENTED | NFC-e/NFS-e emit; cancel Focus HTTP; XML/DANFE local download; IBS/CBS fields |

---

## Reforma Tributária (§25) — auditoria somente

| Aspecto | Classificação |
|---------|---------------|
| Capacidade de evoluir via JSON Focus / contratos | **PARTIAL** |
| Campos IBS/CBS / NFS-e Nacional no payload PRIMOX | **NOT IMPLEMENTED** (não especulativo) |
| Decisão | Arquitetura adapter + contratos permite extensão; **não** declarar READY fiscal reforma |

---

## QA / Build (§20–21)

| Suite | Resultado | Nota |
|-------|-----------|------|
| Fiscal unit filter | **PASS 46/46** | +1 teste `FocusPayload_IncluiSerieQuandoConfigurada` |
| Build Release | **PASS 0 errors** | Warnings existentes (CA1416/NU1701/EOL net6) |
| QaEngine | **PASS** | `TestResults/UiSmoke/2026-09-09_20-21-19` |
| Deep QA / Exhaustive / Long Run | **não reexecutados nesta sessão** (sem regressão de código UI); baseline recente 09/09 documentada em PROJECT_STATUS | Sem claim de nova Exhaustive |

---

## Como desbloquear LIVE (para o operador)

1. Criar empresa + token no **painel Focus Homologação**.  
2. Definir `PRIMOX_FOCUS_HOMOLOG_TOKEN` **ou** gravar via `FiscalConfigurationService.SaveHomologationToken` (DPAPI).  
3. Preencher emitente em **Operações Fiscais** (CNPJ, IE, endereço, IBGE, série, CRT…).  
4. Opt-in `liveHttpEnabled: true` **somente** Homologação.  
5. Cadastrar certificado A1 **no Focus** (não no Git).  
6. Produtos com NCM/CFOP reais; cliente com CPF/CNPJ + endereço.  
7. PDV → venda controlada → **NF-e Homologação**.  
8. Reexecutar Script 6 / readiness: `Scripts\Check-FiscalLiveReadiness.ps1`.

**Nunca** colocar token no repositório.

---

## Implementado nesta fase

- Auditoria completa Script 6 + inventário  
- Doc oficial Focus alinhada  
- Payload Focus: `serie` / `numero` quando configurados  
- Teste unitário do payload  
- `Scripts/Check-FiscalLiveReadiness.ps1`  
- Este relatório + atualização PROJECT_STATUS  

## Não implementado (fora do escopo Script 6)

- SaaS, mobile, IA, DVI, NFS-e completo, NFC-e, auto-update, signing, unlock produção  

## Próximo bloqueio comercial

**Credenciais + emitente Focus Homologação** → depois reexecutar LIVE #1–#6 com evidência real.

---

## Decisão final

```text
YELLOW
FISCAL HOMOLOGATION BLOCKED BY EXTERNAL PREREQUISITE
Production Guard = ACTIVE
v1.0.0 = PRESERVADA
```

**STOP** — não iniciar Script 7 automaticamente.
