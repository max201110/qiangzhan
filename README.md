# NEON BREACH / 霓虹突围

一个基于 **Unity 6 + C# + Built-in Render Pipeline** 的 3D 第一人称枪战原型。单人对抗机器人，守住工业竞技场并完成 5 波战斗；支持训练、标准、极限三档难度。

> **当前交付状态（2026-09-05）**：Unity 6.0.62f1 已安装，工程已完成真实导入、脚本编译、场景/着色器验证，并成功生成 Windows x64 构建。当前版本增加了战斗特效对象池、射击 Bloom、击杀提示、受击方向提示、命中率统计，以及 FOV/灵敏度/静音设置保存。

## 快速开始

1. 使用 Unity Hub 安装 **Unity 6 Editor**。本工程记录版本为 **6000.0.62f1**，采用内置渲染管线。使用其他 6000.x 版本时，请允许 Unity 进行工程升级；不同版本兼容性尚未实测。
2. 在 Unity Hub 中选择 **Projects → Add / Add project from disk**，选中 `D:\github\NeonBreach`。
3. 等待首次导入与脚本编译完成。
4. 打开 `D:\github\NeonBreach\Assets\NeonBreach\Scenes\Arena.unity`；也可以使用编辑器菜单 **Neon Breach → Open Arena**。
5. 点击 Unity 顶部 **▶ Play**，然后点击游戏中的 **DEPLOY TO SECTOR 07**。
6. 首次进入请点击 Game 窗口使其获得焦点；建议以 16:9、1600×900 或 1920×1080 查看。HUD 对其他宽高比做了等比缩放。

已经生成 Windows 版本，可直接双击 `D:\github\NeonBreach\Play-Game.cmd`，它会使用 conda base 环境启动游戏。

安装 Editor 后，也可以直接双击 `D:\github\NeonBreach\Open-Game.cmd`。启动脚本会检查 Unity Hub 记录和常见安装路径。自定义路径可这样指定：

```powershell
& 'D:\github\NeonBreach\Tools\Unity.ps1' -Action Open -EditorPath 'D:\YourUnity\Editor\Unity.exe'
```

**场景在编辑状态下只有一个 Bootstrap 对象是正常的。** 点击 Play 后，竞技场、灯光、枪械、机器人、HUD 和音效会由代码创建。不需要导入 Asset Store 素材、下载模型或烘焙 NavMesh。

## 操作

| 按键 | 功能 |
| --- | --- |
| W / A / S / D | 移动 |
| 鼠标 | 转动视角 |
| 鼠标左键（按住） | 全自动射击 |
| 左 Alt（按住） | 瞄准、缩小散布 |
| R | 换弹 |
| V | 全自动 / 半自动切换 |
| 左 Shift | 向前冲刺，消耗耐力 |
| Space | 跳跃 |
| Esc | 暂停 / 继续，释放 / 捕获鼠标 |
| M | 静音 / 恢复音效 |

暂停菜单提供鼠标灵敏度调整、声音开关、重新开始和返回标题。切换到其他窗口会自动暂停，返回后点击 Resume。

## 已实现的内容

- **第一人称操作**：CharacterController 移动、重力、跳跃、耐力冲刺、瞄准视野变化、持枪晃动。
- **VX-07 自动步枪**：30 发弹匣、150 发初始备用弹药、1.65 秒换弹、射击间隔与散布、后坐力、枪口闪光、曳光线、命中反馈、爆头加成。
- **机器人 AI**：普通 Sentry 与重型 Bulwark，A* 网格寻路、绕掩体移动、视线判断、横移与近距离后退、攻击前预瞄与白色面罩提示。敌人在预瞄后锁定旧位置，玩家可以通过移动躲避。
- **五波战斗**：分别部署 6、8、10、12、14 个敌人；逐波增加敌人血量和伤害。击杀、爆头和通关奖励计分。
- **补给与战间恢复**：橙色弹药箱 +60 备用弹药；青色医疗箱 +35 血量；波次之间恢复 25 血量并补充 75 发备用弹药。弹药上限 300。
- **3D 工业场景**：地面网格、装甲掩体、中央反应堆、部署平台、围墙、上方钢架、远景建筑、青橙灯光、雾与阴影。
- **完整 UI**：开始菜单、血量、耐力、弹药、波次、敌人数、分数、计时、小地图、敌人血条、换弹进度、暂停和胜负结算。
- **程序化音效**：射击、命中、击杀、装弹、受伤、补给和波次提示。没有外部音频依赖。
- **本机最高分**：通过 Unity PlayerPrefs 保存；菜单的重新开始只重置当前局。

