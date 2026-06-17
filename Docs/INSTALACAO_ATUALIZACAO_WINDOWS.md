# Instalacao, Atualizacao e Teste Limpo

## Instalacao

1. Gere o pacote:

```powershell
.\Scripts\New-WindowsInstallerPackage.ps1 -Configuration Release
```

2. Extraia o ZIP gerado em `Artifacts\Installer`.

3. Execute como administrador quando for instalar em `Program Files`:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-PrimoAutoEletrica.ps1
```

O instalador copia o programa, cria a pasta de dados em `%LOCALAPPDATA%\PrimoAutoEletrica` e cria atalhos na area de trabalho e no menu iniciar.

## Requisito .NET

O pacote padrao e framework-dependent e verifica `Microsoft.WindowsDesktop.App 9.x`.

Para gerar pacote com runtime embutido:

```powershell
.\Scripts\New-WindowsInstallerPackage.ps1 -Configuration Release -SelfContained
```

## Teste em Pasta Limpa

Execute:

```powershell
.\Scripts\Test-CleanInstallSimulation.ps1 -Configuration Debug
```

O teste gera pacote, executa o instalador em uma pasta temporaria, cria pasta de dados isolada e valida o executavel instalado.

## Atualizacao

Antes de atualizar:

1. Feche o sistema em todas as estacoes.
2. Gere um pacote novo.
3. Execute:

```powershell
powershell -ExecutionPolicy Bypass -File .\Update-PrimoAutoEletrica.ps1
```

A rotina cria backup do banco local antes de copiar os arquivos, preserva snapshot da instalacao anterior e grava log em `%ProgramData%\PrimoAutoEletrica\UpdateLogs`.

## Evidencias

Guarde estes arquivos junto da entrega:

- `INSTALLER_MANIFEST.txt`
- `README-INSTALACAO.txt`
- Log de `Test-CleanInstallSimulation.ps1`
- Log de update em `%ProgramData%\PrimoAutoEletrica\UpdateLogs`
