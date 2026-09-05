using System.Collections.Generic;
using UnityEngine;
namespace Frontier {
    public enum AlertState { Patrol, Investigate, Engage, Reposition }
    public sealed class EnemyBrain : MonoBehaviour {
        public GameSession Session; public CharacterController Motor; public int Role;public bool Boss,Dead,SeesPlayer;
        public float Health,MaxHealth,Suspicion; public AlertState State;
        public float Telegraph; Vector3 lastSeen,shotTarget,patrolHome,goal; float memory,thinkAt,shootAt,gravity,patrolAt;
        List<Cell> path=new List<Cell>();int pathIndex,patrolIndex; Renderer visor;
        public static EnemyBrain Create(GameSession s,Vector3 position,int role,bool boss) {
            var go=new GameObject(boss?"WARDEN siege unit":role==0?"Patrol rifleman":role==1?"Long-range marksman":role==2?"Flanking scout":"Heavy gunner");go.layer=9;go.transform.SetParent(s.Live);go.transform.position=position+Vector3.up*.2f;
            var e=go.AddComponent<EnemyBrain>();e.Session=s;e.Role=role;e.Boss=boss;e.patrolHome=position;e.goal=position;
            e.MaxHealth=boss?600:role==3?150:80;e.MaxHealth*=1+s.MissionIndex*.15f;e.Health=e.MaxHealth;
            e.Motor=go.AddComponent<CharacterController>();e.Motor.center=Vector3.up;e.Motor.height=2;e.Motor.radius=.43f;e.Motor.stepOffset=.3f;
            float scale=boss?1.7f:role==3?1.2f:1;
            Color c=boss?new Color(.65f,.18f,.48f):role==1?new Color(.8f,.5f,.2f):role==2?new Color(.55f,.3f,.6f):new Color(.65f,.23f,.22f);
            s.World.Shape("Combat chassis",PrimitiveType.Cube,Vector3.up*1.05f,new Vector3(.65f,.9f,.48f)*scale,c,go.transform,false,9);
            s.World.Shape("Head",PrimitiveType.Sphere,Vector3.up*1.75f,Vector3.one*.43f,new Color(.22f,.25f,.29f),go.transform,false,9);
            e.visor=s.World.Shape("Threat indicator",PrimitiveType.Cube,new Vector3(0,1.8f,.22f),new Vector3(.33f,.09f,.05f),Color.red,go.transform,false,9).GetComponent<Renderer>();
            for(int side=-1;side<=1;side+=2)s.World.Shape("Servo leg",PrimitiveType.Cube,new Vector3(side*.2f,.36f,0),new Vector3(.23f,.65f,.27f),new Color(.15f,.18f,.22f),go.transform,false,9);
            s.World.Shape("Ranged weapon",PrimitiveType.Cube,new Vector3(.45f,1.25f,.45f),new Vector3(.17f,.2f,.8f),Color.gray,go.transform,false,9);
            e.shootAt=Time.time+1.2f+(role*.2f);e.thinkAt=Time.time+Random.Range(0,.6f);e.patrolAt=Time.time;return e;
        }
        public bool HasVision(Vector3 target) {
            Vector3 eye=transform.position+Vector3.up*1.65f,delta=target-eye;
            float range=Session.Mode==Mode.Stealth?Session.Player.Crouched?14:22:Role==1?46:32;
            if(delta.magnitude>range)return false;
            if(Session.Mode==Mode.Stealth&&Vector3.Angle(transform.forward,delta)>55&&State!=AlertState.Engage)return false;
            return !Physics.Linecast(eye,target,WorldBuilder.WorldMask,QueryTriggerInteraction.Ignore);
        }
        void Update() {
            if(Dead||Session.State!=Screen.Playing)return;
            float dt=Time.deltaTime;Vector3 pp=Session.Player.transform.position;
            SeesPlayer=!Session.Player.Cloaked&&HasVision(pp+Vector3.up*1.2f);
            if(SeesPlayer){lastSeen=pp;memory=6;Suspicion=Mathf.Min(100,Suspicion+dt*(Session.Mode==Mode.Stealth?42:300));if(Suspicion>=70)State=AlertState.Engage;}
            else {memory-=dt;Suspicion=Mathf.Max(0,Suspicion-dt*14);if(memory<=0)State=AlertState.Patrol;}
            if(Time.time>=thinkAt){thinkAt=Time.time+.9f+Role*.07f;Decide(pp);path=Session.World.Navigation.FindPath(Session.World.CellAt(transform.position),Session.World.CellAt(goal));pathIndex=0;}
            if(Telegraph>0){Telegraph-=dt;if(Telegraph<=0){Fire(shotTarget);shootAt=Time.time+(Boss?.7f:Role==1?2.8f:1.8f);}}
            else if(SeesPlayer&&State==AlertState.Engage&&Time.time>=shootAt){shotTarget=pp+Vector3.up*1.1f;Telegraph=Role==1?1.0f:.55f;Session.Combat.Trace(transform.position+Vector3.up*1.5f,shotTarget,new Color(1,.25f,.15f),.2f);}
            if(Session.Mode==Mode.Arena&&!SeesPlayer&&Vector3.Distance(transform.position,Vector3.zero)<6&&Time.time>=shootAt){Session.Reactor-=3*Rules.Damage(Session.Difficulty);shootAt=Time.time+2;Session.Combat.Trace(transform.position+Vector3.up,Vector3.up,Color.red,.15f);}
            Vector3 move=Vector3.zero;
            if(pathIndex<path.Count&&Telegraph<=0){var p=Session.World.Position(path[pathIndex]);var d=p-transform.position;d.y=0;if(d.magnitude<.5f)pathIndex++;else move=d.normalized;}
            if(Motor.isGrounded)gravity=-2;else gravity-=18*dt;
            float speed=Role==2?4.6f:Boss?2.2f:3;Motor.Move((move*speed+Vector3.up*gravity)*dt);
            Vector3 face=SeesPlayer&&State==AlertState.Engage?pp-transform.position:move;face.y=0;
            if(face.sqrMagnitude>.02f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(face),dt*6);
            visor.sharedMaterial=Session.World.Material(Telegraph>0?Color.white:State==AlertState.Engage?Color.red:Color.yellow);
        }
        void Decide(Vector3 player) {
            if(State==AlertState.Engage&&memory>0){
                float desired=Role==1?23:Role==3?11:Role==2?7:15;
                Vector3 away=transform.position-player;away.y=0;if(away.sqrMagnitude<.01f)away=Vector3.forward;
                Vector3 side=Vector3.Cross(away.normalized,Vector3.up)*(Role%2==0?1:-1);
                goal=Session.World.OpenPosition(player+away.normalized*desired+side*(Role==2?8:3));
                if(Health<MaxHealth*.3f&&Session.World.Covers.Count>0){float best=float.MaxValue;foreach(var cover in Session.World.Covers){float dist=Vector3.Distance(transform.position,cover);if(dist<best&&Physics.Linecast(cover+Vector3.up,player+Vector3.up,WorldBuilder.WorldMask)){best=dist;goal=cover;}}}
            }else if(memory>0)goal=Session.World.OpenPosition(lastSeen);
            else if(Session.Mode==Mode.Arena)goal=Session.World.OpenPosition(Vector3.zero);
            else if(Session.Mode==Mode.Mech)goal=Session.World.OpenPosition(player);
            else if(Time.time>=patrolAt){patrolAt=Time.time+6;patrolIndex++;Vector3 d=new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left}[patrolIndex%4];goal=Session.World.OpenPosition(patrolHome+d*8);}
        }
        void Fire(Vector3 point) {
            Vector3 origin=transform.position+Vector3.up*1.5f+transform.forward*.7f;
            if(Physics.Linecast(origin,point,WorldBuilder.WorldMask))return;
            float damage=(Boss?22:Role==1?25:11)*Rules.Damage(Session.Difficulty);
            Session.Combat.Launch(origin,(point-origin).normalized,damage,Boss?3:0,false,Role==1?42:24);
            if(Boss){Session.Combat.Launch(origin,Quaternion.Euler(0,12,0)*(point-origin).normalized,damage,2,false,22);Session.Combat.Launch(origin,Quaternion.Euler(0,-12,0)*(point-origin).normalized,damage,2,false,22);}
        }
        public void Investigate(Vector3 p){if(Dead||State==AlertState.Engage)return;State=AlertState.Investigate;lastSeen=p;memory=6;thinkAt=0;}
        public void Damage(float amount,bool quiet=false) {
            if(Dead)return;Health=Mathf.Max(0,Health-Mathf.Max(0,amount));
            if(Health<=0){Dead=true;Motor.enabled=false;Session.EnemyKilled(this,quiet);Session.Combat.Burst(transform.position+Vector3.up,Color.red);gameObject.SetActive(false);Destroy(gameObject);}
            else{State=AlertState.Engage;lastSeen=Session.Player.transform.position;memory=8;Suspicion=100;}
        }
    }
}
