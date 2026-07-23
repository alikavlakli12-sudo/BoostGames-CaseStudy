using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Loads the approved flat receiver-queue lane as one trimmed sprite. The
    /// visual is intentionally independent from receiver boxes so advancing or
    /// clearing a queue never rebuilds, moves, or removes its designated lane.
    /// </summary>
    public static class ReceiverQueueLaneArtworkLibrary
    {
        private const string ResourcePath =
            "Presentation/ReceiverLanes/ReceiverQueueLane";

        // The source is a 1254 px square chroma-key extraction. Keep a two-pixel
        // transparent safety margin around the approved 388 x 1068 px artwork.
        private static readonly Rect ApprovedCrop = ReceiverArtworkLibrary.Normalize(
            new Rect(430f, 92f, 392f, 1072f),
            1254f,
            1254f);

        private static Sprite cached;

        public static bool TryGet(out Sprite lane)
        {
            if (cached != null)
            {
                lane = cached;
                return true;
            }

            Texture2D texture = Resources.Load<Texture2D>(ResourcePath);
            if (texture == null)
            {
                Debug.LogError("Approved flat receiver queue lane artwork is missing.");
                lane = null;
                return false;
            }

            cached = ReceiverArtworkLibrary.CreateTrimmedSprite(
                texture,
                ApprovedCrop,
                "Approved Flat Receiver Queue Lane");
            lane = cached;
            return true;
        }
    }
}
