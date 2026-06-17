[CmdletBinding()]
param(
    [string]$ConfigPath = "",
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot "HomologacaoFisica.Common.psm1") -Force

function Invoke-SqlNonQuery {
    param(
        [string]$ConnectionString,
        [string]$CommandText
    )

    Add-Type -AssemblyName System.Data
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 60
        $command.CommandText = $CommandText
        [void]$command.ExecuteNonQuery()
    }
    finally {
        $connection.Dispose()
    }
}

function Invoke-SqlScalar {
    param(
        [string]$ConnectionString,
        [string]$CommandText
    )

    Add-Type -AssemblyName System.Data
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 60
        $command.CommandText = $CommandText
        return $command.ExecuteScalar()
    }
    finally {
        $connection.Dispose()
    }
}

function New-ConnectionStringFromSettings {
    param(
        [pscustomobject]$Settings
    )

    if ($null -eq $Settings -or [string]$Settings.Provider -ne "SqlServer") {
        return ""
    }

    $host = [string]$Settings.SqlServerHost
    $database = [string]$Settings.SqlServerDatabase
    if ([string]::IsNullOrWhiteSpace($host) -or [string]::IsNullOrWhiteSpace($database)) {
        return ""
    }

    if ($Settings.UseWindowsAuthentication) {
        return "Server=$host;Database=$database;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;Connect Timeout=10;"
    }

    if ([string]::IsNullOrWhiteSpace([string]$Settings.SqlServerUsername) -or [string]::IsNullOrWhiteSpace([string]$Settings.SqlServerPassword)) {
        return ""
    }

    return "Server=$host;Database=$database;User ID=$($Settings.SqlServerUsername);Password=$($Settings.SqlServerPassword);Encrypt=False;TrustServerCertificate=True;Connect Timeout=10;"
}

$projectRoot = Get-HomologacaoProjectRoot -ScriptPath $PSScriptRoot
$config = Import-HomologacaoConfig -Path $ConfigPath -ProjectRoot $projectRoot
$effective = Get-EffectiveDatabaseSettings -Config $config

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $projectRoot ("TestResults\HomologacaoFisica\{0}" -f (Get-Date -Format "yyyy-MM-dd_HH-mm-ss"))
}

$outputDirectory = Join-Path $OutputRoot "SqlNetwork"
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$connectionString = [string]$config.Network.SqlConnectionString
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    $connectionString = New-ConnectionStringFromSettings -Settings $effective.Settings
}

$peerAddress = [string]$config.Network.PeerAddress
$sharedPath = [string]$config.Network.SharedPath
$stationRole = [string]$config.Network.StationRole
$sqlPort = [int]$config.Network.SqlPort
$lockTimeoutSeconds = [int]$config.Network.LockTimeoutSeconds
$retryCount = [int]$config.Network.RetryCount
$checks = New-Object System.Collections.Generic.List[object]

if ([string]::IsNullOrWhiteSpace($peerAddress) -or [string]::IsNullOrWhiteSpace($connectionString)) {
    $checks.Add((New-HomologacaoCheck -Name "PhysicalPrerequisites" -Outcome "Skip" -Details "PeerAddress e SqlConnectionString sao obrigatorios para homologacao em duas maquinas." -Module "Rede" -PossibleCause "Falta segunda maquina ou string de conexao real." -SuggestedFix "Preencher PeerAddress e SqlConnectionString no arquivo de configuracao." ))
    $area = New-HomologacaoAreaResult `
        -AreaId "network-real" `
        -AreaName "SQL Server em duas maquinas reais" `
        -OutputDirectory $outputDirectory `
        -Checks $checks `
        -PreferredIgnoredStatus "IGNORADO POR FALTA DE AMBIENTE FISICO" `
        -Metrics @{
            StationRole = $stationRole
            PeerAddress = $peerAddress
            SharedPath = $sharedPath
        }

    Write-HomologacaoAreaArtifacts -AreaResult $area -JsonPath (Join-Path $OutputRoot "network-real-summary.json") -TextPath (Join-Path $OutputRoot "network-real-summary.txt")
    return
}

