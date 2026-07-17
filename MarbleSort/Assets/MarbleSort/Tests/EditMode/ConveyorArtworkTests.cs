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
                "/ConveyorChassisRailFree.png",
                AssetDatabase.GetAssetPath(artwork.texture),
                "The conveyor must use the rail-free chassis so the center rail cannot be duplicated.");
            Assert.That(artwork.rect.width / artwork.rect.height, Is.InRange(3.45f, 3.55f));
            Assert.That(artwork.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(artwork.texture.mipmapCount, Is.EqualTo(1));

            Assert.That(ConveyorArtworkLibrary.TryGetSlot(out Sprite slot), Is.True);
            Assert.That(slot, Is.Not.Null);
            Assert.That(slot.rect.width / slot.rect.height, Is.InRange(0.65f, 0.72f));
            Assert.That(slot.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(slot.texture.mipmapCount, Is.EqualTo(1));

            Assert.That(ConveyorArtworkLibrary.TryGetRail(out Sprite rail), Is.True);
            Assert.That(rail, Is.Not.Null);
            Assert.That(rail.rect.width / rail.rect.height, Is.InRange(16.5f, 16.9f));
            Assert.That(rail.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(rail.texture.mipmapCount, Is.EqualTo(1));
        }
    }
}
