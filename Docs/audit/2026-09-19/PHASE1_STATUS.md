# PHASE1_STATUS — 2026-09-19 (America/Sao_Paulo)

**Escopo:** apenas Workshop 2.0 gaps **seguros localmente**.  
**Proibido neste status:** marcar Phases 2–6 como DONE.

## FEITO (código neste pacote — validar build/test no Windows)

| Item | Evidência |
|------|-----------|
| P1-01 Scan 360 financeiro por nome | `Primox360Service` já agregava por `ClienteId` / `Origem+ReferenciaExterna`; reforço: `DividaTotalDisplay` / `FonteDivida` honestos; teste `Primox360IdFinancialJoinTests` |
| P1-01b Homônimos em ContasReceber | `FinanceiroDatabaseService.ResolverClienteIdPorNome` — match exato e **null se 0 ou 2+** (sem Contains) |
| P1-02 DVI harden | `DviChecklistService`: documento com `OrdemServicoId` + `OrcamentoId`, remount de fotos pendentes, path `os-*.json` + espelho `orc-*.json`; OS window usa `_dviPendingMediaId` estável |
| P1-03 Pós-venda lista local | `LembretesRevisaoWindow` + `MarcarTratadoLocalmente` / `ReabrirPendente`; botão OS abre janela (sem WhatsApp Cloud) |
| P1-04 OS 360 pós-venda link | `ObterOrdemServico360` marca `PosVendaLink=CONNECTED` se existir lembrete local da OS |

## NAO FEITO (explícito)

| Item | Motivo |
|------|--------|
| Portal web cliente | Fora do escopo Phase 1 |
| Mobile app técnico | Fora do escopo |
| WhatsApp Cloud send | Proibido fake; só lista local |
| PIX gateway / TEF | Fora do escopo |
| SaaS / multi-tenant | Fora do escopo |
| Frota full / J1939 | Fora do escopo |
| Intelligence / license server | Fora do escopo |
| JWT API real | Phase 0 residual — não Phase 1 produto |
| Money REAL → DECIMAL migration | Destrutiva — não nesta fase |
| DVI UI em Orçamento (tela própria) | Só espelho JSON por `OrcamentoId` ao salvar OS; **sem** tela DVI em `NovoOrcamentoWindow` |
| Build Release / Deploy / git push | Requer machineId Windows (executor Linux ignorou machineId) |

## FALTA (parent no Windows `de411c5d-...`)

1. Aplicar pacote `phase1-audit-2026-09-19.tgz` via `Apply-Phase1.ps1`
2. `dotnet build` Release + testes filtrados Phase 1
3. `Deploy-ToInstalledApp.ps1` (UI: Lembretes + DVI)
4. Commit + push branch `audit/product-discovery-2026-09`
5. Smoke manual: abrir OS → DVI foto → salvar → reabrir; Lembretes → marcar tratado

## Critério de honestidade

- Não afirmar Phase 2–6.
- Não afirmar WhatsApp enviado.
- Não afirmar portal/mobile/PIX.

## Windows evidence (2026-09-19 23:13 BRT)
- Release build: 0 Erro(s)
- Filtered tests (Primox360IdFinancialJoin + DviChecklist + LembreteRevisao + SecurityTests): **9 PASS / 0 FAIL**
- DVI source-guard test adjusted: allows honest comment `Sem cloud approval`; rejects `CloudApprovalService` / `ApproveOnCloud`
- Deploy: see Deploy-ToInstalledApp.ps1 log below

## Atualizacao 2026-09-20 — DVI no orcamento
- **FEITO:** `DviOrcamentoWindow` + botao **DVI** em `NovoOrcamentoWindow`
- `DviChecklistService.SalvarSomenteOrcamento` / `ExisteParaOrcamento` (JSON `orc-{id}.json`, sem cloud)
- Ainda **NÃO**: portal/mobile/WhatsApp Cloud/PIX
