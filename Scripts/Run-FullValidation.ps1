[CmdletBinding()]
param(
    [switch]$SkipClean,
    [switch]$SkipBuild,
    [switch]$SkipUnitTests,
    [switch]$SkipUiSmoke,
    [switch]$SkipWorkflow,
    [switch]$SkipThemeTest,
    [switch]$SkipPermissionTest,
    [switch]$SkipDatabaseTest,
    [switch]$OpenReport,
    [string]$Configuration = "Debug",
    [string]$Framework = "net9.0-windows"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$solutionPath = Join-Path $projectRoot "PrimoAutoEletrica.sln"
$appProject = Join-Path $projectRoot "PrimoAutoEletrica\PrimoAutoEletrica.csproj"
$testProject = Join-Path $projectRoot "Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$validationDir = Join-Path $projectRoot "TestResults\FullValidation\$timestamp"
$reportsDir = Join-Path $projectRoot "Reports"
$logFile = Join-Path $validationDir "validation.log"
$reportMd = Join-Path $validationDir "RELATORIO_QUALIDADE_$timestamp.md"
$reportTxt = Join-Path $validationDir "RELATORIO_QUALIDADE_$timestamp.txt"
$summaryJson = Join-Path $validationDir "validation-summary.json"
$uiSmokeScript = Join-Path $scriptRoot "Run-UiSmoke.ps1"
$workflowScript = Join-Path $scriptRoot "Run-WorkflowTest.ps1"

New-Item -ItemType Directory -Path $validationDir -Force | Out-Null
New-Item -ItemType Directory -Path $reportsDir -Force | Out-Null

$Results = [ordered]@{
    Clean = [ordered]@{ Status = "PENDENTE"; Details = ""; Artifact = "" }
    Restore = [ordered]@{ Status = "PENDENTE"; Details = ""; Artifact = "" }
    Build = [ordered]@{ Status = "PENDENTE"; Details = ""; Artifact = "" }
    UnitTests = [ordered]@{ Status = "PENDENTE"; Details = ""; Artifact = "" }
    ThemeTest = [ordered]@{ Status = "PENDENTE"; Details = ""; Artifact = "" }
    PermissionTest = [ordered]@{ Status = "PENDENTE"; Details = ""; Artifact = "" }
    DatabaseTest = [ordered]@{ Status = "PENDENTE"; Details = ""; Artifact = "" }
    UiSmoke = [ordered]@{ Status = "PENDENTE"; Details = ""; Artifact = "" }
    Workflow = [ordered]@{ Status = "PENDENTE"; Details = ""; Artifact = "" }
}

function Write-Log {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )

    $entry = "{0} [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message
    Add-Content -Path $logFile -Value $entry
    Write-Host $entry
}

function Set-StepResult {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Details = "",
        [string]$Artifact = ""
    )

    $Results[$Name].Status = $Status
    $Results[$Name].Details = $Details
    $Results[$Name].Artifact = $Artifact
    Write-Log ("{0}: {1} - {2}" -f $Name, $Status, $Details) "RESULT"
}

function Invoke-LoggedCommand {
    param(
        [string]$StepName,
        [string]$Executable,
        [string[]]$Arguments,
        [string]$OutputPath
    )

    Write-Log ("Executando {0}: {1} {2}" -f $StepName, $Executable, ($Arguments -join " "))
    & $Executable @Arguments 2>&1 | Tee-Object -FilePath $OutputPath | Out-Null
    return [int]$LASTEXITCODE
}

function Read-JsonSummary {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return $null
    }

    return Get-Content -Raw -Path $Path | ConvertFrom-Json
}

function Copy-LatestApplicationLog {
    param(
        [string]$ProjectRoot,
        [string]$Configuration,
        [string]$Framework,
        [string]$DestinationRoot
    )

    $logSource = Join-Path $ProjectRoot "PrimoAutoEletrica\bin\$Configuration\$Framework\Logs"
    if (-not (Test-Path $logSource)) {
        return
    }

    $latestAppLog = Get-ChildItem -Path $logSource -File -Filter "log-*.txt" |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $latestAppLog) {
        return
    }

    $appLogDir = Join-Path $DestinationRoot "AppLogs"
    New-Item -ItemType Directory -Path $appLogDir -Force | Out-Null
    Copy-Item -Path $latestAppLog.FullName -Destination (Join-Path $appLogDir $latestAppLog.Name) -Force
    Write-Log "Ultimo log da aplicacao copiado para o pacote de validacao."
}

if (-not (Test-Path $solutionPath)) {
    throw "Solution nao encontrada: $solutionPath"
}

if (-not (Test-Path $appProject)) {
    throw "Projeto principal nao encontrado: $appProject"
}

