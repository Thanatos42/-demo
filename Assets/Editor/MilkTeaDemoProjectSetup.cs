#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MilkTeaDemoProjectSetup
{
    private const string SceneDirectory = "Assets/Scenes";
    private const string ScenePath = SceneDirectory + "/MilkTeaDemo.unity";
    private const string SessionKey = "MilkTeaDemo.ProjectConfigured";

    static MilkTeaDemoProjectSetup()
    {
        EditorApplication.delayCall += ConfigureProjectOnce;
    }

    [MenuItem("奶茶店 Demo/打开演示场景")]
    public static void OpenDemoScene()
    {
        EnsureSceneExists();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static void ConfigureProjectOnce()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        PlayerSettings.companyName = "MilkTeaDemo";
        PlayerSettings.productName = "奶茶店模拟经营 Demo";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;

        EnsureSceneExists();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        Scene activeScene = SceneManager.GetActiveScene();
        if (!EditorApplication.isPlayingOrWillChangePlaymode &&
            !activeScene.isDirty && activeScene.path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }

    private static void EnsureSceneExists()
    {
        if (File.Exists(ScenePath))
        {
            return;
        }

        Directory.CreateDirectory(SceneDirectory);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
    }
}
#endif
