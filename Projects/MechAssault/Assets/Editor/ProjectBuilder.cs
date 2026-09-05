using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Frontier.Editor {
    [InitializeOnLoad] public static class ProjectBuilder {
        const string Scene="Assets/Scenes/Campaign.unity";
        static int checks;
        static ProjectBuilder(){EditorApplication.delayCall+=()=>{if(!Application.isBatchMode&&!EditorApplication.isPlayingOrWillChangePlaymode&&!File.Exists(Scene))Prepare();};}
        static Definition Definition()=>JsonUtility.FromJson<Definition>(Resources.Load<TextAsset>("GameDefinition").text);
        [MenuItem("Campaign/Prepare and Open")]
        public static void Prepare(){
            var def=Definition();if(def==null||def.missions==null||def.missions.Length!=3)throw new Exception("Three mission definitions required");
            Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Assets/Resources");
            if(!File.Exists(Scene)){
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                new GameObject("Campaign bootstrap").AddComponent<GameSession>();EditorSceneManager.SaveScene(scene,Scene);
            }
            foreach(var shader in new[]{"Standard","Sprites/Default"}){
                string path="Assets/Resources/Keep"+shader.Replace("/","")+".mat";
                if(!AssetDatabase.LoadAssetAtPath<Material>(path))AssetDatabase.CreateAsset(new Material(Shader.Find(shader)),path);
            }
            PlayerSettings.companyName="Max201110";PlayerSettings.productName=def.id;PlayerSettings.bundleVersion="0.2.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.max201110."+def.id.ToLowerInvariant());
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Standard);
            QualitySettings.vSyncCount=0;QualitySettings.shadowDistance=55;QualitySettings.antiAliasing=2;
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};AssetDatabase.SaveAssets();
            if(!Application.isBatchMode)EditorSceneManager.OpenScene(Scene);
        }
        static void Check(bool value,string name){if(!value)throw new Exception("VALIDATION FAIL: "+name);checks++;Debug.Log("VALIDATION PASS: "+name);}
        [MenuItem("Campaign/Validate")]
        public static void Validate(){
            Prepare();checks=0;var def=Definition();var scene=EditorSceneManager.OpenScene(Scene);
            Check(scene.GetRootGameObjects().Length==1&&scene.GetRootGameObjects()[0].GetComponent<GameSession>(),"bootstrap scene");
            Check(Shader.Find("Standard")&&Shader.Find("Sprites/Default"),"render shaders");
            var ammo=new Magazine(4,3);for(int i=0;i<4;i++)Check(ammo.Fire(),"magazine shot "+i);
            Check(!ammo.Fire(),"empty magazine");Check(ammo.Reload(),"reload accepted");Check(!ammo.Fire(),"reload blocks fire");ammo.Tick(-1);Check(ammo.ReloadLeft==1.6f,"negative dt ignored");ammo.Tick(2);Check(ammo.Ammo==3&&ammo.Reserve==0,"partial reload conserves ammunition");
            ammo.Supply(1000);Check(ammo.Reserve==360,"reserve capacity");ammo.Supply(-10);Check(ammo.Reserve==360,"negative supply rejected");
            var heat=new Heat();for(int i=0;i<4;i++)heat.Add(25);Check(heat.Locked&&!heat.Add(1),"overheat locks fire");heat.Tick(3,20);Check(heat.Locked,"heat hysteresis");heat.Tick(1,20);Check(!heat.Locked,"heat recovery");
            var save=new SaveData{credits=1000};Check(save.Buy(0)&&save.damage==1&&save.credits==850,"upgrade transaction");Check(!save.Buy(-1),"invalid upgrade rejected");save.credits=0;Check(!save.Buy(1),"no negative credits");save.damage=4;Check(!save.Valid(3),"invalid save rejected");
            for(int wave=1;wave<30;wave++)Check(Rules.Count(wave,4,2)<=18,"bounded wave "+wave);
            for(int chapter=0;chapter<def.missions.Length;chapter++){
                var go=new GameObject("Temporary validation world");var world=new WorldBuilder();
                try{
                    world.Build(go.transform,def,chapter);
                    Check(world.Navigation.FindPath(world.CellAt(world.Start),world.CellAt(world.Exit)).Count>0,"chapter "+chapter+" extraction reachable");
                    foreach(var objective in world.Objectives)Check(world.Navigation.FindPath(world.CellAt(world.Start),world.CellAt(objective)).Count>0,"chapter "+chapter+" objective reachable");
                    foreach(var spawn in world.SpawnPoints)Check(world.Navigation.FindPath(world.CellAt(world.Start),world.CellAt(spawn)).Count>0,"chapter "+chapter+" spawn reachable");
                }finally{UnityEngine.Object.DestroyImmediate(go);world.Dispose();}
            }
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/validation.json","{\"project\":\""+def.id+"\",\"passed\":true,\"assertions\":"+checks+"}");Debug.Log("CAMPAIGN VALIDATION SUCCESS "+checks);
        }
        [MenuItem("Campaign/Build Windows")]
        public static void Build(){
            Validate();var def=Definition();string output="Builds/Windows/"+def.id+".exe";Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},target=BuildTarget.StandaloneWindows64,locationPathName=output,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            Debug.Log("CAMPAIGN BUILD SUCCESS: "+Path.GetFullPath(output));
        }
    }
}
