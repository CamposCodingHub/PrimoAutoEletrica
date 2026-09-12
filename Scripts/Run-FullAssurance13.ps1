# PRIMOX FULL ASSURANCE-13 — security & bulk closure orchestrator (surgical)
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Framework = "net6.0-windows",
    [switch]$SkipExhaustive,
    [switch]$SkipInstaller,
    [switch]$SkipBulk,
    [int]$InstallerCycles = 3,
    [int]$StartupProfileCycles = 10
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outRoot = Join-Path $repoRoot "TestResults\FullAssurance13\$stamp"
$logPath = Join-Path $outRoot "assurance13.log"
New-Item -ItemType Directory -Force -Path $outRoot | Out-Null
$script:Results = [ordered]@{}
$script:FailCount = 0
$started = Get-Date
$env:DOTNET_ROLL_FORWARD = "LatestMajor"

function Write-A13([string]$Message, [string]$Level = "INFO") {
    $line = "{0} [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message
    Add-Content -LiteralPath $logPath -Value $line -Encoding UTF8
    Write-Host $line
}

function Set-A13([string]$Name, [string]$Status, [string]$Detail = "") {
    $script:Results[$Name] = [ordered]@{ Status = $Status; Detail = $Detail }
    if ($Status -eq "FAIL") { $script:FailCount++ }
    Write-A13 ("{0}: {1} - {2}" -f $Name, $Status, $Detail) $(if ($Status -eq "FAIL") { "ERROR" } else { "RESULT" })
}

function Invoke-Smoke([string]$Filter, [string]$Folder, [switch]$SkipBuild) {
    $dir = Join-Path $outRoot $Folder
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $args = @{
        Configuration   = $Configuration
        Framework       = $Framework
        SmokeFilter     = $Filter
        OutputDirectory = $dir
    }
    if ($SkipBuild) { $args.SkipBuild = $true }
    & (Join-Path $repoRoot "Scripts\Run-UiSmoke.ps1") @args
    $code = $LASTEXITCODE
    $sum = Join-Path $dir "ui-smoke-summary.json"
    $detail = "Exit=$code"
    if (Test-Path $sum) {
        $j = Get-Content $sum -Raw | ConvertFrom-Json
        $detail = "Exit=$code Status=$($j.Status) $($j.PassedChecks)/$($j.TotalChecks)"
        if ($code -eq 0 -and $j.Status -eq "APROVADO") {
            Set-A13 $Filter "PASS" $detail
            return $true
        }
        Set-A13 $Filter "FAIL" $detail
        return $false
    }
    Set-A13 $Filter "FAIL" $detail
    return $false
}

Write-A13 "=== FULL ASSURANCE-13 START ==="
Write-A13 ("HEAD={0}" -f (git -C $repoRoot rev-parse HEAD))
Write-A13 ("TAG_v1.0.0={0}" -f (git -C $repoRoot rev-parse v1.0.0))

Get-Process PrimoAutoEletrica -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

$proj = Join-Path $repoRoot "PrimoAutoEletrica\PrimoAutoEletrica.csproj"
dotnet build $proj -c Debug -v q -nologo 2>&1 | Tee-Object (Join-Path $outRoot "build-debug.log") | Out-Null
if ($LASTEXITCODE -eq 0) { Set-A13 "Build-Debug" "PASS" } else { Set-A13 "Build-Debug" "FAIL" "exit=$LASTEXITCODE" }
dotnet build $proj -c Release -v q -nologo 2>&1 | Tee-Object (Join-Path $outRoot "build-release.log") | Out-Null
if ($LASTEXITCODE -eq 0) { Set-A13 "Build-Release" "PASS" } else { Set-A13 "Build-Release" "FAIL" "exit=$LASTEXITCODE" }

dotnet test (Join-Path $repoRoot "Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj") -c Release --nologo --verbosity quiet 2>&1 | Tee-Object (Join-Path $outRoot "dotnet-test.log") | Out-Null
if ($LASTEXITCODE -eq 0) { Set-A13 "UnitTests" "PASS" } else { Set-A13 "UnitTests" "FAIL" "exit=$LASTEXITCODE" }

& (Join-Path $repoRoot "Scripts\QA\Invoke-SecurityRedTeam.ps1") -OutDir (Join-Path $outRoot "Security")
if ($LASTEXITCODE -eq 0) { Set-A13 "SecurityRedTeam" "PASS" } else { Set-A13 "SecurityRedTeam" "FAIL" "exit=$LASTEXITCODE" }

& (Join-Path $repoRoot "Scripts\QA\Invoke-PathTraversalSimulation.ps1") -OutDir (Join-Path $outRoot "PathSecurity")
if ($LASTEXITCODE -eq 0) { Set-A13 "PathTraversalSim" "PASS" } else { Set-A13 "PathTraversalSim" "FAIL" "exit=$LASTEXITCODE" }

