[CmdletBinding()]
param(
    [string]$OutputDirectory = "Artifacts\Installer",
    [string]$Configuration = "Release",
    [string]$Framework = "net6.0-windows",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained,
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$projectPath = Join-Path $projectRoot "PrimoAutoEletrica\PrimoAutoEletrica.csproj"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

$outputRoot = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory
}
else {
    Join-Path $projectRoot $OutputDirectory
}

$packageRoot = Join-Path $outputRoot "PrimoAutoEletrica_Installer_$timestamp"
$payloadRoot = Join-Path $packageRoot "PrimoAutoEletrica"
$manifestPath = Join-Path $packageRoot "INSTALLER_MANIFEST.txt"
$installScriptPath = Join-Path $packageRoot "Install-PrimoAutoEletrica.ps1"
$updateScriptPath = Join-Path $packageRoot "Update-PrimoAutoEletrica.ps1"
$readmePath = Join-Path $packageRoot "README-INSTALACAO.txt"

New-Item -ItemType Directory -Path $payloadRoot -Force | Out-Null

$publishArgs = @("publish", $projectPath, "-c", $Configuration, "-f", $Framework, "-o", $payloadRoot)
if ($SelfContained) {
    $publishArgs += @("-r", $Runtime, "--self-contained", "true")
}

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish falhou com codigo $LASTEXITCODE"
}

$installerScript = @'
[CmdletBinding()]
param(
    [string]$InstallDirectory = (Join-Path $env:ProgramFiles "PrimoAutoEletrica"),
    [string]$DataDirectory = (Join-Path $env:LOCALAPPDATA "PrimoAutoEletrica"),
    [switch]$NoShortcuts,
    [switch]$SkipDotNetCheck
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$packageRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$payloadRoot = Join-Path $packageRoot "PrimoAutoEletrica"
$exePath = Join-Path $payloadRoot "PrimoAutoEletrica.exe"

if (-not (Test-Path $exePath)) {
    throw "Pacote invalido: PrimoAutoEletrica.exe nao encontrado."
}

    if (-not $SkipDotNetCheck) {
    $runtimes = & dotnet --list-runtimes 2>$null
    if ($LASTEXITCODE -ne 0 -or -not ($runtimes -match "Microsoft.WindowsDesktop.App 6\.")) {
        throw ".NET 6 Desktop Runtime nao encontrado. Instale Microsoft.WindowsDesktop.App 6.x ou gere pacote self-contained."
    }
}

New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $DataDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $payloadRoot "*") -Destination $InstallDirectory -Recurse -Force

$installedExe = Join-Path $InstallDirectory "PrimoAutoEletrica.exe"
if (-not (Test-Path $installedExe)) {
    throw "Instalacao falhou: executavel nao encontrado em $installedExe"
}

if (-not $NoShortcuts) {
    $shell = New-Object -ComObject WScript.Shell
    $desktopShortcut = Join-Path ([Environment]::GetFolderPath("Desktop")) "Primo Auto Eletrica.lnk"
    $shortcut = $shell.CreateShortcut($desktopShortcut)
    $shortcut.TargetPath = $installedExe
    $shortcut.WorkingDirectory = $InstallDirectory
    $shortcut.IconLocation = $installedExe
    $shortcut.Save()

    $startMenuDir = Join-Path ([Environment]::GetFolderPath("Programs")) "Primo Auto Eletrica"
    New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null
    $startShortcut = Join-Path $startMenuDir "Primo Auto Eletrica.lnk"
    $shortcut = $shell.CreateShortcut($startShortcut)
    $shortcut.TargetPath = $installedExe
    $shortcut.WorkingDirectory = $InstallDirectory
    $shortcut.IconLocation = $installedExe
    $shortcut.Save()
}

[PSCustomObject]@{
    InstalledExe = $installedExe
    InstallDirectory = $InstallDirectory
    DataDirectory = $DataDirectory
    ShortcutsCreated = -not [bool]$NoShortcuts
}
'@

Set-Content -Path $installScriptPath -Value $installerScript -Encoding UTF8

$updateWrapper = @'
[CmdletBinding()]
param(
    [string]$InstallDirectory = (Join-Path $env:ProgramFiles "PrimoAutoEletrica"),
    [string]$AppDataDirectory = (Join-Path $env:LOCALAPPDATA "PrimoAutoEletrica"),
    [switch]$ForceStop
)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoUpdateScript = Join-Path $scriptRoot "Scripts\Invoke-AppUpdate.ps1"
if (-not (Test-Path $repoUpdateScript)) {
    $repoUpdateScript = Join-Path $scriptRoot "Invoke-AppUpdate.ps1"
}

if (-not (Test-Path $repoUpdateScript)) {
    throw "Script Invoke-AppUpdate.ps1 nao encontrado no pacote."
}

& $repoUpdateScript -PackageDirectory $scriptRoot -InstallDirectory $InstallDirectory -AppDataDirectory $AppDataDirectory -ForceStop:$ForceStop
'@
Set-Content -Path $updateScriptPath -Value $updateWrapper -Encoding UTF8

Copy-Item -LiteralPath (Join-Path $scriptRoot "Invoke-AppUpdate.ps1") -Destination (Join-Path $packageRoot "Invoke-AppUpdate.ps1") -Force

$readme = @(
    "PRIMO AUTO ELETRICA - PACOTE INSTALAVEL WINDOWS",
    "",
    "Instalacao:",
    "1. Abra PowerShell como administrador quando instalar em Program Files.",
    "2. Execute: powershell -ExecutionPolicy Bypass -File .\Install-PrimoAutoEletrica.ps1",
    "3. O instalador cria pasta do programa, pasta de dados e atalhos.",
    "",
    "Atualizacao:",
    "1. Feche o Primo Auto Eletrica em todas as estacoes.",
    "2. Execute: powershell -ExecutionPolicy Bypass -File .\Update-PrimoAutoEletrica.ps1",
    "3. A rotina cria backup antes de copiar a nova versao e grava log em ProgramData.",
    "",
    "Requisito .NET:",
    "Pacote framework-dependent exige Microsoft.WindowsDesktop.App 9.x. Use -SelfContained ao gerar o pacote para embutir runtime."
)
Set-Content -Path $readmePath -Value $readme -Encoding UTF8

$manifest = @(
    "PRIMO AUTO ELETRICA - INSTALLER MANIFEST",
    "GeneratedAt: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")",
    "ProjectRoot: $projectRoot",
    "Configuration: $Configuration",
    "Framework: $Framework",
    "Runtime: $Runtime",
    "SelfContained: $SelfContained",
    "PackageRoot: $packageRoot",
    "PayloadExe: $(Join-Path $payloadRoot "PrimoAutoEletrica.exe")",
    "InstallScript: $installScriptPath",
    "UpdateScript: $updateScriptPath"
)
Set-Content -Path $manifestPath -Value $manifest -Encoding UTF8

$zipPath = "$packageRoot.zip"
if (-not $SkipZip) {
    if (Test-Path $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $zipPath -Force
}

[PSCustomObject]@{
    PackageRoot = $packageRoot
    ZipPath = if ($SkipZip) { "" } else { $zipPath }
    ManifestPath = $manifestPath
    InstallScriptPath = $installScriptPath
    UpdateScriptPath = $updateScriptPath
}
