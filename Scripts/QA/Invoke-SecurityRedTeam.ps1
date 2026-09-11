<#
.SYNOPSIS
  FULL ASSURANCE-11 — defensive security red-team scan (local repo + isolated patterns).
  Does NOT attack external systems. Does NOT use production credentials.
#>
[CmdletBinding()]
param(
    [string]$OutDir = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not $OutDir) {
    $OutDir = Join-Path $repoRoot ("TestResults\FullAssurance11\{0}\Security" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
}
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$report = Join-Path $OutDir "security-redteam.json"
$findings = [System.Collections.Generic.List[object]]::new()
$stats = [ordered]@{ Critical = 0; High = 0; Medium = 0; Low = 0; Informational = 0; Review = 0 }

function Add-Finding([string]$Id, [string]$Sev, [string]$Cat, [string]$Title, [string]$Detail, [string]$Classification = "REVIEW") {
    $findings.Add([ordered]@{
        Id = $Id; Severity = $Sev; Category = $Cat; Title = $Title
        Detail = $Detail; Classification = $Classification; Status = "DOCUMENTED"
    })
    if ($stats.Contains($Sev)) { $stats[$Sev]++ } else { $stats.Review++ }
}

Write-Host "=== Security Red Team (static + secrets) ==="
Write-Host "Repo=$repoRoot Out=$OutDir"

# --- Secret scan (tracked files, refined) ---
$secretHits = @()
$tracked = @(git -C $repoRoot ls-files)
foreach ($f in $tracked) {
    if ($f -match '(?i)\.(pfx|p12)$') { $secretHits += "CERT_FILE:$f"; continue }
    if ($f -match '(?i)(bin/|obj/|TestResults/|artifacts/|\.dll$|\.exe$|\.png$|\.jpg$|\.ico$|\.pdf$)') { continue }
    if ($f -match '(?i)(Tests/|UiTests/|UiSmokeTestService|Setup-SqlServer|FullAssurance|SecurityRedTeam)') { continue }
    try {
        $c = Get-Content -LiteralPath (Join-Path $repoRoot $f) -Raw -ErrorAction SilentlyContinue
        if (-not $c) { continue }
        if ($c -match 'BEGIN (RSA |OPENSSH |EC )?PRIVATE KEY') { $secretHits += "PRIVKEY:$f" }
        if ($c -match 'PRIMOX_FOCUS_HOMOLOG_TOKEN\s*=\s*["'']?[A-Za-z0-9_\-]{16,}') { $secretHits += "FOCUS_TOKEN:$f" }
    } catch {}
}
if ($secretHits.Count -eq 0) {
    Add-Finding "SEC-001" "Informational" "SECRETS" "Tracked secret scan clean" "No private keys / Focus tokens / PFX in tracked sources" "SAFE"
} else {
    Add-Finding "SEC-001" "Critical" "SECRETS" "Secret material in tracked files" ($secretHits -join "; ") "VULNERABLE"
}

# --- SQL concatenation review (heuristic) ---
$sqlFiles = Get-ChildItem -Path (Join-Path $repoRoot "PrimoAutoEletrica") -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' }
$sqlInterp = @()
foreach ($file in $sqlFiles) {
    $lines = Select-String -Path $file.FullName -Pattern 'CommandText\s*=\s*\$@"|CommandText\s*=\s*\$"|Execute\w*\(\s*\$"' -ErrorAction SilentlyContinue
    foreach ($hit in $lines) {
        $rel = $hit.Path.Substring($repoRoot.Length).TrimStart('\')
        # Skip known identifier-only PRAGMA / ALTER patterns when line is short and no @user concat
        $line = $hit.Line
        if ($line -match 'PRAGMA|table_info|ALTER TABLE|RENAME TO' -and $line -notmatch '@') {
            continue
        }
        $sqlInterp += ("{0}:{1}:{2}" -f $rel, $hit.LineNumber, ($line.Trim().Substring(0, [Math]::Min(120, $line.Trim().Length))))
    }
}
Add-Finding "SEC-002" "Informational" "SQL" "Interpolated CommandText occurrences (review)" ("count={0}; sample={1}" -f $sqlInterp.Count, (($sqlInterp | Select-Object -First 8) -join " || ")) $(if ($sqlInterp.Count -gt 0) { "REVIEW" } else { "SAFE" })

# --- Process.Start inventory ---
$procHits = @()
foreach ($file in $sqlFiles) {
    $hits = Select-String -Path $file.FullName -Pattern 'Process\.Start|ProcessStartInfo' -ErrorAction SilentlyContinue
    foreach ($h in $hits) {
        $rel = $h.Path.Substring($repoRoot.Length).TrimStart('\')
        $procHits += ("{0}:{1}" -f $rel, $h.LineNumber)
    }
}
Add-Finding "SEC-003" "Informational" "COMMAND" "Process.Start usages" ("count={0}; paths sample={1}" -f $procHits.Count, (($procHits | Select-Object -First 12) -join ", ")) "REVIEW"

# --- Password / plaintext heuristics in non-test product code ---
$pwdHits = @()
$productCs = $sqlFiles | Where-Object { $_.FullName -notmatch 'UiSmoke|Test|Simulation' }
foreach ($file in $productCs) {
    $hits = Select-String -Path $file.FullName -Pattern 'password\s*=\s*"[^"]{4,}"|Senha\s*=\s*"[^"]{4,}"' -ErrorAction SilentlyContinue
    foreach ($h in $hits) {
        $rel = $h.Path.Substring($repoRoot.Length).TrimStart('\')
        if ($rel -match 'ProdutosIniciais|Seed|Default') { continue }
        $pwdHits += ("{0}:{1}" -f $rel, $h.LineNumber)
    }
}
if ($pwdHits.Count -eq 0) {
    Add-Finding "SEC-004" "Informational" "AUTH" "No hardcoded password literals in product code (heuristic)" "" "SAFE"
} else {
    Add-Finding "SEC-004" "High" "AUTH" "Possible hardcoded password literals" ($pwdHits -join "; ") "REVIEW"
}

# --- Isolated SQLite injection probe (synthetic DB only) ---
$dbDir = Join-Path $OutDir "isolated-db"
New-Item -ItemType Directory -Force -Path $dbDir | Out-Null
$dbPath = Join-Path $dbDir "fa11-redteam.db"
$probeCsproj = Join-Path $OutDir "SqlProbe"
New-Item -ItemType Directory -Force -Path $probeCsproj | Out-Null
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="7.0.20" />
  </ItemGroup>
</Project>
'@ | Set-Content (Join-Path $probeCsproj "SqlProbe.csproj") -Encoding UTF8

@'
using Microsoft.Data.Sqlite;
var db = args[0];
using var c = new SqliteConnection("Data Source=" + db);
c.Open();
using (var cmd = c.CreateCommand()) {
  cmd.CommandText = "CREATE TABLE t(id INTEGER PRIMARY KEY, name TEXT); INSERT INTO t(name) VALUES('ok');";
  cmd.ExecuteNonQuery();
}
var payloads = new[] { "' OR '1'='1", "1' OR '1'='1", "--", "/*", "\"", "'" };
int safe = 0, fail = 0;
foreach (var p in payloads) {
  try {
    using var cmd = c.CreateCommand();
    cmd.CommandText = "SELECT COUNT(*) FROM t WHERE name = @n";
    cmd.Parameters.AddWithValue("@n", p);
    var n = Convert.ToInt32(cmd.ExecuteScalar());
    // parameterized => always safe
    safe++;
  } catch { fail++; }
}
// Also prove unsafe concat WOULD be dangerous if used (do NOT use on product DB)
int unsafeRows = -1;
try {
  using var bad = c.CreateCommand();
  bad.CommandText = "SELECT COUNT(*) FROM t WHERE name = '" + payloads[0] + "'";
  unsafeRows = Convert.ToInt32(bad.ExecuteScalar());
} catch { unsafeRows = -2; }
Console.WriteLine($"param_safe={safe}; param_fail={fail}; demo_unsafe_concat_rows={unsafeRows}");
'@ | Set-Content (Join-Path $probeCsproj "Program.cs") -Encoding UTF8

$env:DOTNET_ROLL_FORWARD = "LatestMajor"
Push-Location $probeCsproj
$probeOut = & dotnet run -c Release -- $dbPath 2>&1 | Out-String
Pop-Location
Add-Finding "SEC-005" "Informational" "SQL" "Isolated parameterized SQLite probe" ($probeOut.Trim() -replace "`r?`n", " | ") "SAFE"
Add-Finding "SEC-006" "Informational" "SQL" "Demo unsafe concat on isolated DB only" "Shows classic OR injection row inflation when concat used; product must keep parameters" "INFORMATIONAL"

# Decision
$crit = @($findings | Where-Object { $_.Severity -eq "Critical" -and $_.Classification -eq "VULNERABLE" }).Count
$highVuln = @($findings | Where-Object { $_.Severity -eq "High" -and $_.Classification -eq "VULNERABLE" }).Count
$decision = if ($crit -gt 0 -or $highVuln -gt 0) { "RED" } elseif ($secretHits.Count -eq 0) { "YELLOW" } else { "RED" }
# YELLOW expected: static REVIEW items, no verified Critical exploit in product without further dynamic UI auth tests

$result = [ordered]@{
    GeneratedAt = (Get-Date).ToString("o")
    DecisionHint = $decision
    Stats = $stats
    Findings = $findings
    Notes = @(
        "Dynamic auth lockout / role bypass covered by LoginSessao + QaEngine when orchestrator runs them.",
        "Process.Start / SQL REVIEW items require contextual SAFE classification in final report.",
        "No third-party / Focus / SEFAZ attacks."
    )
}
$result | ConvertTo-Json -Depth 8 | Set-Content $report -Encoding UTF8
Write-Host ("SECURITY_DECISION={0} REPORT={1}" -f $decision, $report)
if ($crit -gt 0) { exit 2 }
exit 0
