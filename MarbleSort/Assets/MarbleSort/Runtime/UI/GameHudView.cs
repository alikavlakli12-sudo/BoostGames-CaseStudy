using MarbleSort.Gameplay.Flow;
using UnityEngine;

namespace MarbleSort.UI
{
    [DisallowMultipleComponent]
    public sealed class GameHudView : MonoBehaviour
    {
        private const float ReferenceWidth = 720f;
        private const float ReferenceHeight = 1280f;
        private const float HudPlateWidth = 853f;
        private const float HudPlateHeight = 377f;
        private const int DefaultCoinBalance = 575;

        private static readonly int[] UnlockLevels = { 6, 8, 11 };

        [SerializeField] private LevelFlowController levelFlow;

        private GUIStyle levelStyle;
        private GUIStyle walletStyle;
        private GUIStyle unlockStyle;
        private GUIStyle statusStyle;
        private GUIStyle subStatusStyle;
        private GUIStyle actionButtonStyle;
        private GUIStyle iconButtonStyle;
        private GUIStyle hintStyle;
        private GUIStyle panelStyle;
        private GUIStyle overlayPanelStyle;
        private GUIStyle shadowPanelStyle;
        private GUIStyle hintPanelStyle;
        private GUIStyle unlockPanelStyle;
        private GUIStyle settingsLabelStyle;
        private GUIStyle toggleButtonStyle;
        private GUIStyle toggleOffButtonStyle;
        private GUIStyle transparentButtonStyle;

        private Texture2D lavenderPanelTexture;
        private Texture2D overlayPanelTexture;
        private Texture2D goldButtonTexture;
        private Texture2D shadowTexture;
        private Texture2D hintPanelTexture;
        private Texture2D unlockPanelTexture;
        private Texture2D toggleOnTexture;
        private Texture2D toggleOffTexture;
        private Texture2D gearTexture;
        private Texture2D lockTexture;
        private Texture2D coinTexture;
        private Texture2D premiumTopHudPlate;

        private string levelName = "Level 1";
        private string statusTitle = string.Empty;
        private string statusSubtitle = string.Empty;
        private bool overlayVisible;
        private bool retryVisible;
        private bool hintVisible;
        private bool settingsVisible;
        private bool coinPanelVisible;
        private bool hapticsEnabled = true;
        private float overlayAlpha;
        private float hintAlpha;
        private float previousTimeScale = 1f;

        public bool OverlayVisible => overlayVisible;

        public bool RetryVisible => retryVisible;

        public bool HintVisible => hintVisible;

        public bool SettingsVisible => settingsVisible;

        public bool SettingsButtonVisible => true;

        public bool TrayCounterVisible => false;

        public int CoinBalance => DefaultCoinBalance;

        public int UnlockCardCount => UnlockLevels.Length;

        public bool PremiumHudArtworkLoaded => premiumTopHudPlate != null;

        public int CompletedTrayCount { get; private set; }

        public int TotalTrayCount { get; private set; }

        public float LastSafeTopOffset { get; private set; }

        private void Awake()
        {
            premiumTopHudPlate = Resources.Load<Texture2D>("Presentation/UI/PremiumTopHudPlate");
        }

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
            SetSettingsVisible(false);
            coinPanelVisible = false;
            levelName = displayName;
            overlayVisible = false;
            retryVisible = false;
            hintVisible = showHint;
            if (!showHint)
            {
                hintAlpha = 0f;
            }
            SetProgress(completedTrayCount, totalTrayCount);
        }

        public void SetProgress(int completedTrayCount, int totalTrayCount)
        {
            TotalTrayCount = Mathf.Max(0, totalTrayCount);
            CompletedTrayCount = Mathf.Clamp(completedTrayCount, 0, TotalTrayCount);
        }

        public void HideHint()
        {
            hintVisible = false;
        }

        public void ShowComplete(string displayName)
        {
            SetSettingsVisible(false);
            coinPanelVisible = false;
            levelName = displayName;
            statusTitle = "LEVEL COMPLETE!";
            statusSubtitle = "Great sorting — next level starting";
            overlayVisible = true;
            retryVisible = false;
        }

