@echo off
chcp 65001 >nul
title Atualizar Primo Auto Eletrica
cd /d "%~dp0.."
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Deploy-ToInstalledApp.ps1" -ForceStop -Launch
echo.
pause
