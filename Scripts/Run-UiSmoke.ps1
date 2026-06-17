[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [string]$Framework = "net9.0-windows",
    [string]$SmokeFilter = "",
    [switch]$SkipBuild,
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$appProject = Join-Path $projectRoot "PrimoAutoEletrica\PrimoAutoEletrica.csproj"
$reportRoot = Join-Path $projectRoot "PrimoAutoEletrica\bin\$Configuration\$Framework\Logs\smoke-tests"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectRoot "TestResults\UiSmoke\$timestamp"
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$logFile = Join-Path $OutputDirectory "run-ui-smoke.log"
$commandLog = Join-Path $OutputDirectory "dotnet-run-ui-smoke.log"
$summaryPath = Join-Path $OutputDirectory "ui-smoke-summary.json"

function Write-Log {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )

    $entry = "{0} [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message
    Add-Content -Path $logFile -Value $entry
    Write-Host $entry
}

function Get-LatestReportFile {
    param([string]$Directory)

    if (-not (Test-Path $Directory)) {
        return $null
    }

    return Get-ChildItem -Path $Directory -File -Filter "*.txt" |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}

function Get-GeneratedReportFile {
    param(
        [string]$Directory,
        [datetime]$StartedAtUtc,
        [System.IO.FileInfo]$PreviousReport
    )

    if (-not (Test-Path $Directory)) {
        return $null
    }

    $newReport = Get-ChildItem -Path $Directory -File -Filter "*.txt" |
        Where-Object { $_.LastWriteTimeUtc -ge $StartedAtUtc.AddSeconds(-2) } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -ne $newReport) {
        return $newReport
    }

    $latestReport = Get-LatestReportFile -Directory $Directory
    if ($null -eq $latestReport) {
        return $null
    }

    if ($null -eq $PreviousReport) {
        return $latestReport
    }

    if ($latestReport.FullName -ne $PreviousReport.FullName -or $latestReport.LastWriteTimeUtc -gt $PreviousReport.LastWriteTimeUtc) {
        return $latestReport
    }

    return $null
}

function Get-ReportSummary {
    param([string]$ReportPath)

    $summary = [ordered]@{
        GeneratedAt = $null
        TotalChecks = $null
        PassedChecks = $null
        FailedChecks = $null
    }

    foreach ($line in Get-Content -Path $ReportPath) {
        if ($line -match '^(?<Key>[A-Za-z]+):\s*(?<Value>.*)$') {
            $key = $matches["Key"]
            $value = $matches["Value"].Trim()
            if ($summary.Contains($key)) {
                if ($key -match 'Checks$') {
                    $summary[$key] = [int]$value
                }
                else {
                    $summary[$key] = $value
                }
            }
        }
    }

    return $summary
}

if (-not (Test-Path $appProject)) {
    throw "Projeto WPF nao encontrado: $appProject"
}

Write-Log "Iniciando UI smoke real."
Write-Log "Projeto: $appProject"
Write-Log "Saida: $OutputDirectory"

if (-not $SkipBuild) {
    Write-Log "Executando build antes do smoke."
    $buildLog = Join-Path $OutputDirectory "dotnet-build-ui-smoke.log"
    & dotnet build $appProject -c $Configuration 2>&1 | Tee-Object -FilePath $buildLog
    if ($LASTEXITCODE -ne 0) {
        Write-Log "Build falhou antes do UI smoke." "ERROR"

        $failureSummary = [ordered]@{
            Status = "FALHOU"
            ExitCode = $LASTEXITCODE
            SmokeFilter = $SmokeFilter
            OutputDirectory = $OutputDirectory
            CommandLogPath = $buildLog
            ReportPath = $null
            GeneratedAt = $null
            TotalChecks = $null
            PassedChecks = $null
            FailedChecks = $null
        }

        $failureSummary | ConvertTo-Json -Depth 5 | Set-Content -Path $summaryPath -Encoding UTF8
        exit $LASTEXITCODE
    }
}

$previousReport = Get-LatestReportFile -Directory $reportRoot
$startedAtUtc = [datetime]::UtcNow

$command = @(
    "run",
    "--project", $appProject,
    "-c", $Configuration
)

if ($SkipBuild) {
    $command += "--no-build"
}

$command += "--"
$command += "--smoke-test"

if (-not [string]::IsNullOrWhiteSpace($SmokeFilter)) {
    $command += "--smoke-filter=$SmokeFilter"
}

Write-Log ("Executando: dotnet {0}" -f ($command -join " "))
& dotnet @command 2>&1 | Tee-Object -FilePath $commandLog
$exitCode = $LASTEXITCODE

$reportFile = Get-GeneratedReportFile -Directory $reportRoot -StartedAtUtc $startedAtUtc -PreviousReport $previousReport
if ($null -eq $reportFile) {
    Write-Log "Nenhum relatorio de UI smoke foi gerado." "ERROR"

    $failureSummary = [ordered]@{
        Status = "FALHOU"
        ExitCode = $exitCode
        SmokeFilter = $SmokeFilter
        OutputDirectory = $OutputDirectory
        CommandLogPath = $commandLog
        ReportPath = $null
        GeneratedAt = $null
        TotalChecks = $null
        PassedChecks = $null
        FailedChecks = $null
    }

    $failureSummary | ConvertTo-Json -Depth 5 | Set-Content -Path $summaryPath -Encoding UTF8
    exit 1
}

$copiedReportPath = Join-Path $OutputDirectory $reportFile.Name
Copy-Item -Path $reportFile.FullName -Destination $copiedReportPath -Force
$reportSummary = Get-ReportSummary -ReportPath $reportFile.FullName

$status = if ($exitCode -eq 0 -and $reportSummary.FailedChecks -eq 0) { "APROVADO" } else { "FALHOU" }

$summary = [ordered]@{
    Status = $status
    ExitCode = $exitCode
    SmokeFilter = $SmokeFilter
    OutputDirectory = $OutputDirectory
    CommandLogPath = $commandLog
    ReportPath = $copiedReportPath
    GeneratedAt = $reportSummary.GeneratedAt
    TotalChecks = $reportSummary.TotalChecks
    PassedChecks = $reportSummary.PassedChecks
    FailedChecks = $reportSummary.FailedChecks
}

$summary | ConvertTo-Json -Depth 5 | Set-Content -Path $summaryPath -Encoding UTF8
Write-Log "Relatorio copiado para $copiedReportPath"
Write-Log "Resultado final do UI smoke: $status"

if ($status -eq "APROVADO") {
    exit 0
}

exit 1
