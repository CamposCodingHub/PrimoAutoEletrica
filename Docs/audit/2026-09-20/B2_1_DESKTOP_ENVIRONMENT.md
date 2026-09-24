# PRIMOX WORKSHOP — B2.1
## AMBIENTE DE HOMOLOGAÇÃO DO DESKTOP E STARTUP

**Data:** 2026-09-24  
**Branch:** `audit/product-discovery-2026-09`  
**Status:** **PASS**

---

### 1. Especificação do Atalho da Área de Trabalho

A verificação do atalho real utilizado pelo operador foi conduzida com inspeção COM WScript.Shell:

- **Caminho do Atalho:** `C:\Users\campo\OneDrive\Desktop\PRIMOX Workshop.lnk`
- **Destino (Target):** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe`
- **Diretório de Trabalho (WorkingDirectory):** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App`
- **Argumentos:** *(nenhum - execução em modo operacional de produção)*
- **Ambiente de Runtime:** .NET 10.0-windows / WPF
- **Modo de Distribuição:** Self-Contained (`win-x64`)
- **Versão Instalada:** Release compilado a partir da branch `audit/product-discovery-2026-09`

---

### 2. Configuração de Armazenamento e Bancos

- **Arquivo de Configurações:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\database-settings.json`
- **Caminho do Banco Ativo:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db`
- **Banco Baseline Imutável:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db` (`IsReadOnly = True`, SHA intacto)
- **Modo de Conexão SQLite:** `Data Source=...;Mode=ReadWriteCreate;Cache=Shared`

---

### 3. Teste de Startup (3 Execuções Reais pelo Atalho)

O procedimento de validação consistiu em abrir e encerrar a aplicação real 3 vezes consecutivas através do executável instalado e atalho da Área de Trabalho, inspecionando o carregamento dos subsistemas essenciais (Splash, Autenticação, Hardening de Acesso e Shell Principal):

| Execução | PID do Processo | Tempo de Carregamento | Ocorrência de SQLite Error 8 | Janela Inicial | Resultado |
|:---:|:---:|:---:|:---:|:---:|:---:|
| **Execução 1** | PID 3424 | ~1,8 s | **NÃO** (0 exceções) | `LoginWindow` / Shell | **PASS** |
| **Execução 2** | PID 5352 | ~1,2 s | **NÃO** (0 exceções) | `LoginWindow` / Shell | **PASS** |
| **Execução 3** | PID 28108 | ~1,1 s | **NÃO** (0 exceções) | `LoginWindow` / Shell | **PASS** |

#### Verificações Adicionais de Startup:
- `SQLite Error 8 (attempt to write a readonly database)`: **ZERO OCORRÊNCIAS**
- `database locked`: **ZERO OCORRÊNCIAS**
- `database malformed`: **ZERO OCORRÊNCIAS**
- `corrupt database`: **ZERO OCORRÊNCIAS**
- `unhandled exception`: **ZERO OCORRÊNCIAS**
- `crash de inicialização`: **ZERO OCORRÊNCIAS**
