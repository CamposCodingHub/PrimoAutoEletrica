[CmdletBinding()]
param(
    [string]$Server = "(localdb)\MSSQLLocalDB",
    [string]$Database = "PrimoAutoEletrica_RuntimeAudit",
    [switch]$KeepDatabase
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$schemaPath = Join-Path $projectRoot "PrimoAutoEletrica\Services\DatabaseProviders\SqlServerSchema.sql"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$outputDir = Join-Path $projectRoot "TestResults\SqlServerSchema\$timestamp"
$logPath = Join-Path $outputDir "sqlserver-schema.log"
$summaryPath = Join-Path $outputDir "sqlserver-schema-summary.json"

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

function Write-SchemaLog {
    param([string]$Message)
    $entry = "{0} {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Message
    Add-Content -Path $logPath -Value $entry -Encoding UTF8
    Write-Host $entry
}

function Invoke-SqlCmdChecked {
    param(
        [string[]]$Arguments,
        [string]$Step
    )

    Write-SchemaLog "Executando ${Step}: sqlcmd $($Arguments -join ' ')"
    & sqlcmd @Arguments 2>&1 | Tee-Object -FilePath $logPath -Append | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "$Step falhou com exit code $LASTEXITCODE."
    }
}

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "sqlcmd nao encontrado. Instale SQL Server command line tools."
}

if (-not (Test-Path $schemaPath)) {
    throw "Schema SQL Server nao encontrado: $schemaPath"
}

if ($Server -like "*(localdb)*") {
    if (Get-Command sqllocaldb -ErrorAction SilentlyContinue) {
        Write-SchemaLog "Iniciando LocalDB MSSQLLocalDB se necessario."
        & sqllocaldb start MSSQLLocalDB | Tee-Object -FilePath $logPath -Append | Out-Null
    }
}

$dropAndCreate = @"
IF DB_ID('$Database') IS NOT NULL
BEGIN
    ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$Database];
END;
CREATE DATABASE [$Database];
"@

Invoke-SqlCmdChecked -Step "CreateDatabase" -Arguments @("-b", "-S", $Server, "-Q", $dropAndCreate)
Invoke-SqlCmdChecked -Step "ApplySchema" -Arguments @("-b", "-S", $Server, "-d", $Database, "-i", $schemaPath)

$validationQuery = @"
SET NOCOUNT ON;
DECLARE @Missing TABLE (Name sysname NOT NULL);

INSERT INTO @Missing (Name)
SELECT expected.Name
FROM
(
    VALUES
    ('Funcionarios'),
    ('Clientes'),
    ('Produtos'),
    ('Fornecedores'),
    ('Vendas'),
    ('VendaItens'),
    ('Veiculos'),
    ('OrdensServico'),
    ('OrdemServicoItens'),
    ('OrdemServicoEventos'),
    ('AuditLogs'),
    ('Permissoes'),
    ('PerfisAcesso'),
    ('PerfilPermissoes'),
    ('LoginTentativasSeguranca'),
    ('UserSessions'),
    ('RecordLocks'),
    ('SystemEvents')
) AS expected(Name)
WHERE NOT EXISTS
(
    SELECT 1
    FROM sys.tables t
    WHERE t.name = expected.Name
);

IF EXISTS (SELECT 1 FROM @Missing)
BEGIN
    SELECT 'MISSING_TABLE' AS Issue, Name FROM @Missing;
    THROW 51000, 'Schema SQL Server incompleto.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RecordLocks_Entity_Active')
BEGIN
    THROW 51001, 'Indice UX_RecordLocks_Entity_Active ausente.', 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys fk
    WHERE fk.name IS NOT NULL
      AND OBJECT_NAME(fk.parent_object_id) = 'Veiculos'
      AND OBJECT_NAME(fk.referenced_object_id) = 'Clientes'
)
BEGIN
    THROW 51002, 'FK Veiculos -> Clientes ausente.', 1;
END;

SELECT 'OK' AS Status, COUNT(*) AS TablesCreated
FROM sys.tables;
"@

Invoke-SqlCmdChecked -Step "ValidateSchema" -Arguments @("-b", "-S", $Server, "-d", $Database, "-Q", $validationQuery)

if (-not $KeepDatabase) {
    Invoke-SqlCmdChecked -Step "DropDatabase" -Arguments @("-b", "-S", $Server, "-Q", "ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$Database];")
}

$summary = [ordered]@{
    GeneratedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Status = "APROVADO"
    Server = $Server
    Database = $Database
    SchemaPath = $schemaPath
    LogPath = $logPath
    DatabaseKept = [bool]$KeepDatabase
}

$summary | ConvertTo-Json -Depth 4 | Set-Content -Path $summaryPath -Encoding UTF8

[PSCustomObject]@{
    Status = "APROVADO"
    Summary = $summaryPath
    Log = $logPath
}
