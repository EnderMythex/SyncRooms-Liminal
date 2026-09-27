using System.Collections.Generic;
using UnityEngine;

namespace SyncRooms.Player
{
    /// <summary>Breadcrumbs of where the player walked: monsters follow them around corners.</summary>
    public static class PlayerTrail
    {
        static readonly List<Vector3> Points = new();
        const int Max = 256;
        const float Spacing = 0.8f;

        public static IReadOnlyList<Vector3> All => Points;

        public static void Clear() => Points.Clear();

        public static void Record(Vector3 p)
        {
            if (Points.Count > 0 && (Points[^1] - p).sqrMagnitude < Spacing * Spacing) return;
            Points.Add(p);
            if (Points.Count > Max) Points.RemoveAt(0);
        }

        /// <summary>Most recent breadcrumb visible from `from` (walking height). Returns false if none.</summary>
        public static bool NewestVisible(Vector3 from, float maxDist, out Vector3 target)
        {
            for (int i = Points.Count - 1; i >= 0 && i >= Points.Count - 120; i--)
            {
                var p = Points[i] + Vector3.up * 0.6f;
                var d = p - from;
                if (d.sqrMagnitude > maxDist * maxDist) continue;
                if (!Physics.Raycast(from, d.normalized, d.magnitude, ~((1 << 2) | (1 << 9)), QueryTriggerInteraction.Ignore))
                {
                    target = Points[i];
                    return true;
                }
            }
            target = default;
            return false;
        }
    }
}
