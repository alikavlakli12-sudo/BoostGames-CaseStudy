using System;
using UnityEngine;

namespace MarbleSort.Gameplay.TopGrid
{
    public static class MarbleReleasePattern
    {
        public const int MarbleCount = 9;

        public static Vector3 GetLocalPosition(int index, float spacing = 0.24f, float depth = -0.24f)
        {
            if (index < 0 || index >= MarbleCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            int column = index % 3;
            int row = index / 3;
            return new Vector3((column - 1) * spacing, (1 - row) * spacing, depth);
        }
    }
}