$checks.Add((New-HomologacaoCheck -Name "StationRole" -Outcome $(if ($stationRole -in @("Servidor", "Cliente")) { "Pass" } else { "Warn" }) -Details "Papel informado: $stationRole" -Module "Rede" -PossibleCause "Papel da estacao nao padronizado." -SuggestedFix "Usar Servidor ou Cliente para facilitar rastreabilidade da homologacao." ))

try {
    $pingOk = Test-Connection -ComputerName $peerAddress -Count 2 -Quiet
    $checks.Add((New-HomologacaoCheck -Name "PeerPing" -Outcome $(if ($pingOk) { "Pass" } else { "Fail" }) -Details "Peer=$peerAddress" -Module "Rede" -Command "Test-Connection -ComputerName $peerAddress -Count 2 -Quiet" -PossibleCause "Peer desligado, ICMP bloqueado ou rota ausente." -SuggestedFix "Ligar a estacao remota e validar IP/rede." ))
}
catch {
    $checks.Add((New-HomologacaoCheck -Name "PeerPing" -Outcome "Fail" -Details "Falha ao testar ping do peer." -Module "Rede" -ErrorMessage $_.Exception.Message -Command "Test-Connection -ComputerName $peerAddress -Count 2 -Quiet" -PossibleCause "Rede ou DNS indisponivel." -SuggestedFix "Validar conectividade entre as duas estacoes." ))
}

try {
    $tcp = Test-NetConnection -ComputerName $peerAddress -Port $sqlPort -WarningAction SilentlyContinue
    $checks.Add((New-HomologacaoCheck -Name "PeerSqlPort" -Outcome $(if ($tcp.TcpTestSucceeded) { "Pass" } else { "Fail" }) -Details "Peer=$peerAddress Port=$sqlPort Result=$($tcp.TcpTestSucceeded)" -Module "Rede" -Command "Test-NetConnection -ComputerName $peerAddress -Port $sqlPort" -PossibleCause "Firewall ou porta da instancia SQL Server bloqueada." -SuggestedFix "Liberar a porta SQL Server e confirmar a instancia/porta da segunda maquina." ))
}
catch {
    $checks.Add((New-HomologacaoCheck -Name "PeerSqlPort" -Outcome "Fail" -Details "Falha ao testar a porta SQL do peer." -Module "Rede" -ErrorMessage $_.Exception.Message -Command "Test-NetConnection -ComputerName $peerAddress -Port $sqlPort" -PossibleCause "Cmdlet indisponivel ou rede bloqueada." -SuggestedFix "Validar porta manualmente e revisar firewall." ))
}

if (-not [string]::IsNullOrWhiteSpace($sharedPath)) {
    try {
        New-Item -ItemType Directory -Path $sharedPath -Force | Out-Null
        $probeFile = Join-Path $sharedPath ("codex-network-probe-{0}.txt" -f (Get-Date -Format "yyyyMMddHHmmssfff"))
        "network probe" | Set-Content -Path $probeFile -Encoding UTF8
        $readBack = Get-Content -Path $probeFile -Raw
        Remove-Item -Path $probeFile -Force
        $checks.Add((New-HomologacaoCheck -Name "SharedPathReadWrite" -Outcome "Pass" -Details "Compartilhamento OK: $sharedPath; Conteudo=$($readBack.Trim())" -Module "Rede"))
    }
    catch {
        $checks.Add((New-HomologacaoCheck -Name "SharedPathReadWrite" -Outcome "Fail" -Details "Falha em compartilhamento de rede." -Module "Rede" -ErrorMessage $_.Exception.Message -AffectedFile $sharedPath -PossibleCause "UNC inacessivel ou sem permissao de escrita." -SuggestedFix "Validar compartilhamento entre Servidor e Cliente." ))
    }
}
else {
    $checks.Add((New-HomologacaoCheck -Name "SharedPathReadWrite" -Outcome "Warn" -Details "SharedPath nao informado; teste de pasta compartilhada nao executado." -Module "Rede" -PossibleCause "Caminho UNC nao configurado." -SuggestedFix "Informar SharedPath para validar backup e troca de arquivos entre estacoes." ))
}

$probeToken = [guid]::NewGuid().ToString("N")
$probeTableSql = @"
IF OBJECT_ID('dbo.CodexHomologacaoNetworkProbe') IS NULL
BEGIN
    CREATE TABLE dbo.CodexHomologacaoNetworkProbe
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProbeToken NVARCHAR(64) NOT NULL,
        ProbeSource NVARCHAR(64) NOT NULL,
        ProbeValue INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
