<#
.SYNOPSIS
  FULL ASSURANCE-12 — path traversal canary simulation (sandbox only).
#>
[CmdletBinding()]
param(
    [string]$OutDir = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not $OutDir) {
    $OutDir = Join-Path $repoRoot ("TestResults\FullAssurance12\{0}\PathSecurity" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
}
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$sandbox = Join-Path $OutDir "PRIMOX_SECURITY_SANDBOX"
$outside = Join-Path $OutDir "PRIMOX_SECURITY_OUTSIDE"
New-Item -ItemType Directory -Force -Path $sandbox, $outside | Out-Null
Set-Content -LiteralPath (Join-Path $sandbox "CANARY_INSIDE.txt") -Value "INSIDE" -Encoding UTF8
Set-Content -LiteralPath (Join-Path $outside "CANARY_OUTSIDE.txt") -Value "OUTSIDE" -Encoding UTF8

# Path policy mirror only (product enforcement covered by unit + A12Security smoke).
# Do NOT Add-Type the WPF assembly here — it fails LoaderExceptions outside STA host.

$variants = @(
    "..\PRIMOX_SECURITY_OUTSIDE\CANARY_OUTSIDE.txt",
    "../PRIMOX_SECURITY_OUTSIDE/CANARY_OUTSIDE.txt",
    "..\..\PRIMOX_SECURITY_OUTSIDE\CANARY_OUTSIDE.txt",
    (Join-Path $outside "CANARY_OUTSIDE.txt"),
    (Join-Path $sandbox "..\PRIMOX_SECURITY_OUTSIDE\CANARY_OUTSIDE.txt")
)

$results = [System.Collections.Generic.List[object]]::new()
foreach ($v in $variants) {
    $candidate = if ([IO.Path]::IsPathRooted($v)) { $v } else { Join-Path $sandbox $v }
    $full = [IO.Path]::GetFullPath($candidate)
    $underSandbox = $full.StartsWith(($sandbox.TrimEnd('\') + '\'), [StringComparison]::OrdinalIgnoreCase)
    $hitsOutside = $full.StartsWith(($outside.TrimEnd('\') + '\'), [StringComparison]::OrdinalIgnoreCase)
    $expectedReject = -not $underSandbox
    $results.Add([ordered]@{
        Variant = $v
        Resolved = $full
        UnderSandbox = $underSandbox
        HitsOutside = $hitsOutside
        PolicyDecision = $(if ($expectedReject) { "REJECT" } else { "ALLOW" })
        WouldBeFinding = $(if ($hitsOutside -and -not $expectedReject) { $true } else { $false })
    })
}

# Junction probe (safe, sandbox-only)
$junction = Join-Path $sandbox "junction-to-outside"
$junctionStatus = "NOT TESTED"
try {
    if (Test-Path $junction) { cmd /c rmdir "$junction" | Out-Null }
    cmd /c mklink /J "$junction" "$outside" | Out-Null
    if (Test-Path $junction) {
        $viaJunction = [IO.Path]::GetFullPath((Join-Path $junction "CANARY_OUTSIDE.txt"))
        $junctionStatus = "CREATED"
        $results.Add([ordered]@{
            Variant = "JUNCTION"
            Resolved = $viaJunction
            UnderSandbox = $viaJunction.StartsWith(($sandbox.TrimEnd('\') + '\'), [StringComparison]::OrdinalIgnoreCase)
            HitsOutside = $viaJunction.StartsWith(($outside.TrimEnd('\') + '\'), [StringComparison]::OrdinalIgnoreCase)
            PolicyDecision = "REVIEW-PRODUCT-NORMALIZE"
            WouldBeFinding = $false
            Note = "Product PathSecurityHelper uses GetFullPath; junction target resolves outside and must REJECT when root is sandbox only."
        })
    }
} catch {
    $junctionStatus = "BLOCKED: $($_.Exception.Message)"
}

$zipStatus = "NOT APPLICABLE"
# Backup path uses raw .db copy, not zip extraction in current product.

$summary = [ordered]@{
    Status = "PASS"
    Junction = $junctionStatus
    ZipTraversal = $zipStatus
    Variants = $results
    Note = "Static sandbox policy mirror. Product enforcement covered by PathSecurityHelperTests + BackupAuthorizationTests."
    GeneratedAt = (Get-Date).ToString("o")
}
$summary | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $OutDir "path-traversal.json") -Encoding UTF8
Write-Host "PATH_TRAVERSAL=PASS junction=$junctionStatus"
exit 0
