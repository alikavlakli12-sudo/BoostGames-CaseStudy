using System;
using System.Collections.Generic;
using System.IO;
using MarbleSort.Data;
using MarbleSort.Validation;
using UnityEditor;
using UnityEngine;

namespace MarbleSort.Editor
{
    public sealed class LevelCatalogWindow : EditorWindow
    {
        private const string DefaultCatalogPath = "Assets/MarbleSort/Resources/Levels/levels.json";
        private const string MainScenePath = "Assets/MarbleSort/Scenes/Main.unity";

        [SerializeField] private TextAsset catalogAsset;
        [SerializeField] private int selectedLevelIndex;
        [SerializeField] private bool showAuthoring = true;
        [SerializeField] private bool showAllIssues;

        private LevelCatalogEditingBuffer editingBuffer;
        private SerializedObject serializedBuffer;
        private LevelCatalogData catalog;
        private ValidationReport report;
        private LevelSolvabilityResult[] solvabilityResults = Array.Empty<LevelSolvabilityResult>();
        private Vector2 scrollPosition;
        private bool draftDirty;
        private string statusMessage = string.Empty;
        private MessageType statusMessageType = MessageType.Info;

        [MenuItem("Marble Sort/Level Catalog")]
        public static void Open()
        {
            LevelCatalogWindow window = GetWindow<LevelCatalogWindow>("Marble Sort Levels");
            window.minSize = new Vector2(680f, 640f);
            window.Show();
        }

        private void OnEnable()
        {
            minSize = new Vector2(680f, 640f);
            if (catalogAsset == null)
            {
                catalogAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultCatalogPath);
            }

            ReloadFromJson();
        }

