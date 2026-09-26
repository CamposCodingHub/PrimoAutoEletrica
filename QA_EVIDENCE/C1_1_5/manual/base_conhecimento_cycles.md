# C1.1.5 — Ciclos Base Conhecimento / Ferramentas (App EXE)

## Pre-fix (C1.1.4 residual)
- Interacao:Controle:BaseConhecimentoControl FAIL — Owner em Window fechada
- FerramentasControl: mesma excecao async unhandled (check ainda PASS)

## Pos-fix focused
- Filter: Interacao:Modulo:ClientesControl,Interacao:Controle:BaseConhecimentoControl,Interacao:Controle:FerramentasControl
- Resultado: 3/3 PASS; 0 Owner errors (residual_check_*)

## Pos-fix full suite
- Interacao:Controle:BaseConhecimentoControl PASS 3973 ms
- Interacao:Controle:FerramentasControl PASS 7121 ms
- owner_on_closed_errors=0 em todo o log

Nao desabilitado; permanece na bateria; timeouts nao foram apenas elevados.
