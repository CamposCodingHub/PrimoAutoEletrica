param(
    [string]$OutputRoot = "Artifacts",
    [string]$PackageName = ""
)

$ErrorActionPreference = "Stop"

$solutionRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

if ([string]::IsNullOrWhiteSpace($PackageName)) {
    $PackageName = "QA_FIELD_VALIDATION_$timestamp"
}

$outputRootFullPath = if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
    $OutputRoot
}
else {
    Join-Path $solutionRoot $OutputRoot
}

$outputDirectory = Join-Path $outputRootFullPath $PackageName
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$roteiroPath = Join-Path $solutionRoot "PrimoAutoEletrica\Docs\ROTEIRO_QA_MANUAL_FINAL_PRIMOAUTOELETRICA.md"
$statusPath = Join-Path $outputDirectory "QA_MANUAL_STATUS.md"
$checklistPath = Join-Path $outputDirectory "CHECKLIST_CAMPO.md"
$csvPath = Join-Path $outputDirectory "CHECKLIST_CAMPO.csv"
$manifestPath = Join-Path $outputDirectory "manifest.txt"

& (Join-Path $PSScriptRoot "Get-QaManualStatus.ps1") -OutputPath $statusPath | Out-Null

function Get-PendingItems {
    param([string]$Path)

    $currentSection = ""
    $items = New-Object System.Collections.Generic.List[object]

    foreach ($line in (Get-Content -Path $Path -Encoding UTF8)) {
        if ($line -match "^##\s+(.+)$") {
            $currentSection = $Matches[1].Trim()
            continue
        }

        if ($line -notmatch "^\|") {
            continue
        }

        if ($currentSection -eq "Como registrar") {
            continue
        }

        if ($line -match "^\|\s*[-: ]+\|") {
            continue
        }

        $parts = $line.Trim("|") -split "\|" | ForEach-Object { $_.Trim() }
        if ($parts.Count -lt 2 -or $parts[0] -eq "Item") {
            continue
        }

        if ($parts[1] -match "\[ \]") {
            $items.Add([PSCustomObject]@{
                Secao = $currentSection
                Item = $parts[0]
            }) | Out-Null
        }
    }

    $items
}

function Get-FieldGuidance {
    param(
        [string]$Secao,
        [string]$Item
    )

    switch -Regex ($Item) {
        "administrador real" {
            return [PSCustomObject]@{
                Evidencias = @(
                    "Print da tela principal apos login com o usuario administrador real.",
                    "Nome do usuario/perfil exibido no header ou sessao ativa.",
                    "Registro de auditoria/login ou observacao com horario da entrada."
                )
                Criterios = @(
                    "O login deve abrir o sistema sem erro, lockout indevido ou permissao incompleta.",
                    "O perfil deve permitir acesso a Configuracoes, Funcionarios, Financeiro, PDV e Importar NF-e."
                )
            }
        }
        "XML de producao" {
            return [PSCustomObject]@{
                Evidencias = @(
                    "Caminho ou identificacao controlada do XML real usado na homologacao.",
                    "Chave NF-e, numero, serie, fornecedor e totais exibidos no historico.",
                    "Print dos produtos criados/atualizados e cards de importacao."
                )
                Criterios = @(
                    "A importacao deve concluir sem excecao e sem duplicidade indevida.",
                    "Fornecedor, produtos, quantidades, custos e dados fiscais devem bater com o XML real."
                )
            }
        }
        "compras/prazos" {
            return [PSCustomObject]@{
                Evidencias = @(
                    "Print da ficha do fornecedor apos importar a NF-e real.",
                    "Numero/chave da ultima NF-e exibida ou anotada.",
                    "Produtos vinculados, ultima compra, ticket medio e prazo medio visiveis."
                )
                Criterios = @(
                    "A ficha deve refletir a compra real do fornecedor correto.",
                    "Prazo medio, ultima compra, ranking e produtos vinculados devem ser coerentes com a nota."
                )
            }
        }
        "Abrir caixa" {
            return [PSCustomObject]@{
                Evidencias = @(
                    "Nome do operador real, numero do caixa e valor de abertura.",
                    "Print do PDV mostrando caixa aberto.",
                    "Registro de auditoria/sessao operacional da abertura."
                )
                Criterios = @(
                    "O operador real deve conseguir abrir caixa conforme sua permissao.",
                    "Saldo inicial, numero do caixa e estado operacional devem aparecer corretamente no PDV."
                )
            }
        }
        "impressora fisica" {
            return [PSCustomObject]@{
                Evidencias = @(
                    "Modelo da impressora, porta/fila Windows e estacao usada.",
                    "Foto ou digitalizacao do comprovante reimpresso.",
                    "ID da venda reimpressa e registro de auditoria da reimpressao."
                )
                Criterios = @(
                    "O comprovante fisico deve sair sem truncamento de itens, totais, operador, cliente e forma de pagamento.",
                    "A tentativa deve registrar auditoria e respeitar a impressora preferencial configurada quando habilitada."
                )
            }
        }
        default {
            return [PSCustomObject]@{
                Evidencias = @("Print ou arquivo objetivo que comprove o resultado.")
                Criterios = @("O resultado deve corresponder ao item do roteiro.")
            }
        }
    }
}

