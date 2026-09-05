using System.Collections.Generic;
using UnityEngine;
namespace Frontier {
    public sealed class WorldBuilder {
        public const int WorldMask=1<<10;
        public readonly GridPathfinder Navigation=new GridPathfinder(40,40);
        public readonly List<Vector3> SpawnPoints=new List<Vector3>();
        public readonly List<Vector3> Objectives=new List<Vector3>();
        public readonly List<Vector3> Covers=new List<Vector3>();
        public readonly Vector3 Start=new Vector3(0,0,-32), Exit=new Vector3(0,0,32);
        public Transform Root; public Color Accent; readonly Dictionary<Color,Material> materials=new Dictionary<Color,Material>();
        public Material Material(Color c) {
            if(materials.TryGetValue(c,out var m))return m;
            m=new Material(Shader.Find("Standard")); m.color=c; m.SetFloat("_Glossiness",.3f); materials.Add(c,m); return m;
        }
        public GameObject Shape(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Color color,Transform parent=null,bool solid=true,int layer=10) {
            var o=GameObject.CreatePrimitive(type); o.name=name; o.layer=layer; o.transform.SetParent(parent?parent:Root,false);
            o.transform.localPosition=pos; o.transform.localScale=scale; o.GetComponent<Renderer>().sharedMaterial=Material(color);
            if(!solid){var c=o.GetComponent<Collider>(); c.enabled=false; if(Application.isPlaying)Object.Destroy(c);else Object.DestroyImmediate(c);} return o;
        }
        public Cell CellAt(Vector3 p) => new Cell(Mathf.Clamp(Mathf.FloorToInt((p.x+40)/2),0,39),Mathf.Clamp(Mathf.FloorToInt((p.z+40)/2),0,39));
        public Vector3 Position(Cell c) => new Vector3(c.X*2-39,0,c.Z*2-39);
        public Vector3 OpenPosition(Vector3 p) => Position(Navigation.NearestOpen(CellAt(p)));
        void Box(string name,Vector3 pos,Vector3 size,Color c,bool block=true) {
            Shape(name,PrimitiveType.Cube,pos,size,c);
            if(!block)return;
            for(int x=0;x<40;x++)for(int z=0;z<40;z++){
                var v=Position(new Cell(x,z));
                if(Mathf.Abs(v.x-pos.x)<=size.x/2+1f && Mathf.Abs(v.z-pos.z)<=size.z/2+1f)Navigation.Block(x,z);
            }
        }
        public void Build(Transform root,Definition def,int mission) {
            Root=root; Accent=def.Mode==Mode.Arena?new Color(.1f,.85f,.9f):def.Mode==Mode.Mech?new Color(1,.55f,.14f):new Color(.42f,.95f,.55f);
            RenderSettings.ambientLight=new Color(.37f,.43f,.52f); RenderSettings.fog=true; RenderSettings.fogColor=new Color(.065f,.09f,.14f); RenderSettings.fogDensity=.009f;
            var sun=new GameObject("Moon / key light").AddComponent<Light>(); sun.transform.SetParent(root); sun.type=LightType.Directional; sun.intensity=1.3f; sun.color=new Color(.75f,.84f,1); sun.shadows=LightShadows.Soft; sun.transform.rotation=Quaternion.Euler(48,-32,0);
            Box("District foundation",new Vector3(0,-.65f,0),new Vector3(86,1,86),new Color(.1f,.14f,.19f),false);
            for(int i=-40;i<=40;i+=4){
                Shape("Street inlay",PrimitiveType.Cube,new Vector3(i,-.13f,0),new Vector3(.045f,.02f,82),new Color(.17f,.23f,.28f),null,false);
                Shape("Street inlay",PrimitiveType.Cube,new Vector3(0,-.13f,i),new Vector3(82,.02f,.045f),new Color(.17f,.23f,.28f),null,false);
            }
            for(int side=-1;side<=1;side+=2){
                Box("Perimeter",new Vector3(side*42,2,0),new Vector3(2,5,86),new Color(.12f,.17f,.22f));
                Box("Perimeter",new Vector3(0,2,side*42),new Vector3(86,5,2),new Color(.12f,.17f,.22f));
            }
            var rng=new System.Random(def.missions[mission].seed);
            // Wide connected cross streets, perimeter routes and warehouse courtyards.
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2){
                float sx=def.Mode==Mode.Mech?8:10;
                Box("Warehouse",new Vector3(x*21,3,z*20),new Vector3(sx,6,10),new Color(.17f,.22f,.28f));
                Shape("Roof trim",PrimitiveType.Cube,new Vector3(x*21,6.1f,z*20),new Vector3(sx+.3f,.18f,10.3f),Accent,null,false);
                for(int k=0;k<3;k++)Shape("Facade light",PrimitiveType.Cube,new Vector3(x*(21-sx/2-.03f),3,z*20-3+k*3),new Vector3(.08f,1.2f,1),Accent,null,false);
            }
            for(int i=0;i<24;i++) {
                float x=(i%2==0?-1:1)*(7+(i%3)*5); float z=-28+(i/2)*5;
                if(Mathf.Abs(z)<5)continue;
                var pos=new Vector3(x, .8f,z); var size=new Vector3(2.2f,1.6f,2.2f);
                if(i%4==mission%4)size.y=3.3f;
                Box("Armored cover",new Vector3(pos.x,size.y/2-.15f,pos.z),size,new Color(.22f,.28f,.31f));
                Covers.Add(OpenPosition(pos+Vector3.right*(x>0?-4:4)));
            }
            // Distinct mission layouts with deliberate gates instead of sealed rooms.
            if(mission>0)for(int side=-1;side<=1;side+=2)Box("Checkpoint barricade",new Vector3(side*27,1.1f,0),new Vector3(14,2.5f,2),new Color(.27f,.29f,.3f));
            if(mission==2)for(int side=-1;side<=1;side+=2)Box("Inner blast wall",new Vector3(side*9,1.5f,13),new Vector3(7,3,2),new Color(.2f,.25f,.32f));
            for(int i=0;i<20;i++) {
                float a=i*Mathf.PI*2/20; float height=8+rng.Next(16);
                Shape("Distant skyline",PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*65,height/2,Mathf.Sin(a)*65),new Vector3(7,height,7),new Color(.1f,.13f,.19f),null,false);
            }
            foreach(var p in new[]{new Vector3(-34,0,30),new Vector3(34,0,30),new Vector3(-34,0,-24),new Vector3(34,0,-24),new Vector3(0,0,34)})SpawnPoints.Add(OpenPosition(p));
            foreach(var p in new[]{new Vector3(-32,0,8),new Vector3(30,0,10),new Vector3(0,0,25)})Objectives.Add(OpenPosition(p));
            Shape("Extraction pad",PrimitiveType.Cylinder,Exit+Vector3.up*.03f,new Vector3(7,.1f,7),Accent,null,false);
            Shape("Insertion pad",PrimitiveType.Cylinder,Start+Vector3.up*.03f,new Vector3(5,.1f,5),new Color(.2f,.4f,.5f),null,false);
        }
        public void Dispose() { foreach(var m in materials.Values){if(Application.isPlaying)Object.Destroy(m);else Object.DestroyImmediate(m);} materials.Clear(); }
    }
}
