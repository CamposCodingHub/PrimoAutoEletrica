# PRIMOX WORKSHOP — B3
## AUDITORIA DE SEGURANÇA E MATRIZ RBAC (ROLE-BASED ACCESS CONTROL)

**Data:** 2026-09-24  
**Branch:** `audit/product-discovery-2026-09`  
**Status da Auditoria:** **CONFORME (FAIL-CLOSED)**

---

### 1. Diretriz de Segurança e Fail-Closed

O PRIMOX Workshop adota a arquitetura de autorização **Fail-Closed**:
- Toda permissão ausente, indefinida ou em caso de indisponibilidade de banco resulta em **DENY** imediato (`RegistrarPermissaoNegada`).
- Operações críticas (`SISTEMA_CONFIGURAR`, `PERMISSOES_GERENCIAR`, `CAIXA_*`, `FINANCEIRO_*`) possuem verificações duplas tanto em nível de ViewModel/UI quanto nos serviços de retaguarda (`DatabaseBackupService.RequireSistemaConfigurar`, etc.).
- Rotas da API REST exigem autenticação JWT Bearer com claims explícitos (`ORCAMENTO_LER`, `ESTOQUE_AJUSTAR`, etc.).

---

### 2. Perfis de Acesso Cadastrados na Base Operacional

| Perfil | ID | Permissões Ativas | Escopo Comercial / Operacional |
|---|:---:|:---:|---|
| **Administrador** | 1 | 82 / 82 (100%) | Acesso total irrestrito (configurações, auditoria, banco, financeiro) |
| **Gerente** | 2 | 77 / 82 (93.9%) | Gestão da oficina, aprovações, relatórios e descontos |
| **Vendedor** | 5 | 30 / 82 (36.6%) | Clientes, veículos, orçamentos, vendas balcão e agendamentos |
| **Caixa** | 6 | 18 / 82 (22.0%) | Abertura/fechamento de caixa, sangria, suprimento e recebimentos |
| **Estoquista** | 7 | 16 / 82 (19.5%) | Consulta de estoque, entrada de notas fiscais e catálogo |
| **Almoxarife** | 8 | 16 / 82 (19.5%) | Ajuste físico de estoque, conferência e movimentações |
| **Mecânico / Técnico** | 3, 10 | 10 / 82 (12.2%) | Ordens de serviço, checklist DVI e autoelétrica técnica |
| **Financeiro** | 9 | 7 / 82 (8.5%) | Contas a pagar, contas a receber e movimentações bancárias |

---

### 3. Matriz de Autorização por Função e Perfil

| Módulo / Operação | Código da Permissão | Admin | Gerente | Vendedor | Caixa | Mecânico |
|---|---|:---:|:---:|:---:|:---:|:---:|
| Visualizar Dashboard | `DASHBOARD_VER` | ALLOW | ALLOW | ALLOW | ALLOW | DENY |
| Cadastrar Cliente | `CLIENTES_CRIAR` | ALLOW | ALLOW | ALLOW | DENY | DENY |
| Excluir Cliente | `CLIENTES_EXCLUIR` | ALLOW | ALLOW | DENY | DENY | DENY |
| Criar Orçamento | `ORCAMENTOS_CRIAR` | ALLOW | ALLOW | ALLOW | DENY | DENY |
| Aprovar Orçamento | `ORCAMENTO_APROVAR` | ALLOW | ALLOW | DENY | DENY | DENY |
| Converter Orçamento em OS | `ORCAMENTOS_CONVERTER_OS` | ALLOW | ALLOW | ALLOW | DENY | DENY |
| Visualizar OS | `ORDENS_SERVICO_VER` | ALLOW | ALLOW | ALLOW | DENY | ALLOW |
| Finalizar OS / Baixar Estoque | `ORDENS_SERVICO_EDITAR` | ALLOW | ALLOW | DENY | DENY | ALLOW |
| Aplicar Desconto PDV | `PDV_APLICAR_DESCONTO` | ALLOW | ALLOW | DENY | DENY | DENY |
| Ajustar Preço de Custo/Venda | `ESTOQUE_AJUSTAR_PRECO` | ALLOW | ALLOW | DENY | DENY | DENY |
| Realizar Sangria de Caixa | `CAIXA_SANGRIA` | ALLOW | ALLOW | DENY | ALLOW | DENY |
| Executar Backup Manual | `SISTEMA_CONFIGURAR` | ALLOW | DENY | DENY | DENY | DENY |
| Restaurar Backup | `SISTEMA_CONFIGURAR` | ALLOW | DENY | DENY | DENY | DENY |
| Gerenciar Permissões | `PERMISSOES_GERENCIAR` | ALLOW | DENY | DENY | DENY | DENY |

---

### 4. Conclusão da Auditoria de Segurança
- Não foram encontrados bypasses de autorização.
- Não existem telas críticas abertas para perfis não autorizados sem verificação de permissão no carregamento e na execução de ações.
- O sistema mantém conformidade total com o modelo fail-closed.
