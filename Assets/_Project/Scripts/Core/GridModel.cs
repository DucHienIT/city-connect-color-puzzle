using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Immutable board description built from a LevelData: bounds, obstacles and
    /// node (building) positions per color. Path state lives in PathManager.
    /// </summary>
    public class GridModel
    {
        public LevelData Level { get; }
        public int Width { get; }
        public int Height { get; }
        public int ColorCount => Level.pairs.Count;

        readonly int[] nodeColor;   // color index per cell, -1 = no node
        readonly bool[] obstacle;

        public GridModel(LevelData level)
        {
            Level = level;
            Width = level.gridWidth;
            Height = level.gridHeight;
            nodeColor = new int[Width * Height];
            obstacle = new bool[Width * Height];
            for (int i = 0; i < nodeColor.Length; i++) nodeColor[i] = -1;

            foreach (var cell in level.obstacles)
                obstacle[Index(cell.ToVector())] = true;

            for (int i = 0; i < level.pairs.Count; i++)
            {
                nodeColor[Index(level.pairs[i].start.ToVector())] = i;
                nodeColor[Index(level.pairs[i].end.ToVector())] = i;
            }
        }

        int Index(Vector2Int c) => c.y * Width + c.x;

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.x < Width && c.y >= 0 && c.y < Height;

        public bool IsObstacle(Vector2Int c) => obstacle[Index(c)];

        /// <summary>Color index of the node at this cell, or -1.</summary>
        public int NodeColorAt(Vector2Int c) => nodeColor[Index(c)];

        public Vector2Int NodeA(int color) => Level.pairs[color].start.ToVector();
        public Vector2Int NodeB(int color) => Level.pairs[color].end.ToVector();

        public Color DisplayColor(int color) => ColorPalette.Get(Level.pairs[color].color);

        public int PlayableCellCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < obstacle.Length; i++)
                    if (!obstacle[i]) count++;
                return count;
            }
        }
    }
}