游戏 HUD 使用英文；中文操作说明在此文档中。当前是低多边形单人原型，不包含联网、写实角色动画、任务剧情、手柄/移动端适配或音量混音界面。

## 导出 Windows 游戏

本工程的 Windows 构建后端设为 **Mono**。请确保所选 Editor 具备 Windows 构建支持；如果编辑器提示缺少平台支持，按提示通过 Unity Hub 补装相应模块。关闭正在占用本工程的 Unity 实例后，双击：

`D:\github\NeonBreach\Build-Windows.cmd`

或者在已经打开的 Unity 内使用：**Neon Breach → Build Windows x64**。

构建成功后的入口：

`D:\github\NeonBreach\Builds\Windows\NeonBreach.exe`

分发时请复制整个 Windows 构建文件夹，包括 `_Data` 和 Unity 运行库，不要只复制 `.exe`。无 Editor、未激活许可证、缺少构建模块或脚本编译失败时，构建脚本会报错；日志保存在 `D:\github\NeonBreach\Logs`。

## 开发与验证

纯 C# 逻辑和源文件检查（需要 PowerShell 7，不需要 Unity）：

```powershell
pwsh -NoProfile -File 'D:\github\NeonBreach\Tools\Test-Core.ps1'
```

用安装好的 Unity 做脚本编译、场景引用和基础规则检查：

```powershell
& 'D:\github\NeonBreach\Tools\Unity.ps1' -Action Validate -EditorPath 'D:\YourUnity\Editor\Unity.exe'
```

Unity 内也有菜单 **Neon Breach → Validate Project**。完整试玩清单与当前验证状态见 `D:\github\NeonBreach\TESTING.md`。

### 源码结构

```text
Assets/NeonBreach/
├── Scenes/Arena.unity                  # 入口场景
├── Scripts/Core/
│   ├── CombatRules.cs                  # 弹药状态机、波次规则（无 Unity 依赖）
│   └── GridPathfinder.cs               # 四向 A* 寻路（无 Unity 依赖）
├── Scripts/Gameplay/
│   ├── ArenaBootstrap.cs              # 创建场景与系统
│   ├── GameSession.cs                 # 标题、战斗、暂停、胜负和波次
│   ├── PlayerMotor.cs                 # 玩家移动与血量
│   ├── RifleController.cs             # 枪械模型、射击与换弹
│   ├── EnemyAgent.cs                  # AI、攻击与弱点判定
│   └── Pickup.cs                      # 医疗与弹药拾取
├── Scripts/World/ArenaWorld.cs         # 场景、材质、灯光、导航障碍
├── Scripts/Presentation/
│   ├── ArenaHud.cs                    # 菜单、HUD、小地图
│   ├── CombatEffects.cs               # 曳光线、火花和碎片
│   └── SynthAudio.cs                  # 本地合成音效
├── Resources/StandardShaderAnchor.mat  # 保留程序化模型所用的 Standard Shader
└── Editor/BuildTools.cs                # 场景打开、检查与 Windows 构建菜单
```

常用修改点：敌人数量/血量/伤害在 `CombatRules.cs`，地图与掩体在 `ArenaWorld.cs`，步枪伤害/射速/散布在 `RifleController.cs`，玩家速度与耐力在 `PlayerMotor.cs`。地图范围为约 54×54 米，导航网格为 50×50。

## 故障排查

- **编辑器里场景是空的**：打开 Arena 场景并按 Play；此项目故意在运行时构建地图。
- **鼠标无法转动 / Input 报错**：Project Settings → Player → Other Settings → Active Input Handling 设为 **Input Manager (Old)** 或 **Both**，必要时重启 Editor；本工程使用旧输入 API。
- **模型显示粉色**：确认使用 Built-in Render Pipeline，没有额外指定 URP/HDRP 管线资产；不要将本工程作为 URP 模板导入。
- **找不到 Unity**：通过 `-EditorPath` 指定真正的 `Unity.exe`，不是 Unity Hub 可执行文件。
- **无法构建**：先在 Unity 中解决 Console 编译错误，确认激活许可证、Windows 构建模块和工程未被另一实例占用。
- **没有听到声音**：按 M 检查是否静音，确认 Unity Game 视图没有开启 Mute Audio，以及系统音量正常。
- **Game 视图捕获鼠标**：按 Esc 暂停并释放鼠标。
