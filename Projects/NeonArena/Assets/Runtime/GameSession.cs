using System;
using System.Collections.Generic;
using UnityEngine;
namespace Frontier {
    public sealed class GameSession : MonoBehaviour {
        public static GameSession I {get; private set;}
        public Definition Def; public SaveData Save; public SaveStore Store;
        public Screen State=Screen.Title; public WorldBuilder World; public PlayerController Player; public CombatService Combat;
        public readonly List<EnemyBrain> Enemies=new List<EnemyBrain>(); public readonly List<ObjectiveNode> Nodes=new List<ObjectiveNode>();
        public Transform Live; Transform level; public int MissionIndex,Wave=1,Kills,Score,Alarms,Collected,Difficulty=1;
        public float Elapsed,Alarm,Reactor=100,WaveWait,MessageUntil; public string Message="",Prompt="",Debrief="";
        public float Sensitivity=1,Volume=.55f; public bool Smoke,ReadyToExtract;
        float spawnLeft,reinforcementAt; int toSpawn; bool waveResolved; int runScore;
        public Mode Mode=>Def.Mode;
        public Mission Mission=>Def.missions[MissionIndex];
        void Awake() {
            if(I&&I!=this){Destroy(gameObject);return;} I=this;
            Time.timeScale=1; Application.targetFrameRate=120;
            Smoke=Array.IndexOf(Environment.GetCommandLineArgs(),"--smoke-test")>=0;
            Def=JsonUtility.FromJson<Definition>(Resources.Load<TextAsset>("GameDefinition").text);
            Store=new SaveStore(Def.id,Def.missions.Length,Smoke); Save=Smoke?new SaveData():Store.Load();
            Sensitivity=PlayerPrefs.GetFloat(Def.id+".sensitivity",1); Volume=PlayerPrefs.GetFloat(Def.id+".volume",.55f);
            MissionIndex=Save.mission; Combat=gameObject.AddComponent<CombatService>(); Combat.Init(this);
            gameObject.AddComponent<GameHud>().Session=this; BuildPreview(); SetCursor(false);
            if(Smoke)gameObject.AddComponent<SmokeRunner>();
        }
        void BuildPreview() {
            level=new GameObject("Mission world").transform; level.SetParent(transform);
            World=new WorldBuilder(); World.Build(level,Def,MissionIndex);
            Live=new GameObject("Actors").transform; Live.SetParent(level);
            Player=PlayerController.Create(this,World.Start); Player.gameObject.SetActive(true);
        }
        public void StartCampaign(bool fresh) {
            if(fresh){int best=Save.bestScore; Save=new SaveData{bestScore=best}; runScore=0;}
            MissionIndex=Save.mission; StartMission();
        }
        public void StartMission() {
            if(level){level.gameObject.SetActive(false);Destroy(level.gameObject);} World?.Dispose();
            Enemies.Clear(); Nodes.Clear(); Combat.Clear();
            Kills=Score=Alarms=Collected=0; Elapsed=Alarm=0; Reactor=100; Wave=1; ReadyToExtract=false;
            spawnLeft=.4f; WaveWait=0; waveResolved=false; reinforcementAt=0; Prompt="";
            BuildPreview();
            if(Mode==Mode.Stealth){
                foreach(var p in World.Objectives)Nodes.Add(ObjectiveNode.Create(this,p,false));
                toSpawn=0; for(int i=0;i<Mission.enemies;i++)Spawn(i);
                Toast("Stay unseen. Hold E at three data terminals.");
            } else {
                toSpawn=Rules.Count(Wave,Mission.enemies,Difficulty);
                if(Mode==Mode.Mech)foreach(var p in World.Objectives)Nodes.Add(ObjectiveNode.Create(this,p,true));
                else World.Shape("Reactor",PrimitiveType.Cylinder,new Vector3(0,1.5f,0),new Vector3(2,1.5f,2),World.Accent,Live,false);
                Toast(Mode==Mode.Mech?"Destroy relay towers and defeat the assault waves.":"Protect the reactor. Clear every wave.");
            }
            State=Screen.Playing; Time.timeScale=1; SetCursor(true);
        }
        public void Spawn(int index) {
            Vector3 p=World.SpawnPoints[index%World.SpawnPoints.Count];
            if(Mode==Mode.Stealth)p=World.OpenPosition(new Vector3(index%2==0?-12:12,0,-8+(index/2)*10));
            if(Player&&Vector3.Distance(Player.transform.position,p)<12)p=World.SpawnPoints[(index+2)%World.SpawnPoints.Count];
            int role=Mode==Mode.Stealth?index%3:index%4;
            bool boss=Mode==Mode.Mech&&Wave==Mission.waves&&index==0;
            Enemies.Add(EnemyBrain.Create(this,p,role,boss));
        }
        void Update() {
            if(Input.GetKeyDown(KeyCode.Escape)) {if(State==Screen.Playing)Pause();else if(State==Screen.Paused)Resume();}
            if(State!=Screen.Playing)return;
            if(Cursor.lockState!=CursorLockMode.Locked&&!Smoke){Pause();return;}
            Elapsed+=Time.deltaTime; Prompt="";
            if(Mission.timeLimit>0&&Elapsed>Mission.timeLimit){Lose("Mission time expired");return;}
            if(Reactor<=0){Lose("Reactor destroyed");return;}
            Enemies.RemoveAll(e=>!e||e.Dead);
            if(Mode!=Mode.Stealth) {
                if(toSpawn>0){spawnLeft-=Time.deltaTime;if(spawnLeft<=0){Spawn(toSpawn-1);toSpawn--;spawnLeft=.8f;}}
                if(toSpawn==0&&Enemies.Count==0&&!waveResolved){waveResolved=true;WaveWait=5;Toast(Wave<Mission.waves?"Wave clear. Resupply and regroup.":"Hostiles eliminated.");Player.Ammo.Supply(90);Player.Heal(25);}
                if(waveResolved&&Wave<Mission.waves){WaveWait-=Time.deltaTime;if(WaveWait<=0){Wave++;waveResolved=false;toSpawn=Rules.Count(Wave,Mission.enemies,Difficulty);}}
                ReadyToExtract=waveResolved&&Wave==Mission.waves&&(Mode==Mode.Arena||Collected==Nodes.Count);
            } else {
                bool seen=false;foreach(var e in Enemies)if(e.SeesPlayer){seen=true;break;}
                Alarm=Mathf.Clamp(Alarm+(seen?8:-3)*Time.deltaTime,0,100);
                if(Alarm>=75&&Elapsed>=reinforcementAt){Alarms++;reinforcementAt=Elapsed+30;if(Enemies.Count<16){Spawn(Enemies.Count+3);Spawn(Enemies.Count+7);}Toast("ALARM: reinforcement patrol inbound");}
                ReadyToExtract=Collected>=3;
            }
            foreach(var n in Nodes)if(n)n.Tick();
            if(ReadyToExtract&&Vector3.Distance(Player.transform.position,World.Exit)<4){
                Prompt="E  /  EXTRACT"; if(Input.GetKeyDown(KeyCode.E))Win();
            }
        }
        public void Win() {
            if(State!=Screen.Playing)return;
            int reward=Rules.Reward(Kills,Alarms); Save.credits+=reward; Score+=reward;runScore+=Score;
            Save.bestScore=Math.Max(Save.bestScore,runScore);
            bool last=MissionIndex+1>=Def.missions.Length;
            if(last)Save.completed=true;else Save.mission=MissionIndex+1;
            State=last?Screen.Complete:Screen.Debrief; Time.timeScale=0;SetCursor(false);
            Debrief=$"{Kills} hostiles neutralized   /   {Elapsed:0}s elapsed\n+{reward} credits   /   {Alarms} alarm escalations";
            if(!Store.Write(Save))Toast("SAVE FAILED - check storage permissions");
        }
        public void Continue() { MissionIndex=Save.mission;StartMission(); }
        public void Lose(string reason) {if(State!=Screen.Playing)return;State=Screen.Defeat;Debrief=reason;Time.timeScale=0;SetCursor(false);}
        public void Pause(){if(State!=Screen.Playing)return;State=Screen.Paused;Time.timeScale=0;SetCursor(false);}
        public void Resume(){State=Screen.Playing;Time.timeScale=1;SetCursor(true);SaveOptions();}
        public void Title(){State=Screen.Title;Time.timeScale=1;SetCursor(false);SaveOptions();}
        void SetCursor(bool locked){Cursor.lockState=locked?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!locked;}
        public void Toast(string message){Message=message;MessageUntil=Time.unscaledTime+4;}
        public void SaveOptions(){if(Smoke)return;PlayerPrefs.SetFloat(Def.id+".sensitivity",Sensitivity);PlayerPrefs.SetFloat(Def.id+".volume",Volume);PlayerPrefs.Save();}
        public void Buy(int kind){if(Save.Buy(kind)){if(!Store.Write(Save))Toast("SAVE FAILED");}else Toast("Not enough credits or upgrade is at maximum");}
        public void EnemyKilled(EnemyBrain e,bool quiet) {
            Kills++; Score+=e.Boss?500:100; if(quiet)Score+=75;
            if(Kills%3==0)Pickup.Create(this,e.transform.position);
        }
        public void Noise(Vector3 position,float radius) {foreach(var e in Enemies)if(e&&!e.Dead&&Vector3.Distance(e.transform.position,position)<radius)e.Investigate(position);}
        void OnApplicationFocus(bool focus){if(!focus&&!Smoke&&State==Screen.Playing)Pause();}
        void OnDestroy(){World?.Dispose();Time.timeScale=1;SetCursor(false);if(I==this)I=null;}
    }
    public sealed class ObjectiveNode : MonoBehaviour {
        public GameSession Session; public bool Destructible,Done; public float Health=180,Progress;
        public static ObjectiveNode Create(GameSession s,Vector3 p,bool destroy) {
            var go=new GameObject(destroy?"Relay tower":"Data terminal");go.transform.SetParent(s.Live);go.transform.position=p;
            var n=go.AddComponent<ObjectiveNode>();n.Session=s;n.Destructible=destroy;
            s.World.Shape("Console",PrimitiveType.Cube,new Vector3(0,1,0),new Vector3(1.6f,2,1),new Color(.22f,.29f,.35f),go.transform,true,12);
            s.World.Shape("Status screen",PrimitiveType.Cube,new Vector3(0,1.5f,-.52f),new Vector3(1.3f,.6f,.08f),s.World.Accent,go.transform,false);
            return n;
        }
        public void Tick() {
            if(Done||Destructible)return;
            bool near=Vector3.Distance(transform.position,Session.Player.transform.position)<3;
            if(near){Session.Prompt=$"HOLD E / DOWNLOAD {Progress/2.5f:P0}";if(Input.GetKey(KeyCode.E)){Progress+=Time.deltaTime;if(Progress>=2.5f)Complete();}}
        }
        public void Damage(float n){if(!Destructible||Done)return;Health=Mathf.Max(0,Health-Mathf.Max(0,n));if(Health<=0)Complete();}
        public void Complete(){if(Done)return;Done=true;Session.Collected++;Session.Score+=200;Session.Combat.Burst(transform.position+Vector3.up,Session.World.Accent);foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;transform.localScale*=.5f;Session.Toast(Destructible?"Relay disabled":"Intelligence secured");}
    }
    public sealed class Pickup : MonoBehaviour {
        GameSession session; float age;
        public static void Create(GameSession s,Vector3 p){var go=s.World.Shape("Field supplies",PrimitiveType.Cube,p+Vector3.up*.5f,Vector3.one*.8f,s.World.Accent,s.Live,false);go.AddComponent<Pickup>().session=s;}
        void Update(){if(session.State!=Screen.Playing)return;age+=Time.deltaTime;transform.Rotate(0,60*Time.deltaTime,0);if(age>45){Destroy(gameObject);return;}if(Vector3.Distance(transform.position,session.Player.transform.position)<2){session.Player.Heal(25);session.Player.Ammo.Supply(60);session.Combat.Tone(650,.13f);Destroy(gameObject);}}
    }
}
