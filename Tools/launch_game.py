"""Launch the Unity Windows player from the active Python/conda environment."""
from pathlib import Path
import subprocess
import sys

root = Path(__file__).resolve().parents[1]
exe = root / "Builds" / "Windows" / "NeonBreach.exe"
if not exe.is_file():
    sys.exit(f"Game build not found: {exe}. Run Build-Windows.cmd first.")
logs = root / "Logs"
logs.mkdir(exist_ok=True)
process = subprocess.Popen(
    [str(exe), "-screen-fullscreen", "0", "-screen-width", "1600",
     "-screen-height", "900", "-logFile", str(logs / "player.log")],
    cwd=exe.parent, stdin=subprocess.DEVNULL,
    stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
)
print(f"Launched NEON BREACH (PID {process.pid}) using {sys.executable}")
