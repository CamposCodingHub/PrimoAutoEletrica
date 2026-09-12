<#
.SYNOPSIS
  PRIMOX FULL ASSURANCE-12 — security hardening + bulk + installed smoke + full regression.
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Framework = "net6.0-windows",
    [switch]$SkipExhaustive,
    [switch]$SkipInstaller,
    [switch]$SkipBulk,
    [int]$RecoveryCycles = 5,
    [int]$InstallerCycles = 3,
    [int]$InstalledClientesCycles = 10
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outRoot = Join-Path $repoRoot "TestResults\FullAssurance12\$stamp"
$logPath = Join-Path $outRoot "assurance12.log"
$summaryPath = Join-Path $outRoot "assurance12-summary.json"
New-Item -ItemType Directory -Force -Path $outRoot | Out-Null

$started = Get-Date
$env:DOTNET_ROLL_FORWARD = "LatestMajor"
$script:Results = [ordered]@{}
$script:FailCount = 0

function Write-A12([string]$Message, [string]$Level = "INFO") {
    $line = "{0} [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message
    Add-Content -LiteralPath $logPath -Value $line -Encoding UTF8
    Write-Host $line
}

function Set-A12([string]$Name, [string]$Status, [string]$Detail = "") {
    $script:Results[$Name] = [ordered]@{ Status = $Status; Detail = $Detail }
    if ($Status -eq "FAIL") { $script:FailCount++ }
    Write-A12 ("{0}: {1} - {2}" -f $Name, $Status, $Detail) $(if ($Status -eq "FAIL") { "ERROR" } else { "RESULT" })
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
            Set-A12 $Filter "PASS" $detail
            return $true
        }
        Set-A12 $Filter "FAIL" $detail
        return $false
    }
    Set-A12 $Filter "FAIL" $detail
    return $false
}

Write-A12 "=== FULL ASSURANCE-12 START ==="
Write-A12 ("HEAD={0}" -f (git -C $repoRoot rev-parse HEAD))
Write-A12 ("TAG_v1.0.0={0}" -f (git -C $repoRoot rev-parse v1.0.0))
Write-A12 ("OUT={0}" -f $outRoot)

Get-Process PrimoAutoEletrica -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

$proj = Join-Path $repoRoot "PrimoAutoEletrica\PrimoAutoEletrica.csproj"

Write-A12 "Build Debug..."
dotnet build $proj -c Debug -v q -nologo 2>&1 | Tee-Object (Join-Path $outRoot "build-debug.log") | Out-Null
if ($LASTEXITCODE -eq 0) { Set-A12 "Build-Debug" "PASS" "0 errors" } else { Set-A12 "Build-Debug" "FAIL" "exit=$LASTEXITCODE" }

Write-A12 "Build Release..."
dotnet build $proj -c Release -v q -nologo 2>&1 | Tee-Object (Join-Path $outRoot "build-release.log") | Out-Null
if ($LASTEXITCODE -eq 0) { Set-A12 "Build-Release" "PASS" "0 errors" } else { Set-A12 "Build-Release" "FAIL" "exit=$LASTEXITCODE" }

Write-A12 "Unit tests..."
dotnet test (Join-Path $repoRoot "Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj") -c Release --nologo --verbosity quiet 2>&1 | Tee-Object (Join-Path $outRoot "dotnet-test.log") | Out-Null
if ($LASTEXITCODE -eq 0) { Set-A12 "UnitTests" "PASS" "see log" } else { Set-A12 "UnitTests" "FAIL" "exit=$LASTEXITCODE" }

$secOut = Join-Path $outRoot "Security"
& (Join-Path $repoRoot "Scripts\QA\Invoke-SecurityRedTeam.ps1") -OutDir $secOut
if ($LASTEXITCODE -eq 0) { Set-A12 "SecurityRedTeam" "PASS" "static+sqlite probe" }
elseif ($LASTEXITCODE -eq 2) { Set-A12 "SecurityRedTeam" "FAIL" "critical findings" }
else { Set-A12 "SecurityRedTeam" "FAIL" "exit=$LASTEXITCODE" }

$pathOut = Join-Path $outRoot "PathSecurity"
& (Join-Path $repoRoot "Scripts\QA\Invoke-PathTraversalSimulation.ps1") -OutDir $pathOut
if ($LASTEXITCODE -eq 0) { Set-A12 "PathTraversalSim" "PASS" "sandbox canaries" } else { Set-A12 "PathTraversalSim" "FAIL" "exit=$LASTEXITCODE" }

Invoke-Smoke "A12Security" "A12Security" | Out-Null
if (-not $SkipBulk) {
    Invoke-Smoke "BulkDataQa12" "BulkDataQa12" -SkipBuild | Out-Null
}
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

