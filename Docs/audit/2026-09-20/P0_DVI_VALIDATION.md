# P0 DVI orcamento → OS — evidencia

## Unit / filesystem (comprovado)
Filtro `FullyQualifiedName~Dvi` → **6 PASS / 0 FAIL**

| Teste | Prova |
|-------|-------|
| SalvarSomenteOrcamento + CarregarOuPadrao sem OS | herda itens, OkEntrada, Observacoes, FotoPath |
| OS ja tem DVI | OS vence; nao mistura DoOrcamento |
| Reabrir orcamento | nao duplica; mantem estado |

## UI / fluxo comercial completo
| Item | Status |
|------|--------|
| Botao DVI em NovoOrcamentoWindow | CODE presente |
| DviOrcamentoWindow | CODE presente |
| Heranca OS via CarregarDviParaOrdem(orcamentoId) | CODE + unit |
| Fluxo visual cliente→veiculo→orc→DVI→aprov→OS | **NAO EXECUTADO** nesta sessao |
| Light + Dark + resolucao pequena | **NAO EXECUTADO** |
| UiSmoke focado DVI | **NAO EXECUTADO** ainda |

Proximo: Run-UiSmoke Release (orcamentos) + checklist visual manual/desktop.

## UiSmoke (2026-09-20 10:06 BRT)
```
Scripts\Run-UiSmoke.ps1 -Configuration Release -SkipBuild -SmokeFilter Orcamento
```
- Status: **APROVADO**
- Checks: 1 PASS / 0 FAIL (`Orcamentos:ConversoesPdfWhatsAppAlertas`)
- **Nao cobre** botao DVI / heranca visual / Light-Dark

## Ainda NÃO EXECUTADO (obrigatorio antes de fechar P0 DVI)
- Fluxo visual completo cliente→veiculo→orc→DVI→aprov→OS
- Light + Dark + resolucao pequena
- UiSmoke dedicado ao DVI (ainda nao existe case)
