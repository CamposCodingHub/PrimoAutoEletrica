param(
    [string]$CsvDir,
    [string]$ConnectionString
)

if (-not (Test-Path $CsvDir)) { Write-Host "CSV directory not found: $CsvDir" -ForegroundColor Red; exit 2 }

Add-Type -AssemblyName System.Data
Add-Type -AssemblyName System.Data.DataSetExtensions

try {
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($ConnectionString)
} catch {
    Write-Host "Invalid connection string" -ForegroundColor Red; exit 3
}

$server = $builder.DataSource
$database = $builder.InitialCatalog
$useTrusted = $builder.IntegratedSecurity

foreach ($csv in Get-ChildItem -Path $CsvDir -Filter *.csv) {
    $table = [System.IO.Path]::GetFileNameWithoutExtension($csv.Name)
    Write-Host "Importing $($csv.FullName) -> $table"

    $bcpArgs = @()
    $bcpArgs += "`"$($builder.InitialCatalog).dbo.$table`""
    $bcpArgs += "in"
    $bcpArgs += "`"$($csv.FullName)`""
    $bcpArgs += "-c"
    $bcpArgs += "-t,"
    if ($useTrusted) { $bcpArgs += "-T" } else { $bcpArgs += "-U"; $bcpArgs += $builder.UserID; $bcpArgs += "-P"; $bcpArgs += $builder.Password }
    $bcpArgs += "-S"; $bcpArgs += $builder.DataSource

    $cmd = "bcp " + ($bcpArgs -join ' ')
    Write-Host "Running: $cmd"
    $proc = Start-Process -FilePath bcp -ArgumentList $bcpArgs -NoNewWindow -Wait -PassThru
    if ($proc.ExitCode -ne 0) { Write-Host "bcp failed for $table" -ForegroundColor Red }
}

Write-Host "Import finished. Verify data and constraints on SQL Server." -ForegroundColor Green
