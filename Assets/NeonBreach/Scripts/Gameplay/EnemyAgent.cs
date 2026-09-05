using System.Collections.Generic;
using NeonBreach.Core;
using UnityEngine;

namespace NeonBreach
{
    public sealed class EnemyHitbox : MonoBehaviour { public bool Head; }

    public sealed class EnemyAgent : MonoBehaviour
    {
        public const int EnemyLayer = 9;
        public bool Heavy { get; private set; }
        public bool Ranged { get; private set; }
        public bool Boss { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool Charging => windup > 0;
        public void Knockback(Vector3 velocity) { if (controller != null && controller.enabled) controller.Move(velocity * Time.deltaTime); }
        GameSession session;
        CharacterController controller;
        Transform torso, leftLeg, rightLeg, barrel;
        Renderer eye;
        List<Cell> path = new List<Cell>();
        int pathIndex;
        float repath, attackCooldown, windup, age, phase, verticalSpeed;
        Vector3 lockedTarget;
        bool dead;

        public void Initialize(GameSession game, bool heavy, bool ranged = false, bool boss = false)
        {
            session = game; Heavy = heavy; Ranged = ranged; Boss = boss; MaxHealth = Health = WaveRules.Health(game.Wave, heavy) * game.EnemyHealthMultiplier * (boss ? 3.2f : ranged ? 0.82f : 1f);
            gameObject.layer = EnemyLayer;
            controller = gameObject.AddComponent<CharacterController>(); controller.height = 1.7f; controller.radius = heavy ? 0.43f : 0.36f;
            controller.center = new Vector3(0, 0.85f, 0);
            if (Boss) { controller.height = 2.4f; controller.radius = 0.58f; transform.localScale = Vector3.one * 1.35f; } controller.stepOffset = 0.2f;
            phase = Random.Range(0, Mathf.PI * 2); attackCooldown = Random.Range(1.2f, 2.1f); repath = Random.Range(0, 0.4f);
            torso = new GameObject("Articulated chassis").transform; torso.SetParent(transform, false);
            float width = heavy ? 0.83f : 0.64f;
            Shape("Torso", torso, new Vector3(0, 1.17f, 0), new Vector3(width, 0.63f, 0.42f), game.World.Metal);
            Shape("Chest plate", torso, new Vector3(0, 1.21f, 0.23f), new Vector3(width * 0.77f, 0.36f, 0.09f), game.World.Dark);
            Shape("Core", torso, new Vector3(0, 1.21f, 0.29f), new Vector3(0.12f, 0.19f, 0.03f), game.World.Amber);
            var head = Shape("Head weak point", torso, new Vector3(0, 1.79f, 0), new Vector3(0.4f, 0.4f, 0.4f), game.World.Dark, true);
            head.gameObject.AddComponent<EnemyHitbox>().Head = true;
            eye = Shape("Threat visor", torso, new Vector3(0, 1.83f, 0.208f), new Vector3(0.33f, 0.075f, 0.025f), game.World.Red).GetComponent<Renderer>();
            leftLeg = Shape("Left leg", transform, new Vector3(-0.19f, 0.43f, 0), new Vector3(0.24f, 0.77f, 0.26f), game.World.Dark);
            rightLeg = Shape("Right leg", transform, new Vector3(0.19f, 0.43f, 0), new Vector3(0.24f, 0.77f, 0.26f), game.World.Dark);
            Shape("Left shoulder", torso, new Vector3(-width / 2 - 0.1f, 1.34f, 0), new Vector3(0.23f, 0.28f, 0.38f), boss ? game.World.Red : heavy ? game.World.Amber : ranged ? game.World.White : game.World.Metal);
            Shape("Right shoulder", torso, new Vector3(width / 2 + 0.1f, 1.34f, 0), new Vector3(0.23f, 0.28f, 0.38f), boss ? game.World.Red : heavy ? game.World.Amber : ranged ? game.World.White : game.World.Metal);
            Shape("Weapon", torso, new Vector3(0.43f, 1.03f, 0.4f), new Vector3(0.16f, 0.19f, 0.75f), ranged ? game.World.White : game.World.Dark);
            barrel = Shape("Charged muzzle", torso, new Vector3(0.43f, 1.03f, 0.79f), new Vector3(0.08f, 0.08f, 0.04f), game.World.Amber);
        }
        Transform Shape(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider = false)
        {
            return ArenaWorld.Shape(PrimitiveType.Cube, name, parent, position, scale, material, collider, EnemyLayer).transform;
        }
        void Update()
        {
            if (dead || session.State != MatchState.Playing) return;
            age += Time.deltaTime;
            Vector3 target = session.Player.transform.position;
            Vector3 toPlayer = target - transform.position; toPlayer.y = 0;
            if (toPlayer.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), Time.deltaTime * 7);
            Vector3 aimAt = session.Player.View.transform.position;
            Vector3 ray = aimAt - barrel.position;
            bool seesPlayer = Physics.Raycast(barrel.position, ray.normalized, out RaycastHit sight, ray.magnitude + 0.2f, ~(1 << EnemyLayer), QueryTriggerInteraction.Ignore)
                && sight.collider.GetComponent<PlayerMotor>() != null;
            float distance = toPlayer.magnitude;
            Vector3 movement = Vector3.zero;
            if (!seesPlayer || distance > (Heavy ? 13 : Ranged ? 23 : 16))
            {
                repath -= Time.deltaTime;
                if (repath <= 0)
                {
                    path = session.World.Navigation.FindPath(ArenaWorld.ToCell(transform.position), ArenaWorld.ToCell(target));
                    pathIndex = 0; repath = Random.Range(0.6f, 0.95f);
                }
                if (pathIndex < path.Count)
                {
                    Vector3 waypoint = ArenaWorld.ToWorld(path[pathIndex]); waypoint.y = transform.position.y;
                    Vector3 delta = waypoint - transform.position;
                    if (delta.magnitude < 0.28f) pathIndex++;
                    else movement = delta.normalized;
                }
            }
            else
            {
                movement = transform.right * Mathf.Sin(age * 0.7f + phase) * 0.55f;
                if (distance < (Boss ? 9 : Ranged ? 17 : 6)) movement -= transform.forward * (Ranged ? 0.9f : 0.7f);
                if (Ranged && distance > 25) movement += transform.forward * 0.6f;
            }
            foreach (var other in session.Enemies)
            {
                if (other == this) continue;
                Vector3 away = transform.position - other.transform.position; away.y = 0;
                if (away.sqrMagnitude > 0.001f && away.sqrMagnitude < 1.8f) movement += away.normalized * 0.6f;
            }
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed -= 18 * Time.deltaTime;
            controller.Move((Vector3.ClampMagnitude(movement, 1) * (Heavy ? 2.1f : 2.9f) + Vector3.up * verticalSpeed) * Time.deltaTime);
            float stride = Mathf.Sin(age * 10) * Mathf.Clamp01(movement.magnitude) * 23;
            leftLeg.localRotation = Quaternion.Euler(stride, 0, 0); rightLeg.localRotation = Quaternion.Euler(-stride, 0, 0);
            torso.localPosition = new Vector3(0, Mathf.Sin(age * 5) * 0.025f, 0);
            if (windup > 0)
            {
                windup -= Time.deltaTime;
                eye.sharedMaterial = session.World.White;
                if (windup <= 0) { Shoot(); attackCooldown = Boss ? 0.85f : Heavy ? 1.2f : Ranged ? 2.4f : 1.65f; eye.sharedMaterial = session.World.Red; }
            }
            else
            {
                attackCooldown -= Time.deltaTime;
                if (attackCooldown <= 0 && seesPlayer && distance < (Boss ? 38 : Ranged ? 48 : 27))
                {
                    // Snapshot aim BEFORE a visible windup so strafing can dodge the hitscan attack.
                    lockedTarget = aimAt + Random.insideUnitSphere * (Boss ? 0.20f : Heavy ? 0.28f : Ranged ? 0.16f : 0.43f);
                    windup = Boss ? 0.72f : Heavy ? 0.6f : Ranged ? 0.85f : 0.48f;
                }
            }
        }
        void Shoot()
        {
            Vector3 origin = barrel.position; Vector3 direction = (lockedTarget - origin).normalized;
            Vector3 endpoint = origin + direction * 60;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, 60, ~(1 << EnemyLayer), QueryTriggerInteraction.Ignore))
            {
                endpoint = hit.point;
                var player = hit.collider.GetComponent<PlayerMotor>();
                if (player != null) player.TakeDamage(WaveRules.Damage(session.Wave, Heavy) * session.EnemyDamageMultiplier * (Boss ? 1.5f : 1f), origin);
                session.Effects.Burst(hit.point, true, 3);
            }
            session.Effects.Beam(origin, endpoint, true);
            float volume = Mathf.Clamp01(1 - Vector3.Distance(origin, session.Player.transform.position) / 45) * 0.35f;
            session.Audio.Play(SoundCue.EnemyShot, volume);
        }
        public void TakeDamage(float damage, bool headshot)
        {
            if (dead) return;
            Health -= damage;
            if (Health > 0) return;
            dead = true; controller.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            session.Effects.Debris(transform.position + Vector3.up);
            session.RegisterKill(this, headshot); gameObject.SetActive(false); Destroy(gameObject);
        }
    }
}