if (-not (Test-Path $testProject)) {
    throw "Projeto de testes nao encontrado: $testProject"
}

Write-Log "Iniciando validacao completa real."
Write-Log "Projeto: $projectRoot"
Write-Log "Saida: $validationDir"

$buildReady = $false

if ($SkipClean) {
    Set-StepResult -Name "Clean" -Status "PULADO" -Details "Etapa pulada por parametro."
}
else {
    $cleanLog = Join-Path $validationDir "dotnet-clean.log"
    $cleanExitCode = Invoke-LoggedCommand -StepName "Clean" -Executable "dotnet" -Arguments @("clean", $solutionPath, "-c", $Configuration) -OutputPath $cleanLog
    if ($cleanExitCode -eq 0) {
        Set-StepResult -Name "Clean" -Status "APROVADO" -Details "dotnet clean concluido." -Artifact $cleanLog
    }
    else {
        Set-StepResult -Name "Clean" -Status "FALHOU" -Details "dotnet clean retornou codigo $cleanExitCode." -Artifact $cleanLog
    }
}

$restoreLog = Join-Path $validationDir "dotnet-restore.log"
$restoreExitCode = Invoke-LoggedCommand -StepName "Restore" -Executable "dotnet" -Arguments @("restore", $solutionPath) -OutputPath $restoreLog
if ($restoreExitCode -eq 0) {
    Set-StepResult -Name "Restore" -Status "APROVADO" -Details "dotnet restore concluido." -Artifact $restoreLog
}
else {
    Set-StepResult -Name "Restore" -Status "FALHOU" -Details "dotnet restore retornou codigo $restoreExitCode." -Artifact $restoreLog
}

if ($restoreExitCode -eq 0) {
    if ($SkipBuild) {
        $buildReady = $true
        Set-StepResult -Name "Build" -Status "PULADO" -Details "Etapa pulada por parametro. Os proximos passos vao reutilizar o build existente."
    }
    else {
        $buildLog = Join-Path $validationDir "dotnet-build.log"
        $buildExitCode = Invoke-LoggedCommand -StepName "Build" -Executable "dotnet" -Arguments @("build", $solutionPath, "-c", $Configuration) -OutputPath $buildLog
        if ($buildExitCode -eq 0) {
            $buildReady = $true
            Set-StepResult -Name "Build" -Status "APROVADO" -Details "dotnet build concluido." -Artifact $buildLog
        }
        else {
            Set-StepResult -Name "Build" -Status "FALHOU" -Details "dotnet build retornou codigo $buildExitCode." -Artifact $buildLog
        }
    }
}
else {
    Set-StepResult -Name "Build" -Status "BLOQUEADO" -Details "Build nao executado porque o restore falhou."
}

if ($SkipUnitTests) {
    Set-StepResult -Name "UnitTests" -Status "PULADO" -Details "Etapa pulada por parametro."
}
elseif (-not $buildReady) {
    Set-StepResult -Name "UnitTests" -Status "BLOQUEADO" -Details "Build indisponivel para executar os testes."
}
else {
    $unitLog = Join-Path $validationDir "dotnet-test-unit.log"
    $unitArgs = @(
        "test", $solutionPath,
        "-c", $Configuration,
        "--no-build",
        "--logger", "trx;LogFileName=unit-tests.trx",
        "--results-directory", $validationDir
    )
    $unitExitCode = Invoke-LoggedCommand -StepName "UnitTests" -Executable "dotnet" -Arguments $unitArgs -OutputPath $unitLog
    if ($unitExitCode -eq 0) {
        Set-StepResult -Name "UnitTests" -Status "APROVADO" -Details "Suite completa de testes unitarios aprovada." -Artifact $unitLog
    }
    else {
        Set-StepResult -Name "UnitTests" -Status "FALHOU" -Details "dotnet test retornou codigo $unitExitCode." -Artifact $unitLog
    }
}

if ($SkipThemeTest) {
    Set-StepResult -Name "ThemeTest" -Status "PULADO" -Details "Etapa pulada por parametro."
}
elseif (-not $buildReady) {
    Set-StepResult -Name "ThemeTest" -Status "BLOQUEADO" -Details "Build indisponivel para executar o teste de tema."
}
else {
    $themeLog = Join-Path $validationDir "dotnet-test-theme.log"
    $themeArgs = @(
        "test", $testProject,
        "-c", $Configuration,
        "--no-build",
        "--filter", "FullyQualifiedName~ThemeXamlTests",
        "--logger", "trx;LogFileName=theme-tests.trx",
        "--results-directory", $validationDir
    )
    $themeExitCode = Invoke-LoggedCommand -StepName "ThemeTest" -Executable "dotnet" -Arguments $themeArgs -OutputPath $themeLog
    if ($themeExitCode -eq 0) {
        Set-StepResult -Name "ThemeTest" -Status "APROVADO" -Details "Validacao dedicada de recursos XAML aprovada." -Artifact $themeLog
    }
    else {
        Set-StepResult -Name "ThemeTest" -Status "FALHOU" -Details "Teste dedicado de tema retornou codigo $themeExitCode." -Artifact $themeLog
    }
}

