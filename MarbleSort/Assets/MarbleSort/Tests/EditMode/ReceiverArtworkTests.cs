using MarbleSort.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace MarbleSort.Tests.EditMode
{
    public sealed class ReceiverArtworkTests
    {
        [TestCase("green")]
        [TestCase("blue")]
        [TestCase("orange")]
        [TestCase("yellow")]
        public void EverySupportedColor_LoadsThickReceiverBoxBallAndIndependentCapArtwork(string colorId)
        {
            Assert.That(ReceiverArtworkLibrary.TryGet(colorId, out ReceiverArtwork artwork), Is.True);
            Assert.That(artwork.IsValid, Is.True);
            Assert.That(artwork.Box.texture, Is.Not.Null);
            Assert.That(artwork.Ball.texture, Is.Not.Null);
            Assert.That(artwork.Cap.texture, Is.Not.Null);
            Assert.That(artwork.Box.rect.width / artwork.Box.rect.height, Is.InRange(1.7f, 1.8f));
            Assert.That(artwork.Ball.rect.width / artwork.Ball.rect.height, Is.InRange(0.95f, 1.05f));
            Assert.That(artwork.Cap.rect.width / artwork.Cap.rect.height, Is.InRange(2.4f, 2.5f));
            Assert.That(artwork.Box.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(artwork.Box.texture.mipmapCount, Is.EqualTo(1));
            Assert.That(artwork.Cap.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(artwork.Cap.texture.mipmapCount, Is.EqualTo(1));
        }

        [TestCase("green")]
        [TestCase("blue")]
        [TestCase("orange")]
        [TestCase("yellow")]
        public void EverySupportedColor_LoadsTrimmedTopTrayAndSharedBallArtwork(string colorId)
        {
            Assert.That(TopTrayArtworkLibrary.TryGet(colorId, out TopTrayArtwork topTray), Is.True);
            Assert.That(ReceiverArtworkLibrary.TryGet(colorId, out ReceiverArtwork receiver), Is.True);
            Assert.That(topTray.IsValid, Is.True);
            Assert.That(topTray.Tray.rect.width / topTray.Tray.rect.height, Is.InRange(1.03f, 1.04f));
            Assert.That(topTray.Tray.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(topTray.Tray.texture.filterMode, Is.EqualTo(FilterMode.Trilinear));
            Assert.That(topTray.Tray.texture.mipmapCount, Is.GreaterThan(1));
            Assert.That(topTray.Ball, Is.SameAs(receiver.Ball));
        }
    }
}
