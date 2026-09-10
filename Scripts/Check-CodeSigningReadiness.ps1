<#
.SYNOPSIS
  Detecta preparacao de code signing comercial. Nunca imprime chave privada, senha ou PFX.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "PRIMOX Code Signing Readiness"
Write-Host ("Timestamp: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"))

function Find-SignTool {
    $roots = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
        "${env:ProgramFiles}\Windows Kits\10\bin"
    )
    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root)) { continue }
        $hit = Get-ChildItem -LiteralPath $root -Recurse -Filter "signtool.exe" -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($hit) { return $hit.FullName }
    }
    return $null
}

$signtool = Find-SignTool
Write-Host ("signtool.exe: {0}" -f $(if ($signtool) { "PRESENT" } else { "ABSENT" }))
Write-Host ("PRIMOX_CODESIGN_THUMBPRINT env: {0}" -f $(if ([string]::IsNullOrWhiteSpace($env:PRIMOX_CODESIGN_THUMBPRINT)) { "ABSENT" } else { "PRESENT" }))

$commercial = @()
$all = Get-ChildItem Cert:\CurrentUser\My, Cert:\LocalMachine\My -ErrorAction SilentlyContinue | Where-Object { $_.HasPrivateKey }
Write-Host ("Private key certificates in store: {0}" -f @($all).Count)

foreach ($c in @($all)) {
    $ekuNames = @()
    if ($null -ne $c.EnhancedKeyUsageList) {
        foreach ($e in @($c.EnhancedKeyUsageList)) {
            if ($null -ne $e.FriendlyName) { $ekuNames += [string]$e.FriendlyName }
        }
    }
    $isCode = ($ekuNames -contains "Code Signing") -or ($ekuNames -contains "Assinatura de Código")
    $subjectShort = ($c.Subject -replace '^CN=','' -split ',')[0]
    $isLocalhost = $subjectShort -match '(?i)^localhost$'
    Write-Host ("Certificate: Subject={0}; Expiration={1:yyyy-MM-dd}; PrivateKey=AVAILABLE; CodeSigningEKU={2}; LocalhostDev={3}" -f `
        $subjectShort, $c.NotAfter, $isCode, $isLocalhost)
    if ($isCode -and -not $isLocalhost -and $c.NotAfter -gt (Get-Date)) {
        $commercial += $c
    }
}

if (@($commercial).Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($env:PRIMOX_CODESIGN_THUMBPRINT)) {
    Write-Host "RESULT: READY_TO_SIGN"
    Write-Host "Trusted chain: UNKNOWN until signtool verify after sign"
    exit 0
}

Write-Host "RESULT: BLOCKED"
Write-Host "Certificate (commercial code signing): ABSENT"
Write-Host "CODE SIGNING = BLOCKED BY EXTERNAL CERTIFICATE"
Write-Host "Como fornecer: adquirir Authenticode (OV/EV) + instalar no store + definir PRIMOX_CODESIGN_THUMBPRINT"
Write-Host "Nunca usar certificado localhost/teste como assinatura comercial."
exit 2
