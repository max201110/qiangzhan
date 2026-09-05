using UnityEngine;

namespace NeonBreach
{
    // Self-contained immediate-mode HUD: no TMP assets, Canvas setup or package downloads.
    public sealed class ArenaHud : MonoBehaviour
    {
        GameSession session;
        Texture2D pixel;
        GUIStyle label, button;
        readonly Color ink = new Color(0.022f, 0.04f, 0.063f, 0.95f);
        readonly Color muted = new Color(0.45f, 0.59f, 0.65f);
        readonly Color white = new Color(0.9f, 0.96f, 1);
        float smoothDelta = 0.016f;
        public void Initialize(GameSession game) { session = game; }
        void Awake()
        {
            pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply();
        }
        void Update() { smoothDelta = Mathf.Lerp(smoothDelta, Time.unscaledDeltaTime, 0.05f); }
        void Styles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = false, alignment = TextAnchor.UpperLeft, padding = new RectOffset(0, 0, 0, 0) };
            button = new GUIStyle(GUI.skin.button) { fontSize = 19, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(22, 12, 0, 0), border = new RectOffset(0, 0, 0, 0) };
            button.normal.background = pixel; button.hover.background = pixel; button.active.background = pixel;
            button.normal.textColor = ink; button.hover.textColor = ink; button.active.textColor = ink;
        }
        void OnGUI()
        {
            if (session == null || session.Player == null) return;
            Styles();
            Matrix4x4 original = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            float ox = (Screen.width - 1600 * scale) / 2, oy = (Screen.height - 900 * scale) / 2;
            // Backdrop fills letterboxing too, while UI preserves its aspect ratio.
            if (session.State != MatchState.Playing) Rect(0, 0, Screen.width, Screen.height, new Color(0.005f, 0.014f, 0.025f, session.State == MatchState.Title ? 0.36f : 0.77f));
            GUI.matrix = Matrix4x4.TRS(new Vector3(ox, oy, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            if (session.State == MatchState.Title)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) session.SetDifficulty(Difficulty.Training);
                if (Input.GetKeyDown(KeyCode.Alpha2)) session.SetDifficulty(Difficulty.Standard);
                if (Input.GetKeyDown(KeyCode.Alpha3)) session.SetDifficulty(Difficulty.Extreme);
                Title();
            }
            else
            {
                Hud();
                if (session.State == MatchState.Paused) Pause();
                if (session.State == MatchState.Defeat || session.State == MatchState.Victory) Result();
            }
            GUI.matrix = original; GUI.color = Color.white;
        }
        void Title()
        {
            Rect(0, 0, 720, 900, new Color(0.014f, 0.027f, 0.045f, 0.91f));
            Rect(70, 72, 38, 4, ArenaWorld.Cyan);
            Text(120, 63, 460, 26, "O P E R A T O R   /   S U R V I V A L   P R O T O C O L", 13, ArenaWorld.Cyan);
            Text(66, 155, 620, 110, "NEON", 103, white, FontStyle.Bold);
            Text(66, 258, 620, 110, "BREACH", 103, ArenaWorld.Cyan, FontStyle.Bold);
            Text(74, 390, 520, 60, "ONE OPERATOR. FIVE WAVES.\nHOLD THE SECTOR.", 23, white, FontStyle.Bold);
            Text(74, 435, 520, 25, "DIFFICULTY  /  " + session.DifficultyName + "   [1] TRAINING  [2] STANDARD  [3] EXTREME", 13, ArenaWorld.Orange, FontStyle.Bold);
            Text(74, 471, 520, 55, "An ion rifle. A hostile machine network.\nKeep moving, use cover, and make every shot count.", 17, muted);
            if (Button(74, 562, 460, 64, "DEPLOY TO SECTOR 07                       >", ArenaWorld.Cyan)) session.Begin();
            if (Button(74, 640, 220, 48, "QUIT", new Color(0.21f, 0.31f, 0.37f))) session.Quit();
            Text(320, 654, 270, 26, "PERSONAL BEST  /  " + session.BestScore.ToString("N0"), 14, muted);
            Rect(74, 737, 560, 1, new Color(0.20f, 0.30f, 0.36f));
            Text(74, 761, 560, 25, "W A S D  MOVE      MOUSE  LOOK      LMB  FIRE", 14, white);
            Text(74, 793, 560, 25, "ALT  AIM      G  GRENADE      V  FIRE MODE      R  RELOAD      SHIFT  SPRINT      C  CROUCH      SPACE  JUMP", 13, muted);
            Text(74, 835, 560, 25, "ESC  PAUSE     /     M  SOUND     /     SINGLE-PLAYER PROTOTYPE", 11, muted);
            Rect(1175, 70, 355, 108, ink); Rect(1175, 70, 3, 108, ArenaWorld.Cyan);
            Text(1198, 88, 300, 26, "SECTOR STATUS", 13, muted);
            Text(1198, 117, 300, 40, "CONTAINMENT LOST", 24, ArenaWorld.Orange, FontStyle.Bold);
            Text(1200, 797, 330, 24, "VX-07  /  ION ASSAULT SYSTEM", 14, white);
            Text(1200, 825, 330, 24, "30 ROUND MAG  -  HEADSHOT MULTIPLIER", 11, muted);
        }
        void Hud()
        {
            var player = session.Player; var ammo = player.Weapon.Ammo;
            Rect(34, 28, 292, 75, ink); Rect(34, 28, 3, 75, ArenaWorld.Cyan);
            Text(53, 43, 260, 20, "N E O N   B R E A C H   /   S E C T O R   0 7", 11, muted);
            Text(53, 67, 265, 30, "HOLD THE SECTOR", 21, white, FontStyle.Bold);
            Rect(578, 28, 444, 75, ink);
            Text(599, 42, 390, 20, "CONTAINMENT PROTOCOL", 11, muted, FontStyle.Normal, TextAnchor.UpperCenter);
            Text(592, 66, 415, 30, "WAVE  " + session.Wave.ToString("00") + " / 05     -     HOSTILES  " + session.Remaining.ToString("00"), 21, white, FontStyle.Bold, TextAnchor.UpperCenter);
            Text(1220, 30, 345, 27, "SCORE  " + session.Score.ToString("D6"), 23, ArenaWorld.Cyan, FontStyle.Bold, TextAnchor.UpperRight);
            Text(1230, 64, 335, 22, FormatTime(session.Elapsed) + "   /   " + Mathf.RoundToInt(1 / Mathf.Max(0.001f, smoothDelta)) + " FPS", 12, muted, FontStyle.Normal, TextAnchor.UpperRight);
            if (session.State == MatchState.Playing)
            {
                Crosshair(); EnemyBars();
                if (Time.time < session.NoticeUntil)
                {
                    Rect(478, 137, 644, 48, new Color(0.025f, 0.08f, 0.10f, 0.90f));
                    Text(490, 151, 620, 25, session.Notice, 17, ArenaWorld.Cyan, FontStyle.Bold, TextAnchor.UpperCenter);
                }
                if (session.Intermission > 0) Text(500, 200, 600, 35, "NEXT DEPLOYMENT  /  " + Mathf.CeilToInt(session.Intermission), 19, white, FontStyle.Normal, TextAnchor.UpperCenter);
            }
            Rect(34, 748, 330, 120, ink); Rect(34, 748, 3, 120, player.Health < 30 ? ArenaWorld.Orange : ArenaWorld.Cyan);
            Text(54, 766, 130, 22, "VITAL SYSTEMS", 12, muted);
            Text(53, 792, 100, 51, Mathf.CeilToInt(player.Health).ToString("D3"), 42, player.Health < 30 ? ArenaWorld.Orange : white, FontStyle.Bold);
            Text(160, 814, 170, 20, "HEALTH / 100", 12, muted);
            Rect(54, 850, 286, 5, new Color(0.15f, 0.24f, 0.29f)); Rect(54, 850, 286 * player.Health / 100, 5, player.Health < 30 ? ArenaWorld.Orange : ArenaWorld.Cyan);
            Rect(34, 721, 330, 4, new Color(0.12f, 0.20f, 0.25f)); Rect(34, 721, 330 * player.Stamina, 4, ArenaWorld.Cyan);
            Text(36, 698, 330, 19, "MOBILITY  /  " + (player.Crouching ? "CROUCHING" : player.Sprinting ? "SPRINT ACTIVE" : "READY"), 10, muted);
            Rect(1250, 748, 316, 120, ink); Rect(1563, 748, 3, 120, ArenaWorld.Cyan);
            Text(1270, 765, 270, 22, "VX-07  /  ION RIFLE", 13, muted);
            Text(1270, 790, 112, 55, ammo.Magazine.ToString("D2"), 46, ammo.Magazine <= 6 ? ArenaWorld.Orange : white, FontStyle.Bold);
            Text(1380, 808, 170, 35, "/ " + ammo.Reserve.ToString("D3"), 25, muted);
            Text(1270, 848, 270, 20, ammo.Reloading ? "RELOADING ..." : (player.Weapon.Automatic ? "FULL AUTO" : "SEMI AUTO") + "   /   V TO TOGGLE", 10, ammo.Reloading ? ArenaWorld.Orange : muted);
            if (ammo.Reloading)
            {
                Rect(700, 517, 200, 4, new Color(0.14f, 0.22f, 0.27f));
                Rect(700, 517, 200 * Mathf.Clamp01(1 - ammo.RemainingReload / RifleController.ReloadDuration), 4, ArenaWorld.Cyan);
                Text(650, 534, 300, 24, "RELOADING", 12, white, FontStyle.Normal, TextAnchor.UpperCenter);
            }
            else if (ammo.Magazine == 0 && ammo.Reserve == 0) Text(550, 535, 500, 24, "OUT OF AMMO  /  FIND AN ORANGE SUPPLY CELL", 14, ArenaWorld.Orange, FontStyle.Bold, TextAnchor.UpperCenter);
            Text(540, 827, 520, 22, "KILL STREAK  /  x" + session.KillStreak + "    ACCURACY  " + session.Accuracy.ToString("F0") + "%", 10, session.KillStreak >= 3 ? ArenaWorld.Orange : muted, FontStyle.Normal, TextAnchor.UpperCenter);
            Text(540, 851, 520, 22, "G  GRENADE (" + session.Grenades + ")    R  RELOAD    -    SHIFT  SPRINT    -    ESC  PAUSE    -    M  " + (session.Audio.Muted ? "UNMUTE" : "MUTE"), 10, muted, FontStyle.Normal, TextAnchor.UpperCenter);
            Radar();
            if (session.State == MatchState.Playing && session.Wave == 5 && session.Remaining > 0) Text(565, 113, 470, 25, "FINAL WAVE  /  OVERSEER ACTIVE", 13, ArenaWorld.Orange, FontStyle.Bold, TextAnchor.UpperCenter);
            if (session.State == MatchState.Playing && Time.time < session.KillNoticeUntil)
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
            if (player.DamageFlash > 0 && session.State == MatchState.Playing)
            {
                Color hurt = new Color(1, 0.12f, 0.055f, player.DamageFlash * 0.45f);
                Rect(0, 0, 1600, 12, hurt); Rect(0, 888, 1600, 12, hurt); Rect(0, 0, 12, 900, hurt); Rect(1588, 0, 12, 900, hurt);
                Rect(0, 0, 1600, 900, new Color(0.8f, 0.07f, 0.02f, player.DamageFlash * 0.12f));
            }
        }
        void Crosshair()
        {
            var rifle = session.Player.Weapon;
            float spread = (PlayerMotor.IsAiming ? 4 : 9) + rifle.Kick * 8 + rifle.Bloom * 15 + session.Player.Movement * 3;
            Color color = rifle.HitMarker > 0 ? (rifle.LastHitWasHeadshot ? ArenaWorld.Orange : Color.white) : ArenaWorld.Cyan;
            Rect(799, 449, 2, 2, color);
            Rect(800 - spread - 7, 449, 7, 2, color); Rect(800 + spread, 449, 7, 2, color);
            Rect(799, 450 - spread - 7, 2, 7, color); Rect(799, 450 + spread, 2, 7, color);
            if (rifle.HitMarker > 0)
            {
                Line(new Vector2(783, 433), new Vector2(790, 440), color, 2); Line(new Vector2(817, 433), new Vector2(810, 440), color, 2);
                Line(new Vector2(783, 467), new Vector2(790, 460), color, 2); Line(new Vector2(817, 467), new Vector2(810, 460), color, 2);
                if (rifle.LastHitWasHeadshot) Text(675, 484, 250, 21, "CRITICAL", 12, ArenaWorld.Orange, FontStyle.Bold, TextAnchor.UpperCenter);
            }
        }
        void EnemyBars()
        {
            Camera camera = session.Player.View;
            foreach (var enemy in session.Enemies)
            {
                Vector3 world = enemy.transform.position + Vector3.up * 2.25f;
                Vector3 screen = camera.WorldToViewportPoint(world);
                if (screen.z < 0 || screen.x < 0 || screen.x > 1 || screen.y < 0 || screen.y > 1) continue;
                if (Physics.Linecast(camera.transform.position, world, 1 << ArenaWorld.WorldLayer, QueryTriggerInteraction.Ignore)) continue;
                float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
                float x = (screen.x * Screen.width - (Screen.width - 1600 * scale) / 2) / scale;
                float y = ((1 - screen.y) * Screen.height - (Screen.height - 900 * scale) / 2) / scale;
                Rect(x - 25, y, 50, 3, new Color(0.1f, 0.15f, 0.18f));
                Rect(x - 25, y, 50 * Mathf.Clamp01(enemy.Health / enemy.MaxHealth), 3, enemy.Charging ? Color.white : ArenaWorld.Orange);
                if (enemy.Heavy) Text(x - 60, y - 18, 120, 17, "BULWARK", 9, ArenaWorld.Orange, FontStyle.Normal, TextAnchor.UpperCenter);
            }
        }
        void Radar()
        {
            const float x = 1398, y = 118, size = 168;
            Rect(x, y, size, size, ink);
            for (int i = 1; i <= 3; i++) { Rect(x + i * 42, y, 1, size, new Color(0.10f, 0.19f, 0.23f)); Rect(x, y + i * 42, size, 1, new Color(0.10f, 0.19f, 0.23f)); }
            foreach (var enemy in session.Enemies)
            {
                Vector2 p = Map(enemy.transform.position); Rect(x + p.x - 2, y + p.y - 2, 4, 4, ArenaWorld.Orange);
            }
            Vector2 position = Map(session.Player.transform.position);
            Rect(x + position.x - 3, y + position.y - 3, 6, 6, ArenaWorld.Cyan);
            Vector3 forward = session.Player.transform.forward;
            Line(new Vector2(x + position.x, y + position.y), new Vector2(x + position.x + forward.x * 12, y + position.y - forward.z * 12), ArenaWorld.Cyan, 2);
            Text(x, y + size + 8, size, 18, "TACTICAL SCAN  /  N ↑", 10, muted, FontStyle.Normal, TextAnchor.UpperRight);
        }
        static Vector2 Map(Vector3 position) { return new Vector2(Mathf.Clamp01((position.x + 27) / 54) * 168, Mathf.Clamp01((27 - position.z) / 54) * 168); }
        void Pause()
        {
            Rect(490, 160, 620, 600, ink); Rect(490, 160, 620, 3, ArenaWorld.Cyan);
            Text(540, 250, 520, 27, "OPERATOR STANDBY", 13, ArenaWorld.Cyan);
            Text(537, 294, 530, 70, "PAUSED", 58, white, FontStyle.Bold);
            if (Button(540, 384, 520, 57, "RESUME OPERATION                         >", ArenaWorld.Cyan)) session.SetPaused(false);
            Text(540, 468, 520, 25, "MOUSE SENSITIVITY   /   " + session.Player.Sensitivity.ToString("F1"), 13, muted);
            session.Player.Sensitivity = GUI.HorizontalSlider(new Rect(540, 508, 520, 24), session.Player.Sensitivity, 0.3f, 4f);
            Text(540, 545, 520, 25, "FIELD OF VIEW   /   " + session.Player.BaseFieldOfView.ToString("F0"), 13, muted);
            session.Player.BaseFieldOfView = GUI.HorizontalSlider(new Rect(540, 580, 520, 24), session.Player.BaseFieldOfView, 70, 100);
            if (Button(540, 620, 250, 49, session.Audio.Muted ? "SOUND: OFF" : "SOUND: ON", new Color(0.28f, 0.43f, 0.49f))) session.Audio.ToggleMute();
            if (Button(810, 620, 250, 49, "RESTART", new Color(0.28f, 0.43f, 0.49f))) session.Begin();
            if (Button(540, 693, 520, 45, "RETURN TO TITLE", new Color(0.18f, 0.28f, 0.34f))) session.ReturnToTitle();
        }
        void Result()
        {
            bool won = session.State == MatchState.Victory;
            Color accent = won ? ArenaWorld.Cyan : ArenaWorld.Orange;
            Rect(410, 181, 780, 540, ink); Rect(410, 181, 780, 4, accent);
            Text(470, 220, 660, 24, won ? "CONTAINMENT RESTORED  /  MISSION COMPLETE" : "SIGNAL LOST  /  OPERATOR DOWN", 14, accent);
            Text(466, 266, 675, 90, won ? "SECTOR SECURED" : "SYSTEM OFFLINE", 52, white, FontStyle.Bold);
            Text(470, 371, 600, 23, "FINAL SCORE", 13, muted);
            Text(468, 398, 400, 73, session.Score.ToString("D6"), 57, accent, FontStyle.Bold);
            Text(845, 399, 280, 65, "DIFFICULTY  " + session.DifficultyName + "\nWAVE    " + session.Wave + " / 5\nKILLS     " + session.Kills + "     TIME  " + FormatTime(session.Elapsed), 18, white);
            Text(470, 475, 660, 24, "ACCURACY  " + session.Accuracy.ToString("F0") + "%    /    BEST STREAK  " + session.BestKillStreak + "    /    SHOTS  " + session.ShotsFired + "    /    HEADSHOT KILLS  " + session.Headshots, 14, white);
            Text(470, 515, 660, 26, "PERSONAL BEST   /   " + session.BestScore.ToString("N0"), 16, muted);
            if (Button(470, 557, 660, 59, "DEPLOY AGAIN                                             >", accent)) session.Begin();
            if (Button(470, 634, 660, 45, "RETURN TO TITLE", new Color(0.23f, 0.35f, 0.41f))) session.ReturnToTitle();
        }
        static string FormatTime(float time) { return Mathf.FloorToInt(time / 60).ToString("D2") + ":" + Mathf.FloorToInt(time % 60).ToString("D2"); }
        void Text(float x, float y, float w, float h, string text, int size, Color color, FontStyle font = FontStyle.Normal, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            label.fontSize = size; label.fontStyle = font; label.alignment = anchor; label.normal.textColor = color;
            GUI.Label(new Rect(x, y, w, h), text, label);
        }
        bool Button(float x, float y, float w, float h, string text, Color color)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = new Rect(x, y, w, h).Contains(Event.current.mousePosition) ? Color.Lerp(color, Color.white, 0.2f) : color;
            bool clicked = GUI.Button(new Rect(x, y, w, h), text, button); GUI.backgroundColor = previous; return clicked;
        }
        void Rect(float x, float y, float w, float h, Color color)
        {
            GUI.color = color; GUI.DrawTexture(new Rect(x, y, w, h), pixel); GUI.color = Color.white;
        }
        void Line(Vector2 a, Vector2 b, Color color, float width)
        {
            Matrix4x4 old = GUI.matrix;
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, a); Rect(a.x, a.y - width / 2, Vector2.Distance(a, b), width, color); GUI.matrix = old;
        }
        void OnDestroy() { if (pixel != null) Destroy(pixel); }
    }
}
