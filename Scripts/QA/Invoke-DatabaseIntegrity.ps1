<#
.SYNOPSIS
  FULL ASSURANCE-11 — SQLite integrity / FK / migration probe on isolated or PackagingE2E DB.
#>
[CmdletBinding()]
param(
    [string]$DbPath = "",
    [string]$OutDir = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not $OutDir) {
    $OutDir = Join-Path $repoRoot ("TestResults\FullAssurance11\{0}\Database" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
}
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$candidates = @()
if ($DbPath) { $candidates += $DbPath }
$candidates += @(
    (Join-Path $env:LOCALAPPDATA "PRIMOX-Workshop-DataTest-C08\primoauto.db"),
    (Join-Path $env:LOCALAPPDATA "PrimoAutoEletrica\primoauto.db")
)

$target = $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
$summary = Join-Path $OutDir "database-integrity.json"

if (-not $target) {
    $obj = [ordered]@{ Status = "BLOCKED"; Detail = "No SQLite DB found to probe"; Checked = $candidates }
    $obj | ConvertTo-Json -Depth 5 | Set-Content $summary -Encoding UTF8
    Write-Host "DATABASE=BLOCKED"
    exit 0
}

$tool = Join-Path $repoRoot "Scripts\tools\Commercial08DataSeed\Commercial08DataSeed.csproj"
$env:DOTNET_ROLL_FORWARD = "LatestMajor"
$verify = ""
if (Test-Path $tool) {
    $verify = & dotnet run --project $tool -c Release -- $target verify 2>&1 | Out-String
}

# Minimal integrity via seed tool seed path or sqlite through seed project
$integrity = "unknown"
$fk = "unknown"
$migrations = "unknown"
$tmpProj = Join-Path $OutDir "DbCheck"
New-Item -ItemType Directory -Force -Path $tmpProj | Out-Null
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup><PackageReference Include="Microsoft.Data.Sqlite" Version="7.0.20" /></ItemGroup>
</Project>
'@ | Set-Content (Join-Path $tmpProj "DbCheck.csproj")
@"
using Microsoft.Data.Sqlite;
var db = args[0];
using var c = new SqliteConnection("Data Source=" + db);
c.Open();
using (var cmd = c.CreateCommand()) { cmd.CommandText = "PRAGMA integrity_check;"; Console.WriteLine("integrity=" + cmd.ExecuteScalar()); }
using (var cmd = c.CreateCommand()) {
  cmd.CommandText = "PRAGMA foreign_key_check;";
  using var r = cmd.ExecuteReader();
  int n = 0; while (r.Read()) n++;
  Console.WriteLine("fk_violations=" + n);
}
try {
  using var cmd = c.CreateCommand();
  cmd.CommandText = "SELECT COUNT(*) FROM SchemaMigrations;";
  Console.WriteLine("migrations=" + cmd.ExecuteScalar());
} catch { Console.WriteLine("migrations=N/A"); }
"@ | Set-Content (Join-Path $tmpProj "Program.cs")

Push-Location $tmpProj
$out = & dotnet run -c Release -- $target 2>&1 | Out-String
Pop-Location

if ($out -match "integrity=(\S+)") { $integrity = $Matches[1] }
if ($out -match "fk_violations=(\d+)") { $fk = $Matches[1] }
if ($out -match "migrations=(\S+)") { $migrations = $Matches[1] }

$status = if ($integrity -eq "ok" -and $fk -eq "0") { "PASS" } else { "FAIL" }
$result = [ordered]@{
    Status = $status
    DbPath = $target
    Integrity = $integrity
    ForeignKeyViolations = $fk
    Migrations = $migrations
    SeedVerifySnippet = ($verify.Trim().Substring(0, [Math]::Min(400, $verify.Trim().Length)))
    Raw = ($out.Trim() -replace "`r?`n", " | ")
    GeneratedAt = (Get-Date).ToString("o")
}
$result | ConvertTo-Json -Depth 6 | Set-Content $summary -Encoding UTF8
Write-Host ("DATABASE={0} integrity={1} fk={2} migrations={3}" -f $status, $integrity, $fk, $migrations)
if ($status -ne "PASS") { exit 1 }
exit 0
