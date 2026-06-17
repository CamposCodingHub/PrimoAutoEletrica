# Matriz de Permissoes e Seguranca

Referência operacional da Fase 6 do plano mestre. A fonte executável fica em `PermissionService`, `DatabaseService.AccessControl.cs` e nas tabelas `Permissoes`, `PerfisAcesso` e `PerfilPermissoes`.

## Login e Admin Inicial

- `LoginWindow` centraliza a autenticação por e-mail e senha.
- Senhas são persistidas por `PasswordHasherService` com PBKDF2, salt aleatório e verificação em tempo constante.
- O administrador inicial só é criado quando `Funcionarios` está vazio.
- A senha inicial do administrador é aleatória, gravada apenas no arquivo local `credenciais-iniciais-admin.txt` e nunca fica em texto puro no banco.
- `Funcionarios.ExigirTrocaSenha` força troca de senha antes de abrir o sistema quando o acesso usa senha temporária.
- Redefinições de senha feitas pela tela de funcionários gravam senha temporária e marcam troca obrigatória no próximo login.
- Tentativas inválidas são registradas em `LoginTentativasSeguranca` e bloqueiam temporariamente após 5 falhas.

## Perfis

| Perfil | Escopo |
| --- | --- |
| Administrador | Todos os módulos e todas as ações, incluindo configurações críticas. |
| Gerente | Operação completa sem `SISTEMA_CONFIGURAR` e sem administrar permissões críticas. |
| Vendedor | Atendimento, clientes, veículos, orçamentos, OS comercial, catálogo e agenda. |
| Caixa | PDV, caixa operacional, financeiro de consulta/exportação e relatórios. |
| Almoxarife/Estoquista | Estoque, fornecedores, catálogo, NF-e e relatórios operacionais. |
| Mecânico/Técnico | Veículos, ordens de serviço e agenda técnica. |
| Financeiro | Financeiro e relatórios financeiros. |

## Matriz por Modulo

| Perfil | Módulos e ações principais |
| --- | --- |
| Administrador | Todos os módulos: Ver, Criar, Editar, Excluir/Inativar, Exportar, Aprovar, Cancelar, Executar, Imprimir, Configurar. |
| Gerente | Dashboard: Ver. Clientes/Veículos/Fornecedores: Ver, Criar, Editar, Excluir. Orçamentos: Ver, Criar, Editar, Duplicar, Compartilhar, Exportar, Imprimir, Converter venda. OS: Ver, Editar, Aprovar, Avançar status. PDV: Ver, Registrar, Cancelar, Desconto, Pagamento misto, Suspender/Retomar, Caixa, Reimprimir. Estoque: Ver, Criar, Editar, Excluir, Ajustar, Ajustar preço, Inventariar, Aprovar negativo. NF-e: Executar. Financeiro/Relatórios: Ver, Exportar, Imprimir. Funcionários: Ver, Criar, Editar. Agendamentos: Ver, Criar, Editar, Cancelar, Reagendar, Duplicar, Gerar OS, Check-in, Check-out, Compartilhar, Exportar, Imprimir. |
| Vendedor | Dashboard: Ver. Clientes/Veículos: Ver, Criar, Editar. Orçamentos: Ver, Criar, Editar, Duplicar, Compartilhar, Exportar, Imprimir, Converter venda. OS: Ver, Editar, Aprovar, Avançar status. Catálogo: Ver, Revisar. Agendamentos: Ver, Criar, Editar, Reagendar, Duplicar, Gerar OS, Compartilhar, Exportar, Imprimir. |
| Caixa | Dashboard: Ver. PDV: Ver, Registrar venda, Cancelar venda, Pagamento misto, Suspender/Retomar, Abrir/Fechar caixa, Sangria, Suprimento, Reimprimir, Cancelar venda registrada. Financeiro: Ver, Exportar, Imprimir. Relatórios: Ver, Imprimir. |
| Almoxarife/Estoquista | Dashboard: Ver. Estoque: Ver, Criar, Editar, Ajustar, Inventariar. Catálogo: Ver, Importar, Revisar, Criar produto, Exportar. NF-e: Executar. Relatórios: Ver. Fornecedores: Ver, Criar, Editar. |
| Mecânico/Técnico | Dashboard: Ver. Veículos: Ver. OS: Ver, Editar, Avançar status. Agendamentos: Ver, Cancelar, Gerar OS, Check-in, Check-out. |
| Financeiro | Dashboard: Ver. Financeiro: Ver, Exportar, Imprimir. Relatórios: Ver, Exportar, Imprimir. |

## Auditoria Obrigatoria

| Evento | Ações auditadas |
| --- | --- |
| Login e sessão | `Login`, `FalhaLogin`, `LoginBloqueado`, `Logout`, `LogoutInatividade`, `TrocaSenhaObrigatoriaConcluida`, `TrocaSenhaObrigatoriaCancelada`. |
| Acesso negado | `PermissaoNegada`, gravado com usuário, perfil, alvo e código. |
| Exclusão ou bloqueio | `FuncionarioBloqueado`, `FuncionarioReativado`, `PerfilExcluido`, `PermissaoExcluida` e exclusões/inativações nos repositories dedicados. |
| Venda e PDV | `VendaRegistrada`, `VendaCanceladaCompleta`, `VendaReimpressaFallback`, suspensão e retomada de vendas. |
| Preço e estoque | `EntradaEstoqueDedicada`, `SaidaEstoqueDedicada`, `InventarioProduto`, `AjustePrecoLoteItem`, `SaidaVendaProduto`, `EstornoVendaProduto`. |
| Financeiro | `ContaPagarCriada`, `ContaPagarLiquidada`, `ContaReceberCriada`, `ContaReceberLiquidada`, `MovimentacaoCriada`, integrações de orçamento, OS e agendamento. |
| Backup e restauração | `BackupCriado`, `BackupAntesMigracao`, `BackupAntesAtualizacao`, `BackupRestaurado`, `BackupCopiadoRede`. |

## Validacao Automatizada

- `AccessControlPolicyTests` valida a matriz persistida por perfil.
- `LoginSecurityTests` valida admin inicial, hash PBKDF2, senha temporária e troca obrigatória.
- Smoke `LoginSessao:MensagensLockoutLogoutPermissoes` cobre mensagens amigáveis, lockout, logout, timeout e auditoria de permissão negada.