"@

try {
    Invoke-SqlNonQuery -ConnectionString $connectionString -CommandText $probeTableSql
    $checks.Add((New-HomologacaoCheck -Name "SqlConnectionAndAuth" -Outcome "Pass" -Details ("Conexao estabelecida. Banco atual: " + (Invoke-SqlScalar -ConnectionString $connectionString -CommandText "SELECT DB_NAME();")) -Module "Banco" -Command "System.Data.SqlClient.SqlConnection.Open" ))
}
catch {
    $checks.Add((New-HomologacaoCheck -Name "SqlConnectionAndAuth" -Outcome "Fail" -Details "Falha ao conectar no SQL Server remoto." -Module "Banco" -ErrorMessage $_.Exception.Message -Command "System.Data.SqlClient.SqlConnection.Open" -PossibleCause "String de conexao, autenticacao ou firewall invalidos." -SuggestedFix "Revisar SqlConnectionString e credenciais da instancia remota." ))
}

try {
    $durations = New-Object System.Collections.Generic.List[double]
    foreach ($attempt in 1..5) {
        $started = Get-Date
        [void](Invoke-SqlScalar -ConnectionString $connectionString -CommandText "SELECT COUNT(*) FROM dbo.CodexHomologacaoNetworkProbe;")
        $durations.Add(((Get-Date) - $started).TotalMilliseconds)
    }
    $average = [Math]::Round(($durations | Measure-Object -Average).Average, 2)
    $checks.Add((New-HomologacaoCheck -Name "SqlResponseTime" -Outcome $(if ($average -le 500) { "Pass" } elseif ($average -le 1500) { "Warn" } else { "Fail" }) -Details "Latencia media: $average ms" -Module "Banco" -PossibleCause "Rede lenta, antivirus, DNS ou SQL sobrecarregado." -SuggestedFix "Medir latencia entre estacoes e revisar desempenho da instancia SQL." ))
}
catch {
    $checks.Add((New-HomologacaoCheck -Name "SqlResponseTime" -Outcome "Fail" -Details "Nao foi possivel medir latencia SQL." -Module "Banco" -ErrorMessage $_.Exception.Message -PossibleCause "Conexao instavel ou consulta falhou." -SuggestedFix "Corrigir conectividade SQL antes de medir latencia." ))
}

try {
    $insertJobs = @()
    foreach ($source in 1..4) {
        $insertJobs += Start-Job -ScriptBlock {
            param($cs, $token, $sourceId)
            Add-Type -AssemblyName System.Data
            $connection = New-Object System.Data.SqlClient.SqlConnection $cs
            $connection.Open()
            try {
                foreach ($index in 1..5) {
                    $command = $connection.CreateCommand()
                    $command.CommandText = "INSERT INTO dbo.CodexHomologacaoNetworkProbe (ProbeToken, ProbeSource, ProbeValue) VALUES (@Token, @Source, @Value);"
                    [void]$command.Parameters.AddWithValue("@Token", $token)
                    [void]$command.Parameters.AddWithValue("@Source", "writer-$sourceId")
                    [void]$command.Parameters.AddWithValue("@Value", $index)
                    [void]$command.ExecuteNonQuery()
                }
            }
            finally {
                $connection.Dispose()
            }
            return "writer-$sourceId"
        } -ArgumentList $connectionString, $probeToken, $source
    }

    $null = Wait-Job -Job $insertJobs
    $insertOutput = Receive-Job -Job $insertJobs
    Remove-Job -Job $insertJobs -Force
    $insertCount = [int](Invoke-SqlScalar -ConnectionString $connectionString -CommandText "SELECT COUNT(*) FROM dbo.CodexHomologacaoNetworkProbe WHERE ProbeToken = '$probeToken';")
    $checks.Add((New-HomologacaoCheck -Name "SimultaneousWrites" -Outcome $(if ($insertCount -ge 20) { "Pass" } else { "Fail" }) -Details "Linhas gravadas=$insertCount; Jobs=$($insertOutput -join ', ')" -Module "Banco" -PossibleCause "Falha em concorrencia de escrita." -SuggestedFix "Revisar isolamento/transacoes da instancia SQL e latencia de rede." ))
}
catch {
    $checks.Add((New-HomologacaoCheck -Name "SimultaneousWrites" -Outcome "Fail" -Details "Falha ao validar gravacao simultanea." -Module "Banco" -ErrorMessage $_.Exception.Message -PossibleCause "Erro de concorrencia, permissao ou conectividade." -SuggestedFix "Inspecionar logs SQL e permissao de escrita da base." ))
}

