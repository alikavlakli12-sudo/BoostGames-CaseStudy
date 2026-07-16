using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MarbleSort.Editor
{
    public static class DeliveryBuildAutomation
    {
        private const string BuildPathEnvironmentVariable = "MARBLE_SORT_BUILD_PATH";

        [MenuItem("Marble Sort/QA/Build Desktop Smoke Player")]
        public static void BuildDesktopSmokePlayerFromMenu()
        {
            string outputPath = BuildDesktopSmokePlayer();
            EditorUtility.DisplayDialog(
                "Marble Sort Build QA",
                $"Desktop smoke player built successfully at:\n{outputPath}",
                "OK");
        }

        public static void BuildDesktopSmokePlayerFromCommandLine()
        {
            BuildDesktopSmokePlayer();
        }

        public static string BuildDesktopSmokePlayer()
        {
            DeliveryReadinessReport readiness = DeliveryReadinessValidator.Validate();
            if (!readiness.IsReady)
            {
                throw new BuildFailedException(
                    $"Delivery validation failed {readiness.FailureCount} check(s); build cancelled.");
            }

            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No enabled scenes are configured for the player build.");
            }

            BuildTarget target = GetDesktopTarget();
            string outputPath = GetOutputPath(target);
            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new BuildFailedException($"Invalid build output path '{outputPath}'.");
            }

            Directory.CreateDirectory(outputDirectory);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Desktop smoke build failed with result {report.summary.result} and " +
                    $"{report.summary.totalErrors} error(s).");
            }

            Debug.Log(
                $"Marble Sort desktop smoke build passed: {outputPath} " +
                $"({report.summary.totalSize} bytes, {report.summary.totalTime}).");
            return outputPath;
        }

        private static string[] GetEnabledScenes()
        {
            EditorBuildSettingsScene[] configuredScenes = EditorBuildSettings.scenes;
            int enabledCount = 0;
            for (int index = 0; index < configuredScenes.Length; index++)
            {
                if (configuredScenes[index].enabled)
                {
                    enabledCount++;
                }
            }

            string[] paths = new string[enabledCount];
            int writeIndex = 0;
            for (int index = 0; index < configuredScenes.Length; index++)
            {
                if (configuredScenes[index].enabled)
                {
                    paths[writeIndex++] = configuredScenes[index].path;
                }
            }

            return paths;
        }

        private static BuildTarget GetDesktopTarget()
        {
#if UNITY_EDITOR_OSX
            return BuildTarget.StandaloneOSX;
#elif UNITY_EDITOR_WIN
            return BuildTarget.StandaloneWindows64;
#else
            return BuildTarget.StandaloneLinux64;
#endif
        }

        private static string GetOutputPath(BuildTarget target)
        {
            string configuredPath = Environment.GetEnvironmentVariable(BuildPathEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.GetFullPath(configuredPath);
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new DirectoryNotFoundException("Could not resolve the Unity project root.");
            }

            string fileName;
            switch (target)
            {
                case BuildTarget.StandaloneOSX:
                    fileName = "Marble Sort.app";
                    break;
                case BuildTarget.StandaloneWindows64:
                    fileName = "Marble Sort.exe";
                    break;
                default:
                    fileName = "MarbleSort";
                    break;
            }

            return Path.Combine(projectRoot, "Builds", "QA", fileName);
        }
    }
}
