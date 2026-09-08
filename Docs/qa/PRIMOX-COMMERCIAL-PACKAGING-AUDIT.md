# PRIMOX Workshop — Commercial Packaging Audit (Fase 15A)

**Data:** 2026-09-08  
**Escopo:** SOMENTE auditoria — nenhum instalador/código/schema alterado nesta fase.  
**Versão de produto (Assembly):** `1.0.0`  
**Tag Git:** `v1.0.0` → commit `a4ad6fe`  
**HEAD no momento da auditoria:** `5695f67` (docs pós-tag; 1 commit à frente de `v1.0.0`)  
**Branch:** `main`  

### Divergência de estado vs. referência do briefing

| Esperado | Observado | Classificação |
|----------|-----------|---------------|
| HEAD `a4ad6fe` | HEAD `5695f67` | **DOCUMENTATION/STATE NOTE** — tag `v1.0.0` continua em `a4ad6fe`; HEAD tem só docs do gate |
| Versão `1.0.0` | Assembly InformationalVersion `1.0.0` (Debug/LocalAppData App) | **PASS** no binário RC/promovido |
| WIP preservado | HelpControl modificado; Scripts deploy untracked | **PRESERVADO** (não tocado) |

---

## 1. Respostas objetivas (checklist §27)

| # | Pergunta | Resposta |
|---|----------|----------|
| 1 | Qual instalador existe? | **Inno Setup** (`Installer/PrimoAutoEletrica.iss`) + mecanismos paralelos (ver §3) |
| 2 | Como é construído? | `Installer/build-installer.ps1` → `dotnet publish` + `ISCC.exe` (Inno 6). **ISCC não está instalado nesta máquina.** |
| 3 | O que instala? | Conteúdo de `bin/Release/net9.0-windows/win-x64/publish` + Docs (script ISS) |
| 4 | Onde instala? | `{commonpf}\Primo Auto Elétrica` (= Program Files) + pastas em `{commonappdata}` (criadas pelo ISS) |
| 5 | Atalhos? | Menu Iniciar + Desktop/QuickLaunch opcionais; Deploy script cria `Primo Auto Eletrica.lnk` |
| 6 | Desinstalação? | Inno `unins000.exe` + registro ARP; remove programa; **dados AppData em geral não entram em UninstallDelete** (só Temp em commonappdata) |
| 7 | Versão que o instalador declara? | `#define AppVersion "1.0.0"` / registro HKLM Version=1.0.0 (instalação existente) |
| 8 | Versão que o EXE informa? | **LocalAppData App:** ProductVersion=`1.0.0`. **Program Files:** ProductVersion=`0.0.0.0` → **VERSIONING ISSUE** |
| 9 | Onde fica o banco? | Runtime padrão: `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` |
| 10 | Como o banco é criado? | Na inicialização (`DatabaseService` + settings); cria pasta AppData e arquivo se necessário |
| 11 | Como o schema é criado? | Código: `DatabaseService.Migrations` / `SchemaMigrations` + `SchemaVersion` |
| 12 | Existe migration? | **SIM** — 27 migrações versionadas no código (smoke DB: 27 aplicadas) |
| 13 | Existe backup? | **SIM** — `DatabaseBackupService` (manual + automático ~24h + startup) |
| 14 | Existe restore? | **SIM** — UI/configurações de backup (não reexecutado nesta auditoria) |
| 15 | Existe atualização? | **PARCIAL** — `UpdateService` + manifesto; Inno/README descrevem update; **sem pipeline comercial fechado** validado |
| 16 | Uninstall preserva dados? | **SIM na prática esperada** para LocalAppData (não listado em UninstallDelete). **Risco:** dados em `{app}\Data`/`Logs` se misturados |
| 17 | Self-contained? | **Intenção Inno/Deploy:** sim (`--self-contained`). Workflow GitHub Release: ZIP self-contained. App em Program Files observada: self-contained |
| 18 | Dependências? | Self-contained: runtime embutido. Framework-dependent (opção Deploy `-FrameworkDependent`): Desktop Runtime. **Scripts ainda citam .NET 9** enquanto csproj é **net6.0-windows** |
| 19 | Instalador reproduzível? | **NO / PARTIAL** — ver §24 |
| 20 | O que está pronto? | App 1.0.0 versionado; tag Git; DB runtime em LocalAppData; migrations; backup; atalho LocalAppData funcional |
| 21 | O que falta? | Alinhar TFM/publish paths; Inno ↔ app real; identidade PRIMOX vs legado; artefato Setup 1.0.0 gerado; assinatura; smoke install limpo |
| 22 | Riscos? | Dual install paths; EXE PF `0.0.0.0`; TFM net9 vs net6; build-installer path; logs sob BaseDirectory; branding legado |
| 23 | Depois? | Fase 15B (decisão): unificar packaging — **não nesta fase** |

