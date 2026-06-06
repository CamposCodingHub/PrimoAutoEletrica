param(
    [string]$OutputDirectory = "Artifacts",
    [string]$PackageName = "",
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"

$solutionRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$generatedAt = Get-Date

if ([string]::IsNullOrWhiteSpace($PackageName)) {
    $PackageName = "PrimoAutoEletrica_Source_{0:yyyyMMdd_HHmmss}.zip" -f $generatedAt
}

$outputRoot = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory
}
else {
    Join-Path $solutionRoot $OutputDirectory
}

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$packagePath = Join-Path $outputRoot $PackageName
$manifestPath = [System.IO.Path]::ChangeExtension($packagePath, ".manifest.txt")

$excludedDirectoryNames = @(
    ".git",
    ".vs",
    ".vscode",
    ".idea",
    "bin",
    "obj",
    "Debug",
    "Release",
    "Artifacts",
    "Logs",
    "Backups",
    "Reports",
    "Exports",
    "Imports",
    "Temp",
    "TestResults",
    "node_modules"
)

$excludedExtensions = @(
    ".user",
    ".suo",
    ".lscache",
    ".db",
    ".sqlite",
    ".sqlite3",
    ".db-shm",
    ".db-wal",
    ".log",
    ".binlog",
    ".zip",
    ".nupkg",
    ".tmp",
    ".temp",
    ".bak",
    ".orig",
    ".rej",
    ".patch",
    ".backup",
    ".old",
    ".copy",
    ".teste"
)

$excludedFileNames = @(
    "build_output.txt",
    "data.json"
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
    $relativeUri = $baseUri.MakeRelativeUri($targetUri)
    return [Uri]::UnescapeDataString($relativeUri.ToString()).Replace("/", [System.IO.Path]::DirectorySeparatorChar)
}

function Test-IsExcludedFile {
    param([Parameter(Mandatory = $true)][System.IO.FileInfo]$File)

    $relativePath = Get-RelativePath -BasePath $solutionRoot -TargetPath $File.FullName
    $segments = $relativePath -split "[\\/]+"

    foreach ($segment in $segments) {
        if ($excludedDirectoryNames -contains $segment) {
            return $true
        }
    }

    $extension = $File.Extension
    if ($excludedExtensions -contains $extension) {
        return $true
    }

    if ($excludedFileNames -contains $File.Name) {
        return $true
    }

    if ($File.Name -match "\.(db-shm|db-wal)$") {
        return $true
    }

    return $false
}

$includedFiles = Get-ChildItem -Path $solutionRoot -Recurse -File -Force |
    Where-Object { -not (Test-IsExcludedFile -File $_) } |
    Sort-Object FullName

$entryNames = $includedFiles | ForEach-Object {
    (Get-RelativePath -BasePath $solutionRoot -TargetPath $_.FullName).Replace("\", "/")
}

$forbiddenEntries = $entryNames | Where-Object {
    $_ -match "(^|/)(\.vs|bin|obj|Debug|Release|Artifacts|Logs|Backups|Reports|Exports|Imports|Temp|TestResults)(/|$)" -or
    $_ -match "\.(db|sqlite|sqlite3|db-shm|db-wal|log|binlog|zip|nupkg|tmp|temp|bak|orig|rej|patch|backup|old|copy|teste|lscache|user|suo)$" -or
    $_ -ieq "build_output.txt" -or
    $_ -ieq "data.json"
}

if ($forbiddenEntries.Count -gt 0) {
    $preview = $forbiddenEntries | Select-Object -First 20
    throw "Pacote bloqueado: entradas proibidas detectadas antes de compactar:`n$($preview -join [Environment]::NewLine)"
}

if (-not $SkipZip) {
    if (Test-Path $packagePath) {
        Remove-Item -LiteralPath $packagePath -Force
    }

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::Open($packagePath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $includedFiles) {
            $entryName = (Get-RelativePath -BasePath $solutionRoot -TargetPath $file.FullName).Replace("\", "/")
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip,
                $file.FullName,
                $entryName,
                [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $zip.Dispose()
    }
}

$manifest = New-Object System.Collections.Generic.List[string]
$manifest.Add("PRIMO AUTO ELETRICA - DELIVERY PACKAGE MANIFEST")
$manifest.Add(("GeneratedAt: {0:yyyy-MM-dd HH:mm:ss}" -f $generatedAt))
$manifest.Add("SolutionRoot: $solutionRoot")
$manifest.Add("PackagePath: $packagePath")
$manifest.Add("PackageCreated: $(-not $SkipZip)")
$manifest.Add("FileCount: $($includedFiles.Count)")
$manifest.Add("ForbiddenEntries: $($forbiddenEntries.Count)")
$manifest.Add("ExcludedDirectoryNames: $($excludedDirectoryNames -join ', ')")
$manifest.Add("ExcludedExtensions: $($excludedExtensions -join ', ')")
$manifest.Add("ExcludedFileNames: $($excludedFileNames -join ', ')")
$manifest.Add("")
$manifest.Add("IncludedFiles:")
foreach ($entryName in $entryNames) {
    $manifest.Add($entryName)
}

Set-Content -Path $manifestPath -Value $manifest.ToArray() -Encoding UTF8

[PSCustomObject]@{
    PackagePath = $packagePath
    ManifestPath = $manifestPath
    FileCount = $includedFiles.Count
    ForbiddenEntries = $forbiddenEntries.Count
    PackageCreated = -not $SkipZip
}
