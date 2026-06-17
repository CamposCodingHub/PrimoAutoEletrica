[CmdletBinding()]
param(
    [string]$PackageRoot,
    [switch]$RequirePackage
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$outputDir = Join-Path $projectRoot "TestResults\PhysicalHomologation\CleanWindows\$timestamp"
$summaryPath = Join-Path $outputDir "clean-windows-summary.json"
$reportPath = Join-Path $outputDir "clean-windows-report.md"

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

function New-Check {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Details
    )

    [ordered]@{
        Name = $Name
        Status = if ($Passed) { "APROVADO" } else { "FALHOU" }
        Details = $Details
    }
}

$checks = New-Object System.Collections.Generic.List[object]
$os = Get-CimInstance Win32_OperatingSystem
$isWindows10OrNewer = [version]$os.Version -ge [version]"10.0"
$checks.Add((New-Check "WindowsVersion" $isWindows10OrNewer "$($os.Caption) $($os.Version)"))

$dotnetInfo = ""
$dotnetExitCode = 1
try {
    $dotnetInfo = (& dotnet --info 2>&1) -join [Environment]::NewLine
    $dotnetExitCode = $LASTEXITCODE
}
catch {
    $dotnetInfo = $_.Exception.Message
}

$hasDotnet = $dotnetExitCode -eq 0
$hasNet9Windows = $dotnetInfo -match "Microsoft\.WindowsDesktop\.App 9\."
$checks.Add((New-Check "DotNetInstalled" $hasDotnet "dotnet --info exit code: $dotnetExitCode"))
$checks.Add((New-Check "WindowsDesktopRuntime9" $hasNet9Windows "Requer Microsoft.WindowsDesktop.App 9.x para WPF net9.0-windows."))

$executionPolicy = Get-ExecutionPolicy -Scope Process
$checks.Add((New-Check "PowerShellAvailable" $true "ExecutionPolicy(Process)=$executionPolicy"))

if (-not [string]::IsNullOrWhiteSpace($PackageRoot)) {
    $installScript = Join-Path $PackageRoot "Install-PrimoAutoEletrica.ps1"
    $manifest = Join-Path $PackageRoot "INSTALLER_MANIFEST.txt"
    $exe = Join-Path $PackageRoot "PrimoAutoEletrica\PrimoAutoEletrica.exe"

    $checks.Add((New-Check "PackageRootExists" (Test-Path $PackageRoot) $PackageRoot))
    $checks.Add((New-Check "InstallScriptExists" (Test-Path $installScript) $installScript))
    $checks.Add((New-Check "ManifestExists" (Test-Path $manifest) $manifest))
    $checks.Add((New-Check "ExecutableExists" (Test-Path $exe) $exe))
}
elseif ($RequirePackage) {
    $checks.Add((New-Check "PackageRootProvided" $false "Informe -PackageRoot apontando para o pacote gerado em uma maquina limpa."))
}

$overall = if (@($checks | Where-Object { $_.Status -eq "FALHOU" }).Count -eq 0) { "APROVADO" } else { "REPROVADO" }

$summary = [ordered]@{
    GeneratedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    MachineName = $env:COMPUTERNAME
    Overall = $overall
    OS = [ordered]@{
        Caption = $os.Caption
        Version = $os.Version
        Architecture = $os.OSArchitecture
    }
    Checks = $checks
    DotNetInfoPath = Join-Path $outputDir "dotnet-info.txt"
}

$dotnetInfo | Set-Content -Path $summary.DotNetInfoPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 6 | Set-Content -Path $summaryPath -Encoding UTF8

@(
    "# Homologacao Windows limpo"
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
    "- dotnet --info: $($summary.DotNetInfoPath)"
) | Set-Content -Path $reportPath -Encoding UTF8

[PSCustomObject]@{
    Status = $overall
    Summary = $summaryPath
    Report = $reportPath
}
