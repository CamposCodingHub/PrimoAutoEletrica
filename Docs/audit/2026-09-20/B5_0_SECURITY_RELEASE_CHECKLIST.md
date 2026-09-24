# PRIMOX WORKSHOP — B5.0
## CHECKLIST DE SEGURANÇA E CONFORMIDADE TÉCNICA LGPD

**Escopo:** Proteção de Dados, Criptografia, Auditoria e Integridade  
**Status do Checklist:** **APROVADO PARA RELEASE**

---

### 1. Checklist de Segurança de Aplicação e Dados

| Item de Verificação | Padrão Exigido | Implementação no PRIMOX | Status |
| :--- | :--- | :--- | :---: |
| **Armazenamento de Senhas** | Hash criptográfico com sal aleatório | PBKDF2 com HMAC-SHA256, 10.000 iterações e sal de 128 bits | **CONFORME** |
| **Proteção de Segredos Locais** | Criptografia nativa de SO | Windows Data Protection API (DPAPI) via `ProtectedData` | **CONFORME** |
| **Banco de Dados SQLite** | Acesso restrito ao processo | Conexão com lock exclusivo em escrita e permissão de pasta | **CONFORME** |
| **Autorização de Usuários** | Modelo Fail-Closed estrito | Toda permissão não mapeada resulta em `DENY` imediato | **CONFORME** |
| **Token de API REST** | Assinatura JWT Bearer com expiração | Claims validados por endpoint; chave de assinatura protegida | **CONFORME** |
| **Sanitização de Logs** | Proibição de dados confidenciais | Regex de ofuscação de senhas, tokens, PINs e dados de cartão | **CONFORME** |
| **Proteção contra Brute Force**| Bloqueio temporário por tentativas | Contador de falhas de autenticação com delay progressivo | **CONFORME** |
| **Isolamento de Produção** | Bloqueio de gravação indevida | Produção `primoauto.db` marcado como `IsReadOnly = True` | **CONFORME** |

---

### 2. Requisitos Técnicos de Conformidade com a LGPD (Lei 13.709/2018)

O PRIMOX Workshop implementa os requisitos técnicos de custódia e privacidade:

1. **Gestão de Consentimento Auditável:**
   - O cadastro de clientes possui campo estruturado `ConsentimentoLGPD` (Data/Hora, Versão do Termo, Aceite do Titular).
   - O termo autoriza explicitamente o envio de lembretes de revisão preventiva e histórico veicular.
2. **Direito de Acesso e Portabilidade dos Dados:**
   - Funcionalidade no `Client360` para exportar a ficha cadastral completa, veículos e histórico de OS em formato estruturado (JSON ou CSV).
3. **Anonimização e Direito ao Esquecimento:**
   - Rotina técnica para anonimizar clientes inativos que solicitarem exclusão, desvinculando nome, CPF, telefone e endereço, mantendo apenas os registros contábeis/fiscais despersonalizados para atendimento aos prazos legais do Código Tributário Nacional.
4. **Log de Acesso a Dados Pessoais:**
   - Toda consulta ou alteração de ficha de cliente registra evento na tabela `AuditLogs` com UsuárioId, Timestamp e IP/Máquina.
