[CmdletBinding()]
param(
    [string]$ConfigPath = "",
    [string]$OutputRoot = "",
    [switch]$NoPrompt
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot "HomologacaoFisica.Common.psm1") -Force

function Get-CategoryResult {
    param(
        [string]$Name,
        [object[]]$Areas
    )

    $activeAreas = @($Areas | Where-Object { $null -ne $_ })
    if ($activeAreas.Count -eq 0) {
        return [pscustomobject]@{
            Name = $Name
            Status = "REPROVADO"
            Score = 0
        }
    }

    $nonIgnored = @($activeAreas | Where-Object { $_.Status -notlike "IGNORADO*" })
    if ($nonIgnored.Count -eq 0) {
        return [pscustomobject]@{
            Name = $Name
            Status = "IGNORADO POR FALTA DE AMBIENTE FISICO"
            Score = 0
        }
    }

    if (@($nonIgnored | Where-Object { $_.Status -eq "REPROVADO" }).Count -gt 0) {
        $status = "REPROVADO"
    }
    elseif (@($nonIgnored | Where-Object { $_.Status -eq "APROVADO COM RESSALVAS" }).Count -gt 0) {
        $status = "APROVADO COM RESSALVAS"
    }
    else {
        $status = "APROVADO"
    }

    $score = [Math]::Round((($nonIgnored | Measure-Object -Property Score -Average).Average), 1)
    return [pscustomobject]@{
        Name = $Name
        Status = $status
        Score = $score
    }
}

function Invoke-AreaScript {
    param(
        [string]$ScriptPath,
        [hashtable]$Arguments
    )

    try {
        return & $ScriptPath @Arguments
    }
    catch {
        return [pscustomobject]@{
            AreaId = [System.IO.Path]::GetFileNameWithoutExtension($ScriptPath)
            AreaName = [System.IO.Path]::GetFileNameWithoutExtension($ScriptPath)
            GeneratedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
            Status = "REPROVADO"
            Score = 0
            OutputDirectory = ""
            Metrics = @{}
            Notes = @("A etapa lancou excecao antes de gerar seus artefatos.")
            Checks = @(
                [pscustomobject]@{
                    Name = "ScriptExecution"
                    Module = "Infra"
                    Outcome = "Fail"
                    Details = "Falha interna ao executar o script da area."
                    Error = $_.Exception.Message
                    Command = $ScriptPath
                    AffectedFile = $ScriptPath
                    PossibleCause = "Erro nao tratado dentro do script da area."
                    SuggestedFix = "Ler a excecao e corrigir o script correspondente."
                }
            )
            Files = @{}
        }
    }
}

$projectRoot = Get-HomologacaoProjectRoot -ScriptPath $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $projectRoot ("TestResults\HomologacaoFisica\{0}" -f (Get-Date -Format "yyyy-MM-dd_HH-mm-ss"))
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$sharedArgs = @{
    ConfigPath = $ConfigPath
    OutputRoot = $OutputRoot
}

$areas = [ordered]@{}
$areas.Windows = Invoke-AreaScript -ScriptPath (Join-Path $PSScriptRoot "Invoke-HomologacaoWindowsClean.ps1") -Arguments $sharedArgs
$areas.Network = Invoke-AreaScript -ScriptPath (Join-Path $PSScriptRoot "Invoke-HomologacaoSqlNetwork.ps1") -Arguments $sharedArgs
$areas.Printer = Invoke-AreaScript -ScriptPath (Join-Path $PSScriptRoot "Invoke-HomologacaoPrinter.ps1") -Arguments (@{ ConfigPath = $ConfigPath; OutputRoot = $OutputRoot; NoPrompt = $NoPrompt })
$areas.Backup = Invoke-AreaScript -ScriptPath (Join-Path $PSScriptRoot "Invoke-HomologacaoBackupRestore.ps1") -Arguments $sharedArgs
$areas.Permissions = Invoke-AreaScript -ScriptPath (Join-Path $PSScriptRoot "Invoke-HomologacaoPermissions.ps1") -Arguments $sharedArgs
$areas.Simulation = Invoke-AreaScript -ScriptPath (Join-Path $PSScriptRoot "Invoke-HomologacaoOneWeekSimulation.ps1") -Arguments $sharedArgs
$areas.Crash = Invoke-AreaScript -ScriptPath (Join-Path $PSScriptRoot "Invoke-HomologacaoCrashRecovery.ps1") -Arguments $sharedArgs
$areas.Documents = Invoke-AreaScript -ScriptPath (Join-Path $PSScriptRoot "Invoke-HomologacaoDocuments.ps1") -Arguments $sharedArgs
$areas.Crud = Invoke-AreaScript -ScriptPath (Join-Path $PSScriptRoot "Invoke-HomologacaoCrud.ps1") -Arguments $sharedArgs
$areas.Visual = Invoke-AreaScript -ScriptPath (Join-Path $PSScriptRoot "Invoke-HomologacaoVisual.ps1") -Arguments $sharedArgs

