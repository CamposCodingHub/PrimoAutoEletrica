<#
.SYNOPSIS
  FULL ASSURANCE-11 — controlled process kill/restart recovery on Release binary (test AppData).
#>
[CmdletBinding()]
param(
    [string]$OutDir = "",
    [int]$Cycles = 5
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not $OutDir) {
    $OutDir = Join-Path $repoRoot ("TestResults\FullAssurance11\{0}\Recovery" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
}
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$exeCandidates = @(
    (Join-Path $repoRoot "PrimoAutoEletrica\bin\Release\net6.0-windows\PrimoAutoEletrica.exe"),
    (Join-Path $repoRoot "artifacts\publish\win-x64\PrimoAutoEletrica.exe")
)
$exe = $exeCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
$summary = Join-Path $OutDir "recovery.json"

if (-not $exe) {
    @{ Status = "BLOCKED"; Detail = "EXE not found" } | ConvertTo-Json | Set-Content $summary
    Write-Host "RECOVERY=BLOCKED"
    exit 0
}

$dataDir = Join-Path $OutDir "appdata"
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
$results = [System.Collections.Generic.List[object]]::new()
$fail = 0

for ($i = 1; $i -le $Cycles; $i++) {
    Get-Process PrimoAutoEletrica -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    $p = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -ArgumentList @("--app-data=$dataDir") -PassThru
    Start-Sleep -Seconds 6
    $p.Refresh()
    $started = (-not $p.HasExited)
    if ($started) {
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        # restart
        $p2 = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -ArgumentList @("--app-data=$dataDir") -PassThru
        Start-Sleep -Seconds 6
        $p2.Refresh()
        $restartOk = (-not $p2.HasExited) -and $p2.Responding
        Stop-Process -Id $p2.Id -Force -ErrorAction SilentlyContinue
        $st = if ($restartOk) { "PASS" } else { "FAIL"; $fail++ }
        $results.Add([ordered]@{ Cycle = $i; Start = "PASS"; Kill = "PASS"; Restart = $st })
    }
    else {
        $fail++
        $results.Add([ordered]@{ Cycle = $i; Start = "FAIL"; Kill = "N/A"; Restart = "N/A" })
    }
}

Get-Process PrimoAutoEletrica -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
$status = if ($fail -eq 0) { "PASS" } else { "FAIL" }
[ordered]@{
    Status = $status
    FailCount = $fail
    Cycles = $Cycles
    Exe = $exe
    Results = $results
    GeneratedAt = (Get-Date).ToString("o")
} | ConvertTo-Json -Depth 6 | Set-Content $summary -Encoding UTF8
Write-Host "RECOVERY=$status"
exit $(if ($fail -gt 0) { 1 } else { 0 })
