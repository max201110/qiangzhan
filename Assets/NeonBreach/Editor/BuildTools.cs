using System;
using System.IO;
using NeonBreach.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonBreach.Editor
{
    [InitializeOnLoad]
    public static class BuildTools
    {
        const string ScenePath = "Assets/NeonBreach/Scenes/Arena.unity";
        static BuildTools()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
                var scene = SceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(scene.path) && !scene.isDirty && !SessionState.GetBool("NeonBreach.Opened", false))
                {
                    SessionState.SetBool("NeonBreach.Opened", true);
                    EditorSceneManager.OpenScene(ScenePath);
                }
            };
        }
        [MenuItem("Neon Breach/Open Arena")]
        public static void OpenArena()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Neon Breach/Build Windows x64")]
        public static void BuildWindows()
        {
            string output = Path.GetFullPath("Builds/Windows/NeonBreach.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("NEON BREACH BUILD SUCCESS: " + output);
        }
        [MenuItem("Neon Breach/Validate Project")]
        public static void ValidateProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode before validation.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            bool found = false;
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.GetComponent<ArenaBootstrap>() != null) found = true;
            if (!found) throw new Exception("Arena bootstrap missing or unresolved.");
            if (Shader.Find("Standard") == null) throw new Exception("Built-in Standard shader missing.");
            var ammo = new AmmoState(30, 90);
            if (!ammo.TryShoot() || !ammo.StartReload(1) || !ammo.Tick(1) || ammo.Magazine != 30 || ammo.Reserve != 89) throw new Exception("Ammo regression.");
            var grid = new GridPathfinder(5, 5); grid.Block(2, 2);
            if (grid.FindPath(new Cell(0, 0), new Cell(4, 4)).Count != 8) throw new Exception("Navigation regression.");
            Debug.Log("NEON BREACH VALIDATION PASSED: Unity compilation, scene references, shaders and core rules.");
        }
    }
}
