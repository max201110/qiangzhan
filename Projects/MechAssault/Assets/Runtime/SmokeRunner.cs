using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Frontier {
    public sealed class SmokeRunner : MonoBehaviour {
        int assertions;GameSession s;bool failed;string folder;
        void CaptureWorld(){
            var cam=s.Player.View;var target=new RenderTexture(1600,900,24);var previous=RenderTexture.active;
            var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try{cam.targetTexture=target;cam.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(folder,"world-render.png"),image.EncodeToPNG());
                var pixels=image.GetPixels32();int bright=0;for(int i=0;i<pixels.Length;i+=200)if(pixels[i].r+pixels[i].g+pixels[i].b>30)bright++;
                Check(bright>100,"offscreen camera renders nonblank image");
            }finally{cam.targetTexture=null;RenderTexture.active=previous;target.Release();Destroy(target);Destroy(image);}
        }
        void Check(bool value,string name){if(!value){failed=true;Debug.LogError("SMOKE FAIL: "+name);throw new Exception(name);}assertions++;Debug.Log("SMOKE PASS: "+name);}
        IEnumerator Start(){
            s=GetComponent<GameSession>();folder=Path.Combine(Application.dataPath,"..","SmokeResults");Directory.CreateDirectory(folder);
            Application.logMessageReceived+=OnLog;yield return null;
            Check(s.State==Screen.Title,"title loaded");
            yield return new WaitForEndOfFrame();UnityEngine.ScreenCapture.CaptureScreenshot(Path.Combine(folder,"title.png"));yield return null;
            s.StartCampaign(true);yield return null;yield return null;
            Check(s.State==Screen.Playing,"deployment state");Check(s.Player.View&&s.Player.Motor,"player camera/controller present");
            Check(s.World.Navigation.FindPath(s.World.CellAt(s.World.Start),s.World.CellAt(s.World.Exit)).Count>0,"extraction reachable");
            foreach(var p in s.World.Objectives)Check(s.World.Navigation.FindPath(s.World.CellAt(s.World.Start),s.World.CellAt(p)).Count>0,"objective reachable");
            s.Pause();float clock=s.Elapsed;yield return null;Check(s.Elapsed==clock&&Time.timeScale==0,"pause freezes mission");s.Resume();
            Check(s.Player.Ammo.Fire(),"weapon consumes ammo");Check(s.Player.Ammo.Reload(),"reload starts");s.Player.Ammo.Tick(2);Check(s.Player.Ammo.Ammo==s.Player.Ammo.Capacity,"reload completes");
            float health=s.Player.Health;s.Player.Damage(8);Check(s.Player.Health<health,"damage applied");s.Player.Heal(999);Check(s.Player.Health==s.Player.MaxHealth,"health clamp");
            var target=EnemyBrain.Create(s,new Vector3(0,0,-20),0,false);s.Enemies.Add(target);Physics.SyncTransforms();
            s.Combat.Hitscan(new Vector3(0,1,-26),Vector3.forward,999,true);Check(target.Dead,"hitscan kills an actual collider");
            var projectileTarget=EnemyBrain.Create(s,new Vector3(0,0,-20),0,false);s.Enemies.Add(projectileTarget);projectileTarget.enabled=false;Physics.SyncTransforms();
            s.Combat.Launch(new Vector3(0,1,-26),Vector3.forward,999,0,true,48);yield return new WaitForSeconds(.4f);Check(!projectileTarget||projectileTarget.Dead,"swept projectile hits target");
            // Real physics occlusion: a test wall must stop damage before the guard.
            var wall=s.World.Shape("Smoke occluder",PrimitiveType.Cube,new Vector3(0,1,-24),new Vector3(3,3,1),Color.gray,s.Live,true);
            var blocked=EnemyBrain.Create(s,new Vector3(0,0,-20),0,false);s.Enemies.Add(blocked);blocked.enabled=false;Physics.SyncTransforms();
            s.Combat.Hitscan(new Vector3(0,1,-26),Vector3.forward,999,true);Check(!blocked.Dead,"wall blocks hitscan");
            s.Combat.Explosion(new Vector3(0,1,-26),999,10,true);Check(!blocked.Dead,"wall blocks explosive damage");
            wall.SetActive(false);Destroy(wall);blocked.Damage(999);
            yield return new WaitForSeconds(2);
            yield return new WaitForEndOfFrame();UnityEngine.ScreenCapture.CaptureScreenshot(Path.Combine(folder,"gameplay.png"));yield return null;
            Check(s.Player.Health>0,"runtime survives live AI frames");CaptureWorld();
            if(s.Mode==Mode.Mech){foreach(var n in s.Nodes)n.Damage(999);Check(s.Collected==3,"relay destruction");}
            if(s.Mode==Mode.Stealth){foreach(var n in s.Nodes)n.Complete();Check(s.Collected==3,"intel completion");}
            // State transitions are tested explicitly, not presented as an autonomous full playthrough.
            s.Lose("smoke failure path");Check(s.State==Screen.Defeat,"defeat state");s.StartMission();yield return null;Check(s.Player.Health==s.Player.MaxHealth&&s.Collected==0,"retry resets actors/objectives");
            s.Win();Check(s.State==Screen.Debrief,"mission debrief");Check(s.Save.mission==1,"checkpoint advanced");
            Check(s.Store.Load().mission==1,"save/load roundtrip");
            var recovery=new SaveStore("recovery-"+s.Def.id,3,true);recovery.Write(new SaveData{credits=123});recovery.Write(new SaveData{credits=456});
            string recoveryPath=Path.Combine(Application.persistentDataPath,"SmokeTests","recovery-"+s.Def.id+".json");
            File.WriteAllText(recoveryPath,"corrupted test file");Check(recovery.Load().credits==123,"corrupt save recovers previous backup");s.Save.credits=999;s.Buy(0);Check(s.Save.damage==1,"workshop purchase");
            s.Continue();yield return null;Check(s.MissionIndex==1,"second chapter loaded");s.Win();s.Continue();yield return null;Check(s.MissionIndex==2,"third chapter loaded");s.Win();Check(s.State==Screen.Complete&&s.Save.completed,"campaign completion");
            yield return new WaitForEndOfFrame();UnityEngine.ScreenCapture.CaptureScreenshot(Path.Combine(folder,"complete.png"));yield return null;
            File.WriteAllText(Path.Combine(folder,"result.json"),"{\"passed\":"+(!failed?"true":"false")+",\"assertions\":"+assertions+",\"project\":\""+s.Def.id+"\"}");
            Debug.Log("SMOKE COMPLETE: "+s.Def.id+" assertions="+assertions);Application.Quit(failed?1:0);
        }
        void OnLog(string text,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error){failed=true;File.WriteAllText(Path.Combine(folder,"failure.txt"),text+"\n"+trace);Application.Quit(1);}}
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
