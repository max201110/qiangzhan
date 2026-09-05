param(
    [ValidateSet('Open', 'Build', 'Validate')][string]$Action = 'Open',
    [string]$EditorPath
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$preferred = '6000.0.62f1'
if (!$EditorPath -and $env:UNITY_EDITOR_PATH) { $EditorPath = $env:UNITY_EDITOR_PATH }
if (!$EditorPath) {
    $candidates = @()
    $hubEditors = Join-Path $env:APPDATA 'UnityHub\editors-v2.json'
    if (Test-Path -LiteralPath $hubEditors) {
        try {
            $data = Get-Content -Raw -LiteralPath $hubEditors | ConvertFrom-Json
            $entries = @($data.data) + @($data)
            foreach ($entry in $entries) {
                if ($entry.location) {
                    $p = [string]$entry.location
                    if ((Split-Path $p -Leaf) -ne 'Unity.exe') { $p = Join-Path $p 'Editor\Unity.exe' }
                    if (Test-Path -LiteralPath $p) { $candidates += $p }
                }
            }
        } catch { Write-Warning 'Could not read Unity Hub editor list.' }
    }
    foreach ($base in @("$env:ProgramFiles\Unity\Hub\Editor", 'D:\Unity\Hub\Editor', 'D:\Unity', 'D:\Program Files\Unity\Hub\Editor')) {
        if (Test-Path -LiteralPath $base) {
            $candidates += @(Get-ChildItem -LiteralPath $base -Directory | ForEach-Object {
                $p = Join-Path $_.FullName 'Editor\Unity.exe'
                if (Test-Path -LiteralPath $p) { $p }
            })
        }
    }
    $EditorPath = $candidates | Where-Object { $_ -match [regex]::Escape($preferred) } | Select-Object -First 1
    if (!$EditorPath) { $EditorPath = $candidates | Where-Object { $_ -match '6000\.' } | Sort-Object -Descending | Select-Object -First 1 }
}
if (!$EditorPath -or !(Test-Path -LiteralPath $EditorPath -PathType Leaf)) {
    Write-Host ''
    Write-Host 'Unity Editor not found.' -ForegroundColor Yellow
    Write-Host "Install a Unity 6 (6000.x) Editor through Unity Hub, then add this project: $project"
    Write-Host 'For custom paths: .\Tools\Unity.ps1 -Action Open -EditorPath "D:\YourUnity\Editor\Unity.exe"'
    Write-Host 'The selected Editor must support Windows builds with the Mono backend.'
    exit 1
}
$EditorPath = (Resolve-Path -LiteralPath $EditorPath).Path
if ($Action -eq 'Open') {
    # This is the interactive editor the user explicitly wants to open.
    Start-Process -FilePath $EditorPath -ArgumentList @('-projectPath', ('"' + $project + '"'))
    Write-Host "Opened Unity project: $project"
    exit 0
}
$logDir = Join-Path $project 'Logs'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$log = Join-Path $logDir ($Action.ToLowerInvariant() + '.log')
$method = if ($Action -eq 'Build') { 'NeonBreach.Editor.BuildTools.BuildWindows' } else { 'NeonBreach.Editor.BuildTools.ValidateProject' }
$args = @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $project + '"'), '-executeMethod', $method, '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $EditorPath -ArgumentList $args -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log -Tail 80 }
    throw "Unity $Action failed with exit code $($process.ExitCode). See $log"
}
Write-Host "Unity $Action completed. Log: $log" -ForegroundColor Green
