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
        public void EverySupportedColor_LoadsTrimmedReceiverAndBallArtwork(string colorId)
        {
            Assert.That(ReceiverArtworkLibrary.TryGet(colorId, out ReceiverArtwork artwork), Is.True);
            Assert.That(artwork.IsValid, Is.True);
            Assert.That(artwork.Tray.texture, Is.Not.Null);
            Assert.That(artwork.Ball.texture, Is.Not.Null);
            Assert.That(artwork.Tray.rect.width / artwork.Tray.rect.height, Is.InRange(2.3f, 2.5f));
            Assert.That(artwork.Ball.rect.width / artwork.Ball.rect.height, Is.InRange(0.95f, 1.05f));
            Assert.That(artwork.Tray.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(artwork.Tray.texture.mipmapCount, Is.EqualTo(1));
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
            Assert.That(topTray.Tray.rect.width / topTray.Tray.rect.height, Is.InRange(0.99f, 1.02f));
            Assert.That(topTray.Tray.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(topTray.Tray.texture.mipmapCount, Is.EqualTo(1));
            Assert.That(topTray.Ball, Is.SameAs(receiver.Ball));
        }
    }
}
