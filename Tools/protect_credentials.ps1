if (Test-Path 'credenciais-iniciais-admin.txt') {
    Copy-Item 'credenciais-iniciais-admin.txt' -Destination 'credenciais-iniciais-admin.txt.bak' -Force
    $plain = Get-Content -Raw 'credenciais-iniciais-admin.txt'
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($plain)
    $protected = [System.Security.Cryptography.ProtectedData]::Protect($bytes, $null, [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
    $b64 = [Convert]::ToBase64String($protected)
    Set-Content -Path 'credenciais-iniciais-admin.txt' -Value ('__PROTECTED_DPAPI__' + [Environment]::NewLine + $b64) -Force
    Write-Output 'Protected credenciais-iniciais-admin.txt (backup at .bak)'
} else {
    Write-Output 'File not found: credenciais-iniciais-admin.txt'
}
