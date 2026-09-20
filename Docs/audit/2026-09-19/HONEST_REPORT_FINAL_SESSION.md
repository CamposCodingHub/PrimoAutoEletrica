# Relatório sincero — Phase 0 + Phase 1 (Windows)

**Quando:** 2026-09-19 23:16 BRT  
**Branch:** `audit/product-discovery-2026-09`  
**Commits:** Phase 0 `eea1576` · Phase 1 `157e50e` (+ follow-up Case05 se aplicável)

## FEITO (com evidência)

### Phase 0
- RBAC fail-closed, histórico por Id, PBKDF2 600k, CI gates
- JWT **não** wired (gate test SKIP documenta ausência)
- License = scaffold local (não server)

### Phase 1
- Primox360 joins financeiros por ClienteId / Origem+ReferenciaExterna; nome exact-only (0/2+ → null)
- `DividaTotalDisplay` mostra valor só de vínculos por ID + texto explícito (não TEXT_MATCH)
- DVI: `OrdemServicoId` + `OrcamentoId`, remount de fotos pendentes; **sem** cloud approval
- Pós-venda: `LembretesRevisaoWindow` tratar/reabrir local; **sem** envio WhatsApp
- Deploy instalado: `%LocalAppData%\PrimoAutoEletrica\App` ~23:13 BRT (atalho PRIMOX Workshop)

### Testes Windows (esta sessão)
| Suite | Resultado |
|-------|-----------|
| Filtro Phase0/1 (Security+DVI+Lembrete+360Id) | **9 PASS** |
| Suite unitária oficial `Tests\PrimoAutoEletrica.Tests` | **232 PASS / 1 SKIP / 0 FAIL** (Total 233) |
| Exhaustive UI completo (todas as resoluções/modais) | **NÃO RODADO nesta sessão** |
| QaEngine 43/43 | **NÃO re-evidenciado nesta sessão** (último histórico em Docs/qa) |

## NÃO FEITO
- Portal web, mobile, WhatsApp Cloud, PIX, SaaS, frota/J1939, Intelligence, license server, JWT API
- Tela DVI embutida no fluxo de orçamento (além do vínculo no documento)
- ExhaustiveUi + mapa de modais com contagem fresca desta build
- Money INTEGER cents em todos os paths
- Fases 2–6 (ver `PHASES_2_6_ROADMAP_NAO_FEITO.md`)

## FALTA para “certeza comercial”
1. Rodar `Scripts\Run-UiSmoke.ps1 -Configuration Release` e (se existir) Exhaustive — gravar PASS/FAIL reais
2. Mapa de modais atualizado vs build deployada
3. Escopo produto das Fases 2–6 se forem requisito de negócio

**Não afirmar 100% sem bugs / Phases 2–6 feitas.**

## UI smoke (esta sessão — evidência fresca)
- Script: `Scripts\Run-UiSmoke.ps1 -Configuration Release -SkipBuild`
- Resultado: **APROVADO — Total=194, Sucesso=194, Falhas=0** (~23:43 BRT)
- Relatório: `TestResults/UiSmoke/2026-09-19_23-16-25/ui-smoke-2026-09-19-23-43-28-379-p29904.txt`
- Exhaustive multi-resolução / mapa completo de modais: **ainda NÃO FEITO nesta sessão**
