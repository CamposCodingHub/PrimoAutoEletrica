[CmdletBinding()]
param(
    [string]$ConfigPath = "",
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot "HomologacaoFisica.Common.psm1") -Force

function New-BackupRestoreAppData {
    param(
        [string]$Directory,
        [string]$Provider,
        [string]$SqlitePath,
        [pscustomobject]$Settings
    )

    New-Item -ItemType Directory -Path $Directory -Force | Out-Null
    $payload = [ordered]@{
        Provider = $Provider
        SQLitePath = $SqlitePath
        SqlServerHost = if ($null -ne $Settings) { [string]$Settings.SqlServerHost } else { "" }
        SqlServerInstance = if ($null -ne $Settings) { [string]$Settings.SqlServerInstance } else { "" }
        SqlServerDatabase = if ($null -ne $Settings) { [string]$Settings.SqlServerDatabase } else { "" }
        UseWindowsAuthentication = if ($null -ne $Settings) { [bool]$Settings.UseWindowsAuthentication } else { $true }
        SqlServerUsername = if ($null -ne $Settings) { [string]$Settings.SqlServerUsername } else { "" }
        SqlServerPassword = if ($null -ne $Settings) { [string]$Settings.SqlServerPassword } else { "" }
        AllowUnsupportedSqlServerRuntimeFallback = $false
        EncryptSqlServerConnection = $false
        TrustSqlServerCertificate = $true
        CommandTimeoutSeconds = 30
        SessionInactivityTimeoutMinutes = 30
        NetworkBackupDirectory = ""
    }

    $payload | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $Directory "database-settings.json") -Encoding UTF8
}

function Invoke-SqlScalarValue {
    param(
        [string]$ConnectionString,
        [string]$CommandText
    )

    Add-Type -AssemblyName System.Data
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 120
        $command.CommandText = $CommandText
        return $command.ExecuteScalar()
    }
    finally {
        $connection.Dispose()
    }
}

function Invoke-SqlDataTable {
    param(
        [string]$ConnectionString,
        [string]$CommandText
    )

    Add-Type -AssemblyName System.Data
    $table = New-Object System.Data.DataTable
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 300
        $command.CommandText = $CommandText
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
        [void]$adapter.Fill($table)
    }
    finally {
        $connection.Dispose()
    }

    return $table
}

function Invoke-SqlNonQueryValue {
    param(
        [string]$ConnectionString,
        [string]$CommandText
    )

    Add-Type -AssemblyName System.Data
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 300
        $command.CommandText = $CommandText
        [void]$command.ExecuteNonQuery()
    }
    finally {
        $connection.Dispose()
    }
}

$projectRoot = Get-HomologacaoProjectRoot -ScriptPath $PSScriptRoot
$config = Import-HomologacaoConfig -Path $ConfigPath -ProjectRoot $projectRoot
$effective = Get-EffectiveDatabaseSettings -Config $config

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $projectRoot ("TestResults\HomologacaoFisica\{0}" -f (Get-Date -Format "yyyy-MM-dd_HH-mm-ss"))
}

$outputDirectory = Join-Path $OutputRoot "BackupRestore"
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$checks = New-Object System.Collections.Generic.List[object]
$provider = if ($null -ne $effective.Settings) { [string]$effective.Settings.Provider } else { "SQLite" }
$criticalTables = @($config.Backup.CriticalTables)

