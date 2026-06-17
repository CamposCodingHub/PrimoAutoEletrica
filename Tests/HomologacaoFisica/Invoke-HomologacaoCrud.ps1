[CmdletBinding()]
param(
    [string]$ConfigPath = "",
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot "HomologacaoFisica.Common.psm1") -Force

function Get-ObservedOperations {
    param(
        [string[]]$EvidenceNames
    )

    $operations = New-Object System.Collections.Generic.HashSet[string]
    foreach ($name in $EvidenceNames) {
        if ($name -match "Criar|Inserir|Cadastrar|Adicionar") { [void]$operations.Add("Cadastrar") }
        if ($name -match "Obter|Selecionar|Consultar|Carregar") { [void]$operations.Add("Consultar") }
        if ($name -match "Atualizar|Editar|Alterar|Salvar") { [void]$operations.Add("Editar") }
        if ($name -match "Excluir|Delete|Deletar|Remover") { [void]$operations.Add("Excluir") }
        if ($name -match "Buscar|Pesquisa|Filtro|Filtrar|Search") { [void]$operations.Add("Buscar/Filtrar") }
        if ($name -match "Obrigat|Validar") { [void]$operations.Add("Campo obrigatorio") }
        if ($name -match "Inval") { [void]$operations.Add("Dado invalido") }
        if ($name -match "Duplic") { [void]$operations.Add("Duplicidade") }
    }

    return @($operations)
}

$projectRoot = Get-HomologacaoProjectRoot -ScriptPath $PSScriptRoot
$config = Import-HomologacaoConfig -Path $ConfigPath -ProjectRoot $projectRoot

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $projectRoot ("TestResults\HomologacaoFisica\{0}" -f (Get-Date -Format "yyyy-MM-dd_HH-mm-ss"))
}

$outputDirectory = Join-Path $OutputRoot "Crud"
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$testProject = Join-Path $projectRoot "Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj"
$testRun = Invoke-DotNetTest -ProjectRoot $projectRoot -ProjectPath $testProject -Build $config.Build -OutputDirectory (Join-Path $outputDirectory "Tests") -Label "crud-suite"
$workflow = Invoke-WorkflowRun -ProjectRoot $projectRoot -Build $config.Build -OutputDirectory (Join-Path $outputDirectory "Workflow") -AppDataPath (Join-Path $outputDirectory "WorkflowAppData") -SkipBuild

$checks = New-Object System.Collections.Generic.List[object]
$checks.Add((New-HomologacaoCheck -Name "CrudRegressionTests" -Outcome $(if ($testRun.ExitCode -eq 0 -and $testRun.Failed -eq 0) { "Pass" } else { "Fail" }) -Details "Passed=$($testRun.Passed); Failed=$($testRun.Failed)" -Module "CRUD" -Command $testRun.CommandLine -AffectedFile $testRun.TrxPath -PossibleCause "Suite de regressao CRUD falhou." -SuggestedFix "Ler o TRX e corrigir o modulo correspondente." ))
$checks.Add((New-HomologacaoCheck -Name "OperationalWorkflowEvidence" -Outcome $(if ($workflow.Status -eq "APROVADO") { "Pass" } else { "Warn" }) -Details "Workflow=$($workflow.Status); Passed=$($workflow.Summary.PassedChecks); Failed=$($workflow.Summary.FailedChecks)" -Module "CRUD" -Command $workflow.CommandLine -AffectedFile $workflow.ReportPath -PossibleCause "O workflow operacional nao fechou todas as etapas complementares ao CRUD." -SuggestedFix "Corrigir os passos do workflow automatizado e ampliar a evidencia de ponta a ponta." ))

$moduleMap = [ordered]@{
    Clientes = @("Cliente")
    Veiculos = @("Veiculo")
    Produtos = @("Produto")
    Estoque = @("Estoque")
    Fornecedores = @("Fornecedor")
    Funcionarios = @("Funcionario")
    Orcamentos = @("Orcamento")
    OS = @("OrdemServico", "OS")
    PDV = @("Pdv", "Venda", "PDV")
    Financeiro = @("Financeiro")
    Agendamentos = @("Agendamento")
    Relatorios = @("Relatorio")
    Configuracoes = @("Configuracoes", "Backup")
}

$evidenceNames = @($testRun.Cases | ForEach-Object { $_.TestName })
$evidenceNames += @($workflow.Checks | Where-Object { $_.Success } | ForEach-Object { $_.Name })

$moduleResults = [ordered]@{}
foreach ($moduleName in $moduleMap.Keys) {
    $patterns = $moduleMap[$moduleName]
    $moduleEvidence = @($evidenceNames | Where-Object {
        $name = $_
        @($patterns | Where-Object { $name -match $_ }).Count -gt 0
    })
    $operations = @(Get-ObservedOperations -EvidenceNames $moduleEvidence)
    $status = if ($operations.Count -ge 5) {
        "APROVADO"
    }
    elseif ($operations.Count -ge 3) {
        "APROVADO COM RESSALVAS"
    }
    else {
        "REPROVADO"
    }

    $moduleResults[$moduleName] = [ordered]@{
        Status = $status
        OperationsObserved = $operations
        EvidenceCount = $moduleEvidence.Count
        Evidence = $moduleEvidence
    }

    $checks.Add((New-HomologacaoCheck -Name ("Module:" + $moduleName) -Outcome $(if ($status -eq "APROVADO") { "Pass" } elseif ($status -eq "APROVADO COM RESSALVAS") { "Warn" } else { "Fail" }) -Details ("Operacoes observadas: " + ($operations -join ", ")) -Module $moduleName -PossibleCause "Cobertura automatizada insuficiente para o modulo." -SuggestedFix "Criar testes adicionais de CRUD e validacoes do modulo faltante." ))
}

$checks.Add((New-HomologacaoCheck -Name "CrudEvidenceMode" -Outcome "Warn" -Details "A nota de CRUD e inferida a partir de testes automatizados e workflow; ela nao garante que todos os campos de todas as telas tiveram exercicio completo." -Module "CRUD" -PossibleCause "Cobertura de teste heterogenea entre modulos." -SuggestedFix "Expandir a suite com testes dedicados de UI por modulo se precisar prova exaustiva." ))

$area = New-HomologacaoAreaResult `
    -AreaId "crud" `
    -AreaName "CRUD completo" `
    -OutputDirectory $outputDirectory `
    -Checks $checks `
    -Metrics @{
        TestPassed = $testRun.Passed
        WorkflowReport = $workflow.ReportPath
        Modules = $moduleResults
    }

Write-HomologacaoAreaArtifacts -AreaResult $area -JsonPath (Join-Path $OutputRoot "crud-summary.json") -TextPath (Join-Path $OutputRoot "crud-summary.txt")
