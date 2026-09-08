<#
.SYNOPSIS
  Packaging E2E — instala Setup isolado, smoke no EXE, DB, uninstall, reinstall.
#>
[CmdletBinding()]
param(
    [string]$SetupPath = "",
    [string]$Version = "1.0.0",
    [switch]$SkipQaEngine,
    [switch]$SkipRebuild,
    [string]$SmokeFilter = "QaEngine"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($SetupPath)) {
    $SetupPath = Join-Path $repoRoot "artifacts\installer\PRIMOX-Workshop-Setup-$Version.exe"
}

$installDir = Join-Path $env:LOCALAPPDATA "PRIMOX-Workshop-InstallTest-15C"
$dataDir = Join-Path $env:LOCALAPPDATA "PRIMOX-Workshop-DataTest-15C"
$reportDir = Join-Path $repoRoot "TestResults\PackagingE2E"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportPath = Join-Path $reportDir "packaging-e2e-$stamp.md"
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

$script:Results = @()

function Add-Result {
    param([string]$Name, [string]$Status, [string]$Detail = "")
    $script:Results += "| $Name | $Status | $Detail |"
    Write-Host "[$Status] $Name - $Detail"
}

function Assert-True {
    param([bool]$Condition, [string]$Name, [string]$DetailPass, [string]$DetailFail)
    if ($Condition) {
        Add-Result -Name $Name -Status "PASS" -Detail $DetailPass
    }
    else {
        Add-Result -Name $Name -Status "FAIL" -Detail $DetailFail
        throw "FAIL: $Name - $DetailFail"
    }
}

Write-Host "=== PRIMOX Packaging E2E ==="
Write-Host "Repo: $repoRoot"
Write-Host "Setup: $SetupPath"

Get-Process PrimoAutoEletrica -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

if (-not $SkipRebuild) {
    $env:DOTNET_ROLL_FORWARD = "LatestMajor"
    & (Join-Path $repoRoot "Scripts\Build-PrimoXCommercialRelease.ps1") -Version $Version -SkipTests
}

Assert-True -Condition (Test-Path -LiteralPath $SetupPath) -Name "Setup exists" -DetailPass $SetupPath -DetailFail "Setup ausente"

$hash = (Get-FileHash -LiteralPath $SetupPath -Algorithm SHA256).Hash
Add-Result -Name "Setup SHA256" -Status "PASS" -Detail $hash

