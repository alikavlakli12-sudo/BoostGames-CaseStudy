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
        public void PreviewTrayLayerMeshes_AreCachedAndMatchApprovedProportions()
        {
            Mesh lowerSide = PresentationMeshFactory.GetRoundedBoxMesh(0.96f, 0.91f, 0.16f, 0.14f);
            Mesh cachedLowerSide = PresentationMeshFactory.GetRoundedBoxMesh(0.96f, 0.91f, 0.16f, 0.14f);
            Mesh highlightRim = PresentationMeshFactory.GetRoundedBoxMesh(0.94f, 0.89f, 0.12f, 0.135f);
            Mesh face = PresentationMeshFactory.GetRoundedBoxMesh(0.86f, 0.81f, 0.08f, 0.105f, 8);

            Assert.That(cachedLowerSide, Is.SameAs(lowerSide));
            Assert.That(lowerSide.bounds.size.x, Is.EqualTo(0.96f).Within(0.001f));
            Assert.That(lowerSide.bounds.size.y, Is.EqualTo(0.91f).Within(0.001f));
            Assert.That(lowerSide.bounds.size.z, Is.EqualTo(0.16f).Within(0.001f));
            Assert.That(highlightRim.bounds.size.x, Is.EqualTo(0.94f).Within(0.001f));
            Assert.That(face.bounds.size.x, Is.EqualTo(0.86f).Within(0.001f));
            Assert.That(face.bounds.size.y, Is.EqualTo(0.81f).Within(0.001f));
        }

        [Test]
        public void GlossyBallMaterials_AreCachedAndConsistentAcrossProductionColors()
        {
            string[] materialPaths =
            {
                "Assets/MarbleSort/Art/Materials/Green.mat",
                "Assets/MarbleSort/Art/Materials/Blue.mat",
                "Assets/MarbleSort/Art/Materials/Orange.mat",
                "Assets/MarbleSort/Art/Materials/Yellow.mat"
            };

            for (int index = 0; index < materialPaths.Length; index++)
            {
                Material source = AssetDatabase.LoadAssetAtPath<Material>(materialPaths[index]);
                Assert.That(source, Is.Not.Null);

                Material first = PresentationMaterialLibrary.GetGlossyBall(source);
                Material second = PresentationMaterialLibrary.GetGlossyBall(source);
                Material cup = PresentationMaterialLibrary.GetCup(source);

                Assert.That(second, Is.SameAs(first));
                Assert.That(first, Is.Not.SameAs(source));
                Assert.That(first.color.r, Is.EqualTo(source.color.r).Within(0.0001f));
                Assert.That(first.color.g, Is.EqualTo(source.color.g).Within(0.0001f));
                Assert.That(first.color.b, Is.EqualTo(source.color.b).Within(0.0001f));
                Assert.That(first.color.a, Is.EqualTo(source.color.a).Within(0.0001f));
                Assert.That(first.enableInstancing, Is.True);
                Assert.That(first.GetFloat("_Glossiness"), Is.EqualTo(0.72f).Within(0.001f));
                Assert.That(first.GetFloat("_Metallic"), Is.Zero.Within(0.001f));
                Assert.That(cup.color.grayscale, Is.LessThan(source.color.grayscale));
            }
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
