# PRIMOX FULL ASSURANCE-13 — startup / process counter profiling (QA only)
param(
    [int]$StartupCycles = 10,
    [string]$Configuration = "Release",
    [string]$Framework = "net6.0-windows",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not (Test-Path (Join-Path $repo "PrimoAutoEletrica\PrimoAutoEletrica.csproj"))) {
    $repo = Split-Path -Parent $PSScriptRoot
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repo ("TestResults\UiSmoke\a13-perf-profile-{0:yyyyMMdd-HHmmss}" -f (Get-Date))
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$exe = Join-Path $repo "PrimoAutoEletrica\bin\$Configuration\$Framework\PrimoAutoEletrica.exe"
if (-not (Test-Path $exe)) { throw "EXE nao encontrado: $exe" }

$samples = @()
for ($i = 1; $i -le $StartupCycles; $i++) {
    $appData = Join-Path $OutputDirectory ("startup-{0:D2}" -f $i)
    New-Item -ItemType Directory -Force -Path $appData | Out-Null
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $p = Start-Process -FilePath $exe -ArgumentList @(
        "--smoke-test",
        "--smoke-filter=MainWindow",
        "--app-data=$appData"
    ) -PassThru -WindowStyle Minimized
    $null = $p.WaitForExit(120000)
    $sw.Stop()
    if (-not $p.HasExited) {
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        throw "Startup cycle $i timeout"
    }
    $samples += [pscustomobject]@{
        Cycle = $i
        ExitCode = $p.ExitCode
        ElapsedMs = $sw.ElapsedMilliseconds
    }
    Write-Host ("startup {0}: exit={1} ms={2}" -f $i, $p.ExitCode, $sw.ElapsedMilliseconds)
}

$times = $samples | ForEach-Object { $_.ElapsedMs } | Sort-Object
$avg = [math]::Round(($times | Measure-Object -Average).Average, 1)
$median = if ($times.Count % 2 -eq 1) { $times[[int]($times.Count/2)] } else {
    [math]::Round(($times[$times.Count/2 - 1] + $times[$times.Count/2]) / 2.0, 1)
}

# Bulk profile from prior BulkDataQa13 process is not live; sample current smoke host via A13Performance counters file if present.
$summary = [ordered]@{
    GeneratedAt = (Get-Date).ToString("o")
    StartupCycles = $StartupCycles
    StartupMinMs = ($times | Measure-Object -Minimum).Minimum
    StartupMaxMs = ($times | Measure-Object -Maximum).Maximum
    StartupAvgMs = $avg
    StartupMedianMs = $median
    Failures = @($samples | Where-Object { $_.ExitCode -ne 0 }).Count
    Samples = $samples
}
$jsonPath = Join-Path $OutputDirectory "startup-profile.json"
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $jsonPath -Encoding UTF8
Write-Host "Wrote $jsonPath"
if ($summary.Failures -gt 0) { exit 1 } else { exit 0 }
