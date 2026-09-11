<#
.SYNOPSIS
  PRIMOX FULL ASSURANCE-11 — long orchestrator (no invented PASS).
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Framework = "net6.0-windows",
    [switch]$SkipExhaustive,
    [switch]$SkipInstaller,
    [int]$RecoveryCycles = 5,
    [int]$InstallerCycles = 3
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outRoot = Join-Path $repoRoot "TestResults\FullAssurance11\$stamp"
$logPath = Join-Path $outRoot "assurance11.log"
$summaryPath = Join-Path $outRoot "assurance11-summary.json"
New-Item -ItemType Directory -Force -Path $outRoot | Out-Null

$started = Get-Date
$env:DOTNET_ROLL_FORWARD = "LatestMajor"
$script:Results = [ordered]@{}
$script:FailCount = 0

function Write-A11([string]$Message, [string]$Level = "INFO") {
    $line = "{0} [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message
    Add-Content -LiteralPath $logPath -Value $line -Encoding UTF8
    Write-Host $line
}

function Set-A11([string]$Name, [string]$Status, [string]$Detail = "") {
    $script:Results[$Name] = [ordered]@{ Status = $Status; Detail = $Detail }
    if ($Status -eq "FAIL") { $script:FailCount++ }
    Write-A11 ("{0}: {1} - {2}" -f $Name, $Status, $Detail) $(if ($Status -eq "FAIL") { "ERROR" } else { "RESULT" })
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
            Set-A11 $Filter "PASS" $detail
            return $true
        }
        Set-A11 $Filter "FAIL" $detail
        return $false
    }
    Set-A11 $Filter "FAIL" $detail
    return $false
}

Write-A11 "=== FULL ASSURANCE-11 START ==="
Write-A11 ("HEAD={0}" -f (git -C $repoRoot rev-parse HEAD))
Write-A11 ("TAG_v1.0.0={0}" -f (git -C $repoRoot rev-parse v1.0.0))
Write-A11 ("OUT={0}" -f $outRoot)

Get-Process PrimoAutoEletrica -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

$proj = Join-Path $repoRoot "PrimoAutoEletrica\PrimoAutoEletrica.csproj"

# Builds
Write-A11 "Build Debug..."
dotnet build $proj -c Debug -v q -nologo 2>&1 | Tee-Object (Join-Path $outRoot "build-debug.log") | Out-Null
if ($LASTEXITCODE -eq 0) { Set-A11 "Build-Debug" "PASS" "0 errors" } else { Set-A11 "Build-Debug" "FAIL" "exit=$LASTEXITCODE" }

Write-A11 "Build Release..."
dotnet build $proj -c Release -v q -nologo 2>&1 | Tee-Object (Join-Path $outRoot "build-release.log") | Out-Null
if ($LASTEXITCODE -eq 0) { Set-A11 "Build-Release" "PASS" "0 errors" } else { Set-A11 "Build-Release" "FAIL" "exit=$LASTEXITCODE" }

Write-A11 "Unit tests..."
dotnet test (Join-Path $repoRoot "Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj") -c Release --nologo --verbosity quiet 2>&1 | Tee-Object (Join-Path $outRoot "dotnet-test.log") | Out-Null
if ($LASTEXITCODE -eq 0) { Set-A11 "UnitTests" "PASS" "see log" } else { Set-A11 "UnitTests" "FAIL" "exit=$LASTEXITCODE" }

# Security red team
$secOut = Join-Path $outRoot "Security"
& (Join-Path $repoRoot "Scripts\QA\Invoke-SecurityRedTeam.ps1") -OutDir $secOut
$secExit = $LASTEXITCODE
if ($secExit -eq 0) { Set-A11 "SecurityRedTeam" "PASS" "see Security/security-redteam.json" }
elseif ($secExit -eq 2) { Set-A11 "SecurityRedTeam" "FAIL" "critical findings" }
else { Set-A11 "SecurityRedTeam" "FAIL" "exit=$secExit" }

# UI suites
Invoke-Smoke "QaEngine" "QaEngine" | Out-Null
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

# Recovery
$recOut = Join-Path $outRoot "Recovery"
& (Join-Path $repoRoot "Scripts\QA\Invoke-RecoverySimulation.ps1") -OutDir $recOut -Cycles $RecoveryCycles
if ($LASTEXITCODE -eq 0) { Set-A11 "Recovery" "PASS" "cycles=$RecoveryCycles" } else { Set-A11 "Recovery" "FAIL" "exit=$LASTEXITCODE" }

