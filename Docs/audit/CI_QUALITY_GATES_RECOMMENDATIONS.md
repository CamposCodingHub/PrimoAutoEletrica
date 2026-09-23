# PRIMOX Workshop — RECOMENDAÇÕES DE CI QUALITY GATES

> Este documento é uma recomendação formal. **NENHUM workflow foi alterado.**

---

## 1. SITUAÇÃO ATUAL

| Workflow | Gate? | Problema |
|---|---|---|
| `ci.yml` | ✅ SIM | Build + Test executam e falham o pipeline se quebrarem |
| `code-quality.yml` | ❌ NÃO | `continue-on-error: true` em StyleCop, FxCop, Complexity |
| `performance-security.yml` | ❌ NÃO | `continue-on-error: true` em análise |

### Consequência
- PR pode ser mergeado mesmo com vulnerabilidades de segurança detectadas
- Falsa sensação de "pipeline verde"
- Análises de qualidade são informativas, não bloqueantes

---

## 2. RECOMENDAÇÃO: ci-gate.yml

Criar um workflow consolidado que serve como gate real de branch protection.

```yaml
# .github/workflows/ci-gate.yml
name: CI Gate (Obrigatório)

on:
  pull_request:
    branches: [main, develop]
  push:
    branches: [main]

jobs:
  build-and-test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore PrimoAutoEletrica.sln

      - name: Build Release
        run: dotnet build PrimoAutoEletrica.sln --configuration Release --no-restore

      - name: Test
        run: dotnet test Tests/PrimoAutoEletrica.Tests/PrimoAutoEletrica.Tests.csproj --configuration Release --no-build --verbosity normal

      # Futuramente:
      # - name: Security Analysis
      #   run: dotnet format --verify-no-changes
      # - name: Coverage Threshold
      #   run: dotnet test --collect "XPlat Code Coverage" ...
```

---

## 3. BRANCH PROTECTION RULES

Configurar no GitHub:
- **Require status checks to pass before merging**: `ci-gate / build-and-test`
- **Require branches to be up to date before merging**: ✅
- **Require pull request reviews**: 1 (quando houver equipe)

---

## 4. REMOÇÃO GRADUAL DE continue-on-error

### Fase A (Informativo → Aviso)
- Manter `continue-on-error: true`
- Adicionar step final que gera summary com contagem de findings
- Se findings > threshold, gerar annotation de warning no PR

### Fase B (Aviso → Gate)
- Remover `continue-on-error: true` dos jobs críticos (segurança)
- Manter para complexidade e estilo (inicialmente)

### Fase C (Gate Completo)
- Todos os jobs sem `continue-on-error`
- Thresholds definidos por tipo:
  - Vulnerabilidades de segurança: 0
  - Complexidade ciclomática: max 25
  - Cobertura: min 60%

---

## 5. WORKFLOWS SECUNDÁRIOS

Os workflows `code-quality.yml` e `performance-security.yml` devem permanecer como informativos até que:
1. StyleCop rules estejam estabilizadas (sem false positives em massa)
2. FxCop rules estejam configuradas para o projeto
3. Baseline de complexidade esteja documentada

---

## 6. STATUS

| Item | Estado |
|---|---|
| Recomendação documentada | ✅ |
| Workflows alterados | ❌ NÃO ALTERADOS |
| Branch protection configurada | ❌ PENDENTE (requer acesso admin GitHub) |
