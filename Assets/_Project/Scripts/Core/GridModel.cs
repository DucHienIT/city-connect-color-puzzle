using System.Collections.Generic;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Immutable board description built from a LevelData: bounds, obstacles and,
    /// per color, one city plus its houses. Every house must be connected to its
    /// city by a road branch; road state lives in PathManager.
    /// </summary>
    public class GridModel
    {
        public LevelData Level { get; }
        public int Width { get; }
        public int Height { get; }
        public int ColorCount => Level.groups.Count;

        readonly int[] nodeColor;   // color index per cell, -1 = no node
        readonly bool[] cityCell;
        readonly bool[] obstacle;
        readonly Vector2Int[] cities;
        readonly List<Vector2Int>[] houses;

        public GridModel(LevelData level)
        {
            Level = level;
            Width = level.gridWidth;
            Height = level.gridHeight;
            nodeColor = new int[Width * Height];
            cityCell = new bool[Width * Height];
            obstacle = new bool[Width * Height];
            cities = new Vector2Int[level.groups.Count];
            houses = new List<Vector2Int>[level.groups.Count];
            for (int i = 0; i < nodeColor.Length; i++) nodeColor[i] = -1;

            foreach (var cell in level.obstacles)
                obstacle[Index(cell.ToVector())] = true;

            for (int i = 0; i < level.groups.Count; i++)
            {
                var group = level.groups[i];
                cities[i] = group.city.ToVector();
                nodeColor[Index(cities[i])] = i;
                cityCell[Index(cities[i])] = true;

                houses[i] = new List<Vector2Int>();
                foreach (var house in group.houses)
                {
                    houses[i].Add(house.ToVector());
                    nodeColor[Index(house.ToVector())] = i;
                }
            }
        }

        int Index(Vector2Int c) => c.y * Width + c.x;

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.x < Width && c.y >= 0 && c.y < Height;

        public bool IsObstacle(Vector2Int c) => obstacle[Index(c)];

        /// <summary>Color index of the node (city or house) at this cell, or -1.</summary>
        public int NodeColorAt(Vector2Int c) => nodeColor[Index(c)];

        public bool IsCityAt(Vector2Int c) => cityCell[Index(c)];

        public Vector2Int City(int color) => cities[color];

        public IReadOnlyList<Vector2Int> Houses(int color) => houses[color];

        /// <summary>Index into Houses(color) of the house at this cell, or -1.</summary>
        public int HouseIndexAt(int color, Vector2Int cell)
        {
            for (int i = 0; i < houses[color].Count; i++)
                if (houses[color][i] == cell) return i;
            return -1;
        }

        /// <summary>Total house count across all colors (optimal move count).</summary>
        public int TotalHouseCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < houses.Length; i++) total += houses[i].Count;
                return total;
            }
        }

        public Color DisplayColor(int color) => ColorPalette.Get(Level.groups[color].color);
    }
}
