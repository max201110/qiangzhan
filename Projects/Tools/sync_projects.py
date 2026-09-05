"""Reproducible source sync. Only managed runtime/editor files are overwritten."""
from pathlib import Path
import json, shutil
ROOT=Path(__file__).resolve().parents[1]
PARENT=ROOT.parent
DEFS={
 'NeonArena':dict(title='Neon Arena',subtitle='REACTOR DEFENSE / THIRD-PERSON SURVIVAL',mode=0,missions=[
 dict(name='First Contact',brief='Hold the industrial district against three assault waves. Keep the reactor alive, then evacuate north.',seed=171,waves=3,enemies=4,timeLimit=0),
 dict(name='Lockdown',brief='Checkpoint gates redirect the enemy assault. Four waves threaten the reactor. Buy upgrades before deployment.',seed=281,waves=4,enemies=5,timeLimit=0),
 dict(name='Last Light',brief='The final defense. Five waves, heavier fire and new blast walls. Protect the reactor and bring your pilot home.',seed=391,waves=5,enemies=6,timeLimit=0)]),
 'MechAssault':dict(title='Mech Assault',subtitle='ARMORED WARFARE / HEAT & TARGET CONTROL',mode=1,missions=[
 dict(name='Relay Breaker',brief='Destroy three enemy relay towers and survive two assault waves. The last wave deploys a Warden siege unit.',seed=441,waves=2,enemies=4,timeLimit=0),
 dict(name='Iron Corridor',brief='The district is barricaded. Break the relay network, manage core heat and destroy the siege unit.',seed=552,waves=3,enemies=5,timeLimit=0),
 dict(name='Warden Protocol',brief='Cripple the final network. Four waves and a hardened Warden stand between your mech and extraction.',seed=663,waves=4,enemies=6,timeLimit=0)]),
 'TacticalInfiltration':dict(title='Tactical Infiltration',subtitle='SILENT OPERATIONS / PATROL & INTELLIGENCE',mode=2,missions=[
 dict(name='Ghost Signal',brief='Download intelligence from three terminals. Avoid patrol cones and extract north. Shooting and sprinting reveal your position.',seed=771,waves=0,enemies=6,timeLimit=600),
 dict(name='Black Site',brief='Eight patrol guards control the checkpoint district. Use distractions and rear takedowns to open a safe route.',seed=882,waves=0,enemies=8,timeLimit=540),
 dict(name='Silent Exit',brief='Ten guards patrol the final compound. Recover all data before the six-minute deadline and escape.',seed=993,waves=0,enemies=10,timeLimit=360)])
}
# Remove only the explicitly superseded starter files; preserve all unrelated work.
OLD=['NeonArena/Assets/Scripts/NeonArenaBootstrap.cs','NeonArena/Assets/Scripts/GameState.cs','NeonArena/Assets/Scripts/WeaponSystem.cs','NeonArena/Assets/Scripts/WaveDirector.cs','NeonArena/Assets/Scripts/LootPickup.cs','MechAssault/Assets/Scripts/MechAssaultController.cs','MechAssault/Assets/Scripts/MechHealth.cs','MechAssault/Assets/Scripts/HeatManager.cs','MechAssault/Assets/Scripts/TargetLock.cs','MechAssault/Assets/Scripts/Projectile.cs','TacticalInfiltration/Assets/Scripts/StealthGuard.cs','TacticalInfiltration/Assets/Scripts/AlarmSystem.cs','TacticalInfiltration/Assets/Scripts/VisionSensor.cs','TacticalInfiltration/Assets/Scripts/CoverPoint.cs','TacticalInfiltration/Assets/Scripts/MissionObjective.cs']
for rel in OLD:
 p=ROOT/rel
 for item in [p,Path(str(p)+'.meta')]:
  if item.is_file():item.unlink()
for name,d in DEFS.items():
 project=ROOT/name
 for folder in ['Assets/Runtime','Assets/Editor','Assets/Resources','Assets/Scenes','ProjectSettings','Packages','Tools']:(project/folder).mkdir(parents=True,exist_ok=True)
 for kind in ['Runtime','Editor']:
  for src in (ROOT/'_Framework'/kind).glob('*.cs'):shutil.copy2(src,project/'Assets'/kind/src.name)
 (project/'Assets/Runtime'/f'{name}.Runtime.asmdef').write_text(json.dumps(dict(name=f'{name}.Runtime',rootNamespace='Frontier'),indent=2))
 (project/'Assets/Editor'/f'{name}.Editor.asmdef').write_text(json.dumps(dict(name=f'{name}.Editor',references=[f'{name}.Runtime'],includePlatforms=['Editor']),indent=2))
 (project/'Assets/Resources/GameDefinition.json').write_text(json.dumps(dict(id=name,**d),indent=2),encoding='utf-8')
 manifest=json.loads((PARENT/'Packages/manifest.json').read_text(encoding='utf-8-sig'))
 manifest['dependencies'].update({'com.unity.modules.screencapture':'1.0.0','com.unity.modules.imageconversion':'1.0.0'})
 (project/'Packages/manifest.json').write_text(json.dumps(manifest,indent=2))
 for settings in ['ProjectVersion.txt','InputManager.asset','TagManager.asset','ProjectSettings.asset','QualitySettings.asset']:
  dest=project/'ProjectSettings'/settings
  if not dest.exists():shutil.copy2(PARENT/'ProjectSettings'/settings,dest)
 runner=ROOT/'Tools/Project.ps1'
 if runner.exists():shutil.copy2(runner,project/'Tools/Project.ps1')
 (project/'Play.cmd').write_text(f'@echo off\r\ncd /d "%~dp0"\r\nif not exist "Builds\\Windows\\{name}.exe" (\r\n echo Run Build.cmd first.\r\n pause\r\n exit /b 1\r\n)\r\nwhere conda >nul 2>&1\r\nif not errorlevel 1 call conda activate base\r\nstart "" "Builds\\Windows\\{name}.exe"\r\n',encoding='utf-8')
 for action in ['Open','Build','Validate']:
  (project/f'{action}.cmd').write_text(f'@echo off\r\npowershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\\Project.ps1" -Action {action}\r\nif errorlevel 1 pause\r\n',encoding='utf-8')
 print('Synced',name)
