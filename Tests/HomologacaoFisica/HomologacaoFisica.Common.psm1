Set-StrictMode -Version Latest

$script:HomologacaoStatus = @{
    Approved = "APROVADO"
    ApprovedWithNotes = "APROVADO COM RESSALVAS"
    Failed = "REPROVADO"
    IgnoredHardware = "IGNORADO POR FALTA DE HARDWARE"
    IgnoredPhysical = "IGNORADO POR FALTA DE AMBIENTE FISICO"
}

function Get-HomologacaoProjectRoot {
    param(
        [string]$ScriptPath = $PSScriptRoot
    )

    return Split-Path -Parent (Split-Path -Parent $ScriptPath)
}

function Import-HomologacaoConfig {
    param(
        [string]$Path,
        [string]$ProjectRoot
    )

    $defaultAppData = Join-Path $env:LOCALAPPDATA "PrimoAutoEletrica"
    $defaultConfig = [ordered]@{
        Build = [ordered]@{
            Configuration = "Debug"
            Framework = "net9.0-windows"
        }
        App = [ordered]@{
            AppDataPath = $defaultAppData
            DatabaseSettingsPath = (Join-Path $defaultAppData "database-settings.json")
            CredentialsFile = (Join-Path $ProjectRoot "credenciais-iniciais-admin.txt")
        }
        Environment = [ordered]@{
            MinimumFreeDiskGb = 10
            NetworkProbeHost = "www.microsoft.com"
            NetworkProbePort = 443
            RequiredFolders = @(
                $defaultAppData,
                (Join-Path $defaultAppData "Backups"),
                (Join-Path $defaultAppData "Logs"),
                (Join-Path $defaultAppData "Database"),
                (Join-Path $defaultAppData "Config"),
                (Join-Path $defaultAppData "Reports")
            )
        }
        Network = [ordered]@{
            StationRole = "Cliente"
            PeerAddress = ""
            SharedPath = ""
            SqlConnectionString = ""
            SqlPort = 1433
            LockTimeoutSeconds = 2
            RetryCount = 5
        }
        Printer = [ordered]@{
            PrinterName = ""
            AskManualConfirmation = $true
            ManualConfirmation = ""
        }
        Backup = [ordered]@{
            CriticalTables = @(
                "Clientes",
                "Veiculos",
                "Produtos",
                "Fornecedores",
                "Funcionarios",
                "Orcamentos",
                "OrdensServico",
                "Agendamentos",
                "MovimentacoesFinanceiras"
            )
        }
        Simulation = [ordered]@{
            Days = 7
            SharedAutomationAppData = ""
        }
        CrashRecovery = [ordered]@{
            KillDelaySeconds = 4
        }
        Visual = [ordered]@{
            Resolutions = @(
                "1366x768",
                "1440x900",
                "1600x900",
                "1920x1080",
                "2560x1440"
            )
        }
    }

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path $Path)) {
        return $defaultConfig
    }

    $loaded = Import-PowerShellDataFile -Path $Path
    return Merge-HomologacaoHashtable -Base $defaultConfig -Override $loaded
}

function Merge-HomologacaoHashtable {
    param(
        [hashtable]$Base,
        [hashtable]$Override
    )

    $result = [ordered]@{}
    foreach ($key in $Base.Keys) {
        $result[$key] = $Base[$key]
    }

    foreach ($key in $Override.Keys) {
        if ($result[$key] -is [hashtable] -and $Override[$key] -is [hashtable]) {
            $result[$key] = Merge-HomologacaoHashtable -Base $result[$key] -Override $Override[$key]
            continue
        }

        $result[$key] = $Override[$key]
    }

    return $result
}

function New-HomologacaoCheck {
    param(
        [string]$Name,
        [ValidateSet("Pass", "Warn", "Fail", "Skip")]
        [string]$Outcome,
        [string]$Details,
        [string]$Module = "",
        [string]$ErrorMessage = "",
        [string]$Command = "",
        [string]$AffectedFile = "",
        [string]$PossibleCause = "",
        [string]$SuggestedFix = ""
    )

    return [pscustomobject]@{
        Name = $Name
        Module = $Module
        Outcome = $Outcome
        Details = $Details
        Error = $ErrorMessage
        Command = $Command
        AffectedFile = $AffectedFile
        PossibleCause = $PossibleCause
        SuggestedFix = $SuggestedFix
    }
}

