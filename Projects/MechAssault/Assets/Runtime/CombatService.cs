using System.Collections.Generic;
using UnityEngine;
namespace Frontier {
    // Bounded pools prevent continuous Instantiate/Destroy while weapons fire.
    public sealed class CombatService : MonoBehaviour {
        sealed class Bolt {public GameObject Visual;public Vector3 Direction;public float Speed,Damage,Radius,Life;public bool Friendly;}
        sealed class Beam {public LineRenderer Line;public float Life;}
        readonly List<Bolt> bolts=new List<Bolt>();readonly List<Beam> beams=new List<Beam>();
        readonly Collider[] overlaps=new Collider[64];readonly HashSet<EnemyBrain> touched=new HashSet<EnemyBrain>();readonly HashSet<ObjectiveNode> nodes=new HashSet<ObjectiveNode>();
        GameSession session; Material material;AudioSource audioSource;AudioClip[] tones;
        public int ActiveBolts {get{int n=0;foreach(var b in bolts)if(b.Life>0)n++;return n;}}
        public void Init(GameSession s){
            session=s;material=new Material(Shader.Find("Sprites/Default"));audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;
            tones=new AudioClip[8];for(int k=0;k<tones.Length;k++){
                int n=6600;var data=new float[n];float hz=70+k*85;
                for(int i=0;i<n;i++){float t=(float)i/22050;data[i]=Mathf.Sin(2*Mathf.PI*hz*t)*Mathf.Exp(-t*25)*.25f;}
                var clip=AudioClip.Create("Synth "+k,n,1,22050,false);clip.SetData(data,0);tones[k]=clip;
            }
        }
        public void Tone(float hz,float duration){if(session.Volume<=0)return;int k=Mathf.Clamp(Mathf.RoundToInt((hz-70)/85),0,7);audioSource.PlayOneShot(tones[k],session.Volume);}
        public void Hitscan(Vector3 origin,Vector3 direction,float damage,bool friendly){
            int mask=WorldBuilder.WorldMask|(friendly?(1<<9)|(1<<12):1<<8);Vector3 end=origin+direction*90;
            if(Physics.Raycast(origin,direction,out var hit,90,mask,QueryTriggerInteraction.Ignore)){
                end=hit.point;if(friendly){var e=hit.collider.GetComponentInParent<EnemyBrain>();if(e){float multiplier=hit.point.y-e.transform.position.y>1.55f?1.7f:1;e.Damage(damage*multiplier);session.Player.HitMarkerUntil=Time.time+.15f;}var n=hit.collider.GetComponentInParent<ObjectiveNode>();if(n)n.Damage(damage);}else session.Player.Damage(damage);
            }
            Trace(origin,end,friendly?session.World.Accent:Color.red,.065f);
        }
        public void Launch(Vector3 origin,Vector3 direction,float damage,float radius,bool friendly,float speed){
            Bolt b=null;foreach(var p in bolts)if(p.Life<=0){b=p;break;}
            if(b==null){if(bolts.Count>=160)return;b=new Bolt();b.Visual=GameObject.CreatePrimitive(PrimitiveType.Sphere);b.Visual.name="Pooled projectile";b.Visual.transform.SetParent(transform);var col=b.Visual.GetComponent<Collider>();col.enabled=false;Destroy(col);b.Visual.GetComponent<Renderer>().sharedMaterial=material;bolts.Add(b);}
            b.Direction=direction.normalized;b.Damage=damage;b.Radius=radius;b.Friendly=friendly;b.Speed=speed;b.Life=5;
            b.Visual.transform.position=origin;b.Visual.transform.localScale=Vector3.one*(radius>0?.3f:.14f);b.Visual.SetActive(true);
            var props=new MaterialPropertyBlock();props.SetColor("_Color",friendly?Color.cyan:Color.red);b.Visual.GetComponent<Renderer>().SetPropertyBlock(props);
        }
        public void Trace(Vector3 a,Vector3 b,Color c,float life){
            Beam beam=null;foreach(var x in beams)if(x.Life<=0){beam=x;break;}
            if(beam==null){if(beams.Count>=160)return;beam=new Beam();var go=new GameObject("Pooled tracer");go.transform.SetParent(transform);beam.Line=go.AddComponent<LineRenderer>();beam.Line.sharedMaterial=material;beam.Line.positionCount=2;beam.Line.startWidth=beam.Line.endWidth=.04f;beams.Add(beam);}
            beam.Life=life;beam.Line.gameObject.SetActive(true);beam.Line.startColor=beam.Line.endColor=c;beam.Line.SetPosition(0,a);beam.Line.SetPosition(1,b);
        }
        public void Burst(Vector3 p,Color c){for(int i=0;i<8;i++)Trace(p,p+Random.onUnitSphere*1.5f,c,.2f);Tone(90,.1f);}
        public void Explosion(Vector3 p,float damage,float radius,bool friendly){
            Burst(p,Color.yellow);int count=Physics.OverlapSphereNonAlloc(p,radius,overlaps,(friendly?(1<<9)|(1<<12):1<<8),QueryTriggerInteraction.Ignore);
            touched.Clear();nodes.Clear();
            for(int i=0;i<count;i++){
                Vector3 target=overlaps[i].bounds.center;
                if(Physics.Linecast(p,target,WorldBuilder.WorldMask,QueryTriggerInteraction.Ignore))continue;
                float scaled=damage*Mathf.Lerp(1,.35f,Mathf.Clamp01(Vector3.Distance(p,target)/radius));
                if(friendly){var e=overlaps[i].GetComponentInParent<EnemyBrain>();if(e&&touched.Add(e))e.Damage(scaled);var n=overlaps[i].GetComponentInParent<ObjectiveNode>();if(n&&nodes.Add(n))n.Damage(scaled);}else session.Player.Damage(scaled);
            }
        }
        void Update(){
            if(session.State!=Screen.Playing)return;float dt=Time.deltaTime;
            foreach(var b in bolts){if(b.Life<=0)continue;Vector3 from=b.Visual.transform.position;float length=b.Speed*dt;
                int mask=WorldBuilder.WorldMask|(b.Friendly?(1<<9)|(1<<12):1<<8);
                if(Physics.SphereCast(from,.08f,b.Direction,out var hit,length,mask,QueryTriggerInteraction.Ignore)){
                    Vector3 impact=hit.point-b.Direction*.12f;
                    if(b.Radius>0)Explosion(impact,b.Damage,b.Radius,b.Friendly);
                    else if(b.Friendly){var e=hit.collider.GetComponentInParent<EnemyBrain>();if(e)e.Damage(b.Damage);var n=hit.collider.GetComponentInParent<ObjectiveNode>();if(n)n.Damage(b.Damage);}else if(hit.collider.GetComponentInParent<PlayerController>())session.Player.Damage(b.Damage);
                    if(b.Friendly)session.Player.HitMarkerUntil=Time.time+.15f;b.Life=0;
                }else{b.Visual.transform.position+=b.Direction*length;b.Life-=dt;}
                if(b.Life<=0)b.Visual.SetActive(false);
            }
            foreach(var b in beams)if(b.Life>0){b.Life-=dt;if(b.Life<=0)b.Line.gameObject.SetActive(false);}
        }
        public void Clear(){foreach(var b in bolts){b.Life=0;b.Visual.SetActive(false);}foreach(var b in beams){b.Life=0;b.Line.gameObject.SetActive(false);}}
        void OnDestroy(){if(material)Destroy(material);if(tones!=null)foreach(var c in tones)if(c)Destroy(c);}
    }
}
