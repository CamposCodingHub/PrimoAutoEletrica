[CmdletBinding()]
param(
    [string]$ConfigPath = "",
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot "HomologacaoFisica.Common.psm1") -Force

$projectRoot = Get-HomologacaoProjectRoot -ScriptPath $PSScriptRoot
$config = Import-HomologacaoConfig -Path $ConfigPath -ProjectRoot $projectRoot

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $projectRoot ("TestResults\HomologacaoFisica\{0}" -f (Get-Date -Format "yyyy-MM-dd_HH-mm-ss"))
}

$outputDirectory = Join-Path $OutputRoot "WindowsClean"
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$checks = New-Object System.Collections.Generic.List[object]
$effective = Get-EffectiveDatabaseSettings -Config $config
$os = Get-CimInstance Win32_OperatingSystem
$processor = Get-CimInstance Win32_Processor | Select-Object -First 1
$systemDrive = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'"
$adminIdentity = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
$isAdministrator = $adminIdentity.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

$checks.Add((New-HomologacaoCheck -Name "WindowsVersion" -Outcome $(if ([version]$os.Version -ge [version]"10.0") { "Pass" } else { "Fail" }) -Details "$($os.Caption) $($os.Version)" -Module "Instalacao" -PossibleCause "Sistema operacional abaixo do baseline esperado." -SuggestedFix "Homologar em Windows 10/11 atualizado." ))
$checks.Add((New-HomologacaoCheck -Name "ArchitectureX64" -Outcome $(if ($os.OSArchitecture -match "64" -or $processor.AddressWidth -eq 64) { "Pass" } else { "Fail" }) -Details "OS=$($os.OSArchitecture); CPU=$($processor.AddressWidth)-bit" -Module "Instalacao" -PossibleCause "Arquitetura nao compativel com o pacote comercial." -SuggestedFix "Usar estacao x64." ))

$runtimeLog = Join-Path $outputDirectory "dotnet-runtimes.txt"
$dotnet = Invoke-HomologacaoCommand -FilePath "dotnet" -Arguments @("--list-runtimes") -WorkingDirectory $projectRoot -LogPath $runtimeLog -TimeoutSeconds 30
$hasRuntime = $dotnet.ExitCode -eq 0
$hasWindowsDesktop = $hasRuntime -and (($dotnet.Output -join "`n") -match "Microsoft\.WindowsDesktop\.App\s+9\.")
$checks.Add((New-HomologacaoCheck -Name "DotNetAvailable" -Outcome $(if ($hasRuntime) { "Pass" } else { "Fail" }) -Details "dotnet --list-runtimes exit code $($dotnet.ExitCode)" -Module "Instalacao" -Command $dotnet.CommandLine -AffectedFile $runtimeLog -PossibleCause "SDK/runtime .NET nao instalado." -SuggestedFix "Instalar .NET 9 SDK ou runtime Desktop." ))
$checks.Add((New-HomologacaoCheck -Name "WindowsDesktopRuntime9" -Outcome $(if ($hasWindowsDesktop) { "Pass" } else { "Fail" }) -Details "Microsoft.WindowsDesktop.App 9.x requerido para WPF." -Module "Instalacao" -Command $dotnet.CommandLine -AffectedFile $runtimeLog -PossibleCause "Runtime Desktop ausente ou versao divergente." -SuggestedFix "Instalar Microsoft Windows Desktop Runtime 9.x." ))

$writeProbePath = Join-Path $outputDirectory "write-probe.tmp"
try {
    "homologacao" | Set-Content -Path $writeProbePath -Encoding UTF8
    Remove-Item -Path $writeProbePath -Force
    $checks.Add((New-HomologacaoCheck -Name "WritePermissionOutputRoot" -Outcome "Pass" -Details "Escrita confirmada em $outputDirectory" -Module "Instalacao"))
}
catch {
    $checks.Add((New-HomologacaoCheck -Name "WritePermissionOutputRoot" -Outcome "Fail" -Details "Falha ao escrever no diretoria de evidencia." -Module "Instalacao" -ErrorMessage $_.Exception.Message -AffectedFile $writeProbePath -PossibleCause "Permissao NTFS insuficiente." -SuggestedFix "Executar com usuario que tenha permissao de escrita na pasta de testes." ))
}

$appDataProbe = Join-Path $effective.AppDataPath "homologacao-write-probe.tmp"
try {
    New-Item -ItemType Directory -Path $effective.AppDataPath -Force | Out-Null
    "homologacao" | Set-Content -Path $appDataProbe -Encoding UTF8
    Remove-Item -Path $appDataProbe -Force
    $checks.Add((New-HomologacaoCheck -Name "WritePermissionAppData" -Outcome "Pass" -Details "Escrita confirmada em $($effective.AppDataPath)" -Module "Instalacao"))
}
catch {
    $checks.Add((New-HomologacaoCheck -Name "WritePermissionAppData" -Outcome "Fail" -Details "Falha ao escrever no AppData do sistema." -Module "Instalacao" -ErrorMessage $_.Exception.Message -AffectedFile $appDataProbe -PossibleCause "AppData bloqueado ou inexistente." -SuggestedFix "Garantir permissao de escrita no AppData do PrimoAutoEletrica." ))
}