---

## 2. Inventário de mecanismos de instalação/deploy

### A) Inno Setup (oficial documentado)

| Campo | Valor |
|-------|-------|
| Arquivo | `Installer/PrimoAutoEletrica.iss` |
| Tecnologia | **Inno Setup 6.x** |
| Build | `Installer/build-installer.ps1` |
| Saída esperada | `Releases/PrimoAutoEletrica-Setup-{Version}.exe` + `SHA256.txt` |
| Pasta Releases no repo | **MISSING** (nenhum Setup.exe versionado/gerado localmente) |
| ISCC nesta máquina | **MISSING** |
| Evidência de uso passado | `C:\Program Files\Primo Auto Elétrica\unins000.exe` + ARP + HKLM `CamposCodingHub\Primo Auto Elétrica` |

**Comportamento ISS (estático):**
- `PrivilegesRequired=admin`
- `DefaultDirName={commonpf}\Primo Auto Elétrica`
- Fontes: `..\PrimoAutoEletrica\bin\Release\net9.0-windows\win-x64\publish\*`
- Ícone setup: `PrimoAutoEletrica\icon.ico`
- Publisher: CamposCodingHub
- Cria dirs em `{commonappdata}\Primo Auto Elétrica\{Data,Logs,Config,Backups,Media...}`
- UninstallDelete: apenas `{commonappdata}\...\Temp`
- **Não** assina digitalmente no script
- Backup/update automático no `[Code]` = **stub** (“pode ser implementado”)

### B) Deploy-ToInstalledApp.ps1 (WIP local, untracked)

| Campo | Valor |
|-------|-------|
| Caminho | `Scripts/Deploy-ToInstalledApp.ps1` (+ `Atualizar-PrimoAuto.bat`) |
| Tecnologia | PowerShell + `dotnet publish` |
| Destino padrão | `%LOCALAPPDATA%\PrimoAutoEletrica\App` |
| TFM | Lido do **csproj atual** (`net6.0-windows`) |
| Self-contained | default `true` (opção `-FrameworkDependent`) |
| Atalho | `Primo Auto Eletrica.lnk` → exe em LocalAppData\App |
| Dados | **não apaga** AppData; preserva pastas Data/Logs/Config/Backups/Media no install dir se existirem |
| Registro ARP / unins | **não** |

### C) New-WindowsInstallerPackage.ps1

Pacote ZIP/pasta + scripts `Install-PrimoAutoEletrica.ps1` / Update. Default Framework **`net9.0-windows`** (desalinhado). Instala em Program Files + DataDirectory LocalAppData. **Não** é Inno.

### D) GitHub Actions `release.yml`

- Trigger: tags `v*.*.*`
- Setup SDK **9.0.x**
- Publica WPF/API self-contained ZIP (não chama Inno)
- Cria GitHub Release com ZIPs
- **gh CLI não disponível** nesta máquina → Release remoto **NOT TESTABLE** aqui
- Tag local `v1.0.0` existe; push/release externo **não** feito nesta auditoria

### E) Outros

- `Scripts/Create-CleanPackage.ps1`, `Test-CleanInstallSimulation.ps1`, `New-DeliveryPackage.ps1` — auxiliares
- `ARCHITECTURE.md` menciona ClickOnce (legado documental; **sem** projeto ClickOnce encontrado)
- Nenhum WiX/MSIX/NSIS/Squirrel/Velopack encontrado

---

## 3. Fluxo real (as-is)

```
Código (net6.0-windows)
  → dotnet build/publish (vários scripts divergentes)
    → [A] Inno ISCC → Setup.exe → Program Files + ARP  (TFM net9 no ISS — quebra se publish for net6)
    → [B] Deploy-ToInstalledApp → %LOCALAPPDATA%\PrimoAutoEletrica\App + atalho Desktop
    → [C] ZIP package scripts → cópia manual/Program Files
    → [D] GitHub Actions → ZIP no Release (não Inno)
```

**Comandos reais (documentados nos scripts — não executados destrutivamente nesta fase):**

```powershell
# Intenção Inno (build-installer.ps1)
dotnet publish -c Release -r win-x64 --self-contained
# → espera: PrimoAutoEletrica\bin\Release\net9.0-windows\win-x64\publish
ISCC.exe Installer\PrimoAutoEletrica.iss /DAppVersion=1.0.0 /O...\Releases

# Deploy local (Deploy-ToInstalledApp.ps1)
dotnet publish PrimoAutoEletrica.csproj -c Release -r win-x64 -o ...\net6.0-windows\win-x64\publish --self-contained true
# → copia para %LOCALAPPDATA%\PrimoAutoEletrica\App
```