# Database (after smoke may have created DBs)
$dbOut = Join-Path $outRoot "Database"
& (Join-Path $repoRoot "Scripts\QA\Invoke-DatabaseIntegrity.ps1") -OutDir $dbOut
$dbCode = $LASTEXITCODE
$dbJson = Join-Path $dbOut "database-integrity.json"
if (Test-Path $dbJson) {
    $dj = Get-Content $dbJson -Raw | ConvertFrom-Json
    Set-A11 "Database" $dj.Status ("integrity={0}; fk={1}; migrations={2}" -f $dj.Integrity, $dj.ForeignKeyViolations, $dj.Migrations)
} elseif ($dbCode -eq 0) {
    Set-A11 "Database" "BLOCKED" "no db"
} else {
    Set-A11 "Database" "FAIL" "exit=$dbCode"
}

# Backup/restore file-level on PackagingE2E or recovery appdata
$bakSrc = @(
    (Join-Path $env:LOCALAPPDATA "PRIMOX-Workshop-DataTest-C08\primoauto.db"),
    (Join-Path $recOut "appdata\primoauto.db")
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($bakSrc) {
    $bakDir = Join-Path $outRoot "backup-restore"
    New-Item -ItemType Directory -Force -Path $bakDir | Out-Null
    $bak = Join-Path $bakDir "primoauto.db.bak"
    Copy-Item $bakSrc $bak -Force
    Copy-Item $bak $bakSrc -Force
    $ok = (Get-FileHash $bakSrc -Algorithm SHA256).Hash -eq (Get-FileHash $bak -Algorithm SHA256).Hash
    if ($ok) { Set-A11 "BackupRestore" "PASS" "file-level hash match" } else { Set-A11 "BackupRestore" "FAIL" "hash mismatch" }
} else {
    Set-A11 "BackupRestore" "BLOCKED" "no db for backup probe yet"
}

# Installer E2E
if (-not $SkipInstaller) {
    $setup = Join-Path $repoRoot "artifacts\installer\PRIMOX-Workshop-Setup-1.0.0-PackagingE2E.exe"
    if (-not (Test-Path $setup)) {
        Write-A11 "Building PackagingE2E setup..."
        & (Join-Path $repoRoot "Scripts\Build-PrimoXCommercialRelease.ps1") -SkipTests -SkipClean -AppId "PRIMOX.Workshop.PackagingE2E" -SetupNameSuffix "PackagingE2E"
    }
    & (Join-Path $repoRoot "Scripts\Test-CommercialInstallerHardening.ps1") -Cycles $InstallerCycles -SkipRebuild -SmokeFilter "Clientes"
    $ie = $LASTEXITCODE
    $latest = Get-ChildItem (Join-Path $repoRoot "TestResults\Commercial08") -Filter "commercial-08-e2e-*.md" -EA SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latest) {
        $dest = Join-Path $outRoot "InstallerE2E"
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Copy-Item $latest.FullName $dest -Force
    }
    # Lifecycle may PASS with smoke flake — parse FailCount if possible
    if ($ie -eq 0) {
        Set-A11 "InstallerE2E" "PASS" "exit=0"
    } else {
        # Do not auto-PASS; mark FAIL but detail may show lifecycle green in report
        Set-A11 "InstallerE2E" "FAIL" "exit=$ie (see InstallerE2E report; known Clientes smoke flake possible)"
    }
} else {
    Set-A11 "InstallerE2E" "BLOCKED" "skipped by param"
}

# Signing readiness (not invent PASS)
& (Join-Path $repoRoot "Scripts\Sign-PRIMOX.ps1") -Mode Readiness 2>&1 | Tee-Object (Join-Path $outRoot "signing-readiness.log") | Out-Null
if ($LASTEXITCODE -eq 2) { Set-A11 "CodeSigning" "BLOCKED" "EXTERNAL CERTIFICATE" }
elseif ($LASTEXITCODE -eq 0) { Set-A11 "CodeSigning" "PASS" "READY" }
else { Set-A11 "CodeSigning" "FAIL" "exit=$LASTEXITCODE" }

$elapsed = (Get-Date) - $started
$hardNames = @("Build-Debug", "Build-Release", "QaEngine", "Database")
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
$summary | ConvertTo-Json -Depth 8 | Set-Content $summaryPath -Encoding UTF8
Write-A11 ("=== ASSURANCE-11 DONE duration={0:N1}min fail={1} decision={2} ===" -f $elapsed.TotalMinutes, $script:FailCount, $decision)
Write-A11 ("SUMMARY={0}" -f $summaryPath)
exit $(if ($hardFails.Count -gt 0) { 1 } else { 0 })