$adapterCount = 0
try {
    $adapterCount = @(Get-NetAdapter -Physical -ErrorAction Stop | Where-Object { $_.Status -eq "Up" }).Count
}
catch {
    $adapterCount = @(Get-CimInstance Win32_NetworkAdapter | Where-Object { $_.NetEnabled }).Count
}

$checks.Add((New-HomologacaoCheck -Name "NetworkAdaptersUp" -Outcome $(if ($adapterCount -gt 0) { "Pass" } else { "Warn" }) -Details "Adaptadores ativos: $adapterCount" -Module "Rede" -PossibleCause "Sem conectividade fisica ativa." -SuggestedFix "Conectar a maquina a rede local antes da homologacao multiusuario." ))

$probeHost = [string]$config.Environment.NetworkProbeHost
$probePort = [int]$config.Environment.NetworkProbePort
if (-not [string]::IsNullOrWhiteSpace($probeHost)) {
    try {
        $netProbe = Test-NetConnection -ComputerName $probeHost -Port $probePort -WarningAction SilentlyContinue
        $checks.Add((New-HomologacaoCheck -Name "OutboundNetworkProbe" -Outcome $(if ($netProbe.TcpTestSucceeded) { "Pass" } else { "Warn" }) -Details "Host=$probeHost Port=$probePort Result=$($netProbe.TcpTestSucceeded)" -Module "Rede" -Command "Test-NetConnection -ComputerName $probeHost -Port $probePort" -PossibleCause "Firewall de saida, proxy ou sem rota." -SuggestedFix "Liberar saida TCP ou ajustar proxy corporativo." ))
    }
    catch {
        $checks.Add((New-HomologacaoCheck -Name "OutboundNetworkProbe" -Outcome "Warn" -Details "Falha ao testar saida de rede." -Module "Rede" -ErrorMessage $_.Exception.Message -Command "Test-NetConnection -ComputerName $probeHost -Port $probePort" -PossibleCause "Cmdlet indisponivel ou politica de rede." -SuggestedFix "Validar rede manualmente e revisar regras de firewall/proxy." ))
    }
}

$provider = if ($null -ne $effective.Settings) { [string]$effective.Settings.Provider } else { "Desconhecido" }
$sqlExpected = $provider -eq "SqlServer" -or -not [string]::IsNullOrWhiteSpace([string]$config.Network.SqlConnectionString)
$sqlServices = @(Get-Service -Name "MSSQL*" -ErrorAction SilentlyContinue)
$sqlServiceStatus = if ($sqlServices.Count -gt 0) {
    ($sqlServices | ForEach-Object { "{0}={1}" -f $_.Name, $_.Status }) -join "; "
}
else {
    "Nenhum servico MSSQL local detectado."
}
$checks.Add((New-HomologacaoCheck -Name "SqlServerInstalledOrServiceVisible" -Outcome $(if ($sqlServices.Count -gt 0) { "Pass" } elseif ($sqlExpected) { "Fail" } else { "Warn" }) -Details $sqlServiceStatus -Module "Banco" -PossibleCause "SQL Server nao instalado localmente." -SuggestedFix "Instalar SQL Server ou informar um SQL remoto valido na configuracao da homologacao." ))

try {
    $listeners = @(Get-NetTCPConnection -State Listen -ErrorAction Stop | Where-Object { $_.LocalPort -in 1433, 1434 })
    $listenerDetails = if ($listeners.Count -gt 0) {
        ($listeners | ForEach-Object { "{0}:{1}" -f $_.LocalAddress, $_.LocalPort }) -join "; "
    }
    else {
        "Nenhum listener em 1433/1434."
    }
    $checks.Add((New-HomologacaoCheck -Name "SqlPortsObserved" -Outcome $(if ($listeners.Count -gt 0) { "Pass" } elseif ($sqlExpected) { "Warn" } else { "Skip" }) -Details $listenerDetails -Module "Banco" -PossibleCause "Instancia configurada com porta dinamica ou nao iniciada." -SuggestedFix "Conferir porta da instancia SQL Server e firewall local." ))
}
catch {
    $checks.Add((New-HomologacaoCheck -Name "SqlPortsObserved" -Outcome "Warn" -Details "Falha ao enumerar portas TCP." -Module "Banco" -ErrorMessage $_.Exception.Message -PossibleCause "Permissao insuficiente para consulta de sockets." -SuggestedFix "Executar a homologacao com privilegio suficiente para ler listeners locais." ))
}

