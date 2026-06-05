param(
    [string]$RoteiroPath = "PrimoAutoEletrica\Docs\ROTEIRO_QA_MANUAL_FINAL_PRIMOAUTOELETRICA.md",
    [string]$OutputPath = "",
    [switch]$FailOnReprovado
)

$ErrorActionPreference = "Stop"

$solutionRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$roteiroFullPath = if ([System.IO.Path]::IsPathRooted($RoteiroPath)) {
    $RoteiroPath
}
else {
    Join-Path $solutionRoot $RoteiroPath
}

$roteiroFullPath = (Resolve-Path $roteiroFullPath).Path
$lines = Get-Content -Path $roteiroFullPath -Encoding UTF8
$currentSection = ""
$items = New-Object System.Collections.Generic.List[object]

foreach ($line in $lines) {
    if ($line -match "^##\s+(.+)$") {
        $currentSection = $Matches[1].Trim()
        continue
    }

    if ($line -notmatch "^\|") {
        continue
    }

    if ($currentSection -eq "Como registrar") {
        continue
    }

    if ($line -match "^\|\s*[-: ]+\|") {
        continue
    }

    $parts = $line.Trim("|") -split "\|"
    $parts = $parts | ForEach-Object { $_.Trim() }

    if ($parts.Count -lt 2) {
        continue
    }

    $title = $parts[0]
    $statusCell = $parts[1]

    if ($title -eq "Item" -or $title -eq "Campo" -or $title -eq "Resultado") {
        continue
    }

    $status = $null
    if ($statusCell -match "\[x\]") {
        $status = "Aprovado"
    }
    elseif ($statusCell -match "\[!\]") {
        $status = "Reprovado"
    }
    elseif ($statusCell -match "\[~\]") {
        $status = "Aprovado com ressalva"
    }
    elseif ($statusCell -match "\[ \]") {
        $status = "Pendente"
    }

    if ([string]::IsNullOrWhiteSpace($status)) {
        continue
    }

    $evidence = if ($parts.Count -ge 3) { $parts[2] } else { "" }
    $responsible = if ($parts.Count -ge 4) { $parts[3] } else { "" }

    $items.Add([PSCustomObject]@{
        Secao = $currentSection
        Item = $title
        Status = $status
        Evidencia = $evidence
        ResponsavelData = $responsible
    }) | Out-Null
}

$total = $items.Count
$approved = @($items | Where-Object { $_.Status -eq "Aprovado" }).Count
$withWarning = @($items | Where-Object { $_.Status -eq "Aprovado com ressalva" }).Count
$failed = @($items | Where-Object { $_.Status -eq "Reprovado" }).Count
$pending = @($items | Where-Object { $_.Status -eq "Pendente" }).Count

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $outputFullPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
        $OutputPath
    }
    else {
        Join-Path $solutionRoot $OutputPath
    }

    $outputDirectory = Split-Path -Path $outputFullPath -Parent
    if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    }

    $generatedAt = Get-Date
    $report = New-Object System.Collections.Generic.List[string]
    $report.Add("# Status QA manual - PrimoAutoEletrica")
    $report.Add("")
    $report.Add(("Gerado em: {0:dd/MM/yyyy HH:mm:ss}" -f $generatedAt))
    $report.Add("")
    $report.Add(("Roteiro origem: {0}" -f $roteiroFullPath))
    $report.Add("")
    $report.Add("## Resumo")
    $report.Add("")
    $report.Add("| Total | Aprovado | Aprovado com ressalva | Reprovado | Pendente |")
    $report.Add("| --- | --- | --- | --- | --- |")
    $report.Add(("| {0} | {1} | {2} | {3} | {4} |" -f $total, $approved, $withWarning, $failed, $pending))
    $report.Add("")
    $report.Add("## Resumo por secao")
    $report.Add("")
    $report.Add("| Secao | Total | Aprovado | Ressalva | Reprovado | Pendente |")
    $report.Add("| --- | --- | --- | --- | --- | --- |")

    foreach ($group in ($items | Group-Object Secao)) {
        $sectionItems = @($group.Group)
        $report.Add(("| {0} | {1} | {2} | {3} | {4} | {5} |" -f `
            $group.Name,
            $sectionItems.Count,
            @($sectionItems | Where-Object { $_.Status -eq "Aprovado" }).Count,
            @($sectionItems | Where-Object { $_.Status -eq "Aprovado com ressalva" }).Count,
            @($sectionItems | Where-Object { $_.Status -eq "Reprovado" }).Count,
            @($sectionItems | Where-Object { $_.Status -eq "Pendente" }).Count))
    }

    $pendingItems = @($items | Where-Object { $_.Status -eq "Pendente" })
    if ($pendingItems.Count -gt 0) {
        $report.Add("")
        $report.Add("## Pendencias")
        $report.Add("")
        foreach ($item in $pendingItems) {
            $report.Add(("- **{0}**: {1}" -f $item.Secao, $item.Item))
        }
    }

    $failedItems = @($items | Where-Object { $_.Status -eq "Reprovado" })
    if ($failedItems.Count -gt 0) {
        $report.Add("")
        $report.Add("## Reprovacoes")
        $report.Add("")
        foreach ($item in $failedItems) {
            $report.Add(("- **{0}**: {1}" -f $item.Secao, $item.Item))
        }
    }

    Set-Content -Path $outputFullPath -Value $report.ToArray() -Encoding UTF8
}
else {
    $outputFullPath = ""
}

$summary = [PSCustomObject]@{
    RoteiroPath = $roteiroFullPath
    Total = $total
    Aprovado = $approved
    AprovadoComRessalva = $withWarning
    Reprovado = $failed
    Pendente = $pending
    OutputPath = $outputFullPath
}

$summary

if ($FailOnReprovado -and $failed -gt 0) {
    throw "QA manual possui $failed item(ns) reprovado(s)."
}
