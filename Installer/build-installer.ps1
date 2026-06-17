# Script de Build do Instalador - Primo Auto Elétrica
# Versão: 1.0.0

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0",
    [switch]$SkipBuild = $false,
    [switch]$SkipTests = $false
)

$ErrorActionPreference = "Stop"
$StartTime = Get-Date

# Configurações
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$ProjectDir = Join-Path $ProjectRoot "PrimoAutoEletrica"
$InstallerDir = $PSScriptRoot
$ReleasesDir = Join-Path $ProjectRoot "Releases"
$LogsDir = Join-Path $ProjectRoot "LogsValidacao"

# Criar diretórios necessários
New-Item -ItemType Directory -Force -Path $ReleasesDir | Out-Null
New-Item -ItemType Directory -Force -Path $LogsDir | Out-Null

# Log file
$LogFile = Join-Path $LogsDir "build-installer-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] $Message"
    Write-Host $logMessage
    Add-Content -Path $LogFile -Value $logMessage
}

Write-Log "=== Iniciando build do instalador ==="
Write-Log "Versão: $Version"
Write-Log "Runtime: $Runtime"
Write-Log "Configuration: $Configuration"

try {
    # Passo 1: Limpeza
    Write-Log "Passo 1: Limpeza do projeto..."
    Set-Location $ProjectDir
    dotnet clean
    if ($LASTEXITCODE -ne 0) { throw "Falha na limpeza" }
    Write-Log "Limpeza concluída com sucesso"

    # Passo 2: Restauração
    Write-Log "Passo 2: Restauração de dependências..."
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw "Falha na restauração" }
    Write-Log "Restauração concluída com sucesso"

    # Passo 3: Build
    if (-not $SkipBuild) {
        Write-Log "Passo 3: Build do projeto..."
        dotnet build -c $Configuration
        if ($LASTEXITCODE -ne 0) { throw "Falha no build" }
        Write-Log "Build concluído com sucesso"
    }

    # Passo 4: Testes
    if (-not $SkipTests) {
        Write-Log "Passo 4: Execução de testes..."
        dotnet test
        if ($LASTEXITCODE -ne 0) { throw "Falha nos testes" }
        Write-Log "Testes concluídos com sucesso"
    }

    # Passo 5: Publicação self-contained
    Write-Log "Passo 5: Publicação self-contained..."
    $PublishDir = Join-Path $ProjectDir "bin\$Configuration\net9.0-windows\$Runtime\publish"
    dotnet publish -c $Configuration -r $Runtime --self-contained
    if ($LASTEXITCODE -ne 0) { throw "Falha na publicação" }
    Write-Log "Publicação concluída em: $PublishDir"

    # Passo 6: Compilar instalador Inno Setup
    Write-Log "Passo 6: Compilação do instalador Inno Setup..."
    
    # Verificar se Inno Setup está instalado
    $InnoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    if (-not (Test-Path $InnoSetupPath)) {
        $InnoSetupPath = "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    }
    
    if (-not (Test-Path $InnoSetupPath)) {
        throw "Inno Setup não encontrado. Instale em: https://jrsoftware.org/isdl.php"
    }
    
    $IssFile = Join-Path $InstallerDir "PrimoAutoEletrica.iss"
    $OutputFile = Join-Path $ReleasesDir "PrimoAutoEletrica-Setup-$Version.exe"
    
    & $InnoSetupPath $IssFile "/DAppVersion=$Version" "/O$ReleasesDir"
    if ($LASTEXITCODE -ne 0) { throw "Falha na compilação do instalador" }
    
    Write-Log "Instalador gerado com sucesso"

    # Passo 7: Gerar hash SHA256
    Write-Log "Passo 7: Geração de hash SHA256..."
    $HashFile = Join-Path $ReleasesDir "SHA256.txt"
    $Hash = (Get-FileHash -Path $OutputFile -Algorithm SHA256).Hash
    "$Hash  PrimoAutoEletrica-Setup-$Version.exe" | Out-File -FilePath $HashFile -Encoding UTF8
    Write-Log "Hash SHA256: $Hash"
    Write-Log "Hash salvo em: $HashFile"

    # Passo 8: Copiar arquivos adicionais
    Write-Log "Passo 8: Cópia de arquivos adicionais..."
    
    # Copiar README
    if (Test-Path (Join-Path $ProjectRoot "README.md")) {
        Copy-Item (Join-Path $ProjectRoot "README.md") -Destination $ReleasesDir -Force
    }
    
    # Copiar LICENSE
    if (Test-Path (Join-Path $ProjectRoot "LICENSE")) {
        Copy-Item (Join-Path $ProjectRoot "LICENSE") -Destination $ReleasesDir -Force
    }
    
    # Copiar CHANGELOG
    if (Test-Path (Join-Path $ProjectDir "Docs\CHANGELOG.md")) {
        Copy-Item (Join-Path $ProjectDir "Docs\CHANGELOG.md") -Destination $ReleasesDir -Force
    }
    
    Write-Log "Arquivos adicionais copiados"

    # Passo 9: Criar arquivo de release notes
    Write-Log "Passo 9: Criação de release notes..."
    $ReleaseNotesFile = Join-Path $ReleasesDir "RELEASE_NOTES_$Version.md"
    $ReleaseNotes = @"
# Release Notes - Primo Auto Elétrica v$Version

**Data:** $(Get-Date -Format "dd/MM/yyyy")
**Hash SHA256:** $Hash

## Arquivos

- PrimoAutoEletrica-Setup-$Version.exe - Instalador principal
- SHA256.txt - Hash de integridade
- README.md - Instruções de instalação
- LICENSE.txt - Licença de uso
- CHANGELOG.md - Histórico de versões

## Instalação

1. Execute o instalador como administrador
2. Siga as instruções do assistente
3. O sistema será instalado em C:\Program Files\PrimoAutoEletrica
4. Dados do usuário serão preservados em atualizações

## Requisitos

- Windows 7 SP1 (x64) ou superior
- .NET 9.0 Runtime (incluído no instalador)
- 500 MB de espaço em disco
- 4 GB de RAM recomendado

## Atualização

Para atualizar de uma versão anterior:
1. Execute o novo instalador
2. O sistema detectará a versão instalada
3. Backup automático será criado
4. Dados serão preservados

## Suporte

Para suporte, consulte a documentação em: Docs/MANUAL_USUARIO.md

---

**Build gerado em:** $((Get-Date) - $StartTime).ToString('hh\:mm\:ss')
"@
    $ReleaseNotes | Out-File -FilePath $ReleaseNotesFile -Encoding UTF8
    Write-Log "Release notes criados"

    # Passo 10: Validar saída
    Write-Log "Passo 10: Validação da saída..."
    if (-not (Test-Path $OutputFile)) {
        throw "Arquivo do instalador não encontrado: $OutputFile"
    }
    
    $FileSize = (Get-Item $OutputFile).Length / 1MB
    Write-Log "Tamanho do instalador: $([math]::Round($FileSize, 2)) MB"
    
    if ($FileSize -lt 1) {
        Write-Log "AVISO: Instalador muito pequeno, pode estar incompleto"
    }

    $EndTime = Get-Date
    $Duration = $EndTime - $StartTime
    
    Write-Log "=== Build do instalador concluído com sucesso ==="
    Write-Log "Duração total: $($Duration.ToString('hh\:mm\:ss'))"
    Write-Log "Arquivo gerado: $OutputFile"
    Write-Log "Log salvo em: $LogFile"

    exit 0

}
catch {
    Write-Log "ERRO: $_"
    Write-Log "Stack trace: $($_.ScriptStackTrace)"
    exit 1
}
