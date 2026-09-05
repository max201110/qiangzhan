@echo off
cd /d "%~dp0"
if not exist "Builds\Windows\NeonArena.exe" (
 echo Run Build.cmd first.
 pause
 exit /b 1
)
where conda >nul 2>&1
if not errorlevel 1 call conda activate base
start "" "Builds\Windows\NeonArena.exe"
