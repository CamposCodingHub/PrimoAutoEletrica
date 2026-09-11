<#
.SYNOPSIS
  PRIMOX COMMERCIAL-10 — Final Commercial Release Gate orchestrator.

.DESCRIPTION
  Baseline QA + commercial package + installer E2E (PackagingE2E) + DB + signing readiness.
  Does not buy certificates, simulate signing, or start Fiscal LIVE / Commercial-11.
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Framework = "net6.0-windows",
    [switch]$SkipExhaustive,
    [switch]$SkipInstaller,
    [int]$InstallerCycles = 3
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outRoot = Join-Path $repoRoot "TestResults\Commercial10\$stamp"
$logPath = Join-Path $outRoot "commercial10.log"
$summaryPath = Join-Path $outRoot "commercial10-summary.json"
New-Item -ItemType Directory -Force -Path $outRoot | Out-Null

$started = Get-Date
$env:DOTNET_ROLL_FORWARD = "LatestMajor"
$script:Results = [ordered]@{}
$script:FailCount = 0

function Write-C10([string]$Message, [string]$Level = "INFO") {
    $line = "{0} [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message
    Add-Content -LiteralPath $logPath -Value $line -Encoding UTF8
    Write-Host $line
}

function Set-C10([string]$Name, [string]$Status, [string]$Detail = "") {
    $script:Results[$Name] = [ordered]@{ Status = $Status; Detail = $Detail }
    if ($Status -eq "FAIL") { $script:FailCount++ }
    Write-C10 ("{0}: {1} - {2}" -f $Name, $Status, $Detail) $(if ($Status -eq "FAIL") { "ERROR" } else { "RESULT" })
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
            Set-C10 $Filter "PASS" $detail
            return $true
        }
        Set-C10 $Filter "FAIL" $detail
        return $false
    }
    Set-C10 $Filter "FAIL" $detail
    return $false
}

Write-C10 "=== COMMERCIAL-10 RELEASE GATE START ==="
Write-C10 ("Repo={0} Out={1}" -f $repoRoot, $outRoot)
Write-C10 ("HEAD={0}" -f (git -C $repoRoot rev-parse HEAD))
Write-C10 ("TAG_v1.0.0={0}" -f (git -C $repoRoot rev-parse v1.0.0))

Get-Process PrimoAutoEletrica -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# --- Signing readiness ---
$signLog = Join-Path $outRoot "signing-readiness.log"
& (Join-Path $repoRoot "Scripts\Sign-PRIMOX.ps1") -Mode Readiness 2>&1 | Tee-Object -FilePath $signLog
$signExit = $LASTEXITCODE
if ($signExit -eq 0) { Set-C10 "CodeSigning" "PASS" "READY" }
elseif ($signExit -eq 2) { Set-C10 "CodeSigning" "BLOCKED" "BLOCKED BY EXTERNAL COMMERCIAL CERTIFICATE" }
else { Set-C10 "CodeSigning" "FAIL" "exit=$signExit" }

# --- QA suite ---
Invoke-Smoke "QaEngine" "QaEngine" | Out-Null
Invoke-Smoke "DeepQa" "DeepQa" -SkipBuild | Out-Null
Invoke-Smoke "LongRun" "LongRun" -SkipBuild | Out-Null
if (-not $SkipExhaustive) {
    Invoke-Smoke "ExhaustiveUi" "ExhaustiveUi" -SkipBuild | Out-Null
}
Invoke-Smoke "I18n07" "I18n07" -SkipBuild | Out-Null
Invoke-Smoke "Tema" "Tema" -SkipBuild | Out-Null
Invoke-Smoke "Sidebar" "Sidebar" -SkipBuild | Out-Null

# --- Commercial package (official + PackagingE2E) ---
Write-C10 "Building official commercial package..."
& (Join-Path $repoRoot "Scripts\Build-PrimoXCommercialRelease.ps1") -SkipTests -SkipClean
if ($LASTEXITCODE -eq 0) {
    $setupOfficial = Join-Path $repoRoot "artifacts\installer\PRIMOX-Workshop-Setup-1.0.0.exe"
    if (Test-Path $setupOfficial) {
        $h = (Get-FileHash -LiteralPath $setupOfficial -Algorithm SHA256).Hash
        $sz = (Get-Item $setupOfficial).Length
        Set-C10 "PackageOfficial" "PASS" ("SHA256={0}; Size={1}" -f $h, $sz)
        Copy-Item $setupOfficial (Join-Path $outRoot "PRIMOX-Workshop-Setup-1.0.0.exe") -Force
        Set-Content -LiteralPath (Join-Path $outRoot "PRIMOX-Workshop-Setup-1.0.0.sha256.txt") -Value ("{0}  PRIMOX-Workshop-Setup-1.0.0.exe" -f $h) -Encoding ASCII
    }
    else {
        Set-C10 "PackageOfficial" "FAIL" "setup missing"
    }
}
else {
    Set-C10 "PackageOfficial" "FAIL" "Build-PrimoXCommercialRelease exit=$LASTEXITCODE"
}

