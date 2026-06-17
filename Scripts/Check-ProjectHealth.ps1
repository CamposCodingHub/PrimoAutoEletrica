# Check-ProjectHealth.ps1
# Script para verificar a saúde do projeto PrimoAutoEletrica

$ErrorActionPreference = "Stop"

Write-Host "=== VERIFICANDO SAÚDE DO PROJETO PRIMO AUTO ELÉTRICA ===" -ForegroundColor Cyan
Write-Host ""

# Verificar estrutura de pastas
Write-Host "Verificando estrutura de pastas..." -ForegroundColor Yellow

$requiredDirs = @(
    "PrimoAutoEletrica",
    "Tests",
    "Docs",
    "Scripts",
    "Themes",
    "Reports",
    "Logs"
)

$missingDirs = @()
foreach ($dir in $requiredDirs) {
    if (-not (Test-Path $dir)) {
        $missingDirs += $dir
    }
}

if ($missingDirs.Count -eq 0) {
    Write-Host "Todas as pastas obrigatórias existem" -ForegroundColor Green
} else {
    Write-Host "Pastas faltando: $($missingDirs -join ', ')" -ForegroundColor Red
}

Write-Host ""

# Verificar arquivos principais
Write-Host "Verificando arquivos principais..." -ForegroundColor Yellow

$requiredFiles = @(
    "PrimoAutoEletrica.sln",
    "PrimoAutoEletrica\PrimoAutoEletrica.csproj",
    "Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj"
)

$missingFiles = @()
foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -eq 0) {
    Write-Host "Todos os arquivos principais existem" -ForegroundColor Green
} else {
    Write-Host "Arquivos faltando: $($missingFiles -join ', ')" -ForegroundColor Red
}

Write-Host ""

# Verificar se compila
Write-Host "Verificando se compila..." -ForegroundColor Yellow

$buildResult = dotnet build --no-restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "Projeto compila com sucesso" -ForegroundColor Green
} else {
    Write-Host "Projeto não compila" -ForegroundColor Red
}

Write-Host ""

# Verificar se testes passam
Write-Host "Verificando se testes passam..." -ForegroundColor Yellow

$testResult = dotnet test --no-build
if ($LASTEXITCODE -eq 0) {
    Write-Host "Testes passam" -ForegroundColor Green
} else {
    Write-Host "Testes falham" -ForegroundColor Red
}

Write-Host ""

# Verificar arquivos grandes
Write-Host "Verificando arquivos grandes (> 100KB)..." -ForegroundColor Yellow

$largeFiles = Get-ChildItem -Recurse -File | Where-Object { $_.Length -gt 100KB } | Select-Object FullName, Length

if ($largeFiles.Count -eq 0) {
    Write-Host "Nenhum arquivo grande encontrado" -ForegroundColor Green
} else {
    Write-Host "Arquivos grandes encontrados:" -ForegroundColor Yellow
    foreach ($file in $largeFiles) {
        Write-Host "  $($file.FullName) - $($file.Length / 1KB) KB" -ForegroundColor Gray
    }
}

Write-Host ""

# Verificar se há erros nos logs
Write-Host "Verificando logs de erro..." -ForegroundColor Yellow

if (Test-Path "Logs") {
    $errorLogs = Get-ChildItem -Path "Logs" -Filter "*.log" -ErrorAction SilentlyContinue
    if ($errorLogs.Count -eq 0) {
        Write-Host "Nenhum log de erro encontrado" -ForegroundColor Green
    } else {
        Write-Host "Logs encontrados: $($errorLogs.Count)" -ForegroundColor Yellow
        foreach ($log in $errorLogs) {
            Write-Host "  $($log.Name)" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "Pasta Logs não existe" -ForegroundColor Yellow
}

Write-Host ""

# Resumo
Write-Host "=== RESUMO DA SAÚDE DO PROJETO ===" -ForegroundColor Cyan

$health = "SAUDÁVEL"
if ($missingDirs.Count -gt 0 -or $missingFiles.Count -gt 0) {
    $health = "PROBLEMAS ESTRUTURAIS"
}
if ($LASTEXITCODE -ne 0) {
    $health = "PROBLEMAS DE COMPILAÇÃO/TESTE"
}

Write-Host "Status: $health" -ForegroundColor $(if ($health -eq "SAUDÁVEL") { "Green" } else { "Red" })
Write-Host ""
