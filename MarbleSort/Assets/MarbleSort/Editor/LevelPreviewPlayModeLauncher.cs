using MarbleSort.Gameplay.Flow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MarbleSort.Editor
{
    [InitializeOnLoad]
    public static class LevelPreviewPlayModeLauncher
    {
        private const string MainScenePath = "Assets/MarbleSort/Scenes/Main.unity";
        private const string PendingLevelKey = "MarbleSort.PendingPreviewLevel";
        private const int NoPendingLevel = -1;

        static LevelPreviewPlayModeLauncher()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            if (EditorApplication.isPlaying && GetPendingLevelIndex() >= 0)
            {
                EditorApplication.update += TryApplyPendingPreview;
            }
        }

        public static bool PreviewLevel(int levelIndex, out string error)
        {
            if (levelIndex < 0)
            {
                error = "Select a valid level before entering preview mode.";
                return false;
            }

            SceneAsset mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
            if (mainScene == null)
            {
                error = $"Main scene is missing at '{MainScenePath}'.";
                return false;
            }

            SessionState.SetInt(PendingLevelKey, levelIndex);
            EditorSceneManager.playModeStartScene = mainScene;
            EditorApplication.update -= TryApplyPendingPreview;
            EditorApplication.update += TryApplyPendingPreview;

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
            }

            error = string.Empty;
            return true;
        }

        public static bool ReloadCurrentLevel(out string error)
        {
            if (!EditorApplication.isPlaying)
            {
                error = "Enter Play Mode before reloading the current level.";
                return false;
            }

            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            if (flow == null || !flow.IsInitialized)
            {
                error = "The Marble Sort level flow is not initialized yet.";
                return false;
            }

            flow.RetryCurrentLevel();
            error = string.Empty;
            return true;
        }

        [MenuItem("Marble Sort/Preview/Reload Current Level", true)]
        private static bool ValidateReloadCurrentLevel()
        {
            return EditorApplication.isPlaying;
        }

        [MenuItem("Marble Sort/Preview/Reload Current Level")]
        private static void ReloadCurrentLevelMenu()
        {
            if (!ReloadCurrentLevel(out string error))
            {
                Debug.LogWarning(error);
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.update -= TryApplyPendingPreview;
                EditorApplication.update += TryApplyPendingPreview;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                ClearPendingPreview();
            }
        }

        private static void TryApplyPendingPreview()
        {
            int levelIndex = GetPendingLevelIndex();
            if (!EditorApplication.isPlaying || levelIndex < 0)
            {
                EditorApplication.update -= TryApplyPendingPreview;
                return;
            }

            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            if (flow == null || !flow.IsInitialized)
            {
                return;
            }

            if (!flow.TryLoadLevel(levelIndex))
            {
                Debug.LogError($"Could not preview Marble Sort level index {levelIndex}.");
            }

            ClearPendingPreview();
        }

        private static int GetPendingLevelIndex()
        {
            return SessionState.GetInt(PendingLevelKey, NoPendingLevel);
        }

        private static void ClearPendingPreview()
        {
            SessionState.EraseInt(PendingLevelKey);
            EditorApplication.update -= TryApplyPendingPreview;
        }
    }
}
