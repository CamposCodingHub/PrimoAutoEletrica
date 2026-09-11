# PRIMOX-COMMERCIAL-08 — Installer Matrix

**Setup E2E:** `PRIMOX-Workshop-Setup-1.0.0-PackagingE2E.exe`  
**SHA256:** `A18224631F26824A0D4F6C74A95D052B3758F79075B5DF0F902496029BF1CC03`  
**Evidência:** `TestResults/Commercial08/commercial-08-e2e-20260910-202346.md`  
**Open-app + QaEngine:** `TestResults/Commercial08/qaengine-post-reinstall.log`

## Ciclos principais

| Teste | Ciclo 1 | Ciclo 2 | Ciclo 3 | Resultado |
|-------|---------|---------|---------|-----------|
| Install Exit=0 | PASS | PASS | PASS | **3/3** |
| Startup Responding | PASS | PASS | PASS | **3/3** |
| Controlled data seed | PASS | PASS | PASS | **3/3** |
| Silent uninstall Exit=0 | PASS | PASS | PASS | **3/3** |
| Binary cleanup (EXE gone) | PASS | PASS | PASS | **3/3** |
| Data preservation | PASS | PASS | PASS | **3/3** |
| Process residual = 0 | PASS | PASS | PASS | **3/3** |

## Ciclo final / extras

| Teste | Resultado | Notas |
|-------|-----------|-------|
| Final reinstall Exit=0 | PASS | Histórico Exit=2 **corrigido** |
| Final startup | PASS | |
| Final data readable | PASS | Marker + integrity |
| Uninstall com app aberto | PASS | Exit=0, residualProcs=0 |
| Post-reinstall QaEngine | PASS | Exit=0 |
| DB integrity / FK / migrations | PASS | ok / 0 / 28 |
| Start Menu (E2E group) | PASS | |
| Smoke filter Clientes (legado) | FAIL | Flake UI CadastroCompleto — não installer |
| Reboot | BLOCKED | Ambiente |

## Verdict por eixo

| Eixo | Status |
|------|--------|
| Installer lifecycle | **GREEN** |
| Data preservation | **GREEN** |
| Process/binary cleanup | **GREEN** |
| Signing / SmartScreen / Reboot | **YELLOW/BLOCKED** |
