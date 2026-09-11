<#
.SYNOPSIS
  PRIMOX-COMMERCIAL-08 hardening E2E: install / uninstall / reinstall / data preservation / cycles.

.DESCRIPTION
  Uses AppId PackagingE2E (isolated) so commercial install PRIMOX.Workshop.1 is not overwritten.
  Preserves production AppData. Seeds controlled markers in the isolated dataDir.
#>
[CmdletBinding()]
param(
    [string]$Version = "1.0.0",
    [int]$Cycles = 3,
    [switch]$SkipRebuild,
    [switch]$SkipInstalledSmoke,
    [string]$SmokeFilter = "QaEngine"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$e2eSetupName = "PRIMOX-Workshop-Setup-$Version-PackagingE2E.exe"
$setupPath = Join-Path $repoRoot "artifacts\installer\$e2eSetupName"
$installDir = Join-Path $env:LOCALAPPDATA "PRIMOX-Workshop-InstallTest-C08"
$dataDir = Join-Path $env:LOCALAPPDATA "PRIMOX-Workshop-DataTest-C08"
$reportDir = Join-Path $repoRoot "TestResults\Commercial08"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportPath = Join-Path $reportDir "commercial-08-e2e-$stamp.md"
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

$script:Results = [System.Collections.Generic.List[string]]::new()
$script:CycleMatrix = [System.Collections.Generic.List[string]]::new()
$script:FailCount = 0

function Write-Log {
    param([string]$Message)
    Write-Host ("[{0:HH:mm:ss}] {1}" -f (Get-Date), $Message)
}

function Add-Result {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Detail = ""
    )
    $script:Results.Add("| $Name | $Status | $Detail |")
    if ($Status -eq "FAIL") { $script:FailCount++ }
    Write-Log ("[{0}] {1} - {2}" -f $Status, $Name, $Detail)
}

