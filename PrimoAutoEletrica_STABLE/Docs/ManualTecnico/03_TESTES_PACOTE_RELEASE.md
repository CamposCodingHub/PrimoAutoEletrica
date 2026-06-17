# Testes, Pacote Limpo e Release

## Como rodar testes e validacoes

Build da solucao:

```powershell
dotnet build .\PrimoAutoEletrica.sln -c Debug
```

UI smoke completo:

```powershell
.\Scripts\Run-UiSmoke.ps1 -Configuration Debug -Framework net9.0-windows
```

UI smoke filtrado:

```powershell
.\Scripts\Run-UiSmoke.ps1 -Configuration Debug -Framework net9.0-windows -SmokeFilter Configuracoes -SkipBuild
```

Validacao completa:

```powershell
.\Scripts\Run-FullValidation.ps1
```

## Como gerar pacote limpo

```powershell
.\Scripts\New-WindowsInstallerPackage.ps1 -Configuration Release
```

O pacote gerado deve conter:

- Aplicacao publicada.
- Script de instalacao.
- Script de atualizacao.
- Manifesto do pacote.
- Instrucao de instalacao.

## Como testar instalacao limpa

```powershell
.\Scripts\Test-CleanInstallSimulation.ps1 -Configuration Debug
```

O teste precisa confirmar:

- Instalador executa.
- Pasta do programa e criada.
- Pasta de dados e criada.
- Executavel instalado existe.
- Evidencia fica em `TestResults/CleanInstallSimulation`.

## Como criar release

1. Rode build em `Release`.
2. Rode validacao completa.
3. Gere pacote limpo.
4. Rode simulacao de instalacao limpa.
5. Atualize checklist final de qualidade.
6. Gere tag ou pacote versionado.
7. Guarde evidencias de logs e relatorios.

## Como atualizar instalacao existente

1. Feche o sistema nas estacoes.
2. Rode `Update-PrimoAutoEletrica.ps1` apontando para a instalacao.
3. Confirme backup automatico antes da copia.
4. Confirme que o executavel foi atualizado.
5. Abra o sistema e deixe as migracoes rodarem.
6. Valide login, cliente, veiculo, backup, impressao e PDV.
