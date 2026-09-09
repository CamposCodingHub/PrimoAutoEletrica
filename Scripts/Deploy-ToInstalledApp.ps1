<#
.SYNOPSIS
  Compila o PrimoAutoEletrica e atualiza a copia instalada (atalho da area de trabalho).
#>
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$InstallDirectory = "",

    [switch]$ForceStop,

    [switch]$Launch,

    [switch]$SkipBuild,

    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Get-PrimoDesktopLinks {
    $desktops = @(
        [Environment]::GetFolderPath("Desktop"),
        (Join-Path $env:USERPROFILE "OneDrive\Desktop"),
        [Environment]::GetFolderPath("CommonDesktopDirectory")
    ) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -Unique

    $shell = New-Object -ComObject WScript.Shell
    $results = @()
    foreach ($desktop in $desktops) {
        $links = Get-ChildItem -LiteralPath $desktop -Filter "*.lnk" -ErrorAction SilentlyContinue |
            Where-Object { $_.BaseName -match "(?i)primo" }

        foreach ($link in $links) {
            $shortcut = $shell.CreateShortcut($link.FullName)
            $results += [PSCustomObject]@{
                ShortcutPath = $link.FullName
                TargetPath = $shortcut.TargetPath
                WorkingDirectory = $shortcut.WorkingDirectory
            }
        }
    }

    return $results
}

