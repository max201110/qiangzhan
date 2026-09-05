using UnityEngine;

namespace NeonBreach
{
    [DisallowMultipleComponent]
    public sealed class ArenaBootstrap : MonoBehaviour
    {
        ArenaWorld world;
        void Awake()
        {
            Time.timeScale = 1;
            Application.targetFrameRate = 144;
            world = new ArenaWorld(); world.Build(transform);
            var dynamicRoot = new GameObject("Live actors and effects").transform; dynamicRoot.SetParent(transform);
            var audio = gameObject.AddComponent<SynthAudio>();
            var effectRoot = new GameObject("Persistent effect pool"); effectRoot.transform.SetParent(transform, false);
            var effects = effectRoot.AddComponent<CombatEffects>(); effects.Initialize(world);
            var session = gameObject.AddComponent<GameSession>();
            session.Initialize(world, dynamicRoot, audio, effects);
            gameObject.AddComponent<ArenaHud>().Initialize(session);
        }
        void OnDestroy() { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; world?.Dispose(); }
    }
}
