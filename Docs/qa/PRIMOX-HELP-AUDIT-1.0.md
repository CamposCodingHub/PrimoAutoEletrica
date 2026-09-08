# PRIMOX — Help Audit 1.0

**Etapa:** Codebase Sanitization 1.0  
**HEAD base:** `664be6b` → pós-sanitização (commits desta etapa)  
**Tag:** `v1.0.0` → `a4ad6fe` intacta

## 1. Estado inicial

- HelpControl WIP com tema **hardcoded dark** (`#0B1220`, etc.) — fora do Design System Light/Dark.
- HelpTopicsCatalog já existia (Escola PRIMOX) com cargos e passos, porém incompleto vs. escopo Help Center 1.0.
- Placeholders perigosos no produto (fora da Ajuda): NotificationService fake success; FilialService SP/RJ.

## 2. Problemas encontrados

| ID | Problema | Severidade |
|----|----------|------------|
| H-01 | Ajuda fora do padrão de cores (Light quebrado) | HIGH |
| H-02 | Falta tópicos: limites honestos, financeiro, proprietário, problemas, eu-quero | MEDIUM |
| H-03 | Passos curtos demais (“Menu Clientes”) | MEDIUM |
| H-04 | Sem smoke dedicado da Ajuda | MEDIUM |
| H-05 | Idiomas: conteúdo longo só pt-BR | INFO / LIMITATION |

## 3. Problemas corrigidos

- Tema Ajuda → `DynamicResource` (Surface/AppBackground/PrimaryText/…)
- Expansão de catálogo + árvore (cargos, limites, problemas, backup, financeiro)
- Passos detalhados (onde clicar / o que digitar / exemplos QA_HELP_)
- Honestidade NF-e / WhatsApp / multi-filial / sync
- Smoke `QaEngine:CompleteUiHelpCenter` (abrir, buscar, navegar tópicos)

## 4. Módulos documentados

Dashboard, Agenda, Orçamentos, OS, Kanban, PDV, Clientes, Veículos, Auto Elétrica, Estoque, Catálogo, Fornecedores, Funcionários, Financeiro, Relatórios, Configurações, Import NF-e, Ajuda.

## 5. Cargos documentados

Proprietário, Gerente, Administrativo/Recepção, Caixa, Eletricista/Técnico, Financeiro + treinar equipe.

## 6. Screenshots / vídeos

- Screenshots versionados: **não** (mocks ilustrativos na UI da Ajuda).
- Vídeos: **não** nesta etapa (limitação de repositório).
- Matriz: `PRIMOX-HELP-COVERAGE-MATRIX.md`.

## 7. Cobertura

Ver matriz. Idioma conteúdo: **pt-BR**. Shell i18n permanece pt/en/es.

## 8. Light / Dark / Resoluções

Exercitados pela regressão Exhaustive (8 rounds Light/Dark × 4 resoluções) + CompleteUi Dark surfaces + Help Center smoke.

## 9. Acessibilidade

AutomationProperties na Ajuda (nome do controle, busca, índice, conteúdo). Padrão P15E-015 preservado via Exhaustive/DeepQa.

## 10. Limitações

- Sem overlay “modo tutorial” na UI principal (futuro).
- Sem tradução completa dos artigos para en/es.
- Sem vídeos/screenshots reais no Git.
- Calendário: instruído a não clicar dias como botões operacionais.

## 11. Decisão Help

**HELP CENTER READY WITH LIMITATIONS** (pt-BR completo operacional; i18n textual longo parcial).