if (Test-Path $installDir) { Remove-Item $installDir -Recurse -Force }
if (Test-Path $dataDir) { Remove-Item $dataDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null

$install = Start-Process -FilePath $SetupPath -ArgumentList @(
    "/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/DIR=`"$installDir`""
) -Wait -PassThru
Assert-True -Condition ($install.ExitCode -eq 0) -Name "Install" -DetailPass "Exit=0" -DetailFail ("Exit=" + $install.ExitCode)

$exe = Join-Path $installDir "PrimoAutoEletrica.exe"
Assert-True -Condition (Test-Path $exe) -Name "EXE installed" -DetailPass $exe -DetailFail "EXE ausente"
$vi = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
$verOk = ($vi.ProductVersion -eq "1.0.0") -or ($vi.ProductVersion.StartsWith("1.0.0"))
Assert-True -Condition $verOk -Name "Version" -DetailPass ("PV=" + $vi.ProductVersion + " FV=" + $vi.FileVersion) -DetailFail ("PV=" + $vi.ProductVersion)

$sw = [Diagnostics.Stopwatch]::StartNew()
$ui = Start-Process -FilePath $exe -WorkingDirectory $installDir -PassThru
Start-Sleep -Seconds 10
$alive = -not $ui.HasExited
$responding = $false
$ws = 0
if ($alive) {
    $ui.Refresh()
    $responding = $ui.Responding
    $ws = [math]::Round($ui.WorkingSet64 / 1MB, 1)
    Stop-Process -Id $ui.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}
Assert-True -Condition ($alive -and $responding) -Name "Startup (no smoke)" -DetailPass ("Responding WS=" + $ws + "MB t=" + $sw.Elapsed.TotalSeconds + "s") -DetailFail ("alive=$alive responding=$responding")

Get-Process PrimoAutoEletrica -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
$smokeSw = [Diagnostics.Stopwatch]::StartNew()
$smoke = Start-Process -FilePath $exe -WorkingDirectory $installDir -ArgumentList @(
    "--smoke-test",
    ("--smoke-filter=" + $SmokeFilter),
    ("--app-data=" + $dataDir)
) -PassThru -WindowStyle Hidden
$finished = $smoke.WaitForExit(900000)
if (-not $finished) {
    Stop-Process -Id $smoke.Id -Force -ErrorAction SilentlyContinue
    Add-Result -Name "Installed smoke" -Status "FAIL" -Detail "timeout 15min"
    throw "Installed smoke hung"
}
$smokeExit = $smoke.ExitCode
$dbPath = Join-Path $dataDir "primoauto.db"
$dbOk = Test-Path $dbPath
$smokeStatus = if ($smokeExit -eq 0) { "PASS" } else { "FAIL" }
Add-Result -Name ("Installed smoke " + $SmokeFilter) -Status $smokeStatus -Detail ("Exit=" + $smokeExit + " elapsed=" + [math]::Round($smokeSw.Elapsed.TotalSeconds, 1) + "s")
if ($smokeExit -ne 0) { throw ("Installed smoke exit " + $smokeExit) }
Assert-True -Condition $dbOk -Name "Database creation" -DetailPass $dbPath -DetailFail "primoauto.db ausente"

$migProj = Join-Path $env:TEMP "primox-15c-mig\MigAudit"
New-Item -ItemType Directory -Force -Path $migProj | Out-Null
Set-Content -Path (Join-Path $migProj "MigAudit.csproj") -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net6.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup><PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" /></ItemGroup>
</Project>
"@
Set-Content -Path (Join-Path $migProj "Program.cs") -Value @"
using Microsoft.Data.Sqlite;
var db = args[0];
using var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = db, Mode = SqliteOpenMode.ReadOnly }.ToString());
c.Open();
using (var cmd = c.CreateCommand()) { cmd.CommandText = "PRAGMA integrity_check;"; Console.WriteLine("integrity=" + cmd.ExecuteScalar()); }
using (var cmd = c.CreateCommand()) { cmd.CommandText = "PRAGMA foreign_key_check;"; using var r = cmd.ExecuteReader(); int n=0; while(r.Read()) n++; Console.WriteLine("fk=" + n); }
using (var cmd = c.CreateCommand()) { cmd.CommandText = "SELECT COUNT(*) FROM SchemaMigrations;"; Console.WriteLine("migrations=" + cmd.ExecuteScalar()); }
"@
Push-Location $migProj
$env:DOTNET_ROLL_FORWARD = "LatestMajor"
$migOut = (& dotnet run -c Release -- $dbPath 2>&1 | Out-String)
Pop-Location
Write-Host $migOut
Assert-True -Condition ($migOut -match "integrity=ok") -Name "Integrity" -DetailPass "ok" -DetailFail $migOut
Assert-True -Condition ($migOut -match "fk=0") -Name "FK" -DetailPass "0" -DetailFail $migOut
Assert-True -Condition ($migOut -match "migrations=27") -Name "Fresh migrations" -DetailPass "27" -DetailFail $migOut

$marker = Join-Path $env:LOCALAPPDATA "PrimoAutoEletrica\fase15c-retention-marker.txt"
New-Item -ItemType Directory -Force -Path (Split-Path $marker) | Out-Null
Set-Content -LiteralPath $marker -Value "retain-15C" -Encoding UTF8
$unins = Get-ChildItem $installDir -Filter "unins*.exe" | Select-Object -First 1
Assert-True -Condition ($null -ne $unins) -Name "Uninstaller present" -DetailPass $unins.FullName -DetailFail "unins ausente"
$u = Start-Process -FilePath $unins.FullName -ArgumentList "/VERYSILENT","/SUPPRESSMSGBOXES" -Wait -PassThru
Assert-True -Condition (($u.ExitCode -eq 0) -and (-not (Test-Path $exe))) -Name "Uninstall" -DetailPass ("Exit=" + $u.ExitCode) -DetailFail "uninstall falhou"
Assert-True -Condition (Test-Path $marker) -Name "Production marker retained" -DetailPass $marker -DetailFail "marker removido"
Assert-True -Condition (Test-Path $dbPath) -Name "Isolated data retained after uninstall" -DetailPass $dbPath -DetailFail "DB teste perdido"

$install2 = Start-Process -FilePath $SetupPath -ArgumentList @(
    "/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/DIR=`"$installDir`""
) -Wait -PassThru
Assert-True -Condition (($install2.ExitCode -eq 0) -and (Test-Path $exe)) -Name "Reinstall" -DetailPass "Exit=0" -DetailFail "reinstall falhou"
Assert-True -Condition (Test-Path $dbPath) -Name "Reinstall data preserved" -DetailPass $dbPath -DetailFail "DB teste perdido"

$unins2 = Get-ChildItem $installDir -Filter "unins*.exe" | Select-Object -First 1
if ($null -ne $unins2) {
    Start-Process -FilePath $unins2.FullName -ArgumentList "/VERYSILENT","/SUPPRESSMSGBOXES" -Wait | Out-Null
}

$table = ($script:Results -join "`n")
$md = @"
# Packaging E2E $stamp

Setup: ``$SetupPath``
SHA256: ``$hash``
InstallDir: ``$installDir``
AppData teste: ``$dataDir``
SmokeFilter: ``$SmokeFilter``

| Teste | Status | Detalhe |
|-------|--------|---------|
$table
"@
Set-Content -LiteralPath $reportPath -Value $md -Encoding UTF8
Write-Host "Report: $reportPath"

if (-not $SkipQaEngine) {
    Write-Host "Running Debug QaEngine..."
    & (Join-Path $repoRoot "Scripts\Run-UiSmoke.ps1") -Framework net6.0-windows -SmokeFilter QaEngine
}

Write-Host "=== Packaging E2E DONE ==="
