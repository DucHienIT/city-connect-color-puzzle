using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Random level generation (GAME_SPEC.md section 4): fill the whole grid with
    /// non-crossing random walks, turn each walk into a color pair (endpoints = nodes),
    /// then keep only levels the Solver confirms have a unique solution.
    /// Editor tooling lives in LevelGeneratorWindow; this class is engine-only so the
    /// same algorithm could also run in tests or at runtime.
    /// </summary>
    public static class LevelGenerator
    {
        /// <param name="maxPathLen">
        /// Cap on each walk's length. Shorter walks mean more pairs and dramatically
        /// better odds of a unique solution on larger boards. Measured sweet spots:
        /// 5x5–6x6 uncapped, 7x7 ≈ 9 (6–9 pairs), 8x8 ≈ 7 (8–11 pairs), 9x9 ≈ 7 (10–12 pairs).
        /// </param>
        public static LevelData Generate(int width, int height, int minPairs, int maxPairs,
            int obstacleCount, System.Random rng, int maxPathLen = 99, int maxAttempts = 15000)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var level = TryFill(width, height, minPairs, maxPairs, obstacleCount, rng, maxPathLen);
                if (level == null) continue;
                if (Solver.CountSolutions(level, 2) == 1) return level;
            }
            return null;
        }

        static LevelData TryFill(int width, int height, int minPairs, int maxPairs,
            int obstacleCount, System.Random rng, int maxPathLen)
        {
            var used = new bool[width * height];
            var obstacles = new List<Vector2Int>();

            for (int i = 0; i < obstacleCount; i++)
            {
                var cell = new Vector2Int(rng.Next(width), rng.Next(height));
                if (used[cell.y * width + cell.x]) return null; // collision → cheap retry
                used[cell.y * width + cell.x] = true;
                obstacles.Add(cell);
            }

            var paths = new List<List<Vector2Int>>();
            while (true)
            {
                var free = FreeCells(width, height, used);
                if (free.Count == 0) break;

                // Start from a most-constrained free cell to reduce stranded gaps.
                int minNeighbors = free.Min(c => FreeNeighborCount(c, width, height, used));
                var candidates = free.Where(c => FreeNeighborCount(c, width, height, used) == minNeighbors).ToList();
                var start = candidates[rng.Next(candidates.Count)];

                var path = new List<Vector2Int> { start };
                used[start.y * width + start.x] = true;

                while (path.Count < maxPathLen)
                {
                    var options = new List<Vector2Int>();
                    var head = path[path.Count - 1];
                    foreach (var dir in GridUtils.Directions)
                    {
                        var next = head + dir;
                        if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height) continue;
                        if (used[next.y * width + next.x]) continue;
                        options.Add(next);
                    }
                    if (options.Count == 0) break;
                    var pick = options[rng.Next(options.Count)];
                    used[pick.y * width + pick.x] = true;
                    path.Add(pick);
                }

                if (path.Count < 3) return null; // stranded walk → retry whole fill
                paths.Add(path);
            }

            if (paths.Count < minPairs || paths.Count > maxPairs) return null;
            if (paths.Count > ColorPalette.Names.Length) return null;

            var level = new LevelData
            {
                gridWidth = width,
                gridHeight = height,
                requireFullCoverage = true,
            };
            foreach (var cell in obstacles)
                level.obstacles.Add(new CellCoord(cell.x, cell.y));
            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                level.pairs.Add(new ColorPair
                {
                    color = ColorPalette.Names[i],
                    start = new CellCoord(path[0].x, path[0].y),
                    end = new CellCoord(path[path.Count - 1].x, path[path.Count - 1].y),
                });
            }
            return level;
        }

        static List<Vector2Int> FreeCells(int width, int height, bool[] used)
        {
            var result = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (!used[y * width + x]) result.Add(new Vector2Int(x, y));
            return result;
        }

        static int FreeNeighborCount(Vector2Int cell, int width, int height, bool[] used)
        {
            int count = 0;
            foreach (var dir in GridUtils.Directions)
            {
                var next = cell + dir;
                if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height) continue;
                if (!used[next.y * width + next.x]) count++;
            }
            return count;
        }
    }
}
