# PRIMOX WORKSHOP — B5.1
## BASELINE DE IMPLEMENTAÇÃO: CHECKLIST MULTIPONTO + PÓS-VENDA

**Data/Hora:** 2026-09-24 12:20:00 -03:00  
**Branch:** `audit/product-discovery-2026-09`  
**Commit Inicial (B5.0):** `bf06eea` (`docs(product): define B5 commercial release architecture`)  
**Status da Baseline:** **PASS / REPRODUZÍVEL**

---

### 1. Parâmetros de Ambiente e Verificação de Integridade

| Parâmetro | Valor Verificado | Estado / Validação |
| :--- | :--- | :---: |
| **Branch Atual** | `audit/product-discovery-2026-09` | **CONFORME** (Não é `main`) |
| **Commit HEAD** | `bf06eea` | **PASS** |
| **Branch `main`** | Intocada em `29b19b1` | **PRESERVADA** |
| **Banco Produção** | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db` | **CONFORME** |
| **SHA-256 Produção** | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` | **100% EXATO** |
| **Atributo Produção** | `IsReadOnly = True` | **ATIVO** |
| **Banco Operacional** | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db` | **CONFORME** |
| **Integridade SQLite** | `PRAGMA integrity_check;` -> `[('ok',)]` | **OK** |
| **Foreign Keys SQLite** | `PRAGMA foreign_key_check;` -> `[]` | **0 Violações** |
| **Testes Automatizados** | 417 executados / 417 aprovados | **417 PASS / 0 FAIL / 0 SKIP** |
| **UI Smoke Baseline** | 196 verificações / 196 aprovadas | **196 PASS (APROVADO)** |
| **Desktop Executable** | `%LOCALAPPDATA%\PrimoAutoEletrica\App\PrimoAutoEletrica.exe` | Atualizado e validado |
| **Desktop Startups** | 3 execuções sucessivas via atalho | 3/3 PASS (Zero SQLite Error 8) |

---

### 2. Escopo da Fase B5.1

1. **Checklist Multiponto Comercial:**
   - Criação da interface visual dedicada `ChecklistTecnicoWindow` / `ChecklistTecnicoControl`.
   - Acesso direto a partir da Ordem de Serviço (`OrdemServicoWindow` e `OrdensServicoControl`).
   - Categorias estruturadas: Bateria, Alternador, Partida, Aterramento, Fusíveis, Relés, Iluminação, Circuitos, Sensores, Chicotes, Segurança.
   - Status estruturados (`OK`, `ATENCAO`, `CRITICO`, `NAO_SE_APLICA`, `NAO_DISPONIVEL`).
   - Medições com valor, unidade, momento (Antes/Depois) e cálculo de delta.
   - Integração com roteiros D01-D06 e laudo técnico.
   - Persistência contínua e integridade relacional por `OrdemServicoId`, `VeiculoId`, `ClienteId`.

2. **Pós-Venda Comercial:**
   - Criação da interface visual dedicada `PosVendaWindow` / `PosVendaControl`.
   - Acesso a partir da Ordem de Serviço, Client360 e Vehicle360.
   - Tipos e status de ocorrências estruturados (`PosVendaTipoEnum`, `PosVendaStatusEnum`).
   - Vínculo direto com Diagnóstico Técnico em caso de retorno ou reincidência.
   - Histórico cumulativo sem overwrite por `ClienteId`, `VeiculoId`, `OrdemServicoId`.
