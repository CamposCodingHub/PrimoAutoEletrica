<#
.SYNOPSIS
  Classifies Portuguese residuals in XAML UI attributes (not blind code search).
#>
[CmdletBinding()]
param(
    [string]$Root,
    [string]$OutDir
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Root)) { $Root = Split-Path -Parent $PSScriptRoot }
if ([string]::IsNullOrWhiteSpace($OutDir)) { $OutDir = Join-Path $Root 'TestResults\I18n' }
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

$fiscal = @('CPF','CNPJ','NCM','CFOP','CST','CSOSN','NF-e','NFe','IBS','CBS','SEFAZ','XML','DANFE')
$brand = @('PRIMOX','PrimoAuto','Focus')
$uiPt = @(
    'Salvar','Cancelar','Excluir','Editar','Pesquisar','Buscar','Configura','Carregando','Nenhum',
    'Cliente','Veiculo','Orcamento','Estoque','Fornecedor','Funcionario','Agendamento','Relatorio',
    'Aguarde','Processando','Sucesso','Aviso','Atencao','Deseja','Tem certeza','Campo obrig','Registro','Rascunho'
)

$xamlRoot = Join-Path $Root 'PrimoAutoEletrica'
$files = Get-ChildItem $xamlRoot -Recurse -Filter *.xaml | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$rows = New-Object System.Collections.Generic.List[object]
$attrRx = [regex]'(Text|Content|Header|ToolTip)="([^"{][^"]*)"'

foreach ($f in $files) {
    $rel = $f.FullName.Substring($xamlRoot.Length).TrimStart('\')
    $text = [System.IO.File]::ReadAllText($f.FullName)
    foreach ($m in $attrRx.Matches($text)) {
        $attr = $m.Groups[1].Value
        $val = $m.Groups[2].Value.Trim()
        if ([string]::IsNullOrWhiteSpace($val) -or $val.Length -lt 2) { continue }

        $class = 'UNKNOWN'
        $reason = 'no strong classifier'

        $isBrand = $false
        foreach ($b in $brand) { if ($val -like "*$b*") { $isBrand = $true; break } }
        $isFiscal = $false
        foreach ($x in $fiscal) { if ($val -eq $x -or ($val.Length -le 12 -and $val -like "*$x*")) { $isFiscal = $true; break } }

        if ($isBrand) {
            $class = 'BRAND'; $reason = 'brand'
        }
        elseif ($isFiscal -and $val.Length -le 12) {
            $class = 'FISCAL_TERM'; $reason = 'fiscal term'
        }
        else {
            $hit = $null
            foreach ($tok in $uiPt) {
                if ($val -like "*$tok*") { $hit = $tok; break }
            }
            if ($hit) {
                $class = 'TRANSLATION_REQUIRED'
                $reason = "UI token $hit"
            }
            else {
                $nonAscii = $false
                foreach ($ch in $val.ToCharArray()) {
                    if ([int][char]$ch -gt 127) { $nonAscii = $true; break }
                }
                if ($nonAscii) {
                    $class = 'TRANSLATION_REQUIRED'
                    $reason = 'non-ASCII UI attr'
                }
            }
        }

        $rows.Add([pscustomobject]@{
            File = $rel
            Attr = $attr
            Value = $val
            Class = $class
            Reason = $reason
        })
    }
}

$grouped = $rows | Group-Object Class | Sort-Object Name
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$summary = [ordered]@{
    GeneratedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    TotalLiteralsScanned = $rows.Count
    ByClass = @{}
}
foreach ($g in $grouped) { $summary.ByClass[$g.Name] = $g.Count }

$jsonPath = Join-Path $OutDir ("i18n-runtime-residuals-{0}.json" -f $stamp)
$csvPath = Join-Path $OutDir ("i18n-runtime-residuals-{0}.csv" -f $stamp)
$mdPath = Join-Path $OutDir ("i18n-runtime-residuals-{0}.md" -f $stamp)

$summary | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonPath -Encoding UTF8
$rows | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# I18N residual classification (XAML UI attrs)')
[void]$sb.AppendLine('')
[void]$sb.AppendLine(('Generated: {0}' -f $summary.GeneratedAt))
[void]$sb.AppendLine(('Total literals: {0}' -f $summary.TotalLiteralsScanned))
[void]$sb.AppendLine('')
foreach ($g in $grouped) {
    [void]$sb.AppendLine(('- {0}: {1}' -f $g.Name, $g.Count))
}
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## TRANSLATION_REQUIRED sample (max 80)')
$req = $rows | Where-Object { $_.Class -eq 'TRANSLATION_REQUIRED' } | Select-Object -First 80
foreach ($r in $req) {
    [void]$sb.AppendLine(('- {0} :: {1}={2} :: {3}' -f $r.File, $r.Attr, $r.Value, $r.Reason))
}
[System.IO.File]::WriteAllText($mdPath, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))

Write-Host ('Total literals: {0}' -f $rows.Count)
foreach ($g in $grouped) { Write-Host ('{0}: {1}' -f $g.Name, $g.Count) }
Write-Host ('JSON: {0}' -f $jsonPath)
Write-Host ('MD: {0}' -f $mdPath)
Write-Host ('CSV: {0}' -f $csvPath)
