from pathlib import Path
root = Path(r'D:\github\NeonBreach\Assets\NeonBreach\Scripts')
def edit(path, pairs):
    p=root/path; s=p.read_text(encoding='utf-8-sig')
    for before,after in pairs:
        assert before in s, (path,before[:70])
        s=s.replace(before,after)
    p.write_text(s,encoding='utf-8')
edit(Path('Gameplay/ArenaBootstrap.cs'), [
('var effects = dynamicRoot.gameObject.AddComponent<CombatEffects>(); effects.Initialize(world);', 'var effectRoot = new GameObject("Persistent effect pool"); effectRoot.transform.SetParent(transform, false);\n            var effects = effectRoot.AddComponent<CombatEffects>(); effects.Initialize(world);')])
edit(Path('Gameplay/GameSession.cs'), [
('public int Kills { get; private set; }', '''public int Kills { get; private set; }
        public int ShotsFired { get; private set; }
        public int ShotsHit { get; private set; }
        public int Headshots { get; private set; }
        public float Accuracy => ShotsFired == 0 ? 0 : 100f * ShotsHit / ShotsFired;
        public string KillNotice { get; private set; }
        public float KillNoticeUntil { get; private set; }
        public void RecordShot(bool hit) { ShotsFired++; if (hit) ShotsHit++; }'''),
('Enemies.Clear(); Score = 0;', 'Enemies.Clear(); Effects.Clear(); ShotsFired = ShotsHit = Headshots = 0; KillNoticeUntil = 0; Score = 0;'),
('Kills++; Score += WaveRules.Score(enemy.Heavy, headshot);', '''Kills++; if (headshot) Headshots++;
            int points = WaveRules.Score(enemy.Heavy, headshot); Score += points;
            KillNotice = (headshot ? "HEADSHOT" : "ELIMINATED") + "  /  " + (enemy.Heavy ? "BULWARK" : "SENTRY") + "  +" + points;
            KillNoticeUntil = Time.time + 1.8f;'''),
('NoticeUntil = Time.unscaledTime + duration', 'NoticeUntil = Time.time + duration'),
('State = pause ? MatchState.Paused', 'if (!pause) Player.SaveSettings();\n            State = pause ? MatchState.Paused'),
('Enemies.Clear(); State = MatchState.Title;', 'Enemies.Clear(); Effects.Clear(); Player.SaveSettings(); State = MatchState.Title;'),
('public void Quit()\n        {', 'public void Quit()\n        {\n            Player.SaveSettings();')])
edit(Path('Gameplay/PlayerMotor.cs'), [
('public float Movement { get; private set; }', '''public float BaseFieldOfView { get; set; } = 80;
        public Vector3 LastDamageSource { get; private set; }
        public float DamageDirectionTime { get; private set; }
        public float Movement { get; private set; }'''),
('session = game; gameObject.layer = PlayerLayer;', '''session = game; gameObject.layer = PlayerLayer;
            Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("NeonBreach.Sensitivity", 1.7f), 0.3f, 4);
            BaseFieldOfView = Mathf.Clamp(PlayerPrefs.GetFloat("NeonBreach.FOV", 80), 70, 100);'''),
('View.fieldOfView = 80;', 'View.fieldOfView = BaseFieldOfView;'),
('Weapon.ResetRifle(); Weapon.SetVisible(true);', 'DamageDirectionTime = 0; Weapon.ResetRifle(); Weapon.SetVisible(true);'),
('DamageFlash = Mathf.MoveTowards', 'DamageDirectionTime = Mathf.Max(0, DamageDirectionTime - Time.deltaTime);\n            DamageFlash = Mathf.MoveTowards'),
('yaw += Input.GetAxisRaw("Mouse X") * Sensitivity;\n            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * Sensitivity', 'float lookSensitivity = Sensitivity * (Input.GetMouseButton(1) ? 0.65f : 1);\n            yaw += Input.GetAxisRaw("Mouse X") * lookSensitivity;\n            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * lookSensitivity'),
('Input.GetMouseButton(1) ? 57 : Sprinting ? 87 : 80', 'Input.GetMouseButton(1) ? BaseFieldOfView * 0.7125f : Sprinting ? BaseFieldOfView + 7 : BaseFieldOfView'),
('public void TakeDamage(float amount)', 'public void TakeDamage(float amount, Vector3? source = null)'),
('Health = Mathf.Max(0, Health - amount);', 'if (source.HasValue) { LastDamageSource = source.Value; DamageDirectionTime = 1.2f; }\n            Health = Mathf.Max(0, Health - Mathf.Max(0, amount));'),
('public void Heal(float amount)', '''public void SaveSettings()
        {
            PlayerPrefs.SetFloat("NeonBreach.Sensitivity", Sensitivity);
            PlayerPrefs.SetFloat("NeonBreach.FOV", BaseFieldOfView); PlayerPrefs.Save();
        }
        void OnApplicationQuit() { SaveSettings(); }
        public void Heal(float amount)''')])
