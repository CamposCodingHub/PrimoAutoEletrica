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

$outputDirectory = Join-Path $OutputRoot "CrashRecovery"
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$checks = New-Object System.Collections.Generic.List[object]
$appProject = Join-Path $projectRoot "PrimoAutoEletrica\PrimoAutoEletrica.csproj"
$appDataPath = Join-Path $outputDirectory "CrashAppData"
$build = Invoke-HomologacaoCommand -FilePath "dotnet" -Arguments @("build", $appProject, "-c", $config.Build.Configuration) -WorkingDirectory $projectRoot -LogPath (Join-Path $outputDirectory "build.log") -TimeoutSeconds 180
$checks.Add((New-HomologacaoCheck -Name "BuildBeforeCrashTest" -Outcome $(if ($build.ExitCode -eq 0) { "Pass" } else { "Fail" }) -Details "dotnet build exit code $($build.ExitCode)" -Module "Recuperacao" -Command $build.CommandLine -AffectedFile $build.LogPath -PossibleCause "Projeto nao compilou antes do teste de queda." -SuggestedFix "Corrigir erros de build antes de validar recuperacao apos falha." ))

if ($build.ExitCode -eq 0) {
    $stdoutPath = Join-Path $outputDirectory "workflow-killed.stdout.log"
    $stderrPath = Join-Path $outputDirectory "workflow-killed.stderr.log"
    $argumentList = @(
        "run",
        "--project",
        $appProject,
        "-c",
        $config.Build.Configuration,
        "--no-build",
        "--",
        "--workflow-test",
        "--app-data=$appDataPath"
    )

    $process = Start-Process -FilePath "dotnet" -ArgumentList $argumentList -WorkingDirectory $projectRoot -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    Start-Sleep -Seconds ([int]$config.CrashRecovery.KillDelaySeconds)

    $killed = $false
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
        $killed = $true
    }

    $checks.Add((New-HomologacaoCheck -Name "ProcessKilledDuringOperation" -Outcome $(if ($killed) { "Pass" } else { "Warn" }) -Details "PID=$($process.Id); ForcedKill=$killed" -Module "Recuperacao" -PossibleCause "Processo encerrou antes do kill ou workflow muito rapido." -SuggestedFix "Aumentar KillDelaySeconds se quiser forcar o kill no meio de mais gravacoes." ))

    $dbPath = Join-Path $appDataPath "primoauto.db"
    if (Test-Path $dbPath) {
        $pythonScript = @'
import json
import os
import sqlite3
import sys

db_path = sys.argv[1]
conn = sqlite3.connect(db_path)
integrity = conn.execute("PRAGMA integrity_check;").fetchone()[0]
tables = [row[0] for row in conn.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';")]
counts = {}
for table_name in tables:
    quoted = '"' + table_name.replace('"', '""') + '"'
    counts[table_name] = conn.execute(f"SELECT COUNT(*) FROM {quoted};").fetchone()[0]
conn.close()
print(json.dumps({"integrity": integrity, "counts": counts}))
'@
        $pythonFile = New-TemporaryPythonFile -Directory $outputDirectory -Prefix "crash-integrity" -Content $pythonScript
        $probe = Invoke-HomologacaoCommand -FilePath "python" -Arguments @($pythonFile, $dbPath) -WorkingDirectory $outputDirectory -LogPath (Join-Path $outputDirectory "integrity.log") -TimeoutSeconds 60
        if ($probe.ExitCode -eq 0) {
            $probeJson = (($probe.Output -join "`n") | ConvertFrom-Json)
            $checks.Add((New-HomologacaoCheck -Name "PostCrashIntegrity" -Outcome $(if ($probeJson.integrity -eq "ok") { "Pass" } else { "Fail" }) -Details "PRAGMA integrity_check=$($probeJson.integrity)" -Module "Recuperacao" -AffectedFile $dbPath -PossibleCause "Queda provocou corrupcao da base SQLite." -SuggestedFix "Substituir a base por backup valido e revisar operacoes que ficaram em aberto." ))
        }
        else {
            $checks.Add((New-HomologacaoCheck -Name "PostCrashIntegrity" -Outcome "Fail" -Details "Falha ao rodar verificacao de integridade apos kill." -Module "Recuperacao" -ErrorMessage ($probe.Output -join "`n") -AffectedFile $dbPath -PossibleCause "Base nao abriu apos kill ou Python falhou." -SuggestedFix "Executar verificacao manual do SQLite e revisar logs de falha." ))
        }
    }
    else {
        $checks.Add((New-HomologacaoCheck -Name "DatabaseCreatedBeforeCrash" -Outcome "Fail" -Details "Nenhum banco isolado foi criado antes do kill." -Module "Recuperacao" -AffectedFile $dbPath -PossibleCause "Processo caiu cedo demais para iniciar a base." -SuggestedFix "Aumentar KillDelaySeconds e repetir o teste." ))
    }

    $restart = Invoke-WorkflowRun -ProjectRoot $projectRoot -Build $config.Build -OutputDirectory (Join-Path $outputDirectory "RestartWorkflow") -AppDataPath $appDataPath -SkipBuild
    $checks.Add((New-HomologacaoCheck -Name "RestartAfterCrash" -Outcome $(if ($restart.Status -eq "APROVADO") { "Pass" } else { "Fail" }) -Details "Workflow apos kill: $($restart.Status)" -Module "Recuperacao" -Command $restart.CommandLine -AffectedFile $restart.ReportPath -PossibleCause "Aplicativo nao retomou operacao com a base remanescente." -SuggestedFix "Analisar logs da retomada e validar transacoes pendentes." ))

    $combinedCrashLog = @()
    if (Test-Path $stdoutPath) { $combinedCrashLog += Get-Content -Path $stdoutPath }
    if (Test-Path $stderrPath) { $combinedCrashLog += Get-Content -Path $stderrPath }
    $logHasCorruption = @($combinedCrashLog | Where-Object { $_ -match "malformed|corrupt|disk image|fatal" }).Count -gt 0
    $checks.Add((New-HomologacaoCheck -Name "CrashLogScan" -Outcome $(if (-not $logHasCorruption) { "Pass" } else { "Fail" }) -Details "Padroes de corrupcao encontrados: $logHasCorruption" -Module "Recuperacao" -AffectedFile $stdoutPath -PossibleCause "Saida do processo killed mostrou corrupcao ou falha fatal." -SuggestedFix "Investigar log completo e reforcar checkpoint/backup antes de operacoes criticas." ))
}

$area = New-HomologacaoAreaResult `
    -AreaId "crash-recovery" `
    -AreaName "Queda de energia / fechamento inesperado" `
    -OutputDirectory $outputDirectory `
    -Checks $checks `
    -Metrics @{
        AppDataPath = $appDataPath
        KillDelaySeconds = [int]$config.CrashRecovery.KillDelaySeconds
    }

Write-HomologacaoAreaArtifacts -AreaResult $area -JsonPath (Join-Path $OutputRoot "crash-recovery-summary.json") -TextPath (Join-Path $OutputRoot "crash-recovery-summary.txt")