$categoryResults = @(
    (Get-CategoryResult -Name "Instalacao" -Areas @($areas.Windows)),
    (Get-CategoryResult -Name "Rede" -Areas @($areas.Network)),
    (Get-CategoryResult -Name "Banco" -Areas @($areas.Network, $areas.Backup, $areas.Crud)),
    (Get-CategoryResult -Name "Backup" -Areas @($areas.Backup)),
    (Get-CategoryResult -Name "Impressao" -Areas @($areas.Printer, $areas.Documents)),
    (Get-CategoryResult -Name "Seguranca" -Areas @($areas.Permissions)),
    (Get-CategoryResult -Name "Multiusuario" -Areas @($areas.Network, $areas.Simulation)),
    (Get-CategoryResult -Name "Recuperacao apos falha" -Areas @($areas.Crash)),
    (Get-CategoryResult -Name "Funcionalidade" -Areas @($areas.Crud, $areas.Documents, $areas.Simulation)),
    (Get-CategoryResult -Name "Visual" -Areas @($areas.Visual))
)

$commercialBase = @($categoryResults | Where-Object { $_.Status -notlike "IGNORADO*" })
$commercialScore = if ($commercialBase.Count -gt 0) {
    [Math]::Round((($commercialBase | Measure-Object -Property Score -Average).Average), 1)
}
else {
    0
}

$commercialStatus = if (@($commercialBase | Where-Object { $_.Status -eq "REPROVADO" }).Count -gt 0) {
    "REPROVADO"
}
elseif (@($commercialBase | Where-Object { $_.Status -eq "APROVADO COM RESSALVAS" }).Count -gt 0) {
    "APROVADO COM RESSALVAS"
}
else {
    "APROVADO"
}

$categoryResults += [pscustomobject]@{
    Name = "Prontidao comercial"
    Status = $commercialStatus
    Score = $commercialScore
}

$failureChecks = @()
foreach ($area in $areas.Values) {
    $failureChecks += @($area.Checks | Where-Object { $_.Outcome -eq "Fail" })
}

$finalJsonPath = Join-Path $OutputRoot "RELATORIO_FINAL_HOMOLOGACAO.json"
$finalTxtPath = Join-Path $OutputRoot "RELATORIO_FINAL_HOMOLOGACAO.txt"
$finalMdPath = Join-Path $OutputRoot "RELATORIO_FINAL_HOMOLOGACAO.md"
$finalMdLatest = Join-Path $projectRoot "TestResults\HomologacaoFisica\RELATORIO_FINAL_HOMOLOGACAO.md"

$payload = [ordered]@{
    GeneratedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    OutputRoot = $OutputRoot
    Areas = $areas
    Categories = $categoryResults
    OverallStatus = $commercialStatus
    OverallScore = $commercialScore
}

$payload | ConvertTo-Json -Depth 10 | Set-Content -Path $finalJsonPath -Encoding UTF8

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# RELATORIO FINAL DE HOMOLOGACAO FISICA E COMERCIAL")
$lines.Add("")
$lines.Add("Gerado em: $($payload.GeneratedAt)")
$lines.Add("Resultado geral: $commercialStatus")
$lines.Add("Nota geral de prontidao comercial: $commercialScore")
$lines.Add("Pasta base: $OutputRoot")
$lines.Add("")
$lines.Add("## Classificacao por area")
$lines.Add("")
$lines.Add("| Area | Status | Nota |")
$lines.Add("| --- | --- | --- |")
foreach ($area in $areas.Values) {
    $lines.Add("| $($area.AreaName) | $($area.Status) | $($area.Score) |")
}
$lines.Add("")
$lines.Add("## Notas de 0 a 10")
$lines.Add("")
$lines.Add("| Categoria | Status | Nota |")
$lines.Add("| --- | --- | --- |")
foreach ($category in $categoryResults) {
    $lines.Add("| $($category.Name) | $($category.Status) | $($category.Score) |")
}
$lines.Add("")
$lines.Add("## Falhas registradas")
$lines.Add("")

if ($failureChecks.Count -eq 0) {
    $lines.Add("Nenhuma falha critica automatizada foi registrada nesta execucao.")
}
else {
    foreach ($failure in $failureChecks) {
        $lines.Add("- Modulo: $($failure.Module)")
        $lines.Add("  Check: $($failure.Name)")
        $lines.Add("  Erro: $($failure.Error)")
        $lines.Add("  Comando: $($failure.Command)")
        $lines.Add("  Arquivo afetado: $($failure.AffectedFile)")
        $lines.Add("  Possivel causa: $($failure.PossibleCause)")
        $lines.Add("  Sugestao de correcao: $($failure.SuggestedFix)")
    }
}

$lines.Add("")
$lines.Add("## Evidencias por area")
$lines.Add("")
foreach ($area in $areas.Values) {
    $jsonEvidence = if ($area.Files.ContainsKey("Json")) { $area.Files.Json } else { "" }
    $txtEvidence = if ($area.Files.ContainsKey("Text")) { $area.Files.Text } else { "" }
    $lines.Add("- $($area.AreaName): JSON=$jsonEvidence ; TXT=$txtEvidence")
}

$lines | Set-Content -Path $finalMdPath -Encoding UTF8
$lines | Set-Content -Path $finalTxtPath -Encoding UTF8
Copy-Item -Path $finalMdPath -Destination $finalMdLatest -Force

[pscustomobject]@{
    Status = $commercialStatus
    Score = $commercialScore
    OutputRoot = $OutputRoot
    MarkdownReport = $finalMdPath
    JsonReport = $finalJsonPath
    TextReport = $finalTxtPath
}
