[CmdletBinding()]
param(
    [string]$ConfigPath = "",
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot "HomologacaoFisica.Common.psm1") -Force
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

function Save-DesktopScreenshot {
    param(
        [string]$Path
    )

    $bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$projectRoot = Get-HomologacaoProjectRoot -ScriptPath $PSScriptRoot
$config = Import-HomologacaoConfig -Path $ConfigPath -ProjectRoot $projectRoot

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $projectRoot ("TestResults\HomologacaoFisica\{0}" -f (Get-Date -Format "yyyy-MM-dd_HH-mm-ss"))
}

$outputDirectory = Join-Path $OutputRoot "Visual"
$screenshotsDirectory = Join-Path $projectRoot "TestResults\HomologacaoFisica\Screenshots"
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $screenshotsDirectory -Force | Out-Null

$currentBounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$currentResolution = "{0}x{1}" -f $currentBounds.Width, $currentBounds.Height
$checks = New-Object System.Collections.Generic.List[object]
$resolutionResults = New-Object System.Collections.Generic.List[object]

foreach ($resolution in @($config.Visual.Resolutions)) {
    if ($resolution -eq $currentResolution) {
        $beforeShot = Join-Path $screenshotsDirectory ("visual-{0}-before.png" -f $resolution.Replace("x", "-"))
        Save-DesktopScreenshot -Path $beforeShot
        $smoke = Invoke-UiSmokeRun -ProjectRoot $projectRoot -Build $config.Build -OutputDirectory (Join-Path $outputDirectory ("Smoke-" + $resolution.Replace("x", "-"))) -AppDataPath (Join-Path $outputDirectory ("VisualAppData-" + $resolution.Replace("x", "-")))
        $afterShot = Join-Path $screenshotsDirectory ("visual-{0}-after.png" -f $resolution.Replace("x", "-"))
        Save-DesktopScreenshot -Path $afterShot

        $resolutionResults.Add([pscustomobject]@{
            Resolution = $resolution
            Status = $smoke.Status
            BeforeScreenshot = $beforeShot
            AfterScreenshot = $afterShot
            SmokeReport = $smoke.ReportPath
        })

        $checks.Add((New-HomologacaoCheck -Name ("Resolution:" + $resolution) -Outcome $(if ($smoke.Status -eq "APROVADO") { "Pass" } else { "Fail" }) -Details "UI smoke completo executado na resolucao ativa $resolution." -Module "Visual" -Command $smoke.CommandLine -AffectedFile $smoke.ReportPath -PossibleCause "Telas nao abriram corretamente na resolucao atual." -SuggestedFix "Abrir o relatorio de smoke e corrigir o modulo com falha visual ou de layout." ))
    }
    else {
        $resolutionResults.Add([pscustomobject]@{
            Resolution = $resolution
            Status = "IGNORADO POR FALTA DE AMBIENTE FISICO"
            BeforeScreenshot = ""
            AfterScreenshot = ""
            SmokeReport = ""
        })

        $checks.Add((New-HomologacaoCheck -Name ("Resolution:" + $resolution) -Outcome "Skip" -Details "Resolucao fisica atual da estacao e $currentResolution; a rotina nao troca resolucao automaticamente." -Module "Visual" -PossibleCause "A homologacao nao foi rodada nesta resolucao alvo." -SuggestedFix "Ajustar a resolucao do Windows para $resolution e executar novamente este script." ))
    }
}

$checks.Add((New-HomologacaoCheck -Name "VisualEvidenceMode" -Outcome "Warn" -Details "A automacao usa smoke completo e screenshots da estacao atual; deteccao automatica de botao cortado, texto sobreposto e modal fora da tela ainda depende da leitura humana das imagens." -Module "Visual" -PossibleCause "Nao existe diff visual semantico nesta suite." -SuggestedFix "Revisar as screenshots geradas antes do go-live." ))

$validatedCount = @($resolutionResults | Where-Object { $_.Resolution -eq $currentResolution }).Count
$preferredIgnored = if ($validatedCount -eq 0) { "IGNORADO POR FALTA DE AMBIENTE FISICO" } else { "" }

$area = New-HomologacaoAreaResult `
    -AreaId "visual-resolution" `
    -AreaName "Validacao visual em resolucoes diferentes" `
    -OutputDirectory $outputDirectory `
    -Checks $checks `
    -PreferredIgnoredStatus $preferredIgnored `
    -Metrics @{
        CurrentResolution = $currentResolution
        ScreenshotsDirectory = $screenshotsDirectory
        Resolutions = $resolutionResults
    }

Write-HomologacaoAreaArtifacts -AreaResult $area -JsonPath (Join-Path $OutputRoot "visual-resolution-summary.json") -TextPath (Join-Path $OutputRoot "visual-resolution-summary.txt")