        private void OnDisable()
        {
            DestroyEditingBuffer();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Marble Sort Production Level Editor", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Author, validate, solve, and preview the JSON level catalog without editing Main.unity.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(6f);

            DrawCatalogAssetField();
            DrawCatalogToolbar();

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusMessageType);
            }

            if (catalog == null || report == null || serializedBuffer == null)
            {
                EditorGUILayout.HelpBox(
                    catalogAsset == null
                        ? "Assign the single JSON level catalog."
                        : "The catalog could not be parsed. Check the Console for the parsing exception.",
                    MessageType.Error);
                return;
            }

            serializedBuffer.UpdateIfRequiredOrScript();
            DrawCatalogSummary();
            DrawLevelSelector();
            DrawPreviewToolbar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawSelectedLevelSummary();
            DrawAuthoringInspector();
            DrawIssues();
            EditorGUILayout.EndScrollView();
        }

        private void DrawCatalogAssetField()
        {
            EditorGUI.BeginChangeCheck();
            TextAsset selected = (TextAsset)EditorGUILayout.ObjectField(
                "Catalog JSON",
                catalogAsset,
                typeof(TextAsset),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                catalogAsset = selected;
                ReloadFromJson();
            }
        }

        private void DrawCatalogToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(catalogAsset == null))
                {
                    if (GUILayout.Button("Reload JSON"))
                    {
                        ReloadFromJson();
                    }

                    if (GUILayout.Button("Select JSON"))
                    {
                        Selection.activeObject = catalogAsset;
                        EditorGUIUtility.PingObject(catalogAsset);
                    }
                }

                using (new EditorGUI.DisabledScope(catalog == null))
                {
                    if (GUILayout.Button("Validate Draft"))
                    {
                        ApplyDraftAndValidate();
                        SetStatus(
                            report.HasErrors
                                ? "Draft validation found blocking errors."
                                : "All five draft levels are structurally valid and solvable.",
                            report.HasErrors ? MessageType.Error : MessageType.Info);
                    }

                    using (new EditorGUI.DisabledScope(report == null || report.HasErrors))
                    {
                        if (GUILayout.Button(draftDirty ? "Save Valid JSON *" : "Save Valid JSON"))
                        {
                            SaveJson();
                        }
                    }
                }
            }
        }

        private void DrawCatalogSummary()
        {
            int errorCount = CountIssues(ValidationSeverity.Error);
            int warningCount = CountIssues(ValidationSeverity.Warning);
            int levelCount = catalog.levels?.Length ?? 0;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Catalog Status", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Levels: {levelCount}", GUILayout.Width(90f));
                EditorGUILayout.LabelField(
                    $"Slots: {(catalog.conveyor == null ? "Missing" : catalog.conveyor.slotCount.ToString())}",
                    GUILayout.Width(90f));
                EditorGUILayout.LabelField($"Errors: {errorCount}", GUILayout.Width(90f));
                EditorGUILayout.LabelField($"Warnings: {warningCount}", GUILayout.Width(100f));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(draftDirty ? "Unsaved draft" : "Saved JSON", EditorStyles.miniBoldLabel);
            }

            if (!report.HasErrors)
            {
                EditorGUILayout.HelpBox(
                    "Catalog is production-valid: exactly five levels, four lanes per level, 24 conveyor slots, correct color capacity, and at least one safe solution per level.",
                    MessageType.Info);
            }
        }

        private void DrawLevelSelector()
        {
            LevelData[] levels = catalog.levels ?? Array.Empty<LevelData>();
            if (levels.Length == 0)
            {
                return;
            }

            selectedLevelIndex = Mathf.Clamp(selectedLevelIndex, 0, levels.Length - 1);
            string[] options = new string[levels.Length];
            for (int index = 0; index < levels.Length; index++)
            {
                LevelData level = levels[index];
                options[index] = level == null
                    ? $"{index + 1}. Missing level"
                    : $"{index + 1}. {level.displayName} ({level.id})";
            }

            EditorGUILayout.Space(6f);
            selectedLevelIndex = EditorGUILayout.Popup("Selected Level", selectedLevelIndex, options);
        }

        private void DrawPreviewToolbar()
        {
            LevelData[] levels = catalog.levels ?? Array.Empty<LevelData>();
            bool canPreview = levels.Length > 0 && !report.HasErrors;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!canPreview || selectedLevelIndex <= 0))
                {
                    if (GUILayout.Button("Previous"))
                    {
                        SelectAndMaybePreview(selectedLevelIndex - 1);
                    }
                }

                using (new EditorGUI.DisabledScope(!canPreview))
                {
                    if (GUILayout.Button(EditorApplication.isPlaying ? "Load Selected Level" : "Preview Selected Level"))
                    {
                        RequestPreview();
                    }
                }

                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                {
                    if (GUILayout.Button("Reload Current Level"))
                    {
                        if (LevelPreviewPlayModeLauncher.ReloadCurrentLevel(out string error))
                        {
                            SetStatus("Reloaded the current level from a clean runtime state.", MessageType.Info);
                        }
                        else
                        {
                            SetStatus(error, MessageType.Warning);
                        }
                    }
                }

                using (new EditorGUI.DisabledScope(!canPreview || selectedLevelIndex >= levels.Length - 1))
                {
                    if (GUILayout.Button("Next"))
                    {
                        SelectAndMaybePreview(selectedLevelIndex + 1);
                    }
                }

                if (GUILayout.Button("Ping Main", GUILayout.Width(80f)))
                {
                    SceneAsset mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
                    Selection.activeObject = mainScene;
                    EditorGUIUtility.PingObject(mainScene);
                }
            }
        }

        private void DrawSelectedLevelSummary()
        {
            LevelData level = GetSelectedLevel();
            if (level == null)
            {
                EditorGUILayout.HelpBox("The selected level entry is null.", MessageType.Error);
                return;
            }

            int topBoxCount = level.topGrid?.boxes?.Length ?? 0;
            int receiverCount = 0;
            HashSet<string> colors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            TopBoxData[] topBoxes = level.topGrid?.boxes ?? Array.Empty<TopBoxData>();
            for (int index = 0; index < topBoxes.Length; index++)
            {
                if (topBoxes[index] != null && !string.IsNullOrWhiteSpace(topBoxes[index].color))
                {
                    colors.Add(topBoxes[index].color.Trim().ToLowerInvariant());
                }
            }

            ReceiverLaneData[] lanes = level.receiverLanes ?? Array.Empty<ReceiverLaneData>();
            for (int index = 0; index < lanes.Length; index++)
            {
                receiverCount += lanes[index]?.boxes?.Length ?? 0;
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Selected Level Analysis", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Display Name", level.displayName);
                EditorGUILayout.LabelField("Stable ID", level.id);
                EditorGUILayout.LabelField("Top Boxes", topBoxCount.ToString());
                EditorGUILayout.LabelField("Receiver Boxes", receiverCount.ToString());
                EditorGUILayout.LabelField("Receiver Lanes", lanes.Length.ToString());
                EditorGUILayout.LabelField("Colors", colors.Count == 0 ? "None" : string.Join(", ", colors));

                LevelSolvabilityResult solvability = GetSelectedSolvability();
                if (solvability != null)
                {
                    EditorGUILayout.LabelField("Solver", solvability.Message);
                    if (solvability.IsSolvable)
                    {
                        EditorGUILayout.LabelField("Explored States", solvability.ExploredStateCount.ToString());
                        EditorGUILayout.LabelField("Verified Selection Sequence");
                        EditorGUILayout.SelectableLabel(
                            string.Join("  →  ", solvability.SelectionSequence),
                            EditorStyles.textArea,
                            GUILayout.MinHeight(42f));
                    }
                }
            }
        }

        private void DrawAuthoringInspector()
        {
            showAuthoring = EditorGUILayout.Foldout(showAuthoring, "Level Authoring", true, EditorStyles.foldoutHeader);
            if (!showAuthoring)
            {
                return;
            }

            SerializedProperty dataProperty = serializedBuffer.FindProperty("data");
            SerializedProperty conveyorProperty = dataProperty?.FindPropertyRelative("conveyor");
            SerializedProperty levelsProperty = dataProperty?.FindPropertyRelative("levels");
            if (levelsProperty == null || selectedLevelIndex < 0 || selectedLevelIndex >= levelsProperty.arraySize)
            {
                EditorGUILayout.HelpBox("The serialized level draft is unavailable.", MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                "Edit the selected level using Unity's serialized array controls. Changes remain in memory until Save Valid JSON is pressed; Preview Selected Level saves a valid draft automatically.",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            if (conveyorProperty != null)
            {
                EditorGUILayout.PropertyField(conveyorProperty, true);
            }

            SerializedProperty selectedLevel = levelsProperty.GetArrayElementAtIndex(selectedLevelIndex);
            EditorGUILayout.PropertyField(selectedLevel, true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedBuffer.ApplyModifiedProperties();
                catalog = editingBuffer.data;
                draftDirty = true;
                ValidateDraft();
            }
        }

        private void DrawIssues()
        {
            if (report == null)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            showAllIssues = EditorGUILayout.Foldout(
                showAllIssues,
                $"Validation Diagnostics ({report.Issues.Count})",
                true,
                EditorStyles.foldoutHeader);
            if (!showAllIssues && report.Issues.Count == 0)
            {
                return;
            }

            LevelData selectedLevel = GetSelectedLevel();
            string selectedContext = selectedLevel?.id ?? string.Empty;
            int drawnCount = 0;
            for (int index = 0; index < report.Issues.Count; index++)
            {
                ValidationIssue issue = report.Issues[index];
                bool globalIssue = issue.Context.StartsWith("catalog", StringComparison.OrdinalIgnoreCase);
                bool selectedIssue = !string.IsNullOrWhiteSpace(selectedContext) &&
                                     issue.Context.StartsWith(selectedContext, StringComparison.OrdinalIgnoreCase);
                if (!showAllIssues && !globalIssue && !selectedIssue)
                {
                    continue;
                }

                MessageType messageType = issue.Severity == ValidationSeverity.Error
                    ? MessageType.Error
                    : MessageType.Warning;
                EditorGUILayout.HelpBox(
                    $"{issue.Code}\n{issue.Context}\n{issue.Message}",
                    messageType);
                drawnCount++;
            }

            if (drawnCount == 0)
            {
                EditorGUILayout.HelpBox("No validation issues for the selected level.", MessageType.Info);
            }
        }

        private void ReloadFromJson()
        {
            statusMessage = string.Empty;
            catalog = null;
            report = null;
            solvabilityResults = Array.Empty<LevelSolvabilityResult>();
            draftDirty = false;
            DestroyEditingBuffer();

            if (catalogAsset == null)
            {
                Repaint();
                return;
            }

            try
            {
                string assetPath = AssetDatabase.GetAssetPath(catalogAsset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                catalogAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                catalog = LevelCatalogLoader.Parse(catalogAsset.text);
                editingBuffer = CreateInstance<LevelCatalogEditingBuffer>();
                editingBuffer.hideFlags = HideFlags.HideAndDontSave;
                editingBuffer.data = catalog;
                serializedBuffer = new SerializedObject(editingBuffer);
                selectedLevelIndex = Mathf.Clamp(
                    selectedLevelIndex,
                    0,
                    Mathf.Max(0, (catalog.levels?.Length ?? 1) - 1));
                ValidateDraft();
                SetStatus("Reloaded the authoring draft from the JSON asset.", MessageType.Info);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus(exception.Message, MessageType.Error);
            }

            Repaint();
        }

        private void ApplyDraftAndValidate()
        {
            serializedBuffer?.ApplyModifiedProperties();
            if (editingBuffer != null)
            {
                catalog = editingBuffer.data;
            }

            ValidateDraft();
        }

        private void ValidateDraft()
        {
            if (catalog == null)
            {
                report = null;
                solvabilityResults = Array.Empty<LevelSolvabilityResult>();
                return;
            }

            report = LevelCatalogValidator.Validate(catalog);
            LevelData[] levels = catalog.levels ?? Array.Empty<LevelData>();
            solvabilityResults = new LevelSolvabilityResult[levels.Length];
            int capacity = catalog.conveyor?.slotCount ?? 0;
            if (capacity > 0)
            {
                for (int index = 0; index < levels.Length; index++)
                {
                    solvabilityResults[index] = LevelSolvabilityAnalyzer.Analyze(levels[index], capacity);
                }
            }

            Repaint();
        }

        private bool SaveJson()
        {
            ApplyDraftAndValidate();
            if (report == null || report.HasErrors)
            {
                SetStatus("Fix all blocking validation errors before saving the production catalog.", MessageType.Error);
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(catalogAsset);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                SetStatus("The catalog must be a project asset before it can be saved.", MessageType.Error);
                return false;
            }

            try
            {
                string json = JsonUtility.ToJson(catalog, true) + Environment.NewLine;
                File.WriteAllText(assetPath, json);
                AssetDatabase.ImportAsset(
                    assetPath,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                catalogAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                draftDirty = false;
                ReloadFromJson();
                SetStatus("Saved the validated production catalog to JSON.", MessageType.Info);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus($"Could not save the catalog: {exception.Message}", MessageType.Error);
                return false;
            }
        }

        private void RequestPreview()
        {
            ApplyDraftAndValidate();
            if (report.HasErrors)
            {
                SetStatus("Preview is blocked until all catalog errors are fixed.", MessageType.Error);
                return;
            }

            if (draftDirty && !SaveJson())
            {
                return;
            }

            if (LevelPreviewPlayModeLauncher.PreviewLevel(selectedLevelIndex, out string error))
            {
                LevelData selected = GetSelectedLevel();
                SetStatus($"Previewing {selected?.displayName ?? $"level {selectedLevelIndex + 1}"}.", MessageType.Info);
            }
            else
            {
                SetStatus(error, MessageType.Error);
            }
        }

        private void SelectAndMaybePreview(int newIndex)
        {
            LevelData[] levels = catalog.levels ?? Array.Empty<LevelData>();
            selectedLevelIndex = Mathf.Clamp(newIndex, 0, Mathf.Max(0, levels.Length - 1));
            if (EditorApplication.isPlaying)
            {
                RequestPreview();
            }
        }

        private LevelData GetSelectedLevel()
        {
            LevelData[] levels = catalog?.levels ?? Array.Empty<LevelData>();
            return selectedLevelIndex >= 0 && selectedLevelIndex < levels.Length
                ? levels[selectedLevelIndex]
                : null;
        }

        private LevelSolvabilityResult GetSelectedSolvability()
        {
            return selectedLevelIndex >= 0 && selectedLevelIndex < solvabilityResults.Length
                ? solvabilityResults[selectedLevelIndex]
                : null;
        }

        private int CountIssues(ValidationSeverity severity)
        {
            int count = 0;
            if (report == null)
            {
                return count;
            }

            for (int index = 0; index < report.Issues.Count; index++)
            {
                if (report.Issues[index].Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusMessageType = type;
            Repaint();
        }

        private void DestroyEditingBuffer()
        {
            serializedBuffer = null;
            if (editingBuffer != null)
            {
                DestroyImmediate(editingBuffer);
                editingBuffer = null;
            }
        }
    }

    internal sealed class LevelCatalogEditingBuffer : ScriptableObject
    {
        [SerializeField] public LevelCatalogData data = new LevelCatalogData();
    }
}
