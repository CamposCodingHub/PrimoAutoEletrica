param(
    [string]$OutputDirectory = "Artifacts\CleanPackages",
    [string]$PackageName = "",
    [switch]$ValidateExtractionBuild,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$generatedAt = Get-Date
$timestamp = $generatedAt.ToString("yyyyMMdd_HHmmss")

if ([string]::IsNullOrWhiteSpace($PackageName)) {
    $PackageName = "PrimoAutoEletrica_CleanPackage_$timestamp.zip"
}

if (-not $PackageName.EndsWith(".zip", [StringComparison]::OrdinalIgnoreCase)) {
    $PackageName = "$PackageName.zip"
}

$outputRoot = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory
}
else {
    Join-Path $projectRoot $OutputDirectory
}

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

$packagePath = Join-Path $outputRoot $PackageName
$manifestPath = [System.IO.Path]::ChangeExtension($packagePath, ".manifest.txt")
$validationLogPath = [System.IO.Path]::ChangeExtension($packagePath, ".build.log")
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "PrimoAutoEletrica_CleanPackage_$timestamp"
$stagingRoot = Join-Path $tempRoot "source"
$extractRoot = Join-Path $tempRoot "extract"

$excludedDirectoryNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
@(
    ".git",
    ".vs",
    ".vscode",
    ".idea",
    "bin",
    "obj",
    "Debug",
    "Release",
    "Artifacts",
    "Backups",
    "cache",
    ".cache",
    "Logs",
    "logs",
    "node_modules",
    "PackageClean",
    "Reports",
    "Temp",
    "temp",
    "TestResults"
) | ForEach-Object { [void]$excludedDirectoryNames.Add($_) }

$excludedExtensions = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
@(
    ".bak",
    ".backup",
    ".binlog",
    ".copy",
    ".db",
    ".db-shm",
    ".db-wal",
    ".log",
    ".lscache",
    ".nupkg",
    ".old",
    ".orig",
    ".patch",
    ".rej",
    ".sqlite",
    ".sqlite3",
    ".suo",
    ".temp",
    ".teste",
    ".tmp",
    ".user",
    ".zip"
) | ForEach-Object { [void]$excludedExtensions.Add($_) }

$excludedFileNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
@(
    "build_output.txt",
    "data.json",
    "PR_BODY.md",
    "Thumbs.db",
    "Desktop.ini"
) | ForEach-Object { [void]$excludedFileNames.Add($_) }

$requiredRelativePaths = @(
    "PrimoAutoEletrica.sln",
    "PrimoAutoEletrica",
    "PrimoAutoEletrica\Themes",
    "PrimoAutoEletrica\Views",
    "PrimoAutoEletrica\ViewModels",
    "PrimoAutoEletrica\Models",
    "PrimoAutoEletrica\Services",
    "PrimoAutoEletrica\Data",
    "Scripts",
    "Docs",
    "Tests"
)

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string]$TargetPath
    )

    $baseFullPath = [System.IO.Path]::GetFullPath($BasePath)
    if (-not $baseFullPath.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $baseFullPath += [System.IO.Path]::DirectorySeparatorChar
    }

    $targetFullPath = [System.IO.Path]::GetFullPath($TargetPath)
    $baseUri = [Uri]::new($baseFullPath)
    $targetUri = [Uri]::new($targetFullPath)

    $relativePath = [Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString())
    return $relativePath.Replace("/", [System.IO.Path]::DirectorySeparatorChar)
}

function Test-IsExcludedFile {
    param([Parameter(Mandatory = $true)][System.IO.FileInfo]$File)

    $relativePath = Get-RelativePath -BasePath $projectRoot -TargetPath $File.FullName
    $segments = $relativePath -split "[\\/]+"

    foreach ($segment in $segments) {
        if ($excludedDirectoryNames.Contains($segment)) {
            return $true
        }
    }

    if ($excludedExtensions.Contains($File.Extension)) {
        return $true
    }

    if ($excludedFileNames.Contains($File.Name)) {
        return $true
    }

    return $false
}

function Test-IsForbiddenEntry {
    param([Parameter(Mandatory = $true)][string]$EntryName)

    $segments = $EntryName -split "/+"
    foreach ($segment in $segments) {
        if ($excludedDirectoryNames.Contains($segment)) {
            return $true
        }
    }

    $extension = [System.IO.Path]::GetExtension($EntryName)
    if ($excludedExtensions.Contains($extension)) {
        return $true
    }

    $name = [System.IO.Path]::GetFileName($EntryName)
    return $excludedFileNames.Contains($name)
}

