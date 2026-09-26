$shortcut = "C:\Users\campo\OneDrive\Desktop\PRIMOX Workshop.lnk"
Write-Host "Abrindo PRIMOX Workshop via atalho da area de trabalho..."
$p = Start-Process -FilePath $shortcut -PassThru
Write-Host "Processo iniciado com PID $($p.Id)."
Start-Sleep -Seconds 2
$p.Refresh()
if (-not $p.HasExited) {
    Write-Host "PRIMOX Workshop em execucao (Janela: '$($p.MainWindowTitle)')."
} else {
    Write-Host "Processo saiu com codigo $($p.ExitCode)."
}
