# PRIMOX Workshop — Fase B5.2
## Baseline Oficial: Commercial Installer + Update + Recovery Validation

**Data:** 2026-09-24 18:32:00-03:00  
**Branch:** `audit/product-discovery-2026-09`  
**HEAD:** `6e65fa9` — `feat(ui): harden checklist and pos-venda navigation and window ownership`  
**Branch main:** `29b19b1` (Intacta / 100% protegida)  

---

### 1. Status Inicial dos Componentes
- **Fase B5.0:** PASS (Arquitetura comercial, matriz de features, escopo técnico)
- **Fase B5.1:** PASS (Checklist multiponto 12V/24V + Pós-venda comercial + integrações 360)
- **Suíte de Testes Automatizados (xUnit):** 432/432 PASS (0 Falhas, 0 Ignorados)
- **UI Smoke Test Real (Release win-x64):** 200/200 APROVADO
- **Desktop Startups:** 3/3 PASS via atalho oficial, zero SQLite Error 8
- **Modos de Tema:** Light PASS, Dark PASS
- **Resoluções Testadas:** 1280x720 PASS, 1366x768 PASS, 1920x1080 PASS

---

### 2. Integridade dos Bancos de Dados
- **Banco de Produção:**
  - Caminho: `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db`
  - SHA-256: `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
  - ReadOnly: `True` (Protegido contra qualquer escrita)
- **Banco Operacional:**
  - Caminho: `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db`
  - PRAGMA integrity_check: `ok`
  - PRAGMA foreign_key_check: `0` violações (lista vazia)
  - PRAGMA user_version: `0`

---

### 3. Escopo e Metas da Fase B5.2
1. Transformação em pacote instalável comercial via Inno Setup (`primox_setup.iss`).
2. Validação da regra de ouro:
   - Limpeza e instalação em simulação de máquina limpa.
   - First run sem erros com banco operacional inicializado.
   - Criação de dados de homologação com hash registrado.
   - Update por cima com preservação estrita de dados e configurações.
   - Backup timestamped antes do update.
   - Teste de recovery e restauração de dados.
   - Teste de desinstalação (dados preservados, binários removidos).
   - Teste de reinstalação com reconhecimento dos dados existentes.
   - Desktop shortcut apontando para a pasta instalada oficial.
   - Validação da suíte de 432+ testes e smoke de 200 checks.
