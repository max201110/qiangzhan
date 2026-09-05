using UnityEngine;
namespace Frontier {
    public sealed class GameHud : MonoBehaviour {
        public GameSession Session;GUIStyle title,heading,body,small,button;bool initialized,confirmNew;float fps;
        readonly Color panel=new Color(.035f,.055f,.085f,.94f),muted=new Color(.59f,.68f,.77f);
        void Init(){
            title=new GUIStyle(GUI.skin.label){fontSize=55,fontStyle=FontStyle.Bold};title.normal.textColor=Color.white;
            heading=new GUIStyle(title){fontSize=25};body=new GUIStyle(title){fontSize=17,fontStyle=FontStyle.Normal,wordWrap=true};
            small=new GUIStyle(body){fontSize=13};small.normal.textColor=muted;
            button=new GUIStyle(GUI.skin.button){fontSize=18,fontStyle=FontStyle.Bold,padding=new RectOffset(15,15,10,10)};initialized=true;
        }
        void Update(){fps=Mathf.Lerp(fps,1/Mathf.Max(.001f,Time.unscaledDeltaTime),.04f);}
        void Rect(float x,float y,float w,float h,Color c){Color prev=GUI.color;GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=prev;}
        void Text(float x,float y,float w,float h,string t,GUIStyle style=null){GUI.Label(new Rect(x,y,w,h),t,style??body);}
        bool Button(float x,float y,float w,string label){return GUI.Button(new Rect(x,y,w,48),label,button);}
        void Bar(float x,float y,float w,float val,Color color){Rect(x,y,w,7,new Color(.18f,.23f,.3f));Rect(x,y,w*Mathf.Clamp01(val),7,color);}
        void OnGUI(){
            if(!Session||!Session.Player)return;if(!initialized)Init();
            var matrix=GUI.matrix;float scale=Mathf.Min(UnityEngine.Screen.width/1600f,UnityEngine.Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((UnityEngine.Screen.width-1600*scale)/2,(UnityEngine.Screen.height-900*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            if(Session.State==Screen.Playing)Playing();else Menu();
            if(Time.unscaledTime<Session.MessageUntil){Rect(440,802,720,48,panel);Rect(440,802,4,48,Session.World.Accent);Text(458,815,692,30,Session.Message);}
            GUI.matrix=matrix;
        }
        void Playing(){
            var p=Session.Player;var accent=Session.World.Accent;
            Rect(28,25,440,102,panel);Rect(28,25,4,102,accent);
            Text(48,39,405,25,Session.Def.title.ToUpper(),heading);Text(48,76,400,30,$"SECTOR {Session.MissionIndex+1:00}  /  {Session.Mission.name}",small);
            Rect(580,25,460,91,panel);
            string objective=Session.ReadyToExtract?"EXTRACT AT THE NORTH PAD":Session.Mode==Mode.Arena?$"WAVE {Session.Wave} / {Session.Mission.waves}  •  {Session.Enemies.Count} HOSTILES":Session.Mode==Mode.Mech?$"RELAYS {Session.Collected}/3  •  WAVE {Session.Wave}/{Session.Mission.waves}":$"INTELLIGENCE {Session.Collected} / 3";
            Text(600,42,420,30,objective);Text(600,78,420,25,$"MISSION TIME  {Session.Elapsed:0}s    /    SCORE {Session.Score:00000}",small);
            MiniMap();
            Rect(28,730,345,136,panel);Text(48,748,290,26,$"ARMOR  {p.Health:0} / {p.MaxHealth:0}");Bar(48,782,300,p.Health/p.MaxHealth,p.HitUntil>Time.time?Color.red:accent);
            Text(48,803,300,23,Session.Mode==Mode.Mech?"THRUSTER / ORDNANCE ENERGY":"STAMINA / GADGET ENERGY",small);Bar(48,835,300,p.Energy/100,new Color(.85f,.65f,.25f));
            Rect(1230,730,342,136,panel);
            string weapon=Session.Mode==Mode.Mech?(p.Weapon==0?"PULSE CANNON":"SIEGE ROCKETS"):Session.Mode==Mode.Stealth?"SUPPRESSED PISTOL":p.Weapon==0?"ASSAULT RIFLE":"BREACH SHOTGUN";
            Text(1250,747,310,30,weapon,heading);
            if(Session.Mode==Mode.Mech){Text(1250,784,280,30,p.Heat.Locked?"OVERHEATED / COOLING":$"CORE HEAT  {p.Heat.Value:0}%");Bar(1250,833,292,p.Heat.Value/100,p.Heat.Locked?Color.red:accent);}
            else{Text(1250,789,285,37,$"{p.Ammo.Ammo:00} / {p.Ammo.Reserve:000}",heading);if(p.Ammo.ReloadLeft>0)Bar(1250,837,292,1-p.Ammo.ReloadLeft/1.6f,accent);}
            if(Session.Mode==Mode.Arena){Rect(28,143,345,60,panel);Text(45,151,310,25,$"REACTOR INTEGRITY  {Session.Reactor:0}%",small);Bar(45,185,310,Session.Reactor/100,accent);}
            if(Session.Mode==Mode.Stealth){Rect(28,143,345,60,panel);Text(45,151,310,25,$"ALARM LEVEL  {Session.Alarm:0}%",small);Bar(45,185,310,Session.Alarm/100,new Color(1,.45f,.2f));}
            Text(500,852,625,25,$"Q  {(p.AbilityLeft>0?p.AbilityLeft.ToString("0.0")+"s":"READY")}     G  GADGET     ALT  AIM     ESC  MENU",small);
            if(!string.IsNullOrEmpty(Session.Prompt)){Rect(545,664,510,50,panel);Text(567,678,470,30,Session.Prompt,heading);}
            float gap=p.Aiming?5:11;
            Rect(800-gap-9,449,9,2,Color.white);Rect(800+gap,449,9,2,Color.white);Rect(799,450-gap-9,2,9,Color.white);Rect(799,450+gap,2,9,Color.white);
            if(p.HitMarkerUntil>Time.time)Text(791,433,32,35,"×",heading);
            foreach(var e in Session.Enemies){if(!e||e.Dead)continue;Vector3 target=e.transform.position+Vector3.up*2.4f;var v=p.View.WorldToViewportPoint(target);if(v.z<=0||v.x<0||v.x>1||v.y<0||v.y>1)continue;
                if(Session.Mode==Mode.Stealth&&Physics.Linecast(p.View.transform.position,target,WorldBuilder.WorldMask))continue;
                float x=v.x*1600,y=(1-v.y)*900;Bar(x-35,y,70,e.Health/e.MaxHealth,e.Telegraph>0?Color.white:Color.red);
                if(Session.Mode==Mode.Stealth)Text(x-40,y-24,130,24,e.State.ToString().ToUpper(),small);
                if(p.LockedTarget==e)Text(x-30,y-35,160,30,"[ LOCK ]");
            }
            foreach(var n in Session.Nodes)if(n&&!n.Done){var v=p.View.WorldToViewportPoint(n.transform.position+Vector3.up*3);if(v.z>0&&v.x>.05f&&v.x<.95f&&v.y>.05f&&v.y<.95f)Text(v.x*1600-45,(1-v.y)*900,180,30,n.Destructible?"[ RELAY ]":"[ DATA ]",small);}
            Text(1450,882,130,18,$"{fps:0} FPS",small);
        }
        void MiniMap(){
            const float x=1328,y=25,size=244;Rect(x,y,size,size,panel);Text(x+14,y+10,210,25,"TACTICAL OVERVIEW",small);
            float left=x+18,top=y+42,w=208;Rect(left,top,w,184,new Color(.065f,.1f,.14f));
            for(int i=1;i<4;i++){Rect(left+i*w/4,top,1,184,new Color(.12f,.18f,.23f));Rect(left,top+i*184/4,w,1,new Color(.12f,.18f,.23f));}
            foreach(var c in Session.World.Covers)Dot(c,left,top,w,new Color(.3f,.37f,.4f),4);
            foreach(var e in Session.Enemies)if(e&&!e.Dead&&(Session.Mode!=Mode.Stealth||Vector3.Distance(e.transform.position,Session.Player.transform.position)<20))Dot(e.transform.position,left,top,w,e.Telegraph>0?Color.white:Color.red,5);
            foreach(var n in Session.Nodes)if(n&&!n.Done)Dot(n.transform.position,left,top,w,Color.yellow,7);
            Dot(Session.World.Exit,left,top,w,Session.ReadyToExtract?Color.green:Color.gray,9);Dot(Session.Player.transform.position,left,top,w,Session.World.Accent,7);
        }
        void Dot(Vector3 p,float x,float y,float w,Color c,float s){Rect(x+(p.x+42)/84*w-s/2,y+(42-p.z)/84*184-s/2,s,s,c);}
        void Menu(){
            Rect(0,0,1600,900,new Color(.01f,.025f,.05f,.78f));Rect(60,60,1480,780,panel);Rect(60,60,6,780,Session.World.Accent);
            Text(100,85,900,80,Session.Def.title.ToUpper(),title);Text(104,162,900,40,Session.Def.subtitle,small);
            Text(104,216,810,60,Session.State==Screen.Title?"CAMPAIGN OPERATIONS":Session.State==Screen.Paused?"TACTICAL PAUSE":Session.State==Screen.Defeat?"MISSION FAILED":Session.State==Screen.Complete?"CAMPAIGN COMPLETE":"MISSION DEBRIEF",heading);
            Text(104,276,780,95,Session.State==Screen.Title?Session.Def.missions[Session.Save.mission].brief:Session.State==Screen.Paused?Session.Mission.brief:Session.Debrief);
            if(Session.State==Screen.Title){
                if(Button(104,389,330,Session.Save.completed?"REPLAY FINAL MISSION":"CONTINUE CAMPAIGN"))Session.StartCampaign(false);
                if(Button(104,453,330,confirmNew?"CONFIRM NEW CAMPAIGN":"NEW CAMPAIGN")){if(confirmNew){confirmNew=false;Session.StartCampaign(true);}else confirmNew=true;}
                Text(104,523,340,25,"DIFFICULTY",small);Session.Difficulty=GUI.SelectionGrid(new Rect(104,555,580,45),Session.Difficulty,new[]{"TRAINING","STANDARD","VETERAN"},3,button);
                Text(104,632,650,40,$"BEST SCORE  {Session.Save.bestScore:00000}    /    CHAPTER {Session.Save.mission+1:00}");
                if(Button(104,710,330,"QUIT TO DESKTOP"))Application.Quit();
            }else if(Session.State==Screen.Paused){
                if(Button(104,389,330,"RESUME"))Session.Resume();if(Button(104,453,330,"RESTART MISSION"))Session.StartMission();
                if(Button(104,517,330,"RETURN TO TITLE"))Session.Title();
                Text(104,594,600,30,$"CAMERA SENSITIVITY  {Session.Sensitivity:0.0}");Session.Sensitivity=GUI.HorizontalSlider(new Rect(104,634,470,24),Session.Sensitivity,.3f,2.5f);
                Text(104,680,600,30,$"MASTER VOLUME  {Session.Volume:P0}");Session.Volume=GUI.HorizontalSlider(new Rect(104,720,470,24),Session.Volume,0,1);
            }else if(Session.State==Screen.Defeat){if(Button(104,410,330,"RETRY MISSION"))Session.StartMission();if(Button(104,477,330,"RETURN TO TITLE"))Session.Title();}
            else {
                Text(104,376,650,35,$"WORKSHOP CREDITS  {Session.Save.credits}",heading);
                string[] names={"WEAPON DAMAGE","ARMOR PLATING","UTILITY EFFICIENCY"};int[] tiers={Session.Save.damage,Session.Save.armor,Session.Save.utility};
                for(int i=0;i<3;i++)if(Button(104,436+i*65,680,$"{names[i]}   {tiers[i]}/3    {(tiers[i]>=3?"MAX":Session.Save.Cost(tiers[i])+" CR")}"))Session.Buy(i);
                if(Session.State==Screen.Debrief){if(Button(104,680,330,"DEPLOY NEXT MISSION"))Session.Continue();}else if(Button(104,680,330,"RETURN TO TITLE"))Session.Title();
            }
            Rect(929,217,1,560,new Color(.2f,.28f,.34f));Text(980,217,510,40,"FIELD MANUAL",heading);
            Text(980,277,495,410,"WASD  /  Move\nMouse or arrow keys  /  Camera\nLeft ALT  /  Aim down sights\nLeft mouse  /  Fire\nR  /  Reload     1, 2  /  Weapon\nSHIFT  /  Sprint     SPACE  /  Jump\nC  /  Crouch     E  /  Interact\nQ  /  Class ability     G  /  Gadget\nTAB  /  Mech target lock\nF  /  Stealth rear takedown\nESC  /  Pause and settings");
            Text(980,670,490,100,Session.Mode==Mode.Arena?"Protect the reactor through all waves. Reach the north extraction pad to finish. Q heals; G launches a grenade.":Session.Mode==Mode.Mech?"Destroy all three relays and every wave. Q dashes; TAB locks targets. Overheat blocks fire until cooled to 35%.":"Hold E to download three terminals. Crouch and use cover. Q cloaks for 3s; G distracts guards. Extract at the north pad.",small);
            Text(104,807,1200,23,"LOCAL SINGLE-PLAYER CAMPAIGN   /   3 MISSIONS   /   CHECKPOINT SAVE AT MISSION COMPLETION",small);
        }
    }
}
