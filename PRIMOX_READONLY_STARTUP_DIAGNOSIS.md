# PRIMOX WORKSHOP — RELATÓRIO TÉCNICO DE DIAGNÓSTICO
## Inicialização da Área de Trabalho: SQLite Error 8 ("attempt to write a readonly database")

**Data do Diagnóstico:** 2026-09-23 19:55 BRT  
**Documento:** `PRIMOX_READONLY_STARTUP_DIAGNOSIS.md`  
**Status do Banco Real:** **INTACTO E INVIOLÁVEL**  
**Ação Imediata no Código/Banco:** **NENHUMA (Modificações suspensas até deliberação)**  

---

### Sumário Executivo

Ao executar o aplicativo através do atalho da **Área de Trabalho** (`PRIMOX Workshop.lnk`), a aplicação é interrompida imediatamente na inicialização com o erro:
```text
Microsoft.Data.Sqlite.SqliteException (0x80004005): SQLite Error 8: 'attempt to write a readonly database'.
```
O diagnóstico forense identificou com **100% de precisão e evidências materiais** que:
1. O executável da Área de Trabalho está apontando diretamente para o **banco de produção real oficial** (`primoauto.db`), localizado no `%LOCALAPPDATA%\PrimoAutoEletrica`.
2. O arquivo `primoauto.db` possui o atributo de sistema de arquivos NTFS **`ReadOnly = True`**.
3. Essa trava física de somente-leitura foi aplicada deliberadamente na sessão de trabalho no início da **Fase B1 (Product Discovery)** para cumprir a regra mandatória de segurança de impedir qualquer escrita, migração ou modificação acidental no banco real de produção durante os testes e auditorias.
4. Ao abrir a aplicação normalmente, o construtor de `DatabaseService` invoca `EnsureDatabaseInitialized()`, que executa rotinas de alinhamento de schema e higienização de índices de controle de acesso (`HardenAccessControlIndexes`).
5. A instrução DML `DELETE FROM Permissoes WHERE Id NOT IN (...)` tenta iniciar uma transação de escrita no SQLite. Como o arquivo subjacente do sistema operacional está marcado como `ReadOnly`, o SQLite bloqueia a operação e emite o erro `SQLite Error 8`.

---

## 1. Localização e Identificação do Arquivo de Banco de Dados

* **Caminho Absoluto do DB:**
  ```text
  C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db
  ```
* **Classificação do Banco:**
  **`primoauto.db` DE PRODUÇÃO REAL** (Banco oficial original da empresa).
  * *Não é banco de teste.*
  * *Não é banco de homologação.*
  * *Não é cópia local/descartável.*
* **Tamanho do Arquivo:** `20.201.472` bytes (20,2 MB).
* **Hash SHA-256 Calculado:**
  ```text
  C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B
  ```
* **Comparação com o SHA-256 Conhecido de Produção:**
  * Conhecido: `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
  * Encontrado: `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
  * **Resultado:** **COINCIDÊNCIA EXATA (100% IDÊNTICO)**. O banco de dados em questão é o banco real protegido.

---

## 2. Inspeção de Permissões, Atributos e Sistema Operacional

* **Atributos de Arquivo (Windows/NTFS):**
  * `Attributes`: `ReadOnly, Archive`
  * `IsReadOnly`: `True`
* **Permissões de Segurança NTFS (Access Control List - ACL):**
  * `Ciro\campo`: `FullControl` (Allow)
  * `AUTORIDADE NT\SISTEMA`: `FullControl` (Allow)
  * `BUILTIN\Administradores`: `FullControl` (Allow)
  * `Ciro\CodexSandboxUsers`: `ReadAndExecute, Synchronize` (Allow)
  * *Conclusão sobre Permissões:* O usuário do Windows possui permissões plenas de escrita NTFS na pasta e no arquivo. O bloqueio decorre exclusivamente da flag de atributo de arquivo `ReadOnly` do sistema operacional.

---

## 3. Configuração de Runtime e Conexão do Executável da Área de Trabalho

* **Atalho da Área de Trabalho:**
  * Caminho: `C:\Users\campo\OneDrive\Desktop\PRIMOX Workshop.lnk`
  * Destino (`TargetPath`): `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe`
  * Diretório de Trabalho (`WorkingDirectory`): `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App`
  * Argumentos de Linha de Comando (`Arguments`): *(vazio / sem parâmetros)*
* **Arquivo de Configuração Carregado:**
  * Caminho: `C:\Users\campo\AppData\Local\PrimoAutoEletrica\database-settings.json`
  * Conteúdo:
    ```json
    {
      "Provider": "SQLite",
      "SQLitePath": "primoauto.db",
      "SqlServerHost": ".\\SQLEXPRESS",
      "CommandTimeoutSeconds": 30,
      "IsSQLite": true,
      "IsSqlServer": false
    }
    ```
