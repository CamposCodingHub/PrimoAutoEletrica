# Instalador Profissional - Primo Auto Elétrica

## Visão Geral

Este documento descreve o processo de criação e uso do instalador profissional para o sistema Primo Auto Elétrica.

## Tecnologia Escolhida: Inno Setup

Optamos por **Inno Setup** devido a:
- Compatibilidade com Windows 7/8/10/11
- Scripting poderoso e flexível
- Suporte a atualizações
- Criação de desinstalador automático
- Compatibilidade com instalações silenciosas
- Comunidade ativa e documentação extensa

## Estrutura do Instalador

### Arquivos

- `PrimoAutoEletrica.iss` - Script principal do Inno Setup
- `build-installer.ps1` - Script PowerShell para automatizar o build
- `README_INSTALADOR.md` - Este documento

## Funcionalidades do Instalador

### Instalação

- Instala em `C:\Program Files\PrimoAutoEletrica` (padrão) ou pasta configurável
- Cria atalho na área de trabalho
- Cria atalho no menu Iniciar
- Registra entrada no "Adicionar/Remover Programas"
- Requer privilégios de administrador
- Exibe licença/termos de uso

### Preservação de Dados

- Preserva pasta `Data` (banco de dados)
- Preserva pasta `Logs` (logs do sistema)
- Preserva pasta `Config` (configurações do usuário)
- Preserva pasta `Backups` (backups automáticos)
- Detecta instalação anterior
- Cria backup antes de atualizar

### Atualização

- Detecta versão instalada
- Compara com versão do instalador
- Cria backup automático antes de atualizar
- Preserva todos os dados do usuário
- Permite rollback se necessário
- Registra log de atualização

### Desinstalação

- Remove arquivos do programa
- Preserva dados do usuário (configurável)
- Remove atalhos
- Remove registro do sistema
- Oferece opção de preservar banco de dados

## Processo de Build

### Pré-requisitos

1. **Inno Setup Compiler** - Download em https://jrsoftware.org/isdl.php
2. **.NET 9.0 SDK** - Para compilar o aplicativo
3. **PowerShell 5.1+** - Para executar o script de build

### Passos

1. Compilar o aplicativo:
```bash
cd PrimoAutoEletrica
dotnet publish -c Release -r win-x64 --self-contained
```

2. Executar o script de build:
```bash
cd Installer
.\build-installer.ps1
```

3. O instalador será gerado em `Releases/`

### Script build-installer.ps1

O script realiza:
- Publicação self-contained do aplicativo
- Cópia de arquivos necessários
- Compilação do script Inno Setup
- Geração do instalador
- Cálculo de hash SHA256
- Registro de log da release

## Configuração do Script Inno Setup

### Parâmetros Principais

```pascal
#define AppName "Primo Auto Elétrica"
#define AppVersion "1.0.0"
#define Publisher "CamposCodingHub"
#define URL "https://github.com/camposcodinghub/PrimoAutoEletrica"
```

### Diretórios

- `{app}` - Diretório de instalação
- `{commonappdata}` - Dados do aplicativo (SQLite, Config, Logs)
- `{userdesktop}` - Área de trabalho
- `{commonstartup}` - Menu Iniciar

### Arquivos Preservados

Durante atualização:
- `{commonappdata}\PrimoAutoEletrica\Data\*`
- `{commonappdata}\PrimoAutoEletrica\Logs\*`
- `{commonappdata}\PrimoAutoEletrica\Config\*`
- `{commonappdata}\PrimoAutoEletrica\Backups\*`

## Validação

### Checklist de Instalação

- [ ] Instalação limpa em máquina virtual
- [ ] Atualização de versão anterior
- [ ] Preservação de banco de dados
- [ ] Preservação de configurações
- [ ] Criação de atalhos
- [ ] Registro no sistema
- [ ] Desinstalação limpa
- [ ] Desinstalação com preservação de dados
- [ ] Instalação silenciosa
- [ ] Atualização silenciosa

### Testes de Compatibilidade

- Windows 7 SP1 (x64)
- Windows 8.1 (x64)
- Windows 10 (x64)
- Windows 11 (x64)

## Solução de Problemas

### Erro: "Acesso negado"

- Execute como administrador
- Verifique permissões da pasta de destino

### Erro: "Arquivo em uso"

- Feche o aplicativo antes de atualizar
- Verifique se há outras instâncias rodando

### Erro: "Banco de dados corrompido"

- Restaure do backup automático
- Use o utilitário de backup/restauração

## Distribuição

### Pacote de Release

O pacote final contém:
- `PrimoAutoEletrica-Setup.exe` - Instalador
- `README.txt` - Instruções de instalação
- `LICENSE.txt` - Licença de uso
- `MANUAL_USUARIO.pdf` - Manual do usuário
- `CHANGELOG.md` - Histórico de versões
- `SHA256.txt` - Hash de integridade

### Upload

- GitHub Releases
- Site oficial
- Distribuição por e-mail (para clientes)

## Suporte

Para problemas com o instalador:
- Verifique logs em `%LOCALAPPDATA%\PrimoAutoEletrica\Logs\`
- Consulte manual de instalação
- Entre em contato com suporte técnico

---

**Versão do documento:** 1.0
**Última atualização:** 2026-06-17
