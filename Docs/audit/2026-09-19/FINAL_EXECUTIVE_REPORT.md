# J — FINAL_EXECUTIVE_REPORT — Phase 0 ( BRT)

## Veredito

**PrimoAutoEletrica (branch audit) NÃO está pronto para SaaS, NF-e plena, sync multi-filial, nem API pública em produção.**

É um produto desktop WPF sólido em evolução (`net10.0-windows`), com API auxiliar **sem autenticação**, RBAC desktop que **falhava aberto** em erro de DB (mitigado neste PR), e licenciamento local frágil.

## Confirmado vs refutado vs adiado

| Item | Resultado |
|------|-----------|
| API sem JWT / endpoints abertos | CONFIRMADO |
| PermissionService fail-open | CONFIRMADO → mitigado fail-closed |
| Histórico financeiro por nome | CONFIRMADO (código morto/anti-padrão) → removido; load por Id |
| License local SHA256 | CONFIRMADO (sem fix server) |
| CI continue-on-error | CONFIRMADO → GATE/INFO |
| TFM net6 “preso” | **REFUTADO** neste branch (net10.0-windows) |
| Money REAL | CONFIRMADO — adiado (sem migration destrutiva) |
| DatabaseService rewrite | ADIADO |
| DVI/portal/mobile/WhatsApp/etc. | ADIADO (roadmap) |

## Padrões honestos (elogiar / manter)

- NotificationService `NaoConfigurado`
- FilialService neutro (`MultiFilialDisponivel = false`)
- Primox360 financeiro por ID (não por nome)

## Fixes neste PR (código)

1. PermissionCheckResult + fail-closed Unavailable + call sites críticos
2. Remoção matching nome no Histórico Cliente + testes
3. Gate/docs API sem auth (sem fake licensing server)
4. CI GATE vs INFO
5. PBKDF2 → 600_000 com NeedsRehash existente

## Bloqueio do executor

Shell/Read com `machineId=de411c5d-...` **ignorado** neste subagente (só Linux). Sem `gh auth`. Build Release WPF/net10-windows e `Deploy-ToInstalledApp.ps1` **não** rodaram aqui.

## Próxima Phase

1. Parent: CopyFromBox + apply no Windows + `dotnet build` + testes + deploy + push
2. JWT real na API **ou** isolar API de produção
3. Plano money INTEGER/DECIMAL
4. License assinada
5. Produto: escolher 1 gap de mercado (DVI ou pós-venda) após P0 auth/money

Baseline tip pedido: ~`7702197` / worktree `aa744ac`.
