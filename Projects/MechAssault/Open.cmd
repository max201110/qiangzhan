@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Project.ps1" -Action Open
if errorlevel 1 pause
