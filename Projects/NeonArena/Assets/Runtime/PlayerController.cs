using UnityEngine;
namespace Frontier {
    public sealed class PlayerController : MonoBehaviour {
        public GameSession Session; public CharacterController Motor; public Camera View; public Transform Model;
        public Magazine Ammo; public Heat Heat=new Heat(); public float Health,MaxHealth,Energy=100,AbilityLeft,HitUntil,HitMarkerUntil;
        public int Weapon; public EnemyBrain LockedTarget; public bool Aiming,Crouched; public Vector3 AimPoint;
        float yaw,pitch=18,vertical,shotAt,dashLeft,invulnerableUntil; Vector3 dashDirection; float noiseAt;
        public static PlayerController Create(GameSession s,Vector3 pos) {
            var go=new GameObject("Player");go.layer=8;go.tag="Player";go.transform.SetParent(s.Live);go.transform.position=pos+Vector3.up*.3f;
            var p=go.AddComponent<PlayerController>();p.Session=s;p.MaxHealth=s.Mode==Mode.Mech?240:100;p.MaxHealth+=s.Save.armor*30;p.Health=p.MaxHealth;
            p.Ammo=new Magazine(s.Mode==Mode.Stealth?12:30,180);
            p.Motor=go.AddComponent<CharacterController>();p.Motor.height=1.9f;p.Motor.center=Vector3.up*.95f;p.Motor.radius=.4f;p.Motor.stepOffset=.35f;
            p.Model=new GameObject("Visible pilot").transform;p.Model.SetParent(go.transform,false);
            var w=s.World;float scale=s.Mode==Mode.Mech?1.3f:1;
            w.Shape("Armor torso",PrimitiveType.Cube,new Vector3(0,1.1f,0),new Vector3(.65f,.7f,.4f)*scale,w.Accent,p.Model,false,8);
            w.Shape("Helmet",PrimitiveType.Sphere,new Vector3(0,1.7f,0),Vector3.one*.4f,new Color(.16f,.21f,.27f),p.Model,false,8);
            w.Shape("Visor",PrimitiveType.Cube,new Vector3(0,1.74f,.2f),new Vector3(.32f,.1f,.09f),Color.white,p.Model,false,8);
            for(int side=-1;side<=1;side+=2){
                w.Shape("Leg",PrimitiveType.Cube,new Vector3(side*.21f,.4f,0),new Vector3(.22f,.7f,.28f),new Color(.17f,.21f,.25f),p.Model,false,8);
                w.Shape("Arm",PrimitiveType.Cube,new Vector3(side*.48f,1.12f,.12f),new Vector3(.2f,.6f,.23f),new Color(.2f,.25f,.31f),p.Model,false,8);
            }
            w.Shape("Weapon",PrimitiveType.Cube,new Vector3(.45f,1.23f,.6f),new Vector3(.15f,.18f,.8f),new Color(.1f,.12f,.16f),p.Model,false,8);
            p.View=new GameObject("Third person camera").AddComponent<Camera>();p.View.transform.SetParent(s.Live);p.View.fieldOfView=66;p.View.nearClipPlane=.12f;p.View.farClipPlane=180;p.View.backgroundColor=RenderSettings.fogColor;p.View.clearFlags=CameraClearFlags.SolidColor;p.View.gameObject.tag="MainCamera";p.View.gameObject.AddComponent<AudioListener>();
            return p;
        }
        void Update() {
            if(Session.State!=Screen.Playing)return;
            float dt=Time.deltaTime;Ammo.Tick(dt);Heat.Tick(dt,15+Session.Save.utility*4);AbilityLeft=Mathf.Max(0,AbilityLeft-dt);
            Aiming=Input.GetKey(KeyCode.LeftAlt);Crouched=Input.GetKey(KeyCode.C)&&Session.Mode!=Mode.Mech;
            yaw+=(Input.GetAxisRaw("Mouse X")*2.2f+(Input.GetKey(KeyCode.RightArrow)?90*dt:0)-(Input.GetKey(KeyCode.LeftArrow)?90*dt:0))*Session.Sensitivity;
            pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*1.6f*Session.Sensitivity+(Input.GetKey(KeyCode.DownArrow)?60*dt:0)-(Input.GetKey(KeyCode.UpArrow)?60*dt:0),-28,65);
            Vector3 direction=Quaternion.Euler(0,yaw,0)*new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"));direction=Vector3.ClampMagnitude(direction,1);
            bool sprint=Input.GetKey(KeyCode.LeftShift)&&Energy>5&&!Aiming&&!Crouched&&direction.sqrMagnitude>.1f;
            Energy=Mathf.Clamp(Energy+(sprint?-22:16)*dt,0,100);
            float speed=Session.Mode==Mode.Mech?5.8f:6;speed*=Crouched?.5f:Aiming?.65f:sprint?1.6f:1;
            if(Motor.isGrounded&&vertical<0)vertical=-2;if(Motor.isGrounded&&Input.GetKeyDown(KeyCode.Space))vertical=6.3f;vertical-=19*dt;
            if(dashLeft>0){dashLeft-=dt;Motor.Move((dashDirection*22+Vector3.up*vertical)*dt);}else Motor.Move((direction*speed+Vector3.up*vertical)*dt);
            Model.localScale=new Vector3(1,Crouched?.72f:1,1);
            if(Aiming||direction.sqrMagnitude>.01f)Model.rotation=Quaternion.Slerp(Model.rotation,Quaternion.LookRotation(Aiming?Quaternion.Euler(0,yaw,0)*Vector3.forward:direction),dt*14);
            if(sprint&&Time.time>noiseAt){noiseAt=Time.time+1;Session.Noise(transform.position,14);}
            if(Input.GetKeyDown(KeyCode.R)){if(Ammo.Reload())Session.Combat.Tone(300,.15f);}
            if(Input.GetKeyDown(KeyCode.Alpha1))Weapon=0;if(Input.GetKeyDown(KeyCode.Alpha2)&&Session.Mode!=Mode.Stealth)Weapon=1;
            if(Input.GetKeyDown(KeyCode.Tab)&&Session.Mode==Mode.Mech)AcquireLock();
            if(LockedTarget&&(LockedTarget.Dead||Vector3.Distance(transform.position,LockedTarget.transform.position)>50))LockedTarget=null;
            UpdateAim();
            if(Input.GetMouseButton(0)&&Time.time>=shotAt)Fire();
            if(Input.GetKeyDown(KeyCode.Q))Ability();
            if(Input.GetKeyDown(KeyCode.G))Gadget();
            if(Session.Mode==Mode.Stealth&&Input.GetKeyDown(KeyCode.F))Takedown();
            if(transform.position.y<-10)Damage(999);
        }
        void UpdateAim(){var ray=View.ViewportPointToRay(new Vector3(.5f,.5f));AimPoint=ray.GetPoint(85);if(Physics.Raycast(ray,out var hit,85,(1<<9)|(1<<10)|(1<<12),QueryTriggerInteraction.Ignore))AimPoint=hit.point;}
        public bool Fire() {
            if(Session.State!=Screen.Playing||Time.time<shotAt)return false;
            bool mech=Session.Mode==Mode.Mech;
            if(mech){if(Heat.Locked)return false;if(!Heat.Add(Weapon==0?17:34))return false;}else if(!Ammo.Fire()){Ammo.Reload();return false;}
            shotAt=Time.time+(mech?(Weapon==0?.24f:.8f):Weapon==0?.13f:.65f);
            Vector3 origin=transform.position+Vector3.up*1.4f+Model.forward*.65f;
            Vector3 target=LockedTarget&&!Physics.Linecast(origin,LockedTarget.transform.position+Vector3.up,WorldBuilder.WorldMask)?LockedTarget.transform.position+Vector3.up:AimPoint;
            Vector3 dir=(target-origin).normalized;
            float damage=(mech?32:Session.Mode==Mode.Stealth?45:23)*(1+Session.Save.damage*.18f);
            if(mech){Session.Combat.Launch(origin,dir,damage,Weapon==1?5:0,true,Weapon==1?28:48);}
            else {
                int pellets=Weapon==1?6:1;
                for(int i=0;i<pellets;i++){
                    Vector3 spread=Random.insideUnitSphere*(Weapon==1?.055f:Aiming?.003f:.018f);
                    Session.Combat.Hitscan(origin,(dir+spread).normalized,damage/(Weapon==1?1.7f:1),true);
                }
            }
            Session.Combat.Tone(mech?85:200,.075f);Session.Noise(transform.position,Session.Mode==Mode.Stealth?9:35);return true;
        }
        void Ability() {
            if(AbilityLeft>0)return;
            if(Session.Mode==Mode.Mech){dashDirection=Model.forward;dashLeft=.3f;invulnerableUntil=Time.time+.5f;AbilityLeft=5-Session.Save.utility*.6f;}
            else if(Session.Mode==Mode.Arena){Heal(35);AbilityLeft=22-Session.Save.utility*3;Session.Toast("Field medkit activated");}
            else{invulnerableUntil=Time.time+3;AbilityLeft=20-Session.Save.utility*2;Session.Toast("Cloak active / 3 seconds");}
        }
        void Gadget() {
            if(Energy<50)return;Energy-=50;Vector3 origin=transform.position+Vector3.up*1.3f;
            if(Session.Mode==Mode.Stealth){Vector3 point=Session.World.OpenPosition(transform.position+Model.forward*12);Session.Noise(point,30);Session.Combat.Burst(point+Vector3.up,Color.yellow);Session.Toast("Noise lure deployed");}
            else{Session.Combat.Launch(origin,(AimPoint-origin).normalized,85*(1+Session.Save.damage*.18f),6,true,24);Session.Combat.Tone(110,.15f);}
        }
        void AcquireLock(){LockedTarget=null;float best=.8f;foreach(var e in Session.Enemies){if(!e||e.Dead)continue;Vector3 d=e.transform.position-transform.position;if(d.magnitude>50||Physics.Linecast(transform.position+Vector3.up,e.transform.position+Vector3.up,WorldBuilder.WorldMask))continue;float dot=Vector3.Dot(View.transform.forward,d.normalized);if(dot>best){best=dot;LockedTarget=e;}}}
        void Takedown(){foreach(var e in Session.Enemies){if(!e||e.Dead||Vector3.Distance(transform.position,e.transform.position)>2.6f)continue;if(Vector3.Dot(e.transform.forward,(transform.position-e.transform.position).normalized)<-.2f){e.Damage(999,true);Session.Combat.Tone(150,.1f);return;}}Session.Toast("Takedown requires approaching a guard from behind");}
        public bool Cloaked=>Session.Mode==Mode.Stealth&&Time.time<invulnerableUntil;
        public void Heal(float amount){Health=Mathf.Min(MaxHealth,Health+Mathf.Max(0,amount));}
        public void Damage(float amount){if(Session.State!=Screen.Playing||Time.time<invulnerableUntil)return;Health=Mathf.Max(0,Health-Mathf.Max(0,amount));HitUntil=Time.time+.35f;Session.Combat.Tone(70,.1f);if(Health<=0)Session.Lose("Pilot down - retry this mission");}
        void LateUpdate(){
            if(!View||!Session.World.Root)return;
            Quaternion rot=Quaternion.Euler(pitch,yaw,0);Vector3 pivot=transform.position+Vector3.up*(Crouched?1:1.55f);
            float distance=Aiming?2.5f:Session.Mode==Mode.Mech?7:5.5f;
            Vector3 desired=pivot-rot*Vector3.forward*distance+rot*Vector3.right*.65f;
            Vector3 offset=desired-pivot;
            if(Physics.SphereCast(pivot,.22f,offset.normalized,out var hit,offset.magnitude,WorldBuilder.WorldMask,QueryTriggerInteraction.Ignore))desired=pivot+offset.normalized*Mathf.Max(.25f,hit.distance-.1f);
            View.transform.SetPositionAndRotation(desired,rot);View.fieldOfView=Mathf.Lerp(View.fieldOfView,Aiming?52:68,Time.unscaledDeltaTime*10);
        }
    }
}
