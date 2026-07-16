using System.Collections.Generic;
using MarbleSort.Core;
using MarbleSort.Gameplay.Conveyor;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Gameplay.TopGrid;
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

        [MenuItem("Marble Sort/Setup/Rebuild Base Scene")]
        public static void CreateBaseProject()
        {
            ConfigureProjectSettings();

            Material background = GetOrCreateMaterial("Background", new Color32(111, 135, 187, 255));
            Material basin = GetOrCreateMaterial("Basin", new Color32(184, 204, 220, 255));
            Material border = GetOrCreateMaterial("Border", new Color32(65, 86, 139, 255));
            Material conveyor = GetOrCreateMaterial("Conveyor", new Color32(126, 129, 149, 255));
            Material conveyorSlot = GetOrCreateMaterial("ConveyorSlot", new Color32(79, 84, 108, 255));
            Material green = GetOrCreateMaterial("Green", new Color32(64, 211, 77, 255));
            Material blue = GetOrCreateMaterial("Blue", new Color32(57, 83, 232, 255));
            Material orange = GetOrCreateMaterial("Orange", new Color32(255, 165, 35, 255));
            Material yellow = GetOrCreateMaterial("Yellow", new Color32(255, 224, 46, 255));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Main";

            Camera camera = CreateCamera(background.color);
            CreateLighting();

            GameObject root = new GameObject("Marble Sort");
            GameObject systems = new GameObject("Systems");
            systems.transform.SetParent(root.transform);
            GameBootstrap bootstrap = systems.AddComponent<GameBootstrap>();
            MarblePalette palette = systems.AddComponent<MarblePalette>();
            palette.Configure(green, blue, orange, yellow);
            MarblePool marblePool = systems.AddComponent<MarblePool>();
            marblePool.Configure(palette, 72, 0.22f, -8.5f);

            GameObject board = new GameObject("Board");
            board.transform.SetParent(root.transform);
            CreateBasin(board.transform, basin, border);
            CreateTopGrid(board.transform, bootstrap, marblePool, palette, camera);
            StadiumConveyorController conveyorController = CreateConveyor(
                board.transform,
                bootstrap,
                conveyor,
                conveyorSlot);
            CreateConveyorEntrance(board.transform, conveyorController, border);
            CreateReceiverPreview(board.transform, conveyorSlot, green, blue, orange, yellow);

            CreateVisual(
                "Background",
                PrimitiveType.Cube,
                root.transform,
                new Vector3(0f, 0f, 3f),
                new Vector3(12f, 22f, 0.25f),
                Quaternion.identity,
                background,
                false).transform.SetAsFirstSibling();

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

        private static void CreateBasin(Transform parent, Material basin, Material border)
        {
            GameObject basinRoot = new GameObject("Physics Basin");
            basinRoot.transform.SetParent(parent);

            CreateVisual(
                "Basin Back",
                PrimitiveType.Cube,
                basinRoot.transform,
                new Vector3(0f, 2.5f, 1.2f),
                new Vector3(8.2f, 8.2f, 0.2f),
                Quaternion.identity,
                basin,
                false);

            CreateVisual(
                "Left Wall",
                PrimitiveType.Cube,
                basinRoot.transform,
                new Vector3(-4.05f, 2.5f, 0f),
                new Vector3(0.25f, 8.5f, 0.6f),
                Quaternion.identity,
                border,
                true);

            CreateVisual(
                "Right Wall",
                PrimitiveType.Cube,
                basinRoot.transform,
                new Vector3(4.05f, 2.5f, 0f),
                new Vector3(0.25f, 8.5f, 0.6f),
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
                "Right Funnel",
                PrimitiveType.Cube,
                basinRoot.transform,
                new Vector3(2.15f, -1.15f, 0f),
                new Vector3(3.7f, 0.28f, 0.6f),
                Quaternion.Euler(0f, 0f, 13f),
                border,
                true);
        }

        private static void CreateTopGrid(
            Transform parent,
            GameBootstrap bootstrap,
            MarblePool marblePool,
            MarblePalette palette,
            Camera camera)
        {
            GameObject gridRoot = new GameObject("Runtime Top Grid");
            gridRoot.transform.SetParent(parent);
            gridRoot.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            TopGridController controller = gridRoot.AddComponent<TopGridController>();
            controller.Configure(bootstrap, marblePool, palette, camera);
        }

        private static StadiumConveyorController CreateConveyor(
            Transform parent,
            GameBootstrap bootstrap,
            Material conveyor,
            Material conveyorSlot)
        {
            GameObject conveyorRoot = new GameObject("Stadium Conveyor");
            conveyorRoot.transform.SetParent(parent);
            conveyorRoot.transform.localPosition = new Vector3(0f, -3.25f, 0f);

            CreateVisual(
                "Track Center",
                PrimitiveType.Cube,
                conveyorRoot.transform,
                Vector3.zero,
                new Vector3(7f, 1.5f, 0.35f),
                Quaternion.identity,
                conveyor,
                false);

            CreateVisual(
                "Track Left Cap",
                PrimitiveType.Cylinder,
                conveyorRoot.transform,
                new Vector3(-3.5f, 0f, 0f),
                new Vector3(0.75f, 0.18f, 0.75f),
                Quaternion.Euler(90f, 0f, 0f),
                conveyor,
                false);

            CreateVisual(
                "Track Right Cap",
                PrimitiveType.Cylinder,
                conveyorRoot.transform,
                new Vector3(3.5f, 0f, 0f),
                new Vector3(0.75f, 0.18f, 0.75f),
                Quaternion.Euler(90f, 0f, 0f),
                conveyor,
                false);

            List<Transform> slotViews = new List<Transform>(24);
            for (int index = 0; index < 24; index++)
            {
                GameObject slot = CreateVisual(
                    $"Slot {index + 1:00}",
                    PrimitiveType.Sphere,
                    conveyorRoot.transform,
                    Vector3.zero,
                    new Vector3(0.28f, 0.2f, 0.1f),
                    Quaternion.identity,
                    conveyorSlot,
                    false);
                slotViews.Add(slot.transform);
            }

            StadiumConveyorController controller = conveyorRoot.AddComponent<StadiumConveyorController>();
            controller.Configure(bootstrap, 24, 4f, 7f, 0.75f, slotViews.ToArray());
            return controller;
        }

        private static void CreateConveyorEntrance(
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

            CreateVisual(
                "Left Entrance Guide",
                PrimitiveType.Cube,
                entrance.transform,
                new Vector3(-0.34f, 0f, 0f),
                new Vector3(0.12f, 0.9f, 0.6f),
                Quaternion.identity,
                border,
                true);

            CreateVisual(
                "Right Entrance Guide",
                PrimitiveType.Cube,
                entrance.transform,
                new Vector3(0.34f, 0f, 0f),
                new Vector3(0.12f, 0.9f, 0.6f),
                Quaternion.identity,
                border,
                true);

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
        }

        private static void CreateReceiverPreview(
            Transform parent,
            Material slotMaterial,
            Material green,
            Material blue,
            Material orange,
            Material yellow)
        {
            GameObject receiversRoot = new GameObject("Receiver Queue Preview");
            receiversRoot.transform.SetParent(parent);

            Material[][] laneColors =
            {
                new[] { green, orange, blue, yellow },
                new[] { blue, orange, yellow, blue },
                new[] { blue, green, green, blue },
                new[] { green, green, yellow, orange }
            };

            for (int laneIndex = 0; laneIndex < laneColors.Length; laneIndex++)
            {
                GameObject lane = new GameObject($"Receiver Lane {laneIndex + 1:00}");
                lane.transform.SetParent(receiversRoot.transform);
                lane.transform.localPosition = new Vector3(-2.7f + (laneIndex * 1.8f), -4.65f, 0f);

                for (int boxIndex = 0; boxIndex < laneColors[laneIndex].Length; boxIndex++)
                {
                    GameObject box = CreateVisual(
                        $"Receiver {boxIndex + 1:00}",
                        PrimitiveType.Cube,
                        lane.transform,
                        new Vector3(0f, -(boxIndex * 0.68f), 0f),
                        new Vector3(1.45f, 0.56f, 0.35f),
                        Quaternion.identity,
                        laneColors[laneIndex][boxIndex],
                        false);

                    if (boxIndex == 0)
                    {
                        for (int holeIndex = 0; holeIndex < 3; holeIndex++)
                        {
                            CreateVisual(
                                $"Capacity {holeIndex + 1}",
                                PrimitiveType.Sphere,
                                box.transform,
                                new Vector3(-0.35f + (holeIndex * 0.35f), 0f, -0.22f),
                                new Vector3(0.13f, 0.13f, 0.06f),
                                Quaternion.identity,
                                slotMaterial,
                                false);
                        }
                    }
                }
            }
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

            return material;
        }
    }
}
