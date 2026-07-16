using System.Collections.Generic;
using System.IO;
using MarbleSort.Editor;
using NUnit.Framework;
using UnityEngine;

namespace MarbleSort.Tests.EditMode
{
    public sealed class DeliveryReadinessTests
    {
        [Test]
        public void DeliveryReadiness_ReportHasNoFailures()
        {
            DeliveryReadinessReport report = DeliveryReadinessValidator.Validate();
            List<string> failures = new List<string>();
            for (int index = 0; index < report.Checks.Count; index++)
            {
                if (!report.Checks[index].Passed)
                {
                    failures.Add(report.Checks[index].ToString());
                }
            }

            Assert.That(report.IsReady, Is.True, string.Join("\n", failures));
        }

        [Test]
        public void DeliveryReadiness_CoversEverySubmissionBoundary()
        {
            DeliveryReadinessReport report = DeliveryReadinessValidator.Validate();
            HashSet<string> actualCodes = new HashSet<string>();
            for (int index = 0; index < report.Checks.Count; index++)
            {
                actualCodes.Add(report.Checks[index].Code);
            }

            string[] expectedCodes =
            {
                "UNITY_VERSION",
                "PORTRAIT_SETTINGS",
                "BUILD_SCENE",
                "CATALOG_SOURCE",
                "CATALOG_VALIDATION",
                "CATALOG_SOLVABILITY",
                "MATERIAL_SHADERS",
                "REVIEWER_FILES",
                "GITIGNORE",
                "MAIN_SCENE",
                "SCENE_COMPONENTS",
                "SCENE_REFERENCES"
            };
            Assert.That(actualCodes, Is.EquivalentTo(expectedCodes));
        }

        [Test]
        public void RepositoryGitIgnore_ExcludesGeneratedUnityState()
        {
            string gitIgnore = File.ReadAllText(Path.Combine(GetRepositoryRoot(), ".gitignore"));

            Assert.That(gitIgnore, Does.Contain("[Ll]ibrary/"));
            Assert.That(gitIgnore, Does.Contain("[Tt]emp/"));
            Assert.That(gitIgnore, Does.Contain("[Bb]uilds/"));
            Assert.That(gitIgnore, Does.Contain("[Uu]ser[Ss]ettings/"));
            Assert.That(gitIgnore, Does.Contain(".DS_Store"));
        }

        [Test]
        public void Readme_ContainsReviewerSetupTestingAndLimitations()
        {
            string readme = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "README.md"));

            Assert.That(readme, Does.Contain("## Quick start"));
            Assert.That(readme, Does.Contain("## Level editor"));
            Assert.That(readme, Does.Contain("## Testing and delivery QA"));
            Assert.That(readme, Does.Contain("## Known limitations"));
        }

        private static string GetRepositoryRoot()
        {
            DirectoryInfo projectRoot = Directory.GetParent(Application.dataPath);
            Assert.That(projectRoot, Is.Not.Null);
            Assert.That(projectRoot.Parent, Is.Not.Null);
            return projectRoot.Parent.FullName;
        }
    }
}
