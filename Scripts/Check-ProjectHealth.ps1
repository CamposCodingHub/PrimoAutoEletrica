# Check-ProjectHealth.ps1
# Script para verificar a saúde do projeto PrimoAutoEletrica

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$solutionPath = Join-Path $projectRoot "PrimoAutoEletrica.sln"
$mainProjectPath = Join-Path $projectRoot "PrimoAutoEletrica\PrimoAutoEletrica.csproj"
$testProjectPath = Join-Path $projectRoot "Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj"
$ciWorkflowPath = Join-Path $projectRoot ".github\workflows\ci.yml"

Write-Host "=== VERIFICANDO SAÚDE DO PROJETO PRIMO AUTO ELÉTRICA ===" -ForegroundColor Cyan
Write-Host ""

$issues = @()

# Verificar estrutura de pastas
Write-Host "Verificando estrutura de pastas..." -ForegroundColor Yellow
$requiredDirs = @(
    "PrimoAutoEletrica",
    "Tests",
    "Docs",
    "Scripts",
    "Themes",
    "Reports",
    "Logs",
    ".github"
)

$missingDirs = @()
foreach ($dir in $requiredDirs) {
    if (-not (Test-Path (Join-Path $projectRoot $dir))) {
        $missingDirs += $dir
    }
}

if ($missingDirs.Count -eq 0) {
    Write-Host "Todas as pastas obrigatórias existem" -ForegroundColor Green
} else {
    Write-Host "Pastas faltando: $($missingDirs -join ', ')" -ForegroundColor Red
    $issues += "Pastas obrigatórias faltando"
}

Write-Host ""

# Verificar arquivos principais
Write-Host "Verificando arquivos principais..." -ForegroundColor Yellow
$requiredFiles = @(
    "PrimoAutoEletrica.sln",
    "PrimoAutoEletrica\PrimoAutoEletrica.csproj",
    "Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj",
    ".github\workflows\ci.yml"
)

$missingFiles = @()
foreach ($file in $requiredFiles) {
    if (-not (Test-Path (Join-Path $projectRoot $file))) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -eq 0) {
    Write-Host "Todos os arquivos principais existem" -ForegroundColor Green
} else {
    Write-Host "Arquivos faltando: $($missingFiles -join ', ')" -ForegroundColor Red
    $issues += "Arquivos principais faltando"
}

Write-Host ""

# Verificar SDK e drift de framework
Write-Host "Verificando SDK e alinhamento de target framework..." -ForegroundColor Yellow
$sdks = dotnet --list-sdks 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "SDKs instalados: $($sdks.Count)" -ForegroundColor Green
}

$frameworks = @()
Get-ChildItem -Path $projectRoot -Recurse -Filter *.csproj | ForEach-Object {
    $xml = [xml](Get-Content $_.FullName)
    $tfm = $xml.Project.PropertyGroup.TargetFramework
    if ($tfm) {
        $frameworks += "{0}: {1}" -f $_.Name, $tfm
    }
}

if ($frameworks.Count -gt 0) {
    $distinctFrameworks = $frameworks | ForEach-Object { ($_ -split ': ')[1] } | Select-Object -Unique
    Write-Host "Targets detectados: $($distinctFrameworks -join ', ')" -ForegroundColor Cyan
    if ($distinctFrameworks.Count -gt 1) {
        Write-Host "Alerta: há mais de um target framework no repositório." -ForegroundColor Yellow
        $issues += "Drift de target framework"
    }
}

if (Test-Path (Join-Path $projectRoot "global.json")) {
    $globalJson = Get-Content (Join-Path $projectRoot "global.json") -Raw | ConvertFrom-Json
    Write-Host "global.json SDK: $($globalJson.sdk.version)" -ForegroundColor Cyan
}

Write-Host ""

# Verificar se compila
Write-Host "Verificando compilação release..." -ForegroundColor Yellow
$buildOutput = dotnet build "$mainProjectPath" -c Release --no-restore 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "Projeto compila com sucesso em Release" -ForegroundColor Green
} else {
    Write-Host "Projeto não compila em Release" -ForegroundColor Red
    $issues += "Compilação falhou"
    foreach ($line in $buildOutput) { Write-Host "  $line" -ForegroundColor Gray }
}

Write-Host ""

# Verificar se testes passam
Write-Host "Verificando testes..." -ForegroundColor Yellow
$testProjectExists = Test-Path $testProjectPath
if ($testProjectExists) {
    $testOutput = dotnet test "$testProjectPath" -c Release --no-build --logger "trx;LogFileName=health-check.trx" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Testes passam com sucesso" -ForegroundColor Green
    } else {
        Write-Host "Testes falham" -ForegroundColor Red
        $issues += "Testes falharam"
        foreach ($line in $testOutput) { Write-Host "  $line" -ForegroundColor Gray }
    }
} else {
    Write-Host "Projeto de testes não encontrado; validação de testes ignorada." -ForegroundColor Yellow
}

Write-Host ""

# Verificar arquivos grandes
Write-Host "Verificando arquivos grandes (> 100KB)..." -ForegroundColor Yellow
$largeFiles = Get-ChildItem -Recurse -File -Path $projectRoot | Where-Object { $_.Length -gt 100KB } | Select-Object FullName, Length

if ($largeFiles.Count -eq 0) {
    Write-Host "Nenhum arquivo grande encontrado" -ForegroundColor Green
} else {
    Write-Host "Arquivos grandes encontrados:" -ForegroundColor Yellow
    foreach ($file in $largeFiles) {
        Write-Host "  $($file.FullName) - $($file.Length / 1KB) KB" -ForegroundColor Gray
    }
}

Write-Host ""

# Verificar logs e workflow de CI
Write-Host "Verificando logs e CI..." -ForegroundColor Yellow
if (Test-Path $ciWorkflowPath) {
    Write-Host "Workflow de CI encontrado: $ciWorkflowPath" -ForegroundColor Green
} else {
    Write-Host "Workflow de CI não encontrado." -ForegroundColor Red
    $issues += "Workflow de CI ausente"
}

if (Test-Path (Join-Path $projectRoot "Logs")) {
    $errorLogs = Get-ChildItem -Path (Join-Path $projectRoot "Logs") -Filter "*.log" -ErrorAction SilentlyContinue
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
$health = if ($issues.Count -eq 0) { "SAUDÁVEL" } else { "ATENÇÃO" }
Write-Host "Status: $health" -ForegroundColor $(if ($health -eq "SAUDÁVEL") { "Green" } else { "Yellow" })
if ($issues.Count -gt 0) {
    Write-Host "Itens pendentes:" -ForegroundColor Yellow
    foreach ($issue in $issues) {
        Write-Host "  - $issue" -ForegroundColor Gray
    }
}
Write-Host ""
