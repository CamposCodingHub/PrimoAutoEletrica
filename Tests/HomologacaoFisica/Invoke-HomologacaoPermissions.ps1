[CmdletBinding()]
param(
    [string]$ConfigPath = "",
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot "HomologacaoFisica.Common.psm1") -Force

function Get-ModulesForRole {
    param(
        [string]$SourceText,
        [string[]]$Aliases
    )

    foreach ($alias in $Aliases) {
        $pattern = '(?s)"' + [regex]::Escape($alias) + '".*?=>\s*new HashSet<string>\([^)]*\)\s*\{(?<Body>.*?)\}'
        $match = [regex]::Match($SourceText, $pattern)
        if ($match.Success) {
            return @([regex]::Matches($match.Groups["Body"].Value, '"(?<Item>[^"]+)"') | ForEach-Object { $_.Groups["Item"].Value })
        }
    }

    return @()
}

$projectRoot = Get-HomologacaoProjectRoot -ScriptPath $PSScriptRoot
$config = Import-HomologacaoConfig -Path $ConfigPath -ProjectRoot $projectRoot

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $projectRoot ("TestResults\HomologacaoFisica\{0}" -f (Get-Date -Format "yyyy-MM-dd_HH-mm-ss"))
}

$outputDirectory = Join-Path $OutputRoot "Permissions"
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$testProject = Join-Path $projectRoot "Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj"
$permissionSourcePath = Join-Path $projectRoot "PrimoAutoEletrica\Services\PermissionService.cs"
$checks = New-Object System.Collections.Generic.List[object]

$policyRun = Invoke-DotNetTest -ProjectRoot $projectRoot -ProjectPath $testProject -Build $config.Build -OutputDirectory (Join-Path $outputDirectory "AccessControlPolicy") -Label "access-control-policy" -Filter "FullyQualifiedName~AccessControlPolicyTests"
$loginRun = Invoke-DotNetTest -ProjectRoot $projectRoot -ProjectPath $testProject -Build $config.Build -OutputDirectory (Join-Path $outputDirectory "LoginSecurity") -Label "login-security" -Filter "FullyQualifiedName~LoginSecurityTests" -NoBuild

$checks.Add((New-HomologacaoCheck -Name "AccessControlPolicyTests" -Outcome $(if ($policyRun.ExitCode -eq 0 -and $policyRun.Failed -eq 0) { "Pass" } else { "Fail" }) -Details "Passed=$($policyRun.Passed); Failed=$($policyRun.Failed)" -Module "Seguranca" -Command $policyRun.CommandLine -AffectedFile $policyRun.TrxPath -PossibleCause "Matriz persistida de perfis/permissoes divergente do esperado." -SuggestedFix "Revisar seed de perfis, permissoes persistidas e regras de acesso." ))
$checks.Add((New-HomologacaoCheck -Name "LoginSecurityTests" -Outcome $(if ($loginRun.ExitCode -eq 0 -and $loginRun.Failed -eq 0) { "Pass" } else { "Fail" }) -Details "Passed=$($loginRun.Passed); Failed=$($loginRun.Failed)" -Module "Seguranca" -Command $loginRun.CommandLine -AffectedFile $loginRun.TrxPath -PossibleCause "Fluxo de senha temporaria, hash ou troca obrigatoria falhou." -SuggestedFix "Corrigir o seed do admin inicial e as rotinas de autenticacao." ))

$sourceText = Get-Content -Raw -Path $permissionSourcePath
$modulePolicySource = $sourceText
if ($sourceText -match '(?s)private HashSet<string> ObterModulosPermitidosFallback\(\)\s*\{(?<Body>.*?)\n\s*private void RegistrarPermissaoNegada') {
    $modulePolicySource = $matches.Body
}
$roleDefinitions = [ordered]@{
    Administrador = @("ADMINISTRADOR")
    Gerente = @("GERENTE")
    Caixa = @("CAIXA")
    Vendedor = @("VENDEDOR")
    Mecanico = @("MECANICO", "TECNICO")
    Almoxarife = @("ALMOXARIFE", "ESTOQUISTA", "ESTOQUE")
}

$modulesToCheck = [ordered]@{
    Dashboard = "Dashboard"
    Clientes = "Clientes"
    Veiculos = "Veiculos"
    Orcamentos = "Orcamentos"
    OrdensServico = "OrdensServico"
    PDV = "PDV"
    Estoque = "Estoque"
    Financeiro = "Financeiro"
    Fornecedores = "Fornecedores"
    Funcionarios = "Funcionarios"
    Configuracoes = "Sistema"
    Relatorios = "Relatorios"
    Agendamentos = "Agendamentos"
}

$matrix = [ordered]@{}
foreach ($roleName in $roleDefinitions.Keys) {
    $allowedModules = @(Get-ModulesForRole -SourceText $modulePolicySource -Aliases $roleDefinitions[$roleName])
    $matrix[$roleName] = [ordered]@{}
    foreach ($moduleName in $modulesToCheck.Keys) {
        $internalName = $modulesToCheck[$moduleName]
        $allowed = $allowedModules -contains $internalName
        $matrix[$roleName][$moduleName] = if ($allowed) { "PERMITIDO" } else { "BLOQUEADO" }
    }

    $checks.Add((New-HomologacaoCheck -Name ("RoleMatrix:" + $roleName) -Outcome $(if ($allowedModules.Count -gt 0) { "Pass" } else { "Fail" }) -Details ("Modulos encontrados: " + ($allowedModules -join ", ")) -Module "Seguranca" -AffectedFile $permissionSourcePath -PossibleCause "Perfil nao encontrado na politica fallback do app." -SuggestedFix "Revisar PermissionService.ObterModulosPermitidosFallback." ))
}

$checks.Add((New-HomologacaoCheck -Name "PrivilegeEscalationEvidence" -Outcome $(if ($policyRun.ExitCode -eq 0) { "Pass" } else { "Warn" }) -Details "Os testes de politica validam negativas para Gerente, Vendedor, Caixa e Almoxarife em modulos/acoes sensiveis." -Module "Seguranca" -AffectedFile $policyRun.TrxPath -PossibleCause "Nao ha evidencias automatizadas suficientes de negacao para todos os modulos da UI." -SuggestedFix "Complementar com smoke tests de navegacao por perfil ou testes de UI dedicados." ))
$checks.Add((New-HomologacaoCheck -Name "ValidationMode" -Outcome "Warn" -Details "Esta etapa valida matriz de politica e regressao automatizada; ela nao clica cada tela da UI perfil a perfil." -Module "Seguranca" -PossibleCause "Cobertura centrada em politica e persistencia, nao em clique visual." -SuggestedFix "Adicionar smoke tests dedicados para cada perfil se quiser prova visual total." ))

$area = New-HomologacaoAreaResult `
    -AreaId "permissions" `
    -AreaName "Usuarios e permissoes" `
    -OutputDirectory $outputDirectory `
    -Checks $checks `
    -Metrics @{
        Matrix = $matrix
        AccessControlPassed = $policyRun.Passed
        LoginSecurityPassed = $loginRun.Passed
    }

Write-HomologacaoAreaArtifacts -AreaResult $area -JsonPath (Join-Path $OutputRoot "permissions-summary.json") -TextPath (Join-Path $OutputRoot "permissions-summary.txt")
