using System.Collections.Generic;
using NeonBreach.Core;
using UnityEngine;

namespace NeonBreach
{
    public enum MatchState { Title, Playing, Paused, Defeat, Victory }
    public enum Difficulty { Training, Standard, Extreme }
    public sealed class GameSession : MonoBehaviour
    {
        public const int FinalWave = 5;
        public ArenaWorld World { get; private set; }
        public Transform Actors { get; private set; }
        public SynthAudio Audio { get; private set; }
        public CombatEffects Effects { get; private set; }
        public PlayerMotor Player { get; private set; }
        public MatchState State { get; private set; } = MatchState.Title;
        public Difficulty Difficulty { get; private set; } = Difficulty.Standard;
        public string DifficultyName => Difficulty == Difficulty.Training ? "TRAINING" : Difficulty == Difficulty.Extreme ? "EXTREME" : "STANDARD";
        public float EnemyHealthMultiplier => Difficulty == Difficulty.Training ? 0.78f : Difficulty == Difficulty.Extreme ? 1.28f : 1f;
        public float EnemyDamageMultiplier => Difficulty == Difficulty.Training ? 0.72f : Difficulty == Difficulty.Extreme ? 1.32f : 1f;
        public readonly List<EnemyAgent> Enemies = new List<EnemyAgent>();
        public int Wave { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int ShotsFired { get; private set; }
        public int ShotsHit { get; private set; }
        public int Headshots { get; private set; }
        public float Accuracy => ShotsFired == 0 ? 0 : 100f * ShotsHit / ShotsFired;
        public string KillNotice { get; private set; }
        public float KillNoticeUntil { get; private set; }
        public void RecordShot(bool hit) { ShotsFired++; if (hit) ShotsHit++; }
        public int BestScore { get; private set; }
        public int Remaining => Enemies.Count + pendingSpawns;
        public float Elapsed { get; private set; }
        public float Intermission { get; private set; }
        public float GrenadeCooldown { get; private set; }
        public int Grenades { get; private set; } = 3;
        public int KillStreak { get; private set; }
        public int BestKillStreak { get; private set; }
        public string Notice { get; private set; }
        public float NoticeUntil { get; private set; }
        int pendingSpawns;
        float spawnClock;
        bool waiting;

        public void Initialize(ArenaWorld world, Transform actors, SynthAudio audio, CombatEffects effects)
        {
            World = world; Actors = actors; Audio = audio; Effects = effects;
            BestScore = PlayerPrefs.GetInt("NeonBreach.BestScore", 0);
            var go = new GameObject("Operator"); go.transform.SetParent(transform);
            Player = go.AddComponent<PlayerMotor>(); Player.Initialize(this);
            LockCursor(false);
        }
        public void SetDifficulty(Difficulty difficulty) { if (State == MatchState.Title) Difficulty = difficulty; }
        public void Begin()
        {
            Time.timeScale = 1;
            foreach (Transform child in Actors) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            Enemies.Clear(); Effects.Clear(); Grenades = 3; GrenadeCooldown = 0; KillStreak = BestKillStreak = 0; ShotsFired = ShotsHit = Headshots = 0; KillNoticeUntil = 0; Score = 0; Kills = 0; Wave = 0; Elapsed = 0; pendingSpawns = 0;
            Player.ResetOperator(); State = MatchState.Playing; LockCursor(true);
            Pickup.Spawn(this, new Vector3(-22, 0.7f, 0), false);
            Pickup.Spawn(this, new Vector3(22, 0.7f, 0), true);
            Pickup.Spawn(this, new Vector3(0, 0.7f, 17), false);
            waiting = true; Intermission = 2.5f;
            Announce("SYSTEM ONLINE  /  PREPARE FOR CONTACT", 2.4f);
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) Audio.ToggleMute();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == MatchState.Playing) SetPaused(true);
                else if (State == MatchState.Paused) SetPaused(false);
            }
            if (State != MatchState.Playing) return;
            Elapsed += Time.deltaTime;
            GrenadeCooldown = Mathf.Max(0, GrenadeCooldown - Time.deltaTime);
            if (waiting)
            {
                Intermission -= Time.deltaTime;
                if (Intermission <= 0) BeginWave();
                return;
            }
            if (pendingSpawns > 0)
            {
                spawnClock -= Time.deltaTime;
                if (spawnClock <= 0) { SpawnEnemy(); pendingSpawns--; spawnClock = WaveRules.SpawnDelay(Wave); }
            }
            if (pendingSpawns == 0 && Enemies.Count == 0)
            {
                if (Wave >= FinalWave) { Finish(true); return; }
                waiting = true; Intermission = 6;
                Player.Heal(25); Player.Weapon.Ammo.Supply(75); Grenades = Mathf.Min(5, Grenades + 1);
                Announce("SECTOR CLEAR  /  +25 HEALTH  +75 AMMO", 4);
                Audio.Play(SoundCue.Pickup);
            }
        }
        void BeginWave()
        {
            waiting = false; Intermission = 0; Wave++; pendingSpawns = WaveRules.EnemyCount(Wave) + (Difficulty == Difficulty.Extreme ? 2 : Difficulty == Difficulty.Training ? -1 : 0); spawnClock = 0.3f;
            Announce("WAVE " + Wave.ToString("00") + "  /  HOSTILES INBOUND", 3);
            Audio.Play(SoundCue.Wave);
        }
        void SpawnEnemy()
        {
            Vector3 spawn = World.SpawnPoints[Random.Range(0, World.SpawnPoints.Count)];
            float best = -1;
            // Prefer a pad away from the operator and other actors; no spawning in the player's face.
            foreach (Vector3 point in World.SpawnPoints)
            {
                float distance = Vector3.Distance(point, Player.transform.position);
                foreach (EnemyAgent enemy in Enemies) if (Vector3.Distance(point, enemy.transform.position) < 2) distance -= 30;
                distance += Random.Range(0, 7);
                if (distance > best) { best = distance; spawn = point; }
            }
            bool boss = Wave == FinalWave && Enemies.Count == 0 && pendingSpawns == WaveRules.EnemyCount(Wave);
            bool heavy = boss || Wave >= 2 && Random.value < 0.15f + Wave * 0.04f;
            bool ranged = !heavy && Wave >= 2 && Random.value < 0.18f;
            var go = new GameObject(boss ? "OVERSEER // Command Unit" : ranged ? "LANCER // Long-range" : heavy ? "BULWARK // Heavy" : "SENTRY // Rifleman"); go.transform.SetParent(Actors); go.transform.position = spawn;
            var agent = go.AddComponent<EnemyAgent>(); agent.Initialize(this, heavy, ranged, boss); Enemies.Add(agent);
            Effects.Burst(spawn + Vector3.up, true, 14);
        }
        public bool ThrowGrenade(Vector3 origin, Vector3 direction)
        {
            if (State != MatchState.Playing || Grenades <= 0 || GrenadeCooldown > 0) return false;
            Grenades--; GrenadeCooldown = 8f;
            Vector3 center = origin + direction.normalized * 12f;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, 18f, ~(1 << PlayerMotor.PlayerLayer), QueryTriggerInteraction.Ignore)) center = hit.point;
            Effects.Burst(center, false, 28);
            foreach (EnemyAgent enemy in new List<EnemyAgent>(Enemies))
            {
                float distance = Vector3.Distance(center, enemy.transform.position);
                if (distance > 7.5f) continue;
                float damage = Mathf.Lerp(95f, 20f, distance / 7.5f);
                enemy.TakeDamage(damage, false);
                Vector3 push = (enemy.transform.position - center); push.y = 0;
                if (push.sqrMagnitude > 0.01f) enemy.Knockback(push.normalized * Mathf.Lerp(5f, 1f, distance / 7.5f));
            }
            Audio.Play(SoundCue.EnemyShot, 0.9f);
            Announce("FRAG GRENADE  /  BLAST ZONE CLEAR", 1.5f);
            return true;
        }
        
        public void RegisterKill(EnemyAgent enemy, bool headshot)
        {
            if (!Enemies.Remove(enemy)) return;
            Kills++; KillStreak++; BestKillStreak = Mathf.Max(BestKillStreak, KillStreak); if (headshot) Headshots++;
            int points = WaveRules.Score(enemy.Heavy, headshot) + (enemy.Boss ? 750 : 0);
            int streakBonus = Mathf.Min(300, Mathf.Max(0, KillStreak - 1) * 15);
            Score += points + streakBonus;
            KillNotice = (headshot ? "HEADSHOT" : "ELIMINATED") + "  /  " + (enemy.Heavy ? "BULWARK" : "SENTRY") + "  +" + (points + streakBonus) + (streakBonus > 0 ? "  /  STREAK x" + KillStreak : "");
            KillNoticeUntil = Time.time + 1.8f;
            Audio.Play(SoundCue.Kill, 0.65f);
            if (KillStreak >= 5 || Player.Weapon.Ammo.Reserve < 45 || Random.value < 0.30f)
                Pickup.Spawn(this, enemy.transform.position + Vector3.up * 0.7f, Player.Health < 60 && Random.value < 0.6f);
        }
        public void Announce(string message, float duration) { Notice = message; NoticeUntil = Time.time + duration; }
        public void SetPaused(bool pause)
        {
            if (State != MatchState.Playing && State != MatchState.Paused) return;
            if (!pause) Player.SaveSettings();
            State = pause ? MatchState.Paused : MatchState.Playing; Time.timeScale = pause ? 0 : 1; LockCursor(!pause);
        }
        public void Finish(bool victory)
        {
            if (State != MatchState.Playing) return;
            if (victory) Score += 1000 + Mathf.CeilToInt(Player.Health) * 10;
            State = victory ? MatchState.Victory : MatchState.Defeat;
            BestScore = Mathf.Max(BestScore, Score); PlayerPrefs.SetInt("NeonBreach.BestScore", BestScore); PlayerPrefs.Save();
            Time.timeScale = 0; LockCursor(false);
        }
        public void ReturnToTitle()
        {
            Time.timeScale = 1;
            foreach (Transform child in Actors) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            Enemies.Clear(); Effects.Clear(); Player.SaveSettings(); State = MatchState.Title; Player.ShowOverview(); LockCursor(false);
        }
        void OnApplicationFocus(bool focus) { if (!focus && State == MatchState.Playing) SetPaused(true); }
        static void LockCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
        public void Quit()
        {
            Player.SaveSettings();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
