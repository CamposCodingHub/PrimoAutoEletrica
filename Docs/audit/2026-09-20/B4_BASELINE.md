# PRIMOX WORKSHOP
## FASE B4 — BASELINE TÉCNICA E OPERACIONAL

**Data:** 24/09/2026  
**Hora:** 07:50 BRT  
**Ambiente:** Windows 10/11 x64  
**Branch:** `audit/product-discovery-2026-09` (Confirmado: NÃO é `main`)  
**Commit Baseline (HEAD):** `42bbadcb613cadcf27b0bb094f0b9c0f8f130cd2`  

---

### 1. ESTRUTURA DA SOLUÇÃO & PROJETOS

Solução: `PrimoAutoEletrica.sln`  
Total de Projetos: 6

| Projeto | Target Framework | Propósito |
|---|---|---|
| `PrimoAutoEletrica\PrimoAutoEletrica.csproj` | `net10.0-windows` | Core Application WPF / MVVM |
| `PrimoAutoEletrica.Api\PrimoAutoEletrica.Api.csproj` | `net10.0` | API REST / Microservices |
| `Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj` | `net10.0` | Testes Unitários e Integração xUnit |
| `PrimoAutoEletrica.UiTests\PrimoAutoEletrica.UiTests.csproj` | `net10.0-windows` | Testes de Interface FlaUI / Automation |
| `Tools\DbConfigurator\DbConfigurator.csproj` | `net10.0` | Ferramenta de configuração de banco |
| `Tools\LocalSyncSimulator\LocalSyncSimulator.csproj` | `net10.0` | Simulador de sincronização local |

---

### 2. BANCOS DE DADOS & ISOLAMENTO

- **Banco de Produção (Protegido):**
  - Caminho: `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db`
  - SHA-256 Esperado: `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
  - SHA-256 Calculado: `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` (100% Idêntico)
  - ReadOnly: `True` (Travado contra qualquer alteração/DDL)
- **Banco Operacional / Homologação:**
  - Caminho: `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db`
  - ReadOnly: `False`
  - Total de Tabelas: 58
  - Total de Registros: 57.976
  - Configuração: `Desktop DB != Production DB` (Garantido em `database-settings.json`)

---

### 3. SUÍTE DE TESTES AUTOMATIZADOS (BASELINE)

- **Comando:** `dotnet test Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj`
- **Resultados:**
  - Total: 394
  - Passed: 394
  - Failed: 0
  - Skipped: 0
  - Duração: 17.2s
- **Status do Build:** 0 Erros, 0 Warnings impeditivos (Release win-x64 compilado com sucesso).

---

### 4. DESKTOP & INSTALAÇÃO OPERACIONAL

- **Executável:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe`
- **Tamanho:** 203.776 bytes
- **Atalho da Área de Trabalho:** `C:\Users\campo\OneDrive\Desktop\PRIMOX Workshop.lnk`
- **Testes de Startup via Atalho:** 3/3 PASS, zero `SQLite Error 8`.

---

### 5. LIMITAÇÕES CONHECIDAS DO AMBIENTE (HONESTIDADE)

1. `FiscalProductionGuard.ProductionEmissionAllowed = false` (Ambiente fiscal em homologação; emissão de produção SEFAZ bloqueada por segurança).
2. `LicenseService.IsCommercialScaffoldOnly = true` (Licenciamento offline scaffold para demonstração técnica local, sem DRM SaaS em nuvem).
3. `Money Migration` definitiva para MoneyCents no banco de produção permanece rigorosamente bloqueada.
