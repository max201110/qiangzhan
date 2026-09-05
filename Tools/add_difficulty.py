from pathlib import Path
root=Path(r'D:\github\NeonBreach\Assets\NeonBreach\Scripts')
p=root/'Gameplay/GameSession.cs'; s=p.read_text(encoding='utf-8-sig')
s=s.replace('public enum MatchState { Title, Playing, Paused, Defeat, Victory }','public enum MatchState { Title, Playing, Paused, Defeat, Victory }\n    public enum Difficulty { Training, Standard, Extreme }')
s=s.replace('public MatchState State { get; private set; } = MatchState.Title;', '''public MatchState State { get; private set; } = MatchState.Title;
        public Difficulty Difficulty { get; private set; } = Difficulty.Standard;
        public string DifficultyName => Difficulty == Difficulty.Training ? "TRAINING" : Difficulty == Difficulty.Extreme ? "EXTREME" : "STANDARD";
        public float EnemyHealthMultiplier => Difficulty == Difficulty.Training ? 0.78f : Difficulty == Difficulty.Extreme ? 1.28f : 1f;
        public float EnemyDamageMultiplier => Difficulty == Difficulty.Training ? 0.72f : Difficulty == Difficulty.Extreme ? 1.32f : 1f;''')
s=s.replace('public void Begin()\n        {', '''public void SetDifficulty(Difficulty difficulty) { if (State == MatchState.Title) Difficulty = difficulty; }
        public void Begin()
        {''')
s=s.replace('pendingSpawns = WaveRules.EnemyCount(Wave);', 'pendingSpawns = WaveRules.EnemyCount(Wave) + (Difficulty == Difficulty.Extreme ? 2 : Difficulty == Difficulty.Training ? -1 : 0);')
p.write_text(s,encoding='utf-8')
p=root/'Gameplay/EnemyAgent.cs'; s=p.read_text(encoding='utf-8-sig').replace('MaxHealth = Health = WaveRules.Health(game.Wave, heavy);','MaxHealth = Health = WaveRules.Health(game.Wave, heavy) * game.EnemyHealthMultiplier;').replace('WaveRules.Damage(session.Wave, Heavy), origin','WaveRules.Damage(session.Wave, Heavy) * session.EnemyDamageMultiplier, origin')
p.write_text(s,encoding='utf-8')
p=root/'Presentation/ArenaHud.cs'; s=p.read_text(encoding='utf-8-sig')
s=s.replace('if (session.State == MatchState.Title) Title();','''if (session.State == MatchState.Title)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) session.SetDifficulty(Difficulty.Training);
                if (Input.GetKeyDown(KeyCode.Alpha2)) session.SetDifficulty(Difficulty.Standard);
                if (Input.GetKeyDown(KeyCode.Alpha3)) session.SetDifficulty(Difficulty.Extreme);
                Title();
            }''')
s=s.replace('Text(74, 471, 520, 55,', 'Text(74, 435, 520, 25, "DIFFICULTY  /  " + session.DifficultyName + "   [1] TRAINING  [2] STANDARD  [3] EXTREME", 13, ArenaWorld.Orange, FontStyle.Bold);\n            Text(74, 471, 520, 55,')
s=s.replace('"WAVE " + session.Wave.ToString("00")', 'session.DifficultyName + "  /  WAVE " + session.Wave.ToString("00")')
s=s.replace('"WAVE    " + session.Wave + " / 5\\nKILLS', '"DIFFICULTY  " + session.DifficultyName + "\\nWAVE    " + session.Wave + " / 5\\nKILLS')
p.write_text(s,encoding='utf-8')
p=Path(r'D:\github\NeonBreach\README.md'); s=p.read_text(encoding='utf-8-sig')
s=s.replace('单人对抗机器人，守住工业竞技场并完成 5 波战斗。','单人对抗机器人，守住工业竞技场并完成 5 波战斗；支持训练、标准、极限三档难度。')
s=s.replace('### 操作','### 难度选择\n标题界面按 `1` 训练、`2` 标准、`3` 极限。极限会增加敌人数量、生命值和伤害；训练模式相反。\n\n### 操作')
p.write_text(s,encoding='utf-8')