$pendingItems = @(Get-PendingItems -Path $roteiroPath)

$markdown = New-Object System.Collections.Generic.List[string]
$markdown.Add("# Pacote de validacao de campo - PrimoAutoEletrica")
$markdown.Add("")
$markdown.Add(("Gerado em: {0:dd/MM/yyyy HH:mm:ss}" -f (Get-Date)))
$markdown.Add(("Roteiro origem: {0}" -f $roteiroPath))
$markdown.Add(("Status snapshot: {0}" -f $statusPath))
$markdown.Add("")
$markdown.Add("## Resumo")
$markdown.Add("")
$markdown.Add(("Pendencias de campo: {0}" -f $pendingItems.Count))
$markdown.Add("")
$markdown.Add("Use este pacote para executar somente os itens que dependem de usuario real, XML de producao ou impressora fisica. Os itens automatizaveis ja ficam rastreados no roteiro principal.")
$markdown.Add("")

$csvRows = New-Object System.Collections.Generic.List[string]
$csvRows.Add("Secao;Item;Status;Evidencia;ResponsavelData;Observacoes")

for ($index = 0; $index -lt $pendingItems.Count; $index++) {
    $pending = $pendingItems[$index]
    $guidance = Get-FieldGuidance -Secao $pending.Secao -Item $pending.Item

    $markdown.Add(("## {0}. {1}" -f ($index + 1), $pending.Item))
    $markdown.Add("")
    $markdown.Add(("Secao: {0}" -f $pending.Secao))
    $markdown.Add("")
    $markdown.Add("Resultado: [ ] Pendente  [ ] Aprovado  [ ] Reprovado  [ ] Aprovado com ressalva")
    $markdown.Add("")
    $markdown.Add("Evidencias obrigatorias:")
    foreach ($evidence in $guidance.Evidencias) {
        $markdown.Add(("- {0}" -f $evidence))
    }
    $markdown.Add("")
    $markdown.Add("Criterios de aceite:")
    foreach ($criterion in $guidance.Criterios) {
        $markdown.Add(("- {0}" -f $criterion))
    }
    $markdown.Add("")
    $markdown.Add("Registro da execucao:")
    $markdown.Add("- Responsavel/Data:")
    $markdown.Add("- Evidencia local:")
    $markdown.Add("- Observacoes:")
    $markdown.Add("")

    $csvRows.Add(("{0};{1};Pendente;;;" -f $pending.Secao, $pending.Item))
}

Set-Content -Path $checklistPath -Value $markdown.ToArray() -Encoding UTF8
Set-Content -Path $csvPath -Value $csvRows.ToArray() -Encoding UTF8

$pdvValidationPath = Join-Path $solutionRoot "Docs\PDV_REIMPRESSION_VALIDATION.md"
if (Test-Path $pdvValidationPath) {
    Copy-Item -Path $pdvValidationPath -Destination (Join-Path $outputDirectory "PDV_REIMPRESSION_VALIDATION.md") -Force
}

$manifest = @(
    "PrimoAutoEletrica - QA field evidence package",
    ("GeneratedAt={0:yyyy-MM-dd HH:mm:ss}" -f (Get-Date)),
    "Checklist=CHECKLIST_CAMPO.md",
    "ChecklistCsv=CHECKLIST_CAMPO.csv",
    "Status=QA_MANUAL_STATUS.md",
    ("PendingCount={0}" -f $pendingItems.Count)
)

Set-Content -Path $manifestPath -Value $manifest -Encoding UTF8

[PSCustomObject]@{
    OutputDirectory = $outputDirectory
    ChecklistPath = $checklistPath
    CsvPath = $csvPath
    StatusPath = $statusPath
    ManifestPath = $manifestPath
    PendingCount = $pendingItems.Count
}
