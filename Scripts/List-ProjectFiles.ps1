# List-ProjectFiles.ps1
# Script para listar todos os arquivos do projeto PrimoAutoEletrica

param(
    [string]$OutputFile = ".\Logs\project_files_list.txt"
)

$ErrorActionPreference = "Stop"

Write-Host "=== LISTANDO ARQUIVOS DO PROJETO PRIMO AUTO ELÉTRICA ===" -ForegroundColor Cyan
Write-Host ""

# Criar diretório de logs se não existir
$LogDir = Split-Path $OutputFile
if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
}

# Listar todos os arquivos
Write-Host "Listando arquivos..." -ForegroundColor Yellow

$files = Get-ChildItem -Recurse -File | Select-Object FullName, Length, LastWriteTime | Sort-Object LastWriteTime -Descending

# Salvar em arquivo
$files | Out-File -FilePath $OutputFile -Encoding UTF8

Write-Host "Total de arquivos: $($files.Count)" -ForegroundColor Green
Write-Host "Arquivo salvo em: $OutputFile" -ForegroundColor Cyan
Write-Host ""

# Mostrar estatísticas
Write-Host "=== ESTATÍSTICAS ===" -ForegroundColor Cyan

$totalSize = ($files | Measure-Object -Property Length -Sum).Sum
Write-Host "Tamanho total: $($totalSize / 1MB) MB" -ForegroundColor Gray

$filesByExtension = $files | Group-Object { [System.IO.Path]::GetExtension($_.FullName) } | Sort-Object Count -Descending
Write-Host ""
Write-Host "Arquivos por extensão:" -ForegroundColor Gray
foreach ($group in $filesByExtension) {
    Write-Host "  $($group.Name): $($group.Count)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== CONCLUÍDO ===" -ForegroundColor Green