        public void ShowDeadlocked(string displayName)
        {
            SetSettingsVisible(false);
            coinPanelVisible = false;
            levelName = displayName;
            statusTitle = "NO MORE MOVES";
            statusSubtitle = "The conveyor is full. Reset and try a new order.";
            overlayVisible = true;
            retryVisible = true;
        }

        public void ToggleSettings()
        {
            SetSettingsVisible(!settingsVisible);
        }

        public void SetSettingsVisible(bool visible)
        {
            if (settingsVisible == visible)
            {
                return;
            }

            settingsVisible = visible;
            if (visible)
            {
                coinPanelVisible = false;
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else if (Mathf.Approximately(Time.timeScale, 0f))
            {
                Time.timeScale = previousTimeScale;
            }
        }

        private void Update()
        {
            float target = overlayVisible ? 1f : 0f;
            overlayAlpha = Mathf.MoveTowards(overlayAlpha, target, Time.unscaledDeltaTime * 5.5f);

            float hintTarget = hintVisible && !overlayVisible && !settingsVisible ? 1f : 0f;
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

            DrawPremiumTopHud();

            GUI.matrix = Matrix4x4.TRS(
                new Vector3(horizontalOffset, verticalOffset, 0f),
                Quaternion.identity,
                new Vector3(scale, scale, 1f));

            if (hintAlpha > 0.001f)
            {
                DrawHint(topY, hintAlpha);
            }

            if (coinPanelVisible && !settingsVisible && overlayAlpha <= 0.001f)
            {
                DrawCoinPanel();
            }

            if (settingsVisible)
            {
                DrawSettingsPanel();
            }
            else if (overlayAlpha > 0.001f)
            {
                DrawGameplayOverlay(overlayAlpha);
            }

            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void DrawPremiumTopHud()
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = Mathf.Max(0.01f, Screen.width / HudPlateWidth);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            if (premiumTopHudPlate != null)
            {
                GUI.DrawTexture(
                    new Rect(0f, 0f, HudPlateWidth, HudPlateHeight),
                    premiumTopHudPlate,
                    ScaleMode.StretchToFill,
                    true);
            }

            if (GUI.Button(new Rect(29f, 77f, 111f, 112f), GUIContent.none, transparentButtonStyle))
            {
                ToggleSettings();
            }

            DrawOutlinedLabel(new Rect(243f, 80f, 307f, 102f), levelName, levelStyle, 2.5f);

            if (GUI.Button(new Rect(741f, 85f, 91f, 108f), GUIContent.none, transparentButtonStyle))
            {
                coinPanelVisible = !coinPanelVisible;
            }

            GUI.matrix = previousMatrix;
        }

        private void DrawHint(float topY, float alpha)
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Rect shadow = new Rect(130f, topY + 208f, 468f, 48f);
            Rect panel = new Rect(126f, topY + 202f, 468f, 48f);
            GUI.Box(shadow, GUIContent.none, shadowPanelStyle);
            GUI.Box(panel, GUIContent.none, hintPanelStyle);
            GUI.Label(panel, "Tap a dotted box  •  Fill matching trays", hintStyle);
            GUI.color = previous;
        }

        private void DrawCoinPanel()
        {
            DrawDimmer(0.36f);
            Rect shadow = new Rect(122f, 447f, 484f, 298f);
            Rect panel = new Rect(118f, 437f, 484f, 298f);
            GUI.Box(shadow, GUIContent.none, shadowPanelStyle);
            GUI.Box(panel, GUIContent.none, overlayPanelStyle);
            DrawOutlinedLabel(new Rect(154f, 475f, 412f, 62f), "COIN SHOP", statusStyle, 2f);
            GUI.DrawTexture(new Rect(306f, 545f, 108f, 108f), coinTexture, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(150f, 652f, 420f, 40f), "Prototype preview • 575 coins", subStatusStyle);
            if (GUI.Button(new Rect(254f, 704f, 212f, 64f), "CLOSE", actionButtonStyle))
            {
                coinPanelVisible = false;
            }
        }

