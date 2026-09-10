<#
.SYNOPSIS
  Auditoria de catalogo i18n: USED / UNUSED / MISSING_EN / MISSING_ES / DUPLICATE_CANDIDATE.
.DESCRIPTION
  Extrai chaves dos catalogs LocalizationService*.cs e cruza com usos em XAML/CS.
  Nao apaga chaves. Nao modifica codigo.
#>
[CmdletBinding()]
param(
    [string]$Root,
    [string]$OutDir
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Root)) { $Root = Split-Path -Parent $PSScriptRoot }
if ([string]::IsNullOrWhiteSpace($OutDir)) { $OutDir = Join-Path $Root 'TestResults\I18n' }
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

$svc = Join-Path $Root 'PrimoAutoEletrica\Services'
$catalogFiles = @(
    'LocalizationService.cs',
    'LocalizationService.Modules.cs',
    'LocalizationService.Interaction.cs',
    'LocalizationService.Content.cs',
    'LocalizationService.Closure.cs'
) | ForEach-Object { Join-Path $svc $_ } | Where-Object { Test-Path $_ }

function Get-KeysFromSection([string]$text, [string]$marker) {
    $idx = $text.IndexOf($marker)
    if ($idx -lt 0) { return @() }
    $slice = $text.Substring($idx)
    # stop at next internal static Dictionary / private static Dictionary
    $nextInternal = $slice.IndexOf('internal static Dictionary', 10)
    $nextPrivate = $slice.IndexOf('private static Dictionary', 10)
    $next = -1
    if ($nextInternal -gt 0) { $next = $nextInternal }
    if ($nextPrivate -gt 0 -and ($next -lt 0 -or $nextPrivate -lt $next)) { $next = $nextPrivate }
    if ($next -gt 0) { $slice = $slice.Substring(0, $next) }
    return [regex]::Matches($slice, '\["([^"]+)"\]\s*=') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
}

$pt = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
$en = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
$es = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)

foreach ($cf in $catalogFiles) {
    $t = [IO.File]::ReadAllText($cf)
    foreach ($marker in @('Pt()', 'CatalogPt(', 'ModulesPt(', 'InteractionPt(', 'ContentPt(', 'ClosurePt(')) {
        foreach ($k in (Get-KeysFromSection $t $marker)) { [void]$pt.Add($k) }
    }
    foreach ($marker in @('En()', 'CatalogEn(', 'ModulesEn(', 'InteractionEn(', 'ContentEn(', 'ClosureEn(')) {
        foreach ($k in (Get-KeysFromSection $t $marker)) { [void]$en.Add($k) }
    }
    foreach ($marker in @('Es()', 'CatalogEs(', 'ModulesEs(', 'InteractionEs(', 'ContentEs(', 'ClosureEs(')) {
        foreach ($k in (Get-KeysFromSection $t $marker)) { [void]$es.Add($k) }
    }
}

# Fallback: if CatalogPt naming differs, harvest all keys in pt dictionaries by scanning BuildCatalog merge order files wholesale
if ($pt.Count -lt 50) {
    foreach ($cf in $catalogFiles) {
        $t = [IO.File]::ReadAllText($cf)
        foreach ($m in [regex]::Matches($t, '\["([^"]+)"\]\s*=')) { [void]$pt.Add($m.Groups[1].Value) }
    }
}

# Usages
$used = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
$codeRoot = Join-Path $Root 'PrimoAutoEletrica'
$codeFiles = Get-ChildItem $codeRoot -Recurse -Include *.xaml,*.cs |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Name -notmatch 'LocalizationService' }

foreach ($f in $codeFiles) {
    $c = [IO.File]::ReadAllText($f.FullName)
    foreach ($m in [regex]::Matches($c, 'Path=\[([^\]]+)\]')) { [void]$used.Add($m.Groups[1].Value) }
    foreach ($m in [regex]::Matches($c, 'UiText\.T\("([^"]+)"')) { [void]$used.Add($m.Groups[1].Value) }
    foreach ($m in [regex]::Matches($c, 'GetString\("([^"]+)"')) { [void]$used.Add($m.Groups[1].Value) }
    foreach ($m in [regex]::Matches($c, 'LocalizationHelper\}, Path=(\w+)')) { [void]$used.Add($m.Groups[1].Value) }
    foreach ($m in [regex]::Matches($c, '\.GetString\("([^"]+)"')) { [void]$used.Add($m.Groups[1].Value) }
}

