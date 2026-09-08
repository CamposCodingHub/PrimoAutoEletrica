<#
.SYNOPSIS
  Wrapper legado → pipeline oficial PRIMOX Commercial Release.

.DESCRIPTION
  LEGACY WRAPPER. Prefira: Scripts\Build-PrimoXCommercialRelease.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0",
    [switch]$SkipBuild,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$official = Join-Path (Split-Path -Parent $PSScriptRoot) "Scripts\Build-PrimoXCommercialRelease.ps1"
if (-not (Test-Path -LiteralPath $official)) {
    throw "Pipeline oficial nao encontrado: $official"
}

$argsList = @{
    Configuration = $Configuration
    Runtime = $Runtime
    Version = $Version
}
if ($SkipBuild) { $argsList.SkipClean = $true }
if ($SkipTests) { $argsList.SkipTests = $true }

Write-Host "Redirecionando para pipeline oficial: $official" -ForegroundColor Cyan
& $official @argsList
exit $LASTEXITCODE
