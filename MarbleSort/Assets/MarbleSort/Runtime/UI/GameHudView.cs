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
        private GUIStyle subStatusStyle;
        private GUIStyle topButtonStyle;
        private GUIStyle retryButtonStyle;
        private GUIStyle progressValueStyle;
        private GUIStyle progressLabelStyle;
        private GUIStyle hintStyle;
        private GUIStyle levelPanelStyle;
        private GUIStyle overlayPanelStyle;
        private GUIStyle shadowPanelStyle;
        private GUIStyle hintPanelStyle;
        private Texture2D levelPanelTexture;
        private Texture2D overlayPanelTexture;
        private Texture2D buttonTexture;
        private Texture2D shadowTexture;
        private Texture2D hintPanelTexture;
        private string levelName = "Level 1";
        private string progressText = "0/0";
        private string statusTitle = string.Empty;
        private string statusSubtitle = string.Empty;
        private bool overlayVisible;
        private bool retryVisible;
        private bool hintVisible;
        private float overlayAlpha;
        private float hintAlpha;

        public bool OverlayVisible => overlayVisible;

        public bool RetryVisible => retryVisible;

        public bool HintVisible => hintVisible;

        public int CompletedTrayCount { get; private set; }

        public int TotalTrayCount { get; private set; }

        public float LastSafeTopOffset { get; private set; }

        public void Configure(LevelFlowController flow)
        {
            levelFlow = flow;
        }

        public void ShowPlaying(
            string displayName,
            int completedTrayCount,
            int totalTrayCount,
            bool showHint)
        {
            levelName = displayName;
            overlayVisible = false;
            retryVisible = false;
            hintVisible = showHint;
            SetProgress(completedTrayCount, totalTrayCount);
        }

        public void SetProgress(int completedTrayCount, int totalTrayCount)
        {
            TotalTrayCount = Mathf.Max(0, totalTrayCount);
            CompletedTrayCount = Mathf.Clamp(completedTrayCount, 0, TotalTrayCount);
            progressText = $"{CompletedTrayCount}/{TotalTrayCount}";
        }

        public void HideHint()
        {
            hintVisible = false;
        }

        public void ShowComplete(string displayName)
        {
            levelName = displayName;
            statusTitle = "LEVEL COMPLETE!";
            statusSubtitle = "Great sorting — next level starting";
            overlayVisible = true;
            retryVisible = false;
        }

        public void ShowDeadlocked(string displayName)
        {
            levelName = displayName;
            statusTitle = "NO MORE MOVES";
            statusSubtitle = "The conveyor is full. Reset and try a new order.";
            overlayVisible = true;
            retryVisible = true;
        }

        private void Update()
        {
            float target = overlayVisible ? 1f : 0f;
            overlayAlpha = Mathf.MoveTowards(overlayAlpha, target, Time.unscaledDeltaTime * 5.5f);

            float hintTarget = hintVisible && !overlayVisible ? 1f : 0f;
            hintAlpha = Mathf.MoveTowards(hintAlpha, hintTarget, Time.unscaledDeltaTime * 5.5f);
        }

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Mathf.Max(0.01f, Mathf.Min(
                Screen.width / ReferenceWidth,
                Screen.height / ReferenceHeight));
            float horizontalOffset = (Screen.width - (ReferenceWidth * scale)) * 0.5f;
            float verticalOffset = (Screen.height - (ReferenceHeight * scale)) * 0.5f;
            Rect safeArea = Screen.safeArea;
            float safeTopPixels = Screen.height - safeArea.yMax;
            LastSafeTopOffset = Mathf.Max(0f, (safeTopPixels - verticalOffset) / scale);
            float topY = Mathf.Max(22f, LastSafeTopOffset + 14f);

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(horizontalOffset, verticalOffset, 0f),
                Quaternion.identity,
                new Vector3(scale, scale, 1f));

            DrawTopHud(topY);
            if (hintAlpha > 0.001f)
            {
                DrawHint(topY, hintAlpha);
            }

            if (overlayAlpha > 0.001f)
            {
                DrawOverlay(overlayAlpha);
            }

            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void DrawTopHud(float topY)
        {
            Rect retryShadow = new Rect(35f, topY + 6f, 78f, 72f);
            Rect retryRect = new Rect(31f, topY, 78f, 72f);
            GUI.Box(retryShadow, GUIContent.none, shadowPanelStyle);
            if (GUI.Button(retryRect, "↻", topButtonStyle))
            {
                levelFlow?.RetryCurrentLevel();
            }

            Rect levelShadow = new Rect(218f, topY + 7f, 284f, 72f);
            Rect levelRect = new Rect(216f, topY, 284f, 72f);
            GUI.Box(levelShadow, GUIContent.none, shadowPanelStyle);
            GUI.Box(levelRect, GUIContent.none, levelPanelStyle);
            GUI.Label(levelRect, levelName, levelStyle);

            Rect progressShadow = new Rect(576f, topY + 6f, 112f, 72f);
            Rect progressRect = new Rect(572f, topY, 112f, 72f);
            GUI.Box(progressShadow, GUIContent.none, shadowPanelStyle);
            GUI.Box(progressRect, GUIContent.none, levelPanelStyle);
            GUI.Label(new Rect(572f, topY + 5f, 112f, 40f), progressText, progressValueStyle);
            GUI.Label(new Rect(572f, topY + 40f, 112f, 24f), "TRAYS", progressLabelStyle);
        }

        private void DrawHint(float topY, float alpha)
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Rect shadow = new Rect(106f, topY + 96f, 516f, 54f);
            Rect panel = new Rect(102f, topY + 90f, 516f, 54f);
            GUI.Box(shadow, GUIContent.none, shadowPanelStyle);
            GUI.Box(panel, GUIContent.none, hintPanelStyle);
            GUI.Label(panel, "Tap a dotted box  •  Fill matching trays", hintStyle);
            GUI.color = previous;
        }

        private void DrawOverlay(float alpha)
        {
            Color previous = GUI.color;
            Color overlayColor = new Color(0.055f, 0.075f, 0.15f, 0.78f * alpha);
            GUI.color = overlayColor;
            GUI.DrawTexture(
                new Rect(0f, 0f, ReferenceWidth, ReferenceHeight),
                Texture2D.whiteTexture,
                ScaleMode.StretchToFill);

            GUI.color = new Color(1f, 1f, 1f, alpha);
            Rect panelShadow = new Rect(82f, 470f, 564f, 336f);
            Rect panel = new Rect(78f, 458f, 564f, 336f);
            GUI.Box(panelShadow, GUIContent.none, shadowPanelStyle);
            GUI.Box(panel, GUIContent.none, overlayPanelStyle);
            GUI.Label(new Rect(112f, 506f, 496f, 82f), statusTitle, statusStyle);
            GUI.Label(new Rect(126f, 586f, 468f, 70f), statusSubtitle, subStatusStyle);

            if (retryVisible && GUI.Button(
                    new Rect(230f, 680f, 260f, 76f),
                    "RETRY LEVEL",
                    retryButtonStyle))
            {
                levelFlow?.RetryCurrentLevel();
            }

            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (levelStyle != null)
            {
                return;
            }

            levelPanelTexture = CreateRoundedTexture(
                "Level Panel",
                new Color32(156, 118, 232, 255),
                new Color32(204, 178, 255, 255),
                20f,
                4f);
            overlayPanelTexture = CreateRoundedTexture(
                "Overlay Panel",
                new Color32(211, 228, 244, 255),
                new Color32(245, 249, 255, 255),
                24f,
                4f);
            buttonTexture = CreateRoundedTexture(
                "Action Button",
                new Color32(255, 170, 48, 255),
                new Color32(255, 220, 105, 255),
                20f,
                4f);
            shadowTexture = CreateRoundedTexture(
                "Panel Shadow",
                new Color32(41, 54, 102, 205),
                new Color32(41, 54, 102, 205),
                22f,
                0f);
            hintPanelTexture = CreateRoundedTexture(
                "Hint Panel",
                new Color32(76, 94, 149, 235),
                new Color32(146, 169, 226, 255),
                20f,
                3f);

            RectOffset scalableBorder = new RectOffset(22, 22, 22, 22);
            levelPanelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = levelPanelTexture },
                border = scalableBorder
            };
            overlayPanelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = overlayPanelTexture },
                border = scalableBorder
            };
            shadowPanelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = shadowTexture },
                border = scalableBorder
            };
            hintPanelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = hintPanelTexture },
                border = scalableBorder
            };
            levelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color32(43, 55, 96, 255) }
            };
            subStatusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 24,
                wordWrap = true,
                normal = { textColor = new Color32(81, 100, 138, 255) }
            };
            progressValueStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 25,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            progressLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color32(239, 231, 255, 255) }
            };
            hintStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            topButtonStyle = CreateButtonStyle(buttonTexture, 42);
            retryButtonStyle = CreateButtonStyle(buttonTexture, 28);
        }

        private static GUIStyle CreateButtonStyle(Texture2D texture, int fontSize)
        {
            return new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                normal = { background = texture, textColor = Color.white },
                hover = { background = texture, textColor = Color.white },
                active = { background = texture, textColor = new Color32(255, 248, 220, 255) },
                border = new RectOffset(22, 22, 22, 22)
            };
        }

        private static Texture2D CreateRoundedTexture(
            string textureName,
            Color fill,
            Color border,
            float radius,
            float borderWidth)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = textureName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            Vector2 innerHalfSize = new Vector2(center.x - radius, center.y - radius);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 offset = new Vector2(Mathf.Abs(x - center.x), Mathf.Abs(y - center.y));
                    Vector2 corner = new Vector2(
                        Mathf.Max(offset.x - innerHalfSize.x, 0f),
                        Mathf.Max(offset.y - innerHalfSize.y, 0f));
                    float signedDistance = corner.magnitude - radius;
                    float alpha = Mathf.Clamp01(0.75f - signedDistance);
                    Color color = signedDistance > -borderWidth ? border : fill;
                    color.a *= alpha;
                    pixels[(y * size) + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            DestroyTexture(levelPanelTexture);
            DestroyTexture(overlayPanelTexture);
            DestroyTexture(buttonTexture);
            DestroyTexture(shadowTexture);
            DestroyTexture(hintPanelTexture);
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }
    }
}