# Re-parse catalogs more reliably: all three language blocks by method names used in repo
function Harvest-MethodKeys([string]$path, [string]$method) {
    $t = [IO.File]::ReadAllText($path)
    $m = [regex]::Match($t, "internal static Dictionary<string, string> $method\(\) => new\(\)\s*\{(?<body>.*?)\n\s*\};", [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $m.Success) {
        $m = [regex]::Match($t, "private static Dictionary<string, string> $method\(\).*?\{(?<body>.*?)\n\s*\}", [System.Text.RegularExpressions.RegexOptions]::Singleline)
    }
    if (-not $m.Success) { return @() }
    return [regex]::Matches($m.Groups['body'].Value, '\["([^"]+)"\]') | ForEach-Object { $_.Groups[1].Value }
}

$pt2 = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
$en2 = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
$es2 = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
$methodsPt = @('BuildPt','Pt','CatalogPt','ModulesPt','InteractionPt')
# Actual names from repo
foreach ($cf in $catalogFiles) {
    $raw = [IO.File]::ReadAllText($cf)
    # Match any *Pt() dictionary initializer
    foreach ($mm in [regex]::Matches($raw, 'static Dictionary<string, string> (\w*Pt)\(\) => new\(\)\s*\{(.*?)\n\s*\};', [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        foreach ($k in [regex]::Matches($mm.Groups[2].Value, '\["([^"]+)"\]')) { [void]$pt2.Add($k.Groups[1].Value) }
    }
    foreach ($mm in [regex]::Matches($raw, 'static Dictionary<string, string> (\w*En)\(\) => new\(\)\s*\{(.*?)\n\s*\};', [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        foreach ($k in [regex]::Matches($mm.Groups[2].Value, '\["([^"]+)"\]')) { [void]$en2.Add($k.Groups[1].Value) }
    }
    foreach ($mm in [regex]::Matches($raw, 'static Dictionary<string, string> (\w*Es)\(\) => new\(\)\s*\{(.*?)\n\s*\};', [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        foreach ($k in [regex]::Matches($mm.Groups[2].Value, '\["([^"]+)"\]')) { [void]$es2.Add($k.Groups[1].Value) }
    }
}

if ($pt2.Count -gt $pt.Count) { $pt = $pt2 }
if ($en2.Count -gt 0) { $en = $en2 }
if ($es2.Count -gt 0) { $es = $es2 }

# If en/es empty because naming differs (e.g. EnglishCatalog), fallback: keys present only if equal count in file
if ($en.Count -eq 0 -or $es.Count -eq 0) {
    # Assume BuildCatalog merges same keys — treat missing relative to pt
    $en = $pt
    $es = $pt
    $missingMode = 'ASSUMED_SAME_KEYS'
} else {
    $missingMode = 'COMPARED'
}

$unused = $pt | Where-Object { -not $used.Contains($_) } | Sort-Object
$usedKeys = $pt | Where-Object { $used.Contains($_) } | Sort-Object
$missingEn = $pt | Where-Object { -not $en.Contains($_) } | Sort-Object
$missingEs = $pt | Where-Object { -not $es.Contains($_) } | Sort-Object
$orphanUsed = $used | Where-Object { -not $pt.Contains($_) } | Sort-Object

# Duplicate candidates: same normalized value across different keys in pt file text
$dupes = @()
$ptValues = @{}
foreach ($cf in $catalogFiles) {
    $raw = [IO.File]::ReadAllText($cf)
    foreach ($mm in [regex]::Matches($raw, 'static Dictionary<string, string> (\w*Pt)\(\) => new\(\)\s*\{(.*?)\n\s*\};', [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        foreach ($pair in [regex]::Matches($mm.Groups[2].Value, '\["([^"]+)"\]\s*=\s*"([^"]*)"')) {
            $k = $pair.Groups[1].Value
            $v = $pair.Groups[2].Value.Trim().ToLowerInvariant()
            if ([string]::IsNullOrWhiteSpace($v)) { continue }
            if (-not $ptValues.ContainsKey($v)) { $ptValues[$v] = New-Object System.Collections.Generic.List[string] }
            $ptValues[$v].Add($k)
        }
    }
}
foreach ($kv in $ptValues.GetEnumerator()) {
    $uniq = $kv.Value | Select-Object -Unique
    if ($uniq.Count -ge 2) {
        $dupes += [ordered]@{ Value = $kv.Key; Keys = ($uniq -join ', ') }
    }
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$result = [ordered]@{
    GeneratedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    MissingMode = $missingMode
    PtKeys = $pt.Count
    EnKeys = $en.Count
    EsKeys = $es.Count
    Used = $usedKeys.Count
    Unused = $unused.Count
    MissingEn = @($missingEn).Count
    MissingEs = @($missingEs).Count
    OrphanUsedMissingInPt = @($orphanUsed).Count
    DuplicateCandidateGroups = $dupes.Count
    UnusedSample = @($unused | Select-Object -First 40)
    MissingEnSample = @($missingEn | Select-Object -First 40)
    MissingEsSample = @($missingEs | Select-Object -First 40)
    OrphanUsedSample = @($orphanUsed | Select-Object -First 40)
    DuplicateCandidatesSample = @($dupes | Select-Object -First 30)
}

$json = Join-Path $OutDir "i18n-catalog-audit-$stamp.json"
$result | ConvertTo-Json -Depth 6 | Set-Content $json -Encoding UTF8

Write-Host ("PT keys: {0} | EN: {1} | ES: {2}" -f $pt.Count, $en.Count, $es.Count)
Write-Host ("USED: {0} | UNUSED: {1}" -f $usedKeys.Count, $unused.Count)
Write-Host ("MISSING_EN: {0} | MISSING_ES: {1}" -f @($missingEn).Count, @($missingEs).Count)
Write-Host ("ORPHAN_USED: {0} | DUPLICATE_CANDIDATE groups: {1}" -f @($orphanUsed).Count, $dupes.Count)
Write-Host ("JSON: {0}" -f $json)
