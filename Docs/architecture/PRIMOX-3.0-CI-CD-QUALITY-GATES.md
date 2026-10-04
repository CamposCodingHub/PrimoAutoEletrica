# PRIMOX 3.0 — CI/CD QUALITY GATES & PIPELINE SPECIFICATION
**Data:** 04 de Outubro de 2026  
**Versão:** 3.0.0-CICD  
**Status:** POLÍTICA DE ENGENHARIA DE INTEGRAÇÃO E ENTREGA CONTÍNUA  
**Princípio:** Qualidade não é opcional; pipelines permissivos geram falsos positivos de confiabilidade.

---

## 1. INVENTÁRIO DOS WORKFLOWS ATUAIS

| Workflow | Arquivo | Responsabilidade | Vulnerabilidade / Desvio Identificado |
| :--- | :--- | :--- | :--- |
| **CI/CD Pipeline** | `.github/workflows/ci.yml` | Build, Testes, CodeQL, Artefatos | `continue-on-error: true` no CodeQL (linha 156); Pula UITests com filtro (linha 73); Setup do .NET fixo em 9.0. |
| **Code Quality** | `.github/workflows/code-quality.yml` | Formatação e linters | Execução isolada que não bloqueia merge se CI principal passar. |
| **Integration Tests** | `.github/workflows/integration-tests.yml` | Testes de integração | Scripts Windows executados em runners genéricos sem banco provisionado. |
| **Performance & Security** | `.github/workflows/performance-security.yml`| Análise estática | Ausência de gates de regressão de memória e latência real. |
| **Release** | `.github/workflows/release.yml` | Empacotamento de versão | Gera pacotes sem assinatura Authenticode ativa (Readiness apenas). |

---

## 2. POLÍTICA DE QUALITY GATES (BLOCKING VS. NON-BLOCKING)

Para garantir que nenhum código quebrado, vulnerável ou com regressão atinja a branch principal (`main`), os gates são categorizados formalmente:

```
┌────────────────────────────────────────────────────────────────────────┐
│                        QUALITY GATES DO PRIMOX 3.0                     │
├─────────────────────┬──────────────────────────────────────────────────┤
│ CATEGORIA           │ COMPONENTES E GATES                              │
├─────────────────────┼──────────────────────────────────────────────────┤
│ 🛑 BLOCKING         │ 1. Restauração NuGet sem erros de vulnerabilidade│
│ (Bloqueia PR/Merge) │ 2. Compilação Release com ZERO warnings críticos │
│                     │ 3. 100% de aprovação na suíte de testes unitários│
│                     │ 4. CodeQL Security Scan sem vulnerabilidades altas│
│                     │ 5. Verificação de integridade de migrações SQL   │
├─────────────────────┼──────────────────────────────────────────────────┤
│ ⚠️ NON-BLOCKING     │ 1. Cobertura de testes de código (Target >= 80%) │
│ (Alerta no PR)      │ 2. Testes de fumaça de UI (emulados)             │
│                     │ 3. Linters de formatação e convenções de código  │
├─────────────────────┼──────────────────────────────────────────────────┤
│ ℹ️ INFORMATIONAL   │ 1. Relatório de tamanho do binário empacotado    │
│ (Métrica / Log)     │ 2. Registro de tempo de build por etapa          │
│                     │ 3. Alertas de descontinuação de dependências     │
└─────────────────────┴──────────────────────────────────────────────────┘
```

---

## 3. CORREÇÕES OBRIGATÓRIAS NO `ci.yml`

1. **Remoção de `continue-on-error` no Scan de Segurança:**
   * **Antes:**
     ```yaml
     security-scan:
       needs: code-quality
       continue-on-error: true
     ```
   * **Depois:**
     ```yaml
     security-scan:
       needs: code-quality
       continue-on-error: false # Falhas de segurança críticas DEVEM quebrar a esteira
     ```

2. **Atualização da Action do CodeQL:**
   * Migrar de `github/codeql-action/init@v2` para `@v3` (v2 descontinuada pelo GitHub).

3. **Atualização do SDK no Runner:**
   * Alterar `dotnet-version: 9.0.x` para `10.0.x` para refletir o SDK oficial do projeto.

---

## 4. PIPELINE DE RELEASE COM ASSINATURA REAL (CODE SIGNING)

Conforme a regra do produto: *"Nunca considerar readiness como assinatura efetiva"*.

```
[Build Release]
      │
      ▼
[Execução de Testes Automatizados]
      │
      ▼
[Publicação Binária (Publish)]
      │
      ▼
[Assinatura Digital Authenticode (Sign-PRIMOX.ps1)]
  ├── SE Certificado A1 / Key Vault Configurado: Assina DLLs e EXE
  └── SE Certificado Ausente: BLOQUEIA criação de instalador oficial de produção
      │
      ▼
[Verificação de Assinatura (signtool verify)]
      │
      ▼
[Geração de Hashes SHA-256 e Empacotamento de Distribuição]
```
