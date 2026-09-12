# PRIMOX FULL ASSURANCE-12 — INSTALLED CLIENTES SMOKE

## A11 baseline

Installed Clientes smoke: FAIL Exit=2 (classified as harness flake without root cause).

## Root cause (A12)

Multiple contributing issues, **not** “ignore as flake”:

1. **PRODUCT / HARNESS:** Smoke looked for button text `Salvar alteracoes` but UI shows localized `Salvar alterações` (`SaveChanges`). Fixed by naming `SalvarAlteracoesButton` + smoke click by name.
2. **HARNESS:** `PersistReport` wrote under install `BaseDirectory\Logs` (fragile). Moved to `App.RuntimeLogDirectory`.
3. **HARNESS:** `Run-UiSmoke` / installed launcher used relative `--app-data` and/or WinExe without WaitForExit; fixed absolute app-data + EXE WaitForExit.
4. **HARNESS:** Shared AppData across 10 cycles polluted selection / WhatsApp enablement → intermittent Exit=2 (`WhatsAppClienteButton` disabled). Fixed with **per-cycle AppData**.
5. **HARNESS:** stdout redirect on WinExe produced empty ExitCode in installer capture.

## Results (current)

| Run | Cycles | Pass | Fail | Exit=2 |
|-----|-------:|-----:|-----:|-------:|
| After package rebuild (shared data) | 10 | 9 | 1 | 1 |
| Retest per-cycle AppData | 10 | **10** | **0** | **0** |
| Installer E2E Final CRUD smoke | 1 | **PASS Exit=0** | 0 | 0 |

Evidence:

- `TestResults/FullAssurance12/20260911-213415/InstalledClientes/`
- `TestResults/Commercial08/commercial-08-e2e-20260911-213902.md` (fails=0)

## Classification

| Issue | Class |
|-------|-------|
| Accented SaveChanges click | PRODUCT (a11y name) + HARNESS |
| Shared AppData WhatsApp disable | HARNESS |
| PersistReport path | PRODUCT |
| Empty Exit with redirect | HARNESS |
