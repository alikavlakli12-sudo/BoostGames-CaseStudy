using MarbleSort.Data;
using MarbleSort.Session;
using MarbleSort.Validation;
using UnityEngine;

namespace MarbleSort.Core
{
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string levelCatalogResourcePath = "Levels/levels";

        public LevelCatalogData Catalog { get; private set; }

        public GameSession Session { get; private set; }

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Physics.reuseCollisionCallbacks = true;
            Screen.orientation = ScreenOrientation.Portrait;

            TextAsset levelCatalogAsset = Resources.Load<TextAsset>(levelCatalogResourcePath);
            if (levelCatalogAsset == null)
            {
                Debug.LogError($"Level catalog not found at Resources/{levelCatalogResourcePath}.", this);
                enabled = false;
                return;
            }

            Catalog = LevelCatalogLoader.Parse(levelCatalogAsset.text);
            ValidationReport report = LevelCatalogValidator.Validate(Catalog);
            for (int index = 0; index < report.Issues.Count; index++)
            {
                ValidationIssue issue = report.Issues[index];
                if (issue.Severity == ValidationSeverity.Error)
                {
                    Debug.LogError(issue.ToString(), this);
                }
                else
                {
                    Debug.LogWarning(issue.ToString(), this);
                }
            }

            if (report.HasErrors)
            {
                enabled = false;
                return;
            }

            Session = new GameSession(Catalog.levels.Length);
            Debug.Log($"Loaded {Catalog.levels.Length} validated Marble Sort levels.", this);
        }
    }
}