function Get-HomologacaoAreaStatus {
    param(
        [object[]]$Checks,
        [string]$PreferredIgnoredStatus = ""
    )

    $failures = @($Checks | Where-Object { $_.Outcome -eq "Fail" })
    if ($failures.Count -gt 0) {
        return $script:HomologacaoStatus.Failed
    }

    if (-not [string]::IsNullOrWhiteSpace($PreferredIgnoredStatus)) {
        $nonSkipped = @($Checks | Where-Object { $_.Outcome -ne "Skip" })
        if ($nonSkipped.Count -eq 0) {
            return $PreferredIgnoredStatus
        }
    }

    $warnings = @($Checks | Where-Object { $_.Outcome -eq "Warn" })
    if ($warnings.Count -gt 0) {
        return $script:HomologacaoStatus.ApprovedWithNotes
    }

    $passes = @($Checks | Where-Object { $_.Outcome -eq "Pass" })
    if ($passes.Count -eq 0 -and -not [string]::IsNullOrWhiteSpace($PreferredIgnoredStatus)) {
        return $PreferredIgnoredStatus
    }

    return $script:HomologacaoStatus.Approved
}

function Get-HomologacaoAreaScore {
    param(
        [object[]]$Checks,
        [string]$Status
    )

    if ($Status -like "IGNORADO*") {
        return 0
    }

    if (-not $Checks -or $Checks.Count -eq 0) {
        return 0
    }

    $weights = @{
        Pass = 1.0
        Warn = 0.65
        Fail = 0.0
        Skip = 0.45
    }

    $total = 0.0
    foreach ($check in $Checks) {
        $total += [double]$weights[$check.Outcome]
    }

    return [Math]::Round(($total / [double]$Checks.Count) * 10, 1)
}

function New-HomologacaoAreaResult {
    param(
        [string]$AreaId,
        [string]$AreaName,
        [string]$OutputDirectory,
        [object[]]$Checks,
        [hashtable]$Metrics = @{},
        [string]$PreferredIgnoredStatus = "",
        [string[]]$Notes = @()
    )

    $status = Get-HomologacaoAreaStatus -Checks $Checks -PreferredIgnoredStatus $PreferredIgnoredStatus
    $score = Get-HomologacaoAreaScore -Checks $Checks -Status $status

    return [pscustomobject]@{
        AreaId = $AreaId
        AreaName = $AreaName
        GeneratedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        Status = $status
        Score = $score
        OutputDirectory = $OutputDirectory
        Metrics = $Metrics
        Notes = $Notes
        Checks = $Checks
        Files = @{}
    }
}

