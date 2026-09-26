# PRIMOX WORKSHOP — CICLO C1
# MATRIZ DE CONTROLE DE ACESSO BASEADO EM FUNÇÕES (C1 RBAC MATRIX)
**Data:** 2026-09-25  
**Ciclo:** C1 — Operational Intelligence + Tools + Purchasing + Knowledge + Assist Foundation  
**Princípio de Segurança:** FAIL-CLOSED / DENY BY DEFAULT  
**Status:** SPECIFICATION APPROVED  

---

## 1. POLÍTICA DE SEGURANÇA E PRINCÍPIOS RBAC

1. **Deny by Default:** Nenhum usuário obtém acesso a uma ação crítica ou módulo sem concessão explícita no seu perfil de acesso ativo.
2. **Auditabilidade:** Toda tentativa de acesso negado a operações críticas gera evento de aviso/auditoria no `AuditTrailService`.
3. **Imutabilidade de Perfis Protegidos:** Perfis do sistema como `Administrador` mantêm concessão integral, enquanto perfis operacionais (`Mecânico`, `Vendedor`, `Caixa`, `Gerente`) recebem permissões estritamente alinhadas com suas atribuições físicas na oficina.

---

## 2. NOVOS CÓDIGOS DE PERMISSÃO (CICLO C1)

| Código da Permissão | Módulo | Ação | Nome Amigável | Descrição | Crítica? |
|---|---|---|---|---|---|
| `FERRAMENTAS_VER` | Ferramentas | Visualizar | Ver Ferramentas | Permite visualizar o inventário e status de ferramentas | Não |
| `FERRAMENTAS_CRIAR` | Ferramentas | Criar | Cadastrar Ferramenta | Permite cadastrar nova ferramenta no patrimônio | Não |
| `FERRAMENTAS_EDITAR` | Ferramentas | Editar | Editar Ferramenta | Permite alterar dados, localização e foto da ferramenta | Sim |
| `FERRAMENTAS_RETIRAR` | Ferramentas | Executar | Retirar Ferramenta | Permite registrar saída/checkout de ferramenta para serviço | Não |
| `FERRAMENTAS_DEVOLVER` | Ferramentas | Executar | Devolver Ferramenta | Permite registrar devolução/checkin e condição de entrega | Não |
| `FERRAMENTAS_MANUTENCAO` | Ferramentas | Executar | Registrar Manutenção | Permite enviar ferramenta para reparo, calibração ou descarte | Sim |
| `FERRAMENTAS_EXCLUIR` | Ferramentas | Excluir | Excluir Ferramenta | Permite remover ferramenta do patrimônio | Sim |
| `COMPRAS_VER` | Compras | Visualizar | Ver Compras | Permite visualizar necessidades e pedidos de compra | Não |
| `COMPRAS_SOLICITAR` | Compras | Criar | Solicitar Compra | Permite criar requisição de compras ou aceitar sugestão | Não |
| `COMPRAS_APROVAR` | Compras | Aprovar | Aprovar Compra | Permite aprovação gerencial de pedidos de compra | Sim |
| `COMPRAS_PEDIR` | Compras | Executar | Formalizar Pedido | Permite formalizar o pedido com o fornecedor | Sim |
| `COMPRAS_RECEBER` | Compras | Executar | Receber Mercadoria | Permite conferir itens e dar entrada física no estoque | Sim |
| `COMPRAS_CANCELAR` | Compras | Cancelar | Cancelar Compra | Permite rejeitar ou cancelar solicitação de compra | Sim |
| `CONHECIMENTO_VER` | Conhecimento | Visualizar | Ver Base Técnica | Permite consultar manuais, boletins e casos de diagnóstico | Não |
| `CONHECIMENTO_CRIAR` | Conhecimento | Criar | Criar Artigo Técnico | Permite cadastrar procedimento ou caso real de diagnóstico | Não |
| `CONHECIMENTO_EDITAR` | Conhecimento | Editar | Editar Conhecimento | Permite revisar procedimentos técnicos e medições | Sim |
| `CONHECIMENTO_PUBLICAR`| Conhecimento | Publicar | Publicar Boletim | Permite aprovar e publicar caso na base oficial da oficina | Sim |
| `CONHECIMENTO_ARQUIVAR`| Conhecimento | Arquivar | Arquivar Conhecimento| Permite desativar procedimento descontinuado | Sim |
| `ASSIST_UTILIZAR` | Assist | Executar | Usar PRIMOX Assist | Permite consultar o assistente de diagnóstico contextual | Não |
| `ASSIST_CONFIGURAR` | Assist | Configurar | Configurar Assist | Permite administrar parâmetros e provedores do Assist | Sim |

---

## 3. MATRIZ DE ATRIBUIÇÃO POR PERFIL PADRÃO

| Permissão | Administrador | Gerente | Mecânico / Técnico | Vendedor | Caixa |
|---|:---:|:---:|:---:|:---:|:---:|
| `FERRAMENTAS_VER` | SIM | SIM | SIM | SIM | NÃO |
| `FERRAMENTAS_CRIAR` | SIM | SIM | NÃO | NÃO | NÃO |
| `FERRAMENTAS_EDITAR` | SIM | SIM | NÃO | NÃO | NÃO |
| `FERRAMENTAS_RETIRAR` | SIM | SIM | SIM | NÃO | NÃO |
| `FERRAMENTAS_DEVOLVER` | SIM | SIM | SIM | NÃO | NÃO |
| `FERRAMENTAS_MANUTENCAO` | SIM | SIM | SIM | NÃO | NÃO |
| `FERRAMENTAS_EXCLUIR` | SIM | SIM | NÃO | NÃO | NÃO |
| `COMPRAS_VER` | SIM | SIM | SIM | SIM | NÃO |
| `COMPRAS_SOLICITAR` | SIM | SIM | SIM | SIM | NÃO |
| `COMPRAS_APROVAR` | SIM | SIM | NÃO | NÃO | NÃO |
| `COMPRAS_PEDIR` | SIM | SIM | NÃO | NÃO | NÃO |
| `COMPRAS_RECEBER` | SIM | SIM | NÃO | NÃO | NÃO |
| `COMPRAS_CANCELAR` | SIM | SIM | NÃO | NÃO | NÃO |
| `CONHECIMENTO_VER` | SIM | SIM | SIM | SIM | NÃO |
| `CONHECIMENTO_CRIAR` | SIM | SIM | SIM | NÃO | NÃO |
| `CONHECIMENTO_EDITAR` | SIM | SIM | NÃO | NÃO | NÃO |
| `CONHECIMENTO_PUBLICAR` | SIM | SIM | NÃO | NÃO | NÃO |
| `CONHECIMENTO_ARQUIVAR` | SIM | SIM | NÃO | NÃO | NÃO |
| `ASSIST_UTILIZAR` | SIM | SIM | SIM | SIM | NÃO |
| `ASSIST_CONFIGURAR` | SIM | SIM | NÃO | NÃO | NÃO |