try {
    $readJobs = @()
    foreach ($readerId in 1..4) {
        $readJobs += Start-Job -ScriptBlock {
            param($cs)
            Add-Type -AssemblyName System.Data
            $measurements = New-Object System.Collections.Generic.List[double]
            $connection = New-Object System.Data.SqlClient.SqlConnection $cs
            $connection.Open()
            try {
                foreach ($attempt in 1..5) {
                    $started = Get-Date
                    $command = $connection.CreateCommand()
                    $command.CommandText = "SELECT COUNT(*) FROM dbo.CodexHomologacaoNetworkProbe;"
                    [void]$command.ExecuteScalar()
                    $measurements.Add(((Get-Date) - $started).TotalMilliseconds)
                }
            }
            finally {
                $connection.Dispose()
            }
            return [Math]::Round(($measurements | Measure-Object -Average).Average, 2)
        } -ArgumentList $connectionString
    }

    $null = Wait-Job -Job $readJobs
    $readLatencies = @(Receive-Job -Job $readJobs)
    Remove-Job -Job $readJobs -Force
    $readAverage = [Math]::Round(($readLatencies | Measure-Object -Average).Average, 2)
    $checks.Add((New-HomologacaoCheck -Name "SimultaneousReads" -Outcome $(if ($readAverage -le 700) { "Pass" } elseif ($readAverage -le 2000) { "Warn" } else { "Fail" }) -Details "Latencia media dos leitores: $readAverage ms" -Module "Banco" -PossibleCause "Leitura concorrente lenta ou bloqueios." -SuggestedFix "Revisar indices, I/O do servidor e latencia entre estacoes." ))
}
catch {
    $checks.Add((New-HomologacaoCheck -Name "SimultaneousReads" -Outcome "Fail" -Details "Falha ao validar leitura simultanea." -Module "Banco" -ErrorMessage $_.Exception.Message -PossibleCause "Jobs falharam ou SQL indisponivel." -SuggestedFix "Reexecutar com SQL Server estavel e revisar logs do banco." ))
}

