# Runtime Reality Check

**Documento:** `PRIMOX_C1_1_1_RUNTIME_REALITY_REPORT.md`  
**Ciclo:** C1.1.1 (diagnóstico apenas — sem correção de código)  
**Data:** 2026-09-26 (America/Sao_Paulo)  
**Máquina:** CIRO (`de411c5d-4243-4085-bf33-8c3d241d7f2f`)  
**Regra:** C1.1 NÃO está operacionalmente fechada até o EXE real do usuário ser validado.

---

## 1. Objetivo

Explicar a divergência entre a certificação C1.1 (UiSmoke 206/206 PASS em 2026-09-25 21:35 BRT) e as falhas que o usuário continua vendo ao abrir o PRIMOX pelo atalho da Área de Trabalho.

Esta etapa **não** é C2, **não** altera `main`, **não** altera banco protegido, **não** corrige código.

Fluxo executado: EXE real → identidade → reprodução → comparação → causa → evidência.

---

## 2. EXE do usuário

| Campo | Valor |
|-------|-------|
| Desktop shortcut | `C:\Users\campo\OneDrive\Desktop\PRIMOX Workshop.lnk` |
| Target | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe` |
| Working directory | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App` |
| EXE path | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe` |
| EXE filename | `PrimoAutoEletrica.exe` |
| EXE SHA256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| EXE size | 203776 bytes |
| Last modified | 2026-09-25 20:01:48 |
| File version | 1.0.0.0 |
| Product version | 1.0.0 |
| **DLL path (código gerenciado)** | `...\App\PrimoAutoEletrica.dll` |
| **DLL SHA256** | `732CC75F91C254F87E41065A312A169F01FD64205702AEDEDC3124BFDBEA5F31` |
| **DLL size / mtime** | 5685760 bytes / 2026-09-25 20:01:48 |

**Nota crítica:** o host `.exe` é um stub. A identidade do produto está no `PrimoAutoEletrica.dll`. Comparar só o SHA do EXE **não** basta.

---

## 3. EXE do UiSmoke

UiSmoke 206/206 (evidência `QA_EVIDENCE/C1_1/ui_smoke_206_PASS_pre_reconfirm.txt`):

| Campo | Valor |
|-------|-------|
| GeneratedAt | 2026-09-25 21:35:12 |
| Resultado | 206/206 PASS |
| EXE path | `C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\bin\Release\net10.0-windows\PrimoAutoEletrica.exe` |
| App-data isolado | `TestResults\UiSmoke\2026-09-25_21-02-34\appdata` |
| Build configuration | **Release** (`net10.0-windows`, framework-dependent) |
| Publish directory | `bin\Release\net10.0-windows` (não o App instalado) |

Estado atual do mesmo path Release (pós-rebuild 21:46, ainda com marcadores C1.1):

| Campo | Valor |
|-------|-------|
| EXE SHA256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` (igual ao stub do App) |
| **DLL SHA256** | `91C2A806A63528A2E3808ADBEE58DD156CF2EF7CB40B273505777B24368BF436` |
| **DLL size / mtime** | 5688320 bytes / 2026-09-25 21:46:15 |

Marcadores de correção C1.1 no DLL Release: `Tabs.xaml=YES`, `_isInitialized=YES`.

---

## 4. Comparação SHA

| Item | Usuário (Desktop App) | UiSmoke (Release FDD) | Igual? |
|------|----------------------|------------------------|--------|
| Path EXE | `...\App\PrimoAutoEletrica.exe` | `...\bin\Release\net10.0-windows\PrimoAutoEletrica.exe` | **NÃO** |
| SHA256 EXE (stub) | `05D103E9…775B` | `05D103E9…775B` | SIM (enganoso) |
| SHA256 **DLL** | `732CC75F…5F31` | `91C2A806…F436` | **NÃO** |
| Size DLL | 5685760 | 5688320 | **NÃO** |
| mtime DLL | 2026-09-25 20:01:48 | 2026-09-25 21:46:15 | **NÃO** |
| `Tabs.xaml` no DLL | **NÃO** | **SIM** | **NÃO** |
| `_isInitialized` no DLL | **NÃO** | **SIM** | **NÃO** |

