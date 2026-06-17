[CmdletBinding()]
param(
    [string]$PrinterName,
    [switch]$PrintProbe
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$outputDir = Join-Path $projectRoot "TestResults\PhysicalHomologation\Printer\$timestamp"
$summaryPath = Join-Path $outputDir "printer-summary.json"
$reportPath = Join-Path $outputDir "printer-report.md"

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

function Get-InstalledPrinters {
    try {
        return @(Get-Printer | Select-Object Name, DriverName, PortName, PrinterStatus, WorkOffline, Default)
    }
    catch {
        return @(Get-CimInstance Win32_Printer | Select-Object Name, DriverName, PortName, PrinterStatus, WorkOffline, Default)
    }
}

$printers = @(Get-InstalledPrinters)
$targetPrinter = $null
if (-not [string]::IsNullOrWhiteSpace($PrinterName)) {
    $targetPrinter = $printers | Where-Object { $_.Name -eq $PrinterName } | Select-Object -First 1
}
elseif ($printers.Count -gt 0) {
    $targetPrinter = $printers | Where-Object { $_.Default -eq $true } | Select-Object -First 1
    if ($null -eq $targetPrinter) {
        $targetPrinter = $printers | Select-Object -First 1
    }
}

$checks = New-Object System.Collections.Generic.List[object]
$checks.Add([ordered]@{
    Name = "PrinterDetected"
    Status = if ($printers.Count -gt 0) { "APROVADO" } else { "FALHOU" }
    Details = "Impressoras detectadas: $($printers.Count)"
})

$checks.Add([ordered]@{
    Name = "TargetPrinterSelected"
    Status = if ($null -ne $targetPrinter) { "APROVADO" } else { "FALHOU" }
    Details = if ($null -ne $targetPrinter) { "Impressora alvo: $($targetPrinter.Name)" } else { "Nenhuma impressora alvo selecionada." }
})

$printProbeStatus = "IGNORADO"
$printProbeDetails = "Use -PrintProbe para enviar uma pagina real de teste."

if ($PrintProbe) {
    if ($null -eq $targetPrinter) {
        $printProbeStatus = "FALHOU"
        $printProbeDetails = "Nao ha impressora alvo para envio de pagina."
    }
    else {
        $probeFile = Join-Path $outputDir "printer-probe.txt"
        @(
            "Primo Auto Eletrica - teste fisico de impressao"
            "Data: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
            "Impressora: $($targetPrinter.Name)"
            "Se esta pagina saiu corretamente, a homologacao fisica de impressao foi aprovada."
        ) | Set-Content -Path $probeFile -Encoding UTF8

        try {
            Get-Content -Path $probeFile | Out-Printer -Name $targetPrinter.Name
            $printProbeStatus = "APROVADO"
            $printProbeDetails = "Pagina de teste enviada para $($targetPrinter.Name). Confirmacao visual do usuario ainda e necessaria."
        }
        catch {
            $printProbeStatus = "FALHOU"
            $printProbeDetails = $_.Exception.Message
        }
    }
}

$checks.Add([ordered]@{
    Name = "PrintProbe"
    Status = $printProbeStatus
    Details = $printProbeDetails
})

$overall = if (@($checks | Where-Object { $_.Status -eq "FALHOU" }).Count -eq 0) { "APROVADO" } else { "REPROVADO" }

$summary = [ordered]@{
    GeneratedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    MachineName = $env:COMPUTERNAME
    Overall = $overall
    PrinterName = if ($null -ne $targetPrinter) { $targetPrinter.Name } else { $null }
    Checks = $checks
    Printers = $printers
}

$summary | ConvertTo-Json -Depth 6 | Set-Content -Path $summaryPath -Encoding UTF8

@(
    "# Homologacao fisica de impressora"
    ""
    "Status: $overall"
    "Maquina: $env:COMPUTERNAME"
    "Gerado em: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    ""
    "## Checks"
    ($checks | ForEach-Object { "- $($_.Name): $($_.Status) - $($_.Details)" })
    ""
    "## Evidencia"
    "- JSON: $summaryPath"
) | Set-Content -Path $reportPath -Encoding UTF8

[PSCustomObject]@{
    Status = $overall
    Summary = $summaryPath
    Report = $reportPath
}
