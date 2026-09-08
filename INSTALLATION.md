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

## Instalação legada

Se existir `C:\Program Files\Primo Auto Elétrica` com versão `0.0.0.0`, trate como legado. Prefira o atalho **PRIMOX Workshop** da instalação nova. Remova o legado só após confirmar que os dados em AppData estão intactos.

---

## Suporte

- Repositório / issues: conforme URL do publisher no instalador.
- Não envie banco de produção com dados pessoais em tickets públicos.
- Relatório técnico de packaging: `Docs/qa/PRIMOX-COMMERCIAL-PACKAGING-REPORT.md`
