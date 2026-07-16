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
    }
}
