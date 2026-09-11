<#
.SYNOPSIS
  COMMERCIAL-10 validation entrypoint (wraps Run-Commercial10ReleaseGate.ps1).

.EXAMPLE
  .\Scripts\Test-Commercial10ReleaseGate.ps1 -SkipExhaustive -SkipInstaller
  .\Scripts\Test-Commercial10ReleaseGate.ps1
#>
[CmdletBinding()]
param(
    [switch]$SkipExhaustive,
    [switch]$SkipInstaller,
    [int]$InstallerCycles = 3
)

$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "Run-Commercial10ReleaseGate.ps1") `
    -SkipExhaustive:$SkipExhaustive `
    -SkipInstaller:$SkipInstaller `
    -InstallerCycles $InstallerCycles
exit $LASTEXITCODE
