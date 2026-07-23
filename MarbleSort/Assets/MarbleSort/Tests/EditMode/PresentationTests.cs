using MarbleSort.Presentation;
using MarbleSort.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MarbleSort.Tests.EditMode
{
    public sealed class PresentationTests
    {
        private const string BackgroundPath =
            "Assets/MarbleSort/Art/Textures/PortraitEnvironmentReceiverBayLowered.png";
        private const string BackgroundMaterialPath =
            "Assets/MarbleSort/Art/Materials/PortraitBackground.mat";
        private const string ApprovedHudPath =
            "Assets/MarbleSort/Resources/Presentation/UI/Approved/PremiumTopHudPlateAqua.png";
        private const string ApprovedSheetSurfacePath =
            "Assets/MarbleSort/Resources/Presentation/Surround/Approved/AquaSheetSurface.png";
        private const string ApprovedSheetRimPath =
            "Assets/MarbleSort/Resources/Presentation/Surround/Approved/AquaSheetRimProfile.png";
        private const string ApprovedBakedSheetFolder =
            "Assets/MarbleSort/Resources/Presentation/Surround/Approved/Baked";
        private const string ApprovedClearedTraySpotPath =
            "Assets/MarbleSort/Resources/Presentation/TopGrid/ClearedTraySpot.png";
        private const string ApprovedReceiverLanePath =
            "Assets/MarbleSort/Resources/Presentation/ReceiverLanes/ReceiverQueueLane.png";
        private const string ApprovedHiddenTrayFolder =
            "Assets/MarbleSort/Resources/Presentation/TopTrays/Hidden";
        private const string ApprovedCompletionFolder =
            "Assets/MarbleSort/Resources/Presentation/UI/Completion";

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
            Assert.That(first.vertexCount, Is.EqualTo((samples + 1) * 3));
            Assert.That(first.triangles.Length, Is.EqualTo(samples * 12));
            Assert.That(first.bounds.size.x, Is.GreaterThan(7f));
            Assert.That(first.bounds.size.y, Is.GreaterThan(1f));
            Vector3[] vertices = first.vertices;
            Vector2[] uvs = first.uv;
            Assert.That(vertices[vertices.Length - 3], Is.EqualTo(vertices[0]));
            Assert.That(vertices[vertices.Length - 2], Is.EqualTo(vertices[1]));
            Assert.That(vertices[vertices.Length - 1], Is.EqualTo(vertices[2]));
            Assert.That(uvs[0].x, Is.EqualTo(0f));
            Assert.That(uvs[1].y, Is.EqualTo(0.5f));
            Assert.That(uvs[uvs.Length - 3].x, Is.EqualTo(1f));
            Assert.That(uvs[uvs.Length - 2].x, Is.EqualTo(1f));
            Assert.That(uvs[uvs.Length - 1].x, Is.EqualTo(1f));

            Mesh asymmetric = PresentationMeshFactory.GetStadiumRibbonMesh(
                7f,
                0.75f,
                0.3f,
                0.2f,
                samples);
            Vector3[] asymmetricVertices = asymmetric.vertices;
            Assert.That(asymmetricVertices[0].y, Is.EqualTo(0.45f).Within(0.001f));
            Assert.That(asymmetricVertices[1].y, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(asymmetricVertices[2].y, Is.EqualTo(0.95f).Within(0.001f));
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
                "Assets/MarbleSort/Art/Materials/Yellow.mat",
                "Assets/MarbleSort/Art/Materials/Pink.mat"
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
        public void PremiumPortraitBackground_IsImportedForMobilePresentation()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(BackgroundMaterialPath);

            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(853));
            Assert.That(texture.height, Is.EqualTo(1844));
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(material, Is.Not.Null);
            Assert.That(material.mainTexture, Is.SameAs(texture));
        }

        [Test]
        public void ApprovedHudAndSheetArtwork_LoadWithoutCompressionOrMipBleeding()
        {
            AssertApprovedTexture(ApprovedHudPath, 853, 377, true);
            AssertApprovedTexture(ApprovedSheetSurfacePath, 1254, 1254, false);
            AssertApprovedTexture(ApprovedSheetRimPath, 512, 1024, false);
            AssertApprovedTexture(ApprovedClearedTraySpotPath, 1254, 1254, true);
            AssertApprovedTexture(ApprovedReceiverLanePath, 1254, 1254, true);

            string[] bakedSheetGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { ApprovedBakedSheetFolder });
            Assert.That(bakedSheetGuids.Length, Is.GreaterThanOrEqualTo(5));
            for (int index = 0; index < bakedSheetGuids.Length; index++)
            {
                AssertApprovedTexture(
                    AssetDatabase.GUIDToAssetPath(bakedSheetGuids[index]),
                    PremiumSheetArtworkLibrary.TextureWidth,
                    PremiumSheetArtworkLibrary.TextureHeight,
                    true);
            }

            Assert.That(ClearedTraySpotArtworkLibrary.TryGet(out Sprite traySpot), Is.True);
            Assert.That(traySpot, Is.Not.Null);
            Assert.That(traySpot.name, Is.EqualTo("Approved Cleared Tray Spot"));

            Assert.That(ReceiverQueueLaneArtworkLibrary.TryGet(out Sprite receiverLane), Is.True);
            Assert.That(receiverLane, Is.Not.Null);
            Assert.That(receiverLane.name, Is.EqualTo("Approved Flat Receiver Queue Lane"));
            Assert.That(receiverLane.bounds.size.y, Is.GreaterThan(receiverLane.bounds.size.x * 2.5f));
        }

        [Test]
        public void ApprovedHiddenTopTrayArtwork_LoadsAsFourBakedColorSprites()
        {
            string[] colors = { "green", "blue", "orange", "yellow" };
            string[] textureGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { ApprovedHiddenTrayFolder });
            Assert.That(textureGuids.Length, Is.EqualTo(colors.Length));

            for (int index = 0; index < colors.Length; index++)
            {
                Assert.That(HiddenTopTrayArtworkLibrary.TryGet(colors[index], out Sprite sprite), Is.True);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.name, Does.StartWith("Approved Thin Hidden Tray "));
                Assert.That(sprite.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(sprite.texture.mipmapCount, Is.EqualTo(1));

                string path = AssetDatabase.GetAssetPath(sprite.texture);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
            }
        }

        [Test]
        public void ApprovedMysteryTopTrayArtwork_LoadsAsOneTransparentBakedSprite()
        {
            Assert.That(MysteryTopTrayArtworkLibrary.TryGet(out Sprite sprite), Is.True);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.name, Is.EqualTo("Approved Mystery Top Tray"));
            Assert.That(sprite.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(sprite.texture.mipmapCount, Is.EqualTo(1));

            string path = AssetDatabase.GetAssetPath(sprite.texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.alphaIsTransparency, Is.True, path);
            Assert.That(
                importer.textureCompression,
                Is.EqualTo(TextureImporterCompression.Uncompressed),
                path);
        }

        [Test]
        public void ApprovedCompletionStates_AreUncompressedTransparentProductionTextures()
        {
            int[] percentages = { 40, 60, 80, 100 };
            for (int index = 0; index < percentages.Length; index++)
            {
                string path =
                    $"{ApprovedCompletionFolder}/MarbleStarCompletion{percentages[index]}.png";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.GreaterThan(900), path);
                Assert.That(texture.height, Is.GreaterThan(1370), path);
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                Assert.That(
                    importer.textureCompression,
                    Is.EqualTo(TextureImporterCompression.Uncompressed),
                    path);
            }
        }

        [Test]
        public void ApprovedMysteryBoxCompletion_IsAnUncompressedTransparentProductionTexture()
        {
            string path = $"{ApprovedCompletionFolder}/MysteryBoxCompletion.png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.That(texture, Is.Not.Null, path);
            Assert.That(texture.width, Is.GreaterThan(850), path);
            Assert.That(texture.height, Is.GreaterThan(1270), path);
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.alphaIsTransparency, Is.True, path);
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
            Assert.That(
                importer.textureCompression,
                Is.EqualTo(TextureImporterCompression.Uncompressed),
                path);
        }

        [Test]
        public void CompletionProgressFill_IsAlwaysContainedByItsInnerTrack()
        {
            Rect card = new Rect(60f, 194f, 600f, 900f);
            Rect track = GameHudView.CalculateCompletionProgressTrackRect(card);
            float[] fractions = { -1f, 0f, 0.4f, 0.6f, 0.8f, 1f, 2f };

            Assert.That(track.xMin, Is.GreaterThan(card.xMin));
            Assert.That(track.yMin, Is.GreaterThan(card.yMin));
            Assert.That(track.xMax, Is.LessThan(card.xMax));
            Assert.That(track.yMax, Is.LessThan(card.yMax));

            for (int index = 0; index < fractions.Length; index++)
            {
                Rect fill = GameHudView.CalculateCompletionProgressFillRect(
                    track,
                    fractions[index]);
                Assert.That(fill.xMin, Is.EqualTo(track.xMin).Within(0.001f));
                Assert.That(fill.yMin, Is.EqualTo(track.yMin).Within(0.001f));
                Assert.That(fill.xMax, Is.LessThanOrEqualTo(track.xMax + 0.001f));
                Assert.That(fill.yMax, Is.LessThanOrEqualTo(track.yMax + 0.001f));
                Assert.That(fill.width, Is.GreaterThanOrEqualTo(0f));
            }
        }

        private static void AssertApprovedTexture(
            string path,
            int expectedWidth,
            int expectedHeight,
            bool expectsAlpha)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.That(texture, Is.Not.Null, path);
            Assert.That(texture.width, Is.EqualTo(expectedWidth), path);
            Assert.That(texture.height, Is.EqualTo(expectedHeight), path);
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(
                importer.textureCompression,
                Is.EqualTo(TextureImporterCompression.Uncompressed),
                path);
            Assert.That(importer.alphaIsTransparency, Is.EqualTo(expectsAlpha), path);
        }
    }
}
