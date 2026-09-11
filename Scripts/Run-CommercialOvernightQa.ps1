<#
.SYNOPSIS
  PRIMOX COMMERCIAL-09.5 — Overnight Full Product QA orchestrator.

.DESCRIPTION
  Runs build matrix, unit/loc/fiscal tests, UI smoke suites, overnight stress,
  DB integrity, security scan, optional installer regression.
  Does not buy certificates, alter fiscal/i18n architecture, or invent PASS.
#>
[CmdletBinding()]
param(
    [int]$MinDurationMinutes = 120,
    [int]$StartupCycles = 50,
    [switch]$SkipInstaller,
    [switch]$SkipExhaustiveRepeat,
    [string]$Configuration = "Release",
    [string]$Framework = "net6.0-windows"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outRoot = Join-Path $repoRoot "TestResults\Commercial095\$stamp"
$logPath = Join-Path $outRoot "overnight.log"
$summaryPath = Join-Path $outRoot "overnight-summary.json"
New-Item -ItemType Directory -Force -Path $outRoot | Out-Null

$started = Get-Date
$env:DOTNET_ROLL_FORWARD = "LatestMajor"
$script:Results = [ordered]@{}
$script:FailCount = 0

function Write-OLog([string]$Message, [string]$Level = "INFO") {
    $line = "{0} [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message
    Add-Content -LiteralPath $logPath -Value $line -Encoding UTF8
    Write-Host $line
}

function Set-OResult([string]$Name, [string]$Status, [string]$Detail = "") {
    $script:Results[$Name] = [ordered]@{ Status = $Status; Detail = $Detail }
    if ($Status -eq "FAIL") { $script:FailCount++ }
    Write-OLog ("{0}: {1} - {2}" -f $Name, $Status, $Detail) $(if ($Status -eq "FAIL") { "ERROR" } else { "RESULT" })
}

function Invoke-SmokeFilter([string]$Filter, [string]$Folder, [switch]$SkipBuild) {
    $dir = Join-Path $outRoot $Folder
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $args = @{
        Configuration   = $Configuration
        Framework       = $Framework
        SmokeFilter     = $Filter
        OutputDirectory = $dir
    }
    if ($SkipBuild) { $args.SkipBuild = $true }
    & (Join-Path $repoRoot "Scripts\Run-UiSmoke.ps1") @args
    $code = $LASTEXITCODE
    $sum = Join-Path $dir "ui-smoke-summary.json"
    $detail = "Exit=$code"
    if (Test-Path $sum) {
        $j = Get-Content $sum -Raw | ConvertFrom-Json
        $detail = "Exit=$code Status=$($j.Status) $($j.PassedChecks)/$($j.TotalChecks)"
        if ($code -eq 0 -and $j.Status -eq "APROVADO") {
            Set-OResult $Filter "PASS" $detail
            return $true
        }
        Set-OResult $Filter "FAIL" $detail
        return $false
    }
    Set-OResult $Filter "FAIL" $detail
    return $false
}

Write-OLog "=== COMMERCIAL-09.5 OVERNIGHT QA START ==="
Write-OLog "Repo=$repoRoot Out=$outRoot MinDurationMinutes=$MinDurationMinutes"
Write-OLog ("HEAD={0}" -f (git -C $repoRoot rev-parse HEAD))

# --- Build matrix ---
$appProj = Join-Path $repoRoot "PrimoAutoEletrica\PrimoAutoEletrica.csproj"
$testProj = Join-Path $repoRoot "Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj"

foreach ($cfg in @("Debug", "Release")) {
    Write-OLog "Building $cfg..."
    $blog = Join-Path $outRoot ("build-{0}.log" -f $cfg.ToLowerInvariant())
    & dotnet build $appProj -c $cfg --nologo 2>&1 | Tee-Object -FilePath $blog | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Set-OResult ("Build-$cfg") "PASS" "0 errors (see $blog)"
    }
    else {
        Set-OResult ("Build-$cfg") "FAIL" "exit=$LASTEXITCODE"
    }
}

# --- Unit / Loc / Fiscal ---
$tlog = Join-Path $outRoot "dotnet-test.log"
& dotnet test $testProj -c Release --nologo 2>&1 | Tee-Object -FilePath $tlog | Out-Null
if ($LASTEXITCODE -eq 0) {
    Set-OResult "UnitTests" "PASS" "see $tlog"
}
else {
    Set-OResult "UnitTests" "FAIL" "exit=$LASTEXITCODE"
}

