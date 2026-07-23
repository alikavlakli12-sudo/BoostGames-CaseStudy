using System.Collections.Generic;
using UnityEngine;

namespace MarbleSort.Presentation
{
    public static class PresentationMaterialLibrary
    {
        // One restrained cast-shadow treatment shared by the premium sheet and
        // both top-tray states. Keeping these values in one place prevents the
        // presentation layers from drifting into different light directions.
        public static readonly Vector2 LightCastShadowOffset =
            new Vector2(-0.055f, -0.065f);

        public static readonly Color LightCastShadowColor =
            new Color32(48, 62, 96, 36);

        private static readonly Dictionary<int, Material> DarkenedMaterials =
            new Dictionary<int, Material>();

        private static readonly Dictionary<int, Material> HighlightMaterials =
            new Dictionary<int, Material>();

        private static readonly Dictionary<int, Material> GlossyBallMaterials =
            new Dictionary<int, Material>();

        private static readonly Dictionary<int, Material> CupMaterials =
            new Dictionary<int, Material>();

        private static Material softShadow;
        private static Material brightWhite;
        private static Material trayFormationRim;
        private static Material trayFormationFill;
        private static Material trayFormationSidewall;
        private static Material trayFormationRimProfile;
        private static Material trayFormationBevel;
        private static Material trayFormationHighlight;

        public static Material GetDarkened(Material source)
        {
            if (source == null)
            {
                return GetSoftShadow();
            }

            int key = source.GetInstanceID();
            if (!DarkenedMaterials.TryGetValue(key, out Material material))
            {
                Color color = source.color;
                material = CreateDerived(source, new Color(
                    color.r * 0.52f,
                    color.g * 0.52f,
                    color.b * 0.62f,
                    color.a));
                DarkenedMaterials.Add(key, material);
            }

            return material;
        }

        public static Material GetHighlight(Material source)
        {
            if (source == null)
            {
                return GetBrightWhite();
            }

            int key = source.GetInstanceID();
            if (!HighlightMaterials.TryGetValue(key, out Material material))
            {
                Color color = source.color;
                material = CreateDerived(source, Color.Lerp(color, Color.white, 0.3f));
                HighlightMaterials.Add(key, material);
            }

            return material;
        }

        public static Material GetGlossyBall(Material source)
        {
            if (source == null)
            {
                return GetBrightWhite();
            }

            int key = source.GetInstanceID();
            if (!GlossyBallMaterials.TryGetValue(key, out Material material))
            {
                material = CreateDerived(source, source.color, 0.72f, "Glossy Ball");
                GlossyBallMaterials.Add(key, material);
            }

            return material;
        }

        public static Material GetCup(Material source)
        {
            if (source == null)
            {
                return GetSoftShadow();
            }

            int key = source.GetInstanceID();
            if (!CupMaterials.TryGetValue(key, out Material material))
            {
                Color color = source.color;
                material = CreateDerived(source, new Color(
                    color.r * 0.34f,
                    color.g * 0.34f,
                    color.b * 0.42f,
                    color.a), 0.42f, "Cup Interior");
                CupMaterials.Add(key, material);
            }

            return material;
        }

        public static Material GetSoftShadow()
        {
            if (softShadow == null)
            {
                softShadow = CreateMaterial(new Color32(37, 48, 86, 155));
            }

            return softShadow;
        }

        public static Material GetBrightWhite()
        {
            if (brightWhite == null)
            {
                brightWhite = CreateMaterial(new Color32(245, 249, 255, 255));
            }

            return brightWhite;
        }

        public static Material GetTrayFormationRim()
        {
            if (trayFormationRim == null)
            {
                trayFormationRim = CreateLineMaterial(new Color32(26, 66, 116, 255));
                trayFormationRim.name = "Premium Sheet Navy Depth";
            }

            return trayFormationRim;
        }

        public static Material GetTrayFormationFill()
        {
            if (trayFormationFill == null)
            {
                Texture2D surface = Resources.Load<Texture2D>(
                    "Presentation/Surround/Approved/AquaSheetSurface");
                trayFormationFill = CreateTexturedMaterial(
                    surface,
                    Color.white,
                    "Approved Aqua Sheet Surface");
            }

            return trayFormationFill;
        }

        public static Material GetTrayFormationSidewall()
        {
            if (trayFormationSidewall == null)
            {
                Texture2D surface = Resources.Load<Texture2D>(
                    "Presentation/Surround/Approved/AquaSheetSurface");
                trayFormationSidewall = CreateTexturedMaterial(
                    surface,
                    new Color32(49, 119, 140, 255),
                    "Aqua Sheet Lower Sidewall",
                    preferTintableShader: true);
            }

            return trayFormationSidewall;
        }

        public static Material GetTrayFormationRimProfile()
        {
            if (trayFormationRimProfile == null)
            {
                Texture2D profile = Resources.Load<Texture2D>(
                    "Presentation/Surround/Approved/AquaSheetRimProfile");
                trayFormationRimProfile = CreateTexturedMaterial(
                    profile,
                    Color.white,
                    "Approved Aqua Sheet Rim Profile");
            }

            return trayFormationRimProfile;
        }

        public static Material GetTrayFormationBevel()
        {
            if (trayFormationBevel == null)
            {
                trayFormationBevel = CreateLineMaterial(new Color32(76, 163, 181, 255));
                trayFormationBevel.name = "Premium Sheet Aqua Inner Lip";
            }

            return trayFormationBevel;
        }

        public static Material GetTrayFormationHighlight()
        {
            if (trayFormationHighlight == null)
            {
                trayFormationHighlight = CreateLineMaterial(new Color32(215, 244, 249, 255));
                trayFormationHighlight.name = "Premium Sheet Pearlescent Highlight";
            }

            return trayFormationHighlight;
        }

        private static Material CreateDerived(
            Material source,
            Color color,
            float glossiness = 0.3f,
            string suffix = "Derived")
        {
            Material material = new Material(source)
            {
                color = color,
                name = $"{source.name} - {suffix}",
                hideFlags = HideFlags.HideAndDontSave
            };
            ConfigureSurface(material, glossiness);
            return material;
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader)
            {
                color = color,
                hideFlags = HideFlags.HideAndDontSave
            };
            ConfigureSurface(material, 0.3f);
            return material;
        }

        private static Material CreateLineMaterial(Color color)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader)
            {
                color = color,
                hideFlags = HideFlags.HideAndDontSave
            };
            material.enableInstancing = true;
            return material;
        }

        private static Material CreateSpriteMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader)
            {
                color = color,
                hideFlags = HideFlags.HideAndDontSave
            };
            material.enableInstancing = true;
            return material;
        }

        private static Material CreateTexturedMaterial(
            Texture texture,
            Color tint,
            string materialName,
            bool preferTintableShader = false)
        {
            Shader shader = preferTintableShader
                ? Shader.Find("Sprites/Default")
                : Shader.Find("Unlit/Texture");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader)
            {
                mainTexture = texture,
                name = materialName,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tint);
            }

            material.enableInstancing = true;
            return material;
        }

        private static void ConfigureSurface(Material material, float glossiness)
        {
            if (material == null)
            {
                return;
            }

            material.enableInstancing = true;
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", Mathf.Clamp01(glossiness));
            }

            if (material.HasProperty("_GlossMapScale"))
            {
                material.SetFloat("_GlossMapScale", Mathf.Clamp01(glossiness));
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }
        }
    }
}