        private void DrawSettingsPanel()
        {
            DrawDimmer(0.68f);
            Rect shadow = new Rect(91f, 314f, 546f, 590f);
            Rect panel = new Rect(87f, 302f, 546f, 590f);
            GUI.Box(shadow, GUIContent.none, shadowPanelStyle);
            GUI.Box(panel, GUIContent.none, overlayPanelStyle);
            DrawOutlinedLabel(new Rect(142f, 340f, 436f, 72f), "SETTINGS", statusStyle, 2f);

            GUI.Label(new Rect(152f, 449f, 250f, 54f), "SOUND", settingsLabelStyle);
            bool soundEnabled = AudioListener.volume > 0.001f;
            if (GUI.Button(
                    new Rect(442f, 442f, 112f, 58f),
                    soundEnabled ? "ON" : "OFF",
                    soundEnabled ? toggleButtonStyle : toggleOffButtonStyle))
            {
                AudioListener.volume = soundEnabled ? 0f : 1f;
            }

            GUI.Label(new Rect(152f, 535f, 250f, 54f), "HAPTICS", settingsLabelStyle);
            if (GUI.Button(
                    new Rect(442f, 528f, 112f, 58f),
                    hapticsEnabled ? "ON" : "OFF",
                    hapticsEnabled ? toggleButtonStyle : toggleOffButtonStyle))
            {
                hapticsEnabled = !hapticsEnabled;
            }

            if (GUI.Button(new Rect(178f, 650f, 364f, 72f), "RESTART LEVEL", actionButtonStyle))
            {
                SetSettingsVisible(false);
                levelFlow?.RetryCurrentLevel();
            }

            if (GUI.Button(new Rect(226f, 750f, 268f, 68f), "CONTINUE", iconButtonStyle))
            {
                SetSettingsVisible(false);
            }
        }

        private void DrawGameplayOverlay(float alpha)
        {
            DrawDimmer(0.78f * alpha);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Rect panelShadow = new Rect(82f, 470f, 564f, 336f);
            Rect panel = new Rect(78f, 458f, 564f, 336f);
            GUI.Box(panelShadow, GUIContent.none, shadowPanelStyle);
            GUI.Box(panel, GUIContent.none, overlayPanelStyle);
            DrawOutlinedLabel(new Rect(112f, 506f, 496f, 82f), statusTitle, statusStyle, 2f);
            GUI.Label(new Rect(126f, 586f, 468f, 70f), statusSubtitle, subStatusStyle);

            if (retryVisible && GUI.Button(
                    new Rect(230f, 680f, 260f, 76f),
                    "RETRY LEVEL",
                    actionButtonStyle))
            {
                levelFlow?.RetryCurrentLevel();
            }

            GUI.color = Color.white;
        }

