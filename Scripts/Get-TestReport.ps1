[CmdletBinding()]
param(
    [string]$ValidationDirectory,
    [switch]$AsJson
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$fullValidationRoot = Join-Path $projectRoot "TestResults\FullValidation"

function Get-LatestValidationDirectory {
    param([string]$Root)

    if (-not (Test-Path $Root)) {
        return $null
    }

    return Get-ChildItem -Path $Root -Directory |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($ValidationDirectory)) {
    $latestDirectory = Get-LatestValidationDirectory -Root $fullValidationRoot
    if ($null -eq $latestDirectory) {
        throw "Nenhum resultado foi encontrado em $fullValidationRoot"
    }

    $ValidationDirectory = $latestDirectory.FullName
}

$summaryPath = Join-Path $ValidationDirectory "validation-summary.json"
if (-not (Test-Path $summaryPath)) {
    throw "Resumo nao encontrado: $summaryPath"
}

$summary = Get-Content -Raw -Path $summaryPath | ConvertFrom-Json

if ($AsJson) {
    $summary | ConvertTo-Json -Depth 8
    exit 0
}

Write-Host "# Relatorio de Validacao"
Write-Host ""
Write-Host "- Gerado em: $($summary.GeneratedAt)"
Write-Host "- Resultado geral: $($summary.Overall)"
Write-Host "- Projeto: $($summary.ProjectRoot)"
Write-Host "- Configuration: $($summary.Configuration)"
Write-Host "- Framework: $($summary.Framework)"
Write-Host "- .NET: $($summary.DotNetVersion)"
Write-Host "- Branch: $($summary.GitBranch)"
Write-Host "- Commit: $($summary.GitCommit)"
Write-Host "- Pasta: $ValidationDirectory"
Write-Host ""
Write-Host "| Etapa | Status | Detalhes |"
Write-Host "| --- | --- | --- |"

foreach ($result in $summary.Results.PSObject.Properties) {
    $details = ([string]$result.Value.Details).Replace("|", "/")
    Write-Host ("| {0} | {1} | {2} |" -f $result.Name, $result.Value.Status, $details)
}
