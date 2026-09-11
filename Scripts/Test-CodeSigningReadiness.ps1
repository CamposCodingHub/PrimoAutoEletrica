<#
.SYNOPSIS
  COMMERCIAL-09 — valida readiness Authenticode sem assinar e sem alterar arquivos.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$reportDir = Join-Path $repoRoot "TestResults\Commercial09"
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportPath = Join-Path $reportDir "codesign-readiness-$stamp.md"

$signScript = Join-Path $PSScriptRoot "Sign-PRIMOX.ps1"
$exe = Join-Path $repoRoot "artifacts\publish\win-x64\PrimoAutoEletrica.exe"
$setup = Join-Path $repoRoot "artifacts\installer\PRIMOX-Workshop-Setup-1.0.0.exe"

$lines = New-Object System.Collections.Generic.List[string]
function Add-Line([string]$t) { $script:lines.Add($t); Write-Host $t }

Add-Line "# COMMERCIAL-09 Code Signing Readiness $stamp"
Add-Line ""
Add-Line "## Pre-conditions"
Add-Line ("- Sign-PRIMOX.ps1 exists: {0}" -f (Test-Path $signScript))
Add-Line ("- Publish EXE exists: {0}" -f (Test-Path $exe))
Add-Line ("- Setup exists: {0}" -f (Test-Path $setup))
Add-Line ""

# Snapshot hashes before readiness (must be unchanged)
$hashBefore = @{}
foreach ($f in @($exe, $setup)) {
    if (Test-Path $f) {
        $hashBefore[$f] = (Get-FileHash -LiteralPath $f -Algorithm SHA256).Hash
    }
}

Add-Line "## Readiness output"
$readyOut = & $signScript -Mode Readiness 2>&1 | Out-String
$readyCode = $LASTEXITCODE
Add-Line '```'
Add-Line $readyOut.TrimEnd()
Add-Line '```'
Add-Line ("ExitCode: {0}" -f $readyCode)
Add-Line ""

# Verify artifacts remain NotSigned / unchanged
Add-Line "## Artifact signature status (must remain unsigned without commercial cert)"
foreach ($f in @($exe, $setup)) {
    if (-not (Test-Path $f)) {
        Add-Line ("- MISSING: {0}" -f $f)
        continue
    }
    $sig = Get-AuthenticodeSignature -FilePath $f
    $hashAfter = (Get-FileHash -LiteralPath $f -Algorithm SHA256).Hash
    $unchanged = ($hashBefore[$f] -eq $hashAfter)
    Add-Line ("- {0}: Status={1}; HashUnchanged={2}" -f (Split-Path $f -Leaf), $sig.Status, $unchanged)
}

Add-Line ""
$decision = if ($readyCode -eq 2) { "BLOCKED BY EXTERNAL CERTIFICATE" } elseif ($readyCode -eq 0) { "READY_TO_SIGN" } else { "UNEXPECTED" }
Add-Line ("## Decision: **{0}**" -f $decision)
Add-Line ""
Add-Line "No fake/localhost signing attempted. No files modified by readiness."

Set-Content -LiteralPath $reportPath -Value ($lines -join "`n") -Encoding UTF8
Write-Host "Report: $reportPath"

# Expected without commercial cert: exit 2
if ($readyCode -eq 2) { exit 0 }  # test harness: blocked-as-expected = success of readiness test
if ($readyCode -eq 0) { exit 0 }  # ready to sign
exit 1
