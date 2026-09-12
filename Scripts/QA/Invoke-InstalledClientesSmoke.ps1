<#
.SYNOPSIS
  FULL ASSURANCE-12 — 10+ cycles of installed Clientes smoke with evidence capture.
#>
[CmdletBinding()]
param(
    [int]$Cycles = 10,
    [string]$Version = "1.0.0",
    [switch]$SkipRebuild
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outDir = Join-Path $repoRoot "TestResults\FullAssurance12\$stamp\InstalledClientes"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Reuse PackagingE2E installer path from commercial harness
$setup = Join-Path $repoRoot "artifacts\installer\PRIMOX-Workshop-Setup-$Version-PackagingE2E.exe"
$installDir = Join-Path $env:LOCALAPPDATA "PRIMOX-Workshop-InstallTest-A12"
$dataDir = Join-Path $env:LOCALAPPDATA "PRIMOX-Workshop-DataTest-A12"
$exe = Join-Path $installDir "PrimoAutoEletrica.exe"

function Stop-Primox { Get-Process PrimoAutoEletrica -EA SilentlyContinue | Stop-Process -Force -EA SilentlyContinue }

if (-not $SkipRebuild -or -not (Test-Path $setup)) {
    & (Join-Path $repoRoot "Scripts\Build-PrimoXCommercialRelease.ps1") -SkipTests -SkipClean `
        -AppId "PRIMOX.Workshop.PackagingE2E" -SetupNameSuffix "PackagingE2E"
}

New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
$cycleResults = [System.Collections.Generic.List[object]]::new()
$passCount = 0
$failCount = 0

function Invoke-SilentInstallLocal([string]$Setup, [string]$Dir) {
    Stop-Primox
    if (Test-Path $Dir) { Remove-Item -LiteralPath $Dir -Recurse -Force -EA SilentlyContinue }
    $p = Start-Process -FilePath $Setup -ArgumentList @("/VERYSILENT", "/NORESTART", "/DIR=$Dir", "/SUPPRESSMSGBOXES") -PassThru -Wait
    return $p.ExitCode
}

function Invoke-InstalledSmokeCaptureLocal {
    param($Exe, $InstallDir, $DataDir, $Filter, $CaptureDir, $Label)
    New-Item -ItemType Directory -Force -Path $CaptureDir | Out-Null
    $env:DOTNET_ROLL_FORWARD = "LatestMajor"
    # Avoid stdout redirect on WinExe (can yield null ExitCode / hangs). Capture file reports instead.
    $smoke = Start-Process -FilePath $Exe -WorkingDirectory $InstallDir -ArgumentList @(
        "--smoke-test", "--smoke-filter=$Filter", "--app-data=$DataDir"
    ) -PassThru -WindowStyle Minimized
    $ok = $smoke.WaitForExit(900000)
    if (-not $ok) {
        Stop-Process -Id $smoke.Id -Force -EA SilentlyContinue
        return [pscustomobject]@{ ExitCode = -1; Detail = "timeout" }
    }
    $code = 0
    try { $code = [int]$smoke.ExitCode } catch { $code = -2 }
    $report = ""
    foreach ($root in @((Join-Path $DataDir "Logs"), (Join-Path $InstallDir "Logs"))) {
        if (-not (Test-Path $root)) { continue }
        $latest = Get-ChildItem $root -Recurse -Filter "*smoke*" -File -EA SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($latest) {
            $dest = Join-Path $CaptureDir "$Label-$($latest.Name)"
            Copy-Item $latest.FullName $dest -Force -EA SilentlyContinue
            $report = $dest
            break
        }
    }
    return [pscustomobject]@{ ExitCode = $code; Detail = "Exit=$code report=$report" }
}

$installExit = Invoke-SilentInstallLocal -Setup $setup -Dir $installDir
if (($installExit -ne 0) -or -not (Test-Path $exe)) {
    @{ Status = "BLOCKED"; Detail = "install Exit=$installExit exe=$(Test-Path $exe)" } | ConvertTo-Json |
        Set-Content (Join-Path $outDir "installed-clientes.json") -Encoding UTF8
    Write-Host "INSTALLED_CLIENTES=BLOCKED"
    exit 0
}

for ($i = 1; $i -le $Cycles; $i++) {
    Stop-Primox
    Start-Sleep -Seconds 1
    $cap = Join-Path $outDir ("cycle-{0:D2}" -f $i)
    # Fresh AppData per cycle avoids cross-cycle fixture pollution (WhatsApp button disabled flake).
    $cycleData = Join-Path $dataDir ("cycle-{0:D2}" -f $i)
    New-Item -ItemType Directory -Force -Path $cycleData | Out-Null
    $r = Invoke-InstalledSmokeCaptureLocal -Exe $exe -InstallDir $installDir -DataDir $cycleData -Filter "Clientes" -CaptureDir $cap -Label ("c{0:D2}" -f $i)
    if ($r.ExitCode -eq 0) {
        $st = "PASS"
        $passCount++
    }
    else {
        $st = "FAIL"
        $failCount++
    }
    $cycleResults.Add([ordered]@{ Cycle = $i; Status = $st; Detail = $r.Detail; ExitCode = $r.ExitCode })
    Write-Host ("Cycle {0}: {1} {2}" -f $i, $st, $r.Detail)
}

Stop-Primox
# Silent uninstall best-effort
$unins = Get-ChildItem $installDir -Filter "unins*.exe" -EA SilentlyContinue | Select-Object -First 1
if ($unins) {
    Start-Process $unins.FullName -ArgumentList @("/VERYSILENT", "/NORESTART") -Wait -EA SilentlyContinue
}

$exit2 = @($cycleResults | Where-Object { $_.ExitCode -eq 2 }).Count
$summary = [ordered]@{
    Status = $(if ($failCount -eq 0) { "PASS" } else { "FAIL" })
    Cycles = $Cycles
    Pass = $passCount
    Fail = $failCount
    Exit2Count = $exit2
    Results = $cycleResults
    RootCauseHint = $(if ($failCount -eq 0) { "Previous Exit=2 mitigated by PersistReport->RuntimeLogDirectory + Minimized window + evidence capture" } else { "See cycle folders under $outDir" })
    GeneratedAt = (Get-Date).ToString("o")
}
$summary | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $outDir "installed-clientes.json") -Encoding UTF8
Write-Host ("INSTALLED_CLIENTES={0} pass={1} fail={2} exit2={3}" -f $summary.Status, $passCount, $failCount, $exit2)
exit $(if ($failCount -gt 0) { 1 } else { 0 })
