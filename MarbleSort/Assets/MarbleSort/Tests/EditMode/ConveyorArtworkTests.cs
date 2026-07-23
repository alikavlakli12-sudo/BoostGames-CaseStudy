using MarbleSort.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MarbleSort.Tests.EditMode
{
    public sealed class ConveyorArtworkTests
    {
        [Test]
        public void ConveyorAnimation_LoadsFromOneMobileCompressedAtlas()
        {
            Assert.That(
                ConveyorArtworkLibrary.TryGetAnimation(out ConveyorAnimationAsset animation),
                Is.True);
            Assert.That(animation, Is.Not.Null);
            Assert.That(animation.Atlas, Is.Not.Null);
            Assert.That(animation.FrameGeometrySprite, Is.Not.Null);
            Assert.That(animation.Material, Is.Not.Null);
            StringAssert.EndsWith(
                "/ConveyorAnimationAtlas.png",
                AssetDatabase.GetAssetPath(animation.Atlas));
            Assert.That(animation.Atlas.width, Is.EqualTo(ConveyorArtworkLibrary.AtlasWidth));
            Assert.That(animation.Atlas.height, Is.EqualTo(ConveyorArtworkLibrary.AtlasHeight));
            Assert.That(animation.Atlas.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(animation.Atlas.mipmapCount, Is.EqualTo(1));
            Assert.That(animation.Atlas.isReadable, Is.False);
            Assert.That(
                animation.FrameGeometrySprite.rect,
                Is.EqualTo(new Rect(
                    ConveyorArtworkLibrary.AtlasPadding,
                    ConveyorArtworkLibrary.AtlasHeight -
                    ConveyorArtworkLibrary.AtlasCellHeight +
                    ConveyorArtworkLibrary.AtlasPadding,
                    ConveyorArtworkLibrary.AnimationFrameWidth,
                    ConveyorArtworkLibrary.AnimationFrameHeight)));
            Assert.That(animation.FrameCount, Is.EqualTo(192));
            Assert.That(animation.Columns, Is.EqualTo(8));
            Assert.That(animation.Rows, Is.EqualTo(24));
            Assert.That(animation.Padding, Is.EqualTo(4));
            Assert.That(
                animation.Material.shader.name,
                Is.EqualTo("Marble Sort/Conveyor Animation Atlas"));

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(animation.Atlas));
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.isReadable, Is.False);
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.maxTextureSize, Is.EqualTo(8192));
            Assert.That(
                importer.textureCompression,
                Is.EqualTo(TextureImporterCompression.CompressedHQ));

            TextureImporterPlatformSettings ios =
                importer.GetPlatformTextureSettings("iPhone");
            Assert.That(ios.overridden, Is.True);
            Assert.That(ios.maxTextureSize, Is.EqualTo(8192));
            Assert.That(ios.format, Is.EqualTo(TextureImporterFormat.ASTC_5x5));
            Assert.That(
                ios.textureCompression,
                Is.EqualTo(TextureImporterCompression.CompressedHQ));

            TextureImporterPlatformSettings android =
                importer.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden, Is.False);
            Assert.That(android.maxTextureSize, Is.EqualTo(8192));

            Assert.That(
                Resources.LoadAll<Texture2D>("Presentation/Conveyor/Animation").Length,
                Is.Zero,
                "Individual conveyor frames must remain outside Resources and the player build.");

            Assert.That(
                ConveyorArtworkLibrary.TryGetAnimation(out ConveyorAnimationAsset cached),
                Is.True);
            Assert.That(cached, Is.SameAs(animation));
        }
    }
}
