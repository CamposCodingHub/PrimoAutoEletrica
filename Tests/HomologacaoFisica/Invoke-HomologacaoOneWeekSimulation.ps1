[CmdletBinding()]
param(
    [string]$ConfigPath = "",
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot "HomologacaoFisica.Common.psm1") -Force

$projectRoot = Get-HomologacaoProjectRoot -ScriptPath $PSScriptRoot
$config = Import-HomologacaoConfig -Path $ConfigPath -ProjectRoot $projectRoot

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $projectRoot ("TestResults\HomologacaoFisica\{0}" -f (Get-Date -Format "yyyy-MM-dd_HH-mm-ss"))
}

$outputDirectory = Join-Path $OutputRoot "OneWeekSimulation"
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$days = [int]$config.Simulation.Days
$sharedAppData = if ([string]::IsNullOrWhiteSpace([string]$config.Simulation.SharedAutomationAppData)) {
    Join-Path $outputDirectory "AutomationAppData"
}
else {
    [string]$config.Simulation.SharedAutomationAppData
}

$checks = New-Object System.Collections.Generic.List[object]
$dailyMetrics = New-Object System.Collections.Generic.List[object]
$pythonScript = @'
import json
import os
import sqlite3
import sys

db_path = sys.argv[1]
tables = ["Clientes", "Veiculos", "Produtos", "Orcamentos", "OrdensServico", "Agendamentos", "MovimentacoesFinanceiras", "Vendas"]

result = {}
if not os.path.exists(db_path):
    print(json.dumps(result))
    sys.exit(0)

conn = sqlite3.connect(db_path)
for table_name in tables:
    try:
        quoted = '"' + table_name.replace('"', '""') + '"'
        result[table_name] = conn.execute(f"SELECT COUNT(*) FROM {quoted};").fetchone()[0]
    except Exception:
        pass
conn.close()
print(json.dumps(result))
'@

$pythonFile = New-TemporaryPythonFile -Directory $outputDirectory -Prefix "weekly-counts" -Content $pythonScript

for ($day = 1; $day -le $days; $day++) {
    $dayDirectory = Join-Path $outputDirectory ("Day-{0:d2}" -f $day)
    New-Item -ItemType Directory -Path $dayDirectory -Force | Out-Null

    $workflow = Invoke-WorkflowRun -ProjectRoot $projectRoot -Build $config.Build -OutputDirectory $dayDirectory -AppDataPath $sharedAppData -SkipBuild:($day -gt 1)
    $checks.Add((New-HomologacaoCheck -Name ("WorkflowDay" + $day) -Outcome $(if ($workflow.Status -eq "APROVADO") { "Pass" } else { "Fail" }) -Details "Dia $day; Passed=$($workflow.Summary.PassedChecks); Failed=$($workflow.Summary.FailedChecks)" -Module "Funcionalidade" -Command $workflow.CommandLine -AffectedFile $workflow.ReportPath -PossibleCause "Ciclo operacional diario falhou no workflow automatizado." -SuggestedFix "Abrir o relatorio do dia e corrigir o passo com falha antes de seguir com a simulacao." ))

    $dbPath = Join-Path $sharedAppData "primoauto.db"
    $counts = @{}
    if (Test-Path $dbPath) {
        $countsRun = Invoke-HomologacaoCommand -FilePath "python" -Arguments @($pythonFile, $dbPath) -WorkingDirectory $outputDirectory -LogPath (Join-Path $dayDirectory "counts.log") -TimeoutSeconds 60
        if ($countsRun.ExitCode -eq 0) {
            $counts = (($countsRun.Output -join "`n") | ConvertFrom-Json)
        }
    }

    $dailyMetrics.Add([pscustomobject]@{
        Day = $day
        WorkflowStatus = $workflow.Status
        PassedChecks = $workflow.Summary.PassedChecks
        FailedChecks = $workflow.Summary.FailedChecks
        Counts = $counts
        ReportPath = $workflow.ReportPath
    })
}

$checks.Add((New-HomologacaoCheck -Name "CalendarShiftModel" -Outcome "Warn" -Details "A simulacao roda 7 ciclos operacionais consecutivos no mesmo relogio do sistema; ela nao altera data/hora da maquina." -Module "Funcionalidade" -PossibleCause "A automacao nao mexe no relogio do Windows." -SuggestedFix "Se quiser prova temporal completa, repetir a suite em dias reais ou em VM com data controlada." ))

$area = New-HomologacaoAreaResult `
    -AreaId "one-week-simulation" `
    -AreaName "Simulacao de 1 semana de oficina" `
    -OutputDirectory $outputDirectory `
    -Checks $checks `
    -Metrics @{
        Days = $days
        SharedAutomationAppData = $sharedAppData
        Daily = $dailyMetrics
    }

Write-HomologacaoAreaArtifacts -AreaResult $area -JsonPath (Join-Path $OutputRoot "one-week-simulation-summary.json") -TextPath (Join-Path $OutputRoot "one-week-simulation-summary.txt")