if (Test-Path $tempRoot) {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

$includedFiles = @(Get-ChildItem -Path $projectRoot -Recurse -File -Force |
    Where-Object { -not (Test-IsExcludedFile -File $_) } |
    Sort-Object FullName)

foreach ($file in $includedFiles) {
    $relativePath = Get-RelativePath -BasePath $projectRoot -TargetPath $file.FullName
    $destination = Join-Path $stagingRoot $relativePath
    $destinationDirectory = Split-Path -Path $destination -Parent

    if (-not (Test-Path $destinationDirectory)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
}

$metadata = @(
    "PRIMO AUTO ELETRICA - CLEAN PACKAGE",
    "GeneratedAt: $($generatedAt.ToString("yyyy-MM-dd HH:mm:ss"))",
    "ProjectRoot: $projectRoot",
    "PackagePath: $packagePath",
    "Branch: $(git -C $projectRoot branch --show-current)",
    "Commit: $(git -C $projectRoot rev-parse --short HEAD)",
    "IncludedFiles: $($includedFiles.Count)",
    "ExcludedDirectories: $($excludedDirectoryNames -join ', ')",
    "ExcludedExtensions: $($excludedExtensions -join ', ')",
    "ExcludedFiles: $($excludedFileNames -join ', ')",
    "",
    "Usage:",
    "1. Extract the ZIP.",
    "2. Open PrimoAutoEletrica.sln in Visual Studio.",
    "3. Run dotnet restore.",
    "4. Run dotnet build."
)

Set-Content -Path (Join-Path $stagingRoot "PACKAGE_METADATA.txt") -Value $metadata -Encoding UTF8

foreach ($requiredPath in $requiredRelativePaths) {
    if (-not (Test-Path (Join-Path $stagingRoot $requiredPath))) {
        throw "Pacote bloqueado: item obrigatorio ausente no staging: $requiredPath"
    }
}

$stagedEntryNames = @(Get-ChildItem -Path $stagingRoot -Recurse -File -Force |
    ForEach-Object { (Get-RelativePath -BasePath $stagingRoot -TargetPath $_.FullName).Replace("\", "/") } |
    Sort-Object)

$forbiddenEntries = @($stagedEntryNames | Where-Object { Test-IsForbiddenEntry -EntryName $_ })
if ($forbiddenEntries.Count -gt 0) {
    $preview = $forbiddenEntries | Select-Object -First 25
    throw "Pacote bloqueado: entradas proibidas detectadas:`n$($preview -join [Environment]::NewLine)"
}

if (Test-Path $packagePath) {
    Remove-Item -LiteralPath $packagePath -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::Open($packagePath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($entryName in $stagedEntryNames) {
        $sourcePath = Join-Path $stagingRoot $entryName.Replace("/", [System.IO.Path]::DirectorySeparatorChar)
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip,
            $sourcePath,
            $entryName,
            [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally {
    $zip.Dispose()
}

$zipRead = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $zipForbiddenEntries = @($zipRead.Entries.FullName | Where-Object { Test-IsForbiddenEntry -EntryName $_ })
    if ($zipForbiddenEntries.Count -gt 0) {
        $preview = $zipForbiddenEntries | Select-Object -First 25
        throw "Pacote bloqueado: ZIP contem entradas proibidas:`n$($preview -join [Environment]::NewLine)"
    }
}
finally {
    $zipRead.Dispose()
}

$buildStatus = "NAO_EXECUTADO"
if ($ValidateExtractionBuild) {
    if (Test-Path $extractRoot) {
        Remove-Item -LiteralPath $extractRoot -Recurse -Force
    }

    Expand-Archive -LiteralPath $packagePath -DestinationPath $extractRoot -Force
    $extractedSolution = Join-Path $extractRoot "PrimoAutoEletrica.sln"

    if (-not (Test-Path $extractedSolution)) {
        throw "Validacao bloqueada: solucao nao encontrada apos extrair o pacote."
    }

    $buildOutput = & dotnet build $extractedSolution -c $Configuration --nologo 2>&1
    $buildOutput | Set-Content -Path $validationLogPath -Encoding UTF8

    if ($LASTEXITCODE -ne 0) {
        throw "Validacao bloqueada: pacote extraido nao compilou. Veja $validationLogPath"
    }

    $buildStatus = "APROVADO"
}

$packageSizeBytes = (Get-Item -LiteralPath $packagePath).Length
$sourceSizeBytes = ($includedFiles | Measure-Object -Property Length -Sum).Sum

$manifest = New-Object System.Collections.Generic.List[string]
$manifest.Add("PRIMO AUTO ELETRICA - CLEAN PACKAGE MANIFEST")
$manifest.Add("GeneratedAt: $($generatedAt.ToString("yyyy-MM-dd HH:mm:ss"))")
$manifest.Add("ProjectRoot: $projectRoot")
$manifest.Add("PackagePath: $packagePath")
$manifest.Add("PackageSizeMB: $([Math]::Round($packageSizeBytes / 1MB, 2))")
$manifest.Add("IncludedSourceSizeMB: $([Math]::Round($sourceSizeBytes / 1MB, 2))")
$manifest.Add("FileCount: $($stagedEntryNames.Count)")
$manifest.Add("BuildValidation: $buildStatus")
$manifest.Add("BuildValidationLog: $validationLogPath")
$manifest.Add("ForbiddenEntries: 0")
$manifest.Add("")
$manifest.Add("IncludedFiles:")
foreach ($entryName in $stagedEntryNames) {
    $manifest.Add($entryName)
}

Set-Content -Path $manifestPath -Value $manifest.ToArray() -Encoding UTF8

Remove-Item -LiteralPath $stagingRoot -Recurse -Force

[PSCustomObject]@{
    PackagePath = $packagePath
    ManifestPath = $manifestPath
    FileCount = $stagedEntryNames.Count
    PackageSizeMB = [Math]::Round($packageSizeBytes / 1MB, 2)
    BuildValidation = $buildStatus
    ForbiddenEntries = 0
}
