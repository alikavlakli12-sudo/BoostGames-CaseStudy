using System;
using System.Collections.Generic;
using System.IO;
using MarbleSort.Core;
using MarbleSort.Data;
using MarbleSort.Gameplay.Conveyor;
using MarbleSort.Gameplay.Flow;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Gameplay.Receivers;
using MarbleSort.Gameplay.TopGrid;
using MarbleSort.Presentation;
using MarbleSort.UI;
using MarbleSort.Validation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MarbleSort.Editor
{
    public sealed class DeliveryCheck
    {
        public DeliveryCheck(string code, string name, bool passed, string detail)
        {
            Code = code;
            Name = name;
            Passed = passed;
            Detail = detail;
        }

        public string Code { get; }

        public string Name { get; }

        public bool Passed { get; }

        public string Detail { get; }

        public override string ToString()
        {
            return $"[{(Passed ? "PASS" : "FAIL")}] {Code} - {Name}: {Detail}";
        }
    }

    public sealed class DeliveryReadinessReport
    {
        private readonly List<DeliveryCheck> checks = new List<DeliveryCheck>();

        public IReadOnlyList<DeliveryCheck> Checks => checks;

        public bool IsReady
        {
            get
            {
                for (int index = 0; index < checks.Count; index++)
                {
                    if (!checks[index].Passed)
                    {
                        return false;
                    }
                }

                return checks.Count > 0;
            }
        }

        public int FailureCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < checks.Count; index++)
                {
                    if (!checks[index].Passed)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        internal void Add(DeliveryCheck check)
        {
            checks.Add(check);
        }
    }

    public static class DeliveryReadinessValidator
    {
        public const string RequiredUnityVersion = "6000.3.10f1";
        public const string MainScenePath = "Assets/MarbleSort/Scenes/Main.unity";
        public const string CatalogPath = "Assets/MarbleSort/Resources/Levels/levels.json";

        private const string MaterialsFolder = "Assets/MarbleSort/Art/Materials";
        private const string LevelsFolder = "Assets/MarbleSort/Resources/Levels";

        [MenuItem("Marble Sort/QA/Validate Delivery")]
        public static void ValidateFromMenu()
        {
            DeliveryReadinessReport report = Validate();
            Log(report);
            EditorUtility.DisplayDialog(
                "Marble Sort Delivery QA",
                report.IsReady
                    ? $"Delivery validation passed all {report.Checks.Count} checks."
                    : $"Delivery validation failed {report.FailureCount} of {report.Checks.Count} checks. See the Console for details.",
                "OK");
        }

        public static void ValidateFromCommandLine()
        {
            DeliveryReadinessReport report = Validate();
            Log(report);
            if (!report.IsReady)
            {
                throw new BuildFailedException(
                    $"Marble Sort delivery validation failed {report.FailureCount} check(s).");
            }
        }

        public static DeliveryReadinessReport Validate()
        {
            DeliveryReadinessReport report = new DeliveryReadinessReport();
            Add(report, "UNITY_VERSION", "Required Unity editor", CheckUnityVersion);
            Add(report, "PORTRAIT_SETTINGS", "Portrait player settings", CheckPortraitSettings);
            Add(report, "BUILD_SCENE", "Enabled build scene", CheckBuildScene);
            Add(report, "CATALOG_SOURCE", "Single JSON level source", CheckCatalogSource);
            Add(report, "CATALOG_VALIDATION", "Production catalog validation", CheckCatalogValidation);
            Add(report, "CATALOG_SOLVABILITY", "Five solvable levels", CheckCatalogSolvability);
            Add(report, "MATERIAL_SHADERS", "Material shader references", CheckMaterials);
            Add(report, "REVIEWER_FILES", "Reviewer documentation", CheckReviewerFiles);
            Add(report, "GITIGNORE", "Unity repository hygiene", CheckGitIgnore);

            SceneAsset mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
            report.Add(new DeliveryCheck(
                "MAIN_SCENE",
                "Main scene asset",
                mainScene != null,
                mainScene == null ? $"Missing {MainScenePath}." : MainScenePath));

            if (mainScene != null)
            {
                AddSceneAuditChecks(report);
            }

            return report;
        }

        private static void Add(
            DeliveryReadinessReport report,
            string code,
            string name,
            Func<CheckOutcome> check)
        {
            try
            {
                CheckOutcome outcome = check();
                report.Add(new DeliveryCheck(code, name, outcome.Passed, outcome.Detail));
            }
            catch (Exception exception)
            {
                report.Add(new DeliveryCheck(code, name, false, exception.Message));
            }
        }

        private static CheckOutcome CheckUnityVersion()
        {
            return Application.unityVersion == RequiredUnityVersion
                ? CheckOutcome.Pass(Application.unityVersion)
                : CheckOutcome.Fail($"Expected {RequiredUnityVersion}, found {Application.unityVersion}.");
        }

        private static CheckOutcome CheckPortraitSettings()
        {
            bool portrait = PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait;
            bool portraitResolution = PlayerSettings.defaultScreenHeight > PlayerSettings.defaultScreenWidth;
            bool windowed = PlayerSettings.fullScreenMode == FullScreenMode.Windowed;
            if (!portrait || !portraitResolution || !windowed)
            {
                return CheckOutcome.Fail(
                    $"Orientation={PlayerSettings.defaultInterfaceOrientation}, default resolution=" +
                    $"{PlayerSettings.defaultScreenWidth}x{PlayerSettings.defaultScreenHeight}, " +
                    $"window mode={PlayerSettings.fullScreenMode}.");
            }

            return CheckOutcome.Pass(
                $"Portrait, {PlayerSettings.defaultScreenWidth}x{PlayerSettings.defaultScreenHeight}, windowed.");
        }

        private static CheckOutcome CheckBuildScene()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            int enabledCount = 0;
            bool mainEnabled = false;
            for (int index = 0; index < scenes.Length; index++)
            {
                if (!scenes[index].enabled)
                {
                    continue;
                }

                enabledCount++;
                mainEnabled |= string.Equals(
                    scenes[index].path,
                    MainScenePath,
                    StringComparison.Ordinal);
            }

            return enabledCount == 1 && mainEnabled
                ? CheckOutcome.Pass(MainScenePath)
                : CheckOutcome.Fail(
                    $"Expected only {MainScenePath} to be enabled; found {enabledCount} enabled scene(s).");
        }

        private static CheckOutcome CheckCatalogSource()
        {
            TextAsset catalogAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
            string[] assetGuids = AssetDatabase.FindAssets(string.Empty, new[] { LevelsFolder });
            int jsonCount = 0;
            for (int index = 0; index < assetGuids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[index]);
                if (string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                {
                    jsonCount++;
                }
            }

            return catalogAsset != null && jsonCount == 1
                ? CheckOutcome.Pass(CatalogPath)
                : CheckOutcome.Fail(
                    $"Expected one JSON catalog at {CatalogPath}; asset present={catalogAsset != null}, JSON count={jsonCount}.");
        }

        private static CheckOutcome CheckCatalogValidation()
        {
            LevelCatalogData catalog = LoadCatalog();
            ValidationReport validation = LevelCatalogValidator.Validate(catalog);
            if (!validation.HasErrors)
            {
                return CheckOutcome.Pass($"{catalog.levels.Length} levels, 0 blocking issues.");
            }

            List<string> errors = new List<string>();
            for (int index = 0; index < validation.Issues.Count; index++)
            {
                if (validation.Issues[index].Severity == ValidationSeverity.Error)
                {
                    errors.Add(validation.Issues[index].ToString());
                }
            }

            return CheckOutcome.Fail(string.Join("; ", errors));
        }

        private static CheckOutcome CheckCatalogSolvability()
        {
            LevelCatalogData catalog = LoadCatalog();
            if (catalog.levels == null || catalog.levels.Length != 5)
            {
                return CheckOutcome.Fail(
                    $"Expected 5 levels, found {catalog.levels?.Length ?? 0}.");
            }

            int totalExploredStates = 0;
            for (int index = 0; index < catalog.levels.Length; index++)
            {
                LevelSolvabilityResult result = LevelSolvabilityAnalyzer.Analyze(
                    catalog.levels[index],
                    catalog.conveyor.slotCount);
                if (!result.IsSolvable)
                {
                    return CheckOutcome.Fail(
                        $"{catalog.levels[index].displayName}: {result.Message}");
                }

                totalExploredStates += result.ExploredStateCount;
            }

            return CheckOutcome.Pass(
                $"5/5 levels solved; {totalExploredStates} deterministic states explored.");
        }

        private static CheckOutcome CheckMaterials()
        {
            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
            List<string> invalidMaterials = new List<string>();
            for (int index = 0; index < materialGuids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(materialGuids[index]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == null ||
                    material.shader.name == "Hidden/InternalErrorShader")
                {
                    invalidMaterials.Add(path);
                }
            }

            return materialGuids.Length > 0 && invalidMaterials.Count == 0
                ? CheckOutcome.Pass($"{materialGuids.Length} materials have valid shaders.")
                : CheckOutcome.Fail(
                    invalidMaterials.Count == 0
                        ? "No project materials were found."
                        : string.Join(", ", invalidMaterials));
        }

        private static CheckOutcome CheckReviewerFiles()
        {
            string repositoryRoot = GetRepositoryRoot();
            string[] requiredRelativePaths =
            {
                "README.md",
                "Docs/Architecture.md",
                "Docs/LevelAuthoring.md",
                "Docs/PresentationAndPerformance.md",
                "Docs/DeliveryQA.md",
                "MarbleSort/Packages/manifest.json",
                "MarbleSort/ProjectSettings/ProjectVersion.txt"
            };

            List<string> missing = new List<string>();
            for (int index = 0; index < requiredRelativePaths.Length; index++)
            {
                if (!File.Exists(Path.Combine(repositoryRoot, requiredRelativePaths[index])))
                {
                    missing.Add(requiredRelativePaths[index]);
                }
            }

            return missing.Count == 0
                ? CheckOutcome.Pass($"{requiredRelativePaths.Length} required files are present.")
                : CheckOutcome.Fail($"Missing: {string.Join(", ", missing)}");
        }

        private static CheckOutcome CheckGitIgnore()
        {
            string path = Path.Combine(GetRepositoryRoot(), ".gitignore");
            if (!File.Exists(path))
            {
                return CheckOutcome.Fail("Missing repository .gitignore.");
            }

            string contents = File.ReadAllText(path);
            string[] requiredPatterns =
            {
                "[Ll]ibrary/",
                "[Tt]emp/",
                "[Bb]uilds/",
                "[Ll]ogs/",
                "[Uu]ser[Ss]ettings/",
                ".DS_Store"
            };
            List<string> missing = new List<string>();
            for (int index = 0; index < requiredPatterns.Length; index++)
            {
                if (!contents.Contains(requiredPatterns[index]))
                {
                    missing.Add(requiredPatterns[index]);
                }
            }

            return missing.Count == 0
                ? CheckOutcome.Pass("Unity caches, local builds, logs, user settings, and OS files are ignored.")
                : CheckOutcome.Fail($"Missing patterns: {string.Join(", ", missing)}");
        }

        private static void AddSceneAuditChecks(DeliveryReadinessReport report)
        {
            Scene scene = SceneManager.GetSceneByPath(MainScenePath);
            bool closeAfterAudit = !scene.IsValid() || !scene.isLoaded;
            try
            {
                if (closeAfterAudit)
                {
                    scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
                }

                GameObject[] roots = scene.GetRootGameObjects();
                List<string> invalidCounts = new List<string>();
                RequireExactlyOne<GameBootstrap>(roots, invalidCounts);
                RequireExactlyOne<TopGridController>(roots, invalidCounts);
                RequireExactlyOne<MarblePool>(roots, invalidCounts);
                RequireExactlyOne<StadiumConveyorController>(roots, invalidCounts);
                RequireExactlyOne<ConveyorAdmissionController>(roots, invalidCounts);
                RequireExactlyOne<ReceiverQueueController>(roots, invalidCounts);
                RequireExactlyOne<LevelFlowController>(roots, invalidCounts);
                RequireExactlyOne<GameHudView>(roots, invalidCounts);
                RequireExactlyOne<GameFeedbackController>(roots, invalidCounts);
                RequireExactlyOne<ResponsiveCameraController>(roots, invalidCounts);
                RequireExactlyOne<RuntimePerformanceProbe>(roots, invalidCounts);
                RequireExactlyOne<AudioListener>(roots, invalidCounts);

                report.Add(new DeliveryCheck(
                    "SCENE_COMPONENTS",
                    "Required runtime systems",
                    invalidCounts.Count == 0,
                    invalidCounts.Count == 0
                        ? "All 12 required systems occur exactly once."
                        : string.Join(", ", invalidCounts)));

                int missingScriptCount = 0;
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                    for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                    {
                        Component[] components = transforms[transformIndex].GetComponents<Component>();
                        for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                        {
                            if (components[componentIndex] == null)
                            {
                                missingScriptCount++;
                            }
                        }
                    }
                }

                report.Add(new DeliveryCheck(
                    "SCENE_REFERENCES",
                    "Missing scene scripts",
                    missingScriptCount == 0,
                    missingScriptCount == 0
                        ? "No missing MonoBehaviour references."
                        : $"Found {missingScriptCount} missing script reference(s)."));
            }
            catch (Exception exception)
            {
                report.Add(new DeliveryCheck(
                    "SCENE_COMPONENTS",
                    "Required runtime systems",
                    false,
                    exception.Message));
                report.Add(new DeliveryCheck(
                    "SCENE_REFERENCES",
                    "Missing scene scripts",
                    false,
                    exception.Message));
            }
            finally
            {
                if (closeAfterAudit && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void RequireExactlyOne<T>(GameObject[] roots, List<string> invalidCounts)
            where T : Component
        {
            int count = 0;
            for (int index = 0; index < roots.Length; index++)
            {
                count += roots[index].GetComponentsInChildren<T>(true).Length;
            }

            if (count != 1)
            {
                invalidCounts.Add($"{typeof(T).Name}={count}");
            }
        }

        private static LevelCatalogData LoadCatalog()
        {
            TextAsset catalogAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
            if (catalogAsset == null)
            {
                throw new FileNotFoundException($"Missing catalog at {CatalogPath}.");
            }

            return LevelCatalogLoader.Parse(catalogAsset.text);
        }

        private static string GetRepositoryRoot()
        {
            DirectoryInfo projectRoot = Directory.GetParent(Application.dataPath);
            DirectoryInfo repositoryRoot = projectRoot?.Parent;
            if (repositoryRoot == null)
            {
                throw new DirectoryNotFoundException("Could not resolve the repository root.");
            }

            return repositoryRoot.FullName;
        }

        private static void Log(DeliveryReadinessReport report)
        {
            for (int index = 0; index < report.Checks.Count; index++)
            {
                DeliveryCheck check = report.Checks[index];
                if (check.Passed)
                {
                    Debug.Log(check.ToString());
                }
                else
                {
                    Debug.LogError(check.ToString());
                }
            }

            Debug.Log(
                report.IsReady
                    ? $"Marble Sort delivery QA passed all {report.Checks.Count} checks."
                    : $"Marble Sort delivery QA failed {report.FailureCount} of {report.Checks.Count} checks.");
        }

        private readonly struct CheckOutcome
        {
            private CheckOutcome(bool passed, string detail)
            {
                Passed = passed;
                Detail = detail;
            }

            public bool Passed { get; }

            public string Detail { get; }

            public static CheckOutcome Pass(string detail)
            {
                return new CheckOutcome(true, detail);
            }

            public static CheckOutcome Fail(string detail)
            {
                return new CheckOutcome(false, detail);
            }
        }
    }
}