**TESTED_ARTIFACT ≠ USER_ARTIFACT** (no nível do DLL gerenciado).

O DLL do App é **idêntico** ao publish `win-x64` / `win-x64\publish` de **20:01:48** (pré-correção C1.1).

---

## 5. Comparação de versão

| Item | Usuário | UiSmoke Release | Igual? |
|------|---------|-----------------|--------|
| FileVersion | 1.0.0.0 | 1.0.0.0 | SIM (não discrimina) |
| ProductVersion | 1.0.0 | 1.0.0 | SIM (não discrimina) |
| Conteúdo efetivo (DLL) | pré-fix 20:01 | pós-fix 21:xx | **NÃO** |

---

## 6. Comparação de commit

| Artefato | Cadeia | Conclusão |
|----------|--------|-----------|
| Branch atual | `cycle-c1/operational-intelligence` @ `83b3ffa` | OK |
| Commit de correção | `57ef36c` — *C1.1: fix QA regressions…* (2026-09-26 07:09 BRT no git; build Release na noite de 25/09) | Ancestral do HEAD |
| `main` | `29b19b16d0e6e3413bdba20c505e20c992596c24` | Intacta (não alterada nesta etapa) |
| EXE/DLL do **usuário** | Sem `Themes/Tabs.xaml`, sem `_isInitialized` | **Não contém** o payload de `57ef36c` |
| DLL do **UiSmoke/Release** | Contém `Tabs.xaml` + `_isInitialized`; arquivos do fix: `Themes/Tabs.xaml`, `GlobalStyles.xaml`, `FerramentasControl.*`, `CatalogoPecasViewModel.cs` | Alinhado ao fix |

Não há SourceLink no EXE instalado. A prova é: **marcadores binários do fix ausentes no App e presentes no Release testado**.

---

## 7. Diretórios

| Papel | Diretório |
|-------|-----------|
| Atalho Desktop → App instalado | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\` |
| UiSmoke 206 | `...\PrimoAutoEletrica\bin\Release\net10.0-windows\` |
| Publish win-x64 (mesmo DLL do App) | `...\bin\Release\net10.0-windows\win-x64\` e `\publish\` @ 20:01:48 |
| Debug (não usado no 206) | `...\bin\Debug\net10.0-windows\` (DLL SHA distinto) |

---

## 8. Instalações encontradas

Inventário completo em `QA_EVIDENCE/C1_1_1/exe_inventory.txt`.

Resumo:

- **Muitas** cópias históricas em `AppData\Local\PrimoAutoEletrica\Backups\BeforeDeploy\install-*`
- App atual: SHA EXE `05D103…` / DLL `732CC75F…` (20:01)
- Release FDD atual: mesmo stub EXE, DLL `91C2A806…` (21:46)
- Várias gerações anteriores (`EE3784…`, `E5EE9D…`, `B86EDC…`, net6.0, etc.)

**Sim: existem múltiplos executáveis.** O atalho aponta para **App**, não para `bin\Release`.

---

## 9. Runtime .NET

| | App (usuário) | Release (UiSmoke) |
|--|---------------|-------------------|
| TFM | net10.0 | net10.0 |
| runtimeconfig | `includedFrameworks` 10.0.10 | `frameworks` 10.0.0 |
| DLLs no diretório | ~306–311 | ~64–68 |

---

## 10. Publish Mode

| Artefato | Modo |
|----------|------|
| Desktop App | **Self-contained** (framework embutido via `includedFrameworks`) |
| UiSmoke Release | **Framework-dependent** |

Isso reforça **deployment mismatch**, além do conteúdo do DLL.

---

## 11. Arquivos de publicação

- App: dezenas de assemblies de runtime + app; **sem** pasta Themes solta (recursos devem ir embutidos no DLL).
- Release FDD: app + deps menores; `Tabs.xaml` embutido no DLL Release.
- Diff qualitativo principal: **DLL gerenciado diferente** + modo publish diferente.
- Evidência: `QA_EVIDENCE/C1_1_1/file_count_compare.txt`, `app_runtimeconfig.json`.

---

## 12. Teste real do usuário

### 12.1 Sessão real (logs do usuário)

- Login e menu funcionam no App instalado (ex.: 2026-09-26 ~06:47–07:12).
- Navegação a várias páginas dispara erros (ver seções 13–18).
- Startup sozinho **não** é PASS funcional.

### 12.2 Smoke no EXE exato do Desktop

Comando (isolado, sem tocar DB protegido):

```text
C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe
  --smoke-test
  --app-data=...\QA_EVIDENCE\C1_1_1\smoke_APP_FULL\appdata
