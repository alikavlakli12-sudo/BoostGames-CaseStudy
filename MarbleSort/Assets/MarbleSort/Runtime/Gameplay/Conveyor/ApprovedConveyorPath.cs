using UnityEngine;

namespace MarbleSort.Gameplay.Conveyor
{
    /// <summary>
    /// The canonical conveyor loop measured directly from the finally oriented
    /// approved artwork. Both the rendered sockets and the mechanical marble
    /// anchors use this same Catmull-Rom path, eliminating any invisible track.
    /// </summary>
    public static class ApprovedConveyorPath
    {
        public const int SlotCount = 24;
        public const float EntranceNormalizedDistance = 5f / SlotCount;

        private static readonly Vector2[] ControlPoints =
        {
            new Vector2( 2.9312f,  0.4288f),
            new Vector2( 2.3707f,  0.4595f),
            new Vector2( 1.7747f,  0.4595f),
            new Vector2( 1.1698f,  0.4595f),
            new Vector2( 0.5738f,  0.4595f),
            new Vector2(-0.0133f,  0.4595f),
            new Vector2(-0.6005f,  0.4595f),
            new Vector2(-1.1965f,  0.4595f),
            new Vector2(-1.7836f,  0.4595f),
            new Vector2(-2.3796f,  0.4595f),
            new Vector2(-2.9401f,  0.4288f),
            new Vector2(-3.3048f, -0.0316f),
            new Vector2(-2.9490f, -0.5431f),
            new Vector2(-2.3796f, -0.5738f),
            new Vector2(-1.7836f, -0.5738f),
            new Vector2(-1.1965f, -0.5738f),
            new Vector2(-0.6094f, -0.5738f),
            new Vector2(-0.0133f, -0.5635f),
            new Vector2( 0.5738f, -0.5738f),
            new Vector2( 1.1698f, -0.5738f),
            new Vector2( 1.7658f, -0.5738f),
            new Vector2( 2.3707f, -0.5738f),
            new Vector2( 2.9312f, -0.5431f),
            new Vector2( 3.2870f, -0.0316f)
        };

        public static StadiumPose Evaluate(float normalizedDistance)
        {
            float parameter = Mathf.Repeat(normalizedDistance, 1f) * SlotCount;
            int segment = Mathf.FloorToInt(parameter) % SlotCount;
            float t = parameter - Mathf.Floor(parameter);

            Vector2 p0 = ControlPoints[Wrap(segment - 1)];
            Vector2 p1 = ControlPoints[segment];
            Vector2 p2 = ControlPoints[Wrap(segment + 1)];
            Vector2 p3 = ControlPoints[Wrap(segment + 2)];
            float t2 = t * t;
            float t3 = t2 * t;

            Vector2 position = 0.5f *
                ((2f * p1) +
                 ((-p0 + p2) * t) +
                 (((2f * p0) - (5f * p1) + (4f * p2) - p3) * t2) +
                 ((-p0 + (3f * p1) - (3f * p2) + p3) * t3));

            Vector2 tangent = 0.5f *
                ((-p0 + p2) +
                 (2f * ((2f * p0) - (5f * p1) + (4f * p2) - p3) * t) +
                 (3f * (-p0 + (3f * p1) - (3f * p2) + p3) * t2));

            if (tangent.sqrMagnitude <= Mathf.Epsilon)
            {
                tangent = Vector2.left;
            }

            return new StadiumPose(
                new Vector3(position.x, position.y, 0f),
                new Vector3(tangent.x, tangent.y, 0f).normalized);
        }

        private static int Wrap(int index)
        {
            return (index % SlotCount + SlotCount) % SlotCount;
        }
    }
}
