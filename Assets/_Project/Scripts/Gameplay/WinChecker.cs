using System.Collections.Generic;
using UnityEngine;

namespace TinyTownRoads
{
    public static class WinChecker
    {
        /// <summary>Win = every city has all of its houses connected; empty cells are fine.</summary>
        public static bool IsWin(GridModel grid, PathManager paths)
        {
            return paths.AllComplete();
        }

        /// <summary>
        /// Fraction (0..1) of the playable board (every non-obstacle cell) that the town
        /// occupies: cities, houses and every road cell. The fuller the grid, the higher
        /// the value — this is what scoring rewards now.
        /// </summary>
        public static float Coverage(GridModel grid, PathManager paths)
        {
            var covered = new HashSet<Vector2Int>();
            for (int c = 0; c < grid.ColorCount; c++)
            {
                covered.Add(grid.City(c));
                foreach (var house in grid.Houses(c)) covered.Add(house);
                for (int h = 0; h < paths.BranchCount(c); h++)
                    foreach (var cell in paths.GetBranch(c, h)) covered.Add(cell);
            }

            int playable = 0;
            for (int y = 0; y < grid.Height; y++)
                for (int x = 0; x < grid.Width; x++)
                    if (!grid.IsObstacle(new Vector2Int(x, y))) playable++;

            return playable == 0 ? 0f : (float)covered.Count / playable;
        }

        /// <summary>Coverage as a rounded 0..100 percentage, for display.</summary>
        public static int CoveragePercent(GridModel grid, PathManager paths)
        {
            return Mathf.RoundToInt(Coverage(grid, paths) * 100f);
        }

        /// <summary>
        /// Stars are earned by how much of the grid the roads cover: fill more of the
        /// board to earn more stars. Thresholds are tunable here.
        /// </summary>
        public static int CalcStars(GridModel grid, PathManager paths)
        {
            float coverage = Coverage(grid, paths);
            if (coverage >= 0.80f) return 3;
            if (coverage >= 0.60f) return 2;
            return 1;
        }
    }
}
