using System;

namespace NeonBreach.Core
{
    // Pure C#: tested without an installed Unity Editor.
    public sealed class AmmoState
    {
        public int Capacity { get; private set; }
        public int Magazine { get; private set; }
        public int Reserve { get; private set; }
        public bool Reloading { get; private set; }
        public float RemainingReload { get; private set; }

        public AmmoState(int capacity, int reserve)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            Magazine = capacity;
            Reserve = Math.Max(0, reserve);
        }

        public bool TryShoot()
        {
            if (Reloading || Magazine <= 0) return false;
            Magazine--;
            return true;
        }

        public bool StartReload(float duration)
        {
            if (Reloading || Magazine == Capacity || Reserve == 0) return false;
            Reloading = true;
            RemainingReload = Math.Max(0.01f, duration);
            return true;
        }

        public bool Tick(float deltaTime)
        {
            if (!Reloading) return false;
            RemainingReload -= Math.Max(0, deltaTime);
            if (RemainingReload > 0) return false;
            int transfer = Math.Min(Capacity - Magazine, Reserve);
            Magazine += transfer;
            Reserve -= transfer;
            Reloading = false;
            RemainingReload = 0;
            return true;
        }

        public void Supply(int amount) { Reserve = Math.Min(300, Reserve + Math.Max(0, amount)); }
    }

    public static class WaveRules
    {
        public static int EnemyCount(int wave) { return Math.Min(6 + Math.Max(0, wave - 1) * 2, 24); }
        public static float Health(int wave, bool heavy) { return (heavy ? 108 : 66) + Math.Max(0, wave - 1) * 7; }
        public static float Damage(int wave, bool heavy) { return (heavy ? 12 : 7) + Math.Min(8, Math.Max(0, wave - 1)); }
        public static float SpawnDelay(int wave) { return Math.Max(0.45f, 1.3f - wave * 0.06f); }
        public static int Score(bool heavy, bool headshot) { return (heavy ? 180 : 100) + (headshot ? 50 : 0); }
    }
}
