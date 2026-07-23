using MarbleSort.Gameplay.Conveyor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MarbleSort.Editor
{
    /// <summary>
    /// Permanently removes the obsolete procedural conveyor artwork while keeping
    /// the 24 empty slot transforms used by gameplay and marble anchoring.
    /// </summary>
    public static class ConveyorSceneVisualCleanup
    {
        private const string MainScenePath = "Assets/MarbleSort/Scenes/Main.unity";

        [MenuItem("Marble Sort/Setup/Remove Obsolete Conveyor Visuals")]
        public static void CleanFromMenu()
        {
            CleanMainScene();
            EditorUtility.DisplayDialog(
                "Conveyor Visual Cleanup",
                "Removed every obsolete conveyor MeshRenderer and MeshFilter.",
                "OK");
        }

        public static void CleanFromCommandLine()
        {
            CleanMainScene();
        }

        private static void CleanMainScene()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            StadiumConveyorController conveyor = Object.FindFirstObjectByType<StadiumConveyorController>();
            if (conveyor == null)
            {
                throw new System.InvalidOperationException("The Stadium Conveyor is missing from Main.unity.");
            }

            string[] obsoleteTrackObjects =
            {
                "Track Shadow",
                "Track Rim",
                "Track Surface",
                "Track Highlight"
            };
            for (int index = 0; index < obsoleteTrackObjects.Length; index++)
            {
                Transform obsolete = conveyor.transform.Find(obsoleteTrackObjects[index]);
                if (obsolete != null)
                {
                    Object.DestroyImmediate(obsolete.gameObject);
                }
            }

            MeshRenderer[] renderers = conveyor.GetComponentsInChildren<MeshRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Object.DestroyImmediate(renderers[index]);
            }

            MeshFilter[] filters = conveyor.GetComponentsInChildren<MeshFilter>(true);
            for (int index = 0; index < filters.Length; index++)
            {
                Object.DestroyImmediate(filters[index]);
            }

            for (int index = 0; index < conveyor.ConfiguredSlotViewCount; index++)
            {
                Transform slot = conveyor.GetSlotView(index);
                if (slot != null)
                {
                    slot.localScale = Vector3.one;
                }
            }

            SerializedObject serializedConveyor = new SerializedObject(conveyor);
            serializedConveyor.FindProperty("turnRadius").floatValue = 0.51f;
            serializedConveyor.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"Conveyor cleanup complete: {conveyor.ConfiguredSlotViewCount} empty mechanical slot anchors, " +
                $"{conveyor.GetComponentsInChildren<MeshRenderer>(true).Length} MeshRenderers, and " +
                $"{conveyor.GetComponentsInChildren<MeshFilter>(true).Length} MeshFilters remain.");
        }
    }
}
