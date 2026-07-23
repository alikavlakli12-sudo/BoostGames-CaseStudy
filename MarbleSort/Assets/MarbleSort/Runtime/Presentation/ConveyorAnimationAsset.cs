using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Immutable runtime description of the baked conveyor animation atlas.
    /// One texture, one geometry sprite, and one shared material replace the
    /// former array of 192 textures and runtime-created sprites.
    /// </summary>
    public sealed class ConveyorAnimationAsset
    {
        internal ConveyorAnimationAsset(
            Texture2D atlas,
            Sprite frameGeometrySprite,
            Material material,
            int frameCount,
            int columns,
            int rows,
            int padding)
        {
            Atlas = atlas;
            FrameGeometrySprite = frameGeometrySprite;
            Material = material;
            FrameCount = frameCount;
            Columns = columns;
            Rows = rows;
            Padding = padding;
        }

        public Texture2D Atlas { get; }

        public Sprite FrameGeometrySprite { get; }

        public Material Material { get; }

        public int FrameCount { get; }

        public int Columns { get; }

        public int Rows { get; }

        public int Padding { get; }
    }
}
