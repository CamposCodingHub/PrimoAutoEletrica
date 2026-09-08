# PRIMOX Workshop — Product Gaps 1.0

**Auditoria:** Product Truth 1.0 · 2026-09-08 · HEAD `1f3af7e` · tag `v1.0.0`→`a4ad6fe`

Severidade baseada em impacto comercial/piloto, não em marketing.

---

## CRITICAL

| ID | Descrição | Impacto | Evidência | Ação |
|----|-----------|---------|-----------|------|
| GAP-C01 | PROJECT_STATUS maturidade **98/100** + seções “enterprise/ROI” contradizem gaps reais | Decisão comercial enviesada | PROJECT_STATUS L9 + seções ROI/.NET 9 | Corrigir documentação (esta auditoria) |
| GAP-C02 | Emissão NF-e SEFAZ **não implementada** enquanto docs antigos podem sugerir fiscal completo | Risco fiscal/comercial | `NFeEmissaoService.cs` vazio; roadmap ESTUDO_FISCAL unchecked | Manter como FUTURO; nunca vender como emissão |

---

## HIGH

| ID | Descrição | Impacto | Evidência | Ação |
|----|-----------|---------|-----------|------|
| GAP-H01 | Multi-filial = mock em memória | Cliente espera filiais reais | `FilialService` comentários “simulação” | Documentar SCAFFOLD; backlog futuro |
| GAP-H02 | Offline sync remoto ausente (`ProcessarFilaOfflineAsync` inexistente) | Não operar multi-loja sync | grep zero | FUTURO / NÃO IMPLEMENTADO |
| GAP-H03 | 2FA documentado como DONE no login mas LoginWindow sem desafio | Falsa sensação de segurança | PROJECT_STATUS vs Login*.cs | Corrigir docs; opcional wiring futuro |
| GAP-H04 | API JWT/policies/Keycloak não wired; docs API exageram | Integração externa insegura/incorreta | Program.cs; DOCUMENTACAO_API.md | Marcar API como parcial piloto |
| GAP-H05 | Update automático comercial NOT IMPLEMENTED; AtualizacaoWindow órfã | Atualização via script/deploy manual | Release Gate; sem `new AtualizacaoWindow` consumers | Deploy script / installer; não chamar auto-update |

---

## MEDIUM

| ID | Descrição | Impacto | Evidência | Ação |
|----|-----------|---------|-----------|------|
| GAP-M01 | Paginação SQL `ObterPaginado` inexistente; Estoque CLIENT_SIDE | Performance em catálogos grandes | EstoqueViewModel + PagingHelper | Aceitar limitação 1.0; backlog |
| GAP-M02 | NotificationService Twilio = PLACEHOLDER | SMS automático não funciona | TODO + Task.Delay | Usar apenas wa.me; docs honestas |
| GAP-M03 | FuncionariosViewModel / RelatoriosModernoViewModel ORPHAN | Dívida/manutenção | DI + testes sem UI | RETAIN; não apagar nesta auditoria |
| GAP-M04 | SchemaVersion histórico ~32 vs 27 ApplyMigration no código | Confusão de suporte | DB live vs Migrations.cs | Documentar; NÃO apagar migrations |
| GAP-M05 | P15E-015 a11y icon-only PARTIAL | Acessibilidade incompleta | Exhaustive 3.0 | Backlog LOW/MED a11y |
| GAP-M06 | ARCHITECTURE.md afirma .NET 9 WPF | TFM shipping = net6.0-windows | csproj + README | Corrigir doc |

---

## LOW

| ID | Descrição | Impacto | Evidência | Ação |
|----|-----------|---------|-----------|------|
| GAP-L01 | HelpControl / HelpTopicsCatalog WIP | Ajuda incompleta | git status untracked/modified | Preservar WIP; não bloquear 1.0 |
| GAP-L02 | Code signing NOT CONFIGURED | SmartScreen | Packaging report | Processo release futuro |
| GAP-L03 | Calendar Dark header KNOWN | Cosmético | 15E | Aceitar |

---

## DOCUMENTATION

| ID | Descrição | Ação |
|----|-----------|------|
| GAP-D01 | Scores conflitantes 98/100 vs 60/80 antigos no mesmo PROJECT_STATUS | Banner Product Truth + rebaixar maturidade honestamente |
| GAP-D02 | SECURITY.md lista 2FA sem qualificar “login não exige TOTP” | Atualizar SECURITY.md |
| GAP-D03 | DOCUMENTACAO_API JWT/OS routes vs Program.cs | Marcar desatualizada |
| GAP-D04 | CHANGELOG WCAG AA / “completo” | Tratar como OVERSTATED |
| GAP-D05 | QaEngine 42/42 vs menções 37/37 legadas | Preferir evidência Exhaustive 3.0 / último smoke |

---

## FUTURE

| ID | Descrição |
|----|-----------|
| GAP-F01 | Emissão NF-e/NFC-e SEFAZ + certificado |
| GAP-F02 | Multi-filial persistida + sync |
| GAP-F03 | Offline-first com fila remota |
| GAP-F04 | SaaS / website / licenciamento cloud |
| GAP-F05 | Auto-update assinado end-to-end |
| GAP-F06 | API autenticada completa (JWT + policies reais) |
| GAP-F07 | Paginação server-side WPF+API |

Nenhum GAP-F* será implementado nesta auditoria.