$physicalPrinters = @()
try {
    $physicalPrinters = @(Get-Printer -ErrorAction Stop)
}
catch {
    $physicalPrinters = @(Get-CimInstance Win32_Printer)
}

$printerNames = ($physicalPrinters | ForEach-Object { $_.Name }) -join "; "
$checks.Add((New-HomologacaoCheck -Name "PrintersInstalled" -Outcome $(if ($physicalPrinters.Count -gt 0) { "Pass" } else { "Warn" }) -Details "Total=$($physicalPrinters.Count); $printerNames" -Module "Impressao" -PossibleCause "Nenhuma impressora instalada na estacao." -SuggestedFix "Instalar ao menos Microsoft Print to PDF e a impressora fisica da oficina." ))
$hasPdfPrinter = @($physicalPrinters | Where-Object { $_.Name -match "Microsoft Print to PDF" }).Count -gt 0
$checks.Add((New-HomologacaoCheck -Name "MicrosoftPrintToPdf" -Outcome $(if ($hasPdfPrinter) { "Pass" } else { "Warn" }) -Details "Presenca do driver Microsoft Print to PDF: $hasPdfPrinter" -Module "Impressao" -PossibleCause "Driver virtual padrao do Windows ausente." -SuggestedFix "Habilitar Microsoft Print to PDF nos recursos opcionais do Windows." ))

try {
    $profiles = @(Get-NetFirewallProfile -ErrorAction Stop)
    $profileDetails = ($profiles | ForEach-Object { "{0}=Enabled:{1}" -f $_.Name, $_.Enabled }) -join "; "
    $firewallEnabled = @($profiles | Where-Object { $_.Enabled }).Count -gt 0
    $checks.Add((New-HomologacaoCheck -Name "FirewallProfiles" -Outcome $(if ($firewallEnabled) { "Pass" } else { "Warn" }) -Details $profileDetails -Module "Seguranca" -PossibleCause "Firewall desativado em todos os perfis." -SuggestedFix "Revisar politica de firewall e liberar somente as portas necessarias." ))
}
catch {
    $checks.Add((New-HomologacaoCheck -Name "FirewallProfiles" -Outcome "Warn" -Details "Falha ao ler perfis de firewall." -Module "Seguranca" -ErrorMessage $_.Exception.Message -PossibleCause "Modulo NetSecurity indisponivel." -SuggestedFix "Validar firewall via Painel de Controle ou PowerShell com privilegios administrativos." ))
}

$freeGb = if ($null -ne $systemDrive) { [Math]::Round($systemDrive.FreeSpace / 1GB, 2) } else { 0 }
$diskOutcome = if ($freeGb -ge [double]$config.Environment.MinimumFreeDiskGb) { "Pass" } else { "Fail" }
$checks.Add((New-HomologacaoCheck -Name "DiskFreeSpace" -Outcome $diskOutcome -Details "Livre em C: $freeGb GB" -Module "Instalacao" -PossibleCause "Espaco insuficiente para banco, logs e backups." -SuggestedFix "Liberar espaco ou mover backups/logs para outra unidade." ))

$checks.Add((New-HomologacaoCheck -Name "AdministratorSession" -Outcome $(if ($isAdministrator) { "Pass" } else { "Warn" }) -Details "Usuario atual administrador: $isAdministrator" -Module "Instalacao" -PossibleCause "Sessao sem privilegio elevado." -SuggestedFix "Executar homologacao elevada para validar instalacao, firewall e portas." ))

foreach ($requiredFolder in @($config.Environment.RequiredFolders)) {
    $checks.Add((New-HomologacaoCheck -Name ("RequiredFolder:" + $requiredFolder) -Outcome $(if (Test-Path $requiredFolder) { "Pass" } else { "Warn" }) -Details $requiredFolder -Module "Instalacao" -AffectedFile $requiredFolder -PossibleCause "Estrutura base ainda nao criada no computador de homologacao." -SuggestedFix "Abrir o sistema ao menos uma vez ou criar a estrutura esperada antes do go-live." ))
}

$area = New-HomologacaoAreaResult `
    -AreaId "windows-clean" `
    -AreaName "Windows limpo" `
    -OutputDirectory $outputDirectory `
    -Checks $checks `
    -Metrics @{
        ComputerName = $env:COMPUTERNAME
        Provider = $provider
        FreeDiskGb = $freeGb
        AdapterCount = $adapterCount
        PrinterCount = $physicalPrinters.Count
    } `
    -Notes @(
        "Esta etapa mede prontidao da estacao, nao substitui o teste funcional do aplicativo."
    )

$jsonPath = Join-Path $OutputRoot "windows-clean-summary.json"
$txtPath = Join-Path $OutputRoot "windows-clean-summary.txt"
Write-HomologacaoAreaArtifacts -AreaResult $area -JsonPath $jsonPath -TextPath $txtPath
