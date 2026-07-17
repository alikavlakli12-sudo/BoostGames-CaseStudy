using System.Collections.Generic;
using MarbleSort.Core;
using MarbleSort.Gameplay.Conveyor;
using MarbleSort.Gameplay.Flow;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Gameplay.Receivers;
using MarbleSort.Gameplay.TopGrid;
using MarbleSort.Presentation;
using MarbleSort.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MarbleSort.Editor
{
    public static class ProjectBootstrapper
    {
        private const string ScenePath = "Assets/MarbleSort/Scenes/Main.unity";
        private const string MaterialsPath = "Assets/MarbleSort/Art/Materials";
        private const string BackgroundTexturePath =
            "Assets/MarbleSort/Art/Textures/PortraitEnvironmentV3.png";
        private const string HudPlateTexturePath =
            "Assets/MarbleSort/Resources/Presentation/UI/PremiumTopHudPlate.png";

        [MenuItem("Marble Sort/Setup/Rebuild Base Scene")]
        public static void CreateBaseProject()
        {
            ConfigureProjectSettings();
            ConfigurePremiumTexture(HudPlateTexturePath, true);

            Material background = GetOrCreateMaterial("Background", new Color32(58, 88, 198, 255));
            Material basin = GetOrCreateMaterial("Basin", new Color32(196, 225, 242, 255));
            Material basinHighlight = GetOrCreateMaterial("BasinHighlight", new Color32(242, 249, 255, 255));
            Material receiverBay = GetOrCreateMaterial("ReceiverBay", new Color32(191, 220, 239, 255));
            Material border = GetOrCreateMaterial("Border", new Color32(67, 88, 160, 255));
            Material shadow = GetOrCreateMaterial("Shadow", new Color32(39, 50, 103, 255));
            Material conveyor = GetOrCreateMaterial("Conveyor", new Color32(147, 153, 180, 255));
            Material conveyorInner = GetOrCreateMaterial("ConveyorInner", new Color32(220, 225, 239, 255));
            Material conveyorSlot = GetOrCreateMaterial("ConveyorSlot", new Color32(82, 88, 121, 255));
            Material particle = GetOrCreateMaterial("Particle", new Color32(247, 250, 255, 255));
            Material green = GetOrCreateMaterial("Green", new Color32(73, 214, 78, 255));
            Material blue = GetOrCreateMaterial("Blue", new Color32(57, 84, 239, 255));
            Material orange = GetOrCreateMaterial("Orange", new Color32(255, 164, 46, 255));
            Material yellow = GetOrCreateMaterial("Yellow", new Color32(255, 225, 61, 255));
            Material backgroundArt = GetOrCreateBackgroundMaterial();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Main";

            Camera camera = CreateCamera(background.color);
            CreateLighting();

            GameObject root = new GameObject("Marble Sort");
            Transform backgroundTransform = CreateBackground(root.transform, backgroundArt).transform;
            ResponsiveCameraController responsiveCamera = camera.gameObject.AddComponent<ResponsiveCameraController>();
            responsiveCamera.Configure(9.4f, 4.65f, backgroundTransform, 853f / 1844f);

            GameObject systems = new GameObject("Systems");
            systems.transform.SetParent(root.transform);
            GameBootstrap bootstrap = systems.AddComponent<GameBootstrap>();
            MarblePalette palette = systems.AddComponent<MarblePalette>();
            palette.Configure(green, blue, orange, yellow);
            MarblePool marblePool = systems.AddComponent<MarblePool>();
            marblePool.Configure(palette, 72, MarblePool.ActiveMarbleDiameter, -8.5f);

            GameObject board = new GameObject("Board");
            board.transform.SetParent(root.transform);
            CreateBasin(board.transform, basin, basinHighlight, receiverBay, border, shadow);
            TopGridController topGrid = CreateTopGrid(board.transform, bootstrap, marblePool, palette, camera);
            StadiumConveyorController conveyorController = CreateConveyor(
                board.transform,
                bootstrap,
                conveyor,
                conveyorInner,
                border,
                shadow,
                conveyorSlot);
            ConveyorAdmissionController admission = CreateConveyorEntrance(
                board.transform,
                conveyorController,
                border);
            ReceiverQueueController receivers = CreateReceivers(
                board.transform,
                bootstrap,
                conveyorController,
                marblePool,
                palette,
                conveyorSlot);

            LevelFlowController levelFlow = systems.AddComponent<LevelFlowController>();
            GameHudView hud = CreateHud(root.transform, levelFlow);
            levelFlow.Configure(
                bootstrap,
                topGrid,
                conveyorController,
                admission,
                receivers,
                marblePool,
                hud,
                1.6f);

            GameFeedbackController feedback = systems.AddComponent<GameFeedbackController>();
            feedback.Configure(topGrid, admission, receivers, levelFlow, palette, particle);
            systems.AddComponent<RuntimePerformanceProbe>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Marble Sort base project created at {ScenePath}.");
        }

        private static void ConfigureProjectSettings()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            PlayerSettings.companyName = "Case Study";
            PlayerSettings.productName = "Marble Sort";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth = 720;
            PlayerSettings.defaultScreenHeight = 1280;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        private static Camera CreateCamera(Color backgroundColor)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -20f);
            cameraObject.transform.rotation = Quaternion.identity;
            camera.orthographic = true;
            camera.orthographicSize = 9.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Key Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.9f);
            lightObject.transform.rotation = Quaternion.Euler(25f, -25f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.76f);
        }

        private static GameObject CreateBackground(Transform parent, Material backgroundMaterial)
        {
            GameObject background = CreateVisual(
                "Illustrated Background",
                PrimitiveType.Quad,
                parent,
                new Vector3(0f, 0f, 4f),
                Vector3.one,
                Quaternion.identity,
                backgroundMaterial,
                false);
            background.transform.SetAsFirstSibling();
            return background;
        }

        private static void CreateBasin(
            Transform parent,
            Material basin,
            Material basinHighlight,
            Material receiverBay,
            Material border,
            Material shadow)
        {
            GameObject basinRoot = new GameObject("Physics Basin");
            basinRoot.transform.SetParent(parent);

            GameObject basinShadow = PresentationMeshFactory.CreateRoundedBox(
                "Basin Shadow",
                basinRoot.transform,
                8.9f,
                7.18f,
                0.12f,
                0.62f,
                shadow);
            basinShadow.transform.localPosition = new Vector3(0.06f, 1.72f, 1.62f);

            GameObject basinRim = PresentationMeshFactory.CreateRoundedBox(
                "Basin Rim",
                basinRoot.transform,
                8.74f,
                7.06f,
                0.14f,
                0.6f,
                border);
            basinRim.transform.localPosition = new Vector3(0f, 1.76f, 1.5f);

            GameObject basinBack = PresentationMeshFactory.CreateRoundedBox(
                "Basin Back",
                basinRoot.transform,
                8.35f,
                6.68f,
                0.16f,
                0.48f,
                basin);
            basinBack.transform.localPosition = new Vector3(0f, 1.79f, 1.32f);

            GameObject upperHighlight = PresentationMeshFactory.CreateRoundedBox(
                "Basin Upper Highlight",
                basinRoot.transform,
                7.48f,
                0.09f,
                0.03f,
                0.04f,
                basinHighlight);
            upperHighlight.transform.localPosition = new Vector3(0f, 5.03f, 1.2f);

            GameObject bayShadow = PresentationMeshFactory.CreateRoundedBox(
                "Receiver Bay Shadow",
                basinRoot.transform,
                8.88f,
                5.2f,
                0.12f,
                0.6f,
                shadow);
            bayShadow.transform.localPosition = new Vector3(0.06f, -6.4f, 1.62f);

            GameObject bayRim = PresentationMeshFactory.CreateRoundedBox(
                "Receiver Bay Rim",
                basinRoot.transform,
                8.72f,
                5.08f,
                0.14f,
                0.58f,
                border);
            bayRim.transform.localPosition = new Vector3(0f, -6.32f, 1.5f);

            GameObject bayBack = PresentationMeshFactory.CreateRoundedBox(
                "Receiver Bay Back",
                basinRoot.transform,
                8.35f,
                4.72f,
                0.16f,
                0.46f,
                receiverBay);
            bayBack.transform.localPosition = new Vector3(0f, -6.34f, 1.32f);

            CreateVisual(
                "Left Wall",
                PrimitiveType.Cube,
                basinRoot.transform,
                new Vector3(-4.05f, 1.7f, 0f),
                new Vector3(0.25f, 6.75f, 0.6f),
                Quaternion.identity,
                border,
                true);

            CreateVisual(
                "Right Wall",
                PrimitiveType.Cube,
                basinRoot.transform,
                new Vector3(4.05f, 1.7f, 0f),
                new Vector3(0.25f, 6.75f, 0.6f),
                Quaternion.identity,
                border,
                true);

            CreateVisual(
                "Left Funnel",
                PrimitiveType.Cube,
                basinRoot.transform,
                new Vector3(-2.15f, -1.15f, 0f),
                new Vector3(3.7f, 0.28f, 0.6f),
                Quaternion.Euler(0f, 0f, -13f),
                border,
                true);

            CreateVisual(
                "Left Funnel Shadow",
                PrimitiveType.Cube,
                basinRoot.transform,
                new Vector3(-2.12f, -1.23f, 0.32f),
                new Vector3(3.78f, 0.38f, 0.22f),
                Quaternion.Euler(0f, 0f, -13f),
                shadow,
                false);

            CreateVisual(
                "Right Funnel",
                PrimitiveType.Cube,
                basinRoot.transform,
                new Vector3(2.15f, -1.15f, 0f),
                new Vector3(3.7f, 0.28f, 0.6f),
                Quaternion.Euler(0f, 0f, 13f),
                border,
                true);

            CreateVisual(
                "Right Funnel Shadow",
                PrimitiveType.Cube,
                basinRoot.transform,
                new Vector3(2.12f, -1.23f, 0.32f),
                new Vector3(3.78f, 0.38f, 0.22f),
                Quaternion.Euler(0f, 0f, 13f),
                shadow,
                false);

            // The approved environment plate provides the complete premium molded artwork.
            // Keep these objects as the gameplay collision rig but do not render flat duplicates.
            Renderer[] physicsRenderers = basinRoot.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < physicsRenderers.Length; index++)
            {
                physicsRenderers[index].enabled = false;
            }
        }

        private static TopGridController CreateTopGrid(
            Transform parent,
            GameBootstrap bootstrap,
            MarblePool marblePool,
            MarblePalette palette,
            Camera camera)
        {
            GameObject gridRoot = new GameObject("Runtime Top Grid");
            gridRoot.transform.SetParent(parent);
            gridRoot.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            gridRoot.transform.localScale = new Vector3(1.18f, 1.18f, 1f);
            TopGridController controller = gridRoot.AddComponent<TopGridController>();
            controller.Configure(bootstrap, marblePool, palette, camera);
            return controller;
        }

        private static StadiumConveyorController CreateConveyor(
            Transform parent,
            GameBootstrap bootstrap,
            Material conveyor,
            Material conveyorInner,
            Material border,
            Material shadow,
            Material conveyorSlot)
        {
            GameObject conveyorRoot = new GameObject("Stadium Conveyor");
            conveyorRoot.transform.SetParent(parent);
            conveyorRoot.transform.localPosition = new Vector3(0f, -3.25f, 0f);
            conveyorRoot.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            GameObject trackShadow = PresentationMeshFactory.CreateStadiumRibbon(
                "Track Shadow",
                conveyorRoot.transform,
                7f,
                0.75f,
                0.61f,
                shadow);
            trackShadow.transform.localPosition = new Vector3(0.04f, -0.07f, 0.18f);

            GameObject trackRim = PresentationMeshFactory.CreateStadiumRibbon(
                "Track Rim",
                conveyorRoot.transform,
                7f,
                0.75f,
                0.55f,
                border);
            trackRim.transform.localPosition = new Vector3(0f, 0f, 0.1f);

            GameObject trackSurface = PresentationMeshFactory.CreateStadiumRibbon(
                "Track Surface",
                conveyorRoot.transform,
                7f,
                0.75f,
                0.42f,
                conveyor);
            trackSurface.transform.localPosition = new Vector3(0f, 0f, 0.04f);

            GameObject trackHighlight = PresentationMeshFactory.CreateStadiumRibbon(
                "Track Highlight",
                conveyorRoot.transform,
                7f,
                0.75f,
                0.3f,
                conveyorInner);
            trackHighlight.transform.localPosition = new Vector3(0f, 0f, 0f);

            List<Transform> slotViews = new List<Transform>(24);
            for (int index = 0; index < 24; index++)
            {
                GameObject slot = CreateVisual(
                    $"Slot {index + 1:00}",
                    PrimitiveType.Sphere,
                    conveyorRoot.transform,
                    Vector3.zero,
                    new Vector3(0.3f, 0.21f, 0.1f),
                    Quaternion.identity,
                    conveyorSlot,
                    false);
                slotViews.Add(slot.transform);
            }

            StadiumConveyorController controller = conveyorRoot.AddComponent<StadiumConveyorController>();
            controller.Configure(bootstrap, 24, 4f, 7f, 0.75f, slotViews.ToArray());
            conveyorRoot.AddComponent<ConveyorArtworkPresenter>();
            return controller;
        }

        private static ConveyorAdmissionController CreateConveyorEntrance(
            Transform parent,
            StadiumConveyorController conveyor,
            Material border)
        {
            GameObject entrance = new GameObject("Conveyor Entrance");
            entrance.transform.SetParent(parent);
            entrance.transform.localPosition = new Vector3(0f, -2f, 0f);

            BoxCollider trigger = entrance.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(0.54f, 0.9f, 0.6f);

            ConveyorAdmissionController admission = entrance.AddComponent<ConveyorAdmissionController>();
            admission.Configure(conveyor, 0.16f, 0.011f, 0.1f);

            GameObject leftEntranceGuide = CreateVisual(
                "Left Entrance Guide",
                PrimitiveType.Cube,
                entrance.transform,
                new Vector3(-0.34f, 0f, 0f),
                new Vector3(0.12f, 0.9f, 0.6f),
                Quaternion.identity,
                border,
                true);

            GameObject rightEntranceGuide = CreateVisual(
                "Right Entrance Guide",
                PrimitiveType.Cube,
                entrance.transform,
                new Vector3(0.34f, 0f, 0f),
                new Vector3(0.12f, 0.9f, 0.6f),
                Quaternion.identity,
                border,
                true);

            // The premium environment plate already includes the molded chute edges.
            // Keep the guide colliders for admission physics, but hide the old flat posts.
            leftEntranceGuide.GetComponent<Renderer>().enabled = false;
            rightEntranceGuide.GetComponent<Renderer>().enabled = false;

            GameObject admissionGate = CreateVisual(
                "Admission Gate",
                PrimitiveType.Cube,
                entrance.transform,
                new Vector3(0f, -0.43f, 0f),
                new Vector3(0.64f, 0.12f, 0.6f),
                Quaternion.identity,
                border,
                true);
            admissionGate.GetComponent<Renderer>().enabled = false;
            return admission;
        }

        private static ReceiverQueueController CreateReceivers(
            Transform parent,
            GameBootstrap bootstrap,
            StadiumConveyorController conveyor,
            MarblePool marblePool,
            MarblePalette palette,
            Material slotMaterial)
        {
            GameObject receiversRoot = new GameObject("Runtime Receiver Queues");
            receiversRoot.transform.SetParent(parent);
            ReceiverQueueController controller = receiversRoot.AddComponent<ReceiverQueueController>();
            controller.Configure(
                bootstrap,
                conveyor,
                marblePool,
                palette,
                slotMaterial,
                -4f,
                0.18f,
                0.18f,
                0.12f);
            return controller;
        }

        private static GameHudView CreateHud(Transform parent, LevelFlowController levelFlow)
        {
            GameObject hudObject = new GameObject("Game HUD");
            hudObject.transform.SetParent(parent, false);
            GameHudView hud = hudObject.AddComponent<GameHudView>();
            hud.Configure(levelFlow);
            return hud;
        }

        private static GameObject CreateVisual(
            string name,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material,
            bool keepCollider)
        {
            GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
            gameObject.name = name;
            gameObject.transform.SetParent(parent);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = localRotation;
            gameObject.transform.localScale = localScale;

            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (!keepCollider)
            {
                Collider collider = gameObject.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.DestroyImmediate(collider);
                }
            }

            return gameObject;
        }

        private static Material GetOrCreateBackgroundMaterial()
        {
            ConfigurePremiumTexture(BackgroundTexturePath, false);

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundTexturePath);
            string path = $"{MaterialsPath}/PortraitBackground.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Unlit/Texture");
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "PortraitBackground"
                };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.mainTexture = texture;
            material.color = Color.white;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigurePremiumTexture(string assetPath, bool alphaIsTransparency)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = alphaIsTransparency;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsPath}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                material = new Material(shader)
                {
                    name = name,
                    color = color
                };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.color = color;
                EditorUtility.SetDirty(material);
            }

            material.enableInstancing = true;
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0.3f);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            return material;
        }
    }
}
