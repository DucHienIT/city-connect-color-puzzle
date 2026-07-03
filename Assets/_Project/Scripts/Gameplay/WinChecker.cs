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
        /// moves = completed drags that changed a road. Optimal play is one drag per house,
        /// so: 3 stars at optimal, 2 stars within a small tolerance, 1 star otherwise.
        /// </summary>
        public static int CalcStars(GridModel grid, int moves)
        {
            int optimal = grid.TotalHouseCount;
            if (moves <= optimal) return 3;
            if (moves <= optimal + 2) return 2;
            return 1;
        }
    }
}
