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

        public void Configure(Material greenMaterial, Material blueMaterial, Material orangeMaterial, Material yellowMaterial)
        {
            green = greenMaterial;
            blue = blueMaterial;
            orange = orangeMaterial;
            yellow = yellowMaterial;
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorId), colorId, "Unsupported marble color.");
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