---

## 4. Projeto .NET / versionamento EXE

| Propriedade | Valor |
|-------------|-------|
| TargetFramework | **net6.0-windows** |
| GenerateAssemblyInfo | **false** |
| Version props no csproj | **ausentes** |
| Directory.Build.props | **não existe** |
| AssemblyVersion / FileVersion | `1.0.0.0` (`Properties/AssemblyInfo.cs`) |
| InformationalVersion | `1.0.0` |
| PublishSingleFile / Trimmed | não no csproj (Actions usa SingleFile no publish) |
| SelfContained | via CLI, não PropertyGroup fixa |

| Binário | ProductVersion | Classificação |
|---------|----------------|---------------|
| `bin\Debug\net6.0-windows\PrimoAutoEletrica.exe` | `1.0.0` | PASS vs tag |
| `%LOCALAPPDATA%\PrimoAutoEletrica\App\PrimoAutoEletrica.exe` | `1.0.0` | PASS (deploy recente) |
| `Program Files\Primo Auto Elétrica\PrimoAutoEletrica.exe` | **`0.0.0.0`** | **VERSIONING ISSUE** / instalação desatualizada |

---

## 5. Banco de dados (arquitetura)

### Runtime (código)

| Item | Valor |
|------|-------|
| Nome arquivo padrão | `primoauto.db` |
| Pasta AppData | `%LOCALAPPDATA%\PrimoAutoEletrica` (`AppRuntimeConfiguration.CreateDefault`) |
| Connection | `database-settings.json` → `SQLitePath` relativo → `ResolveSqlitePath(appDataPath)` |
| Criação | `Directory.CreateDirectory` + provider SQLite na init |
| Schema | `DatabaseService` + `DatabaseService.Migrations.cs` |
| Migrations | IDs `202605210001` … `202609060001` (**27** no código) |
| Smoke isolation | AutomatedTests sob BaseDirectory; **bloqueia** AppData produção |
| SQL Server | suportado via settings (fora do foco SQLite default) |

### Locais observados nesta máquina