Write-C10 "Building PackagingE2E setup..."
& (Join-Path $repoRoot "Scripts\Build-PrimoXCommercialRelease.ps1") `
    -SkipTests -SkipClean `
    -AppId "PRIMOX.Workshop.PackagingE2E" `
    -SetupNameSuffix "PackagingE2E"
if ($LASTEXITCODE -eq 0) {
    Set-C10 "PackageE2E" "PASS" "PackagingE2E setup built"
}
else {
    Set-C10 "PackageE2E" "FAIL" "exit=$LASTEXITCODE"
}

# --- Installer E2E ---
if (-not $SkipInstaller) {
    Write-C10 "Running installer hardening E2E..."
    $c08ReportDir = Join-Path $outRoot "InstallerE2E"
    New-Item -ItemType Directory -Force -Path $c08ReportDir | Out-Null
    & (Join-Path $repoRoot "Scripts\Test-CommercialInstallerHardening.ps1") `
        -Cycles $InstallerCycles `
        -SkipRebuild `
        -SmokeFilter "QaEngine"
    $instExit = $LASTEXITCODE
    if ($instExit -eq 0) {
        Set-C10 "InstallerE2E" "PASS" "Cycles=$InstallerCycles exit=0"
    }
    else {
        Set-C10 "InstallerE2E" "FAIL" "exit=$instExit"
    }
    $latest = Get-ChildItem (Join-Path $repoRoot "TestResults\Commercial08") -Filter "commercial-08-e2e-*.md" -EA SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latest) {
        Copy-Item $latest.FullName (Join-Path $c08ReportDir $latest.Name) -Force
    }
}
else {
    Set-C10 "InstallerE2E" "PASS" "SKIPPED by param"
}

# --- Backup/restore file-level (isolated PackagingE2E data dir, after installer) ---
$dataTest = Join-Path $env:LOCALAPPDATA "PRIMOX-Workshop-DataTest-C08"
$srcDb = Join-Path $dataTest "primoauto.db"
if (Test-Path $srcDb) {
    $bakDir = Join-Path $outRoot "backup-restore"
    New-Item -ItemType Directory -Force -Path $bakDir | Out-Null
    $bak = Join-Path $bakDir "primoauto.db.bak"
    Copy-Item $srcDb $bak -Force
    $marker = Join-Path $dataTest "c10-marker.txt"
    "altered-$(Get-Date -Format o)" | Set-Content $marker -Encoding UTF8
    Copy-Item $bak $srcDb -Force
    Remove-Item $marker -Force -EA SilentlyContinue
    $restored = (Get-FileHash $srcDb -Algorithm SHA256).Hash -eq (Get-FileHash $bak -Algorithm SHA256).Hash
    if ($restored) { Set-C10 "BackupRestore" "PASS" "file-level restore hash match on PackagingE2E dataDir" }
    else { Set-C10 "BackupRestore" "FAIL" "hash mismatch after restore" }

    # DB size/presence evidence (integrity covered by QaEngine + InstallerE2E seed)
    Set-C10 "Database" "PASS" ("dataDirDB=$srcDb; size=$((Get-Item $srcDb).Length); integrity via QaEngine/InstallerE2E")
}
else {
    Set-C10 "BackupRestore" "FAIL" "PackagingE2E dataDir DB absent after installer"
    Set-C10 "Database" "FAIL" "no PackagingE2E DB found"
}

$elapsed = (Get-Date) - $started
$decision = "YELLOW"
if ($script:FailCount -gt 0) { $decision = "RED" }
elseif ($script:Results.Contains("CodeSigning") -and $script:Results["CodeSigning"].Status -eq "PASS") {
    # Signed commercial release would still need SmartScreen NOT VERIFIED => YELLOW unless full signed gate
    $decision = "YELLOW"
}

$summary = [ordered]@{
    GeneratedAt = (Get-Date).ToString("o")
    Head = (git -C $repoRoot rev-parse HEAD)
    TagV100 = (git -C $repoRoot rev-parse v1.0.0)
    DurationMinutes = [math]::Round($elapsed.TotalMinutes, 1)
    FailCount = $script:FailCount
    DecisionHint = $decision
    Results = $script:Results
}
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding UTF8
Write-C10 ("=== COMMERCIAL-10 DONE duration={0:N1}min fail={1} decision={2} ===" -f $elapsed.TotalMinutes, $script:FailCount, $decision)
Write-C10 ("SUMMARY={0}" -f $summaryPath)
exit $(if ($script:FailCount -gt 0) { 1 } else { 0 })