try {
    $lockToken = "LOCK-" + [guid]::NewGuid().ToString("N")
    Invoke-SqlNonQuery -ConnectionString $connectionString -CommandText "INSERT INTO dbo.CodexHomologacaoNetworkProbe (ProbeToken, ProbeSource, ProbeValue) VALUES ('$lockToken', 'lock-seed', 1);"

    $holder = Start-Job -ScriptBlock {
        param($cs, $token)
        Add-Type -AssemblyName System.Data
        $connection = New-Object System.Data.SqlClient.SqlConnection $cs
        $connection.Open()
        $transaction = $connection.BeginTransaction()
        try {
            $command = $connection.CreateCommand()
            $command.Transaction = $transaction
            $command.CommandText = "UPDATE dbo.CodexHomologacaoNetworkProbe SET ProbeValue = ProbeValue + 1 WHERE ProbeToken = '$token'; WAITFOR DELAY '00:00:05';"
            [void]$command.ExecuteNonQuery()
            $transaction.Commit()
            return "holder-committed"
        }
        catch {
            $transaction.Rollback()
            throw
        }
        finally {
            $connection.Dispose()
        }
    } -ArgumentList $connectionString, $lockToken

    Start-Sleep -Seconds 1

    $contender = Start-Job -ScriptBlock {
        param($cs, $token, $timeoutSeconds)
        Add-Type -AssemblyName System.Data
        $connection = New-Object System.Data.SqlClient.SqlConnection $cs
        $connection.Open()
        try {
            $command = $connection.CreateCommand()
            $command.CommandTimeout = [Math]::Max(5, $timeoutSeconds + 2)
            $command.CommandText = "SET LOCK_TIMEOUT $($timeoutSeconds * 1000); UPDATE dbo.CodexHomologacaoNetworkProbe SET ProbeValue = ProbeValue + 1 WHERE ProbeToken = '$token';"
            [void]$command.ExecuteNonQuery()
            return "unexpected-success"
        }
        catch {
            return $_.Exception.Message
        }
        finally {
            $connection.Dispose()
        }
    } -ArgumentList $connectionString, $lockToken, $lockTimeoutSeconds

    $null = Wait-Job -Job @($holder, $contender)
    $holderResult = Receive-Job -Job $holder
    $contenderResult = Receive-Job -Job $contender
    Remove-Job -Job @($holder, $contender) -Force

    $retrySucceeded = $false
    for ($attempt = 1; $attempt -le $retryCount; $attempt++) {
        try {
            Invoke-SqlNonQuery -ConnectionString $connectionString -CommandText "UPDATE dbo.CodexHomologacaoNetworkProbe SET ProbeValue = ProbeValue + 1 WHERE ProbeToken = '$lockToken';"
            $retrySucceeded = $true
            break
        }
        catch {
            Start-Sleep -Milliseconds 300
        }
    }

    $lockBlocked = ($contenderResult -join " ") -match "lock|timeout|deadlock"
    $checks.Add((New-HomologacaoCheck -Name "ConcurrencyAndLocks" -Outcome $(if ($lockBlocked -and $retrySucceeded -and ($holderResult -join " ") -match "holder-committed") { "Pass" } else { "Fail" }) -Details "Bloqueio detectado=$lockBlocked; RetryOk=$retrySucceeded; Holder=$($holderResult -join ', '); Contender=$($contenderResult -join ', ')" -Module "Banco" -PossibleCause "Controle de lock inconsistente ou sem retry apos liberacao." -SuggestedFix "Revisar transacoes concorrentes e politica de retry na aplicacao." ))
}
catch {
    $checks.Add((New-HomologacaoCheck -Name "ConcurrencyAndLocks" -Outcome "Fail" -Details "Falha ao validar bloqueios e concorrencia." -Module "Banco" -ErrorMessage $_.Exception.Message -PossibleCause "Erro na rotina de lock ou SQL indisponivel." -SuggestedFix "Inspecionar isolamento, lock timeout e saude do SQL Server." ))
}

try {
    Add-Type -AssemblyName System.Data
    $connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
    $connection.Open()
    $connection.Close()
    $connection.Open()
    [void](Invoke-SqlScalar -ConnectionString $connectionString -CommandText "SELECT 1;")
    $checks.Add((New-HomologacaoCheck -Name "Reconnect" -Outcome "Pass" -Details "Reconexao logica validada apos fechamento de sessao." -Module "Rede"))
}
catch {
    $checks.Add((New-HomologacaoCheck -Name "Reconnect" -Outcome "Fail" -Details "Falha ao reconectar apos fechamento de sessao." -Module "Rede" -ErrorMessage $_.Exception.Message -PossibleCause "Pool, firewall ou credencial instavel." -SuggestedFix "Validar estabilidade de reconexao da instancia SQL Server." ))
}

$checks.Add((New-HomologacaoCheck -Name "ConnectionLossSimulation" -Outcome "Warn" -Details "A perda fisica de link nao foi automatizada por script; somente reconexao logica foi validada." -Module "Rede" -PossibleCause "A suite nao desconecta cabo/VPN automaticamente." -SuggestedFix "Executar uma rodada manual desligando a interface de rede durante uma gravacao real." ))

$area = New-HomologacaoAreaResult `
    -AreaId "network-real" `
    -AreaName "SQL Server em duas maquinas reais" `
    -OutputDirectory $outputDirectory `
    -Checks $checks `
    -Metrics @{
        StationRole = $stationRole
        PeerAddress = $peerAddress
        SqlPort = $sqlPort
        SharedPath = $sharedPath
    } `
    -Notes @(
        "A perda fisica de conectividade continua exigindo uma rodada manual com cabo, Wi-Fi ou VPN sendo interrompidos."
    )

Write-HomologacaoAreaArtifacts -AreaResult $area -JsonPath (Join-Path $OutputRoot "network-real-summary.json") -TextPath (Join-Path $OutputRoot "network-real-summary.txt")

