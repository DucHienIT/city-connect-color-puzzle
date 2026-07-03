using System.Collections.Generic;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Applies one hint: draws the full correct branch (house → city) for the first
    /// house whose current branch differs from a solved solution, truncating any
    /// player road it collides with. The solution is computed lazily (once per
    /// level) via the Solver.
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
                for (int house = 0; house < paths.BranchCount(color); house++)
                {
                    if (paths.BranchComplete(color, house)) continue;

                    var target = solution.Branches[color][house];
                    // Road cells only: the shared city cell must not truncate siblings.
                    var occupied = new HashSet<Vector2Int>(target);
                    occupied.Remove(grid.City(color));

                    for (int c = 0; c < grid.ColorCount; c++)
                        for (int h = 0; h < paths.BranchCount(c); h++)
                        {
                            if (c == color && h == house) continue;
                            TruncateBeforeConflict(c, h, occupied);
                        }

                    paths.SetBranch(color, house, target);
                    LastHintedColor = color;
                    return true;
                }
            }
            return false; // every house is already connected
        }

        void TruncateBeforeConflict(int color, int house, HashSet<Vector2Int> occupied)
        {
            var branch = paths.GetBranch(color, house);
            for (int i = 0; i < branch.Count; i++)
            {
                if (!occupied.Contains(branch[i])) continue;
                // A house cell never sits on another branch's solution, so i > 0.
                var kept = new List<Vector2Int>();
                for (int k = 0; k < i; k++) kept.Add(branch[k]);
                paths.SetBranch(color, house, kept);
                return;
            }
        }
    }
}
