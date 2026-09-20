# B — HIGH_PRIORITY (Phase 0)

## P1-01 — PBKDF2 iterations abaixo do OWASP

| Campo | Valor |
|-------|-------|
| ID | P1-01 |
| PRIORIDADE | P1 |
| CATEGORIA | Segurança / Crypto |
| ARQUIVO | `PrimoAutoEletrica/Services/PasswordHasherService.cs` |
| PROBLEMA | Iterations estava em 100_000; OWASP recomenda ≥600_000 para PBKDF2-HMAC-SHA256. |
| EVIDÊNCIA | CODE: const Iterations; `NeedsRehash` + upgrade no login (`DatabaseService` ~906). |
| RISCO | Offline cracking mais barato. |
| CORREÇÃO | Neste PR: bump para 600_000; rehash gradual via caminho existente. |
| STATUS | MITIGADO (código) — validar login Windows. |
| TESTE | Hash novo com 600k; senha antiga 100k ainda verifica e regrava. |

## P1-02 — Results.Problem com ex.Message na API

| ID | P1-02 | PRIORIDADE | P1 | STATUS | ABERTO |
| ARQUIVO | Program.cs | PROBLEMA | Detalhe interno em 500s | CORREÇÃO | ProblemDetails genérico + log server-side |

## P1-03 — LicenseService SHA256 local

| ID | P1-03 | PRIORIDADE | P1/P0 comercial | STATUS | ABERTO (sem fake server) |
| Ver | A_CRITICAL_BUGS P0-04 |

## P1-04 — App.Services / DatabaseService god-object

| ID | P1-04 | PRIORIDADE | P1 | STATUS | ABERTO |
| CATEGORIA | Arquitetura | RISCO | Testabilidade e acoplamento |

## P1-05 — Suites de teste fragmentadas / asserts fracos

| ID | P1-05 | PRIORIDADE | P1 | STATUS | PARCIAL |
| EVIDÊNCIA | SecurityTests com helpers inventados; novos testes reais PermissionService adicionados |

## P1-06 — Dinheiro REAL no SQLite

| ID | P1-06 | PRIORIDADE | P1 | STATUS | ABERTO |
| CORREÇÃO | Planejar INTEGER cents / DECIMAL — **sem migration destrutiva neste PR** |

## Atualizacao 2026-09-20 (audit tip d3bdfc7+)

| ID | Status atualizado |
|----|-------------------|
| P1-01 PBKDF2 600k | **MITIGADO** (confirmado) |
| P1-02 ex.Message API | **MITIGADO** — `SafeProblem` + 0 `ex.Message` em Program.cs |
| P1-03 License server | **ABERTO** — scaffold flag `IsCommercialScaffoldOnly` |
| P1-04 God-object DI | **ABERTO** |
| P1-05 Suites | **PARCIAL** — oficial = Tests/; raiz LEGADO |
| P1-06 Money cents | **ABERTO** — mapa P0.10 |
| JWT API | **MITIGADO no audit** — ainda **NÃO em main** |
