<#
.SYNOPSIS
  Procedimento controlado para limpeza de instalacao legada "Primo Auto Eleétrica".

.DESCRIPTION
  DEVELOPMENT/OPS helper — NAO apaga banco de usuario.
  1) Inventaria
  2) Backup de %LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db
  3) Uninstall via unins000.exe (elevado)
  4) Remove restos de Program Files se o uninstaller deixar
  5) Nao remove LocalAppData\App (DEVELOPMENT) nem o banco

.EXAMPLE
  .\Scripts\Cleanup-LegacyPrimoInstall.ps1 -WhatIf
  .\Scripts\Cleanup-LegacyPrimoInstall.ps1 -ConfirmCleanup
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$ConfirmCleanup,
    [string]$LegacyDir = "C:\Program Files\Primo Auto Elétrica"
)

$ErrorActionPreference = "Stop"
Write-Host "=== PRIMOX Legacy Cleanup ==="
Write-Host "LegacyDir: $LegacyDir"

$prodDb = Join-Path $env:LOCALAPPDATA "PrimoAutoEletrica\primoauto.db"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = Join-Path $env:LOCALAPPDATA "PrimoAutoEletrica\Backups\LegacyCleanup-$stamp"
New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null

if (Test-Path -LiteralPath $prodDb) {
    $copy = Join-Path $backupRoot "primoauto-official-copy.db"
    Copy-Item -LiteralPath $prodDb $copy -Force
    $hash = (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash
    Write-Host "Backup DB: $copy"
    Write-Host "SHA256: $hash"
    @"
Stamp=$stamp
OfficialDbCopy=$copy
SHA256=$hash
Size=$((Get-Item $copy).Length)
"@ | Set-Content (Join-Path $backupRoot "backup-manifest.txt") -Encoding UTF8
}
else {
    Write-Host "AVISO: banco oficial nao encontrado em $prodDb"
}

if (-not (Test-Path -LiteralPath $LegacyDir)) {
    Write-Host "Nenhuma instalacao legada em $LegacyDir"
    exit 0
}

$exe = Join-Path $LegacyDir "PrimoAutoEletrica.exe"
if (Test-Path -LiteralPath $exe) {
    $vi = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
    Write-Host "Legacy EXE PV=$($vi.ProductVersion) FV=$($vi.FileVersion) Product=$($vi.ProductName)"
}

$dbInLegacy = @(Get-ChildItem -LiteralPath $LegacyDir -Recurse -Filter "primoauto.db" -EA SilentlyContinue)
if ($dbInLegacy.Count -gt 0) {
    Write-Host "BLOQUEADO: banco encontrado dentro do Program Files legado. Backup manual necessario."
    $dbInLegacy | ForEach-Object { Write-Host "  $($_.FullName)" }
    exit 2
}

$unins = Join-Path $LegacyDir "unins000.exe"
if (-not (Test-Path -LiteralPath $unins)) {
    Write-Host "BLOQUEADO: unins000.exe ausente. Nao remover pasta manualmente sem suporte."
    exit 3
}

if (-not $ConfirmCleanup) {
    Write-Host "Dry-run OK. Execute novamente com -ConfirmCleanup para desinstalar."
    exit 0
}

if ($PSCmdlet.ShouldProcess($LegacyDir, "Uninstall legacy via unins000.exe")) {
    $p = Start-Process -FilePath $unins -ArgumentList "/VERYSILENT","/SUPPRESSMSGBOXES" -Wait -PassThru -Verb RunAs
    Write-Host "UninsExit=$($p.ExitCode)"
    Start-Sleep 2
    if (Test-Path -LiteralPath $LegacyDir) {
        Write-Host "Restos detectados; tentando remover pasta (somente programa)."
        Start-Process powershell -ArgumentList "-NoProfile","-Command","Remove-Item -LiteralPath '$LegacyDir' -Recurse -Force -ErrorAction SilentlyContinue" -Verb RunAs -Wait
    }
}

Write-Host "LegacyExists=$(Test-Path -LiteralPath $LegacyDir)"
Write-Host "ProdDbExists=$(Test-Path -LiteralPath $prodDb)"
Write-Host "DevAppExists=$(Test-Path (Join-Path $env:LOCALAPPDATA 'PrimoAutoEletrica\App\PrimoAutoEletrica.exe'))"
Write-Host "Concluido. Instale PRIMOX Workshop 1.0.0 e confira atalhos -> Program Files\PRIMOX\Workshop."
