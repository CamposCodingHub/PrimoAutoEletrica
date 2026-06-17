[CmdletBinding()]
param(
    [string]$ConfigPath = "",
    [string]$OutputRoot = "",
    [switch]$NoPrompt
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot "HomologacaoFisica.Common.psm1") -Force

function Test-IsVirtualPrinter {
    param(
        [string]$Name,
        [string]$DriverName,
        [string]$PortName
    )

    $patterns = @("pdf", "xps", "fax", "onenote", "document writer", "portprompt", "file:", "nul:", "snagit", "cutepdf", "pdfcreator", "bullzip")
    $haystack = "{0} {1} {2}" -f $Name, $DriverName, $PortName
    foreach ($pattern in $patterns) {
        if ($haystack -match [regex]::Escape($pattern)) {
            return $true
        }
    }

    return $false
}

$projectRoot = Get-HomologacaoProjectRoot -ScriptPath $PSScriptRoot
$config = Import-HomologacaoConfig -Path $ConfigPath -ProjectRoot $projectRoot

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $projectRoot ("TestResults\HomologacaoFisica\{0}" -f (Get-Date -Format "yyyy-MM-dd_HH-mm-ss"))
}

$outputDirectory = Join-Path $OutputRoot "Printer"
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

try {
    $printers = @(Get-Printer -ErrorAction Stop | Select-Object Name, DriverName, PortName, PrinterStatus, WorkOffline, Default)
}
catch {
    $printers = @(Get-CimInstance Win32_Printer | Select-Object Name, DriverName, PortName, PrinterStatus, WorkOffline, Default)
}

$selectedName = [string]$config.Printer.PrinterName
$physicalPrinters = @($printers | Where-Object { -not (Test-IsVirtualPrinter -Name $_.Name -DriverName $_.DriverName -PortName $_.PortName) })
$targetPrinter = if (-not [string]::IsNullOrWhiteSpace($selectedName)) {
    $printers | Where-Object { $_.Name -eq $selectedName } | Select-Object -First 1
}
else {
    $physicalPrinters | Where-Object { $_.Default } | Select-Object -First 1
}

if ($null -eq $targetPrinter) {
    $targetPrinter = $physicalPrinters | Select-Object -First 1
}

$checks = New-Object System.Collections.Generic.List[object]
$checks.Add((New-HomologacaoCheck -Name "InstalledPrinters" -Outcome $(if ($printers.Count -gt 0) { "Pass" } else { "Warn" }) -Details "Total=$($printers.Count)" -Module "Impressao" -PossibleCause "Nenhuma fila instalada no Windows." -SuggestedFix "Instalar impressora fisica e Microsoft Print to PDF." ))
$checks.Add((New-HomologacaoCheck -Name "MicrosoftPrintToPdf" -Outcome $(if (@($printers | Where-Object { $_.Name -match "Microsoft Print to PDF" }).Count -gt 0) { "Pass" } else { "Warn" }) -Details "Presenca da impressora virtual padrao: $(@($printers | Where-Object { $_.Name -match 'Microsoft Print to PDF' }).Count -gt 0)" -Module "Impressao" -PossibleCause "Driver virtual do Windows ausente." -SuggestedFix "Habilitar Microsoft Print to PDF." ))

if ($physicalPrinters.Count -eq 0) {
    $checks.Add((New-HomologacaoCheck -Name "PhysicalPrinterRequired" -Outcome "Skip" -Details "Nenhuma impressora fisica detectada nesta estacao." -Module "Impressao" -PossibleCause "Somente filas virtuais disponiveis." -SuggestedFix "Conectar a impressora fisica da oficina antes da homologacao final." ))
    $area = New-HomologacaoAreaResult `
        -AreaId "printer-real" `
        -AreaName "Impressora fisica real" `
        -OutputDirectory $outputDirectory `
        -Checks $checks `
        -PreferredIgnoredStatus "IGNORADO POR FALTA DE HARDWARE" `
        -Metrics @{
            InstalledPrinters = $printers.Count
            PhysicalPrinters = $physicalPrinters.Count
        }

    Write-HomologacaoAreaArtifacts -AreaResult $area -JsonPath (Join-Path $OutputRoot "printer-real-summary.json") -TextPath (Join-Path $OutputRoot "printer-real-summary.txt")
    return
}

