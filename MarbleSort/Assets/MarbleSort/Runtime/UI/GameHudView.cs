using MarbleSort.Gameplay.Flow;
using MarbleSort.Gameplay.Conveyor;
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
        private const float HudScaleFactor = 0.88f;
        private const float HudVisualTop = 48f;
        private const float HudSafePadding = 14f;
        private const int DefaultCoinBalance = 0;
        private const string PremiumTopHudResourcePath =
            "Presentation/UI/Approved/PremiumTopHudPlateAqua";
        private const string CompletionCardResourcePrefix =
            "Presentation/UI/Completion/MarbleStarCompletion";
        private const string MysteryBoxCardResourcePath =
            "Presentation/UI/Completion/MysteryBoxCompletion";
        private const string LossCardResourcePath =
            "Presentation/UI/Completion/ConveyorFullLossCard";
        private const int CompletionStageCount = 5;
        private const float CompletionFillAnimationDuration = 1.15f;
        private const float CompletionCardMaxWidth = 600f;
        private const float CompletionCardMaxHeight = 932f;
        private const float CompletionCardCenterY = 644f;
        private const float CompletionTrackNormalizedX = 0.132f;
        private const float CompletionTrackNormalizedY = 0.602f;
        private const float CompletionTrackNormalizedWidth = 0.736f;
        private const float CompletionTrackNormalizedHeight = 0.069f;
        private const float ButtonPressScale = 0.965f;
        private const float ButtonReleaseBounceScale = 0.045f;
        private const float ButtonReleaseDuration = 0.24f;
        private const float ContinueActionDelay = 0.14f;

        private static readonly Rect SettingsButtonHitRect =
            new Rect(27f, 48f, 130f, 136f);
        private static readonly Rect SettingsButtonVisualRect =
            new Rect(20f, 42f, 144f, 150f);
        private static readonly Rect ContinueButtonNormalizedVisualRect =
            new Rect(0.16f, 0.81f, 0.68f, 0.13f);
        private static readonly Rect MysteryContinueButtonNormalizedVisualRect =
            new Rect(0.175f, 0.815f, 0.65f, 0.14f);
        private static readonly Rect MysteryContinueButtonNormalizedHitRect =
            new Rect(0.20f, 0.835f, 0.60f, 0.095f);
        private static readonly Rect LossSnapshotNormalizedRect =
            new Rect(0.11f, 0.305f, 0.78f, 0.325f);
        private static readonly Rect LossRetryButtonNormalizedVisualRect =
            new Rect(0.16f, 0.82f, 0.68f, 0.125f);
        private static readonly Rect LossRetryButtonNormalizedHitRect =
            new Rect(0.19f, 0.84f, 0.62f, 0.085f);

        [SerializeField] private LevelFlowController levelFlow;

        private GUIStyle levelStyle;
        private GUIStyle walletStyle;
        private GUIStyle unlockStyle;
        private GUIStyle statusStyle;
        private GUIStyle subStatusStyle;
        private GUIStyle actionButtonStyle;
        private GUIStyle iconButtonStyle;
        private GUIStyle panelStyle;
        private GUIStyle overlayPanelStyle;
        private GUIStyle shadowPanelStyle;
        private GUIStyle unlockPanelStyle;
        private GUIStyle settingsLabelStyle;
        private GUIStyle completionPercentStyle;
        private GUIStyle toggleButtonStyle;
        private GUIStyle toggleOffButtonStyle;
        private GUIStyle transparentButtonStyle;

        private Texture2D lavenderPanelTexture;
        private Texture2D overlayPanelTexture;
        private Texture2D goldButtonTexture;
        private Texture2D shadowTexture;
        private Texture2D unlockPanelTexture;
        private Texture2D toggleOnTexture;
        private Texture2D toggleOffTexture;
        private Texture2D gearTexture;
        private Texture2D lockTexture;
        private Texture2D completionTrackTexture;
        private Texture2D completionFillTexture;
        private Texture2D premiumTopHudPlate;
        private Texture2D mysteryBoxCard;
        private Texture2D lossCard;
        private Texture2D lossConveyorSnapshot;
        private readonly Texture2D[] completionCards = new Texture2D[CompletionStageCount];

        private string levelName = "Level 1";
        private string statusTitle = string.Empty;
        private string statusSubtitle = string.Empty;
        private bool overlayVisible;
        private bool retryVisible;
        private bool completionVisible;
        private bool mysteryBoxVisible;
        private bool showMysteryBoxAfterContinue;
        private bool settingsVisible;
        private bool hapticsEnabled = true;
        private int coinBalance = DefaultCoinBalance;
        private float overlayAlpha;
        private float previousTimeScale = 1f;
        private float completionAnimationElapsed;
        private float completionStartPercent;
        private float completionTargetPercent;
        private float completionDisplayedPercent;
        private bool settingsButtonHeld;
        private bool continueButtonHeld;
        private bool continueActionPending;
        private float settingsButtonPressAmount;
        private float continueButtonPressAmount;
        private float settingsButtonReleaseTime;
        private float continueButtonReleaseTime;
        private float continueActionTimer;

        public bool OverlayVisible => overlayVisible;

        public bool RetryVisible => retryVisible;

        public bool HintVisible => false;

        public bool SettingsVisible => settingsVisible;

        public bool SettingsButtonVisible => true;

        public bool TrayCounterVisible => false;

        public int CoinBalance => coinBalance;

        public int UnlockCardCount => 0;

        public bool LevelIndicatorInteractive => false;

        public bool PremiumHudArtworkLoaded => premiumTopHudPlate != null;

        public bool CompletionArtworkLoaded
        {
            get
            {
                for (int index = 0; index < completionCards.Length; index++)
                {
                    if (completionCards[index] == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool CompletionVisible => completionVisible;

        public bool MysteryBoxVisible => mysteryBoxVisible;

        public bool MysteryBoxArtworkLoaded => mysteryBoxCard != null;

        public bool LossArtworkLoaded => lossCard != null;

        public bool LossSnapshotAvailable => lossConveyorSnapshot != null;

        public int CompletionRewardStage { get; private set; }

        public int CompletionRewardPercent => CompletionRewardStage * 20;

        public float CompletionDisplayedPercent => completionDisplayedPercent;

        public int CompletionCoinReward { get; private set; }

        public int CompletedTrayCount { get; private set; }

        public int TotalTrayCount { get; private set; }

        public float LastHudScale { get; private set; }

        public float LastHudTopOffset { get; private set; }

        public float LastHudVisualTop => LastHudTopOffset + (HudVisualTop * LastHudScale);

        private void Awake()
        {
            premiumTopHudPlate = Resources.Load<Texture2D>(PremiumTopHudResourcePath);
            mysteryBoxCard = Resources.Load<Texture2D>(MysteryBoxCardResourcePath);
            lossCard = Resources.Load<Texture2D>(LossCardResourcePath);
            for (int index = 0; index < completionCards.Length; index++)
            {
                int percent = (index + 1) * 20;
                completionCards[index] = Resources.Load<Texture2D>(
                    CompletionCardResourcePrefix + percent);
            }
        }

        public void Configure(LevelFlowController flow)
        {
            levelFlow = flow;
        }

        public void ShowPlaying(
            string displayName,
            int completedTrayCount,
            int totalTrayCount)
        {
            SetSettingsVisible(false);
            levelName = displayName;
            overlayVisible = false;
            retryVisible = false;
            completionVisible = false;
            mysteryBoxVisible = false;
            showMysteryBoxAfterContinue = false;
            ResetContinueButtonState();
            ReleaseLossSnapshot();
            SetProgress(completedTrayCount, totalTrayCount);
        }

        public void SetProgress(int completedTrayCount, int totalTrayCount)
        {
            TotalTrayCount = Mathf.Max(0, totalTrayCount);
            CompletedTrayCount = Mathf.Clamp(completedTrayCount, 0, TotalTrayCount);
        }

        public void SetCoinBalance(int balance)
        {
            coinBalance = Mathf.Max(0, balance);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            coinBalance = coinBalance > int.MaxValue - amount
                ? int.MaxValue
                : coinBalance + amount;
        }

        public void ShowComplete(string displayName)
        {
            ShowComplete(displayName, 2, LevelFlowController.CompletionCoinReward);
        }

        public void ShowComplete(string displayName, int rewardStage, int coinReward)
        {
            ShowComplete(displayName, rewardStage, coinReward, false);
        }

        public void ShowComplete(
            string displayName,
            int rewardStage,
            int coinReward,
            bool showMysteryAfterContinue)
        {
            SetSettingsVisible(false);
            levelName = displayName;
            float previousPercent = CompletionRewardPercent;
            CompletionRewardStage = Mathf.Clamp(rewardStage, 1, CompletionStageCount);
            completionTargetPercent = CompletionRewardPercent;
            completionStartPercent = previousPercent >= completionTargetPercent
                ? 0f
                : previousPercent;
            completionDisplayedPercent = completionStartPercent;
            completionAnimationElapsed = 0f;
            CompletionCoinReward = Mathf.Max(0, coinReward);
            continueActionPending = false;
            continueButtonHeld = false;
            continueButtonPressAmount = 0f;
            continueButtonReleaseTime = 0f;
            overlayVisible = true;
            retryVisible = false;
            completionVisible = true;
            mysteryBoxVisible = false;
            showMysteryBoxAfterContinue = showMysteryAfterContinue;
        }

        public void ShowDeadlocked(string displayName)
        {
            ShowDeadlocked(displayName, null);
        }

        public void ShowDeadlocked(
            string displayName,
            StadiumConveyorController conveyorSource)
        {
            SetSettingsVisible(false);
            levelName = displayName;
            statusTitle = "NO MORE MOVES";
            statusSubtitle = "The conveyor is full. Reset and try a new order.";
            CaptureLossConveyorSnapshot(conveyorSource);
            ResetContinueButtonState();
            overlayVisible = true;
            retryVisible = true;
            completionVisible = false;
            mysteryBoxVisible = false;
            showMysteryBoxAfterContinue = false;
        }

        public bool ContinueAfterCompletion()
        {
            if (levelFlow == null)
            {
                return false;
            }

            if (mysteryBoxVisible)
            {
                overlayVisible = false;
                mysteryBoxVisible = false;
                levelFlow.AdvanceToNextLevel();
                return true;
            }

            if (!completionVisible)
            {
                return false;
            }

            if (showMysteryBoxAfterContinue)
            {
                completionVisible = false;
                mysteryBoxVisible = true;
                showMysteryBoxAfterContinue = false;
                AddCoins(LevelFlowController.MysteryBoxCoinReward);
                ResetContinueButtonState();
                return true;
            }

            overlayVisible = false;
            completionVisible = false;
            levelFlow.AdvanceToNextLevel();
            return true;
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

            if (completionVisible && completionDisplayedPercent < completionTargetPercent)
            {
                completionAnimationElapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(
                    completionAnimationElapsed / CompletionFillAnimationDuration);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                completionDisplayedPercent = Mathf.Lerp(
                    completionStartPercent,
                    completionTargetPercent,
                    easedProgress);
                if (progress >= 1f)
                {
                    completionDisplayedPercent = completionTargetPercent;
                }
            }

            float unscaledDelta = Time.unscaledDeltaTime;
            settingsButtonPressAmount = Mathf.MoveTowards(
                settingsButtonPressAmount,
                settingsButtonHeld ? 1f : 0f,
                unscaledDelta * (settingsButtonHeld ? 14f : 20f));
            continueButtonPressAmount = Mathf.MoveTowards(
                continueButtonPressAmount,
                continueButtonHeld ? 1f : 0f,
                unscaledDelta * (continueButtonHeld ? 14f : 20f));
            settingsButtonReleaseTime = Mathf.Max(
                0f,
                settingsButtonReleaseTime - unscaledDelta);
            continueButtonReleaseTime = Mathf.Max(
                0f,
                continueButtonReleaseTime - unscaledDelta);

            if (continueActionPending)
            {
                continueActionTimer -= unscaledDelta;
                if (continueActionTimer <= 0f)
                {
                    continueActionPending = false;
                    if (retryVisible)
                    {
                        levelFlow?.RetryCurrentLevel();
                    }
                    else
                    {
                        ContinueAfterCompletion();
                    }
                }
            }

        }

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Mathf.Max(0.01f, Mathf.Min(
                Screen.width / ReferenceWidth,
                Screen.height / ReferenceHeight));
            float horizontalOffset = (Screen.width - (ReferenceWidth * scale)) * 0.5f;
            float verticalOffset = (Screen.height - (ReferenceHeight * scale)) * 0.5f;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;

            DrawPremiumTopHud();

            // Draw completion dimming in physical screen space before entering
            // the fixed 720x1280 gameplay coordinate system. This covers every
            // pixel on tall and wide devices instead of leaving undimmed bands.
            if (!settingsVisible &&
                (completionVisible || mysteryBoxVisible || retryVisible) &&
                overlayAlpha > 0.001f)
            {
                DrawScreenDimmer((retryVisible ? 0.64f : 0.56f) * overlayAlpha);
            }

            GUI.matrix = Matrix4x4.TRS(
                new Vector3(horizontalOffset, verticalOffset, 0f),
                Quaternion.identity,
                new Vector3(scale, scale, 1f));

            if (settingsVisible)
            {
                DrawSettingsPanel();
            }
            else if (overlayVisible && overlayAlpha > 0.001f)
            {
                DrawGameplayOverlay(overlayAlpha);
            }

            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void DrawPremiumTopHud()
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            float safeTopPixels = Mathf.Max(0f, Screen.height - Screen.safeArea.yMax);
            Rect plateRect = CalculateHudPlateRect(Screen.width, safeTopPixels);
            float scale = plateRect.width / HudPlateWidth;
            LastHudScale = scale;
            LastHudTopOffset = plateRect.y;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(plateRect.x, plateRect.y, 0f),
                Quaternion.identity,
                new Vector3(scale, scale, 1f));

            if (premiumTopHudPlate != null)
            {
                GUI.DrawTexture(
                    new Rect(0f, 0f, HudPlateWidth, HudPlateHeight),
                    premiumTopHudPlate,
                    ScaleMode.StretchToFill,
                    true);
            }

            TrackButtonInteraction(
                SettingsButtonHitRect,
                ref settingsButtonHeld,
                ref settingsButtonReleaseTime);
            DrawAnimatedTextureRegion(
                premiumTopHudPlate,
                NormalizeTextureRect(SettingsButtonVisualRect, HudPlateWidth, HudPlateHeight),
                SettingsButtonVisualRect,
                settingsButtonPressAmount,
                settingsButtonReleaseTime,
                1f);
            if (GUI.Button(
                    SettingsButtonHitRect,
                    GUIContent.none,
                    transparentButtonStyle))
            {
                TriggerButtonRelease(
                    ref settingsButtonHeld,
                    ref settingsButtonReleaseTime);
                ToggleSettings();
            }

            // The level capsule is deliberately display-only. Keeping it as a
            // label prevents it from behaving like a button while preserving
            // the dynamic level name inside the approved artwork.
            DrawPremiumHudLabel(
                new Rect(208f, 55f, 324f, 105f),
                levelName.ToUpperInvariant(),
                levelStyle,
                2.25f);

            DrawPremiumHudLabel(
                new Rect(659f, 59f, 96f, 104f),
                coinBalance.ToString(),
                walletStyle,
                2f);

            // The plus remains part of the approved prototype artwork, but it
            // deliberately has no action: this prototype has no coin shop UI.
            GUI.Button(
                new Rect(746f, 61f, 82f, 108f),
                GUIContent.none,
                transparentButtonStyle);

            GUI.matrix = previousMatrix;
        }

        public static Rect CalculateHudPlateRect(float screenWidth, float safeTopPixels)
        {
            float scale = Mathf.Max(
                0.01f,
                (Mathf.Max(1f, screenWidth) / HudPlateWidth) * HudScaleFactor);
            float width = HudPlateWidth * scale;
            float height = HudPlateHeight * scale;
            float x = (screenWidth - width) * 0.5f;
            float y = Mathf.Max(
                0f,
                Mathf.Max(0f, safeTopPixels) + HudSafePadding - (HudVisualTop * scale));
            return new Rect(x, y, width, height);
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
            if (mysteryBoxVisible)
            {
                DrawMysteryBoxOverlay(alpha);
                return;
            }

            if (completionVisible)
            {
                DrawCompletionOverlay(alpha);
                return;
            }

            if (retryVisible && lossCard != null)
            {
                DrawLossOverlay(alpha);
                return;
            }

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

        private void DrawLossOverlay(float alpha)
        {
            Rect cardRect = CalculateCompletionCardRect(lossCard);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(cardRect, lossCard, ScaleMode.StretchToFill, true);

            if (lossConveyorSnapshot != null)
            {
                Rect snapshotRect = DenormalizeRect(cardRect, LossSnapshotNormalizedRect);
                GUI.DrawTexture(
                    snapshotRect,
                    lossConveyorSnapshot,
                    ScaleMode.ScaleToFit,
                    true);
            }

            Rect retryRect = DenormalizeRect(
                cardRect,
                LossRetryButtonNormalizedHitRect);
            Rect retryVisualRect = DenormalizeRect(
                cardRect,
                LossRetryButtonNormalizedVisualRect);
            TrackButtonInteraction(
                retryRect,
                ref continueButtonHeld,
                ref continueButtonReleaseTime);
            DrawAnimatedTextureRegion(
                lossCard,
                LossRetryButtonNormalizedVisualRect,
                retryVisualRect,
                continueButtonPressAmount,
                continueButtonReleaseTime,
                alpha);
            if (!continueActionPending && alpha > 0.82f && GUI.Button(
                    retryRect,
                    GUIContent.none,
                    transparentButtonStyle))
            {
                TriggerButtonRelease(
                    ref continueButtonHeld,
                    ref continueButtonReleaseTime);
                continueActionPending = true;
                continueActionTimer = ContinueActionDelay;
            }

            GUI.color = previous;
        }

        private void CaptureLossConveyorSnapshot(
            StadiumConveyorController conveyorSource)
        {
            ReleaseLossSnapshot();
            if (conveyorSource == null)
            {
                return;
            }

            Camera gameplayCamera = Camera.main;
            if (gameplayCamera == null)
            {
                gameplayCamera = FindFirstObjectByType<Camera>();
            }

            if (gameplayCamera == null ||
                !TryCalculateConveyorViewportBounds(
                    gameplayCamera,
                    conveyorSource,
                    out Rect viewportBounds))
            {
                return;
            }

            int sourceWidth = Mathf.Max(1, Screen.width);
            int sourceHeight = Mathf.Max(1, Screen.height);
            int renderWidth = Mathf.Clamp(Mathf.Max(sourceWidth, 720), 720, 1440);
            int renderHeight = Mathf.Clamp(
                Mathf.RoundToInt(renderWidth * (sourceHeight / (float)sourceWidth)),
                720,
                2560);
            RenderTexture captureTarget = RenderTexture.GetTemporary(
                renderWidth,
                renderHeight,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            RenderTexture previousTarget = gameplayCamera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                gameplayCamera.targetTexture = captureTarget;
                gameplayCamera.Render();
                RenderTexture.active = captureTarget;

                int x = Mathf.Clamp(
                    Mathf.FloorToInt(viewportBounds.xMin * renderWidth),
                    0,
                    renderWidth - 1);
                int y = Mathf.Clamp(
                    Mathf.FloorToInt(viewportBounds.yMin * renderHeight),
                    0,
                    renderHeight - 1);
                int width = Mathf.Clamp(
                    Mathf.CeilToInt(viewportBounds.width * renderWidth),
                    1,
                    renderWidth - x);
                int height = Mathf.Clamp(
                    Mathf.CeilToInt(viewportBounds.height * renderHeight),
                    1,
                    renderHeight - y);

                Texture2D snapshot = new Texture2D(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false)
                {
                    name = "Runtime Full Conveyor Snapshot",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                snapshot.ReadPixels(new Rect(x, y, width, height), 0, 0, false);
                snapshot.Apply(false, false);
                lossConveyorSnapshot = snapshot;
            }
            finally
            {
                gameplayCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(captureTarget);
            }
        }

        private static bool TryCalculateConveyorViewportBounds(
            Camera gameplayCamera,
            StadiumConveyorController conveyorSource,
            out Rect viewportBounds)
        {
            viewportBounds = default;
            Renderer[] renderers = conveyorSource.GetComponentsInChildren<Renderer>(false);
            bool found = false;
            float minimumX = 1f;
            float minimumY = 1f;
            float maximumX = 0f;
            float maximumY = 0f;

            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;
                Vector3[] corners =
                {
                    new Vector3(bounds.min.x, bounds.min.y, bounds.center.z),
                    new Vector3(bounds.min.x, bounds.max.y, bounds.center.z),
                    new Vector3(bounds.max.x, bounds.min.y, bounds.center.z),
                    new Vector3(bounds.max.x, bounds.max.y, bounds.center.z)
                };
                for (int cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
                {
                    Vector3 viewport = gameplayCamera.WorldToViewportPoint(corners[cornerIndex]);
                    if (viewport.z <= 0f)
                    {
                        continue;
                    }

                    found = true;
                    minimumX = Mathf.Min(minimumX, viewport.x);
                    minimumY = Mathf.Min(minimumY, viewport.y);
                    maximumX = Mathf.Max(maximumX, viewport.x);
                    maximumY = Mathf.Max(maximumY, viewport.y);
                }
            }

            if (!found)
            {
                return false;
            }

            const float horizontalPadding = 0.025f;
            const float verticalPadding = 0.025f;
            minimumX = Mathf.Clamp01(minimumX - horizontalPadding);
            maximumX = Mathf.Clamp01(maximumX + horizontalPadding);
            minimumY = Mathf.Clamp01(minimumY - verticalPadding);
            maximumY = Mathf.Clamp01(maximumY + verticalPadding);
            viewportBounds = Rect.MinMaxRect(
                minimumX,
                minimumY,
                maximumX,
                maximumY);
            return viewportBounds.width > 0.001f && viewportBounds.height > 0.001f;
        }

        private void ReleaseLossSnapshot()
        {
            if (lossConveyorSnapshot == null)
            {
                return;
            }

            Destroy(lossConveyorSnapshot);
            lossConveyorSnapshot = null;
        }

        private void DrawCompletionOverlay(float alpha)
        {
            int cardIndex = Mathf.Clamp(CompletionRewardStage - 1, 0, completionCards.Length - 1);
            Texture2D card = completionCards[cardIndex];
            if (card == null)
            {
                DrawDimmer(0.18f * alpha);
                return;
            }

            Rect cardRect = CalculateCompletionCardRect(card);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(cardRect, card, ScaleMode.StretchToFill, true);
            // Keep the live progress channel after the count-up finishes. The
            // approved card supplies the frame and star artwork, while this
            // single live layer supplies both the animated and final bar. That
            // prevents an endpoint jump to the differently colored bar baked
            // into the source card.
            DrawAnimatedCompletionProgress(cardRect, alpha);

            // The visual button is baked into the approved card. This invisible
            // hit area is deliberately inset so only the green Continue button
            // advances the level.
            Rect continueRect = new Rect(
                cardRect.x + (cardRect.width * 0.18f),
                cardRect.y + (cardRect.height * 0.835f),
                cardRect.width * 0.64f,
                cardRect.height * 0.105f);
            Rect continueVisualRect = DenormalizeRect(
                cardRect,
                ContinueButtonNormalizedVisualRect);
            TrackButtonInteraction(
                continueRect,
                ref continueButtonHeld,
                ref continueButtonReleaseTime);
            DrawAnimatedTextureRegion(
                card,
                ContinueButtonNormalizedVisualRect,
                continueVisualRect,
                continueButtonPressAmount,
                continueButtonReleaseTime,
                alpha);
            if (!continueActionPending && alpha > 0.82f && GUI.Button(
                    continueRect,
                    GUIContent.none,
                    transparentButtonStyle))
            {
                TriggerButtonRelease(
                    ref continueButtonHeld,
                    ref continueButtonReleaseTime);
                continueActionPending = true;
                continueActionTimer = ContinueActionDelay;
            }

            GUI.color = previous;
        }

        private void DrawMysteryBoxOverlay(float alpha)
        {
            if (mysteryBoxCard == null)
            {
                DrawDimmer(0.18f * alpha);
                return;
            }

            Rect cardRect = CalculateCompletionCardRect(mysteryBoxCard);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(cardRect, mysteryBoxCard, ScaleMode.StretchToFill, true);

            Rect continueRect = DenormalizeRect(
                cardRect,
                MysteryContinueButtonNormalizedHitRect);
            Rect continueVisualRect = DenormalizeRect(
                cardRect,
                MysteryContinueButtonNormalizedVisualRect);
            TrackButtonInteraction(
                continueRect,
                ref continueButtonHeld,
                ref continueButtonReleaseTime);
            DrawAnimatedTextureRegion(
                mysteryBoxCard,
                MysteryContinueButtonNormalizedVisualRect,
                continueVisualRect,
                continueButtonPressAmount,
                continueButtonReleaseTime,
                alpha);
            if (!continueActionPending && alpha > 0.82f && GUI.Button(
                    continueRect,
                    GUIContent.none,
                    transparentButtonStyle))
            {
                TriggerButtonRelease(
                    ref continueButtonHeld,
                    ref continueButtonReleaseTime);
                continueActionPending = true;
                continueActionTimer = ContinueActionDelay;
            }

            GUI.color = previous;
        }

        private void ResetContinueButtonState()
        {
            continueActionPending = false;
            continueButtonHeld = false;
            continueButtonPressAmount = 0f;
            continueButtonReleaseTime = 0f;
            continueActionTimer = 0f;
        }

        private void DrawAnimatedCompletionProgress(Rect cardRect, float alpha)
        {
            // This is the inner navy channel, not the pearlescent outer frame.
            // Both the track replacement and its fill are restricted to these
            // authored bounds, so no animated pixel can overlap the frame.
            Rect trackRect = CalculateCompletionProgressTrackRect(cardRect);

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(trackRect, completionTrackTexture, ScaleMode.StretchToFill, true);

            float fillFraction = Mathf.Clamp01(completionDisplayedPercent / 100f);
            if (fillFraction > 0.001f)
            {
                Rect clippedRect = CalculateCompletionProgressFillRect(
                    trackRect,
                    fillFraction);
                GUI.BeginGroup(clippedRect);
                Color fill = CompletionColorForPercent(completionDisplayedPercent);
                GUI.color = new Color(fill.r, fill.g, fill.b, alpha);
                GUI.DrawTexture(
                    new Rect(0f, 0f, trackRect.width, trackRect.height),
                    completionFillTexture,
                    ScaleMode.StretchToFill,
                    true);
                GUI.EndGroup();
            }

            GUI.color = new Color(1f, 1f, 1f, alpha);
            DrawPremiumHudLabel(
                trackRect,
                $"{Mathf.RoundToInt(completionDisplayedPercent)}%",
                completionPercentStyle,
                1.5f);
            GUI.color = previous;
        }

        private static Rect CalculateCompletionCardRect(Texture2D card)
        {
            float sourceWidth = Mathf.Max(1f, card != null ? card.width : 1f);
            float sourceHeight = Mathf.Max(1f, card != null ? card.height : 1f);
            float aspect = sourceWidth / sourceHeight;
            float width = CompletionCardMaxWidth;
            float height = width / aspect;
            if (height > CompletionCardMaxHeight)
            {
                height = CompletionCardMaxHeight;
                width = height * aspect;
            }

            return new Rect(
                (ReferenceWidth - width) * 0.5f,
                CompletionCardCenterY - (height * 0.5f),
                width,
                height);
        }

        private static Rect NormalizeTextureRect(
            Rect pixelRect,
            float textureWidth,
            float textureHeight)
        {
            return new Rect(
                pixelRect.x / Mathf.Max(1f, textureWidth),
                pixelRect.y / Mathf.Max(1f, textureHeight),
                pixelRect.width / Mathf.Max(1f, textureWidth),
                pixelRect.height / Mathf.Max(1f, textureHeight));
        }

        private static Rect DenormalizeRect(Rect parent, Rect normalized)
        {
            return new Rect(
                parent.x + (parent.width * normalized.x),
                parent.y + (parent.height * normalized.y),
                parent.width * normalized.width,
                parent.height * normalized.height);
        }

        private static Rect ScaleAroundCenter(Rect rect, float scale)
        {
            Vector2 center = rect.center;
            Vector2 size = rect.size * Mathf.Max(0.01f, scale);
            return new Rect(
                center.x - (size.x * 0.5f),
                center.y - (size.y * 0.5f),
                size.x,
                size.y);
        }

        private static void TrackButtonInteraction(
            Rect hitRect,
            ref bool held,
            ref float releaseTime)
        {
            Event current = Event.current;
            if (current == null || current.button != 0)
            {
                return;
            }

            if (current.type == EventType.MouseDown && hitRect.Contains(current.mousePosition))
            {
                held = true;
                releaseTime = 0f;
            }
            else if (current.rawType == EventType.MouseUp && held)
            {
                held = false;
                releaseTime = ButtonReleaseDuration;
            }
        }

        private static void TriggerButtonRelease(
            ref bool held,
            ref float releaseTime)
        {
            held = false;
            releaseTime = ButtonReleaseDuration;
        }

        private static void DrawAnimatedTextureRegion(
            Texture2D texture,
            Rect normalizedSourceRect,
            Rect destinationRect,
            float pressAmount,
            float releaseTime,
            float alpha)
        {
            if (texture == null ||
                (pressAmount <= 0.001f && releaseTime <= 0.001f) ||
                alpha <= 0.001f)
            {
                return;
            }

            float releaseNormalized = Mathf.Clamp01(
                releaseTime / ButtonReleaseDuration);
            float releaseProgress = 1f - releaseNormalized;
            float bounce = Mathf.Sin(releaseProgress * Mathf.PI) *
                           releaseNormalized *
                           ButtonReleaseBounceScale;
            float pressedScale = Mathf.Lerp(1f, ButtonPressScale, Mathf.Clamp01(pressAmount));
            Rect animatedRect = ScaleAroundCenter(destinationRect, pressedScale + bounce);
            Rect uvRect = new Rect(
                normalizedSourceRect.x,
                1f - normalizedSourceRect.y - normalizedSourceRect.height,
                normalizedSourceRect.width,
                normalizedSourceRect.height);

            Color previous = GUI.color;
            float pressShade = Mathf.Lerp(1f, 0.88f, Mathf.Clamp01(pressAmount));
            float releaseHighlight = Mathf.Sin(releaseProgress * Mathf.PI) *
                                     releaseNormalized * 0.12f;
            float tint = Mathf.Clamp(pressShade + releaseHighlight, 0f, 1.08f);
            GUI.color = new Color(tint, tint, tint, alpha);
            GUI.DrawTextureWithTexCoords(animatedRect, texture, uvRect, true);
            GUI.color = previous;
        }

        public static Rect CalculateCompletionProgressTrackRect(Rect cardRect)
        {
            return new Rect(
                cardRect.x + (cardRect.width * CompletionTrackNormalizedX),
                cardRect.y + (cardRect.height * CompletionTrackNormalizedY),
                cardRect.width * CompletionTrackNormalizedWidth,
                cardRect.height * CompletionTrackNormalizedHeight);
        }

        public static Rect CalculateCompletionProgressFillRect(
            Rect trackRect,
            float fillFraction)
        {
            return new Rect(
                trackRect.x,
                trackRect.y,
                trackRect.width * Mathf.Clamp01(fillFraction),
                trackRect.height);
        }

        private static Color CompletionColorForPercent(float percent)
        {
            Color red = new Color32(245, 54, 66, 255);
            Color blue = new Color32(26, 143, 255, 255);
            Color green = new Color32(18, 214, 112, 255);
            Color yellow = new Color32(255, 201, 32, 255);
            Color violet = new Color32(154, 52, 229, 255);

            if (percent <= 20f)
            {
                return red;
            }

            if (percent <= 40f)
            {
                return Color.Lerp(red, blue, Mathf.InverseLerp(20f, 40f, percent));
            }

            if (percent <= 60f)
            {
                return Color.Lerp(blue, green, Mathf.InverseLerp(40f, 60f, percent));
            }

            if (percent <= 80f)
            {
                return Color.Lerp(green, yellow, Mathf.InverseLerp(60f, 80f, percent));
            }

            return Color.Lerp(yellow, violet, Mathf.InverseLerp(80f, 100f, percent));
        }

        private static void DrawScreenDimmer(float alpha)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.025f, 0.035f, 0.095f, alpha);
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture,
                ScaleMode.StretchToFill);
            GUI.color = previous;
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

        private static void DrawPremiumHudLabel(
            Rect rect,
            string text,
            GUIStyle style,
            float offset)
        {
            Color original = style.normal.textColor;
            style.normal.textColor = new Color32(23, 58, 104, 255);
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    GUI.Label(
                        new Rect(
                            rect.x + (x * offset),
                            rect.y + (y * offset),
                            rect.width,
                            rect.height),
                        text,
                        style);
                }
            }

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
            completionTrackTexture = CreateRoundedGradientTexture(
                "Completion Progress Track",
                new Color32(17, 55, 101, 255),
                new Color32(5, 31, 69, 255),
                new Color32(20, 59, 105, 255),
                22f,
                1.5f);
            completionFillTexture = CreateRoundedGradientTexture(
                "Completion Progress Fill",
                new Color32(255, 255, 255, 255),
                new Color32(194, 194, 194, 255),
                new Color32(255, 255, 255, 255),
                22f,
                1.5f);

            gearTexture = CreateOutlinedIconTexture("Settings Gear", IsGearPixel);
            lockTexture = CreateOutlinedIconTexture("Level Lock", IsLockPixel);
            if (premiumTopHudPlate == null)
            {
                premiumTopHudPlate = Resources.Load<Texture2D>(PremiumTopHudResourcePath);
            }

            RectOffset scalableBorder = new RectOffset(22, 22, 22, 22);
            panelStyle = CreatePanelStyle(lavenderPanelTexture, scalableBorder);
            overlayPanelStyle = CreatePanelStyle(overlayPanelTexture, scalableBorder);
            shadowPanelStyle = CreatePanelStyle(shadowTexture, scalableBorder);
            unlockPanelStyle = CreatePanelStyle(unlockPanelTexture, scalableBorder);

            levelStyle = CreateTextStyle(44, Color.white, TextAnchor.MiddleCenter);
            walletStyle = CreateTextStyle(39, Color.white, TextAnchor.MiddleCenter);
            unlockStyle = CreateTextStyle(20, Color.white, TextAnchor.MiddleCenter);
            statusStyle = CreateTextStyle(41, new Color32(65, 72, 129, 255), TextAnchor.MiddleCenter);
            statusStyle.wordWrap = true;
            subStatusStyle = CreateTextStyle(23, new Color32(82, 104, 151, 255), TextAnchor.UpperCenter);
            subStatusStyle.wordWrap = true;
            settingsLabelStyle = CreateTextStyle(26, new Color32(65, 77, 134, 255), TextAnchor.MiddleLeft);
            completionPercentStyle = CreateTextStyle(42, Color.white, TextAnchor.MiddleCenter);

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
            ReleaseLossSnapshot();
            DestroyTexture(lavenderPanelTexture);
            DestroyTexture(overlayPanelTexture);
            DestroyTexture(goldButtonTexture);
            DestroyTexture(shadowTexture);
            DestroyTexture(unlockPanelTexture);
            DestroyTexture(toggleOnTexture);
            DestroyTexture(toggleOffTexture);
            DestroyTexture(gearTexture);
            DestroyTexture(lockTexture);
            DestroyTexture(completionTrackTexture);
            DestroyTexture(completionFillTexture);
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
