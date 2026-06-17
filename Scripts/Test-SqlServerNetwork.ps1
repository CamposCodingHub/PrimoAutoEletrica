# Script de Teste de Conexão SQL Server em Rede
# Uso: .\Test-SqlServerNetwork.ps1 -ServerName "SERVIDOR\SQLEXPRESS" -Database "PrimoAutoEletrica"

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerName,
    
    [string]$Database = "master",
    
    [string]$Username = "",
    
    [string]$Password = ""
)

Write-Host "=== Teste de Conexão SQL Server ===" -ForegroundColor Cyan
Write-Host "Servidor: $ServerName"
Write-Host "Banco: $Database"
Write-Host ""

$testsPassed = 0
$testsFailed = 0

# Teste 1: Ping
Write-Host "[1/5] Testando ping..." -ForegroundColor Yellow
try {
    $ping = Test-Connection -ComputerName $ServerName.Split('\')[0] -Count 1 -Quiet
    if ($ping) {
        Write-Host "✓ Ping bem-sucedido" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "✗ Ping falhou" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "✗ Erro no ping: $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Teste 2: Porta TCP
Write-Host "[2/5] Testando porta TCP 1433..." -ForegroundColor Yellow
try {
    $tcpTest = Test-NetConnection -ComputerName $ServerName.Split('\')[0] -Port 1433 -InformationLevel Quiet
    if ($tcpTest) {
        Write-Host "✓ Porta 1433 acessível" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "✗ Porta 1433 não acessível" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "✗ Erro no teste de porta: $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Teste 3: Conexão SQL
Write-Host "[3/5] Testando conexão SQL..." -ForegroundColor Yellow
try {
    if ([string]::IsNullOrEmpty($Username)) {
        $connectionString = "Server=$ServerName;Database=$Database;Integrated Security=True;Connection Timeout=30;"
    } else {
        $connectionString = "Server=$ServerName;Database=$Database;User Id=$Username;Password=$Password;Connection Timeout=30;"
    }
    
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    Write-Host "✓ Conexão SQL bem-sucedida" -ForegroundColor Green
    $connection.Close()
    $testsPassed++
} catch {
    Write-Host "✗ Erro na conexão SQL: $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Teste 4: Latência
Write-Host "[4/5] Testando latência..." -ForegroundColor Yellow
try {
    $measure = Measure-Command {
        $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $connection.Open()
        $connection.Close()
    }
    $latency = $measure.TotalMilliseconds
    Write-Host "✓ Latência: $([math]::Round($latency, 2)) ms" -ForegroundColor Green
    $testsPassed++
} catch {
    Write-Host "✗ Erro no teste de latência: $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Teste 5: Query de teste
Write-Host "[5/5] Testando query..." -ForegroundColor Yellow
try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT @@VERSION"
    $version = $command.ExecuteScalar()
    Write-Host "✓ Query executada com sucesso" -ForegroundColor Green
    Write-Host "  Versão SQL: $($version.Split('`n')[0])" -ForegroundColor Gray
    $connection.Close()
    $testsPassed++
} catch {
    Write-Host "✗ Erro na query: $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Resumo
Write-Host ""
Write-Host "=== Resumo ===" -ForegroundColor Cyan
Write-Host "Testes passados: $testsPassed/5" -ForegroundColor Green
Write-Host "Testes falhados: $testsFailed/5" -ForegroundColor Red

if ($testsFailed -eq 0) {
    Write-Host ""
    Write-Host "✓ Todos os testes passaram! O SQL Server está configurado corretamente." -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "✗ Alguns testes falharam. Verifique a configuração." -ForegroundColor Red
    exit 1
}
