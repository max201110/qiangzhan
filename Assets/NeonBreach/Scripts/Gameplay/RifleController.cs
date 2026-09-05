using NeonBreach.Core;
using UnityEngine;

namespace NeonBreach
{
    public sealed class RifleController : MonoBehaviour
    {
        public AmmoState Ammo { get; private set; }
        public float HitMarker { get; private set; }
        public bool LastHitWasHeadshot { get; private set; }
        public float Kick { get; private set; }
        public float Bloom { get; private set; }
        public bool Automatic { get; private set; } = true;
        public const float ReloadDuration = 1.65f;
        GameSession session;
        PlayerMotor player;
        Transform model, muzzle;
        GameObject flash;
        float cooldown, flashUntil;

        public void Initialize(GameSession game, PlayerMotor owner)
        {
            session = game; player = owner;
            model = new GameObject("VX-07 // Ion assault rifle").transform; model.SetParent(transform, false);
            var world = game.World;
            Part("Receiver", new Vector3(0, 0, 0.1f), new Vector3(0.15f, 0.15f, 0.42f), world.Metal);
            Part("Upper rail", new Vector3(0, 0.095f, 0.13f), new Vector3(0.12f, 0.04f, 0.49f), world.Dark);
            Part("Foregrip", new Vector3(0, -0.02f, 0.42f), new Vector3(0.11f, 0.12f, 0.22f), world.Dark);
            Part("Barrel", new Vector3(0, 0.015f, 0.64f), new Vector3(0.05f, 0.055f, 0.3f), world.Metal);
            Part("Muzzle brake", new Vector3(0, 0.015f, 0.82f), new Vector3(0.08f, 0.08f, 0.1f), world.Dark);
            Part("Magazine", new Vector3(0, -0.18f, 0.08f), new Vector3(0.09f, 0.24f, 0.13f), world.Dark).localRotation = Quaternion.Euler(-12, 0, 0);
            Part("Grip", new Vector3(0, -0.14f, -0.12f), new Vector3(0.08f, 0.19f, 0.1f), world.Dark).localRotation = Quaternion.Euler(-16, 0, 0);
            Part("Stock", new Vector3(0, -0.025f, -0.26f), new Vector3(0.11f, 0.15f, 0.25f), world.Metal);
            Part("Energy strip", new Vector3(0.077f, 0.025f, 0.14f), new Vector3(0.009f, 0.027f, 0.28f), world.Teal);
            Part("Sight left", new Vector3(-0.044f, 0.15f, 0.04f), new Vector3(0.018f, 0.1f, 0.025f), world.Dark);
            Part("Sight right", new Vector3(0.044f, 0.15f, 0.04f), new Vector3(0.018f, 0.1f, 0.025f), world.Dark);
            Part("Sight bridge", new Vector3(0, 0.2f, 0.04f), new Vector3(0.105f, 0.017f, 0.025f), world.Dark);
            Part("Front sight", new Vector3(0, 0.15f, 0.61f), new Vector3(0.015f, 0.075f, 0.022f), world.Teal);
            Part("Gloved support", new Vector3(-0.015f, -0.11f, 0.35f), new Vector3(0.16f, 0.10f, 0.15f), world.Concrete);
            Part("Left sleeve", new Vector3(-0.16f, -0.2f, 0.21f), new Vector3(0.15f, 0.17f, 0.39f), world.Dark).localRotation = Quaternion.Euler(-10, 35, 0);
            Part("Right sleeve", new Vector3(0.08f, -0.25f, -0.13f), new Vector3(0.15f, 0.17f, 0.35f), world.Dark);
            muzzle = new GameObject("Muzzle origin").transform; muzzle.SetParent(model, false); muzzle.localPosition = new Vector3(0, 0.015f, 0.9f);
            flash = ArenaWorld.Shape(PrimitiveType.Sphere, "Muzzle flash", muzzle, Vector3.zero, new Vector3(0.13f, 0.13f, 0.25f), world.White, false, PlayerMotor.PlayerLayer);
            flash.SetActive(false); ResetRifle();
        }
        Transform Part(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = ArenaWorld.Shape(PrimitiveType.Cube, name, model, position, scale, material, false, PlayerMotor.PlayerLayer);
            var renderer = go.GetComponent<Renderer>(); renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            return go.transform;
        }
        public void SetVisible(bool visible) { model.gameObject.SetActive(visible); }
        public void ResetRifle()
        {
            Ammo = new AmmoState(30, 150); cooldown = 0.15f; flashUntil = 0; Kick = 0; Bloom = 0; HitMarker = 0; flash.SetActive(false);
            model.localPosition = new Vector3(0.24f, -0.24f, 0.38f); model.localRotation = Quaternion.identity;
        }
        void Update()
        {
            if (session.State != MatchState.Playing) return;
            Bloom = Mathf.MoveTowards(Bloom, 0, Time.deltaTime * 1.4f);
            cooldown -= Time.deltaTime; HitMarker = Mathf.MoveTowards(HitMarker, 0, Time.deltaTime * 5);
            Kick = Mathf.MoveTowards(Kick, 0, Time.deltaTime * 9);
            if (Ammo.Tick(Time.deltaTime)) session.Audio.Play(SoundCue.ReloadEnd);
            if (Input.GetKeyDown(KeyCode.V)) { Automatic = !Automatic; session.Announce(Automatic ? "FIRE MODE  /  FULL AUTO" : "FIRE MODE  /  SEMI AUTO", 1.2f); session.Audio.Play(SoundCue.Wave, 0.25f); }
            if (Input.GetKeyDown(KeyCode.R)) Reload();
            if ((Automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0)) && cooldown <= 0 && !Ammo.Reloading)
            {
                if (Ammo.TryShoot()) Fire();
                else { cooldown = 0.25f; session.Audio.Play(SoundCue.Empty, 0.5f); Reload(); }
            }
            flash.SetActive(Time.time < flashUntil);
        }
        void LateUpdate()
        {
            if (session.State != MatchState.Playing) return;
            bool aimed = PlayerMotor.IsAiming;
            Vector3 target = aimed ? new Vector3(0, -0.167f, 0.38f) : new Vector3(0.24f, -0.24f, 0.38f);
            float reload = Ammo.Reloading ? Mathf.Sin((1 - Ammo.RemainingReload / ReloadDuration) * Mathf.PI) : 0;
            target += new Vector3(0, -0.20f * reload, -Kick * 0.07f);
            if (!aimed) target += new Vector3(Mathf.Sin(Time.time * 7) * 0.005f, Mathf.Cos(Time.time * 14) * 0.005f, 0) * player.Movement;
            model.localPosition = Vector3.Lerp(model.localPosition, target, Time.deltaTime * 18);
            model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.Euler(-Kick * 6 + reload * 24, reload * -18, reload * -30), Time.deltaTime * 18);
        }
        void Reload() { if (Ammo.StartReload(ReloadDuration)) session.Audio.Play(SoundCue.Reload); }
        void Fire()
        {
            cooldown = 0.095f; Kick = 1; flashUntil = Time.time + 0.045f;
            session.Audio.Play(SoundCue.Shot, 0.8f);
            bool aimed = PlayerMotor.IsAiming;
            Vector2 spread = Random.insideUnitCircle * (aimed ? 0.0018f + Bloom * 0.003f : player.Sprinting ? 0.026f : 0.006f + player.Movement * 0.004f + Bloom * 0.012f);
            Bloom = Mathf.Clamp01(Bloom + 0.22f);
            bool hitEnemy = false;
            Vector3 direction = (transform.forward + transform.right * spread.x + transform.up * spread.y).normalized;
            Vector3 endpoint = transform.position + direction * 120;
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, 120, ~(1 << PlayerMotor.PlayerLayer), QueryTriggerInteraction.Ignore))
            {
                endpoint = hit.point;
                var box = hit.collider.GetComponent<EnemyHitbox>();
                var enemy = hit.collider.GetComponentInParent<EnemyAgent>();
                if (enemy != null)
                {
                    hitEnemy = true;
                    bool headshot = box != null && box.Head;
                    enemy.TakeDamage(headshot ? 72 : 27, headshot);
                    HitMarker = 1; LastHitWasHeadshot = headshot; session.Audio.Play(SoundCue.Hit, 0.45f);
                }
                session.Effects.Burst(hit.point + hit.normal * 0.03f, enemy != null, enemy != null ? 8 : 4);
            }
            session.RecordShot(hitEnemy);
            session.Effects.Beam(muzzle.position, endpoint); player.Recoil(aimed ? 0.34f : 0.62f);
        }
    }
}
