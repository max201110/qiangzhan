@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Unity.ps1" -Action Open %*
if errorlevel 1 pause
