# Create-ProjectBackup.ps1
# Script para criar backup completo do projeto PrimoAutoEletrica

param(
    [string]$BackupPath = ".\Backups",
    [switch]$IncludeDatabase = $true
)

$ErrorActionPreference = "Stop"

# Data e hora para o nome do backup
$Timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$BackupName = "PrimoAutoEletrica_Backup_$Timestamp"
$BackupDir = Join-Path $BackupPath $BackupName

Write-Host "=== CRIANDO BACKUP DO PROJETO PRIMO AUTO ELÉTRICA ===" -ForegroundColor Cyan
Write-Host "Data: $Timestamp" -ForegroundColor Gray
Write-Host "Destino: $BackupDir" -ForegroundColor Gray
Write-Host ""

# Criar diretório de backup
Write-Host "Criando diretório de backup..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
Write-Host "Diretório criado: $BackupDir" -ForegroundColor Green
Write-Host ""

# Copiar arquivos do projeto (excluindo bin, obj, .vs, logs temporários)
Write-Host "Copiando arquivos do projeto..." -ForegroundColor Yellow

$SourceDir = Get-Location
$ExcludeDirs = @("bin", "obj", ".vs", "Logs", "TestResults", "Backups")

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
        $destPath = Join-Path $BackupDir $relativePath.TrimStart('\')
        $destDir = Split-Path $destPath -Parent
        
        if ($destDir -and -not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        }
        
        Copy-Item -Path $_.FullName -Destination $destPath -Force
    }
}

Write-Host "Arquivos copiados" -ForegroundColor Green
Write-Host ""

# Copiar banco de dados se solicitado
if ($IncludeDatabase) {
    Write-Host "Copiando banco de dados..." -ForegroundColor Yellow
    
    $LocalAppData = [Environment]::GetFolderPath("LocalApplicationData")
    $DatabasePath = Join-Path $LocalAppData "PrimoAutoEletrica\database.db"
    
    if (Test-Path $DatabasePath) {
        $DatabaseBackupDir = Join-Path $BackupDir "DatabaseBackup"
        New-Item -ItemType Directory -Force -Path $DatabaseBackupDir | Out-Null
        
        Copy-Item -Path $DatabasePath -Destination $DatabaseBackupDir -Force
        Write-Host "Banco de dados copiado para: $DatabaseBackupDir" -ForegroundColor Green
    } else {
        Write-Host "Banco de dados não encontrado em: $DatabasePath" -ForegroundColor Yellow
    }
    
    Write-Host ""
}

# Criar arquivo de metadados do backup
$MetadataFile = Join-Path $BackupDir "BACKUP_METADATA.txt"
$MetadataContent = @"
BACKUP DO PROJETO PRIMO AUTO ELÉTRICA
=====================================

Data e Hora: $Timestamp
Nome do Backup: $BackupName
Caminho do Backup: $BackupDir
Inclui Banco de Dados: $IncludeDatabase

Branch Git: $(git branch --show-current)
Último Commit: $(git log -1 --oneline)

Arquivos copiados: $(Get-ChildItem -Path $BackupDir -Recurse -File | Measure-Object).Count
Tamanho do Backup: $((Get-ChildItem -Path $BackupDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB) MB

Gerado por: Create-ProjectBackup.ps1
"@

Set-Content -Path $MetadataFile -Value $MetadataContent
Write-Host "Metadados do backup criados" -ForegroundColor Green
Write-Host ""

# Compactar backup (opcional - requer 7-Zip ou similar)
Write-Host "Backup criado com sucesso!" -ForegroundColor Green
Write-Host "Local: $BackupDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para restaurar este backup, consulte: Docs/GUIA_BACKUP_RESTAURACAO.md" -ForegroundColor Yellow
