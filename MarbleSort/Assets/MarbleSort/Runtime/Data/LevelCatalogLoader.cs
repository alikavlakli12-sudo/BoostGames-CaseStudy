using System;
using UnityEngine;

namespace MarbleSort.Data
{
    public static class LevelCatalogLoader
    {
        public static LevelCatalogData Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("The level catalog JSON is empty.", nameof(json));
            }

            LevelCatalogData catalog = JsonUtility.FromJson<LevelCatalogData>(json);
            if (catalog == null)
            {
                throw new InvalidOperationException("Unity could not deserialize the level catalog JSON.");
            }

            return catalog;
        }
    }
}