Invoke-Smoke "A13Security" "A13Security" | Out-Null
Invoke-Smoke "A12Security" "A12Security" -SkipBuild | Out-Null
if (-not $SkipBulk) {
    Invoke-Smoke "BulkDataQa13" "BulkDataQa13" -SkipBuild | Out-Null
}
Invoke-Smoke "A13Database" "A13Database" -SkipBuild | Out-Null
Invoke-Smoke "A13Performance" "A13Performance" -SkipBuild | Out-Null
Invoke-Smoke "A13Concurrency" "A13Concurrency" -SkipBuild | Out-Null
Invoke-Smoke "QaEngine" "QaEngine" -SkipBuild | Out-Null
Invoke-Smoke "DeepQa" "DeepQa" -SkipBuild | Out-Null
Invoke-Smoke "LoginSessao" "LoginSessao" -SkipBuild | Out-Null
Invoke-Smoke "LongRun" "LongRun" -SkipBuild | Out-Null
if (-not $SkipExhaustive) {
    Invoke-Smoke "ExhaustiveUi" "ExhaustiveUi" -SkipBuild | Out-Null
}
Invoke-Smoke "I18n07" "I18n07" -SkipBuild | Out-Null
Invoke-Smoke "Tema" "Tema" -SkipBuild | Out-Null
Invoke-Smoke "Sidebar" "Sidebar" -SkipBuild | Out-Null
Invoke-Smoke "OvernightQa" "OvernightQa" -SkipBuild | Out-Null
Invoke-Smoke "Clientes" "Clientes" -SkipBuild | Out-Null
Invoke-Smoke "OrdensServico" "OrdensServico" -SkipBuild | Out-Null
Invoke-Smoke "Orcamentos" "Orcamentos" -SkipBuild | Out-Null
Invoke-Smoke "Agendamentos" "Agendamentos" -SkipBuild | Out-Null
Invoke-Smoke "Estoque" "Estoque" -SkipBuild | Out-Null
Invoke-Smoke "PDV" "PDV" -SkipBuild | Out-Null
Invoke-Smoke "Financeiro" "Financeiro" -SkipBuild | Out-Null
Invoke-Smoke "Relatorios" "Relatorios" -SkipBuild | Out-Null
Invoke-Smoke "Configuracoes" "Configuracoes" -SkipBuild | Out-Null
Invoke-Smoke "Fornecedores" "Fornecedores" -SkipBuild | Out-Null
Invoke-Smoke "Funcionarios" "Funcionarios" -SkipBuild | Out-Null

$perfDir = Join-Path $outRoot "StartupProfile"
& (Join-Path $repoRoot "Scripts\QA\Invoke-Assurance13PerformanceProfile.ps1") -StartupCycles $StartupProfileCycles -Configuration $Configuration -Framework $Framework -OutputDirectory $perfDir
if ($LASTEXITCODE -eq 0) { Set-A13 "StartupProfile" "PASS" "cycles=$StartupProfileCycles" } else { Set-A13 "StartupProfile" "FAIL" "exit=$LASTEXITCODE" }

$dbOut = Join-Path $outRoot "Database"
& (Join-Path $repoRoot "Scripts\QA\Invoke-DatabaseIntegrity.ps1") -DbPath (Join-Path $outRoot "BulkDataQa13\appdata\primoauto.db") -OutDir $dbOut
$dbJson = Join-Path $dbOut "database-integrity.json"
if (Test-Path $dbJson) {
    $dj = Get-Content $dbJson -Raw | ConvertFrom-Json
    Set-A13 "Database" $dj.Status ("integrity={0}; fk={1}" -f $dj.Integrity, $dj.ForeignKeyViolations)
} else {
    Set-A13 "Database" "BLOCKED" "no db json"
}

if (-not $SkipInstaller) {
    $setup = Join-Path $repoRoot "artifacts\installer\PRIMOX-Workshop-Setup-1.0.0-PackagingE2E.exe"
    if (-not (Test-Path $setup)) {
        & (Join-Path $repoRoot "Scripts\Build-PrimoXCommercialRelease.ps1") -SkipTests -SkipClean -AppId "PRIMOX.Workshop.PackagingE2E" -SetupNameSuffix "PackagingE2E"
    }
    & (Join-Path $repoRoot "Scripts\Test-CommercialInstallerHardening.ps1") -Cycles $InstallerCycles -SkipRebuild -SmokeFilter "Clientes"
    if ($LASTEXITCODE -eq 0) { Set-A13 "InstallerE2E" "PASS" "cycles=$InstallerCycles" } else { Set-A13 "InstallerE2E" "FAIL" "exit=$LASTEXITCODE" }
} else {
    Set-A13 "InstallerE2E" "BLOCKED" "skipped"
}

$elapsed = (Get-Date) - $started
$summary = [ordered]@{
    GeneratedAt = (Get-Date).ToString("o")
    Head = (git -C $repoRoot rev-parse HEAD)
    TagV100 = (git -C $repoRoot rev-parse v1.0.0)
    DurationMinutes = [math]::Round($elapsed.TotalMinutes, 1)
    FailCount = $script:FailCount
    Results = $script:Results
}
$summary | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $outRoot "assurance13-summary.json") -Encoding UTF8
Write-A13 ("=== FULL ASSURANCE-13 END fail={0} min={1} ===" -f $script:FailCount, $summary.DurationMinutes)
exit $(if ($script:FailCount -gt 0) { 1 } else { 0 })
