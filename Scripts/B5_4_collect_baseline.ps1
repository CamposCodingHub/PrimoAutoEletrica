# Collect Baseline for B5.4
$ErrorActionPreference = 'Stop'

$installedExe = Join-Path $env:LOCALAPPDATA 'PrimoAutoEletrica\App\PrimoAutoEletrica.exe'
$desktop = [Environment]::GetFolderPath([Environment+SpecialFolder]::Desktop)
$desktopLnk = Join-Path $desktop 'PRIMOX Workshop.lnk'
if (-not (Test-Path $desktopLnk)) {
    $desktopLnk = Join-Path $env:USERPROFILE 'OneDrive\Desktop\PRIMOX Workshop.lnk'
}

Write-Host "=== INSTALLED EXE ==="
if (Test-Path $installedExe) {
    Get-Item $installedExe | Select-Object FullName, Length, LastWriteTime | Format-List
    (Get-Item $installedExe).VersionInfo | Format-List
} else {
    Write-Host "Installed exe not found: $installedExe"
}

Write-Host "=== DESKTOP SHORTCUT ==="
if (Test-Path $desktopLnk) {
    Get-Item $desktopLnk | Select-Object FullName, Length, LastWriteTime | Format-List
    $sh = New-Object -ComObject WScript.Shell
    $sc = $sh.CreateShortcut($desktopLnk)
    Write-Host "TargetPath: $($sc.TargetPath)"
    Write-Host "Arguments: $($sc.Arguments)"
    Write-Host "WorkingDirectory: $($sc.WorkingDirectory)"
} else {
    Write-Host "Desktop shortcut not found: $desktopLnk"
}

Write-Host "=== INNO SETUP ==="
$candidates = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 7\ISCC.exe'),
    'C:\Program Files\Inno Setup 7\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe',
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files (x86)\Inno Setup 7\ISCC.exe'
)
$iscc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($iscc) {
    Write-Host "ISCC found at: $iscc"
    & $iscc 2>&1 | Select-Object -First 3
} else {
    Write-Host "ISCC not found"
}

Write-Host "=== RUNTIME & ARCHITECTURE ==="
Write-Host "Dotnet version: $(dotnet --version)"
Write-Host "OS Architecture: $([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture)"
Write-Host "Process Architecture: $([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture)"
Write-Host "OS Description: $([System.Runtime.InteropServices.RuntimeInformation]::OSDescription)"
