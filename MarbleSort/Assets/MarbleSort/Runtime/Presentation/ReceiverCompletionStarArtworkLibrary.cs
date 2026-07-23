using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Loads the approved pearlescent completion star as one transparent sprite.
    /// The measured crop removes the source canvas without changing any star pixels.
    /// </summary>
    public static class ReceiverCompletionStarArtworkLibrary
    {
        private const string ResourcePath =
            "Presentation/Effects/ReceiverCompletionStar";
        private static readonly Rect StarCrop = ReceiverArtworkLibrary.Normalize(
            new Rect(200f, 229f, 857f, 840f),
            1254f,
            1254f);

        private static Sprite cachedStar;

        public static bool TryGet(out Sprite star)
        {
            if (cachedStar != null)
            {
                star = cachedStar;
                return true;
            }

            Texture2D texture = Resources.Load<Texture2D>(ResourcePath);
            if (texture == null)
            {
                Debug.LogError("Approved receiver-completion star artwork is missing.");
                star = null;
                return false;
            }

            cachedStar = ReceiverArtworkLibrary.CreateTrimmedSprite(
                texture,
                StarCrop,
                "Approved Receiver Completion Star");
            star = cachedStar;
            return true;
        }
    }
}
