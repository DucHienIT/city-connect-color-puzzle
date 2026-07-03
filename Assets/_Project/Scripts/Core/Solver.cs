using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Backtracking solver (GAME_SPEC.md section 4). Each color is one city plus
    /// N houses; the solver routes one self-avoiding branch per house to the city
    /// with DFS (branches block each other; the city cell is shared and only ever
    /// a branch terminus), pruning states where a remaining house can no longer
    /// share an empty region with its city. Used by the editor level generator
    /// (validation) and the hint system.
    /// </summary>
    public static class Solver
    {
        public class Result
        {
            /// <summary>Solution branches indexed [color][house], each from house to city.</summary>
            public List<Vector2Int>[][] Branches;
        }

        const int Empty = -1;
        const int Obstacle = -2;

        class Ctx
        {
            public int W, H;
            public int[] Cells;                 // Empty / Obstacle / color index
            public Vector2Int[] Cities;
            public Vector2Int[][] Houses;
            public int[] Order;                 // color processing order
            public List<Vector2Int>[][] Current;
            public List<Vector2Int>[][] First;
            public int Found, MaxCount;
            public long Budget;

            public int Idx(Vector2Int c) => c.y * W + c.x;
            public bool InBounds(Vector2Int c) => c.x >= 0 && c.x < W && c.y >= 0 && c.y < H;
        }

        /// <summary>Finds one solution, or null if none exists (within the search budget).</summary>
        public static Result Solve(LevelData level)
        {
            var ctx = Build(level, 1, 50_000_000);
            Search(ctx, 0, 0);
            return ctx.Found > 0 ? new Result { Branches = ctx.First } : null;
        }

        /// <summary>
        /// Counts solutions up to max. Returns -1 if the search budget ran out before
        /// the count could be established (treat as "too hard to verify").
        /// </summary>
        public static int CountSolutions(LevelData level, int max, long budget = 20_000_000)
        {
            var ctx = Build(level, max, budget);
            Search(ctx, 0, 0);
            return ctx.Budget <= 0 && ctx.Found < max ? -1 : ctx.Found;
        }

        static Ctx Build(LevelData level, int maxCount, long budget)
        {
            var ctx = new Ctx
            {
                W = level.gridWidth,
                H = level.gridHeight,
                Cells = new int[level.gridWidth * level.gridHeight],
                Cities = level.groups.Select(g => g.city.ToVector()).ToArray(),
                Houses = level.groups.Select(g => g.houses.Select(h => h.ToVector()).ToArray()).ToArray(),
                Current = new List<Vector2Int>[level.groups.Count][],
                MaxCount = maxCount,
                Budget = budget,
            };
            for (int i = 0; i < ctx.Cells.Length; i++) ctx.Cells[i] = Empty;
            foreach (var o in level.obstacles) ctx.Cells[ctx.Idx(o.ToVector())] = Obstacle;
            for (int c = 0; c < ctx.Cities.Length; c++)
            {
                ctx.Cells[ctx.Idx(ctx.Cities[c])] = c;
                foreach (var house in ctx.Houses[c]) ctx.Cells[ctx.Idx(house)] = c;
                ctx.Current[c] = new List<Vector2Int>[ctx.Houses[c].Length];
                for (int h = 0; h < ctx.Houses[c].Length; h++)
                    ctx.Current[c][h] = new List<Vector2Int>();
            }
            // Most demanding colors first (total house→city distance) to prune earlier.
            ctx.Order = Enumerable.Range(0, ctx.Cities.Length)
                .OrderByDescending(c => ctx.Houses[c].Sum(h =>
                    Mathf.Abs(h.x - ctx.Cities[c].x) + Mathf.Abs(h.y - ctx.Cities[c].y)))
                .ToArray();
            return ctx;
        }

        /// <summary>Routes branch <paramref name="houseIdx"/> of the orderIdx-th color, then advances.</summary>
        static void Search(Ctx ctx, int orderIdx, int houseIdx)
        {
            if (ctx.Found >= ctx.MaxCount || ctx.Budget <= 0) return;
            if (orderIdx == ctx.Order.Length)
            {
                Record(ctx);
                return;
            }
            int color = ctx.Order[orderIdx];
            if (houseIdx == ctx.Houses[color].Length)
            {
                if (Prune(ctx, orderIdx + 1)) Search(ctx, orderIdx + 1, 0);
                return;
            }
            var branch = ctx.Current[color][houseIdx];
            branch.Clear();
            branch.Add(ctx.Houses[color][houseIdx]);
            Route(ctx, orderIdx, color, houseIdx, ctx.Houses[color][houseIdx]);
            branch.Clear();
        }

        static void Route(Ctx ctx, int orderIdx, int color, int houseIdx, Vector2Int pos)
        {
            if (ctx.Found >= ctx.MaxCount || --ctx.Budget <= 0) return;
            var city = ctx.Cities[color];
            var branch = ctx.Current[color][houseIdx];

            foreach (var dir in GridUtils.Directions)
            {
                var next = pos + dir;
                if (!ctx.InBounds(next)) continue;

                if (next == city)
                {
                    branch.Add(next);
                    Search(ctx, orderIdx, houseIdx + 1);
                    branch.RemoveAt(branch.Count - 1);
                    continue;
                }

                int idx = ctx.Idx(next);
                if (ctx.Cells[idx] != Empty) continue;
                ctx.Cells[idx] = color;
                branch.Add(next);
                Route(ctx, orderIdx, color, houseIdx, next);
                branch.RemoveAt(branch.Count - 1);
                ctx.Cells[idx] = Empty;
            }
        }

        /// <summary>
        /// Necessary-condition check after completing a color: every remaining color's
        /// houses must each share an empty region with their city (or touch it).
        /// </summary>
        static bool Prune(Ctx ctx, int fromOrderIdx)
        {
            if (fromOrderIdx == ctx.Order.Length) return true;

            int n = ctx.W * ctx.H;
            var region = new int[n];   // 0 = not empty / unvisited, else region id
            int regionCount = 0;
            var stack = new Stack<int>();

            for (int i = 0; i < n; i++)
            {
                if (ctx.Cells[i] != Empty || region[i] != 0) continue;
                regionCount++;
                region[i] = regionCount;
                stack.Push(i);
                while (stack.Count > 0)
                {
                    int cur = stack.Pop();
                    int cx = cur % ctx.W, cy = cur / ctx.W;
                    TryFlood(ctx, region, stack, cx - 1, cy, regionCount);
                    TryFlood(ctx, region, stack, cx + 1, cy, regionCount);
                    TryFlood(ctx, region, stack, cx, cy - 1, regionCount);
                    TryFlood(ctx, region, stack, cx, cy + 1, regionCount);
                }
            }

            for (int oi = fromOrderIdx; oi < ctx.Order.Length; oi++)
            {
                int c = ctx.Order[oi];
                foreach (var house in ctx.Houses[c])
                    if (!Connected(ctx, region, house, ctx.Cities[c])) return false;
            }
            return true;
        }

        static void TryFlood(Ctx ctx, int[] region, Stack<int> stack, int x, int y, int id)
        {
            if (x < 0 || x >= ctx.W || y < 0 || y >= ctx.H) return;
            int i = y * ctx.W + x;
            if (ctx.Cells[i] != Empty || region[i] != 0) return;
            region[i] = id;
            stack.Push(i);
        }

        static bool Connected(Ctx ctx, int[] region, Vector2Int a, Vector2Int b)
        {
            if (GridUtils.AreAdjacent(a, b)) return true;

            var regionsOfA = new List<int>(4);
            foreach (var dir in GridUtils.Directions)
            {
                var c = a + dir;
                if (!ctx.InBounds(c)) continue;
                int r = region[ctx.Idx(c)];
                if (r != 0) regionsOfA.Add(r);
            }
            foreach (var dir in GridUtils.Directions)
            {
                var c = b + dir;
                if (!ctx.InBounds(c)) continue;
                int r = region[ctx.Idx(c)];
                if (r != 0 && regionsOfA.Contains(r)) return true;
            }
            return false;
        }

        static void Record(Ctx ctx)
        {
            if (ctx.Found == 0)
            {
                ctx.First = new List<Vector2Int>[ctx.Current.Length][];
                for (int c = 0; c < ctx.Current.Length; c++)
                {
                    ctx.First[c] = new List<Vector2Int>[ctx.Current[c].Length];
                    for (int h = 0; h < ctx.Current[c].Length; h++)
                        ctx.First[c][h] = new List<Vector2Int>(ctx.Current[c][h]);
                }
            }
            ctx.Found++;
        }
    }
}
