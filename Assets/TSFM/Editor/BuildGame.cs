using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TSFM.Editor {
    [InitializeOnLoad]
    public static class BuildGame {
        const string Scene="Assets/TSFM/Scenes/Market.unity";
        static BuildGame(){EditorApplication.delayCall+=()=>{if(!Application.isBatchMode&&!File.Exists(Scene))Prepare();};}
        [MenuItem("TSFM/Prepare city block")]
        public static void Prepare() {
            Directory.CreateDirectory("Assets/TSFM/Scenes");
            if(!File.Exists(Scene)) {
                var previous=EditorSceneManager.GetActiveScene();
                if(previous.isDirty&&!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene,Scene);
            }
            if(AssetDatabase.LoadAssetAtPath<Material>("Assets/TSFM/Resources/MarketBase.mat")==null) {
                var material=new Material(Shader.Find("Standard"));material.color=Color.white;
                AssetDatabase.CreateAsset(material,"Assets/TSFM/Resources/MarketBase.mat");
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
            PlayerSettings.companyName="TSFM";PlayerSettings.productName="The Strangest Flea Market";PlayerSettings.bundleVersion="0.2.0";
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.runInBackground=true;
            PlayerSettings.WebGL.template="PROJECT:TSFM";
            // Disabled compression keeps the first deployment independent of host-specific encoding headers.
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback=false;
            QualitySettings.antiAliasing=2;QualitySettings.shadowDistance=55;
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        [MenuItem("TSFM/Build browser game")]
        public static void WebGL() {
            Prepare();
            var output=Environment.GetEnvironmentVariable("TSFM_BUILD_PATH");
            if(string.IsNullOrEmpty(output))output="server/public/game";
            Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=output,target=BuildTarget.WebGL,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Unity WebGL build failed: "+report.summary.result);
            Debug.Log("TSFM browser build ready at "+Path.GetFullPath(output));
        }
    }
}