function Stop-PrimoxProcesses {
    $procs = @(Get-Process -Name "PrimoAutoEletrica" -ErrorAction SilentlyContinue)
    foreach ($p in $procs) {
        Write-Log ("Stopping PID={0} Path={1}" -f $p.Id, $p.Path)
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2
    $left = @(Get-Process -Name "PrimoAutoEletrica" -ErrorAction SilentlyContinue)
    return ($left.Count -eq 0)
}

function Get-ProcessAudit {
    $procs = @(Get-Process -Name "PrimoAutoEletrica" -ErrorAction SilentlyContinue)
    $pidList = (@($procs | ForEach-Object { $_.Id }) -join ",")
    $pathList = (@($procs | ForEach-Object { $_.Path }) -join " ; ")
    return [PSCustomObject]@{
        Count = $procs.Count
        Pids  = $pidList
        Paths = $pathList
    }
}

function Invoke-SilentInstall {
    param(
        [string]$Setup,
        [string]$Dir
    )
    $argList = @(
        '/VERYSILENT',
        '/SUPPRESSMSGBOXES',
        '/NORESTART',
        '/CLOSEAPPLICATIONS',
        '/FORCECLOSEAPPLICATIONS',
        ("/DIR=`"{0}`"" -f $Dir),
        '/TASKS=!desktopicon'
    )
    $p = Start-Process -FilePath $Setup -ArgumentList $argList -Wait -PassThru
    return $p.ExitCode
}

function Invoke-SilentUninstall {
    param([string]$Dir)
    $unins = Get-ChildItem -LiteralPath $Dir -Filter "unins*.exe" -ErrorAction SilentlyContinue |
        Sort-Object Name |
        Select-Object -First 1
    if ($null -eq $unins) {
        return @{ ExitCode = -1; Detail = "unins missing"; TimedOut = $false }
    }
    $argList = @(
        '/VERYSILENT',
        '/SUPPRESSMSGBOXES',
        '/NORESTART',
        '/CLOSEAPPLICATIONS',
        '/FORCECLOSEAPPLICATIONS'
    )
    $p = Start-Process -FilePath $unins.FullName -ArgumentList $argList -PassThru
    $ok = $p.WaitForExit(240000)
    if (-not $ok) {
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        return @{ ExitCode = -9; Detail = "timeout 240s"; TimedOut = $true }
    }
    return @{ ExitCode = $p.ExitCode; Detail = ("Exit=" + $p.ExitCode); TimedOut = $false }
}

function Ensure-ControlledData {
    param([string]$DbPath)
    New-Item -ItemType Directory -Force -Path (Split-Path $DbPath) | Out-Null
    $proj = Join-Path $PSScriptRoot "tools\Commercial08DataSeed\Commercial08DataSeed.csproj"
    $env:DOTNET_ROLL_FORWARD = "LatestMajor"
    $out = & dotnet run --project $proj -c Release -- $DbPath seed 2>&1 | Out-String
    return $out
}

function Read-ControlledData {
    param([string]$DbPath)
    $proj = Join-Path $PSScriptRoot "tools\Commercial08DataSeed\Commercial08DataSeed.csproj"
    $env:DOTNET_ROLL_FORWARD = "LatestMajor"
    $out = & dotnet run --project $proj -c Release -- $DbPath verify 2>&1 | Out-String
    return $out
}

Write-Log "=== PRIMOX COMMERCIAL-08 E2E ==="
Write-Log "Repo: $repoRoot"
[void](Stop-PrimoxProcesses)

if (-not $SkipRebuild) {
    $env:DOTNET_ROLL_FORWARD = "LatestMajor"
    & (Join-Path $repoRoot "Scripts\Build-PrimoXCommercialRelease.ps1") `
        -Version $Version `
        -SkipTests `
        -AppId "PRIMOX.Workshop.PackagingE2E" `
        -SetupNameSuffix "PackagingE2E"
    $setupPath = Join-Path $repoRoot "artifacts\installer\$e2eSetupName"
}
elseif (-not (Test-Path -LiteralPath $setupPath)) {
    throw "Setup missing: $setupPath (run without -SkipRebuild)"
}

if (-not (Test-Path -LiteralPath $setupPath)) {
    throw "Setup not found: $setupPath"
}

$hash = (Get-FileHash -LiteralPath $setupPath -Algorithm SHA256).Hash
$size = (Get-Item -LiteralPath $setupPath).Length
Add-Result -Name "Setup exists" -Status "PASS" -Detail $setupPath
Add-Result -Name "Setup SHA256" -Status "PASS" -Detail $hash
Add-Result -Name "Setup size" -Status "PASS" -Detail ("$size bytes")

if (Test-Path $installDir) { Remove-Item $installDir -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path $dataDir) { Remove-Item $dataDir -Recurse -Force -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
$dbPath = Join-Path $dataDir "primoauto.db"
$prodMarker = Join-Path $env:LOCALAPPDATA "PrimoAutoEletrica\c08-retention-marker.txt"
New-Item -ItemType Directory -Force -Path (Split-Path $prodMarker) | Out-Null
Set-Content -LiteralPath $prodMarker -Value ("c08-retain-" + (Get-Date -Format o)) -Encoding UTF8

$exe = Join-Path $installDir "PrimoAutoEletrica.exe"

for ($i = 1; $i -le $Cycles; $i++) {
    Write-Log ("===== CYCLE {0} / {1} =====" -f $i, $Cycles)
    [void](Stop-PrimoxProcesses)
    $before = Get-ProcessAudit
    Add-Result -Name ("C{0} pre-process cleanup" -f $i) -Status $(if ($before.Count -eq 0) { "PASS" } else { "FAIL" }) -Detail ("count=" + $before.Count)

    $exitInstall = Invoke-SilentInstall -Setup $setupPath -Dir $installDir
    $installOk = ($exitInstall -eq 0) -and (Test-Path $exe)
    Add-Result -Name ("C{0} Install" -f $i) -Status $(if ($installOk) { "PASS" } else { "FAIL" }) -Detail ("Exit=$exitInstall")
    if (-not $installOk) {
        $script:CycleMatrix.Add("| $i | FAIL | - | - | - | Install Exit=$exitInstall |")
        break
    }

    $vi = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
    Add-Result -Name ("C{0} Version" -f $i) -Status "PASS" -Detail ("PV=" + $vi.ProductVersion + " FV=" + $vi.FileVersion)

    $ui = Start-Process -FilePath $exe -WorkingDirectory $installDir -PassThru
    Start-Sleep -Seconds 8
    $ui.Refresh()
    $startupOk = (-not $ui.HasExited) -and $ui.Responding
    Add-Result -Name ("C{0} Startup" -f $i) -Status $(if ($startupOk) { "PASS" } else { "FAIL" }) -Detail ("PID=$($ui.Id) Responding=$($ui.Responding)")
    Stop-Process -Id $ui.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    [void](Stop-PrimoxProcesses)

    if ((-not $SkipInstalledSmoke) -and ($i -eq 1)) {
        $smoke = Start-Process -FilePath $exe -WorkingDirectory $installDir -ArgumentList @(
            "--smoke-test",
            ("--smoke-filter=" + $SmokeFilter),
            ("--app-data=" + $dataDir)
        ) -PassThru -WindowStyle Hidden
        $finished = $smoke.WaitForExit(600000)
        if (-not $finished) {
            Stop-Process -Id $smoke.Id -Force -ErrorAction SilentlyContinue
            Add-Result -Name ("C{0} Installed smoke" -f $i) -Status "FAIL" -Detail "timeout"
        }
        else {
            Add-Result -Name ("C{0} Installed smoke" -f $i) -Status $(if ($smoke.ExitCode -eq 0) { "PASS" } else { "FAIL" }) -Detail ("Exit=" + $smoke.ExitCode)
        }
        [void](Stop-PrimoxProcesses)
    }

    New-Item -ItemType Directory -Force -Path (Split-Path $dbPath) | Out-Null
    $seedOut = Ensure-ControlledData -DbPath $dbPath
    $seedOk = $seedOut -match "seed=ok"
    Add-Result -Name ("C{0} Controlled data seed" -f $i) -Status $(if ($seedOk) { "PASS" } else { "FAIL" }) -Detail (($seedOut.Trim() -replace "`r?`n", " ; "))

    [void](Stop-PrimoxProcesses)
    $mid = Get-ProcessAudit
    Add-Result -Name ("C{0} pre-uninstall process" -f $i) -Status $(if ($mid.Count -eq 0) { "PASS" } else { "FAIL" }) -Detail ("count=" + $mid.Count + " pids=" + $mid.Pids)

    $un = Invoke-SilentUninstall -Dir $installDir
    $exeGone = -not (Test-Path $exe)
    $unOk = ($un.ExitCode -eq 0) -and $exeGone -and (-not $un.TimedOut)
    Add-Result -Name ("C{0} Uninstall" -f $i) -Status $(if ($unOk) { "PASS" } else { "FAIL" }) -Detail ($un.Detail + " exeGone=$exeGone")
    Add-Result -Name ("C{0} Binary cleanup" -f $i) -Status $(if ($exeGone) { "PASS" } else { "FAIL" }) -Detail ("installDirExists=" + (Test-Path $installDir))

    $dataOk = (Test-Path $dbPath) -and (Test-Path $prodMarker)
    Add-Result -Name ("C{0} Data preservation" -f $i) -Status $(if ($dataOk) { "PASS" } else { "FAIL" }) -Detail ("db=" + (Test-Path $dbPath) + " marker=" + (Test-Path $prodMarker))

    $verifyOut = if (Test-Path $dbPath) { Read-ControlledData -DbPath $dbPath } else { "found=" }
    $dataFound = ($verifyOut -match "PRIMOX INSTALLER TEST CLIENT") -and ($verifyOut -match "PRIMOX-TEST-001")
    Add-Result -Name ("C{0} Data still readable" -f $i) -Status $(if ($dataFound) { "PASS" } else { "FAIL" }) -Detail (($verifyOut.Trim() -replace "`r?`n", " ; "))

    $post = Get-ProcessAudit
    Add-Result -Name ("C{0} post-uninstall process" -f $i) -Status $(if ($post.Count -eq 0) { "PASS" } else { "FAIL" }) -Detail ("count=" + $post.Count)

    $rowInstall = if ($installOk) { "PASS" } else { "FAIL" }
    $rowStart = if ($startupOk) { "PASS" } else { "FAIL" }
    $rowUn = if ($unOk) { "PASS" } else { "FAIL" }
    $rowData = if ($dataOk -and $dataFound) { "PASS" } else { "FAIL" }
    $script:CycleMatrix.Add("| $i | $rowInstall | $rowStart | $rowUn | $rowData | ExitInstall=$exitInstall; $($un.Detail) |")

    if (-not $unOk) { break }
}

Write-Log "===== FINAL REINSTALL + VERIFY ====="
[void](Stop-PrimoxProcesses)
$exitFinal = Invoke-SilentInstall -Setup $setupPath -Dir $installDir
$finalInstallOk = ($exitFinal -eq 0) -and (Test-Path $exe)
Add-Result -Name "Final reinstall" -Status $(if ($finalInstallOk) { "PASS" } else { "FAIL" }) -Detail ("Exit=$exitFinal")

if ($finalInstallOk) {
    $ui2 = Start-Process -FilePath $exe -WorkingDirectory $installDir -PassThru
    Start-Sleep -Seconds 8
    $ui2.Refresh()
    $finalStart = (-not $ui2.HasExited) -and $ui2.Responding
    Add-Result -Name "Final startup" -Status $(if ($finalStart) { "PASS" } else { "FAIL" }) -Detail ("PID=" + $ui2.Id)
    Stop-Process -Id $ui2.Id -Force -ErrorAction SilentlyContinue
    [void](Stop-PrimoxProcesses)

    $finalData = Read-ControlledData -DbPath $dbPath
    $finalDataOk = $finalData -match "PRIMOX INSTALLER TEST CLIENT"
    Add-Result -Name "Final data after reinstall" -Status $(if ($finalDataOk) { "PASS" } else { "FAIL" }) -Detail (($finalData.Trim() -replace "`r?`n", " ; "))

    if (-not $SkipInstalledSmoke) {
        $smoke2 = Start-Process -FilePath $exe -WorkingDirectory $installDir -ArgumentList @(
            "--smoke-test",
            "--smoke-filter=Clientes",
            ("--app-data=" + $dataDir)
        ) -PassThru -WindowStyle Hidden
        $fin2 = $smoke2.WaitForExit(600000)
        if (-not $fin2) {
            Stop-Process -Id $smoke2.Id -Force -ErrorAction SilentlyContinue
            Add-Result -Name "Final CRUD smoke" -Status "FAIL" -Detail "timeout"
        }
        else {
            Add-Result -Name "Final CRUD smoke" -Status $(if ($smoke2.ExitCode -eq 0) { "PASS" } else { "FAIL" }) -Detail ("Exit=" + $smoke2.ExitCode)
        }
        [void](Stop-PrimoxProcesses)
    }

    $group = Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs\PRIMOX Workshop PackagingE2E"
    $groupAlt = Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs\PRIMOX Workshop"
    $groupOk = (Test-Path $group) -or (Test-Path $groupAlt)
    Add-Result -Name "Start Menu group" -Status $(if ($groupOk) { "PASS" } else { "FAIL" }) -Detail ("e2e=" + (Test-Path $group) + " commercial=" + (Test-Path $groupAlt))

    $unFinal = Invoke-SilentUninstall -Dir $installDir
    Add-Result -Name "Final cleanup uninstall" -Status $(if (($unFinal.ExitCode -eq 0) -or (-not (Test-Path $exe))) { "PASS" } else { "FAIL" }) -Detail $unFinal.Detail
}

$decision = if ($script:FailCount -eq 0) { "GREEN" } elseif ($script:FailCount -le 2) { "YELLOW" } else { "RED" }

$table = ($script:Results -join "`n")
$matrix = ($script:CycleMatrix -join "`n")
$md = @"
# COMMERCIAL-08 E2E $stamp

Setup: ``$setupPath``
SHA256: ``$hash``
Size: $size
InstallDir: ``$installDir``
DataDir: ``$dataDir``
Cycles: $Cycles
FailCount: $($script:FailCount)
Decision hint: **$decision**

## Results

| Teste | Status | Detalhe |
|-------|--------|---------|
$table

## Cycle matrix

| Ciclo | Install | Startup | Uninstall | Data | Notes |
|-------|---------|---------|-----------|------|-------|
$matrix
"@
Set-Content -LiteralPath $reportPath -Value $md -Encoding UTF8
Write-Log "Report: $reportPath"
Write-Log ("=== COMMERCIAL-08 E2E DONE fails={0} ===" -f $script:FailCount)
if ($script:FailCount -gt 0) { exit 1 } else { exit 0 }
