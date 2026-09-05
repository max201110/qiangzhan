param([ValidateSet('Open','Build','Validate')][string]$Action='Open',[string]$EditorPath)
$ErrorActionPreference='Stop'
$project=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if (!$EditorPath) { $EditorPath=$env:UNITY_EDITOR_PATH }
if (!$EditorPath) {
    foreach ($p in @('D:\Unity\Hub\Editor\6000.0.62f1\Editor\Unity.exe', "$env:ProgramFiles\Unity\Hub\Editor\6000.0.62f1\Editor\Unity.exe")) {
        if (Test-Path -LiteralPath $p) { $EditorPath=$p; break }
    }
}
if (!$EditorPath -or !(Test-Path -LiteralPath $EditorPath)) { throw 'Unity 6000.0.62f1 not found. Set UNITY_EDITOR_PATH or use -EditorPath.' }
New-Item -ItemType Directory -Force -Path (Join-Path $project 'Logs') | Out-Null
$log=Join-Path $project "Logs\$($Action.ToLowerInvariant()).log"
$args=@('-projectPath', ('"'+$project+'"'))
if ($Action -eq 'Open') { Start-Process -FilePath $EditorPath -ArgumentList $args; exit 0 }
$method=if ($Action -eq 'Build') { 'Frontier.Editor.ProjectBuilder.Build' } else { 'Frontier.Editor.ProjectBuilder.Validate' }
$args+=@('-batchmode','-nographics','-quit','-executeMethod',$method,'-logFile',('"'+$log+'"'))
$p=Start-Process -FilePath $EditorPath -ArgumentList $args -WindowStyle Hidden -PassThru -Wait
if ($p.ExitCode -ne 0) { Get-Content -LiteralPath $log -Tail 90; throw "Unity $Action failed: $($p.ExitCode)" }
Write-Host "$Action succeeded: $project"
