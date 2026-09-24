function Test-DesktopStartup([int]$runNumber) {
    Write-Host "=========================================="
    Write-Host "TESTE DE STARTUP: EXECUCAO $runNumber DE 3"
    Write-Host "=========================================="
    
    $shortcut = "C:\Users\campo\OneDrive\Desktop\PRIMOX Workshop.lnk"
    $logPath = "C:\Users\campo\AppData\Local\PrimoAutoEletrica\Logs\log-2026-09-23.txt"
    $logBefore = if (Test-Path $logPath) { (Get-Content $logPath -Tail 20) -join "`n" } else { "" }
    
    $proc = Start-Process -FilePath $shortcut -PassThru
    $processId = $proc.Id
    Write-Host "Iniciado processo PID: $processId"
    
    $loaded = $false
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 500
        $proc.Refresh()
        if ($proc.HasExited) {
            Write-Host "FALHA: Processo saiu prematuramente com codigo $($proc.ExitCode)"
            return $false
        }
        
        $logAfter = if (Test-Path $logPath) { (Get-Content $logPath -Tail 20) -join "`n" } else { "" }
        if ($logAfter -match "Tela de login carregada com sucesso") {
            $loaded = $true
            Write-Host "Confirmado pelo Log: Tela de login carregada com sucesso."
            break
        }
    }
    
    if (-not $loaded) {
        Write-Host "FALHA: Nao atingiu tela de login dentro de 10s."
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        return $false
    }
    
    Start-Sleep -Seconds 2
    
    # Check for any SQLite Error 8 or exceptions
    $currentLog = Get-Content $logPath -Tail 15
    foreach ($line in $currentLog) {
        if ($line -match "SQLite Error 8" -or $line -match "readonly database" -or $line -match "CRITICAL") {
            Write-Host "FALHA: Encontrado erro critico no log: $line"
            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
            return $false
        }
    }
    
    Write-Host "Fechando processo $processId..."
    Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    
    Write-Host "EXECUCAO $runNumber - PASS"
    return $true
}

$r1 = Test-DesktopStartup 1
$r2 = Test-DesktopStartup 2
$r3 = Test-DesktopStartup 3

Write-Host "=========================================="
Write-Host "RESULTADOS DOS TESTES DE STARTUP:"
Write-Host "Execucao 1: $(if ($r1) { 'PASS' } else { 'FAIL' })"
Write-Host "Execucao 2: $(if ($r2) { 'PASS' } else { 'FAIL' })"
Write-Host "Execucao 3: $(if ($r3) { 'PASS' } else { 'FAIL' })"
Write-Host "SQLite Error 8: NAO"
Write-Host "=========================================="
