using System.Collections.Generic;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Applies one hint: draws the full correct path for the first color whose current
    /// path differs from a solved solution, truncating any player path it collides with.
    /// The solution is computed lazily (once per level) via the Solver.
    /// </summary>
    public class HintController
    {
        readonly GridModel grid;
        readonly PathManager paths;
        Solver.Result solution;
        bool solveAttempted;

        public HintController(GridModel grid, PathManager paths)
        {
            this.grid = grid;
            this.paths = paths;
        }

        /// <summary>Color index the last hint was applied to (for VFX), or -1.</summary>
        public int LastHintedColor { get; private set; } = -1;

        public bool TryApplyHint()
        {
            LastHintedColor = -1;
            if (!solveAttempted)
            {
                solveAttempted = true;
                solution = Solver.Solve(grid.Level);
                if (solution == null)
                    Debug.LogWarning($"Level {grid.Level.levelId} has no solution; hints unavailable.");
            }
            if (solution == null) return false;

            for (int color = 0; color < grid.ColorCount; color++)
            {
                var target = solution.Paths[color];
                if (MatchesSolution(paths.GetPath(color), target)) continue;

                var occupied = new HashSet<Vector2Int>(target);
                for (int other = 0; other < grid.ColorCount; other++)
                {
                    if (other == color) continue;
                    TruncateBeforeConflict(other, occupied);
                }
                paths.SetPath(color, target);
                LastHintedColor = color;
                return true;
            }
            return false; // everything already matches the solution
        }

        bool MatchesSolution(IReadOnlyList<Vector2Int> current, List<Vector2Int> target)
        {
            if (current.Count != target.Count) return false;
            bool forward = true, backward = true;
            for (int i = 0; i < target.Count; i++)
            {
                if (current[i] != target[i]) forward = false;
                if (current[target.Count - 1 - i] != target[i]) backward = false;
                if (!forward && !backward) return false;
            }
            return true;
        }

        void TruncateBeforeConflict(int color, HashSet<Vector2Int> occupied)
        {
            var path = paths.GetPath(color);
            for (int i = 0; i < path.Count; i++)
            {
                if (!occupied.Contains(path[i])) continue;
                // A pair's own nodes never sit on another color's solution path, so i > 0.
                var kept = new List<Vector2Int>();
                for (int k = 0; k < i; k++) kept.Add(path[k]);
                paths.SetPath(color, kept);
                return;
            }
        }
    }
}
