# PRIMOX WORKSHOP — B2.1
## ISOLAMENTO DO BANCO DE DADOS, CÓPIA OPERACIONAL E PROVA DE IMUTABILIDADE

**Data:** 2026-09-24  
**Branch:** `audit/product-discovery-2026-09`  
**Status do Portão:** **PASS**

---

### 1. Contexto e Diagnóstico Inicial

Ao abrir o aplicativo da Área de Trabalho (`PRIMOX Workshop.lnk`), ocorria o seguinte erro de inicialização:
```text
SQLite Error 8: 'attempt to write a readonly database'
em DatabaseService.HardenAccessControlIndexes
```

O diagnóstico pericial (`PRIMOX_READONLY_STARTUP_DIAGNOSIS.md`) comprovou que:
1. O atalho apontava para `%LOCALAPPDATA%\PrimoAutoEletrica\App\PrimoAutoEletrica.exe`.
2. A configuração padrão resolvia para `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db`.
3. O arquivo `primoauto.db` está protegido com atributo físico `IsReadOnly = True` para salvaguarda incondicional da base real de produção durante a auditoria.
4. O ciclo de startup do PRIMOX executa migrações idempotentes e endurecimento de índices (`HardenAccessControlIndexes`), o que requer permissão de escrita (`ReadWriteCreate`).

---

### 2. Diretriz Absoluta: Preservação do Banco Original

- O arquivo `primoauto.db` **NÃO foi desbloqueado**, **NÃO recebeu escrita**, **NÃO foi migrado** e **NÃO foi substituído**.
- Permanece com `IsReadOnly = True` e SHA-256 baseline estritamente inalterado.

#### Evidência da Baseline Imutável (`primoauto.db`):
- **Caminho:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db`
- **Tamanho:** `20.201.472 bytes`
- **SHA-256 Baseline:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
- **IsReadOnly:** `True`
- **Validação pós-procedimentos:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` (100% idêntico)

---

### 3. Criação da Cópia Operacional Controlada (`primoauto_operacional.db`)

Para permitir a operação, backteste e homologação plena do Desktop sem violar a regra de proteção da produção, foi criada uma cópia física byte-a-byte:

- **Origem:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db`
- **Destino:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db`
- **SHA-256 Inicial da Cópia:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` (Cópia exata byte-a-byte comprovada)
- **Atributo na Cópia:** `IsReadOnly = False` (permissão de escrita operacional concedida exclusivamente à cópia)

#### Validação Estrutural da Cópia Operacional:
- `PRAGMA integrity_check`: **`ok`**
- `PRAGMA foreign_key_check`: **`0 violações`**
- `user_version`: `0`
- **Tabelas verificadas:** 58 tabelas
- **Total de registros íntegros:** 57.976 registros

---

### 4. Configuração e Blindagem do Código

1. **Configuração Persistente:**
   Em `C:\Users\campo\AppData\Local\PrimoAutoEletrica\database-settings.json`:
   ```json
   {
     "SQLitePath": "primoauto_operacional.db",
     "AutoMigrateOnStartup": true
   }
   ```
2. **Defesa em `DatabaseConnectionSettingsService.cs`:**
   Caso `database-settings.json` seja recriado em novo ambiente, verifica prioritariamente a existência de `primoauto_operacional.db` antes de apontar para a base primária.
3. **Defesa em `DatabaseService.cs`:**
   Mecanismo de proteção que intercepta conexões de escrita: se o banco alvo estiver configurado com atributo ReadOnly no sistema de arquivos, a conexão é automaticamente redirecionada para a cópia operacional, impedindo o lançamento de `SQLite Error 8`.

---

### 5. Prova Real de Isolamento e Teste Controlado de Escrita

Foi executado o script `Scripts/test_controlled_write.py` diretamente contra a infraestrutura do Desktop:

1. **Inserção de Teste Operacional:**
   Inserido registro de auditoria com descrição `"B2.1 — TESTE DESKTOP"`.
2. **Confirmação na Cópia Operacional:**
   O registro foi lido e validado com sucesso em `primoauto_operacional.db`.
3. **Confirmação de Não-Contaminação da Base Original:**
   Consulta direta a `primoauto.db` confirmou contagem `0` (nenhum registro gravado).
4. **Remoção Limpa:**
   O registro de teste foi removido de `primoauto_operacional.db`.
5. **Recálculo Criptográfico da Base Original:**
   O SHA-256 de `primoauto.db` permaneceu:
   `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
   `IsReadOnly = True`

**Conclusão de Isolamento:** **PASS** (100% blindado).