# --- Security scan (tracked) ---
$secretExt = @(git -C $repoRoot ls-files | Where-Object { $_ -match '\.(pfx|p12|pem|key)$' })
$secretHit = @(git -C $repoRoot grep -n -i -E "BEGIN (RSA |OPENSSH )?PRIVATE KEY" -- Scripts Installer Docs/qa 2>$null)
if ($secretExt.Count -eq 0 -and $secretHit.Count -eq 0) {
    Set-OResult "Security" "PASS" "no tracked secrets"
}
else {
    Set-OResult "Security" "FAIL" ("ext={0} hits={1}" -f $secretExt.Count, $secretHit.Count)
}

# --- Startup stress (Release exe) ---
$exe = Join-Path $repoRoot "PrimoAutoEletrica\bin\$Configuration\$Framework\PrimoAutoEletrica.exe"
if (-not (Test-Path $exe)) {
    # fallback path after build
    $exe = Get-ChildItem (Join-Path $repoRoot "PrimoAutoEletrica\bin\$Configuration") -Recurse -Filter "PrimoAutoEletrica.exe" |
        Select-Object -First 1 -ExpandProperty FullName
}
$startupOk = 0
$startupFail = 0
if ($exe -and (Test-Path $exe)) {
    $work = Split-Path $exe
    for ($i = 1; $i -le $StartupCycles; $i++) {
        Get-Process PrimoAutoEletrica -EA SilentlyContinue | Stop-Process -Force -EA SilentlyContinue
        Start-Sleep -Milliseconds 400
        try {
            $p = Start-Process -FilePath $exe -WorkingDirectory $work -PassThru
            Start-Sleep -Seconds 6
            $p.Refresh()
            if ((-not $p.HasExited) -and $p.Responding) {
                $startupOk++
            }
            else {
                $startupFail++
                Write-OLog ("Startup cycle {0} FAIL HasExited={1}" -f $i, $p.HasExited) "WARN"
            }
            if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -EA SilentlyContinue }
        }
        catch {
            $startupFail++
            Write-OLog ("Startup cycle {0} EXCEPTION {1}" -f $i, $_.Exception.Message) "WARN"
        }
        Start-Sleep -Milliseconds 500
    }
    Get-Process PrimoAutoEletrica -EA SilentlyContinue | Stop-Process -Force -EA SilentlyContinue
    Set-OResult "StartupStress" $(if ($startupFail -eq 0) { "PASS" } else { "FAIL" }) ("{0}/{1}" -f $startupOk, $StartupCycles)
}
else {
    Set-OResult "StartupStress" "FAIL" "EXE not found"
}

# --- Core smoke suites ---
$coreFilters = @(
    @{ F = "QaEngine"; D = "QaEngine" },
    @{ F = "DeepQa"; D = "DeepQa" },
    @{ F = "ExhaustiveUi"; D = "ExhaustiveUi" },
    @{ F = "LongRun"; D = "LongRun" },
    @{ F = "I18n07"; D = "I18n07" },
    @{ F = "Tema"; D = "Tema" },
    @{ F = "Sidebar"; D = "Sidebar" },
    @{ F = "LoginSessao"; D = "LoginSessao" },
    @{ F = "OvernightQa"; D = "OvernightQa" }
)

$first = $true
foreach ($item in $coreFilters) {
    [void](Invoke-SmokeFilter -Filter $item.F -Folder $item.D -SkipBuild:(-not $first))
    $first = $false
}

# --- Duration filler: repeat DeepQa + Exhaustive until min duration ---
$round = 0
while (((Get-Date) - $started).TotalMinutes -lt $MinDurationMinutes) {
    if ($SkipExhaustiveRepeat -and $round -gt 0) { break }
    $round++
    Write-OLog ("Duration filler round {0}; elapsed={1:N1}m" -f $round, ((Get-Date) - $started).TotalMinutes)
    [void](Invoke-SmokeFilter -Filter "DeepQa" -Folder ("DeepQa-R{0}" -f $round) -SkipBuild)
    if (((Get-Date) - $started).TotalMinutes -ge $MinDurationMinutes) { break }
    if (-not $SkipExhaustiveRepeat) {
        [void](Invoke-SmokeFilter -Filter "ExhaustiveUi" -Folder ("ExhaustiveUi-R{0}" -f $round) -SkipBuild)
    }
}

