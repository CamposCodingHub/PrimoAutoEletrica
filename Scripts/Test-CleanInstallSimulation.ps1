[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [string]$Framework = "net9.0-windows",
    [switch]$KeepArtifacts
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$simulationRoot = Join-Path $projectRoot "TestResults\CleanInstallSimulation\$timestamp"
$packageOutput = Join-Path $simulationRoot "Package"
$installDir = Join-Path $simulationRoot "InstalledApp"
$dataDir = Join-Path $simulationRoot "AppData"
$logFile = Join-Path $simulationRoot "clean-install-simulation.log"

New-Item -ItemType Directory -Path $simulationRoot -Force | Out-Null

function Write-SimLog {
    param([string]$Message)
    $entry = "{0} {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Message
    Add-Content -Path $logFile -Value $entry -Encoding UTF8
    Write-Host $entry
}

Write-SimLog "Gerando pacote instalavel em ambiente limpo."
$packageOutputResult = & (Join-Path $scriptRoot "New-WindowsInstallerPackage.ps1") `
    -OutputDirectory $packageOutput `
    -Configuration $Configuration `
    -Framework $Framework `
    -SkipZip

$package = @($packageOutputResult) |
    Where-Object { $_ -is [psobject] -and $_.PSObject.Properties.Name -contains "PackageRoot" } |
    Select-Object -Last 1

if ($null -eq $package) {
    throw "Gerador de pacote nao retornou metadados PackageRoot."
}

$packageRoot = $package.PackageRoot
$installScript = Join-Path $packageRoot "Install-PrimoAutoEletrica.ps1"
$updateScript = Join-Path $packageRoot "Update-PrimoAutoEletrica.ps1"
$manifest = Join-Path $packageRoot "INSTALLER_MANIFEST.txt"

foreach ($required in @($installScript, $updateScript, $manifest, (Join-Path $packageRoot "PrimoAutoEletrica\PrimoAutoEletrica.exe"))) {
    if (-not (Test-Path $required)) {
        throw "Item obrigatorio ausente na simulacao de instalacao: $required"
    }
}

Write-SimLog "Executando instalador em pasta temporaria."
& powershell -NoProfile -ExecutionPolicy Bypass -File $installScript `
    -InstallDirectory $installDir `
    -DataDirectory $dataDir `
    -NoShortcuts `
    -SkipDotNetCheck | Out-Null

$installedExe = Join-Path $installDir "PrimoAutoEletrica.exe"
if (-not (Test-Path $installedExe)) {
    throw "Executavel nao foi instalado em $installedExe"
}

if (-not (Test-Path $dataDir)) {
    throw "Pasta de dados nao foi criada em $dataDir"
}

$readme = Join-Path $packageRoot "README-INSTALACAO.txt"
if (-not (Test-Path $readme)) {
    throw "README de instalacao nao foi gerado."
}

Write-SimLog "Simulacao de instalacao limpa aprovada."

if (-not $KeepArtifacts) {
    Write-SimLog "Artefatos mantidos em $simulationRoot para auditoria automatizada."
}

[PSCustomObject]@{
    Status = "APROVADO"
    SimulationRoot = $simulationRoot
    PackageRoot = $packageRoot
    InstalledExe = $installedExe
    DataDirectory = $dataDir
    LogFile = $logFile
}
