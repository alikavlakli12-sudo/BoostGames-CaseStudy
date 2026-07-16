using System;
using UnityEngine;

namespace MarbleSort.Gameplay.Conveyor
{
    public readonly struct StadiumPose
    {
        public StadiumPose(Vector3 position, Vector3 tangent)
        {
            Position = position;
            Tangent = tangent;
        }

        public Vector3 Position { get; }

        public Vector3 Tangent { get; }
    }

    public static class StadiumPath
    {
        public static float GetPerimeter(float straightLength, float turnRadius)
        {
            ValidateGeometry(straightLength, turnRadius);
            return (2f * straightLength) + (2f * Mathf.PI * turnRadius);
        }

        public static float GetTopCenterNormalizedDistance(float straightLength, float turnRadius)
        {
            return (straightLength * 0.5f) / GetPerimeter(straightLength, turnRadius);
        }

        public static StadiumPose Evaluate(float normalizedDistance, float straightLength, float turnRadius)
        {
            ValidateGeometry(straightLength, turnRadius);

            float perimeter = GetPerimeter(straightLength, turnRadius);
            float distance = Mathf.Repeat(normalizedDistance, 1f) * perimeter;
            float halfStraight = straightLength * 0.5f;
            float arcLength = Mathf.PI * turnRadius;

            if (distance < straightLength)
            {
                return new StadiumPose(
                    new Vector3(halfStraight - distance, turnRadius, 0f),
                    Vector3.left);
            }

            distance -= straightLength;
            if (distance < arcLength)
            {
                float angle = (Mathf.PI * 0.5f) + (distance / turnRadius);
                Vector3 position = new Vector3(
                    -halfStraight + (Mathf.Cos(angle) * turnRadius),
                    Mathf.Sin(angle) * turnRadius,
                    0f);
                Vector3 tangent = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0f);
                return new StadiumPose(position, tangent.normalized);
            }

            distance -= arcLength;
            if (distance < straightLength)
            {
                return new StadiumPose(
                    new Vector3(-halfStraight + distance, -turnRadius, 0f),
                    Vector3.right);
            }

            distance -= straightLength;
            float rightAngle = (-Mathf.PI * 0.5f) + (distance / turnRadius);
            Vector3 rightPosition = new Vector3(
                halfStraight + (Mathf.Cos(rightAngle) * turnRadius),
                Mathf.Sin(rightAngle) * turnRadius,
                0f);
            Vector3 rightTangent = new Vector3(-Mathf.Sin(rightAngle), Mathf.Cos(rightAngle), 0f);
            return new StadiumPose(rightPosition, rightTangent.normalized);
        }

        private static void ValidateGeometry(float straightLength, float turnRadius)
        {
            if (straightLength <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(straightLength));
            }

            if (turnRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(turnRadius));
            }
        }
    }
}
