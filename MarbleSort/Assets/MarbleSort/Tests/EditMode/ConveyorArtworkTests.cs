using MarbleSort.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MarbleSort.Tests.EditMode
{
    public sealed class ConveyorArtworkTests
    {
        [Test]
        public void ApprovedConveyorArtwork_LoadsWithMobileSafeImportSettings()
        {
            Assert.That(ConveyorArtworkLibrary.TryGet(out Sprite artwork), Is.True);
            Assert.That(artwork, Is.Not.Null);
            Assert.That(artwork.texture, Is.Not.Null);
            StringAssert.EndsWith(
                "/ConveyorApprovedReference.png",
                AssetDatabase.GetAssetPath(artwork.texture));
            Assert.That(artwork.texture.width, Is.EqualTo(2172));
            Assert.That(artwork.texture.height, Is.EqualTo(724));
            Assert.That(artwork.rect, Is.EqualTo(new Rect(100f, 154f, 1970f, 445f)));
            Assert.That(artwork.rect.width / artwork.rect.height, Is.EqualTo(1970f / 445f).Within(0.001f));
            Assert.That(artwork.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(artwork.texture.mipmapCount, Is.EqualTo(1));

            Assert.That(ConveyorArtworkLibrary.TryGetBeltLoop(out Texture2D beltLoop), Is.True);
            Assert.That(beltLoop, Is.Not.Null);
            StringAssert.EndsWith(
                "/ConveyorApprovedBeltLoop.png",
                AssetDatabase.GetAssetPath(beltLoop));
            Assert.That(beltLoop.width, Is.EqualTo(1920));
            Assert.That(beltLoop.height, Is.EqualTo(128));
            Assert.That(beltLoop.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
            Assert.That(beltLoop.mipmapCount, Is.EqualTo(1));

            Assert.That(ConveyorArtworkLibrary.TryGetAnimation(out Sprite[] frames), Is.True);
            Assert.That(
                frames.Length,
                Is.EqualTo(ConveyorArtworkLibrary.ExpectedAnimationFrameCount));
            Assert.That(
                frames[0].texture.width,
                Is.EqualTo(ConveyorArtworkLibrary.AnimationFrameWidth));
            Assert.That(
                frames[0].texture.height,
                Is.EqualTo(ConveyorArtworkLibrary.AnimationFrameHeight));
            Assert.That(frames[0].texture, Is.Not.SameAs(frames[24].texture));
            Assert.That(frames[0].texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(frames[0].texture.mipmapCount, Is.EqualTo(1));
        }
    }
}
