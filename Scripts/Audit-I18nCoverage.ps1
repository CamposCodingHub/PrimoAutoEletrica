<#
.SYNOPSIS
  Auditoria de cobertura i18n (XAML literals vs LocalizationHelper).
.DESCRIPTION
  Conta atributos Text/Content/Header/ToolTip hardcoded vs bindings LocalizationHelper.
  Nao classifica semanticamente 100% das strings — produz metricas objetivas.
#>
[CmdletBinding()]
param(
    [string]$Root
)

if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Split-Path -Parent $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($Root)) {
        $Root = (Get-Location).Path
    }
}

$ErrorActionPreference = 'Stop'
$xamlRoot = Join-Path $Root 'PrimoAutoEletrica'
$files = Get-ChildItem $xamlRoot -Recurse -Filter *.xaml |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

$literal = 0
$bound = 0
$helperRefs = 0
$byFolder = @{}

foreach ($f in $files) {
    $c = [System.IO.File]::ReadAllText($f.FullName)
    $litCount = ([regex]::Matches($c, '(Text|Content|Header|ToolTip)="[^"{][^"]*"')).Count
    $bndCount = ([regex]::Matches($c, '(Text|Content|Header|ToolTip)="\{Binding[^"]*LocalizationHelper[^"]*"')).Count
    $refCount = ([regex]::Matches($c, 'LocalizationHelper')).Count
    $literal += $litCount
    $bound += $bndCount
    $helperRefs += $refCount

    $rel = $f.FullName.Substring($xamlRoot.Length).TrimStart('\')
    $folder = if ($rel -match '^(UserControls|Views)\\') { $Matches[1] } else { 'Other' }
    if (-not $byFolder.ContainsKey($folder)) {
        $byFolder[$folder] = [ordered]@{ Literals = 0; Bound = 0; Files = 0 }
    }
    $byFolder[$folder].Literals += $litCount
    $byFolder[$folder].Bound += $bndCount
    $byFolder[$folder].Files++
}

$totalUi = $literal + $bound
$pct = if ($totalUi -gt 0) { [math]::Round(100.0 * $bound / $totalUi, 1) } else { 0 }

$result = [ordered]@{
    GeneratedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    XamlFiles = $files.Count
    LiteralAttrs = $literal
    BoundAttrs = $bound
    LocalizationHelperRefs = $helperRefs
    CoveragePercentOfUiAttrs = $pct
    ByArea = $byFolder
}

$outDir = Join-Path $Root 'TestResults\I18n'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$jsonPath = Join-Path $outDir ("i18n-coverage-{0}.json" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
$result | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonPath -Encoding UTF8

Write-Host ("XAML files: {0}" -f $files.Count)
Write-Host ("Literal UI attrs: {0}" -f $literal)
Write-Host ("Bound LocalizationHelper attrs: {0}" -f $bound)
Write-Host ("LocalizationHelper refs: {0}" -f $helperRefs)
Write-Host ("Coverage (bound / (literal+bound)): {0}%" -f $pct)
Write-Host ("JSON: {0}" -f $jsonPath)
