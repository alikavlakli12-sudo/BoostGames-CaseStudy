using System.Collections.Generic;
using UnityEngine;

namespace MarbleSort.Presentation
{
    public static class PresentationMaterialLibrary
    {
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
