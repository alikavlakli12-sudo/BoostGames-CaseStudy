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

        private static Material CreateDerived(Material source, Color color)
        {
            Material material = new Material(source)
            {
                color = color,
                hideFlags = HideFlags.HideAndDontSave
            };
            ConfigureSurface(material);
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
            ConfigureSurface(material);
            return material;
        }

        private static void ConfigureSurface(Material material)
        {
            if (material == null)
            {
                return;
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
        }
    }
}