        private static void DrawDimmer(float alpha)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.035f, 0.05f, 0.13f, alpha);
            GUI.DrawTexture(
                new Rect(0f, 0f, ReferenceWidth, ReferenceHeight),
                Texture2D.whiteTexture,
                ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        private static void DrawOutlinedLabel(Rect rect, string text, GUIStyle style, float offset)
        {
            Color original = style.normal.textColor;
            style.normal.textColor = new Color32(38, 42, 82, 255);
            GUI.Label(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, style);
            style.normal.textColor = original;
            GUI.Label(rect, text, style);
        }

        private void EnsureStyles()
        {
            if (levelStyle != null)
            {
                return;
            }

            lavenderPanelTexture = CreateRoundedGradientTexture(
                "Premium Lavender Panel",
                new Color32(210, 183, 255, 255),
                new Color32(159, 116, 232, 255),
                new Color32(229, 211, 255, 255),
                20f,
                4f);
            overlayPanelTexture = CreateRoundedGradientTexture(
                "Premium Overlay Panel",
                new Color32(248, 251, 255, 255),
                new Color32(211, 228, 246, 255),
                new Color32(255, 255, 255, 255),
                25f,
                4f);
            goldButtonTexture = CreateRoundedGradientTexture(
                "Premium Gold Button",
                new Color32(255, 229, 112, 255),
                new Color32(255, 164, 41, 255),
                new Color32(255, 243, 169, 255),
                20f,
                4f);
            shadowTexture = CreateRoundedGradientTexture(
                "Premium Panel Shadow",
                new Color32(42, 56, 117, 205),
                new Color32(31, 43, 96, 225),
                new Color32(31, 43, 96, 225),
                22f,
                0f);
            hintPanelTexture = CreateRoundedGradientTexture(
                "Premium Hint Panel",
                new Color32(113, 141, 213, 245),
                new Color32(66, 87, 154, 245),
                new Color32(181, 204, 251, 255),
                18f,
                3f);
            unlockPanelTexture = CreateRoundedGradientTexture(
                "Premium Unlock Panel",
                new Color32(214, 229, 232, 255),
                new Color32(163, 187, 194, 255),
                new Color32(226, 239, 240, 255),
                20f,
                4f);
            toggleOnTexture = CreateRoundedGradientTexture(
                "Toggle On",
                new Color32(111, 235, 117, 255),
                new Color32(52, 188, 80, 255),
                new Color32(194, 255, 196, 255),
                20f,
                3f);
            toggleOffTexture = CreateRoundedGradientTexture(
                "Toggle Off",
                new Color32(189, 197, 218, 255),
                new Color32(125, 136, 172, 255),
                new Color32(221, 227, 240, 255),
                20f,
                3f);

            gearTexture = CreateOutlinedIconTexture("Settings Gear", IsGearPixel);
            lockTexture = CreateOutlinedIconTexture("Level Lock", IsLockPixel);
            coinTexture = CreateCoinTexture();
            if (premiumTopHudPlate == null)
            {
                premiumTopHudPlate = Resources.Load<Texture2D>("Presentation/UI/PremiumTopHudPlate");
            }

            RectOffset scalableBorder = new RectOffset(22, 22, 22, 22);
            panelStyle = CreatePanelStyle(lavenderPanelTexture, scalableBorder);
            overlayPanelStyle = CreatePanelStyle(overlayPanelTexture, scalableBorder);
            shadowPanelStyle = CreatePanelStyle(shadowTexture, scalableBorder);
            hintPanelStyle = CreatePanelStyle(hintPanelTexture, scalableBorder);
            unlockPanelStyle = CreatePanelStyle(unlockPanelTexture, scalableBorder);

            levelStyle = CreateTextStyle(44, Color.white, TextAnchor.MiddleCenter);
            walletStyle = CreateTextStyle(31, Color.white, TextAnchor.MiddleCenter);
            unlockStyle = CreateTextStyle(20, Color.white, TextAnchor.MiddleCenter);
            statusStyle = CreateTextStyle(41, new Color32(65, 72, 129, 255), TextAnchor.MiddleCenter);
            statusStyle.wordWrap = true;
            subStatusStyle = CreateTextStyle(23, new Color32(82, 104, 151, 255), TextAnchor.UpperCenter);
            subStatusStyle.wordWrap = true;
            hintStyle = CreateTextStyle(20, Color.white, TextAnchor.MiddleCenter);
            settingsLabelStyle = CreateTextStyle(26, new Color32(65, 77, 134, 255), TextAnchor.MiddleLeft);

            actionButtonStyle = CreateButtonStyle(goldButtonTexture, 27, Color.white);
            iconButtonStyle = CreateButtonStyle(lavenderPanelTexture, 25, Color.white);
            toggleButtonStyle = CreateButtonStyle(toggleOnTexture, 20, Color.white);
            toggleOffButtonStyle = CreateButtonStyle(toggleOffTexture, 20, Color.white);
            transparentButtonStyle = new GUIStyle(GUIStyle.none);
        }

        private static GUIStyle CreatePanelStyle(Texture2D texture, RectOffset border)
        {
            return new GUIStyle(GUI.skin.box)
            {
                normal = { background = texture },
                border = border
            };
        }

        private static GUIStyle CreateTextStyle(int fontSize, Color color, TextAnchor alignment)
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = alignment,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = color }
            };
        }

        private static GUIStyle CreateButtonStyle(Texture2D texture, int fontSize, Color textColor)
        {
            return new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                normal = { background = texture, textColor = textColor },
                hover = { background = texture, textColor = textColor },
                active = { background = texture, textColor = new Color32(255, 248, 220, 255) },
                border = new RectOffset(22, 22, 22, 22)
            };
        }

        private static Texture2D CreateRoundedGradientTexture(
            string textureName,
            Color top,
            Color bottom,
            Color border,
            float radius,
            float borderWidth)
        {
            const int size = 64;
            Texture2D texture = CreateTransientTexture(textureName, size);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            Vector2 innerHalfSize = new Vector2(center.x - radius, center.y - radius);
            for (int y = 0; y < size; y++)
            {
                Color fill = Color.Lerp(bottom, top, y / (size - 1f));
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

        private static Texture2D CreateOutlinedIconTexture(string name, System.Func<float, float, bool> shape)
        {
            const int size = 64;
            Texture2D texture = CreateTransientTexture(name, size);
            Color[] pixels = new Color[size * size];
            Color main = Color.white;
            Color outline = new Color32(50, 43, 91, 255);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = ((x + 0.5f) / size) - 0.5f;
                    float ny = ((y + 0.5f) / size) - 0.5f;
                    bool inside = shape(nx, ny);
                    bool near = false;
                    if (!inside)
                    {
                        const float d = 2.2f / size;
                        near = shape(nx + d, ny) || shape(nx - d, ny) ||
                               shape(nx, ny + d) || shape(nx, ny - d) ||
                               shape(nx + d, ny + d) || shape(nx - d, ny - d);
                    }

                    pixels[(y * size) + x] = inside ? main : near ? outline : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static bool IsGearPixel(float x, float y)
        {
            float radius = Mathf.Sqrt((x * x) + (y * y));
            float angle = Mathf.Atan2(y, x);
            float tooth = Mathf.Cos(angle * 8f);
            float outer = tooth > 0.25f ? 0.39f : 0.325f;
            bool ring = radius <= outer && radius >= 0.13f;
            bool hub = radius <= 0.19f && radius >= 0.12f;
            return ring || hub;
        }

        private static bool IsLockPixel(float x, float y)
        {
            bool body = Mathf.Abs(x) <= 0.29f && y >= -0.34f && y <= 0.12f;
            float shackleRadius = Mathf.Sqrt((x * x) + ((y - 0.12f) * (y - 0.12f)));
            bool shackle = shackleRadius >= 0.205f && shackleRadius <= 0.29f && y >= 0.08f;
            bool keyhole = Mathf.Sqrt((x * x) + ((y + 0.08f) * (y + 0.08f))) < 0.075f ||
                           (Mathf.Abs(x) < 0.035f && y > -0.23f && y < -0.08f);
            return (body || shackle) && !keyhole;
        }

        private static Texture2D CreateCoinTexture()
        {
            const int size = 64;
            Texture2D texture = CreateTransientTexture("Coin Icon", size);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = ((x + 0.5f) / size) - 0.5f;
                    float ny = ((y + 0.5f) / size) - 0.5f;
                    float radius = Mathf.Sqrt((nx * nx) + (ny * ny));
                    Color color = Color.clear;
                    if (radius <= 0.42f)
                    {
                        if (radius > 0.35f)
                        {
                            color = new Color32(224, 126, 25, 255);
                        }
                        else if (radius > 0.28f)
                        {
                            color = new Color32(255, 196, 48, 255);
                        }
                        else
                        {
                            float light = Mathf.Clamp01(0.58f + ((ny - nx) * 0.45f));
                            color = Color.Lerp(new Color32(255, 175, 35, 255), new Color32(255, 242, 125, 255), light);
                        }
                    }

                    pixels[(y * size) + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateTransientTexture(string name, int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void OnDisable()
        {
            if (settingsVisible)
            {
                settingsVisible = false;
                if (Mathf.Approximately(Time.timeScale, 0f))
                {
                    Time.timeScale = previousTimeScale;
                }
            }
        }

        private void OnDestroy()
        {
            DestroyTexture(lavenderPanelTexture);
            DestroyTexture(overlayPanelTexture);
            DestroyTexture(goldButtonTexture);
            DestroyTexture(shadowTexture);
            DestroyTexture(hintPanelTexture);
            DestroyTexture(unlockPanelTexture);
            DestroyTexture(toggleOnTexture);
            DestroyTexture(toggleOffTexture);
            DestroyTexture(gearTexture);
            DestroyTexture(lockTexture);
            DestroyTexture(coinTexture);
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
