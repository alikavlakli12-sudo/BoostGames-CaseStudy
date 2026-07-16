using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MarbleSort.Editor
{
    [InitializeOnLoad]
    public static class PlayModeSceneGuard
    {
        private const string MainScenePath = "Assets/MarbleSort/Scenes/Main.unity";

        static PlayModeSceneGuard()
        {
            EditorApplication.delayCall += EnsureMainSceneConfigured;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && IsTestBootstrapSceneActive())
            {
                // Unity Test Framework needs to enter its own temporary scene.
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += EnsureMainSceneConfigured;
            }
        }

        private static bool IsTestBootstrapSceneActive()
        {
            string activeScenePath = SceneManager.GetActiveScene().path;
            return activeScenePath.StartsWith("Assets/InitTestScene", System.StringComparison.Ordinal);
        }

        [MenuItem("Marble Sort/Setup/Use Main Scene When Playing")]
        public static void EnsureMainSceneConfigured()
        {
            SceneAsset mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
            if (mainScene == null)
            {
                Debug.LogWarning($"Cannot configure Play Mode because '{MainScenePath}' is missing.");
                return;
            }

            if (EditorSceneManager.playModeStartScene != mainScene)
            {
                EditorSceneManager.playModeStartScene = mainScene;
                Debug.Log("Marble Sort Play Mode will start from Main.unity.");
            }
        }
    }
}
