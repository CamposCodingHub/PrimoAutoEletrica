<#
.SYNOPSIS
  Pipeline oficial de distribuição comercial do PRIMOX Workshop 1.0.0.

.DESCRIPTION
  OFFICIAL COMMERCIAL DISTRIBUTION
  Código → Build Release → Publish win-x64 self-contained → Inno Setup → Setup + SHA256

  TFM fonte de verdade: PrimoAutoEletrica.csproj (net6.0-windows).
  Não altera banco, schema ou regras de negócio.

.EXAMPLE
  .\Scripts\Build-PrimoXCommercialRelease.ps1
  .\Scripts\Build-PrimoXCommercialRelease.ps1 -SkipTests -SkipClean
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0",
    [switch]$SkipClean,
    [switch]$SkipTests,
    [switch]$SkipInstaller,
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "PrimoAutoEletrica\PrimoAutoEletrica.csproj"
$issPath = Join-Path $repoRoot "Installer\PrimoAutoEletrica.iss"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishDir = Join-Path $artifactsRoot "publish\$Runtime"
$installerDir = Join-Path $artifactsRoot "installer"
$checksumsDir = Join-Path $artifactsRoot "checksums"
$logsDir = Join-Path $artifactsRoot "logs"
$setupFileName = "PRIMOX-Workshop-Setup-$Version.exe"
$setupPath = Join-Path $installerDir $setupFileName
$shaPath = Join-Path $checksumsDir "PRIMOX-Workshop-Setup-$Version.sha256.txt"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "csproj nao encontrado: $projectPath"
}

New-Item -ItemType Directory -Force -Path $publishDir, $installerDir, $checksumsDir, $logsDir | Out-Null
$logFile = Join-Path $logsDir ("commercial-release-{0:yyyyMMdd-HHmmss}.log" -f (Get-Date))

function Write-Log([string]$Message) {
    $line = "[{0:yyyy-MM-dd HH:mm:ss}] {1}" -f (Get-Date), $Message
    Write-Host $line
    Add-Content -LiteralPath $logFile -Value $line
}

function Get-ProjectTfm {
    $xml = [xml](Get-Content -LiteralPath $projectPath -Raw)
    $tfm = $xml.Project.PropertyGroup.TargetFramework | Where-Object { $_ } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($tfm)) {
        throw "TargetFramework nao encontrado em $projectPath"
    }
    return $tfm.Trim()
}

function Find-Iscc {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 7\ISCC.exe"),
        "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 7\ISCC.exe"
    )
    foreach ($c in $candidates) {
        if ($c -and (Test-Path -LiteralPath $c)) { return $c }
    }
    return $null
}

$started = Get-Date
$tfm = Get-ProjectTfm
Write-Log "=== PRIMOX Commercial Release ==="
Write-Log "Repo: $repoRoot"
Write-Log "Version: $Version"
Write-Log "TFM: $tfm"
Write-Log "Runtime: $Runtime"
Write-Log "Configuration: $Configuration"
Write-Log "SelfContained: $(-not $FrameworkDependent)"

if ($tfm -ne "net6.0-windows") {
    Write-Log "AVISO: TFM esperado net6.0-windows; encontrado $tfm. Prosseguindo com TFM do csproj."
}