| Local | Papel |
|-------|-------|
| `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` | **Banco runtime ativo** (atalho → App LocalAppData) |
| `%LOCALAPPDATA%\PrimoAutoEletrica\Backups\` | Backups de app |
| `%LOCALAPPDATA%\PrimoAutoEletrica\App\` | **Program files do usuário** (deploy B) |
| `Program Files\Primo Auto Elétrica\` | Instalação Inno legado/self-contained + `unins000` |
| Inno `{commonappdata}\Primo Auto Elétrica\...` | Dirs criados pelo ISS — **não** são o AppDataPath do código |
| `AppContext.BaseDirectory\Logs` | Logs (config runtime) — **mistura programa/dados** |

### Integridade (somente cópias seguras / leitura)

| DB | integrity_check | foreign_key_check | SchemaMigrations |
|----|-----------------|-------------------|------------------|
| Smoke AutomatedTests `primoauto.db` | **ok** | **ok** | 27 |
| LocalAppData `primoauto.db` | **ok** | **ok** | 32 |

**Nota:** produção LocalAppData reportou 32 entradas em `SchemaMigrations` vs 27 no código atual — **divergência a investigar em fase futura** (não corrigida). Sem exposição de dados pessoais; apenas COUNT/estrutura.

### Tabelas (smoke — inventário)

Agendamento*, Alertas*, AuditLogs, Auditoria, CaixaSessoes, Catalogo*, Clientes, ConfiguracoesSistema, ContasPagar/Receber, ContatosFornecedor, DatabaseBackups, Fornecedores, Funcionarios, ImportacoesNFe*, LoginTentativasSeguranca, Metas*, Movimentacoes*, Orcamento*, OrdemServico*, Perfis/Permissoes, Produto*, RecordLocks/RegistroBloqueios, Relatorios, SchemaMigrations/SchemaVersion, Timeline, UserSessions, Veiculos, Venda*, sqlite_sequence.

### Relacionamentos principais (lógicos + índices)

| Relação | Evidência |
|---------|-----------|
| Cliente → Veículo | `IX_Veiculos_ClienteId` |
| Cliente/Veículo → OS / Orçamento | FKs/IDs no domínio + smokes Fase 13/14 |
| OS → Itens / Eventos | tabelas `OrdemServicoItens`, `OrdemServicoEventos` |
| Produto → Estoque / VendaItens / Fornecedores | índices Produtos / VendaItens / ProdutoFornecedores |
| Agenda → Cliente/Veículo/OS | tabelas Agendamento* |
| Financeiro → contas / movimentações | Contas*/Movimentacoes* |
| NF-e import | ImportacoesNFe* (emissão real NOT TESTABLE) |

Orphan risk: soft-delete (`IsDeleted`) em várias entidades — FKs SQLite nem sempre `ON DELETE`; risco residual **KNOWN** em domínio legado.

---

## 6. Programa vs dados

| Tipo | Local típico | Separado? |
|------|--------------|-----------|
| EXE/DLLs (Deploy B) | LocalAppData\App | Parcial (AppData) |
| EXE/DLLs (Inno) | Program Files | SIM (programa) |
| SQLite / configs JSON | LocalAppData\PrimoAutoEletrica | SIM (dados) |
| Logs | **BaseDirectory\Logs** (junto ao EXE) | **NÃO** (misturado) |
| Backups | LocalAppData\...\Backups | SIM |
| Media/anexos | LocalAppData\...\Media | SIM |
| PDFs/export user | Documents\PrimoAutoEletrica\... | SIM |

**Conclusão:** intenção “dados em AppData” existe, mas **logs no BaseDirectory** e possível `Data` sob pasta de instalação → **PARTIAL / RISK**.

---

## 7. Backup / Restore / Update

| Capacidade | Estado | Evidência |
|------------|--------|-----------|
| Backup manual | SIM | `DatabaseBackupService` + UI Configurações |
| Backup automático | SIM | timer 24h + startup |
| Backup rede | CONDITIONAL | `NetworkBackupDirectory` em settings |
| Restore | SIM (código/UI) | não revalidado E2E nesta fase |
| Update in-app | PARTIAL | `UpdateService` + manifesto local |
| Update via Inno reinstall | DOCUMENTADO | Code stubs; não validado |
| Update via Deploy script | SIM (dev) | sobrescreve App; preserva dados AppData |

**UPDATE MECHANISM (comercial fechado / auto-update publicado):** **NOT IMPLEMENTED** como produto completo — apenas peças.

---

## 8. Identidade / branding

| Elemento | Valor | Classificação |
|----------|-------|---------------|
| Assembly Product | PRIMOX Workshop | OK (1.0.0) |
| Inno AppName | Primo Auto Elétrica | LEGACY / INCONSISTENCY |
| Atalho | Primo Auto Eletrica.lnk | LEGACY |
| Publisher registro | CamposCodingHub | OK/legado org |
| Pasta PF | Primo Auto Elétrica | LEGACY |
| Pasta AppData | PrimoAutoEletrica | LEGACY técnico OK |
| Ícone | icon.ico no csproj / ISS | PASS existência |
| Assinatura digital Setup/EXE | não encontrada no fluxo | MISSING |

---

## 9. Instalação / primeira execução / uninstall (escopo seguro)

| Teste | Resultado | Motivo |
|-------|-----------|--------|
| Gerar Setup 1.0.0 agora | **BLOCKED** | ISCC ausente; TFM net9 vs net6 |
| Install limpo em Sandbox | **NOT TESTABLE** nesta sessão | sem sandbox executada |
| Observação PF existente | **PARTIAL** | Inno instalado (unins+ARP); EXE `0.0.0.0` |
| Observação atalho atual | **PASS** (destino) | aponta LocalAppData\App 1.0.0 |
| Persistência CRUD em install limpo | **NOT TESTABLE** | não executar sobre produção |
| Uninstall real | **NOT TESTABLE** | risco de ambiente real |

**DATA RETENTION RISK:** Uninstall Inno não remove LocalAppData (bom). Se dados forem gravados sob `{app}`, podem ser removidos ou órfãos — documentar política na 15B.

---

## 10. Reproducibilidade (§24)

**Classificação: NO (para Setup Inno 1.0.0 idêntico) / PARTIAL (para publish app)**

Motivos:
1. ISS/build-installer assumem **net9.0-windows**; csproj é **net6.0-windows**
2. `build-installer.ps1` sobe **dois** níveis a partir de `Installer` → `ProjectRoot` aponta fora do repo (bug de path)
3. Inno Setup Compiler **não** está na máquina
4. Pasta `Releases/` ausente
5. Actions usa SDK 9 + SingleFile ZIP, não o ISS
6. Deploy script WIP fora do Git

Clone limpo → gerar Setup.exe 1.0.0 bit-a-bit: **NO**.  
Clone limpo → `dotnet publish` net6 self-contained: **PARTIAL/YES** com SDK 6+ roll-forward.

---

## 11. Matriz final

| ITEM | ESTADO | EVIDÊNCIA | RISCO | AÇÃO FUTURA |
|------|--------|-----------|-------|-------------|
| Instalador Inno | PARTIAL | `.iss` + unins PF | Alto se usado sem alinhar TFM | Alinhar net6 + paths; gerar Setup |
| Build app | PASS | csproj net6; 0 erros conhecidos | Baixo | Manter |
| Publish | PARTIAL | scripts divergentes net6/net9 | Alto | Um único pipeline |
| Versionamento | PARTIAL | Assembly 1.0.0 vs PF 0.0.0.0 | Médio | Reinstalar/atualizar PF |
| EXE (LocalAppData) | PASS | ProductVersion 1.0.0 | Baixo | — |
| EXE (Program Files) | RISK | 0.0.0.0 | Médio | Atualizar ou desinstalar legado |
| Ícone | PASS | icon.ico | Baixo | Branding PRIMOX opcional |
| Atalho | PASS | Desktop → LocalAppData\App | Baixo | Unificar nome PRIMOX |
| Uninstall | PARTIAL | unins000 + ARP | Médio (dados) | Política explícita dados |
| Banco | PASS | LocalAppData + integrity ok | Médio (dual path) | Documentar path único |
| Schema | PASS | migrations código + smoke | Médio (32 vs 27) | Auditar prod migrations |
| Migration | PASS | 27 IDs | Baixo | — |
| Backup | PASS | serviço + pasta Backups | Baixo | Testar restore E2E |
| Restore | PARTIAL | código existe | Médio | Teste isolado |
| Update | PARTIAL / NOT IMPLEMENTED (completo) | UpdateService + stubs Inno | Alto comercial | Definir estratégia 15B |
| Dependências | PARTIAL | self-contained vs net9 docs | Médio | Docs = net6 |
| Assinatura | MISSING | sem Authenticode no fluxo | Alto SmartScreen | Certificado code signing |
| Git tag | PASS | v1.0.0 → a4ad6fe | Baixo | — |
| GitHub Release | NOT TESTABLE | gh ausente; Actions existe | Médio | Verificar remoto depois |
| Documentação packaging | PARTIAL | README_INSTALADOR desatualizado (net9, PF path) | Médio | Reescrever após 15B |

---

## 12. Pronto / Falta / Riscos / Prioridades

### Pronto
- Produto **1.0.0** versionado no assembly e tag Git  
- Runtime SQLite em LocalAppData com migrations e integrity ok (amostras)  
- Backup automático/manual no código  
- Deploy rápido de desenvolvimento (LocalAppData) funcional  
- Desinstalador Inno presente na instalação PF legada  

### Falta
- Pipeline único Build→Publish→Installer alinhado a **net6.0-windows**  
- Artefato `PrimoAutoEletrica-Setup-1.0.0.exe` gerado e checksum  
- Inno Setup no ambiente de build / CI  
- Assinatura digital  
- Smoke install limpo (Sandbox) + primeira execução + persistência  
- Branding PRIMOX consistente no instalador/atalhos  
- Política escrita: o que uninstall remove  

### Riscos (priorizados)
1. **TFM mismatch net9 (scripts/ISS) vs net6 (app)** — instalador “oficial” pode falhar ao gerar  
2. **Duas instalações** (PF 0.0.0.0 + LocalAppData 1.0.0) — usuário pode abrir o EXE errado  
3. **Logs em BaseDirectory** — UAC/permissões e mistura programa/dados  
4. **Documentação Inno promete** backup/update/rollback que o `[Code]` **não implementa**  
5. **Sem code signing** — SmartScreen  
6. **SchemaMigrations 32 vs 27** em prod LocalAppData — investigar  

### Recomendação (para decisão posterior — NÃO executar agora)

1. Congelar um único caminho comercial (Inno **ou** MSIX **ou** ZIP self-contained)  
2. Corrigir ISS/scripts para `net6.0-windows` e paths do repo  
3. Gerar e testar Setup em Windows Sandbox  
4. Remover/atualizar instalação PF `0.0.0.0` após validação  
5. Assinar binários  
6. Atualizar docs; alinhar nome PRIMOX Workshop  

---

## 13. Git / documentação desta fase

- **Sem alteração de código funcional**  
- Artefatos desta auditoria: este arquivo + entrada em `PROJECT_STATUS.md`  
- Commit sugerido: `docs(qa): audit commercial packaging and deployment`  

**PARAR após documentação.** Não implementar instalador novo nesta fase.
