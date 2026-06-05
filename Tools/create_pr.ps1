param(
    [string]$Title = "Feature: local-sync-ui-smoke",
    [string]$BodyFile = "PR_BODY.md",
    [string]$Base = "main",
    [string]$Head = "feature/local-sync-ui-smoke",
    [string]$Owner = "CamposCodingHub",
    [string]$Repo = "PrimoAutoEletrica"
)

function Use-GhCli {
    try {
        $null = gh --version 2>$null
        return $true
    } catch {
        return $false
    }
}

if (Use-GhCli) {
    Write-Host "Using gh CLI to create PR..."
    gh pr create --title $Title --body-file $BodyFile --base $Base --head $Head
    exit $LASTEXITCODE
}

if (-not $env:GITHUB_TOKEN) {
    Write-Host "No gh CLI and no GITHUB_TOKEN found. Please set GITHUB_TOKEN or install gh CLI." -ForegroundColor Yellow
    Write-Host "You can create the PR manually at: https://github.com/$Owner/$Repo/pull/new/$Head"
    exit 1
}

$body = Get-Content -Raw -Path $BodyFile
$json = @{ title = $Title; head = $Head; base = $Base; body = $body } | ConvertTo-Json -Depth 6

$uri = "https://api.github.com/repos/$Owner/$Repo/pulls"
Write-Host "Creating PR via GitHub API..."

$resp = Invoke-RestMethod -Method Post -Uri $uri -Headers @{ Authorization = "token $($env:GITHUB_TOKEN)"; Accept = 'application/vnd.github.v3+json' } -Body $json -ContentType 'application/json'
Write-Host "PR created: $($resp.html_url)"
