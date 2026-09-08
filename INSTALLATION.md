# PRIMOX Workshop — Instalação (usuário)

**Produto:** PRIMOX Workshop **1.0.0**  
**Publisher:** CamposCodingHub  
**Canal oficial:** instalador Inno Setup (`PRIMOX-Workshop-Setup-1.0.0.exe`)

---

## Requisitos

- Windows 10 ou 11 (x64)
- Direitos de administrador para instalar em Program Files
- **Não** é necessário instalar o .NET Runtime manualmente (pacote **self-contained**)
- Não depende de Visual Studio nem do SDK no PC do cliente

---

## Instalação

1. Obtenha o arquivo `PRIMOX-Workshop-Setup-1.0.0.exe` e o checksum SHA256 correspondente.
2. Confira o hash (PowerShell):

```powershell
Get-FileHash .\PRIMOX-Workshop-Setup-1.0.0.exe -Algorithm SHA256
```

3. Execute o Setup e siga o assistente (ou instalação silenciosa apenas se souber o que está fazendo).
4. Destino padrão do **programa:** `C:\Program Files\PRIMOX\Workshop`
5. Atalhos: **PRIMOX Workshop** no Menu Iniciar e (opcional) na Área de trabalho.

---

## Primeira execução

1. Abra **PRIMOX Workshop** pelo atalho oficial.
2. Faça login com o usuário administrador inicial (credenciais geradas na primeira configuração da estação, conforme o fluxo do aplicativo).
3. Confirme o Dashboard.
4. Cadastre um cliente/veículo de teste se desejar validar persistência; depois remova dados de teste se não forem reais.

O banco SQLite é criado automaticamente em:

`%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db`

---

## Banco

- Motor: **SQLite**
- Arquivo: `primoauto.db`
- Pasta: `%LOCALAPPDATA%\PrimoAutoEletrica\`
- O instalador **não** embute banco de produção nem dados reais.

---

## Backup

- Feito pelo **aplicativo** (`DatabaseBackupService`), não pelo instalador.
- Pasta típica: `%LOCALAPPDATA%\PrimoAutoEletrica\Backups\`
- Há backup manual, automático (intervalo configurável) e cópias de segurança antes de operações críticas/migração.

---

## Desinstalação

Use “Adicionar ou remover programas” / desinstalador **PRIMOX Workshop**.

**Remove:** programa, DLLs, atalhos, arquivos sob Program Files.

**Preserva:** banco, backups, configuração e mídia em `%LOCALAPPDATA%\PrimoAutoEletrica\` (a menos que você apague essa pasta manualmente).

---

## Recuperação

1. Feche o PRIMOX.
2. Restaure um arquivo `.db` válido da pasta Backups usando a função de restauração do aplicativo (quando disponível na UI) **ou** com suporte técnico — sempre com cópia de segurança prévia.
3. Não delete `SchemaMigrations` nem “limpe” o banco para “consertar” versões.

---

## Atualização

- Auto-update comercial completo: **ainda não implementado**.
- Fluxo esperado futuro: instalar Setup mais novo sobre o programa; dados em AppData permanecem.
- Sempre faça backup antes de atualizar.

---

## Instalação legada (“Primo Auto Elétrica” 0.0.0.0)

Instalações antigas em `C:\Program Files\Primo Auto Elétrica` **não** são o produto comercial oficial. O oficial é **PRIMOX Workshop 1.0.0** em `C:\Program Files\PRIMOX\Workshop`.

O banco **não** fica em Program Files. Use sempre:

`%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db`

### Procedimento seguro

1. Identifique a versão antiga (ProductVersion do EXE / pasta legada).
2. Localize o banco em AppData (e pastas Backups/Logs/Media no mesmo perfil).
3. Faça **backup** (cópia do `.db` + anote tamanho e, se possível, SHA256).
4. Confirme que o arquivo de backup abre/copia sem erro (integridade).
5. Instale `PRIMOX-Workshop-Setup-1.0.0.exe`.
6. Confirme que o banco em AppData **permanece**.
7. Confirme que o atalho **PRIMOX Workshop** aponta para `Program Files\PRIMOX\Workshop\PrimoAutoEletrica.exe` (não para `LocalAppData\App`, nem para a pasta legada).
8. Abra o aplicativo e valide dados conhecidos.
9. Só então remova o legado pelo desinstalador do Windows / `unins000.exe`.
10. Não apague Program Files “na mão” sem backup e sem desinstalador.

**Nunca** apague `%LOCALAPPDATA%\PrimoAutoEletrica` pensando que é “lixo de instalação”.

Ops (máquina de suporte): `Scripts/Cleanup-LegacyPrimoInstall.ps1` (dry-run) e, com confirmação, `-ConfirmCleanup`.

Relatório técnico: `Docs/qa/PRIMOX-LEGACY-CLEANUP-REPORT.md`.

### Desenvolvimento vs comercial

`%LOCALAPPDATA%\PrimoAutoEletrica\App` pode existir no fluxo de desenvolvimento (`Deploy-ToInstalledApp.ps1`). Isso **não** é a instalação comercial. Atalhos de cliente devem apontar somente para Program Files oficial.

---

## SmartScreen / assinatura

O Setup pode ser alertado pelo SmartScreen enquanto a assinatura digital **não** estiver configurada. Isso não indica falha do instalador em si.

---

## Suporte

- Repositório / issues: conforme URL do publisher no instalador.
- Não envie banco de produção com dados pessoais em tickets públicos.
- Relatórios: `Docs/qa/PRIMOX-COMMERCIAL-PACKAGING-REPORT.md`, `Docs/qa/PRIMOX-INSTALLATION-E2E-REPORT.md`, `Docs/qa/PRIMOX-LEGACY-CLEANUP-REPORT.md`