if ($SkipPermissionTest) {
    Set-StepResult -Name "PermissionTest" -Status "PULADO" -Details "Etapa pulada por parametro."
}
elseif (-not $buildReady) {
    Set-StepResult -Name "PermissionTest" -Status "BLOQUEADO" -Details "Build indisponivel para executar o teste de permissoes."
}
else {
    $permissionLog = Join-Path $validationDir "dotnet-test-permissions.log"
    $permissionArgs = @(
        "test", $testProject,
        "-c", $Configuration,
        "--no-build",
        "--filter", "FullyQualifiedName~PermissionTests",
        "--logger", "trx;LogFileName=permission-tests.trx",
        "--results-directory", $validationDir
    )
    $permissionExitCode = Invoke-LoggedCommand -StepName "PermissionTest" -Executable "dotnet" -Arguments $permissionArgs -OutputPath $permissionLog
    if ($permissionExitCode -eq 0) {
        Set-StepResult -Name "PermissionTest" -Status "APROVADO" -Details "Suite dedicada de permissoes aprovada." -Artifact $permissionLog
    }
    else {
        Set-StepResult -Name "PermissionTest" -Status "FALHOU" -Details "Teste dedicado de permissoes retornou codigo $permissionExitCode." -Artifact $permissionLog
    }
}

if ($SkipDatabaseTest) {
    Set-StepResult -Name "DatabaseTest" -Status "PULADO" -Details "Etapa pulada por parametro."
}
elseif (-not $buildReady) {
    Set-StepResult -Name "DatabaseTest" -Status "BLOQUEADO" -Details "Build indisponivel para executar o teste de banco."
}
else {
    $databaseLog = Join-Path $validationDir "dotnet-test-database.log"
    $databaseArgs = @(
        "test", $testProject,
        "-c", $Configuration,
        "--no-build",
        "--filter", "FullyQualifiedName~DatabasePersistenceTests",
        "--logger", "trx;LogFileName=database-tests.trx",
        "--results-directory", $validationDir
    )
    $databaseExitCode = Invoke-LoggedCommand -StepName "DatabaseTest" -Executable "dotnet" -Arguments $databaseArgs -OutputPath $databaseLog
    if ($databaseExitCode -eq 0) {
        Set-StepResult -Name "DatabaseTest" -Status "APROVADO" -Details "Suite dedicada de banco aprovada." -Artifact $databaseLog
    }
    else {
        Set-StepResult -Name "DatabaseTest" -Status "FALHOU" -Details "Teste dedicado de banco retornou codigo $databaseExitCode." -Artifact $databaseLog
    }
}

if ($SkipUiSmoke) {
    Set-StepResult -Name "UiSmoke" -Status "PULADO" -Details "Etapa pulada por parametro."
}
elseif (-not $buildReady) {
    Set-StepResult -Name "UiSmoke" -Status "BLOQUEADO" -Details "Build indisponivel para executar o UI smoke."
}
else {
    $uiSmokeOutput = Join-Path $validationDir "UiSmoke"
    try {
        & $uiSmokeScript -Configuration $Configuration -Framework $Framework -SkipBuild:$true -OutputDirectory $uiSmokeOutput
        $uiExitCode = $LASTEXITCODE
    }
    catch {
        $uiExitCode = 1
        Write-Log ("Falha ao executar Run-UiSmoke.ps1: {0}" -f $_.Exception.Message) "ERROR"
    }

    $uiSummary = Read-JsonSummary -Path (Join-Path $uiSmokeOutput "ui-smoke-summary.json")
    if ($null -ne $uiSummary -and $uiExitCode -eq 0 -and $uiSummary.Status -eq "APROVADO") {
        $details = "UI smoke aprovado com $($uiSummary.PassedChecks)/$($uiSummary.TotalChecks) checks."
        Set-StepResult -Name "UiSmoke" -Status "APROVADO" -Details $details -Artifact $uiSummary.ReportPath
    }
    else {
        $details = if ($null -ne $uiSummary) {
            "UI smoke falhou com $($uiSummary.FailedChecks) checks falhos."
        }
        else {
            "UI smoke falhou sem gerar resumo."
        }
        $artifact = if ($null -ne $uiSummary) { [string]$uiSummary.CommandLogPath } else { "" }
        Set-StepResult -Name "UiSmoke" -Status "FALHOU" -Details $details -Artifact $artifact
    }
}

