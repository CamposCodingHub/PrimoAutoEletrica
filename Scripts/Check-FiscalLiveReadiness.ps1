<#
.SYNOPSIS
  Verifica pre-requisitos externos para homologacao live Focus NFe.
  Nunca imprime token, certificado ou senha — apenas PRESENT/ABSENT.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$configDir = Join-Path $env:LOCALAPPDATA "PrimoAutoEletrica\Config"
$configPath = Join-Path $configDir "fiscal-foundation.json"
$dpapiPath = Join-Path $configDir "fiscal-secrets.dpapi"

Write-Host "PRIMOX Fiscal Live Readiness Check"
Write-Host ("Timestamp: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"))
Write-Host "Provider: Focus NFe"
Write-Host "Environment target: Homologacao"
Write-Host ""

$tokenEnv = [bool]$env:PRIMOX_FOCUS_HOMOLOG_TOKEN
Write-Host ("Token env PRIMOX_FOCUS_HOMOLOG_TOKEN: {0}" -f $(if ($tokenEnv) { "PRESENT" } else { "ABSENT" }))
Write-Host ("Token DPAPI file: {0}" -f $(if (Test-Path $dpapiPath) { "PRESENT" } else { "ABSENT" }))

$liveHttp = $false
$issuerComplete = $false
if (Test-Path $configPath) {
    Write-Host "Config file: PRESENT"
    $j = Get-Content -Raw -LiteralPath $configPath | ConvertFrom-Json
    $liveHttp = [bool]$j.liveHttpEnabled
    Write-Host ("LiveHttpEnabled: {0}" -f $liveHttp)
    Write-Host ("Environment enum: {0}" -f $j.environment)
    Write-Host ("HomologationBaseUrl: {0}" -f $j.homologationBaseUrl)
    Write-Host ("ProductionBaseUrl: {0}" -f $(if ([string]::IsNullOrWhiteSpace([string]$j.productionBaseUrl)) { "(empty)" } else { "SET" }))
    Write-Host ("ProductionUnlocked: {0}" -f $j.productionUnlocked)

    $issuer = $j.issuer
    $required = @("cnpj","razaoSocial","inscricaoEstadual","logradouro","numero","bairro","municipio","codigoMunicipioIbge","uf","cep","serieNFe","regimeTributario")
    $missing = @()
    foreach ($k in $required) {
        $val = [string]$issuer.$k
        $ok = -not [string]::IsNullOrWhiteSpace($val)
        Write-Host ("Issuer.{0}: {1}" -f $k, $(if ($ok) { "PRESENT" } else { "EMPTY" }))
        if (-not $ok) { $missing += $k }
    }
    $issuerComplete = ($missing.Count -eq 0)
} else {
    Write-Host "Config file: ABSENT"
}

$sw = [Diagnostics.Stopwatch]::StartNew()
$net = "FAIL"
try {
    Invoke-WebRequest -Uri "https://homologacao.focusnfe.com.br/v2/" -Method GET -TimeoutSec 20 -UseBasicParsing | Out-Null
    $net = "REACHABLE"
} catch {
    $code = $null
    if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
    if ($code -in 401, 403, 404, 405) { $net = "REACHABLE" } else { $net = "FAIL" }
}
Write-Host ("Homolog TLS/network: {0} ({1} ms) - HTTP auth NOT attempted without token" -f $net, $sw.ElapsedMilliseconds)

$canLive = $tokenEnv -or (Test-Path $dpapiPath)
$ready = $canLive -and $liveHttp -and $issuerComplete -and ($net -eq "REACHABLE")

Write-Host ""
if ($ready) {
    Write-Host "RESULT: READY_FOR_LIVE_ATTEMPT"
    Write-Host "Next: PDV -> NF-e Homologacao on a controlled sale (do not invent PASS)."
    exit 0
}

Write-Host "RESULT: BLOCKED"
Write-Host "O que falta:"
if (-not $canLive) { Write-Host "  - Token homolog Focus (env PRIMOX_FOCUS_HOMOLOG_TOKEN ou DPAPI via SaveHomologationToken)" }
if (-not $liveHttp) { Write-Host "  - LiveHttpEnabled=true em fiscal-foundation.json (somente Homologacao)" }
if (-not $issuerComplete) { Write-Host "  - Dados do emitente completos em Operacoes Fiscais / fiscal-foundation.json" }
if ($net -ne "REACHABLE") { Write-Host "  - Rede/TLS ate homologacao.focusnfe.com.br" }
Write-Host "Como fornecer: painel Focus NFe (token empresa) + preencher emitente + opt-in LiveHttpEnabled."
Write-Host "Producao permanece BLOCKED por design."
exit 2
