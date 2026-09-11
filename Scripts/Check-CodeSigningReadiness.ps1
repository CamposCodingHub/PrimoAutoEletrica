<#
.SYNOPSIS
  Wrapper de readiness Authenticode — delega a Sign-PRIMOX.ps1 -Mode Readiness.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$sign = Join-Path $PSScriptRoot "Sign-PRIMOX.ps1"
if (-not (Test-Path -LiteralPath $sign)) {
    Write-Host "RESULT: BLOCKED"
    Write-Host "Sign-PRIMOX.ps1 ABSENT"
    exit 2
}

& $sign -Mode Readiness
exit $LASTEXITCODE
