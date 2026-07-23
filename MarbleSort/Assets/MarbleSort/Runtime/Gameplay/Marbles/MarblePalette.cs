using System;
using UnityEngine;

namespace MarbleSort.Gameplay.Marbles
{
    [DisallowMultipleComponent]
    public sealed class MarblePalette : MonoBehaviour
    {
        [SerializeField] private Material green;
        [SerializeField] private Material blue;
        [SerializeField] private Material orange;
        [SerializeField] private Material yellow;
        [SerializeField] private Material pink;

        private Material runtimePinkFallback;

        public void Configure(Material greenMaterial, Material blueMaterial, Material orangeMaterial, Material yellowMaterial)
        {
            Configure(greenMaterial, blueMaterial, orangeMaterial, yellowMaterial, null);
        }

        public void Configure(
            Material greenMaterial,
            Material blueMaterial,
            Material orangeMaterial,
            Material yellowMaterial,
            Material pinkMaterial)
        {
            green = greenMaterial;
            blue = blueMaterial;
            orange = orangeMaterial;
            yellow = yellowMaterial;
            pink = pinkMaterial;
        }

        public Material GetMaterial(string colorId)
        {
            string normalized = Normalize(colorId);
            switch (normalized)
            {
                case "green":
                    return green;
                case "blue":
                    return blue;
                case "orange":
                    return orange;
                case "yellow":
                    return yellow;
                case "pink":
                    return pink != null ? pink : GetOrCreatePinkFallback();
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorId), colorId, "Unsupported marble color.");
            }
        }

        private Material GetOrCreatePinkFallback()
        {
            if (runtimePinkFallback != null)
            {
                return runtimePinkFallback;
            }

            Shader shader = Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            runtimePinkFallback = new Material(shader)
            {
                name = "Pink (Runtime Fallback)",
                color = new Color32(246, 78, 178, 255),
                hideFlags = HideFlags.DontSave
            };
            return runtimePinkFallback;
        }

        private void OnDestroy()
        {
            if (runtimePinkFallback != null)
            {
                Destroy(runtimePinkFallback);
            }
        }

        public static string Normalize(string colorId)
        {
            if (string.IsNullOrWhiteSpace(colorId))
            {
                throw new ArgumentException("A marble color is required.", nameof(colorId));
            }

            return colorId.Trim().ToLowerInvariant();
        }
    }
}
