using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Random level generation (GAME_SPEC.md section 4): repeatedly plant a city on
    /// a free cell and grow 1–4 non-crossing random-walk branches out of it; each
    /// branch's far end becomes a house, so the level is solvable by construction
    /// (the walks are one valid road layout). Leftover free cells simply stay empty.
    /// A final Solver pass double-checks. Editor tooling lives in
    /// LevelGeneratorWindow; this class is engine-only so the same algorithm can
    /// also run in tests or standalone harnesses.
    /// </summary>
    public static class LevelGenerator
    {
        /// <param name="maxBranchLen">Cap on each house→city walk (road cells between them + 1).</param>
        public static LevelData Generate(int width, int height, int minColors, int maxColors,
            int obstacleCount, System.Random rng, int maxBranchLen = 6, int maxAttempts = 2000)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var level = TryBuild(width, height, minColors, maxColors, obstacleCount, rng, maxBranchLen);
                if (level == null) continue;
                if (Solver.Solve(level) != null) return level;
            }
            return null;
        }

        static LevelData TryBuild(int width, int height, int minColors, int maxColors,
            int obstacleCount, System.Random rng, int maxBranchLen)
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

            var groups = new List<(Vector2Int city, List<Vector2Int> houses)>();
            int failures = 0;
            while (groups.Count < maxColors && failures < 40)
            {
                var free = FreeCells(width, height, used);
                if (free.Count < 3) break;

                var city = free[rng.Next(free.Count)];
                used[city.y * width + city.x] = true;

                // Grow 1–4 branches, each starting through a distinct city neighbor.
                int wanted = 1 + rng.Next(4);
                var houses = new List<Vector2Int>();
                var exits = FreeNeighbors(city, width, height, used);
                Shuffle(exits, rng);
                foreach (var exit in exits.Take(wanted))
                {
                    var house = GrowBranch(exit, width, height, used, rng, maxBranchLen);
                    if (house.HasValue) houses.Add(house.Value);
                }

                if (houses.Count == 0)
                {
                    used[city.y * width + city.x] = false;
                    failures++;
                    continue;
                }
                groups.Add((city, houses));
            }

            if (groups.Count < minColors || groups.Count > ColorPalette.Names.Length) return null;

            var level = new LevelData { gridWidth = width, gridHeight = height };
            foreach (var cell in obstacles)
                level.obstacles.Add(new CellCoord(cell.x, cell.y));
            for (int i = 0; i < groups.Count; i++)
            {
                var group = new ColorGroup
                {
                    color = ColorPalette.Names[i],
                    city = new CellCoord(groups[i].city.x, groups[i].city.y),
                };
                foreach (var house in groups[i].houses)
                    group.houses.Add(new CellCoord(house.x, house.y));
                level.groups.Add(group);
            }
            return level;
        }

        /// <summary>
        /// Random walk from a city exit cell; the last cell becomes the house.
        /// Marks every visited cell used (the walk is the reserved road corridor).
        /// Returns null if even the exit cell cannot host a house.
        /// </summary>
        static Vector2Int? GrowBranch(Vector2Int exit, int width, int height, bool[] used,
            System.Random rng, int maxBranchLen)
        {
            if (used[exit.y * width + exit.x]) return null;

            var walk = new List<Vector2Int> { exit };
            used[exit.y * width + exit.x] = true;
            int targetLen = 2 + rng.Next(Mathf.Max(maxBranchLen - 1, 1));

            while (walk.Count < targetLen)
            {
                var options = FreeNeighbors(walk[walk.Count - 1], width, height, used);
                if (options.Count == 0) break;
                var pick = options[rng.Next(options.Count)];
                used[pick.y * width + pick.x] = true;
                walk.Add(pick);
            }
            return walk[walk.Count - 1];
        }

        static List<Vector2Int> FreeCells(int width, int height, bool[] used)
        {
            var result = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (!used[y * width + x]) result.Add(new Vector2Int(x, y));
            return result;
        }

        static List<Vector2Int> FreeNeighbors(Vector2Int cell, int width, int height, bool[] used)
        {
            var result = new List<Vector2Int>();
            foreach (var dir in GridUtils.Directions)
            {
                var next = cell + dir;
                if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height) continue;
                if (!used[next.y * width + next.x]) result.Add(next);
            }
            return result;
        }

        static void Shuffle(List<Vector2Int> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
