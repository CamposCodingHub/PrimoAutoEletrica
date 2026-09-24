# PRIMOX WORKSHOP — B3
## RELATÓRIO OFICIAL DE REGRESSÃO E SMOKE TEST DESKTOP

**Data:** 2026-09-24  
**Branch:** `audit/product-discovery-2026-09`  
**Resultado da Regressão:** **PASS (100% DE APROVAÇÃO)**

---

### 1. Execuções em Runtime Real

1. **Suíte Completa de Testes Automatizados:**
   - Comando: `dotnet test Tests/PrimoAutoEletrica.Tests/PrimoAutoEletrica.Tests.csproj`
   - Total de Testes: **394**
   - Aprovados: **394**
   - Falhas: **0**
   - Ignorados: **0**

2. **Smoke Test UI em Runtime do Executável Instalado:**
   - Comando: `PrimoAutoEletrica.exe --smoke-test`
   - Total de Verificações: **196**
   - Aprovadas: **196**
   - Falhas: **0**
   - Cobertura: 16 módulos de navegação, 31 janelas de diálogo, 14 controles de usuário, 8 testes de relatórios e exportações, e testes RBAC.

3. **Ciclo de Vida Diagnóstico A/B:**
   - Veículo Volvo FH (`MLB9J14`, Id: `d6b09217-5066-4d25-adaf-49a4d120f766`)
   - Diagnósticos D01 e D02 independentes, persistidos em JSON e vinculados a OSs distintas.
   - Resultado: **PASS**.

4. **Ciclo de Backup e Restore:**
   - Criação de backup, verificação de integridade, detecção e rejeição de arquivo adulterado/corrompido, e restauração com sucesso.
   - Resultado: **PASS**.
