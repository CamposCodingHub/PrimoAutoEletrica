# Run-AppWithLog.ps1
# Script para executar a aplicação PrimoAutoEletrica com log

param(
    [string]$LogFile = ".\Logs\app_run.log"
)

$ErrorActionPreference = "Stop"

# Criar diretório de logs se não existir
$LogDir = Split-Path $LogFile
if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
}

Write-Host "=== EXECUTANDO APLICAÇÃO PRIMO AUTO ELÉTRICA COM LOG ===" -ForegroundColor Cyan
Write-Host "Log: $LogFile" -ForegroundColor Gray
Write-Host ""

# Executar aplicação com log
Write-Host "Iniciando aplicação..." -ForegroundColor Yellow
Write-Host "Pressione Ctrl+C para interromper" -ForegroundColor Gray
Write-Host ""

dotnet run --project .\PrimoAutoEletrica\PrimoAutoEletrica.csproj *> $LogFile

Write-Host ""
Write-Host "Aplicação encerrada" -ForegroundColor Green
Write-Host "Log salvo em: $LogFile" -ForegroundColor Cyan
