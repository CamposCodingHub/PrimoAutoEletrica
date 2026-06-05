param(
    [int]$Port = 52000,
    [int]$Duration = 8
)

$solutionRoot = Split-Path -Parent $PSScriptRoot
$simPath = Join-Path $solutionRoot 'Tools\LocalSyncSimulator'

Write-Host "Building simulator..."
dotnet build $simPath -c Debug
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed"; exit 1 }

$dotnet = 'dotnet'
$proj = Join-Path $simPath 'bin\Debug\net9.0\LocalSyncSimulator.dll'

Write-Host "Starting StationA and StationB for $Duration seconds (port $Port)..."
$proc1 = Start-Process -NoNewWindow -PassThru -FilePath $dotnet -ArgumentList "$proj --id StationA --port $Port --duration $Duration"
$proc2 = Start-Process -NoNewWindow -PassThru -FilePath $dotnet -ArgumentList "$proj --id StationB --port $Port --duration $Duration"

Write-Host "Waiting for processes to finish..."
Wait-Process -Id $proc1.Id,$proc2.Id

Write-Host "Simulação concluída. Logs em: Tools\LocalSyncSimulator\bin\Debug\net9.0\logs\"