```

Resultado parcial (interrompido após falhas críticas, ~pass 127 / fail 4):

| Check | Resultado |
|-------|-----------|
| Controle:BaseConhecimentoControl | **FAIL** |
| Controle:FerramentasControl | **FAIL** |
| Controle:NecessidadesCompraControl | **FAIL** |
| Tema:ClaroEscuroModulosPrincipais | **FAIL** (BaseConhecimento não carregou) |
| Modulo:CatalogoPecas | PASS abertura, com **WARN BUG-002** |

Evidências: `QA_EVIDENCE/C1_1_1/smoke_APP_FULL/`, `smoke_APP_FULL_failures_summary.txt`.

---

## 13. Base de Conhecimento

| Critério | Resultado |
|----------|-----------|
| Tela abre? | **NÃO** |
| Classificação | **BUG-001** (ainda no artefato do usuário) |

**Mensagem exata (smoke Desktop EXE, 2026-09-26 07:23:07):**

- Outer: `TargetInvocationException`
- Inner: `XamlParseException` — StaticResourceExtension
- Root: `Não é possível encontrar o recurso denominado 'ModernTabControl'`
- Arquivo: `BaseConhecimentoControl.xaml` / `.xaml.cs:23`

---

## 14. Necessidades de Compra

| Critério | Resultado |
|----------|-----------|
| Tela abre? | **NÃO** |
| Classificação | **BUG-001** |

**Mensagem exata (usuário 2026-09-25 21:41:55 e smoke 2026-09-26 07:23:07):**

- `XamlParseException` + inner `ModernTabControl` não encontrado
- `NecessidadesCompraControl.xaml` / `.xaml.cs:21`

---

## 15. Ferramentas

| Critério | Resultado |
|----------|-----------|
| Tela abre? | **NÃO** |
| Classificação | **BUG-003** |

**Mensagem exata:**

- `NullReferenceException`
- `FerramentasControl.AplicarFiltros()` linha **87**
- `FiltroCombo_SelectionChanged` linha **206**
- Durante `InitializeComponent` / seleção inicial do ComboBox
- Sem `_isInitialized` no DLL do App (marcador da correção ausente)

---

## 16. Catálogo de Peças

| Critério | Resultado |
|----------|-----------|
| Tela abre? | **SIM** (navegação conclui) |
| BUG-002 residual? | **SIM** |

**Evidência:**

```text
Permissao negada. ... Tipo=Modulo; Alvo=ESTOQUE_CRIAR; Codigo=N/A
```

Observado no uso real (2026-09-25) e no smoke do App (2026-09-26).  
No código fonte pós-`57ef36c`: `TemPermissaoCodigo("ESTOQUE_CRIAR")`.  
No App instalado o comportamento ainda trata o código como **módulo** → **BUG-002** presente no artefato do usuário.

---

## 17. Outras páginas

| Página | No App do usuário | Notas |
|--------|-------------------|-------|
| Estoque | PASS (ex.: 549 produtos @ 21:41) | |
| FiscalOperacoes | PASS | |
| Orcamentos | **FAIL** | MoneyIO / DBNull índice 16 (ver §18) |
| OficinaKanban | **FAIL** | mesma cadeia MoneyIO |
| Cliente 360 / Vehicle 360 / Financeiro / Auto Elétrica | Não retestados um a um nesta etapa após as falhas críticas | |
| Tema Light/Dark nos módulos C1 | **FAIL** | Tema check falhou porque BaseConhecimento não carrega |

---

## 18. Popup/Exception

### BUG-001 (Necessidades / BaseConhecimento)

| Campo | Valor |
|-------|-------|
| Título / tipo | `System.Windows.Markup.XamlParseException` |
| Texto | O valor fornecido em `StaticResourceExtension` iniciou uma exceção |
| InnerException | `Não é possível encontrar o recurso denominado 'ModernTabControl'` |
| Thread | UI |
| Timestamp (usuário) | 2026-09-25 21:41:55 BRT |
| Timestamp (smoke App) | 2026-09-26 07:23:07 BRT |

### BUG-003 (Ferramentas)

| Campo | Valor |
|-------|-------|
| Tipo | `System.NullReferenceException` |
| Método | `FerramentasControl.AplicarFiltros` |
| Arquivo / linha | `FerramentasControl.xaml.cs:87` |
| Timestamp (usuário) | 2026-09-25 20:09:32 BRT |

### MoneyIO (NÃO é BUG-001/002/003)

| Campo | Valor |
|-------|-------|
| Tipo | `System.InvalidOperationException` |
| Texto | Coluna monetária no índice 16 retornou DBNull em campo NOT NULL |
| Método | `MoneyIO.LerMoeda` (`MoneyIO.cs:38`) |
| Cadeia | `OrcamentoDatabaseService.ObterTodosOrcamentos` → Orcamentos / OficinaKanban |
| Timestamp | 2026-09-25 20:09+ e **2026-09-26 06:47 / 07:11** (uso real no App) |
| Classificação | **NEW BUG** / estado do banco operacional — **fora do escopo de correção desta etapa** |

---

## 19. Stack Trace

Stacks completas salvas em:

- `QA_EVIDENCE/C1_1_1/user_errors_critical.txt`
- `QA_EVIDENCE/C1_1_1/bug001_moderntab_stack.txt`
- `QA_EVIDENCE/C1_1_1/bug003_ferramentas_stack.txt`
- `QA_EVIDENCE/C1_1_1/smoke_APP_FULL/appdata/Logs/log-2026-09-26.txt`
- `C:\Users\campo\AppData\Local\PrimoAutoEletrica\Logs\app-2026-09-25.log`
- `C:\Users\campo\AppData\Local\PrimoAutoEletrica\Logs\app-2026-09-26.log`

---

## 20. BUG-001

**Presente no EXE/DLL do Desktop.** Ausente (passou) no UiSmoke Release 206/206.  
Causa local: `Themes/Tabs.xaml` / merge em `GlobalStyles` **não** estão no DLL do App (`Tabs.xaml` string ausente).

---

## 21. BUG-002

**Presente no comportamento do Desktop App** (`ESTOQUE_CRIAR` como Tipo=Modulo).  
Corrigido no fonte/`57ef36c` via `TemPermissaoCodigo`; Release testado no 206 passou `Modulo:CatalogoPecas` / interações sem esse sintoma de regressão.

---

## 22. BUG-003

**Presente no Desktop App** (NRE linha 87).  
Marcador `_isInitialized` ausente no DLL do App; presente no Release.

---

## 23. Artifact mismatch

**CONFIRMADO.**

```text
TESTED_ARTIFACT = bin\Release\net10.0-windows (FDD) + DLL 91C2A806… (pós-fix)
USER_ARTIFACT   = AppData\Local\...\App (SCD) + DLL 732CC75F… (publish 20:01 pré-fix)
```

O UiSmoke 206/206 **não** validou o EXE que o atalho abre.

---

## 24. Deployment mismatch

**CONFIRMADO.**

- Atalho → App self-contained @ 20:01
- Certificação → Release FDD @ 21:02–21:35
- Após C1.1 **não** houve republish/redeploy para `AppData\Local\PrimoAutoEletrica\App`

---

## 25. Ambiente

- Mesma máquina CIRO para uso e testes.
- Smoke de certificação usou `--app-data` isolado (banco limpo de teste).
- Uso real usa o perfil/dados locais da oficina (operacional), onde MoneyIO/DBNull aparece.
- Ambiente sozinho **não** explica BUG-001/002/003 no App: o smoke isolado no **mesmo EXE do Desktop** reproduziu BUG-001 e BUG-003.

---

## 26. Banco

| Check | Resultado |
|-------|-----------|
| Path protegido | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db` |
| SHA256 | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` |
| Match esperado | **SIM** |
| ReadOnly | **True** |
| integrity_check | `ok` |
| foreign_key_check | vazio |
| Alterado nesta etapa? | **NÃO** |

MoneyIO em Orcamentos/Kanban aponta para **dados operacionais** (não o DB protegido), evidência de estado — documentado, não corrigido.

---

## 27. Git

| Item | Valor |
|------|-------|
| branch | `cycle-c1/operational-intelligence` |
| HEAD | `83b3ffa` |
| commit fix | `57ef36c` (ancestral) |
| main | `29b19b16d0e6e3413bdba20c505e20c992596c24` |
| main alterada? | **NÃO** |
| C2 iniciado? | **NÃO** |
| Commit de correção nesta etapa? | **NÃO** |

---

## 28. Causa provável

1. **Causa principal da divergência C1.1 vs usuário:** o atalho abre um **publish antigo** (DLL `732CC75F…` de 20:01) **sem** as correções C1.1; a certificação rodou contra **outro artefato** (Release FDD com as correções).
2. Por isso o usuário ainda vê **BUG-001, BUG-002 e BUG-003** no programa real.
3. **Adicional (não explica o trio C1.1, mas explica outras falhas de página):** `MoneyIO.LerMoeda` / DBNull no banco operacional ao abrir Orçamentos e Oficina Kanban.

---

## 29. Evidências

| Arquivo | Conteúdo |
|---------|----------|
| `QA_EVIDENCE/C1_1_1/desktop_shortcut.txt` | Target do .lnk |
| `QA_EVIDENCE/C1_1_1/artifact_identity.txt` | SHA App vs Release |
| `QA_EVIDENCE/C1_1_1/exe_inventory.txt` | Cópias de EXE |
| `QA_EVIDENCE/C1_1_1/app_runtimeconfig.json` | Self-contained |
| `QA_EVIDENCE/C1_1_1/protected_db.txt` | SHA DB |
| `QA_EVIDENCE/C1_1_1/smoke_APP_FULL/` | Reprodução no EXE do atalho |
| `QA_EVIDENCE/C1_1/ui_smoke_206_PASS_pre_reconfirm.txt` | Certificação 206/206 |
| Logs AppData `Logs\app-2026-09-25.log` / `app-2026-09-26.log` | Uso real |

---

## 30. Conclusão

### Classificação (com evidência)

| Código | Aplica? | Evidência |
|--------|---------|-----------|
| **B — EXE DIFERENTE** | **SIM (principal)** | DLL SHA distinto; marcadores C1.1 ausentes no App |
| **C — DEPLOYMENT DIFERENTE** | **SIM (principal)** | App SCD 20:01 vs Release FDD testado; atalho não aponta para Release |
| A — mesmo EXE, bug ainda existe | NÃO como causa da divergência | No Release testado, 206/206 passou os três |
| D — ambiente | Parcial só para MoneyIO | Não explica ModernTab/NRE no smoke isolado do App |
| E — configuração | Não primário | |
| F — banco/estado | **SIM secundário** | MoneyIO/DBNull no operacional |
| G — novo bug | **SIM secundário** | MoneyIO (fora do trio C1.1) |
| H — não reproduzido | **NÃO** | Trio C1.1 reproduzido no EXE do Desktop |

### Status operacional C1.1

**C1.1 NÃO está operacionalmente PASS.**

xUnit 483/483, E2E 42/42 e UiSmoke 206/206 validaram o artefato **Release**, não o **App** que o usuário abre.

### O que esta etapa NÃO fez

- Não corrigiu código  
- Não republicou/redeployou  
- Não iniciou C2  
- Não alterou `main`  
- Não alterou o banco protegido  

### Próximo passo sugerido (fora desta etapa)

Republicar/substituir `AppData\Local\PrimoAutoEletrica\App` com o artefato que contém o DLL pós-`57ef36c`, depois **revalidar as 4 páginas no EXE do atalho**. Só então C1.1 pode ser considerada operacionalmente fechada. Tratar MoneyIO/operacional em trilha separada.

---

**FIM — C1.1.1 Runtime Reality Check — PARAR.**
