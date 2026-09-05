@echo off
setlocal
set "CONDA_EXE=D:\anaconda\Scripts\conda.exe"
if not exist "%CONDA_EXE%" (
    echo Conda was not found at %CONDA_EXE%.
    echo You can run Builds\Windows\NeonBreach.exe directly.
    pause
    exit /b 1
)
call "%CONDA_EXE%" run -n base --no-capture-output python "%~dp0Tools\launch_game.py"
if errorlevel 1 pause
