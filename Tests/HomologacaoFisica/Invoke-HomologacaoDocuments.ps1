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

$outputDirectory = Join-Path $OutputRoot "Documents"
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$checks = New-Object System.Collections.Generic.List[object]
$appDataPath = Join-Path $outputDirectory "DocumentsAppData"
$smoke = Invoke-UiSmokeRun -ProjectRoot $projectRoot -Build $config.Build -OutputDirectory (Join-Path $outputDirectory "Smoke") -SmokeFilter "Documentos" -AppDataPath $appDataPath
$checks.Add((New-HomologacaoCheck -Name "DocumentSmoke" -Outcome $(if ($smoke.Status -eq "APROVADO") { "Pass" } else { "Fail" }) -Details "Checks aprovados=$($smoke.Summary.PassedChecks); falhos=$($smoke.Summary.FailedChecks)" -Module "Documentos" -Command $smoke.CommandLine -AffectedFile $smoke.ReportPath -PossibleCause "Geracao automatica de PDFs ou roteiro de impressao falhou." -SuggestedFix "Abrir o relatorio de smoke e corrigir o check de documentos com falha." ))

$documentosRoot = Join-Path $projectRoot "PrimoAutoEletrica\bin\$($config.Build.Configuration)\$($config.Build.Framework)\Logs\documentos-smoke"
$latestDocumentFolder = if (Test-Path $documentosRoot) {
    Get-ChildItem -Path $documentosRoot -Directory | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
}
else {
    $null
}

if ($null -ne $latestDocumentFolder) {
    $pdfs = @(Get-ChildItem -Path $latestDocumentFolder.FullName -File -Filter "*.pdf")
    $checks.Add((New-HomologacaoCheck -Name "GeneratedPdfSet" -Outcome $(if ($pdfs.Count -ge 8) { "Pass" } else { "Fail" }) -Details "Pasta=$($latestDocumentFolder.FullName); PDFs=$($pdfs.Count)" -Module "Documentos" -AffectedFile $latestDocumentFolder.FullName -PossibleCause "Nem todos os documentos padronizados foram gerados." -SuggestedFix "Revisar DocumentoPdfService e fixtures do smoke de documentos." ))
}
else {
    $checks.Add((New-HomologacaoCheck -Name "GeneratedPdfSet" -Outcome "Fail" -Details "Nenhuma pasta documentos-smoke foi encontrada." -Module "Documentos" -PossibleCause "O smoke nao chegou a materializar os PDFs." -SuggestedFix "Executar novamente o smoke de documentos e revisar os logs do app." ))
}

$printerSummaryPath = Join-Path $OutputRoot "printer-real-summary.json"
if (Test-Path $printerSummaryPath) {
    $printerSummary = Get-Content -Raw -Path $printerSummaryPath | ConvertFrom-Json
    $printerStatus = [string]$printerSummary.Status
    $checks.Add((New-HomologacaoCheck -Name "PhysicalPrintEvidence" -Outcome $(if ($printerStatus -eq "APROVADO") { "Pass" } elseif ($printerStatus -like "IGNORADO*") { "Skip" } else { "Warn" }) -Details "Resultado da etapa de impressora fisica: $printerStatus" -Module "Documentos" -AffectedFile $printerSummaryPath -PossibleCause "Impressao fisica ainda nao foi validada junto com a geracao de documentos." -SuggestedFix "Executar a etapa de impressora fisica no mesmo computador da oficina." ))
}
else {
    $checks.Add((New-HomologacaoCheck -Name "PhysicalPrintEvidence" -Outcome "Warn" -Details "Resumo da impressora fisica nao encontrado; esta etapa ficou apenas na geracao de documentos." -Module "Documentos" -PossibleCause "A area de impressao nao foi rodada antes da validacao documental." -SuggestedFix "Rodar Invoke-HomologacaoPrinter.ps1 para fechar o ciclo fisico." ))
}

$checks.Add((New-HomologacaoCheck -Name "ValidationMode" -Outcome "Warn" -Details "A automacao comprova geracao, metadados e arquivos; avaliacao estetica do layout impresso continua dependendo da impressora e do operador." -Module "Documentos" -PossibleCause "Nao existe OCR/layout diff fisico nesta suite." -SuggestedFix "Conferir o PDF impresso e anexar a evidencia ao dossie comercial." ))

$area = New-HomologacaoAreaResult `
    -AreaId "documents" `
    -AreaName "Emissao e impressao de documentos" `
    -OutputDirectory $outputDirectory `
    -Checks $checks `
    -Metrics @{
        SmokeReport = $smoke.ReportPath
        LatestDocumentFolder = if ($null -ne $latestDocumentFolder) { $latestDocumentFolder.FullName } else { "" }
    }

Write-HomologacaoAreaArtifacts -AreaResult $area -JsonPath (Join-Path $OutputRoot "documents-summary.json") -TextPath (Join-Path $OutputRoot "documents-summary.txt")
