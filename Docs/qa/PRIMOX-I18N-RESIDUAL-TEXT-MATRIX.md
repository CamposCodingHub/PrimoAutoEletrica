# PRIMOX-I18N-RESIDUAL-TEXT-MATRIX — I18N-04

**Data:** 2026-09-10  
**Fontes:** `Scripts/Audit-I18nRuntimeResiduals.ps1` · smoke I18n04 · inventário SURF-*

---

## Static XAML residual classification

| Class | Count (approx) |
|-------|---------------:|
| TRANSLATION_REQUIRED | **508** |
| UNKNOWN | 1572 |
| FISCAL_TERM | 19 |
| BRAND | 5 |
| Total literals scanned | 2104 |

JSON: `TestResults/I18n/i18n-runtime-residuals-20260910-073316.*`

---

## Runtime residuals (EN/ES visual tree)

Após exclusão de dados Smoke / IDs ORC-/OS-:

| Área | Exemplos TRANSLATION_REQUIRED | Severidade |
|------|-------------------------------|------------|
| Dashboard | empty “Nenhuma venda…”, attention “orçamento(s)” | P1 |
| Clientes | “Veículos vinculados…”, “Veiculo principal” | P1 |
| Veículos | headers/histórico PT | P1 |
| OS | “Sem orcamento vinculado”, coluna Veículo | P1 |
| Orçamentos | status/timeline PT | P0/P1 |
| PDV | empty “Nenhuma venda suspensa”; ES ainda “ESC Cancelar” se binding falhar em TextBlock filho | P1 |
| Estoque | empties “Nenhum…” | P1 |
| Financeiro | label “Fornecedor” | P1 |
| Agenda | coluna/labels Veículo | P1 |
| Funcionários | matriz permissões “Excluir/Editar…” | P1 |
| Fornecedores | “Buscar”, filtros | P1 |
| Relatórios | status “Sucesso” em grids de auditoria | P2 |
| Catálogo | subtítulo fornecedores | P2 |
| Fiscal | parágrafo config emitente | P1 |
| Help | corpo HELP CORE/EXTENDED pt-BR | P0 conteúdo / P2 extended |

### Separação obrigatória

| Bucket | Exemplos |
|--------|----------|
| TRANSLATION_REQUIRED | botões/labels/empty/Help chrome |
| LEGITIMATE_DATA / USER_DATA | “Fornecedor Smoke…”, nomes cadastrados |
| TECHNICAL/FISCAL | CPF, CNPJ, NCM, CFOP, NF-e |
| BRAND | PRIMOX |
| INTERNAL | ORC-2026…, OS-2026…, module IDs |

---

## P0 corrigidos nesta fase

| Problema | Correção |
|----------|----------|
| ToolTip “Recolher menu” | `CollapseMenu` |
| Salvar rascunho (OS/Orçamentos) | `SaveDraft` |
| Salvar alterações (Editar Cliente) | `SaveChanges` |
| Salvar emitente / assinatura / nova senha | chaves novas |
| PDV F7/Cancelar concluída/ESC | bindings |
| Funcionários Novo/Perfis/Permissões | `NewEmployee` / `ManageProfiles` / `ConfigurePermissions` |
| PDV F5 buscar cliente | `SearchClientShortcut` |

---

## Pendências principais (não fingir PASS)

1. Empty states / copy descritiva dos módulos  
2. Help body (HELP CORE priority)  
3. Dialogs Views/* titles & forms  
4. Relatórios strings densas  
5. DataGrid headers residuais (“Veículo”, “Fornecedor”)
