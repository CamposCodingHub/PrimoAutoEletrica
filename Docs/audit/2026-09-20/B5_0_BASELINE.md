# PRIMOX WORKSHOP — B5.0
## BASELINE DE DESCOBERTA E ARQUITETURA DE RELEASE

**Data/Hora:** 2026-09-24 12:10:00 -03:00  
**Branch:** `audit/product-discovery-2026-09`  
**Commit:** `72e46ac` (`feat(product): complete B4 product evolution and commercial hardening`)  
**Status da Baseline:** **PASS / REPRODUZÍVEL**

---

### 1. Parâmetros de Ambiente e Verificação de Integridade

| Componente | Especificação / Valor Registrado | Estado / Status |
| :--- | :--- | :--- |
| **Branch Atual** | `audit/product-discovery-2026-09` | CONFORME (Não é `main`) |
| **Commit HEAD** | `72e46ac` | PASS |
| **Branch `main`** | Intocada (zero commits) | PRESERVADA |
| **Banco Produção** | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db` | CONFORME |
| **SHA-256 Produção** | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` | 100% EXATO |
| **Atributo Produção** | `IsReadOnly = True` | ATIVO |
| **Banco Operacional** | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db` | ATIVO (`IsReadOnly = False`) |
| **Projetos na Solution** | 6 Projetos (.sln oficial) | BUILD RELEASE 0 ERROS |
| **Target Framework** | .NET 10 (`net10.0-windows` / `net10.0`) | CONFORME |
| **Testes xUnit** | 417 executados / 417 aprovados | **417 PASS / 0 FAIL / 0 SKIP** |
| **UI Smoke Automated** | 196 verificações / 196 aprovadas | **196 PASS / 0 FAIL (APROVADO)** |
| **Desktop Executable** | `%LOCALAPPDATA%\PrimoAutoEletrica\App\PrimoAutoEletrica.exe` | Atualizado (24/09/2026 08:09:05) |
| **Atalho Desktop** | `C:\Users\campo\OneDrive\Desktop\PRIMOX Workshop.lnk` | Apontando para o binário real |
| **Desktop Startups** | 3 execuções sucessivas via atalho | 3/3 PASS (Zero SQLite Error 8) |

---

### 2. Bloqueadores Conhecidos e Deliberados

1. **`ProductionEmissionAllowed = false`**: Emissão fiscal de produção para SEFAZ permanece estritamente bloqueada no código ([FiscalDatabaseService.cs](file:///c:/Projetos/PrimoAutoEletrica/PrimoAutoEletrica/Services/FiscalDatabaseService.cs)).
2. **Migração Física de Money em Produção Bloqueada**: O banco `primoauto.db` mantém seus dados históricos inalterados. Toda operação utiliza `MoneyIO` em memória.
3. **Servidor de Licenciamento Remoto**: Mantido em scaffold fail-closed (`IsCommercialScaffoldOnly = true`). Não há servidor de produção implementado.
4. **Hardware Scanner / PassThru**: Leituras ao vivo de barramento CAN/OBD2 via J2534 ou ELM327 classificadas como dependência externa/futura.

---

### 3. Conclusão da Baseline
A baseline B5.0 está perfeitamente estabilizada, idêntica ao estado de encerramento da B4, permitindo o avanço das atividades de arquitetura de release.
