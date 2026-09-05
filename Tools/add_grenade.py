from pathlib import Path
root=Path(r'D:\github\NeonBreach\Assets\NeonBreach\Scripts')
p=root/'Gameplay/GameSession.cs'; s=p.read_text(encoding='utf-8-sig')
s=s.replace('public float Intermission { get; private set; }','public float Intermission { get; private set; }\n        public float GrenadeCooldown { get; private set; }\n        public int Grenades { get; private set; } = 3;')
s=s.replace('Elapsed += Time.deltaTime;', 'Elapsed += Time.deltaTime;\n            GrenadeCooldown = Mathf.Max(0, GrenadeCooldown - Time.deltaTime);')
s=s.replace('Enemies.Clear(); Effects.Clear(); ShotsFired', 'Enemies.Clear(); Effects.Clear(); Grenades = 3; GrenadeCooldown = 0; ShotsFired')
needle='public void RegisterKill(EnemyAgent enemy, bool headshot)'
method='''public bool ThrowGrenade(Vector3 origin, Vector3 direction)
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
        
        '''+needle
s=s.replace(needle,method)
p.write_text(s,encoding='utf-8')
p=root/'Gameplay/EnemyAgent.cs'; s=p.read_text(encoding='utf-8-sig')
s=s.replace('public bool Charging => windup > 0;', 'public bool Charging => windup > 0;\n        public void Knockback(Vector3 velocity) { if (controller != null && controller.enabled) controller.Move(velocity * Time.deltaTime); }')
p.write_text(s,encoding='utf-8')
p=root/'Gameplay/PlayerMotor.cs'; s=p.read_text(encoding='utf-8-sig')
s=s.replace('if (Input.GetKeyDown(KeyCode.R))', 'if (Input.GetKeyDown(KeyCode.G)) session.ThrowGrenade(View.transform.position + View.transform.forward * 0.5f, View.transform.forward);\n            if (Input.GetKeyDown(KeyCode.R))')
p.write_text(s,encoding='utf-8')
p=root/'Presentation/ArenaHud.cs'; s=p.read_text(encoding='utf-8-sig')
s=s.replace('Text(540, 851, 520, 22, "R  RELOAD', 'Text(540, 851, 520, 22, "G  GRENADE (' + '" + session.Grenades + "' + ')    R  RELOAD')
s=s.replace('ALT  AIM      R  RELOAD      SHIFT  SPRINT      C  CROUCH      SPACE  JUMP','ALT  AIM      G  GRENADE      R  RELOAD      SHIFT  SPRINT      C  CROUCH      SPACE  JUMP')
p.write_text(s,encoding='utf-8')
p=Path(r'D:\github\NeonBreach\README.md'); s=p.read_text(encoding='utf-8-sig')
s=s.replace('| 左 Shift（按住） | 向前冲刺，消耗耐力 |','| 左 Shift（按住） | 向前冲刺，消耗耐力 |\n| G | 投掷手雷（3 枚，8 秒冷却） |')
s=s.replace('左 Shift 冲刺 | C 蹲伏 | Space 跳跃', '左 Shift 冲刺 | C 蹲伏 | G 手雷 | Space 跳跃')
p.write_text(s,encoding='utf-8')