function Write-HomologacaoAreaArtifacts {
    param(
        [pscustomobject]$AreaResult,
        [string]$JsonPath,
        [string]$TextPath
    )

    $parent = Split-Path -Parent $JsonPath
    if (-not (Test-Path $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $AreaResult | ConvertTo-Json -Depth 10 | Set-Content -Path $JsonPath -Encoding UTF8

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("Area: $($AreaResult.AreaName)")
    $lines.Add("Status: $($AreaResult.Status)")
    $lines.Add("Score: $($AreaResult.Score)")
    $lines.Add("GeneratedAt: $($AreaResult.GeneratedAt)")
    $lines.Add("OutputDirectory: $($AreaResult.OutputDirectory)")
    $lines.Add("")
    $lines.Add("Checks:")

    foreach ($check in $AreaResult.Checks) {
        $lines.Add("- $($check.Name) [$($check.Outcome)] - $($check.Details)")
        if (-not [string]::IsNullOrWhiteSpace($check.Error)) {
            $lines.Add("  Error: $($check.Error)")
        }
        if (-not [string]::IsNullOrWhiteSpace($check.Module)) {
            $lines.Add("  Module: $($check.Module)")
        }
        if (-not [string]::IsNullOrWhiteSpace($check.Command)) {
            $lines.Add("  Command: $($check.Command)")
        }
        if (-not [string]::IsNullOrWhiteSpace($check.AffectedFile)) {
            $lines.Add("  File: $($check.AffectedFile)")
        }
        if (-not [string]::IsNullOrWhiteSpace($check.PossibleCause)) {
            $lines.Add("  PossibleCause: $($check.PossibleCause)")
        }
        if (-not [string]::IsNullOrWhiteSpace($check.SuggestedFix)) {
            $lines.Add("  SuggestedFix: $($check.SuggestedFix)")
        }
    }

    if ($AreaResult.Metrics.Count -gt 0) {
        $lines.Add("")
        $lines.Add("Metrics:")
        foreach ($key in ($AreaResult.Metrics.Keys | Sort-Object)) {
            $lines.Add("- ${key}: $($AreaResult.Metrics[$key])")
        }
    }

    if ($AreaResult.Notes.Count -gt 0) {
        $lines.Add("")
        $lines.Add("Notes:")
        foreach ($note in $AreaResult.Notes) {
            $lines.Add("- $note")
        }
    }

    $lines | Set-Content -Path $TextPath -Encoding UTF8

    $AreaResult.Files = @{
        Json = $JsonPath
        Text = $TextPath
    }

    return $AreaResult
}

function ConvertTo-HomologacaoProcessArgument {
    param(
        [AllowEmptyString()]
        [string]$Argument
    )

    if ($null -eq $Argument) {
        return '""'
    }

    if ($Argument -eq "") {
        return '""'
    }

    if ($Argument -notmatch '[\s"]') {
        return $Argument
    }

    $escaped = $Argument -replace '(\\*)"', '$1$1\"'
    $escaped = $escaped -replace '(\\+)$', '$1$1'
    return '"' + $escaped + '"'
}

function ConvertTo-HomologacaoProcessArgumentString {
    param(
        [string[]]$Arguments = @()
    )

    if (-not $Arguments -or $Arguments.Count -eq 0) {
        return ""
    }

    return (($Arguments | ForEach-Object { ConvertTo-HomologacaoProcessArgument -Argument $_ }) -join " ")
}

function Invoke-HomologacaoCommand {
    param(
        [string]$FilePath,
        [string[]]$Arguments = @(),
        [string]$WorkingDirectory = "",
        [string]$LogPath = "",
        [int]$TimeoutSeconds = 0
    )

    $resolvedWorkingDirectory = if ([string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        (Get-Location).Path
    }
    else {
        $WorkingDirectory
    }

    $resolvedFilePath = if ([System.IO.Path]::IsPathRooted($FilePath) -or $FilePath.Contains("\") -or $FilePath.Contains("/")) {
        $FilePath
    }
    else {
        (Get-Command $FilePath -ErrorAction Stop | Select-Object -First 1 -ExpandProperty Source)
    }

    $argumentString = ConvertTo-HomologacaoProcessArgumentString -Arguments $Arguments
    $commandLine = "$resolvedFilePath $argumentString".Trim()

    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = $resolvedFilePath
    $processInfo.Arguments = $argumentString
    $processInfo.WorkingDirectory = $resolvedWorkingDirectory
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $processInfo

    $startedAt = Get-Date
    $null = $process.Start()

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $timedOut = $false

    if ($TimeoutSeconds -gt 0) {
        $exited = $process.WaitForExit($TimeoutSeconds * 1000)
        if (-not $exited) {
            $timedOut = $true
            try {
                $process.Kill()
            }
            catch {
            }
        }
    }
    else {
        $process.WaitForExit()
    }

    $process.WaitForExit()

    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    $outputLines = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($stdout)) {
        foreach ($line in ($stdout -split "`r?`n")) {
            if ($null -ne $line) {
                $outputLines.Add($line)
            }
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($stderr)) {
        foreach ($line in ($stderr -split "`r?`n")) {
            if ($null -ne $line) {
                $outputLines.Add($line)
            }
        }
    }

    if ($timedOut) {
        $outputLines.Add("[TIMEOUT] Processo excedeu ${TimeoutSeconds}s e foi encerrado.")
        $exitCode = 124
    }
    else {
        $exitCode = $process.ExitCode
        if ($null -eq $exitCode) {
            $exitCode = 0
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($LogPath)) {
        $parent = Split-Path -Parent $LogPath
        if (-not (Test-Path $parent)) {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }
        $outputLines | Set-Content -Path $LogPath -Encoding UTF8
    }

    return [pscustomobject]@{
        CommandLine = $commandLine
        ExitCode = [int]$exitCode
        Output = @($outputLines)
        LogPath = $LogPath
        TimedOut = $timedOut
        DurationSeconds = [Math]::Round(((Get-Date) - $startedAt).TotalSeconds, 1)
    }
}

function Get-LatestFileAfter {
    param(
        [string]$Path,
        [string]$Filter = "*",
        [datetime]$StartedAtUtc
    )

    if (-not (Test-Path $Path)) {
        return $null
    }

    return Get-ChildItem -Path $Path -File -Filter $Filter |
        Where-Object { $_.LastWriteTimeUtc -ge $StartedAtUtc.AddSeconds(-2) } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}

function Read-KeyValueReport {
    param(
        [string]$ReportPath
    )

    $map = @{}
    if (-not (Test-Path $ReportPath)) {
        return $map
    }

    foreach ($line in Get-Content -Path $ReportPath) {
        if ($line -match '^(?<Key>[A-Za-z]+):\s*(?<Value>.*)$') {
            $map[$matches.Key] = $matches.Value.Trim()
        }
    }

    return $map
}

function Read-AutomationCheckReport {
    param(
        [string]$ReportPath
    )

    if (-not (Test-Path $ReportPath)) {
        return @()
    }

    $checks = New-Object System.Collections.Generic.List[object]
    $lines = Get-Content -Path $ReportPath
    for ($index = 0; $index -lt $lines.Count; $index++) {
        $line = $lines[$index]
        if ($line -match '^\[(?<Kind>PASS|FAIL)\]\s+(?<Name>.+?)\s+\((?<Duration>\d+)\s+ms\)$') {
            $kind = $matches.Kind
            $name = $matches.Name
            $duration = [int]$matches.Duration
            $message = "OK"
            if (($index + 1) -lt $lines.Count -and $lines[$index + 1] -match '^Message:\s*(?<Message>.*)$') {
                $message = $matches.Message
            }

            $checks.Add([pscustomobject]@{
                Name = $name
                Success = ($kind -eq "PASS")
                DurationMs = $duration
                Message = $message
            })
        }
    }

    return $checks
}

function Invoke-UiSmokeRun {
    param(
        [string]$ProjectRoot,
        [hashtable]$Build,
        [string]$OutputDirectory,
        [string]$SmokeFilter = "",
        [string]$AppDataPath = "",
        [switch]$SkipBuild,
        [int]$BuildTimeoutSeconds = 180,
        [int]$RunTimeoutSeconds = 300
    )

    $appProject = Join-Path $ProjectRoot "PrimoAutoEletrica\PrimoAutoEletrica.csproj"
    $reportRoot = Join-Path $ProjectRoot "PrimoAutoEletrica\bin\$($Build.Configuration)\$($Build.Framework)\Logs\smoke-tests"
    $commandLog = Join-Path $OutputDirectory "ui-smoke-command.log"
    $buildLog = Join-Path $OutputDirectory "ui-smoke-build.log"
    $previousReport = if (Test-Path $reportRoot) {
        Get-ChildItem -Path $reportRoot -File -Filter "*.txt" |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1
    }
    else {
        $null
    }

    if (-not $SkipBuild) {
        $buildResult = Invoke-HomologacaoCommand -FilePath "dotnet" -Arguments @("build", $appProject, "-c", $Build.Configuration) -WorkingDirectory $ProjectRoot -LogPath $buildLog -TimeoutSeconds $BuildTimeoutSeconds
        if ($buildResult.ExitCode -ne 0) {
            return [pscustomobject]@{
                Status = $script:HomologacaoStatus.Failed
                ExitCode = $buildResult.ExitCode
                CommandLine = $buildResult.CommandLine
                CommandLogPath = $buildResult.LogPath
                ReportPath = ""
                Checks = @()
                Summary = @{}
            }
        }
    }

    $startedAtUtc = [datetime]::UtcNow
    $arguments = @("run", "--project", $appProject, "-c", $Build.Configuration)
    if ($SkipBuild) {
        $arguments += "--no-build"
    }
    $arguments += "--"
    $arguments += "--smoke-test"
    if (-not [string]::IsNullOrWhiteSpace($SmokeFilter)) {
        $arguments += "--smoke-filter=$SmokeFilter"
    }
    if (-not [string]::IsNullOrWhiteSpace($AppDataPath)) {
        $arguments += "--app-data=$AppDataPath"
    }

    $run = Invoke-HomologacaoCommand -FilePath "dotnet" -Arguments $arguments -WorkingDirectory $ProjectRoot -LogPath $commandLog -TimeoutSeconds $RunTimeoutSeconds
    $reportFile = Get-LatestFileAfter -Path $reportRoot -Filter "*.txt" -StartedAtUtc $startedAtUtc
    if ($null -eq $reportFile -and $null -ne $previousReport) {
        $candidate = Get-ChildItem -Path $reportRoot -File -Filter "*.txt" |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1
        if ($null -ne $candidate -and ($candidate.FullName -ne $previousReport.FullName -or $candidate.LastWriteTimeUtc -gt $previousReport.LastWriteTimeUtc)) {
            $reportFile = $candidate
        }
    }

    $summary = if ($null -ne $reportFile) { Read-KeyValueReport -ReportPath $reportFile.FullName } else { @{} }
    $checks = if ($null -ne $reportFile) { Read-AutomationCheckReport -ReportPath $reportFile.FullName } else { @() }
    $failedChecks = if ($summary.ContainsKey("FailedChecks")) { [int]$summary.FailedChecks } else { 1 }
    $status = if ($run.ExitCode -eq 0 -and $failedChecks -eq 0) {
        $script:HomologacaoStatus.Approved
    }
    else {
        $script:HomologacaoStatus.Failed
    }

    $evidencePath = if ($null -ne $reportFile) { $reportFile.FullName } else { $run.LogPath }

    return [pscustomobject]@{
        Status = $status
        ExitCode = $run.ExitCode
        CommandLine = $run.CommandLine
        CommandLogPath = $run.LogPath
        ReportPath = $evidencePath
        Checks = $checks
        Summary = $summary
    }
}

function Invoke-WorkflowRun {
    param(
        [string]$ProjectRoot,
        [hashtable]$Build,
        [string]$OutputDirectory,
        [string]$AppDataPath = "",
        [switch]$SkipBuild,
        [int]$BuildTimeoutSeconds = 180,
        [int]$RunTimeoutSeconds = 300
    )

    $appProject = Join-Path $ProjectRoot "PrimoAutoEletrica\PrimoAutoEletrica.csproj"
    $reportRoot = Join-Path $ProjectRoot "PrimoAutoEletrica\bin\$($Build.Configuration)\$($Build.Framework)\Logs\workflow-tests"
    $commandLog = Join-Path $OutputDirectory "workflow-command.log"
    $buildLog = Join-Path $OutputDirectory "workflow-build.log"
    $previousReport = if (Test-Path $reportRoot) {
        Get-ChildItem -Path $reportRoot -File -Filter "*.txt" |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1
    }
    else {
        $null
    }

    if (-not $SkipBuild) {
        $buildResult = Invoke-HomologacaoCommand -FilePath "dotnet" -Arguments @("build", $appProject, "-c", $Build.Configuration) -WorkingDirectory $ProjectRoot -LogPath $buildLog -TimeoutSeconds $BuildTimeoutSeconds
        if ($buildResult.ExitCode -ne 0) {
            return [pscustomobject]@{
                Status = $script:HomologacaoStatus.Failed
                ExitCode = $buildResult.ExitCode
                CommandLine = $buildResult.CommandLine
                CommandLogPath = $buildResult.LogPath
                ReportPath = ""
                Checks = @()
                Summary = @{}
            }
        }
    }

    $startedAtUtc = [datetime]::UtcNow
    $arguments = @("run", "--project", $appProject, "-c", $Build.Configuration)
    if ($SkipBuild) {
        $arguments += "--no-build"
    }
    $arguments += "--"
    $arguments += "--workflow-test"
    if (-not [string]::IsNullOrWhiteSpace($AppDataPath)) {
        $arguments += "--app-data=$AppDataPath"
    }

    $run = Invoke-HomologacaoCommand -FilePath "dotnet" -Arguments $arguments -WorkingDirectory $ProjectRoot -LogPath $commandLog -TimeoutSeconds $RunTimeoutSeconds
    $reportFile = Get-LatestFileAfter -Path $reportRoot -Filter "*.txt" -StartedAtUtc $startedAtUtc
    if ($null -eq $reportFile -and $null -ne $previousReport) {
        $candidate = Get-ChildItem -Path $reportRoot -File -Filter "*.txt" |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1
        if ($null -ne $candidate -and ($candidate.FullName -ne $previousReport.FullName -or $candidate.LastWriteTimeUtc -gt $previousReport.LastWriteTimeUtc)) {
            $reportFile = $candidate
        }
    }

    $summary = if ($null -ne $reportFile) { Read-KeyValueReport -ReportPath $reportFile.FullName } else { @{} }
    $checks = if ($null -ne $reportFile) { Read-AutomationCheckReport -ReportPath $reportFile.FullName } else { @() }
    $failedChecks = if ($summary.ContainsKey("FailedChecks")) { [int]$summary.FailedChecks } else { 1 }
    $status = if ($run.ExitCode -eq 0 -and $failedChecks -eq 0) {
        $script:HomologacaoStatus.Approved
    }
    else {
        $script:HomologacaoStatus.Failed
    }

    $evidencePath = if ($null -ne $reportFile) { $reportFile.FullName } else { $run.LogPath }

    return [pscustomobject]@{
        Status = $status
        ExitCode = $run.ExitCode
        CommandLine = $run.CommandLine
        CommandLogPath = $run.LogPath
        ReportPath = $evidencePath
        Checks = $checks
        Summary = $summary
    }
}

function Invoke-DotNetTest {
    param(
        [string]$ProjectRoot,
        [string]$ProjectPath,
        [hashtable]$Build,
        [string]$OutputDirectory,
        [string]$Label,
        [string]$Filter = "",
        [switch]$NoBuild,
        [int]$TimeoutSeconds = 600
    )

    $logPath = Join-Path $OutputDirectory "$Label.log"
    $trxName = "$Label.trx"
    $arguments = @(
        "test",
        $ProjectPath,
        "-c",
        $Build.Configuration,
        "--logger",
        "trx;LogFileName=$trxName",
        "--results-directory",
        $OutputDirectory
    )

    if ($NoBuild) {
        $arguments += "--no-build"
    }

    if (-not [string]::IsNullOrWhiteSpace($Filter)) {
        $arguments += "--filter"
        $arguments += $Filter
    }

    $result = Invoke-HomologacaoCommand -FilePath "dotnet" -Arguments $arguments -WorkingDirectory $ProjectRoot -LogPath $logPath -TimeoutSeconds $TimeoutSeconds
    $trxPath = Join-Path $OutputDirectory $trxName
    $cases = Read-TrxTestCases -Path $trxPath

    return [pscustomobject]@{
        ExitCode = $result.ExitCode
        CommandLine = $result.CommandLine
        CommandLogPath = $result.LogPath
        TrxPath = $trxPath
        Cases = $cases
        Passed = @($cases | Where-Object { $_.Outcome -eq "Passed" }).Count
        Failed = @($cases | Where-Object { $_.Outcome -eq "Failed" }).Count
        Skipped = @($cases | Where-Object { $_.Outcome -eq "NotExecuted" }).Count
    }
}

function Read-TrxTestCases {
    param(
        [string]$Path
    )

    if (-not (Test-Path $Path)) {
        return @()
    }

    $namespace = @{ ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010" }
    $items = Select-Xml -Path $Path -Namespace $namespace -XPath "//ns:UnitTestResult"

    return @($items | ForEach-Object {
        [pscustomobject]@{
            TestName = $_.Node.testName
            Outcome = $_.Node.outcome
            Duration = $_.Node.duration
        }
    })
}

function Get-EffectiveDatabaseSettings {
    param(
        [hashtable]$Config
    )

    $appDataPath = $Config.App.AppDataPath
    $settingsPath = if ([string]::IsNullOrWhiteSpace($Config.App.DatabaseSettingsPath)) {
        Join-Path $appDataPath "database-settings.json"
    }
    else {
        $Config.App.DatabaseSettingsPath
    }

    $settings = if (Test-Path $settingsPath) {
        Get-Content -Raw -Path $settingsPath | ConvertFrom-Json
    }
    else {
        $null
    }

    return [pscustomobject]@{
        AppDataPath = $appDataPath
        SettingsPath = $settingsPath
        Settings = $settings
    }
}

function New-TemporaryPythonFile {
    param(
        [string]$Directory,
        [string]$Prefix,
        [string]$Content
    )

    if (-not (Test-Path $Directory)) {
        New-Item -ItemType Directory -Path $Directory -Force | Out-Null
    }

    $path = Join-Path $Directory ("{0}-{1}.py" -f $Prefix, [guid]::NewGuid().ToString("N"))
    $Content | Set-Content -Path $path -Encoding UTF8
    return $path
}

Export-ModuleMember -Function @(
    "Get-HomologacaoProjectRoot",
    "Import-HomologacaoConfig",
    "New-HomologacaoCheck",
    "New-HomologacaoAreaResult",
    "Write-HomologacaoAreaArtifacts",
    "Invoke-HomologacaoCommand",
    "Invoke-UiSmokeRun",
    "Invoke-WorkflowRun",
    "Invoke-DotNetTest",
    "Read-AutomationCheckReport",
    "Read-KeyValueReport",
    "Read-TrxTestCases",
    "Get-EffectiveDatabaseSettings",
    "New-TemporaryPythonFile"
)
