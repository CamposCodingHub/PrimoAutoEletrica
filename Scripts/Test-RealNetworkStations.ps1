[CmdletBinding()]
param(
    [string]$PeerAddress,
    [string]$SharedPath,
    [int]$Port = 52000
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$outputDir = Join-Path $projectRoot "TestResults\PhysicalHomologation\NetworkStations\$timestamp"
$summaryPath = Join-Path $outputDir "network-stations-summary.json"
$reportPath = Join-Path $outputDir "network-stations-report.md"

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

function New-Check {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Details
    )

    [ordered]@{
        Name = $Name
        Status = $Status
        Details = $Details
    }
}

$checks = New-Object System.Collections.Generic.List[object]
$checks.Add((New-Check "MachineIdentity" "APROVADO" "Maquina=$env:COMPUTERNAME; Usuario=$env:USERNAME"))

if (-not [string]::IsNullOrWhiteSpace($PeerAddress)) {
    $pingOk = $false
    try {
        $pingOk = Test-Connection -ComputerName $PeerAddress -Count 2 -Quiet
    }
    catch {
        $pingOk = $false
    }

    $checks.Add((New-Check "PeerPing" $(if ($pingOk) { "APROVADO" } else { "FALHOU" }) "Peer=$PeerAddress"))

    try {
        $tcp = Test-NetConnection -ComputerName $PeerAddress -Port $Port -WarningAction SilentlyContinue
        $checks.Add((New-Check "PeerPort" $(if ($tcp.TcpTestSucceeded) { "APROVADO" } else { "FALHOU" }) "Peer=$PeerAddress; Port=$Port; Result=$($tcp.TcpTestSucceeded)"))
    }
    catch {
        $checks.Add((New-Check "PeerPort" "FALHOU" $_.Exception.Message))
    }
}
else {
    $checks.Add((New-Check "PeerPing" "IGNORADO" "Informe -PeerAddress para validar comunicacao com outra estacao."))
    $checks.Add((New-Check "PeerPort" "IGNORADO" "Informe -PeerAddress para validar porta $Port."))
}

if (-not [string]::IsNullOrWhiteSpace($SharedPath)) {
    try {
        New-Item -ItemType Directory -Path $SharedPath -Force | Out-Null
        $probeFile = Join-Path $SharedPath "primo-network-probe-$env:COMPUTERNAME-$timestamp.txt"
        "Primo Auto Eletrica network probe $(Get-Date -Format 'O')" | Set-Content -Path $probeFile -Encoding UTF8
        $readBack = Get-Content -Path $probeFile -Raw
        Remove-Item -Path $probeFile -Force
        $checks.Add((New-Check "SharedPathReadWrite" "APROVADO" "Compartilhamento validado: $SharedPath; Conteudo=$($readBack.Trim())"))
    }
    catch {
        $checks.Add((New-Check "SharedPathReadWrite" "FALHOU" $_.Exception.Message))
    }
}
else {
    $checks.Add((New-Check "SharedPathReadWrite" "IGNORADO" "Informe -SharedPath para validar escrita/leitura em pasta de rede."))
}

$localSimulator = Join-Path $projectRoot "Tools\simulate-two-stations.ps1"
if (Test-Path $localSimulator) {
    try {
        & $localSimulator -Duration 5 | Tee-Object -FilePath (Join-Path $outputDir "local-sync-simulator.log") | Out-Null
        $checks.Add((New-Check "LocalSyncSimulator" "APROVADO" "Simulador local executado por 5 segundos."))
    }
    catch {
        $checks.Add((New-Check "LocalSyncSimulator" "FALHOU" $_.Exception.Message))
    }
}
else {
    $checks.Add((New-Check "LocalSyncSimulator" "IGNORADO" "Script nao encontrado: $localSimulator"))
}

$overall = if (@($checks | Where-Object { $_.Status -eq "FALHOU" }).Count -eq 0) { "APROVADO" } else { "REPROVADO" }

$summary = [ordered]@{
    GeneratedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    MachineName = $env:COMPUTERNAME
    Overall = $overall
    PeerAddress = $PeerAddress
    SharedPath = $SharedPath
    Port = $Port
    Checks = $checks
}

$summary | ConvertTo-Json -Depth 6 | Set-Content -Path $summaryPath -Encoding UTF8

@(
    "# Homologacao rede/estacoes reais"
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
