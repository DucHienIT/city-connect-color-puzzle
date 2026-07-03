namespace TinyTownRoads
{
    public static class WinChecker
    {
        public static bool IsWin(GridModel grid, PathManager paths)
        {
            if (!paths.AllComplete()) return false;
            if (grid.Level.requireFullCoverage && paths.CoveredCellCount() < grid.PlayableCellCount)
                return false;
            return true;
        }

        /// <summary>
        /// moves = completed drags that changed a path. Optimal play is one drag per pair,
        /// so: 3 stars at optimal, 2 stars within a small tolerance, 1 star otherwise.
        /// </summary>
        public static int CalcStars(GridModel grid, int moves)
        {
            int optimal = grid.ColorCount;
            if (moves <= optimal) return 3;
            if (moves <= optimal + 2) return 2;
            return 1;
        }
    }
}
