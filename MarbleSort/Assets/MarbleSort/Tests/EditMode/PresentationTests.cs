using MarbleSort.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MarbleSort.Tests.EditMode
{
    public sealed class PresentationTests
    {
        private const string BackgroundPath =
            "Assets/MarbleSort/Art/Textures/PortraitBackground.png";

        [Test]
        public void RoundedBoxMesh_IsCachedAndMatchesRequestedBounds()
        {
            Mesh first = PresentationMeshFactory.GetRoundedBoxMesh(2f, 1f, 0.3f, 0.2f, 5);
            Mesh second = PresentationMeshFactory.GetRoundedBoxMesh(2f, 1f, 0.3f, 0.2f, 5);

            Assert.That(second, Is.SameAs(first));
            Assert.That(first.bounds.size.x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(first.bounds.size.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(first.bounds.size.z, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(first.vertexCount, Is.GreaterThan(40));
        }

        [Test]
        public void StadiumRibbonMesh_IsCachedAndUsesOneSharedLoop()
        {
            const int samples = 96;
            Mesh first = PresentationMeshFactory.GetStadiumRibbonMesh(7f, 0.75f, 0.42f, samples);
            Mesh second = PresentationMeshFactory.GetStadiumRibbonMesh(7f, 0.75f, 0.42f, samples);

            Assert.That(second, Is.SameAs(first));
            Assert.That(first.vertexCount, Is.EqualTo(samples * 2));
            Assert.That(first.triangles.Length, Is.EqualTo(samples * 6));
            Assert.That(first.bounds.size.x, Is.GreaterThan(7f));
            Assert.That(first.bounds.size.y, Is.GreaterThan(1f));
        }

        [Test]
        public void ProceduralAudioBank_PrewarmsEveryRequiredFeedbackClip()
        {
            ProceduralAudioBank bank = new ProceduralAudioBank();
            AudioClip[] clips =
            {
                bank.Tap,
                bank.Admission,
                bank.Collection,
                bank.ReceiverComplete,
                bank.LevelComplete,
                bank.Deadlock
            };

            try
            {
                for (int index = 0; index < clips.Length; index++)
                {
                    Assert.That(clips[index], Is.Not.Null);
                    Assert.That(clips[index].samples, Is.GreaterThan(1000));
                    Assert.That(clips[index].frequency, Is.EqualTo(44100));
                    Assert.That(clips[index].channels, Is.EqualTo(1));
                }
            }
            finally
            {
                bank.Dispose();
            }
        }

        [Test]
        public void PortraitBackground_IsImportedForMobilePresentation()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;

            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(1024));
            Assert.That(texture.height, Is.EqualTo(1536));
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Compressed));
        }
    }
}
