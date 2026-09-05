@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Project.ps1" -Action Validate
if errorlevel 1 pause
