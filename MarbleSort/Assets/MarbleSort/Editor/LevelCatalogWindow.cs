using MarbleSort.Data;
using MarbleSort.Validation;
using UnityEditor;
using UnityEngine;

namespace MarbleSort.Editor
{
    public sealed class LevelCatalogWindow : EditorWindow
    {
        private const string DefaultCatalogPath = "Assets/MarbleSort/Resources/Levels/levels.json";

        private TextAsset catalogAsset;
        private LevelCatalogData catalog;
        private ValidationReport report;
        private Vector2 scrollPosition;

        [MenuItem("Marble Sort/Level Catalog")]
        public static void Open()
        {
            GetWindow<LevelCatalogWindow>("Marble Sort Levels");
        }

        private void OnEnable()
        {
            if (catalogAsset == null)
            {
                catalogAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultCatalogPath);
            }

            RefreshValidation();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("JSON Level Catalog", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();
            catalogAsset = (TextAsset)EditorGUILayout.ObjectField("Catalog", catalogAsset, typeof(TextAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshValidation();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate"))
                {
                    RefreshValidation();
                }

                using (new EditorGUI.DisabledScope(catalogAsset == null))
                {
                    if (GUILayout.Button("Select JSON"))
                    {
                        Selection.activeObject = catalogAsset;
                        EditorGUIUtility.PingObject(catalogAsset);
                    }
                }
            }

            EditorGUILayout.Space(8f);
            DrawSummary();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawIssues();
            EditorGUILayout.EndScrollView();
        }

        private void DrawSummary()
        {
            if (catalogAsset == null)
            {
                EditorGUILayout.HelpBox("Assign the single JSON level catalog.", MessageType.Warning);
                return;
            }

            if (catalog == null || report == null)
            {
                EditorGUILayout.HelpBox("The catalog could not be parsed.", MessageType.Error);
                return;
            }

            int levelCount = catalog.levels == null ? 0 : catalog.levels.Length;
            EditorGUILayout.LabelField("Levels", levelCount.ToString());
            EditorGUILayout.LabelField("Conveyor Slots", catalog.conveyor == null ? "Missing" : catalog.conveyor.slotCount.ToString());
            EditorGUILayout.LabelField("Validation Issues", report.Issues.Count.ToString());
            EditorGUILayout.Space(6f);

            if (report.Issues.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Catalog is valid. Every color satisfies the required 1 top box to 3 receiver boxes ratio.",
                    MessageType.Info);
            }
        }

        private void DrawIssues()
        {
            if (report == null)
            {
                return;
            }

            for (int index = 0; index < report.Issues.Count; index++)
            {
                ValidationIssue issue = report.Issues[index];
                MessageType messageType = issue.Severity == ValidationSeverity.Error
                    ? MessageType.Error
                    : MessageType.Warning;
                EditorGUILayout.HelpBox($"{issue.Code}\n{issue.Context}\n{issue.Message}", messageType);
            }
        }

        private void RefreshValidation()
        {
            catalog = null;
            report = null;

            if (catalogAsset == null)
            {
                Repaint();
                return;
            }

            try
            {
                catalog = LevelCatalogLoader.Parse(catalogAsset.text);
                report = LevelCatalogValidator.Validate(catalog);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }

            Repaint();
        }
    }
}
