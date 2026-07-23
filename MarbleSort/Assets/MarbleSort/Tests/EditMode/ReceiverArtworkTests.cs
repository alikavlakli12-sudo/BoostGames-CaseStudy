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
        [TestCase("pink")]
        public void EverySupportedColor_LoadsAllCompleteApprovedReceiverStates(string colorId)
        {
            Assert.That(ReceiverArtworkLibrary.TryGet(colorId, out ReceiverArtwork artwork), Is.True);
            Assert.That(artwork.IsValid, Is.True);
            Assert.That(artwork.OpenFrames.Length, Is.EqualTo(ReceiverArtworkLibrary.OpenFrameCount));
            Assert.That(artwork.Box, Is.SameAs(artwork.GetOpenFrame(0)));
            Assert.That(artwork.Ball.texture, Is.Not.Null);
            Assert.That(artwork.Cap, Is.SameAs(artwork.Closed));
            Assert.That(artwork.Closed.texture.width, Is.EqualTo(768));
            Assert.That(artwork.Closed.texture.height, Is.EqualTo(416));
            Assert.That(artwork.Closed.rect.width / artwork.Closed.rect.height, Is.InRange(1.84f, 1.85f));
            Assert.That(artwork.Ball.rect.width / artwork.Ball.rect.height, Is.InRange(0.95f, 1.05f));
            Assert.That(artwork.Closed.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(artwork.Closed.texture.filterMode, Is.EqualTo(FilterMode.Trilinear));
            Assert.That(artwork.Closed.texture.mipmapCount, Is.GreaterThan(1));

            for (int count = 0; count < ReceiverArtworkLibrary.OpenFrameCount; count++)
            {
                Sprite frame = artwork.GetOpenFrame(count);
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.name, Does.EndWith($"{count:00}"));
                Assert.That(frame.texture.width, Is.EqualTo(768));
                Assert.That(frame.texture.height, Is.EqualTo(416));
                Assert.That(frame.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(frame.texture.filterMode, Is.EqualTo(FilterMode.Trilinear));
                Assert.That(frame.texture.mipmapCount, Is.GreaterThan(1));
            }
        }

        [TestCase("green")]
        [TestCase("blue")]
        [TestCase("orange")]
        [TestCase("yellow")]
        [TestCase("pink")]
        public void EverySupportedColor_LoadsAllApprovedBakedTopTrayOccupancyFrames(string colorId)
        {
            Assert.That(TopTrayArtworkLibrary.TryGet(colorId, out TopTrayArtwork topTray), Is.True);
            Assert.That(ReceiverArtworkLibrary.TryGet(colorId, out ReceiverArtwork receiver), Is.True);
            Assert.That(topTray.IsValid, Is.True);
            Assert.That(
                topTray.OccupancyFrames.Length,
                Is.EqualTo(TopTrayArtworkLibrary.OccupancyFrameCount));
            Assert.That(topTray.Tray, Is.SameAs(topTray.GetFrame(9)));
            Assert.That(topTray.Tray.rect.width / topTray.Tray.rect.height, Is.InRange(1.03f, 1.04f));
            Assert.That(topTray.Tray.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(topTray.Tray.texture.filterMode, Is.EqualTo(FilterMode.Trilinear));
            Assert.That(topTray.Tray.texture.mipmapCount, Is.GreaterThan(1));
            Assert.That(topTray.Ball, Is.SameAs(receiver.Ball));

            for (int remainingCount = 0;
                 remainingCount < TopTrayArtworkLibrary.OccupancyFrameCount;
                 remainingCount++)
            {
                Sprite frame = topTray.GetFrame(remainingCount);
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.name, Does.EndWith($"{remainingCount:00}"));
                Assert.That(frame.texture.width, Is.EqualTo(512));
                Assert.That(frame.texture.height, Is.EqualTo(495));
                Assert.That(frame.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(frame.texture.filterMode, Is.EqualTo(FilterMode.Trilinear));
                Assert.That(frame.texture.mipmapCount, Is.GreaterThan(1));
            }
        }

        [Test]
        public void ReceiverCompletionStar_LoadsApprovedTransparentArtwork()
        {
            Assert.That(
                ReceiverCompletionStarArtworkLibrary.TryGet(out Sprite star),
                Is.True);
            Assert.That(star, Is.Not.Null);
            Assert.That(star.name, Is.EqualTo("Approved Receiver Completion Star"));
            Assert.That(star.texture.width, Is.EqualTo(1254));
            Assert.That(star.texture.height, Is.EqualTo(1254));
            Assert.That(star.rect.width / star.rect.height, Is.InRange(1.01f, 1.03f));
            Assert.That(star.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(star.texture.mipmapCount, Is.EqualTo(1));
        }
    }
}
