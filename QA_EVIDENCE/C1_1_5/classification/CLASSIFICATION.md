# C1.1.5 Classification — Interacao:Controle:BaseConhecimentoControl Owner-on-closed

Date: 2026-09-26 America/Sao_Paulo
Residual source: C1.1.4 App EXE UiSmoke 205/206

## Exception (authoritative)
InvalidOperationException: Nao e possivel definir a propriedade Owner para uma Janela fechada.
at Window.set_Owner
at BaseConhecimentoControl.AbrirArtigoDetalhes (Owner = Window.GetWindow(this))
at VerDetalhesButton_Click
at UiSmokeTestService.RaiseButtonClick → ExerciseInteractionSurface → ExerciseHostedElementButtons (generic)
Preceding: WARN x2 'janela Window nao ficou visivel apos 5s'
Collateral: FerramentasControl.AbrirTool360 same exception as unhandled UI error while Interacao:Controle:FerramentasControl still PASS (async void)

## Hypotheses A–G

| ID | Hypothesis | Verdict | Evidence |
|----|------------|---------|----------|
| A | Host closed before Owner= by stale AutomatedDialogSupervisor BeginInvoke (Dispose Join 1s does not drain dispatcher); HandleManagedDialogs treats next host as transient and Close()s it | CONFIRMED primary harness | Supervisor uses BeginInvoke; Dispose only Cancel+Join(1s); HandleManagedDialogs closes any visible non-owner window; WARNs match ShowWindow during close race; Ferramentas Owner-on-closed under same generic harness |
| B | GetWindow returns wrong window (not host) | REJECTED | Visual tree parent of hosted UserControl is CreateHostWindow; no reparent |
| C | Dialog itself already closed when Owner set | REJECTED | Stack is set_Owner on new CasoTecnicoDialog / Tool360Window before ShowDialog |
| D | ShowWindow off-screen (-10000) makes IsVisible false permanently | REJECTED as root | Isolated run WARNs but PASS; IsVisible WARN alone does not throw Owner-on-closed |
| E | App lacks closed-owner guard (raw Owner= vs WindowOwnerHelper) | CONFIRMED secondary REAL BUG | BaseConhecimento/Ferramentas use Owner=Window.GetWindow(this); WindowOwnerHelper.ConfigureOwner already exists and is used elsewhere (64 call sites); raw assign throws; Ferramentas unhandled in reproduce_race |
| F | Missing selection → MessageBox path only | REJECTED as fail path | PrimeSelectors selects DataGrid[0]; fail stack is AbrirArtigoDetalhes Owner line, not MessageBox |
| G | Only raise smoke timeout / empty catch | REJECTED approach | Forbidden by C1.1.5 constraints; would mask BOTH defects |

## Classification
**BOTH**
- HARNESS (primary smoke residual): stale supervisor can Close the next interaction host; ShowWindowForInteraction does not fail-fast when host dies; generic per-button host recreate amplifies race.
- REAL BUG (app): raw Owner assignment without WindowOwnerHelper; production MainWindow rarely closes mid-click, but async Ferramentas path already surfaces unhandled InvalidOperationException under harness stress — defensive Owner is required.

## Fix plan (root cause only)
1. Harness: drain dispatcher on AutomatedDialogSupervisor.Dispose; skip smoke-host windows in HandleManagedDialogs; assert host alive before RaiseButtonClick in ExerciseInteractionSurface.
2. App: BaseConhecimentoControl + FerramentasControl use WindowOwnerHelper.ConfigureOwner.
3. Regression test for closed-owner ConfigureOwner no-throw + harness host-guard if unit-testable.
4. Do NOT disable BaseConhecimento smoke; do NOT only raise timeouts; no empty catches swallowing Owner errors in product code (helper already catches InvalidOperationException after validation — that is intentional safe configure).