if ($provider -eq "SqlServer") {
    $connectionString = [string]$config.Network.SqlConnectionString
    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        if ($effective.Settings.UseWindowsAuthentication) {
            $connectionString = "Server=$($effective.Settings.SqlServerHost);Database=$($effective.Settings.SqlServerDatabase);Integrated Security=True;Encrypt=False;TrustServerCertificate=True;Connect Timeout=10;"
        }
        elseif (-not [string]::IsNullOrWhiteSpace([string]$effective.Settings.SqlServerUsername) -and -not [string]::IsNullOrWhiteSpace([string]$effective.Settings.SqlServerPassword)) {
            $connectionString = "Server=$($effective.Settings.SqlServerHost);Database=$($effective.Settings.SqlServerDatabase);User ID=$($effective.Settings.SqlServerUsername);Password=$($effective.Settings.SqlServerPassword);Encrypt=False;TrustServerCertificate=True;Connect Timeout=10;"
        }
    }

    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        $checks.Add((New-HomologacaoCheck -Name "SqlConnectionString" -Outcome "Fail" -Details "Nao foi possivel montar a string de conexao do SQL Server." -Module "Backup" -PossibleCause "Configuracao do banco incompleta." -SuggestedFix "Preencher SqlConnectionString ou credenciais SQL Server no arquivo de configuracao." ))
    }
    else {
        $backupPath = Join-Path $outputDirectory ("PrimoAutoEletrica_Backup_{0}.bak" -f (Get-Date -Format "yyyyMMddHHmmss"))
        $restoreDb = "PrimoAutoEletrica_HomologacaoRestore_" + (Get-Date -Format "yyyyMMddHHmmss")

        try {
            $dbName = [string](Invoke-SqlScalarValue -ConnectionString $connectionString -CommandText "SELECT DB_NAME();")
            Invoke-SqlNonQueryValue -ConnectionString $connectionString -CommandText "BACKUP DATABASE [$dbName] TO DISK = N'$backupPath' WITH INIT, CHECKSUM;"
            $checks.Add((New-HomologacaoCheck -Name "SqlBackupCreate" -Outcome $(if (Test-Path $backupPath) { "Pass" } else { "Fail" }) -Details $backupPath -Module "Backup" -AffectedFile $backupPath -PossibleCause "Falha na escrita do arquivo .bak." -SuggestedFix "Revisar permissao da pasta de backup e espaco em disco." ))

            $fileList = Invoke-SqlDataTable -ConnectionString $connectionString -CommandText "RESTORE FILELISTONLY FROM DISK = N'$backupPath';"
            $defaultDataPath = [string](Invoke-SqlScalarValue -ConnectionString $connectionString -CommandText "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(4000));")
            $defaultLogPath = [string](Invoke-SqlScalarValue -ConnectionString $connectionString -CommandText "SELECT CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS NVARCHAR(4000));")
            $dataLogical = [string]($fileList.Rows | Where-Object { $_.Type -eq 'D' } | Select-Object -First 1).LogicalName
            $logLogical = [string]($fileList.Rows | Where-Object { $_.Type -eq 'L' } | Select-Object -First 1).LogicalName
            $restoredData = Join-Path $defaultDataPath "$restoreDb.mdf"
            $restoredLog = Join-Path $defaultLogPath "${restoreDb}_log.ldf"

            $restoreSql = @"
IF DB_ID(N'$restoreDb') IS NOT NULL
BEGIN
    ALTER DATABASE [$restoreDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$restoreDb];
END;
RESTORE DATABASE [$restoreDb]
FROM DISK = N'$backupPath'
WITH MOVE N'$dataLogical' TO N'$restoredData',
     MOVE N'$logLogical' TO N'$restoredLog',
     REPLACE, RECOVERY, CHECKSUM;
DBCC CHECKDB([$restoreDb]) WITH NO_INFOMSGS;
"@
            Invoke-SqlNonQueryValue -ConnectionString $connectionString -CommandText $restoreSql
            $checks.Add((New-HomologacaoCheck -Name "SqlRestoreToTemporaryDb" -Outcome "Pass" -Details "Banco temporario restaurado: $restoreDb" -Module "Backup"))

            $originalCounts = New-Object System.Collections.Generic.List[string]
            $restoredCounts = New-Object System.Collections.Generic.List[string]
            foreach ($tableName in $criticalTables) {
                try {
                    $originalCount = Invoke-SqlScalarValue -ConnectionString $connectionString -CommandText "SELECT COUNT(*) FROM [$tableName];"
                    $restoredCount = Invoke-SqlScalarValue -ConnectionString ($connectionString -replace "Database=[^;]+", "Database=$restoreDb") -CommandText "SELECT COUNT(*) FROM [$tableName];"
                    $originalCounts.Add("{0}={1}" -f $tableName, $originalCount)
                    $restoredCounts.Add("{0}={1}" -f $tableName, $restoredCount)
                }
                catch {
                }
            }

            $checks.Add((New-HomologacaoCheck -Name "SqlCriticalTableCounts" -Outcome $(if (($originalCounts -join '; ') -eq ($restoredCounts -join '; ')) { "Pass" } else { "Warn" }) -Details "Original=[$($originalCounts -join '; ')]; Restored=[$($restoredCounts -join '; ')]" -Module "Backup" -PossibleCause "Tabela critica ausente no snapshot restaurado." -SuggestedFix "Revisar job de backup, schema e integridade do restore." ))

            $restoreAppData = Join-Path $outputDirectory "SqlRestoreAppData"
            New-BackupRestoreAppData -Directory $restoreAppData -Provider "SqlServer" -SqlitePath "" -Settings ([pscustomobject]@{
                SqlServerHost = $effective.Settings.SqlServerHost
                SqlServerInstance = $effective.Settings.SqlServerInstance
                SqlServerDatabase = $restoreDb
                UseWindowsAuthentication = $effective.Settings.UseWindowsAuthentication
                SqlServerUsername = $effective.Settings.SqlServerUsername
                SqlServerPassword = $effective.Settings.SqlServerPassword
            })
            $smoke = Invoke-UiSmokeRun -ProjectRoot $projectRoot -Build $config.Build -OutputDirectory (Join-Path $outputDirectory "SqlRestoreSmoke") -SmokeFilter "MainWindow" -AppDataPath $restoreAppData
            $checks.Add((New-HomologacaoCheck -Name "SqlRestoredDatabaseOpensInApp" -Outcome $(if ($smoke.Status -eq "APROVADO") { "Pass" } else { "Fail" }) -Details "Smoke MainWindow em banco restaurado: $($smoke.Status)" -Module "Backup" -Command $smoke.CommandLine -AffectedFile $smoke.ReportPath -PossibleCause "Banco restaurado nao abriu no aplicativo." -SuggestedFix "Validar compatibilidade do schema SQL Server com a versao atual do app." ))

            try {
                Invoke-SqlNonQueryValue -ConnectionString $connectionString -CommandText "ALTER DATABASE [$restoreDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$restoreDb];"
            }
            catch {
            }
        }
        catch {
            $checks.Add((New-HomologacaoCheck -Name "SqlBackupRestore" -Outcome "Fail" -Details "Falha ao executar backup/restore SQL Server." -Module "Backup" -ErrorMessage $_.Exception.Message -PossibleCause "Permissao SQL insuficiente ou path de data/log nao acessivel." -SuggestedFix "Conceder permissoes de BACKUP/RESTORE e revisar paths default da instancia." ))
        }
    }
}
else {
    $sqlitePath = if ($null -ne $effective.Settings) {
        $path = [string]$effective.Settings.SQLitePath
        if ([System.IO.Path]::IsPathRooted($path)) { $path } else { Join-Path $effective.AppDataPath $path }
    }
    else {
        Join-Path $effective.AppDataPath "primoauto.db"
    }

    if (-not (Test-Path $sqlitePath)) {
        $checks.Add((New-HomologacaoCheck -Name "SqliteSourceDatabase" -Outcome "Fail" -Details "Banco SQLite nao encontrado em $sqlitePath" -Module "Backup" -AffectedFile $sqlitePath -PossibleCause "AppData incorreto ou sistema ainda nao inicializado." -SuggestedFix "Executar o sistema uma vez ou apontar AppDataPath valido no arquivo de configuracao." ))
    }
    else {
        $pythonScript = @'
import hashlib
import json
import os
import shutil
import sqlite3
import sys

source_path = sys.argv[1]
backup_path = sys.argv[2]
restore_path = sys.argv[3]
critical_tables = [item for item in sys.argv[4].split(",") if item]

def snapshot(db_path, critical):
    conn = sqlite3.connect(db_path)
    conn.execute("PRAGMA wal_checkpoint(FULL);")
    integrity = conn.execute("PRAGMA integrity_check;").fetchone()[0]
    tables = [row[0] for row in conn.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;")]
    counts = {}
    hashes = {}
    for table_name in tables:
        quoted = '"' + table_name.replace('"', '""') + '"'
        counts[table_name] = conn.execute(f"SELECT COUNT(*) FROM {quoted};").fetchone()[0]
        if table_name in critical:
            rows = conn.execute(f"SELECT * FROM {quoted} ORDER BY 1;").fetchall()
            hashes[table_name] = hashlib.sha256(repr(rows).encode("utf-8")).hexdigest()
    conn.close()
    return {"integrity": integrity, "counts": counts, "hashes": hashes}

shutil.copy2(source_path, backup_path)
shutil.copy2(backup_path, restore_path)

result = {
    "source": snapshot(source_path, critical_tables),
    "backup_exists": os.path.exists(backup_path),
    "restore_exists": os.path.exists(restore_path),
    "restored": snapshot(restore_path, critical_tables),
}

print(json.dumps(result))
'@
        $pythonFile = New-TemporaryPythonFile -Directory $outputDirectory -Prefix "sqlite-backup-restore" -Content $pythonScript
        $backupPath = Join-Path $outputDirectory ("PrimoAutoEletrica_Backup_{0}.db" -f (Get-Date -Format "yyyyMMddHHmmss"))
        $restorePath = Join-Path $outputDirectory "restored-temp.db"
        $pythonRun = Invoke-HomologacaoCommand -FilePath "python" -Arguments @($pythonFile, $sqlitePath, $backupPath, $restorePath, ($criticalTables -join ",")) -WorkingDirectory $outputDirectory -LogPath (Join-Path $outputDirectory "sqlite-backup-restore.log") -TimeoutSeconds 180

        if ($pythonRun.ExitCode -ne 0) {
            $checks.Add((New-HomologacaoCheck -Name "SqliteBackupRestore" -Outcome "Fail" -Details "Rotina Python de backup/restore falhou." -Module "Backup" -ErrorMessage ($pythonRun.Output -join "`n") -Command $pythonRun.CommandLine -AffectedFile $pythonRun.LogPath -PossibleCause "Python indisponivel ou base SQLite inconsistente." -SuggestedFix "Instalar Python 3 e revisar integridade da base SQLite." ))
        }
        else {
            $probe = ($pythonRun.Output -join "`n") | ConvertFrom-Json
            $countsEqual = (($probe.source.counts | ConvertTo-Json -Compress) -eq ($probe.restored.counts | ConvertTo-Json -Compress))
            $hashesEqual = (($probe.source.hashes | ConvertTo-Json -Compress) -eq ($probe.restored.hashes | ConvertTo-Json -Compress))
            $integrityOk = ($probe.source.integrity -eq "ok" -and $probe.restored.integrity -eq "ok")

            $checks.Add((New-HomologacaoCheck -Name "SqliteBackupFileCreated" -Outcome $(if ($probe.backup_exists) { "Pass" } else { "Fail" }) -Details $backupPath -Module "Backup" -AffectedFile $backupPath -PossibleCause "Arquivo .db de backup nao gerado." -SuggestedFix "Validar escrita da pasta de backup e bloqueios no arquivo SQLite." ))
            $checks.Add((New-HomologacaoCheck -Name "SqliteRestoreFileCreated" -Outcome $(if ($probe.restore_exists) { "Pass" } else { "Fail" }) -Details $restorePath -Module "Backup" -AffectedFile $restorePath -PossibleCause "Arquivo restaurado nao foi materializado." -SuggestedFix "Validar espaco em disco e permissao de escrita." ))
            $checks.Add((New-HomologacaoCheck -Name "SqliteIntegrity" -Outcome $(if ($integrityOk) { "Pass" } else { "Fail" }) -Details "Source=$($probe.source.integrity); Restored=$($probe.restored.integrity)" -Module "Backup" -PossibleCause "PRAGMA integrity_check retornou erro." -SuggestedFix "Executar analise de corrupcao e substituir a base por um backup valido." ))
            $checks.Add((New-HomologacaoCheck -Name "SqliteCriticalCounts" -Outcome $(if ($countsEqual) { "Pass" } else { "Fail" }) -Details "Contagens de tabelas criticas comparadas entre origem e restore." -Module "Backup" -PossibleCause "Linhas divergentes entre origem e restaurado." -SuggestedFix "Revisar checkpoint WAL e processo de copia do banco." ))
            $checks.Add((New-HomologacaoCheck -Name "SqliteCriticalHashes" -Outcome $(if ($hashesEqual) { "Pass" } else { "Warn" }) -Details "Hashes agregados de dados criticos comparados." -Module "Backup" -PossibleCause "Diferenca de ordenacao, schema ou dados criticos." -SuggestedFix "Conferir tabelas indicadas no resumo JSON e reexecutar comparacao." ))

            $restoreAppData = Join-Path $outputDirectory "RestoreAppData"
            New-BackupRestoreAppData -Directory $restoreAppData -Provider "SQLite" -SqlitePath $restorePath -Settings $effective.Settings
            $smoke = Invoke-UiSmokeRun -ProjectRoot $projectRoot -Build $config.Build -OutputDirectory (Join-Path $outputDirectory "RestoreSmoke") -SmokeFilter "MainWindow" -AppDataPath $restoreAppData
            $checks.Add((New-HomologacaoCheck -Name "RestoredDatabaseOpensInApp" -Outcome $(if ($smoke.Status -eq "APROVADO") { "Pass" } else { "Fail" }) -Details "Smoke MainWindow em base restaurada: $($smoke.Status)" -Module "Backup" -Command $smoke.CommandLine -AffectedFile $smoke.ReportPath -PossibleCause "Base restaurada abre mas derruba a UI principal." -SuggestedFix "Revisar compatibilidade de schema, migracoes pendentes e integridade da base restaurada." ))
        }
    }
}

$area = New-HomologacaoAreaResult `
    -AreaId "backup-restore" `
    -AreaName "Backup e restauracao real" `
    -OutputDirectory $outputDirectory `
    -Checks $checks `
    -Metrics @{
        Provider = $provider
        SettingsPath = $effective.SettingsPath
        SourceAppData = $effective.AppDataPath
    }

Write-HomologacaoAreaArtifacts -AreaResult $area -JsonPath (Join-Path $OutputRoot "backup-restore-summary.json") -TextPath (Join-Path $OutputRoot "backup-restore-summary.txt")