* **Resolução do Caminho pelo Runtime:**
  * O método `DatabaseConnectionSettings.ResolveSqlitePath(appDataPath)` combina o caminho padrão `%LOCALAPPDATA%\PrimoAutoEletrica` com `"primoauto.db"`.
  * Resultando em: `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db`.
* **String de Conexão Gerada em `DatabaseService.cs`:**
  ```text
  Data Source=C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db;Cache=Shared;Mode=ReadWriteCreate;Pooling=True;Default Timeout=30
  ```
  *(Segredos/credenciais: nenhum, banco SQLite local).*
* **SQLite Mode Configurado no Código:**
  * `SqliteOpenMode.ReadWriteCreate`
* **Parâmetro `Mode=ReadOnly` na Connection String:**
  * **Inexistente**. O código tenta abrir como leitura/escrita.
* **`PRAGMA query_only`:**
  * **Inexistente**. Não é executado pelo aplicativo.
* **Isolamento de Testes (`_testHostAppDataPath`):**
  * O código em `AppRuntimeConfiguration.cs` possui a verificação:
    ```csharp
    var isRunningInTest = AppDomain.CurrentDomain.GetAssemblies()
        .Any(a => {
            var n = a.GetName().Name ?? string.Empty;
            return n.Contains("xunit", StringComparison.OrdinalIgnoreCase) ||
                   n.Contains("testhost", StringComparison.OrdinalIgnoreCase);
        });
    ```
  * Ao abrir o aplicativo pelo atalho da Área de Trabalho, o processo é um executável comum (`PrimoAutoEletrica.exe`). Portanto, `isRunningInTest` é falso, `_testHostAppDataPath` não é ativado, e a aplicação direciona a execução para o `%LOCALAPPDATA%` padrão.
* **Variáveis de Ambiente:**
  * Nenhuma variável de ambiente de redirecionamento ou prefixada com `PRIMO*` está configurada no sistema operacional.

---

## 4. Análise Forense da Falha (Stack Trace e Comando SQL)

### Registro Forense do Arquivo de Log (`app-2026-09-23.log` às 19:45:41)
```text
DataHora=2026-09-23T19:45:41.6775939-03:00 | Nivel=CRITICAL | Acao=Falha ao inicializar infraestrutura. |
ErroTecnico=Microsoft.Data.Sqlite.SqliteException: SQLite Error 8: 'attempt to write a readonly database'.
StackTrace=
   at Microsoft.Data.Sqlite.SqliteException.ThrowExceptionForRC(Int32 rc, sqlite3 db)
   at Microsoft.Data.Sqlite.SqliteCommand.ExecuteNonQuery()
   at PrimoAutoEletrica.Services.DatabaseService.HardenAccessControlIndexes(DbConnection connection) in DatabaseService.AccessControl.cs:line 81
   at PrimoAutoEletrica.Services.DatabaseService.InitializeAccessControlSchema(DbConnection connection) in DatabaseService.AccessControl.cs:line 65
   at PrimoAutoEletrica.Services.DatabaseService.InitializeDatabase() in DatabaseService.cs:line 270
   at PrimoAutoEletrica.Services.DatabaseService.EnsureDatabaseInitialized() in DatabaseService.cs:line 182
   at PrimoAutoEletrica.Services.DatabaseService..ctor(...) in DatabaseService.cs:line 102
   at PrimoAutoEletrica.App.EnsureInfrastructureInitialized() in App.xaml.cs:line 770
   at PrimoAutoEletrica.App.OnStartup(StartupEventArgs e) in App.xaml.cs:line 213
```

### O Ponto Exato da Falha

1. **Método Responsável:**
   `DatabaseService.HardenAccessControlIndexes(DbConnection connection)`  
   (Arquivo: `PrimoAutoEletrica/Services/DatabaseService.AccessControl.cs`, linha 81).

2. **Comando SQL que Disparou o Erro:**
   ```sql
   DELETE FROM Permissoes
   WHERE Id NOT IN
   (
       SELECT MIN(Id)
       FROM Permissoes
       GROUP BY Codigo
   );
   ```

3. **Mecanismo da Falha:**
   * O método `DatabaseService.InitializeDatabase()` tenta garantir que a tabela `Permissoes` e `PerfisAcesso` não tenham duplicatas de código antes de criar índices únicos (`CREATE UNIQUE INDEX IF NOT EXISTS UX_Permissoes_Codigo`).
   * No SQLite, uma instrução `DELETE` obrigatoriamente requisita uma transação de escrita (adquirindo lock e gravando no arquivo/journal).
   * Como o sistema operacional informou ao subsistema SQLite que o arquivo `primoauto.db` possui atributo `ReadOnly`, a tentativa de abrir o arquivo para gravação é rejeitada pelo SQLite com o código `SQLITE_READONLY` (8).

