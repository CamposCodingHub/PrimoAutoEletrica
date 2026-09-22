# PRIMOX Workshop — SECURITY AUDIT

> Auditoria de segurança real baseada em código fonte e execução.

---

## 1. Autenticação

### 1.1 API JWT Bearer ✅
- **AddAuthentication** + **AddJwtBearer** configurados em `Program.cs:65-94`
- Chave mínima 32 caracteres, validada em startup
- Chave placeholder (`DEV_ONLY_`/`CHANGE_ME`) bloqueada em Production
- Todos endpoints de negócio usam `RequireAuthorization(policy)`
- Health check marcado `AllowAnonymous` (correto)
- Token endpoint rate-limited

### 1.2 Hashing de Senhas ✅
- `PasswordHasherService`: PBKDF2-HMAC-SHA256, 600.000 iterações (OWASP ASVS 2023+)
- Salt de 16 bytes via `RandomNumberGenerator`
- Verificação com `CryptographicOperations.FixedTimeEquals` (timing-attack safe)
- Senhas plaintext armazenadas: **rejeitadas** (`IsHashed()` check)
- `NeedsRehash()` detecta hashes com iterações antigas

### 1.3 Sessão Desktop
- `SessionInactivityService` com timeout configurável
- `UserSessionService` com estado de autenticação
- `TwoFactorService` existe como scaffold (TOTP via Otp.NET)

---

## 2. Autorização

### 2.1 RBAC — Fail-Closed ✅
- `PermissionCheckResult` tristate: Allowed / Denied / Unavailable
- 22 operações críticas em `CriticalPermissionCodes` HashSet
- Unavailable em operação crítica → **bloqueada** (fail-closed)
- Fallback de perfil apenas para operações não-críticas sem linha persistida
- Perfis: Administrador, Gerente, Vendedor, Caixa, Almoxarife, Mecânico/Técnico, Financeiro

### 2.2 Operações Críticas Protegidas
```
CLIENTES_EXCLUIR, VEICULOS_EXCLUIR, ESTOQUE_EXCLUIR, ESTOQUE_AJUSTAR,
ESTOQUE_AJUSTAR_PRECO, ESTOQUE_PERMITIR_NEGATIVO, FUNCIONARIOS_EXCLUIR,
FUNCIONARIOS_EDITAR, FORNECEDORES_EXCLUIR, FINANCEIRO_EDITAR,
FINANCEIRO_EXCLUIR, FINANCEIRO_PAGAR, FINANCEIRO_RECEBER,
ORCAMENTOS_EXCLUIR, ORDENS_SERVICO_EXCLUIR, ORDEM_SERVICO_EXCLUIR,
AGENDAMENTOS_EXCLUIR, PDV_APLICAR_DESCONTO, PDV_CANCELAR_VENDA_REGISTRADA,
CAIXA_ABRIR, CAIXA_FECHAR, CAIXA_SANGRIA, CAIXA_SUPRIMENTO,
SISTEMA_CONFIGURAR, PERMISSOES_GERENCIAR
```

---

## 3. API Security

### 3.1 Error Handling ✅
- `SafeProblem()` nunca expõe `ex.Message` ao consumidor
- Mensagem pública genérica + log interno detalhado
- Teste `SafeProblem_NaoVazaMensagemInterna` verifica ausência de stack trace, SQLiteException

### 3.2 Rate Limiting ✅
- Fixed window: 120 requests/minuto por policy "api"
- `429 Too Many Requests` retornado

### 3.3 CORS ✅
- Policy "RestrictiveCors" com origens configuráveis
- Default: localhost apenas

---

## 4. Dados Sensíveis

### 4.1 LGPD
- `ConsentimentoLGPD` e `AutorizaContatoWhatsApp` verificados antes de contato WhatsApp
- Confirmação crítica (CriticalActionDialogService) para contato sem consentimento
- Auditoria registrada em cada tentativa

### 4.2 Logging
- **ATENÇÃO**: `result.Detail` em `PermissionCheckResult` contém apenas tipo da exception (e.g., `lookup_exception:InvalidOperationException`), nunca a mensagem completa
- Teste `Unavailable_Detail_NaoVazaMensagemDaException` verifica ausência de "segredo", "password", "SQLite"

---

## 5. Vulnerabilidades Identificadas

### 5.1 License Tampering — P0 ⚠️
Licença JSON local sem assinatura criptográfica. Manipulável.

### 5.2 Empty Catches — P2
13+ `catch { }` em Services podem mascarar falhas em operações de segurança, financeiro, sync.

### 5.3 DateTime.Now — P2
100+ arquivos usando `DateTime.Now` (local) em vez de UTC. Risco para multi-timezone, cloud, sync.

---

## 6. Cobertura de Testes de Segurança

| Teste | Tipo | Status |
|---|---|---|
| PermissionServiceFailClosedTests (10) | Real componente | ✅ |
| ApiJwtHttpTests (10) | Real HTTP | ✅ |
| PasswordHasherRealTests (14) | Real componente | ✅ NOVO |
| ApiJwtProductionBootstrapTests (4) | Real bootstrap | ✅ |
| ApiAuthenticationGateTests | Real gate | ✅ |
| HistoricoClienteFinancialIsolationTests | Source proof | ✅ |
| Primox360IdFinancialJoinTests | Source proof | ✅ |
| LicenseScaffoldHonestyTests | Honesty check | ✅ |
| SecurityTests (antigos) | Self-validating ❌ | Excluídos via Compile Remove |

### Testes PENDENTES (recomendados):
- Broken Access Control: tentativa de IDOR via API
- Privilege Escalation: usuário tenta alterar próprio perfil
- Tenant Isolation: quando multi-tenant for implementado
- Session Expiration: teste de timeout real
- 2FA: teste de TOTP quando ativado