$recOut = Join-Path $outRoot "Recovery"
& (Join-Path $repoRoot "Scripts\QA\Invoke-RecoverySimulation.ps1") -OutDir $recOut -Cycles $RecoveryCycles
if ($LASTEXITCODE -eq 0) { Set-A12 "Recovery" "PASS" "cycles=$RecoveryCycles" } else { Set-A12 "Recovery" "FAIL" "exit=$LASTEXITCODE" }

$dbOut = Join-Path $outRoot "Database"
& (Join-Path $repoRoot "Scripts\QA\Invoke-DatabaseIntegrity.ps1") -OutDir $dbOut
$dbJson = Join-Path $dbOut "database-integrity.json"
if (Test-Path $dbJson) {
    $dj = Get-Content $dbJson -Raw | ConvertFrom-Json
    Set-A12 "Database" $dj.Status ("integrity={0}; fk={1}; migrations={2}" -f $dj.Integrity, $dj.ForeignKeyViolations, $dj.Migrations)
} else {
    Set-A12 "Database" "BLOCKED" "no db json"
}

if (-not $SkipInstaller) {
    $setup = Join-Path $repoRoot "artifacts\installer\PRIMOX-Workshop-Setup-1.0.0-PackagingE2E.exe"
    if (-not (Test-Path $setup)) {
        Write-A12 "Building PackagingE2E setup..."
        & (Join-Path $repoRoot "Scripts\Build-PrimoXCommercialRelease.ps1") -SkipTests -SkipClean -AppId "PRIMOX.Workshop.PackagingE2E" -SetupNameSuffix "PackagingE2E"
    }
    & (Join-Path $repoRoot "Scripts\Test-CommercialInstallerHardening.ps1") -Cycles $InstallerCycles -SkipRebuild -SmokeFilter "Clientes"
    if ($LASTEXITCODE -eq 0) { Set-A12 "InstallerE2E" "PASS" "cycles=$InstallerCycles" } else { Set-A12 "InstallerE2E" "FAIL" "exit=$LASTEXITCODE" }

    & (Join-Path $repoRoot "Scripts\QA\Invoke-InstalledClientesSmoke.ps1") -Cycles $InstalledClientesCycles -SkipRebuild
    $icJson = Get-ChildItem (Join-Path $repoRoot "TestResults\FullAssurance12") -Recurse -Filter "installed-clientes.json" -EA SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($icJson) {
        Copy-Item $icJson.FullName (Join-Path $outRoot "installed-clientes.json") -Force
        $ic = Get-Content $icJson.FullName -Raw | ConvertFrom-Json
        Set-A12 "InstalledClientes" $ic.Status ("pass={0} fail={1} exit2={2}" -f $ic.Pass, $ic.Fail, $ic.Exit2Count)
    } elseif ($LASTEXITCODE -eq 0) {
        Set-A12 "InstalledClientes" "PASS" "exit=0"
    } else {
        Set-A12 "InstalledClientes" "FAIL" "exit=$LASTEXITCODE"
    }
} else {
    Set-A12 "InstallerE2E" "BLOCKED" "skipped"
    Set-A12 "InstalledClientes" "BLOCKED" "skipped"
}

& (Join-Path $repoRoot "Scripts\Sign-PRIMOX.ps1") -Mode Readiness 2>&1 | Tee-Object (Join-Path $outRoot "signing-readiness.log") | Out-Null
if ($LASTEXITCODE -eq 2) { Set-A12 "CodeSigning" "BLOCKED" "EXTERNAL CERTIFICATE" }
elseif ($LASTEXITCODE -eq 0) { Set-A12 "CodeSigning" "PASS" "READY" }
else { Set-A12 "CodeSigning" "FAIL" "exit=$LASTEXITCODE" }

$elapsed = (Get-Date) - $started
$hardNames = @("Build-Debug", "Build-Release", "UnitTests", "A12Security", "QaEngine", "Database")
$hardFails = @($hardNames | Where-Object {
    $script:Results.Contains($_) -and $script:Results[$_].Status -eq "FAIL"
})
$decision = "YELLOW"
if ($hardFails.Count -gt 0) { $decision = "RED" }

$summary = [ordered]@{
    GeneratedAt = (Get-Date).ToString("o")
    Head = (git -C $repoRoot rev-parse HEAD)
    TagV100 = (git -C $repoRoot rev-parse v1.0.0)
    DurationMinutes = [math]::Round($elapsed.TotalMinutes, 1)
    FailCount = $script:FailCount
    DecisionHint = $decision
    HardFails = $hardFails
    Results = $script:Results
}
# StrictMode-safe write
($summary | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $summaryPath -Encoding UTF8
Write-A12 ("=== FULL ASSURANCE-12 DONE decision={0} fails={1} minutes={2} ===" -f $decision, $script:FailCount, $summary.DurationMinutes)
exit $(if ($hardFails.Count -gt 0) { 1 } else { 0 })
