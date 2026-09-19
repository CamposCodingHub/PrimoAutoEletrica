#Requires -Version 5.1
<#
.SYNOPSIS
  Remove produtos/estoque de seed Smoke/Teste com seguranca (backup opcional).
#>
param(
    [string]$DatabasePath = "",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

function Resolve-DbPath {
    param([string]$Path)
    if (-not [string]::IsNullOrWhiteSpace($Path) -and (Test-Path -LiteralPath $Path)) {
        return (Resolve-Path -LiteralPath $Path).Path
    }

    $candidates = @(
        (Join-Path $PSScriptRoot "..\PrimoAutoEletrica\bin\Release\net9.0-windows\primoauto.db"),
        (Join-Path $PSScriptRoot "..\PrimoAutoEletrica\bin\Debug\net9.0-windows\primoauto.db"),
        (Join-Path $env:LOCALAPPDATA "PrimoAutoEletrica\primoauto.db"),
        (Join-Path $env:APPDATA "PrimoAutoEletrica\primoauto.db")
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

    if (-not $candidates) {
        throw "Banco primoauto.db nao encontrado. Informe -DatabasePath."
    }

    return $candidates[0]
}

Add-Type -AssemblyName System.Data

$db = Resolve-DbPath -Path $DatabasePath
Write-Host "Banco: $db"

$backup = "$db.smoke-cleanup-$(Get-Date -Format 'yyyyMMdd-HHmmss').bak"
Copy-Item -LiteralPath $db -Destination $backup -Force
Write-Host "Backup: $backup"

$cs = "Data Source=$db;Mode=ReadWrite;Cache=Shared"
$con = New-Object Microsoft.Data.Sqlite.SqliteConnection $cs
# Fallback to System.Data.SQLite style if Microsoft.Data.Sqlite assembly missing:
try {
    $con.Open()
} catch {
    # Use sqlite3 CLI if available
    $sqlite = Get-Command sqlite3 -ErrorAction SilentlyContinue
    if (-not $sqlite) { throw "Nao foi possivel abrir o SQLite ($($_.Exception.Message)). Instale Microsoft.Data.Sqlite ou sqlite3." }

    $selectSql = @"
SELECT Id, Codigo, Nome FROM Produtos
WHERE IFNULL(IsDeleted,0)=0 AND (
  Codigo LIKE 'SMK%' OR Codigo LIKE '%SMOKE%' OR
  Nome LIKE '%Smoke%' OR Nome LIKE '%smoke%' OR
  Nome LIKE '%Teste%' OR Nome LIKE '%teste%' OR
  Nome LIKE 'Produto Estoque %'
);
"@
    $rows = & sqlite3 -separator '|' $db $selectSql
    if (-not $rows) {
        Write-Host "Nenhum produto Smoke/Teste encontrado."
        return
    }
    Write-Host "Encontrados $($rows.Count) produtos:"
    $rows | ForEach-Object { Write-Host "  $_" }
    if ($WhatIf) { return }

    $ids = $rows | ForEach-Object { ($_ -split '\|')[0] }
    foreach ($id in $ids) {
        & sqlite3 $db "UPDATE Produtos SET Ativo=0, IsDeleted=1, ExcluidoEm=datetime('now'), ExcluidoPor='SmokeCleanup' WHERE Id='$id';"
    }
    Write-Host "Soft-delete concluido em $($ids.Count) produtos."
    return
}