function Get-DesktopShortcutTarget {
    $links = @(Get-PrimoDesktopLinks)
    # Preferencia: atalho publico "PRIMOX Workshop" / instalacao em Program Files.
    $preferred = $links | Where-Object {
        $_.ShortcutPath -match '(?i)Public\\Desktop\\PRIMOX' -or
        ($_.TargetPath -and $_.TargetPath -match '(?i)\\Program Files\\PRIMOX\\')
    } | Select-Object -First 1
    if ($preferred -and $preferred.TargetPath -and (Test-Path -LiteralPath $preferred.TargetPath)) {
        return $preferred
    }

    foreach ($link in $links) {
        if ($link.TargetPath -and (Test-Path -LiteralPath $link.TargetPath)) {
            return $link
        }
    }

    return $null
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Request-ElevationIfNeeded([string]$InstallDirectory) {
    $programFiles = $env:ProgramFiles
    $programFilesX86 = ${env:ProgramFiles(x86)}
    $needsAdmin = $false
    if ($programFiles -and $InstallDirectory.StartsWith($programFiles, [StringComparison]::OrdinalIgnoreCase)) {
        $needsAdmin = $true
    }
    if ($programFilesX86 -and $InstallDirectory.StartsWith($programFilesX86, [StringComparison]::OrdinalIgnoreCase)) {
        $needsAdmin = $true
    }

    if (-not $needsAdmin -or (Test-IsAdministrator)) {
        return
    }

    Write-Host "A pasta de instalacao exige administrador. Reiniciando o script elevado..." -ForegroundColor Yellow

    $argList = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", "`"$PSCommandPath`"",
        "-Configuration", $Configuration
    )

    if ($InstallDirectory) { $argList += @("-InstallDirectory", "`"$InstallDirectory`"") }
    if ($ForceStop) { $argList += "-ForceStop" }
    if ($Launch) { $argList += "-Launch" }
    if ($SkipBuild) { $argList += "-SkipBuild" }
    if ($FrameworkDependent) { $argList += "-FrameworkDependent" }

    $process = Start-Process -FilePath "powershell.exe" -Verb RunAs -ArgumentList $argList -Wait -PassThru
    exit $process.ExitCode
}

$solutionRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $solutionRoot "PrimoAutoEletrica\PrimoAutoEletrica.csproj"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Projeto nao encontrado: $projectPath"
}

$shortcutInfo = Get-DesktopShortcutTarget
$defaultUserInstall = Join-Path $env:LOCALAPPDATA "PrimoAutoEletrica\App"

if ([string]::IsNullOrWhiteSpace($InstallDirectory)) {
    # Preferencia: pasta do atalho da area de trabalho (inclui Program Files / PRIMOX Workshop).
    if ($shortcutInfo -and $shortcutInfo.TargetPath) {
        $InstallDirectory = Split-Path -Parent $shortcutInfo.TargetPath
        Write-Host "Instalacao detectada pelo atalho: $InstallDirectory"
        Write-Host "Atalho: $($shortcutInfo.ShortcutPath)"
    }
    else {
        $InstallDirectory = $defaultUserInstall
        Write-Host "Usando instalacao do usuario (sem admin): $InstallDirectory"
    }
}

Request-ElevationIfNeeded -InstallDirectory $InstallDirectory

Write-Step "Resumo"
Write-Host "Projeto:      $projectPath"
Write-Host "Config:       $Configuration"
Write-Host "Instalacao:   $InstallDirectory"
Write-Host "Dados (AppData) NAO serao apagados."

$running = @(Get-Process -Name "PrimoAutoEletrica" -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    if (-not $ForceStop) {
        throw "O PrimoAutoEletrica esta aberto. Feche-o ou rode com -ForceStop."
    }

    Write-Step "Encerrando processo em execucao"
    $running | Stop-Process -Force
    Start-Sleep -Seconds 1
}

$csproj = Get-Content -LiteralPath $projectPath -Raw
if ($csproj -match "<TargetFramework>([^<]+)</TargetFramework>") {
    $tfm = $Matches[1].Trim()
}
else {
    $tfm = "net6.0-windows"
}

$publishDir = Join-Path $solutionRoot "PrimoAutoEletrica\bin\$Configuration\$tfm\win-x64\publish"

if (-not $SkipBuild) {
    Write-Step "Publicando aplicativo ($tfm / $Configuration)"

    $publishArgs = @(
        "publish", $projectPath,
        "-c", $Configuration,
        "-r", "win-x64",
        "-o", $publishDir,
        "--nologo"
    )

    if ($FrameworkDependent) {
        $publishArgs += @("--self-contained", "false")
    }
    else {
        $publishArgs += @("--self-contained", "true")
    }

    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish falhou com codigo $LASTEXITCODE"
    }
}

$publishedExe = Join-Path $publishDir "PrimoAutoEletrica.exe"
if (-not (Test-Path -LiteralPath $publishedExe)) {
    throw "Publish nao gerou o executavel em $publishedExe"
}

Write-Step "Backup da instalacao atual"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupRoot = Join-Path $env:LOCALAPPDATA "PrimoAutoEletrica\Backups\BeforeDeploy"
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null

if (Test-Path -LiteralPath $InstallDirectory) {
    $backupDir = Join-Path $backupRoot "install-$timestamp"
    Copy-Item -LiteralPath $InstallDirectory -Destination $backupDir -Recurse -Force
    Write-Host "Backup criado em: $backupDir"
}
else {
    New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null
    Write-Host "Pasta de instalacao criada."
}

Write-Step "Copiando arquivos novos para a instalacao"
$preserveNames = @("Data", "Logs", "Config", "Backups", "Media", "Updates", "AutomatedTests")
$tempPreserve = Join-Path $env:TEMP "PrimoDeployPreserve-$timestamp"
New-Item -ItemType Directory -Path $tempPreserve -Force | Out-Null

foreach ($name in $preserveNames) {
    $source = Join-Path $InstallDirectory $name
    if (Test-Path -LiteralPath $source) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $tempPreserve $name) -Recurse -Force
    }
}

Get-ChildItem -LiteralPath $InstallDirectory -Force |
    Where-Object { $preserveNames -notcontains $_.Name -and $_.Name -notmatch "^unins" } |
    ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }

Copy-Item -Path (Join-Path $publishDir "*") -Destination $InstallDirectory -Recurse -Force

foreach ($name in $preserveNames) {
    $saved = Join-Path $tempPreserve $name
    if (Test-Path -LiteralPath $saved) {
        $dest = Join-Path $InstallDirectory $name
        if (-not (Test-Path -LiteralPath $dest)) {
            Copy-Item -LiteralPath $saved -Destination $dest -Recurse -Force
        }
    }
}

Remove-Item -LiteralPath $tempPreserve -Recurse -Force -ErrorAction SilentlyContinue

$installedExe = Join-Path $InstallDirectory "PrimoAutoEletrica.exe"
if (-not (Test-Path -LiteralPath $installedExe)) {
    throw "Atualizacao falhou: $installedExe nao encontrado."
}

Write-Step "Atualizando atalho(s) da area de trabalho"
$shell = New-Object -ComObject WScript.Shell
$iconCandidate = Join-Path $InstallDirectory "icon.ico"
$iconLocation = if (Test-Path -LiteralPath $iconCandidate) { $iconCandidate } else { $installedExe }

$existingLinks = @(Get-PrimoDesktopLinks)
$pathsToUpdate = @($existingLinks | ForEach-Object { $_.ShortcutPath })

# Garante atalho do usuario mesmo se so existir o publico "PRIMOX Workshop".
$userDesktop = [Environment]::GetFolderPath("Desktop")
if (-not $userDesktop) { $userDesktop = Join-Path $env:USERPROFILE "Desktop" }
$fallbackUserLnk = Join-Path $userDesktop "PRIMOX Workshop.lnk"
if ((Test-Path $userDesktop) -and ($pathsToUpdate -notcontains $fallbackUserLnk) -and -not ($existingLinks | Where-Object { $_.ShortcutPath -like "*\PRIMOX Workshop.lnk" })) {
    $pathsToUpdate += $fallbackUserLnk
}

foreach ($lnkPath in ($pathsToUpdate | Select-Object -Unique)) {
    try {
        $shortcut = $shell.CreateShortcut($lnkPath)
        $shortcut.TargetPath = $installedExe
        $shortcut.WorkingDirectory = $InstallDirectory
        $shortcut.Description = "PRIMOX Workshop"
        $shortcut.IconLocation = $iconLocation
        $shortcut.Save()
        Write-Host "Atalho atualizado: $lnkPath"
    }
    catch {
        Write-Host "Nao foi possivel atualizar $lnkPath : $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

$installedInfo = Get-Item -LiteralPath $installedExe
Write-Step "Concluido"
Write-Host "Executavel: $($installedInfo.FullName)"
Write-Host "Atualizado em: $($installedInfo.LastWriteTime)"
Write-Host ""
Write-Host "NAO e necessario desinstalar. Seus dados em %LOCALAPPDATA%\PrimoAutoEletrica permanecem." -ForegroundColor Green

if ($Launch) {
    Write-Step "Abrindo o sistema atualizado"
    Start-Process -FilePath $installedExe -WorkingDirectory $InstallDirectory
}

[PSCustomObject]@{
    InstallDirectory = $InstallDirectory
    PublishedFrom = $publishDir
    Executable = $installedExe
    LastWriteTime = $installedInfo.LastWriteTime
}
