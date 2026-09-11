<#
.SYNOPSIS
  PRIMOX Workshop — Authenticode commercial signing (READINESS / SIGN).

.DESCRIPTION
  Nunca usa certificado localhost como comercial.
  Nunca imprime thumbprint completo, senha, PFX ou chave privada.
  Env:
    PRIMOX_CODESIGN_THUMBPRINT     (obrigatorio para SIGN)
    PRIMOX_CODESIGN_TIMESTAMP_URL  (opcional; default DigiCert RFC 3161)

.EXAMPLE
  .\Scripts\Sign-PRIMOX.ps1 -Mode Readiness
.EXAMPLE
  .\Scripts\Sign-PRIMOX.ps1 -Mode Sign -Path .\artifacts\publish\win-x64\PrimoAutoEletrica.exe
#>
[CmdletBinding()]
param(
    [ValidateSet("Readiness", "Sign", "Verify")]
    [string]$Mode = "Readiness",

    [string]$Path = "",

    [string]$PublishDir = "",

    [string]$SetupPath = "",

    [switch]$StrictFailOnBlocked
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$script:ResultCode = 0
$script:Decision = "UNKNOWN"

function Write-SignLog {
    param([string]$Message, [string]$Level = "INFO")
    Write-Host ("[{0}] {1}" -f $Level, $Message)
}

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

function Get-TimestampUrl {
    if (-not [string]::IsNullOrWhiteSpace($env:PRIMOX_CODESIGN_TIMESTAMP_URL)) {
        return $env:PRIMOX_CODESIGN_TIMESTAMP_URL.Trim()
    }
    return "http://timestamp.digicert.com"
}

function Test-IsCommercialCodeSigningCertificate {
    param([System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate)
    if (-not $Certificate -or -not $Certificate.HasPrivateKey) { return $false }
    if ($Certificate.NotAfter -lt (Get-Date)) { return $false }
    $subject = [string]$Certificate.Subject
    if ([string]::IsNullOrWhiteSpace($subject)) { $subject = "" }
    if ($subject -match '(?i)CN=localhost') { return $false }
    if ($subject -match '(?i)\b(TEST|DEV|DEVELOPMENT|SELF[- ]?SIGNED)\b' -and $subject -notmatch '(?i)CamposCodingHub|PRIMOX') {
        # Soft signal only — EKU still required below
    }
    if ($null -eq $Certificate.EnhancedKeyUsageList) { return $false }
    foreach ($eku in @($Certificate.EnhancedKeyUsageList)) {
        $name = [string]$eku.FriendlyName
        if ($name -eq "Code Signing" -or $name -eq "Assinatura de Codigo" -or $name -eq "Assinatura de Código") {
            return $true
        }
        $oid = $null
        try { $oid = [string]$eku.Value } catch { $oid = $null }
        if ($oid -eq "1.3.6.1.5.5.7.3.3") { return $true }
    }
    return $false
}

function Get-ThumbprintConfigured {
    $thumb = $env:PRIMOX_CODESIGN_THUMBPRINT
    if ([string]::IsNullOrWhiteSpace($thumb)) { return $null }
    return ($thumb -replace '\s', '').ToUpperInvariant()
}

function Find-CommercialCertificate {
    param([string]$Thumbprint)
    $all = @(Get-ChildItem Cert:\CurrentUser\My, Cert:\LocalMachine\My -ErrorAction SilentlyContinue | Where-Object { $_.HasPrivateKey })
    if (-not [string]::IsNullOrWhiteSpace($Thumbprint)) {
        return $all | Where-Object { $_.Thumbprint.ToUpperInvariant() -eq $Thumbprint } | Select-Object -First 1
    }
    return $null
}

function Get-SubjectShort {
    param([System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate)
    $s = [string]$Certificate.Subject
    if ($s -match 'CN=([^,]+)') { return $Matches[1] }
    return $s
}

function Invoke-Readiness {
    Write-SignLog "=== PRIMOX Sign-PRIMOX READINESS ==="
    Write-SignLog ("Timestamp local: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"))

    $signtool = Find-SignTool
    $signtoolState = if ($signtool) { "READY" } else { "BLOCKED" }
    Write-SignLog ("SignTool: {0}" -f $signtoolState)
    if ($signtool) {
        Write-SignLog ("SignTool path: present (Windows Kit x64; path not required for audit)")
    }

    $tsUrl = Get-TimestampUrl
    $tsConfigured = -not [string]::IsNullOrWhiteSpace($tsUrl)
    Write-SignLog ("Timestamp RFC3161 URL: {0}" -f $(if ($tsConfigured) { "READY (configured/default)" } else { "BLOCKED" }))
    # Nao imprimir URL completa em logs se vier de env custom — mas DigiCert default e publico
    if ($tsUrl -eq "http://timestamp.digicert.com") {
        Write-SignLog "Timestamp provider: DigiCert (default)"
    }
    else {
        Write-SignLog "Timestamp provider: custom via PRIMOX_CODESIGN_TIMESTAMP_URL (value not echoed)"
    }

    $thumbPresent = -not [string]::IsNullOrWhiteSpace((Get-ThumbprintConfigured))
    Write-SignLog ("PRIMOX_CODESIGN_THUMBPRINT: {0}" -f $(if ($thumbPresent) { "PRESENT" } else { "ABSENT" }))

    $storeCerts = @(Get-ChildItem Cert:\CurrentUser\My, Cert:\LocalMachine\My -ErrorAction SilentlyContinue | Where-Object { $_.HasPrivateKey })
    Write-SignLog ("Store certs with private key: {0}" -f $storeCerts.Count)

    $commercialCandidates = @()
    foreach ($c in $storeCerts) {
        $short = Get-SubjectShort -Certificate $c
        $isLocalhost = $short -match '(?i)^localhost$'
        $isCode = Test-IsCommercialCodeSigningCertificate -Certificate $c
        Write-SignLog ("Cert: Subject={0}; Expires={1:yyyy-MM-dd}; CodeSigningEKU={2}; LocalhostDev={3}; CommercialEligible={4}" -f `
            $short, $c.NotAfter, $isCode, $isLocalhost, ($isCode -and -not $isLocalhost))
        if ($isCode -and -not $isLocalhost) {
            $commercialCandidates += $c
        }
    }

    $commercialAvailable = $commercialCandidates.Count -gt 0
    $thumb = Get-ThumbprintConfigured
    $matched = $null
    if ($thumb) {
        $matched = Find-CommercialCertificate -Thumbprint $thumb
    }

    $certValidation = "BLOCKED"
    if ($matched -and (Test-IsCommercialCodeSigningCertificate -Certificate $matched)) {
        $certValidation = "PASS"
    }
    elseif ($matched) {
        $certValidation = "BLOCKED_NOT_COMMERCIAL"
        Write-SignLog "Configured thumbprint found but certificate is NOT commercial Code Signing (localhost/dev rejected)." "WARN"
    }
    elseif ($thumbPresent) {
        $certValidation = "BLOCKED_THUMBPRINT_NOT_IN_STORE"
        Write-SignLog "Thumbprint env set but certificate not found in CurrentUser/LocalMachine My." "WARN"
    }

    Write-SignLog ("Commercial certificate eligible in store: {0}" -f $(if ($commercialAvailable) { "AVAILABLE" } else { "BLOCKED" }))
    Write-SignLog ("Certificate validation vs env: {0}" -f $certValidation)

    $readyToSign = ($signtoolState -eq "READY") -and ($certValidation -eq "PASS") -and $tsConfigured

    if ($readyToSign) {
        $script:Decision = "READY_TO_SIGN"
        Write-SignLog "RESULT: READY_TO_SIGN"
        Write-SignLog "Trusted chain: UNKNOWN until signtool verify after SIGN"
        $script:ResultCode = 0
    }
    else {
        $script:Decision = "BLOCKED_BY_EXTERNAL_CERTIFICATE"
        Write-SignLog "RESULT: BLOCKED"
        Write-SignLog "CODE SIGNING = BLOCKED BY EXTERNAL CERTIFICATE"
        Write-SignLog "Provide: Authenticode OV/EV in store + PRIMOX_CODESIGN_THUMBPRINT. Never use localhost as commercial."
        $script:ResultCode = 2
        if ($StrictFailOnBlocked) { $script:ResultCode = 2 }
    }

    # Machine-readable one-liner for CI (no secrets)
    Write-SignLog ("SUMMARY SignTool={0}; Cert={1}; CertValidation={2}; Timestamp={3}; Decision={4}" -f `
        $signtoolState,
        $(if ($commercialAvailable) { "AVAILABLE" } else { "BLOCKED" }),
        $certValidation,
        $(if ($tsConfigured) { "READY" } else { "BLOCKED" }),
        $script:Decision)
}

function Invoke-VerifyFile {
    param([Parameter(Mandatory = $true)][string]$FilePath)

    if (-not (Test-Path -LiteralPath $FilePath)) {
        Write-SignLog ("Verify FAIL: file missing") "ERROR"
        return "ABSENT"
    }

    $sig = Get-AuthenticodeSignature -FilePath $FilePath
    $signer = if ($sig.SignerCertificate) { Get-SubjectShort -Certificate $sig.SignerCertificate } else { "None" }
    $ts = if ($sig.TimeStamperCertificate) { "PRESENT" } else { "ABSENT" }
    Write-SignLog ("Verify {0}: Status={1}; Signer={2}; TimestampCert={3}" -f (Split-Path -Leaf $FilePath), $sig.Status, $signer, $ts)

    $signtool = Find-SignTool
    if ($signtool -and $sig.Status -eq "Valid") {
        & $signtool verify /pa $FilePath | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-SignLog ("signtool verify failed exit={0}" -f $LASTEXITCODE) "ERROR"
            return "FAILED_VERIFY"
        }
    }

    if ($sig.Status -eq "Valid") { return "VALID" }
    if ($sig.Status -eq "NotSigned") { return "NOT_SIGNED" }
    return ("STATUS_" + [string]$sig.Status)
}

function Invoke-SignFile {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [string]$Label = "artifact"
    )

    if (-not (Test-Path -LiteralPath $FilePath)) {
        Write-SignLog ("Sign skip ({0}): file missing" -f $Label) "ERROR"
        return "ABSENT_FILE"
    }

    $thumb = Get-ThumbprintConfigured
    if ([string]::IsNullOrWhiteSpace($thumb)) {
        Write-SignLog ("CODE SIGNING BLOCKED ({0}): PRIMOX_CODESIGN_THUMBPRINT ABSENT - not simulating signature." -f $Label)
        return "BLOCKED_NO_THUMBPRINT"
    }

    $cert = Find-CommercialCertificate -Thumbprint $thumb
    if (-not $cert) {
        Write-SignLog ("CODE SIGNING FAILED ({0}): thumbprint not found in store." -f $Label) "ERROR"
        return "FAILED_CERT_NOT_FOUND"
    }
    if (-not (Test-IsCommercialCodeSigningCertificate -Certificate $cert)) {
        Write-SignLog ("CODE SIGNING BLOCKED ({0}): certificate is not valid commercial Code Signing (localhost/dev rejected)." -f $Label)
        return "BLOCKED_NOT_COMMERCIAL"
    }

    $signtool = Find-SignTool
    if (-not $signtool) {
        Write-SignLog ("CODE SIGNING FAILED ({0}): signtool.exe absent." -f $Label) "ERROR"
        return "FAILED_NO_SIGNTOOL"
    }

    $timestampUrl = Get-TimestampUrl
    Write-SignLog ("Signing {0} (thumbprint configured; value not printed)..." -f $Label)
    & $signtool sign /fd SHA256 /td SHA256 /tr $timestampUrl /sha1 $thumb /v $FilePath
    if ($LASTEXITCODE -ne 0) {
        Write-SignLog ("CODE SIGNING FAILED ({0}): signtool exit={1}" -f $Label, $LASTEXITCODE) "ERROR"
        return "FAILED_SIGNTOOL"
    }

    $verify = Invoke-VerifyFile -FilePath $FilePath
    if ($verify -ne "VALID") {
        Write-SignLog ("CODE SIGNING VERIFY FAILED ({0}): {1}" -f $Label, $verify) "ERROR"
        return "FAILED_VERIFY"
    }

    Write-SignLog ("CODE SIGNING VERIFIED ({0})" -f $Label)
    return "VERIFIED"
}

function Get-DefaultSignTargets {
    param(
        [string]$PublishDirectory,
        [string]$Setup
    )
    $targets = [System.Collections.Generic.List[string]]::new()
    # Ordem comercial: 1) EXE principal (publicado) 2) Setup (apos Inno)
    # Nao assinar DLLs do runtime .NET self-contained nem createdump.exe (nao sao superficie comercial primaria).
    if (-not [string]::IsNullOrWhiteSpace($PublishDirectory)) {
        $exe = Join-Path $PublishDirectory "PrimoAutoEletrica.exe"
        if (Test-Path -LiteralPath $exe) { $targets.Add($exe) }
    }
    if (-not [string]::IsNullOrWhiteSpace($Setup) -and (Test-Path -LiteralPath $Setup)) {
        $targets.Add($Setup)
    }
    return @($targets)
}

switch ($Mode) {
    "Readiness" {
        Invoke-Readiness
        exit $script:ResultCode
    }
    "Verify" {
        if ([string]::IsNullOrWhiteSpace($Path)) {
            throw "Verify requires -Path"
        }
        $v = Invoke-VerifyFile -FilePath $Path
        if ($v -eq "VALID") { exit 0 }
        if ($v -eq "NOT_SIGNED") { exit 2 }
        exit 1
    }
    "Sign" {
        # Pre-flight readiness (does not exit)
        Invoke-Readiness
        if ($script:Decision -ne "READY_TO_SIGN") {
            Write-SignLog "SIGN aborted: readiness not READY_TO_SIGN. No files modified." "WARN"
            exit 2
        }

        $files = [System.Collections.Generic.List[string]]::new()
        if (-not [string]::IsNullOrWhiteSpace($Path)) {
            $files.Add($Path)
        }
        else {
            foreach ($t in (Get-DefaultSignTargets -PublishDirectory $PublishDir -Setup $SetupPath)) {
                $files.Add($t)
            }
        }

        if ($files.Count -eq 0) {
            throw "Sign requires -Path and/or -PublishDir / -SetupPath with existing files."
        }

        $failed = $false
        foreach ($f in $files) {
            $status = Invoke-SignFile -FilePath $f -Label (Split-Path -Leaf $f)
            if ($status -ne "VERIFIED") {
                $failed = $true
                Write-SignLog ("Sign result {0}: {1}" -f (Split-Path -Leaf $f), $status) "ERROR"
            }
        }
        if ($failed) { exit 1 }
        Write-SignLog "SIGN complete: all targets VERIFIED"
        exit 0
    }
}