edit(Path('Gameplay/EnemyAgent.cs'), [('player.TakeDamage(WaveRules.Damage(session.Wave, Heavy))','player.TakeDamage(WaveRules.Damage(session.Wave, Heavy), origin)')])
edit(Path('Presentation/SynthAudio.cs'), [
('source = gameObject.AddComponent<AudioSource>();', 'Muted = PlayerPrefs.GetInt("NeonBreach.Muted", 0) != 0;\n            source = gameObject.AddComponent<AudioSource>(); source.mute = Muted;'),
('source.mute = Muted; }', 'source.mute = Muted; PlayerPrefs.SetInt("NeonBreach.Muted", Muted ? 1 : 0); PlayerPrefs.Save(); }')])
edit(Path('Gameplay/RifleController.cs'), [
('public float Kick { get; private set; }', 'public float Kick { get; private set; }\n        public float Bloom { get; private set; }'),
('Kick = 0; HitMarker = 0;', 'Kick = 0; Bloom = 0; HitMarker = 0;'),
('cooldown -= Time.deltaTime;', 'Bloom = Mathf.MoveTowards(Bloom, 0, Time.deltaTime * 1.4f);\n            cooldown -= Time.deltaTime;'),
('Vector2 spread = Random.insideUnitCircle * (aimed ? 0.0018f : player.Sprinting ? 0.026f : 0.010f);', 'Vector2 spread = Random.insideUnitCircle * (aimed ? 0.0018f + Bloom * 0.003f : player.Sprinting ? 0.026f : 0.006f + player.Movement * 0.004f + Bloom * 0.012f);\n            Bloom = Mathf.Clamp01(Bloom + 0.22f);\n            bool hitEnemy = false;'),
('bool headshot = box != null && box.Head;', 'hitEnemy = true;\n                    bool headshot = box != null && box.Head;'),
('session.Effects.Beam(muzzle.position, endpoint);', 'session.RecordShot(hitEnemy);\n            session.Effects.Beam(muzzle.position, endpoint);')])
edit(Path('Presentation/ArenaHud.cs'), [
('Time.unscaledTime < session.NoticeUntil', 'Time.time < session.NoticeUntil'),
('if (player.DamageFlash > 0', '''if (session.State == MatchState.Playing && Time.time < session.KillNoticeUntil)
                Text(480, 574, 640, 26, session.KillNotice, 16, ArenaWorld.Cyan, FontStyle.Bold, TextAnchor.UpperCenter);
            if (player.DamageDirectionTime > 0 && session.State == MatchState.Playing)
            {
                Vector3 direction = player.transform.InverseTransformDirection(player.LastDamageSource - player.transform.position);
                Vector2 outward = new Vector2(direction.x, -direction.z).normalized;
                Vector2 tip = new Vector2(800, 450) + outward * 112;
                Vector2 side = new Vector2(-outward.y, outward.x) * 10;
                Color warning = new Color(1, 0.36f, 0.12f, Mathf.Clamp01(player.DamageDirectionTime));
                Line(tip - outward * 15 + side, tip, warning, 4);
                Line(tip - outward * 15 - side, tip, warning, 4);
            }
            if (player.DamageFlash > 0'''),
('rifle.Kick * 12 + session.Player.Movement * 3', 'rifle.Kick * 8 + rifle.Bloom * 15 + session.Player.Movement * 3'),
('Rect(490, 215, 620, 490, ink)', 'Rect(490, 160, 620, 600, ink)'),
('Rect(490, 215, 620, 3, ArenaWorld.Cyan)', 'Rect(490, 160, 620, 3, ArenaWorld.Cyan)'),
('if (Button(540, 552, 250, 49,', '''Text(540, 545, 520, 25, "FIELD OF VIEW   /   " + session.Player.BaseFieldOfView.ToString("F0"), 13, muted);
            session.Player.BaseFieldOfView = GUI.HorizontalSlider(new Rect(540, 580, 520, 24), session.Player.BaseFieldOfView, 70, 100);
            if (Button(540, 620, 250, 49,'''),
('Button(810, 552, 250, 49,', 'Button(810, 620, 250, 49,'),
('Button(540, 620, 520, 45,', 'Button(540, 693, 520, 45,'),
('Text(470, 493, 660, 26, "PERSONAL BEST', 'Text(470, 475, 660, 24, "ACCURACY  " + session.Accuracy.ToString("F0") + "%    /    SHOTS  " + session.ShotsFired + "    /    HEADSHOT KILLS  " + session.Headshots, 14, white);\n            Text(470, 515, 660, 26, "PERSONAL BEST')])