---

## 5. Origem da Configuração ReadOnly e Relação com a Fase B1/B2

1. **Quando e por que o atributo foi aplicado?**
   * Foi aplicado no início da **Fase B1 (Product Discovery / Auditoria)** às **18:56:29 BRT** (21:56:29 UTC) de 23/09/2026.
   * O prompt da Fase B1 exigia categoricamente:
     > *"REGRA PRINCIPAL: O banco real continua obrigatoriamente intacto. NÃO migrar o banco real. NÃO alterar dados. NÃO executar UPDATE/DELETE/INSERT no banco real."*
   * Para garantir de forma 100% mecânica que nenhuma execução acidental alterasse o banco de produção durante os relatórios de discovery, o atributo de arquivo foi ativado via script:
     ```powershell
     Set-ItemProperty -Path C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db -Name IsReadOnly -Value $true
     ```
2. **Relação com o Deploy da Área de Trabalho:**
   * Quando o executável de Release foi compilado e publicado para a pasta `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\`, o aplicativo continuou lendo o `database-settings.json` padrão da pasta mãe.
   * A Área de Trabalho estava, portanto, operando diretamente sobre o banco real de produção protegido.
   * Ao abrir a aplicação, ela tentou realizar o ciclo de vida normal de startup (`EnsureDatabaseInitialized`), que colidiu frontalmente com a proteção temporária de auditoria.

---

## 6. Mecanismos Existentes de Separação de Bancos no PRIMOX

O código-fonte do PRIMOX Workshop já conta com **3 mecanismos arquiteturais** de separação de ambientes de banco de dados:

| Mecanismo | Onde está implementado | Como opera |
| :--- | :--- | :--- |
| **1. Parâmetro `--app-data=<caminho>`** | `AppRuntimeConfiguration.cs:35` | Permite que qualquer atalho ou inicializador passe `--app-data="C:\Caminho\Custom"`, isolando banco, configurações e logs por completo. |
| **2. Configuração `SQLitePath`** | `DatabaseConnectionSettings.cs:34` | O arquivo `database-settings.json` aceita caminhos absolutos ou nomes de arquivos alternativos, como `"SQLitePath": "primoauto_operacional.db"` ou `"SQLitePath": "primoauto_homologacao.db"`. |
| **3. Flags de Testes Automatizados** | `AppRuntimeConfiguration.cs:52` e `DatabaseService.cs:46` | `--smoke-test` e `--workflow-test` isolam compulsoriamente a base em subpastas de `AutomatedTests` e recusam abrir o AppData de produção com `InvalidOperationException`. |

---

## 7. Conclusão Diagnóstica

* **O erro não decorre de corrupção de banco de dados.**
* **O erro não decorre de falha de migração ou script quebrado.**
* **O banco de dados real está 100% íntegro e intocado** (SHA-256 idêntico ao original: `C7420D18...CE0B`).
* A causa raiz é a coexistência de:
  1. A trava de proteção NTFS (`ReadOnly`) ativada intencionalmente na Fase B1 para auditoria; com
  2. O atalho da Área de Trabalho apontando para o banco de produção real, executando um startup que requer validação de escrita de schema (`DatabaseService.InitializeDatabase`).

---

## 8. Opções de Correção Recomendadas (Para Decisão do Usuário)

Como a diretriz do usuário é **"NÃO CORRIGIR AINDA. Primeiro apresentar o diagnóstico."**, as alternativas técnicas são submetidas para aprovação:

### Opção A (Recomendada se o objetivo é operar normalmente no banco real):
* **Remover o atributo ReadOnly do arquivo de produção real:**
  ```powershell
  Set-ItemProperty -Path 'C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db' -Name IsReadOnly -Value $false
  ```
* **Efeito:** O aplicativo da Área de Trabalho abre normalmente, completa o `EnsureDatabaseInitialized()` e exibe a tela de login para uso regular.
* **Segurança:** O banco continuará no formato LegacyReal (sem alterações monetárias), pois as migrações CentsV1 permanecem bloqueadas.

### Opção B (Recomendada se o usuário quer manter o banco de produção blindado e operar em Homologação/Operacional):
* Criar uma cópia operacional do banco para a Área de Trabalho (ex: `primoauto_operacional.db`), configurando o `database-settings.json` para apontar para ele:
  ```json
  "SQLitePath": "primoauto_operacional.db"
  ```
* O banco de produção `primoauto.db` permanece com atributo `ReadOnly` e inviolável, e o atalho da Área de Trabalho roda sobre a cópia operacional liberada para escrita.

---
*Relatório de diagnóstico concluído. Aguardando decisão do usuário para prosseguir.*
