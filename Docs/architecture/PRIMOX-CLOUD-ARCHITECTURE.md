# PRIMOX — Arquitetura Cloud / Multi-tenant / SaaS (FASE A)

**Status:** ARCHITECTURE PROPOSAL — **NÃO IMPLEMENTAR INFRA** nesta fase  
**Produto atual:** WPF desktop + SQLite local (v1.0.0)  
**API atual:** minimal ASP.NET, **sem** JWT/policies efetivas  

---

## 1. Princípio

```text
desktop sólido → integrações → fiscal → multi-filial → sync → API robusta → multi-tenant → SaaS → billing
```

Não transformar WPF em SaaS.  
Não abandonar modo local.

```text
PRIMOX Desktop
 ├── modo local (SQLite) — default 1.x
 └── modo conectado (opt-in)
         → API
         → PostgreSQL multi-tenant
```

---

## 2. Multi-filial (antes de SaaS)

### Modelo conceitual

```text
Empresa (ou Tenant futuro)
 └── Filial*
      ├── Usuários / Perfis
      ├── Estoque / Caixa
      ├── OS / Vendas / Financeiro
      └── Documentos fiscais
```

### Ownership (avaliar antes de `FilialId` em tudo)

| Entidade | Isolamento típico |
|----------|-------------------|
| Estoque, Caixa, Vendas, OS | Filial |
| Funcionários | Filial ou Empresa |
| Clientes / Veículos | Empresa (compartilhável) **ou** Filial — **decisão de produto** |
| Configuração fiscal | Filial (CNPJ) |
| Catálogo peças | Empresa |

`FilialService` atual = SCAFFOLD; Guid novos a cada `CarregarFiliaisAsync` — **não** é multi-filial.

### Critério REAL multi-filial

empresa + filiais persistidas + isolamento + permissões + estoque/caixa/financeiro/relatórios/auditoria **testados**.

---

## 3. Multi-tenant

```text
Tenant A ⟂ Tenant B  (zero leak)
  └── Filiais...
```

Testes obrigatórios: usuário A → recurso B = **403/404**.

Campos: `TenantId` no servidor; cliente desktop autentica e recebe claims.

---

## 4. API alvo (não o estado atual)

| Capacidade | Atual | Alvo |
|------------|-------|------|
| Host | Minimal maps | Versioned `/api/v1` |
| Auth | Ausente | JWT (ou session) real |
| RBAC | UseAuthorization sem policies | Policies **aplicadas** |
| Health | Existe | + readiness/deps |
| Tests | HTTP parcial/frágil | 200/401/403/404/409/422 |

Text-scan de `Program.cs` ≠ teste de API.

---

## 5. SaaS foundation (só após multi-tenant)

Onboarding · planos · trial · suspensão · limites · backup cloud · monitoramento · CorrelationId/TenantId/UserId nos logs (sem PII/secrets).

### Billing (depois)

`TRIAL → ACTIVE → PAST_DUE → GRACE → SUSPENDED → CANCELLED`  
Webhooks: auth, idempotência, log, retry — **sandbox only** até GO.

---

## 6. Dados

| Ambiente | Store |
|----------|-------|
| Desktop 1.x | SQLite AppData (preservar) |
| Cloud | PostgreSQL (proposta) |
| Migrations | Nunca apagar histórico SQLite sem auditoria (27 código vs ~32 DB) |

Backup/restore desktop já REAL; cloud exige teste periódico de restore.

---

## 7. Observabilidade mínima cloud

`CorrelationId, TenantId, FilialId, UserId, Operation, Duration, Result, Error`  
Proibido: senha, token, certificado, segredo.

---

## 8. Decisões bloqueantes

1. Clientes compartilhados entre filiais?  
2. Hosting (Azure/AWS/self-host)?  
3. IdP próprio vs externo?  
4. Horizonte comercial SaaS (sim/não/quando)?  
5. Quem opera suporte multi-tenant?

Até lá: **não** criar infra cloud definitiva.

---

## 9. Relação com v1.0.0

v1.0.0 permanece **desktop workshop**.  
Cloud/SaaS = versões futuras (`1.1+` / `2.0`) com commits e tags novos — **nunca** redefinir `v1.0.0`.
