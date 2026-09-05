param()
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Run this check with PowerShell 7 (pwsh), which includes the Roslyn C# compiler.' }
$project = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$core = Join-Path $project 'Assets\NeonBreach\Scripts\Core'
Add-Type -Path @((Join-Path $core 'CombatRules.cs'), (Join-Path $core 'GridPathfinder.cs'))
$script:count = 0
function Assert([bool]$condition, [string]$message) {
    if (!$condition) { throw "FAIL: $message" }
    $script:count++
    Write-Host "PASS: $message"
}

$a = [NeonBreach.Core.AmmoState]::new(30, 90)
Assert ($a.Magazine -eq 30 -and $a.Reserve -eq 90) 'Initial ammunition'
Assert (!$a.StartReload(1)) 'Cannot reload a full magazine'
for ($i=0; $i -lt 30; $i++) { Assert ($a.TryShoot()) "Valid shot $($i+1)" }
Assert (!$a.TryShoot()) 'Empty magazine refuses fire'
Assert ($a.StartReload(1.65)) 'Reload starts with reserve'
Assert (!$a.TryShoot()) 'Cannot fire while reloading'
Assert (!$a.StartReload(1)) 'Cannot start a duplicate reload'
Assert (!$a.Tick(0.5)) 'Reload waits for duration'
Assert ($a.Tick(1.2)) 'Reload completes'
Assert ($a.Magazine -eq 30 -and $a.Reserve -eq 60 -and !$a.Reloading) 'Reload transfers exactly 30 rounds'
Assert (!$a.Tick(10)) 'Completed reload is not repeated'
$a.Supply(999)
Assert ($a.Reserve -eq 300) 'Reserve capacity clamps at 300'
$a.Supply(-100)
Assert ($a.Reserve -eq 300) 'Negative supply cannot remove ammo'
$b = [NeonBreach.Core.AmmoState]::new(5, 2)
1..5 | ForEach-Object { [void]$b.TryShoot() }
[void]$b.StartReload(1)
Assert (!$b.Tick(-1)) 'Negative time does not advance reload'
[void]$b.Tick(2)
Assert ($b.Magazine -eq 2 -and $b.Reserve -eq 0) 'Partial reserve refill'
[void]$b.TryShoot()
Assert (!$b.StartReload(1)) 'Reload cannot start without reserve'
$caught=$false
try { [void][NeonBreach.Core.AmmoState]::new(0, 10) } catch { $caught=$true }
Assert $caught 'Zero capacity rejected'
Assert ([NeonBreach.Core.WaveRules]::EnemyCount(1) -eq 6) 'Wave one spawns six enemies'
Assert ([NeonBreach.Core.WaveRules]::EnemyCount(5) -eq 14) 'Wave five spawns fourteen enemies'
Assert ([NeonBreach.Core.WaveRules]::EnemyCount(100) -eq 24) 'Enemy count has a performance cap'
Assert ([NeonBreach.Core.WaveRules]::Health(5,$true) -gt [NeonBreach.Core.WaveRules]::Health(1,$false)) 'Heavy enemies scale health'
Assert ([NeonBreach.Core.WaveRules]::Score($true,$true) -eq 230) 'Heavy headshot awards 230 points'
Assert ([NeonBreach.Core.WaveRules]::SpawnDelay(100) -ge 0.44) 'Spawn delay remains bounded'
function Cell([int]$x,[int]$z) { return [NeonBreach.Core.Cell]::new($x,$z) }
$grid=[NeonBreach.Core.GridPathfinder]::new(8,8)
$path=$grid.FindPath((Cell 0 0),(Cell 7 7))
Assert ($path.Count -eq 14) 'Open-grid shortest path'
Assert ($grid.FindPath((Cell 2 2),(Cell 2 2)).Count -eq 0) 'Start equals goal'
for($z=0;$z -lt 7;$z++){ $grid.Block(3,$z) }
$path=$grid.FindPath((Cell 1 1),(Cell 6 1))
Assert ($path.Count -eq 17) 'A-star routes around cover through the gap'
$prev=Cell 1 1
foreach($step in $path){
    Assert ($grid.IsOpen($step)) 'Path only contains traversable cells'
    Assert (([Math]::Abs($step.X-$prev.X)+[Math]::Abs($step.Z-$prev.Z)) -eq 1) 'Path is connected without diagonal corner cutting'
    $prev=$step
}
$grid.Block(3,7)
Assert ($grid.FindPath((Cell 1 1),(Cell 6 1)).Count -eq 0) 'Unreachable goal returns empty path'
Assert (!$grid.IsOpen((Cell -1 0))) 'Out-of-range cell rejected'
Assert ($grid.IsOpen($grid.NearestOpen((Cell 3 3)))) 'Blocked target is projected to a free cell'
$closed=[NeonBreach.Core.GridPathfinder]::new(2,2)
for($x=0;$x -lt 2;$x++){for($z=0;$z -lt 2;$z++){$closed.Block($x,$z)}}
Assert ($closed.FindPath((Cell 0 0),(Cell 1 1)).Count -eq 0) 'Fully blocked grid terminates'

