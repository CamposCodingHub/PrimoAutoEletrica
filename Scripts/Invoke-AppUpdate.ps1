[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory,

    [string]$InstallDirectory = (Join-Path $env:ProgramFiles "PrimoAutoEletrica"),

    [string]$AppDataDirectory = (Join-Path $env:LOCALAPPDATA "PrimoAutoEletrica"),

    [switch]$ForceStop
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logRoot = Join-Path $env:ProgramData "PrimoAutoEletrica\UpdateLogs"
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
$logFile = Join-Path $logRoot "update-$timestamp.log"

function Write-UpdateLog {
    param([string]$Message)
    $entry = "{0} {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Message
    Add-Content -Path $logFile -Value $entry -Encoding UTF8
    Write-Host $entry
}

$packageFullPath = (Resolve-Path $PackageDirectory).Path
$appPayload = Join-Path $packageFullPath "PrimoAutoEletrica"
$exeInPayload = Join-Path $appPayload "PrimoAutoEletrica.exe"

if (-not (Test-Path $exeInPayload)) {
    throw "Pacote invalido: executavel nao encontrado em $exeInPayload"
}

Write-UpdateLog "Iniciando atualizacao. Pacote=$packageFullPath InstallDirectory=$InstallDirectory"

$running = @(Get-Process -Name "PrimoAutoEletrica" -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    if (-not $ForceStop) {
        throw "PrimoAutoEletrica esta em execucao. Feche o sistema ou use -ForceStop."
    }

    Write-UpdateLog "Encerrando processo PrimoAutoEletrica para atualizacao."
    $running | Stop-Process -Force
}

New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $AppDataDirectory -Force | Out-Null

$databasePath = Join-Path $AppDataDirectory "primoauto.db"
if (Test-Path $databasePath) {
    $backupDir = Join-Path $AppDataDirectory "Backups\BeforeUpdate"
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    $backupPath = Join-Path $backupDir "primoauto-before-update-$timestamp.db"
    Copy-Item -LiteralPath $databasePath -Destination $backupPath -Force
    Write-UpdateLog "Backup antes da atualizacao criado em $backupPath"
}
else {
    Write-UpdateLog "Banco local ainda nao existe. Backup antes da atualizacao nao necessario."
}

$backupInstallDir = "$InstallDirectory.backup-$timestamp"
if (Test-Path $InstallDirectory) {
    Copy-Item -LiteralPath $InstallDirectory -Destination $backupInstallDir -Recurse -Force
    Write-UpdateLog "Snapshot da instalacao anterior criado em $backupInstallDir"
}

Copy-Item -Path (Join-Path $appPayload "*") -Destination $InstallDirectory -Recurse -Force

$installedExe = Join-Path $InstallDirectory "PrimoAutoEletrica.exe"
if (-not (Test-Path $installedExe)) {
    throw "Atualizacao falhou: executavel final nao encontrado em $installedExe"
}

Write-UpdateLog "Atualizacao concluida. Executavel=$installedExe"
Write-UpdateLog "Log da atualizacao: $logFile"

[PSCustomObject]@{
    InstallDirectory = $InstallDirectory
    AppDataDirectory = $AppDataDirectory
    LogFile = $logFile
    BackupInstallDirectory = $backupInstallDir
}