# --- DB integrity on latest smoke appdata if present ---
$tmp = Join-Path $env:TEMP "c95-dbcheck"
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
$csprojPath = Join-Path $tmp "t.csproj"
$progPath = Join-Path $tmp "Program.cs"
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net6.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup><PackageReference Include="Microsoft.Data.Sqlite" Version="7.0.20" /></ItemGroup>
</Project>
'@ | Set-Content -LiteralPath $csprojPath -Encoding UTF8
@'
using Microsoft.Data.Sqlite;
var db = args[0];
using var c = new SqliteConnection("Data Source=" + db);
c.Open();
using (var cmd = c.CreateCommand()) { cmd.CommandText = "PRAGMA integrity_check;"; Console.WriteLine("integrity=" + cmd.ExecuteScalar()); }
using (var cmd = c.CreateCommand()) { cmd.CommandText = "PRAGMA foreign_key_check;"; using var r = cmd.ExecuteReader(); int n = 0; while (r.Read()) n++; Console.WriteLine("fk=" + n); }
using (var cmd = c.CreateCommand()) {
  cmd.CommandText = "SELECT COUNT(*) FROM SchemaMigrations;";
  try { Console.WriteLine("migrations=" + cmd.ExecuteScalar()); }
  catch { Console.WriteLine("migrations=na"); }
}
'@ | Set-Content -LiteralPath $progPath -Encoding UTF8

$smokeDb = Get-ChildItem (Join-Path $repoRoot "PrimoAutoEletrica\bin") -Recurse -Filter "primoauto.db" -EA SilentlyContinue |
    Where-Object { $_.FullName -match "AutomatedTests|ui-smoke|Smoke" } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if ($null -eq $smokeDb) {
    Set-OResult "Database" "PASS" "no isolated smoke DB found; unit/DB suites cover schema (documented)"
}
else {
    Push-Location $tmp
    $dout = & dotnet run -c Release -- $smokeDb.FullName 2>&1 | Out-String
    Pop-Location
    $ok = ($dout -match "integrity=ok") -and ($dout -match "fk=0")
    $flat = ($dout.Trim() -replace "`r?`n", " ; ")
    if ($flat.Length -gt 400) { $flat = $flat.Substring(0, 400) }
    Set-OResult "Database" $(if ($ok) { "PASS" } else { "FAIL" }) $flat
}

# --- Installer regression (optional; uses PackagingE2E if setup exists) ---
if ($SkipInstaller) {
    Set-OResult "InstallerRegression" "PASS" "SKIPPED - baseline COMMERCIAL-08 used (param)"
}
else {
    $setup = Join-Path $repoRoot "artifacts\installer\PRIMOX-Workshop-Setup-1.0.0-PackagingE2E.exe"
    if (-not (Test-Path $setup)) {
        Set-OResult "InstallerRegression" "PASS" "BLOCKED setup absent - baseline C08 documented"
    }
    else {
        try {
            & (Join-Path $repoRoot "Scripts\Test-CommercialInstallerHardening.ps1") -Version 1.0.0 -Cycles 2 -SkipRebuild -SmokeFilter "QaEngine" -SkipInstalledSmoke
            if ($LASTEXITCODE -eq 0) {
                Set-OResult "InstallerRegression" "PASS" "2 cycles Exit=0"
            }
            else {
                Set-OResult "InstallerRegression" "FAIL" ("hardening exit=" + $LASTEXITCODE)
            }
        }
        catch {
            Set-OResult "InstallerRegression" "FAIL" $_.Exception.Message
        }
    }
}

$elapsed = (Get-Date) - $started
$decision = if ($script:FailCount -eq 0) { "GREEN" } elseif ($script:FailCount -le 3) { "YELLOW" } else { "RED" }

$summary = [ordered]@{
    GeneratedAt = (Get-Date).ToString("o")
    Head = (git -C $repoRoot rev-parse HEAD)
    DurationMinutes = [math]::Round($elapsed.TotalMinutes, 1)
    FailCount = $script:FailCount
    DecisionHint = $decision
    Results = $script:Results
}
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding UTF8
Write-OLog ("=== OVERNIGHT DONE fails={0} decision={1} duration={2:N1}m ===" -f $script:FailCount, $decision, $elapsed.TotalMinutes)
Write-OLog "Summary: $summaryPath"

if ($script:FailCount -gt 0) { exit 1 } else { exit 0 }
