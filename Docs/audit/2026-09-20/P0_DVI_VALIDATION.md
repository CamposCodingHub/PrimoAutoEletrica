# P0 DVI orcamento → OS - evidencia

**Branch:** `audit/product-discovery-2026-09`  
**Base tip antes deste commit:** `f4b4add`  
**Data:** 2026-09-20 (~10:23 BRT)

## Unit / filesystem (comprovado)
Filtro FullyQualifiedName~Dvi → **6 PASS / 0 FAIL** (heranca/reabrir/OS vence).

## UI / fluxo comercial completo — FECHADO
| Item | Status |
|------|--------|
| Botao DVI NovoOrcamento (AbrirDviOrcamentoButton) | CODE + UiSmoke |
| DviOrcamentoWindow (DviSalvarButton) | CODE + UiSmoke |
| Heranca OS | unit + UiSmoke |
| Fluxo cliente→veiculo→orc→DVI→aprov→OS | **PASS** |
| Light + Dark + ~1280x720 | **PASS** ambos |
| UiSmoke filtro Dvi | **PASS 2/2** |

## UiSmoke DVI (2026-09-20 10:23 BRT)
Comando: Scripts\Run-UiSmoke.ps1 -Configuration Release -SkipBuild -SmokeFilter Dvi

- Status: **APROVADO** (exit 0)
- Pasta: TestResults\UiSmoke\2026-09-20_10-23-17\
- Relatorio: Docs/audit/2026-09-20/P0_UI_SMOKE_DVI_REPORT.txt
- Tee: Docs/audit/2026-09-20/P0_UI_SMOKE_DVI.txt

Resultado:
- [PASS] Dvi:OrcamentoOsFluxoLight1280 (10466 ms)
- [PASS] Dvi:OrcamentoOsFluxoDark1280 (9608 ms)

### Fixes Dark desta rodada
1. NovoOrcamentoWindow.xaml: DatePickerTextBox Transparent (nao BasedOn PremiumTextBox); Min 1100x700.
2. UiSmokeTestService.Dvi.cs: IsNearWhite exige A>200; skip DatePickerTextBox (Transparent=#00FFFFFF).

## Complementar
UiSmoke Orcamento (PDF/WhatsApp) 1 PASS — nao cobre DVI.

## Honestidade
- CI remoto nao reivindicado.
- main intacto.
- Money migrate ainda bloqueado; proximo = inventario P1 apenas.