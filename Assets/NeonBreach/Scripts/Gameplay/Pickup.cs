using UnityEngine;

namespace NeonBreach
{
    public sealed class Pickup : MonoBehaviour
    {
        GameSession session;
        bool health;
        Vector3 home;
        float age;
        public static void Spawn(GameSession session, Vector3 position, bool health)
        {
            var go = new GameObject(health ? "Medical cell +35" : "Ammo cell +60"); go.transform.SetParent(session.Actors); go.transform.position = position;
            var pickup = go.AddComponent<Pickup>(); pickup.session = session; pickup.health = health; pickup.home = position;
            var mat = health ? session.World.Teal : session.World.Amber;
            ArenaWorld.Shape(PrimitiveType.Cube, "Supply body", go.transform, Vector3.zero, new Vector3(0.48f, 0.48f, 0.48f), session.World.Dark);
            if (health)
            {
                ArenaWorld.Shape(PrimitiveType.Cube, "Cross horizontal", go.transform, new Vector3(0, 0, -0.25f), new Vector3(0.34f, 0.10f, 0.025f), mat);
                ArenaWorld.Shape(PrimitiveType.Cube, "Cross vertical", go.transform, new Vector3(0, 0, -0.26f), new Vector3(0.10f, 0.34f, 0.025f), mat);
            }
            else for (int i = -1; i <= 1; i++) ArenaWorld.Shape(PrimitiveType.Cube, "Cartridge indicator", go.transform, new Vector3(i * 0.11f, 0, -0.25f), new Vector3(0.06f, 0.29f, 0.025f), mat);
            ArenaWorld.Shape(PrimitiveType.Cube, "Supply glow", go.transform, new Vector3(0, -0.26f, 0), new Vector3(0.52f, 0.025f, 0.52f), mat);
        }
        void Update()
        {
            if (session.State != MatchState.Playing) return;
            age += Time.deltaTime; transform.position = home + Vector3.up * Mathf.Sin(age * 2.5f) * 0.12f; transform.Rotate(0, Time.deltaTime * 50, 0);
            Vector3 playerCenter = session.Player.transform.position + Vector3.up * 0.8f;
            if (Vector3.Distance(playerCenter, transform.position) > 1.35f) return;
            if (health && session.Player.Health >= 100) return;
            if (!health && session.Player.Weapon.Ammo.Reserve >= 300) return;
            // A close pickup on the other side of cover must not be collected through it.
            if (Physics.Linecast(playerCenter, transform.position, 1 << ArenaWorld.WorldLayer, QueryTriggerInteraction.Ignore)) return;
            if (health) session.Player.Heal(35); else session.Player.Weapon.Ammo.Supply(60);
            session.Audio.Play(SoundCue.Pickup); session.Announce(health ? "+35 HEALTH  /  MEDICAL CELL" : "+60 AMMO  /  SUPPLY CELL", 1.5f);
            Destroy(gameObject);
        }
    }
}
