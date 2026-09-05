using System;
namespace Frontier {
    public enum Mode { Arena, Mech, Stealth }
    public enum Screen { Title, Playing, Paused, Debrief, Defeat, Complete }
    [Serializable] public class Mission {
        public string name, brief; public int seed, waves, enemies; public float timeLimit;
    }
    [Serializable] public class Definition {
        public string id, title, subtitle; public int mode; public Mission[] missions;
        public Mode Mode => (Mode)mode;
    }
    // Pure rules: deterministic and independent of frame rate or scene objects.
    public sealed class Magazine {
        public int Capacity {get;} public int Ammo {get; private set;} public int Reserve {get; private set;}
        public float ReloadLeft {get; private set;}
        public Magazine(int capacity, int reserve) { if(capacity<1)throw new ArgumentOutOfRangeException(); Capacity=capacity; Ammo=capacity; Reserve=Math.Max(0,reserve); }
        public bool Fire() { if(Ammo<1 || ReloadLeft>0)return false; Ammo--; return true; }
        public bool Reload() { if(Ammo==Capacity||Reserve<1||ReloadLeft>0)return false; ReloadLeft=1.6f; return true; }
        public void Tick(float dt) { if(ReloadLeft<=0)return; ReloadLeft=Math.Max(0,ReloadLeft-Math.Max(0,dt)); if(ReloadLeft==0){int n=Math.Min(Capacity-Ammo,Reserve); Ammo+=n; Reserve-=n;} }
        public void Supply(int n) { Reserve=Math.Min(360,Reserve+Math.Max(0,n)); }
    }
    public sealed class Heat {
        public float Value {get; private set;} public bool Locked {get; private set;}
        public bool Add(float amount) { if(Locked)return false; Value=Math.Min(100,Value+Math.Max(0,amount)); if(Value>=100)Locked=true; return true; }
        public void Tick(float dt, float cooling) { Value=Math.Max(0,Value-Math.Max(0,dt)*Math.Max(0,cooling)); if(Value<=35)Locked=false; }
    }
    [Serializable] public class SaveData {
        public int version=1, mission, credits, damage, armor, utility, bestScore; public bool completed;
        public int Cost(int tier) => 150+Math.Max(0,tier)*100;
        public bool Buy(int kind) {
            if(kind<0||kind>2)return false;
            int tier=kind==0?damage:kind==1?armor:utility; int cost=Cost(tier);
            if(tier>=3||credits<cost)return false; credits-=cost;
            if(kind==0)damage++; else if(kind==1)armor++; else utility++; return true;
        }
        public bool Valid(int missionCount) => version==1 && mission>=0 && mission<missionCount && credits>=0 && credits<=10000000 && damage>=0 && damage<=3 && armor>=0 && armor<=3 && utility>=0 && utility<=3 && bestScore>=0;
    }
    public static class Rules {
        public static int Count(int wave,int baseCount,int difficulty) => Math.Min(18,Math.Max(1,baseCount)+Math.Max(0,wave-1)*2+Math.Max(0,difficulty-1));
        public static float Damage(int difficulty) => difficulty==0?.65f:difficulty==2?1.3f:1f;
        public static int Reward(int kills,int alarms) => Math.Max(150,200+Math.Max(0,kills)*25-Math.Max(0,alarms)*20);
    }
}
