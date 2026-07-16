using MarbleSort.Gameplay.Flow;
using UnityEngine;

namespace MarbleSort.UI
{
    [DisallowMultipleComponent]
    public sealed class GameHudView : MonoBehaviour
    {
        private const float ReferenceWidth = 720f;
        private const float ReferenceHeight = 1280f;

        [SerializeField] private LevelFlowController levelFlow;

        private GUIStyle levelStyle;
        private GUIStyle statusStyle;
        private GUIStyle buttonStyle;
        private string levelName = "Level 1";
        private string statusMessage = string.Empty;
        private bool overlayVisible;
        private bool retryVisible;

        public void Configure(LevelFlowController flow)
        {
            levelFlow = flow;
        }

        public void ShowPlaying(string displayName)
        {
            levelName = displayName;
            overlayVisible = false;
            retryVisible = false;
        }

        public void ShowComplete(string displayName)
        {
            levelName = displayName;
            statusMessage = "LEVEL COMPLETE!\nNext level starting...";
            overlayVisible = true;
            retryVisible = false;
        }

        public void ShowDeadlocked(string displayName)
        {
            levelName = displayName;
            statusMessage = "NO MORE MOVES";
            overlayVisible = true;
            retryVisible = true;
        }

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            float horizontalOffset = (Screen.width - (ReferenceWidth * scale)) * 0.5f;
            float verticalOffset = (Screen.height - (ReferenceHeight * scale)) * 0.5f;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(horizontalOffset, verticalOffset, 0f),
                Quaternion.identity,
                new Vector3(scale, scale, 1f));

            GUI.color = new Color32(157, 119, 229, 255);
            GUI.Box(new Rect(230f, 20f, 260f, 64f), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(230f, 20f, 260f, 64f), levelName, levelStyle);

            if (overlayVisible)
            {
                GUI.color = new Color(0.08f, 0.11f, 0.2f, 0.78f);
                GUI.Box(new Rect(0f, 0f, ReferenceWidth, ReferenceHeight), GUIContent.none);
                GUI.color = new Color32(182, 205, 225, 255);
                GUI.Box(new Rect(100f, 490f, 520f, 300f), GUIContent.none);
                GUI.color = new Color32(42, 55, 91, 255);
                GUI.Label(new Rect(130f, 525f, 460f, 130f), statusMessage, statusStyle);

                if (retryVisible)
                {
                    GUI.color = new Color32(255, 174, 48, 255);
                    if (GUI.Button(new Rect(250f, 675f, 220f, 70f), "RETRY", buttonStyle))
                    {
                        levelFlow?.RetryCurrentLevel();
                    }
                }
            }

            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void EnsureStyles()
        {
            if (levelStyle != null)
            {
                return;
            }

            levelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 38,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color32(42, 55, 91, 255) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                hover = { textColor = Color.white },
                active = { textColor = Color.white }
            };
        }
    }
}
