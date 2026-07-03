using UnityEngine;

namespace TinyTownRoads
{
    public static class GridUtils
    {
        public static readonly Vector2Int[] Directions =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        public static bool AreAdjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }
    }
}