if ($SkipWorkflow) {
    Set-StepResult -Name "Workflow" -Status "PULADO" -Details "Etapa pulada por parametro."
}
elseif (-not $buildReady) {
    Set-StepResult -Name "Workflow" -Status "BLOQUEADO" -Details "Build indisponivel para executar o workflow test."
}
else {
    $workflowOutput = Join-Path $validationDir "Workflow"
    try {
        & $workflowScript -Configuration $Configuration -Framework $Framework -SkipBuild:$true -OutputDirectory $workflowOutput
        $workflowExitCode = $LASTEXITCODE
    }
    catch {
        $workflowExitCode = 1
        Write-Log ("Falha ao executar Run-WorkflowTest.ps1: {0}" -f $_.Exception.Message) "ERROR"
    }

    $workflowSummary = Read-JsonSummary -Path (Join-Path $workflowOutput "workflow-test-summary.json")
    if ($null -ne $workflowSummary -and $workflowExitCode -eq 0 -and $workflowSummary.Status -eq "APROVADO") {
        $details = "Workflow aprovado com $($workflowSummary.PassedChecks)/$($workflowSummary.TotalChecks) checks."
        Set-StepResult -Name "Workflow" -Status "APROVADO" -Details $details -Artifact $workflowSummary.ReportPath
    }
    else {
        $details = if ($null -ne $workflowSummary) {
            "Workflow falhou com $($workflowSummary.FailedChecks) checks falhos."
        }
        else {
            "Workflow falhou sem gerar resumo."
        }
        $artifact = if ($null -ne $workflowSummary) { [string]$workflowSummary.CommandLogPath } else { "" }
        Set-StepResult -Name "Workflow" -Status "FALHOU" -Details $details -Artifact $artifact
    }
}

Copy-LatestApplicationLog -ProjectRoot $projectRoot -Configuration $Configuration -Framework $Framework -DestinationRoot $validationDir

$reprovatedStatuses = @("FALHOU", "BLOQUEADO")
$resultadosReprovados = @($Results.Values | Where-Object { $reprovatedStatuses -contains $_.Status })
$overallStatus = if ($resultadosReprovados.Count -eq 0) { "APROVADO" } else { "REPROVADO" }

$summary = [ordered]@{
    GeneratedAt = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
    ProjectRoot = $projectRoot
    SolutionPath = $solutionPath
    Configuration = $Configuration
    Framework = $Framework
    DotNetVersion = (& dotnet --version)
    GitBranch = (git branch --show-current 2>$null)
    GitCommit = (git rev-parse --short HEAD 2>$null)
    Overall = $overallStatus
    Results = $Results
}

$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryJson -Encoding UTF8

$reportLines = @()
$reportLines += "# Relatorio de Validacao Completa"
$reportLines += ""
$reportLines += "- Gerado em: $($summary.GeneratedAt)"
$reportLines += "- Projeto: $projectRoot"
$reportLines += "- Configuration: $Configuration"
$reportLines += "- Framework: $Framework"
$reportLines += "- .NET: $($summary.DotNetVersion)"
$reportLines += "- Branch: $($summary.GitBranch)"
$reportLines += "- Commit: $($summary.GitCommit)"
$reportLines += "- Resultado geral: $overallStatus"
$reportLines += ""
$reportLines += "| Etapa | Status | Detalhes | Artefato |"
$reportLines += "| --- | --- | --- | --- |"

foreach ($entry in $Results.GetEnumerator()) {
    $stepName = $entry.Key
    $stepStatus = $entry.Value.Status
    $stepDetails = ([string]$entry.Value.Details).Replace("|", "/")
    $stepArtifact = ([string]$entry.Value.Artifact).Replace("|", "/")
    $reportLines += "| $stepName | $stepStatus | $stepDetails | $stepArtifact |"
}

$reportLines += ""
$reportLines += "Resumo json: $summaryJson"

$reportLines | Set-Content -Path $reportMd -Encoding UTF8
$reportLines | Set-Content -Path $reportTxt -Encoding UTF8

$latestReportCopy = Join-Path $reportsDir "VALIDACAO_COMPLETA_$timestamp.md"
Copy-Item -Path $reportMd -Destination $latestReportCopy -Force
Write-Log "Relatorio final gerado."

if ($OpenReport) {
    Invoke-Item $reportMd
}

if ($overallStatus -eq "APROVADO") {
    Write-Log "Validacao completa aprovada." "SUCCESS"
    exit 0
}

Write-Log "Validacao completa reprovada." "ERROR"
exit 1
