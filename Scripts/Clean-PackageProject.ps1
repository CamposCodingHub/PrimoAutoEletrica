# Clean-PackageProject.ps1
# Script para criar pacote limpo do projeto PrimoAutoEletrica

param(
    [string]$OutputPath = ".\PackageClean"
)

$ErrorActionPreference = "Stop"

# Data e hora para o nome do pacote
$Timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$PackageName = "PrimoAutoEletrica_PackageClean_$Timestamp"
$PackageDir = Join-Path $OutputPath $PackageName

Write-Host "=== CRIANDO PACOTE LIMPO DO PROJETO PRIMO AUTO ELÉTRICA ===" -ForegroundColor Cyan
Write-Host "Data: $Timestamp" -ForegroundColor Gray
Write-Host "Destino: $PackageDir" -ForegroundColor Gray
Write-Host ""

# Criar diretório do pacote
Write-Host "Criando diretório do pacote..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $PackageDir | Out-Null
Write-Host "Diretório criado: $PackageDir" -ForegroundColor Green
Write-Host ""

# Copiar arquivos do projeto (excluindo bin, obj, .vs, logs temporários)
Write-Host "Copiando arquivos do projeto..." -ForegroundColor Yellow

$SourceDir = Get-Location
$ExcludeDirs = @("bin", "obj", ".vs", "Logs", "TestResults", "Backups", "PackageClean")

Get-ChildItem -Path $SourceDir -Recurse | ForEach-Object {
    $relativePath = $_.FullName.Substring($SourceDir.Path.Length)
    $exclude = $false
    
    foreach ($excludeDir in $ExcludeDirs) {
        if ($relativePath -like "*\$excludeDir*") {
            $exclude = $true
            break
        }
    }
    
    if (-not $exclude -and -not $_.PSIsContainer) {
        $destPath = Join-Path $PackageDir $relativePath.TrimStart('\')
        $destDir = Split-Path $destPath -Parent
        
        if ($destDir -and -not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        }
        
        Copy-Item -Path $_.FullName -Destination $destPath -Force
    }
}

Write-Host "Arquivos copiados" -ForegroundColor Green
Write-Host ""

# Criar arquivo de metadados do pacote
$MetadataFile = Join-Path $PackageDir "PACKAGE_METADATA.txt"
$MetadataContent = @"
PACOTE LIMPO DO PROJETO PRIMO AUTO ELÉTRICA
============================================

Data e Hora: $Timestamp
Nome do Pacote: $PackageName
Caminho do Pacote: $PackageDir

Branch Git: $(git branch --show-current)
Último Commit: $(git log -1 --oneline)

Arquivos incluídos: $(Get-ChildItem -Path $PackageDir -Recurse -File | Measure-Object).Count
Tamanho do Pacote: $((Get-ChildItem -Path $PackageDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB) MB

Diretórios excluídos: bin, obj, .vs, Logs, TestResults, Backups, PackageClean

Gerado por: Clean-PackageProject.ps1
"@

Set-Content -Path $MetadataFile -Value $MetadataContent
Write-Host "Metadados do pacote criados" -ForegroundColor Green
Write-Host ""

# Criar arquivo README para o pacote
$ReadmeFile = Join-Path $PackageDir "README.txt"
$ReadmeContent = @"
PRIMO AUTO ELÉTRICA - PACOTE LIMPO
==================================

Este é um pacote limpo do projeto PrimoAutoEletrica.

Para usar este pacote:

1. Extraia o conteúdo para uma pasta
2. Abra o arquivo PrimoAutoEletrica.sln no Visual Studio
3. Execute dotnet restore para restaurar os pacotes NuGet
4. Execute dotnet build para compilar o projeto
5. Execute dotnet run para executar a aplicação

Requisitos:
- .NET 9.0 SDK
- Visual Studio 2022 (opcional)
- Windows 10 ou superior

Para mais informações, consulte a documentação em Docs/
"@

Set-Content -Path $ReadmeFile -Value $ReadmeContent
Write-Host "README criado" -ForegroundColor Green
Write-Host ""

Write-Host "Pacote limpo criado com sucesso!" -ForegroundColor Green
Write-Host "Local: $PackageDir" -ForegroundColor Cyan
