using UnityEngine;

namespace NeonBreach
{
    // Fixed-capacity pools: combat never instantiates a tracer or fragment.
    public sealed class CombatEffects : MonoBehaviour
    {
        public const int MoteCapacity = 192, BeamCapacity = 32;
        sealed class Mote
        {
            public Transform Transform;
            public Renderer Renderer;
            public Vector3 Velocity;
            public float Remaining, Duration, Scale;
        }
        sealed class Tracer { public LineRenderer Line; public float Remaining; }
        readonly Mote[] motes = new Mote[MoteCapacity];
        readonly Tracer[] beams = new Tracer[BeamCapacity];
        ArenaWorld world;
        int nextMote, nextBeam;
        public int ActiveMotes { get; private set; }
        public int ActiveBeams { get; private set; }
        public void Initialize(ArenaWorld arena)
        {
            world = arena;
            for (int i = 0; i < motes.Length; i++)
            {
                var go = ArenaWorld.Shape(PrimitiveType.Cube, "Pooled fragment", transform, Vector3.zero, Vector3.one, world.Teal);
                var renderer = go.GetComponent<Renderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                motes[i] = new Mote { Transform = go.transform, Renderer = renderer };
                go.SetActive(false);
            }
            for (int i = 0; i < beams.Length; i++)
            {
                var go = new GameObject("Pooled tracer"); go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>(); line.positionCount = 2;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false; line.useWorldSpace = true;
                beams[i] = new Tracer { Line = line }; go.SetActive(false);
            }
        }
        public void Beam(Vector3 start, Vector3 end, bool hostile = false)
        {
            var beam = beams[nextBeam]; nextBeam = (nextBeam + 1) % beams.Length;
            if (beam.Remaining <= 0) ActiveBeams++;
            beam.Remaining = 0.065f;
            var line = beam.Line; line.sharedMaterial = hostile ? world.Amber : world.Teal;
            line.SetPosition(0, start); line.SetPosition(1, end);
            line.startWidth = hostile ? 0.045f : 0.035f; line.endWidth = 0.009f;
            line.gameObject.SetActive(true);
        }
        void Emit(Vector3 at, Vector3 velocity, float scale, float lifetime, Material material)
        {
            var mote = motes[nextMote]; nextMote = (nextMote + 1) % motes.Length;
            if (mote.Remaining <= 0) ActiveMotes++;
            mote.Velocity = velocity; mote.Duration = mote.Remaining = lifetime; mote.Scale = scale;
            mote.Transform.position = at; mote.Transform.rotation = Random.rotation;
            mote.Transform.localScale = Vector3.one * scale; mote.Renderer.sharedMaterial = material;
            mote.Transform.gameObject.SetActive(true);
        }
        public void Burst(Vector3 at, bool enemy, int count = 8)
        {
            for (int i = 0; i < Mathf.Min(count, MoteCapacity); i++)
                Emit(at, Random.onUnitSphere * Random.Range(2f, 6f), Random.Range(0.035f, 0.1f), 0.3f, enemy ? world.Amber : world.Teal);
        }
        public void Debris(Vector3 at)
        {
            for (int i = 0; i < 9; i++)
                Emit(at + Random.insideUnitSphere * 0.4f, Random.onUnitSphere * 4 + Vector3.up * 4,
                    Random.Range(0.12f, 0.3f), 0.9f, i % 3 == 0 ? world.Amber : world.Metal);
        }
        void Update()
        {
            float dt = Time.deltaTime; if (dt <= 0) return;
            foreach (var beam in beams)
            {
                if (beam.Remaining <= 0) continue;
                beam.Remaining -= dt;
                if (beam.Remaining <= 0) { beam.Line.gameObject.SetActive(false); ActiveBeams--; }
            }
            foreach (var mote in motes)
            {
                if (mote.Remaining <= 0) continue;
                mote.Remaining -= dt;
                if (mote.Remaining <= 0) { mote.Transform.gameObject.SetActive(false); ActiveMotes--; continue; }
                mote.Velocity += Vector3.down * 12 * dt;
                mote.Transform.position += mote.Velocity * dt; mote.Transform.Rotate(110 * dt, 80 * dt, 0);
                mote.Transform.localScale = Vector3.one * mote.Scale * Mathf.Clamp01(mote.Remaining / (mote.Duration * 0.35f));
            }
        }
        public void Clear()
        {
            foreach (var mote in motes) { mote.Remaining = 0; mote.Transform.gameObject.SetActive(false); }
            foreach (var beam in beams) { beam.Remaining = 0; beam.Line.gameObject.SetActive(false); }
            ActiveMotes = ActiveBeams = nextMote = nextBeam = 0;
        }
    }
}