# Reconstruct the authored arena occupancy and prove every deployment pad can reach the player.
$arena=[NeonBreach.Core.GridPathfinder]::new(50,50)
$cover=@(@(-10,-12,7,3.5),@(10,-12,7,3.5),@(-10,12,7,3.5),@(10,12,7,3.5),@(-17,0,3,7),@(17,0,3,7),@(-6,-3,3,2.5),@(6,3,3,2.5),@(0,0,3,3))
for($x=0;$x -lt 50;$x++){for($z=0;$z -lt 50;$z++){
    foreach($box in $cover){
        if([Math]::Abs($x-24.5-$box[0]) -le ($box[2]/2+0.55) -and [Math]::Abs($z-24.5-$box[1]) -le ($box[3]/2+0.55)){$arena.Block($x,$z)}
    }
}}
foreach($spawn in @(@(3,47),@(47,47),@(3,3),@(47,3),@(25,48))){
    Assert ($arena.FindPath((Cell $spawn[0] $spawn[1]),(Cell 25 5)).Count -gt 0) "Arena spawn ($($spawn[0]),$($spawn[1])) reaches operator"
}
# Verify all C# files with the actual Roslyn parser, both player and editor preprocessor branches.
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')
foreach($file in Get-ChildItem -LiteralPath (Join-Path $project 'Assets') -Filter '*.cs' -Recurse){
    foreach($symbols in @('', 'UNITY_EDITOR')){
        $options=[Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithPreprocessorSymbols([string[]]@($symbols | Where-Object {$_}))
        $tree=[Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([string](Get-Content -Raw -LiteralPath $file.FullName), $options)
        $errors=@($tree.GetDiagnostics() | Where-Object {$_.Severity -eq 'Error'})
        if($errors.Count){$errors | ForEach-Object {Write-Host $_.ToString()}; throw "C# syntax errors in $($file.Name) [$symbols]"}
    }
    Assert $true "C# syntax: $($file.Name) (player + editor)"
}
$manifest=Get-Content -Raw -LiteralPath (Join-Path $project 'Packages\manifest.json') | ConvertFrom-Json
Assert ($manifest.dependencies.'com.unity.modules.physics' -eq '1.0.0') 'Physics package declared'
$scene=Get-Content -Raw -LiteralPath (Join-Path $project 'Assets\NeonBreach\Scenes\Arena.unity')
$meta=Get-Content -Raw -LiteralPath (Join-Path $project 'Assets\NeonBreach\Scripts\Gameplay\ArenaBootstrap.cs.meta')
Assert ($scene -match 'a45b10355fb04187bf40445a5c1e0302' -and $meta -match 'a45b10355fb04187bf40445a5c1e0302') 'Scene bootstrap GUID resolves'
$guids=@{}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $project 'Assets') -Recurse -Filter '*.meta'){
    $content=Get-Content -Raw -LiteralPath $file.FullName
    if($content -match '(?m)^guid: ([0-9a-f]{32})\r?$'){
        if($guids.ContainsKey($Matches[1])){throw "Duplicate GUID: $($file.FullName)"}
        $guids[$Matches[1]]=$file.FullName
    } else {throw "Invalid meta: $($file.FullName)"}
}
Assert ($guids.Count -gt 15) 'Asset GUIDs are valid and unique'
$tokens=$null;$parseErrors=$null
[void][System.Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Unity.ps1'),[ref]$tokens,[ref]$parseErrors)
Assert ($parseErrors.Count -eq 0) 'Unity launcher PowerShell syntax'
Write-Host "`n$count checks passed. Unity-dependent API compilation, visual QA, input/audio, and player builds still require Unity Editor." -ForegroundColor Green

