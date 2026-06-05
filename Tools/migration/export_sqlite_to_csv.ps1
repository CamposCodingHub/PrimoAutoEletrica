param(
    [string]$SqliteFile,
    [string]$OutDir = "./exported_csv"
)

if (-not (Get-Command sqlite3 -ErrorAction SilentlyContinue)) {
    Write-Host "sqlite3 not found in PATH. Install sqlite3 CLI to use this script." -ForegroundColor Yellow
    exit 2
}

if (-not (Test-Path $SqliteFile)) {
    Write-Host "SQLite file not found: $SqliteFile" -ForegroundColor Red
    exit 3
}

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

$tables = & sqlite3 $SqliteFile "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';"

foreach ($t in $tables.Split("`n") | Where-Object { $_ -ne '' }) {
    $file = Join-Path $OutDir ("$t.csv")
    Write-Host "Exporting table $t to $file"
    & sqlite3 -header -csv $SqliteFile "SELECT * FROM [$t];" > $file
}

Write-Host "Export completed. CSV files in: $OutDir"