Push-Location $repoRoot
try {
    if (-not $SkipClean) {
        Write-Log "Clean..."
        & dotnet clean $projectPath -c $Configuration --nologo
        if ($LASTEXITCODE -ne 0) { throw "dotnet clean falhou ($LASTEXITCODE)" }
        if (Test-Path -LiteralPath $publishDir) {
            Remove-Item -LiteralPath $publishDir -Recurse -Force
            New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
        }
    }

    Write-Log "Restore..."
    & dotnet restore $projectPath --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore falhou ($LASTEXITCODE)" }

    Write-Log "Build..."
    & dotnet build $projectPath -c $Configuration --no-restore --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet build falhou ($LASTEXITCODE)" }

    if (-not $SkipTests) {
        $testProj = Join-Path $repoRoot "PrimoAutoEletrica\Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj"
        if (Test-Path -LiteralPath $testProj) {
            Write-Log "Test (unit)..."
            & dotnet test $testProj -c $Configuration --nologo --verbosity minimal
            if ($LASTEXITCODE -ne 0) { throw "dotnet test falhou ($LASTEXITCODE)" }
        }
        else {
            Write-Log "Test project ausente - pulando unit tests."
        }
    }

    Write-Log ("Publish -> {0}" -f $publishDir)
    $publishArgs = @(
        "publish", $projectPath,
        "-c", $Configuration,
        "-r", $Runtime,
        "-o", $publishDir,
        "--nologo",
        "-p:PublishTrimmed=false",
        "-p:PublishSingleFile=false"
    )
    if ($FrameworkDependent) {
        $publishArgs += @("--self-contained", "false")
    }
    else {
        $publishArgs += @("--self-contained", "true")
    }

    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou ($LASTEXITCODE)" }

    $exe = Join-Path $publishDir "PrimoAutoEletrica.exe"
    if (-not (Test-Path -LiteralPath $exe)) {
        throw "Publish nao gerou $exe"
    }

    $fv = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
    Write-Log ("EXE ProductVersion={0}; FileVersion={1}; Size={2:N0} bytes" -f $fv.ProductVersion, $fv.FileVersion, (Get-Item $exe).Length)
    if ($fv.ProductVersion -ne $Version -and $fv.ProductVersion -ne ($Version + ".0")) {
        Write-Log ("AVISO VERSIONING: ProductVersion={0} esperado={1}" -f $fv.ProductVersion, $Version)
    }
    if ($fv.ProductVersion -eq "0.0.0.0" -or [string]::IsNullOrWhiteSpace($fv.ProductVersion)) {
        throw ("VERSIONING ISSUE: ProductVersion invalido ({0})." -f $fv.ProductVersion)
    }

    # Garantir que nenhum DB de producao/dados reais entre no pacote
    Get-ChildItem -LiteralPath $publishDir -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in @('.db', '.db-wal', '.db-shm') -or $_.Name -like '*.db-wal' -or $_.Name -like '*.db-shm' } |
        ForEach-Object {
            Write-Log ("Removendo DB acidental do publish: {0}" -f $_.FullName)
            Remove-Item -LiteralPath $_.FullName -Force -Confirm:$false -ErrorAction Stop
        }

    $manifest = [ordered]@{
        Product = "PRIMOX Workshop"
        Version = $Version
        Tfm = $tfm
        Runtime = $Runtime
        SelfContained = (-not $FrameworkDependent)
        PublishDir = $publishDir
        ProductVersion = $fv.ProductVersion
        FileVersion = $fv.FileVersion
        GeneratedAt = (Get-Date).ToString("o")
        Classification = "OFFICIAL COMMERCIAL DISTRIBUTION"
        DataPath = "%LOCALAPPDATA%\PrimoAutoEletrica"
        DatabaseFile = "primoauto.db"
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $publishDir "PRIMOX-PUBLISH-MANIFEST.json") -Encoding UTF8

    if ($SkipInstaller) {
        Write-Log "SkipInstaller: publish concluido sem Inno."
    }
    else {
        $iscc = Find-Iscc
        if (-not $iscc) {
            throw "ISCC.exe nao encontrado. Instale Inno Setup 6 (winget install JRSoftware.InnoSetup) e reexecute."
        }
        Write-Log "ISCC: $iscc"
        if (-not (Test-Path -LiteralPath $issPath)) {
            throw "ISS nao encontrado: $issPath"
        }

        if (Test-Path -LiteralPath $setupPath) {
            Remove-Item -LiteralPath $setupPath -Force
        }

        # Inno trata melhor caminhos com barras invertidas escapadas via define relativo ao repo
        $publishDirForIss = (Resolve-Path -LiteralPath $publishDir).Path
        $outputDirForIss = (Resolve-Path -LiteralPath $installerDir).Path
        Write-Log "Compilando instalador..."
        Write-Log "PublishDir=$publishDirForIss"
        Write-Log "OutputDir=$outputDirForIss"
        & $iscc $issPath `
            "/DAppVersion=$Version" `
            "/DPublishDir=$publishDirForIss" `
            "/DOutputDir=$outputDirForIss"
        if ($LASTEXITCODE -ne 0) { throw "ISCC falhou ($LASTEXITCODE)" }

        if (-not (Test-Path -LiteralPath $setupPath)) {
            # Fallback: pegar o Setup mais recente na pasta
            $setupPath = Get-ChildItem -LiteralPath $installerDir -Filter "PRIMOX-Workshop-Setup-*.exe" |
                Sort-Object LastWriteTime -Descending |
                Select-Object -First 1 -ExpandProperty FullName
        }
        if (-not $setupPath -or -not (Test-Path -LiteralPath $setupPath)) {
            throw "Setup nao gerado em $installerDir"
        }

        $hash = (Get-FileHash -LiteralPath $setupPath -Algorithm SHA256).Hash
        $shaLine = "{0}  {1}" -f $hash, (Split-Path -Leaf $setupPath)
        Set-Content -LiteralPath $shaPath -Value $shaLine -Encoding ASCII
        Write-Log "Setup: $setupPath"
        Write-Log "SHA256: $hash"
        Write-Log "Checksum file: $shaPath"
        Write-Log ("Setup size: {0:N2} MB" -f ((Get-Item $setupPath).Length / 1MB))
    }

    $elapsed = (Get-Date) - $started
    Write-Log ("=== CONCLUIDO em {0:mm\:ss} ===" -f $elapsed)
    Write-Log "Log: $logFile"

    [PSCustomObject]@{
        Version = $Version
        Tfm = $tfm
        PublishDir = $publishDir
        SetupPath = $(if (Test-Path -LiteralPath $setupPath) { $setupPath } else { $null })
        Sha256Path = $(if (Test-Path -LiteralPath $shaPath) { $shaPath } else { $null })
        ProductVersion = $fv.ProductVersion
        LogFile = $logFile
    }
}
finally {
    Pop-Location
}