$queueDetails = if ($null -ne $targetPrinter) {
    "Name=$($targetPrinter.Name); Driver=$($targetPrinter.DriverName); Port=$($targetPrinter.PortName); Offline=$($targetPrinter.WorkOffline); Status=$($targetPrinter.PrinterStatus)"
}
else {
    "Nenhuma impressora alvo selecionada."
}
$checks.Add((New-HomologacaoCheck -Name "TargetPrinter" -Outcome $(if ($null -ne $targetPrinter) { "Pass" } else { "Fail" }) -Details $queueDetails -Module "Impressao" -PossibleCause "A impressora fisica nao esta configurada ou foi removida." -SuggestedFix "Selecionar uma impressora fisica valida no arquivo de configuracao." ))

if ($null -ne $targetPrinter) {
    $documents = @(
        @{ Name = "test-page"; Title = "Pagina de teste"; Body = "Teste fisico da impressora." },
        @{ Name = "orcamento"; Title = "Orcamento teste"; Body = "Documento comercial sintetico para homologacao." },
        @{ Name = "ordem-servico"; Title = "Ordem de servico teste"; Body = "OS sintetica para confirmar layout e spool." },
        @{ Name = "recibo-venda"; Title = "Recibo/venda teste"; Body = "Comprovante sintetico de venda para PDV." }
    )

    foreach ($document in $documents) {
        $documentPath = Join-Path $outputDirectory ("{0}.txt" -f $document.Name)
        @(
            "Primo Auto Eletrica - homologacao fisica"
            "Documento: $($document.Title)"
            "Data: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
            "Impressora: $($targetPrinter.Name)"
            ""
            $document.Body
        ) | Set-Content -Path $documentPath -Encoding UTF8

        try {
            Get-Content -Path $documentPath | Out-Printer -Name $targetPrinter.Name
            $checks.Add((New-HomologacaoCheck -Name ("Print:" + $document.Name) -Outcome "Pass" -Details "Documento enviado para fila $($targetPrinter.Name)." -Module "Impressao" -Command "Get-Content '$documentPath' | Out-Printer -Name '$($targetPrinter.Name)'" -AffectedFile $documentPath ))
        }
        catch {
            $checks.Add((New-HomologacaoCheck -Name ("Print:" + $document.Name) -Outcome "Fail" -Details "Falha ao enviar documento para a fila." -Module "Impressao" -ErrorMessage $_.Exception.Message -Command "Get-Content '$documentPath' | Out-Printer -Name '$($targetPrinter.Name)'" -AffectedFile $documentPath -PossibleCause "Fila offline, driver com erro ou spooler indisponivel." -SuggestedFix "Validar spooler, cabo/USB/rede e driver da impressora fisica." ))
        }
    }
}

$manualConfirmation = [string]$config.Printer.ManualConfirmation
if ([string]::IsNullOrWhiteSpace($manualConfirmation) -and -not $NoPrompt -and [bool]$config.Printer.AskManualConfirmation) {
    try {
        $manualConfirmation = Read-Host "A pagina saiu corretamente? (sim/nao)"
    }
    catch {
        $manualConfirmation = ""
    }
}

if ([string]::IsNullOrWhiteSpace($manualConfirmation)) {
    $checks.Add((New-HomologacaoCheck -Name "ManualConfirmation" -Outcome "Warn" -Details "Confirmacao manual nao informada." -Module "Impressao" -PossibleCause "Execucao nao interativa ou operador nao respondeu." -SuggestedFix "Registrar confirmacao manual do operador da oficina apos a impressao." ))
}
elseif ($manualConfirmation.Trim().ToLowerInvariant() -in @("sim", "s", "yes", "y")) {
    $checks.Add((New-HomologacaoCheck -Name "ManualConfirmation" -Outcome "Pass" -Details "Operador confirmou a saida correta da pagina." -Module "Impressao"))
}
else {
    $checks.Add((New-HomologacaoCheck -Name "ManualConfirmation" -Outcome "Fail" -Details "Operador informou que a pagina nao saiu corretamente." -Module "Impressao" -PossibleCause "Layout fisico, corte, falha de driver ou problema mecanico na impressora." -SuggestedFix "Revisar impressora, margens, tamanho de papel e spool antes do go-live." ))
}

$area = New-HomologacaoAreaResult `
    -AreaId "printer-real" `
    -AreaName "Impressora fisica real" `
    -OutputDirectory $outputDirectory `
    -Checks $checks `
    -Metrics @{
        InstalledPrinters = $printers.Count
        PhysicalPrinters = $physicalPrinters.Count
        TargetPrinter = if ($null -ne $targetPrinter) { $targetPrinter.Name } else { "" }
    }

Write-HomologacaoAreaArtifacts -AreaResult $area -JsonPath (Join-Path $OutputRoot "printer-real-summary.json") -TextPath (Join-Path $OutputRoot "printer-real-summary.txt")
