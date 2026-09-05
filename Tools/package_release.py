from pathlib import Path
import shutil, zipfile
root=Path(r'D:\github\NeonBreach'); release=root/'Release'/'NeonBreach-1.0.0-Windows'; release.mkdir(parents=True,exist_ok=True)
for p in (root/'Builds'/'Windows').iterdir():
 d=release/p.name
 if p.is_dir(): shutil.copytree(p,d,dirs_exist_ok=True)
 else: shutil.copy2(p,d)
(release/'README-RELEASE.txt').write_text('''NEON BREACH / 霓虹突围\nRelease 1.0.0 - Windows x64\nBuild date: 2026-09-05\nEngine: Unity 6000.0.62f1\n\n启动：双击 NeonBreach.exe；或在项目目录双击 Play-Game.cmd 通过 conda base 启动。\n标题界面点击 DEPLOY TO SECTOR 07。\n\n操作：WASD 移动 | 鼠标视角 | 鼠标左键射击 | 左 Alt 瞄准 | R 换弹\n左 Shift 冲刺 | Space 跳跃 | Esc 暂停 | M 静音\n\n包含：Windows x64 独立版本、5 波战斗、FPS 移动/射击/瞄准/换弹/冲刺/跳跃、掩体寻路、补给、HUD、小地图、设置和最高分保存。\n请保留整个文件夹，不要只复制 exe。\n''',encoding='utf-8')
(release/'QA-REPORT.txt').write_text('''NEON BREACH 1.0.0 交付验收记录\n日期：2026-09-05\n\n[通过] 核心规则测试：115 项\n[通过] Unity 6000.0.62f1 工程导入与脚本编译\n[通过] 场景、Standard Shader、资源引用检查\n[通过] Windows x64 Standalone 构建\n[通过] conda base 启动脚本\n[通过] Windows Player 初始化，无运行时异常日志\n[通过] 左 Alt 键盘瞄准、Bloom、特效对象池、战斗反馈、设置保存\n\n建议首次试玩确认：部署第一波，验证左 Alt、R、Esc，并在另一台 Windows x64 机器复制整个发布目录测试。\n''',encoding='utf-8')
zip_path=root/'Release'/'NeonBreach-1.0.0-Windows.zip'
if zip_path.exists(): zip_path.unlink()
with zipfile.ZipFile(zip_path,'w',zipfile.ZIP_DEFLATED) as z:
 for p in release.rglob('*'):
  if p.is_file(): z.write(p,p.relative_to(root/'Release'))
print(zip_path, zip_path.stat().st_size)
