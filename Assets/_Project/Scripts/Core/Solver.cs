using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Backtracking solver (GAME_SPEC.md section 4). Routes each color pair with DFS,
    /// pruning states where remaining endpoints are disconnected or, in full-coverage
    /// levels, an empty region can no longer be reached by any unfinished color.
    /// Used by the editor level generator (validation/uniqueness) and the hint system.
    /// </summary>
    public static class Solver
    {
        public class Result
        {
            /// <summary>Solution paths indexed by pair index, each from pair.start to pair.end.</summary>
            public List<Vector2Int>[] Paths;
        }

        const int Empty = -1;
        const int Obstacle = -2;

        class Ctx
        {
            public int W, H;
            public int[] Cells;                 // Empty / Obstacle / color index
            public Vector2Int[] Starts, Ends;
            public int[] Order;                 // color processing order
            public bool RequireCoverage;
            public List<Vector2Int>[] Current;
            public List<Vector2Int>[] First;
            public int Found, MaxCount;
            public long Budget;

            public int Idx(Vector2Int c) => c.y * W + c.x;
            public bool InBounds(Vector2Int c) => c.x >= 0 && c.x < W && c.y >= 0 && c.y < H;
        }

        /// <summary>Finds one solution, or null if the level is unsolvable.</summary>
        public static Result Solve(LevelData level)
        {
            var ctx = Build(level, 1, 50_000_000);
            Search(ctx, 0);
            return ctx.Found > 0 ? new Result { Paths = ctx.First } : null;
        }

        /// <summary>
        /// Counts solutions up to max. Returns -1 if the search budget ran out before
        /// the count could be established (treat as "too hard to verify").
        /// </summary>
        public static int CountSolutions(LevelData level, int max, long budget = 20_000_000)
        {
            var ctx = Build(level, max, budget);
            Search(ctx, 0);
            return ctx.Budget <= 0 && ctx.Found < max ? -1 : ctx.Found;
        }

        static Ctx Build(LevelData level, int maxCount, long budget)
        {
            var ctx = new Ctx
            {
                W = level.gridWidth,
                H = level.gridHeight,
                Cells = new int[level.gridWidth * level.gridHeight],
                Starts = level.pairs.Select(p => p.start.ToVector()).ToArray(),
                Ends = level.pairs.Select(p => p.end.ToVector()).ToArray(),
                RequireCoverage = level.requireFullCoverage,
                Current = new List<Vector2Int>[level.pairs.Count],
                MaxCount = maxCount,
                Budget = budget,
            };
            for (int i = 0; i < ctx.Cells.Length; i++) ctx.Cells[i] = Empty;
            foreach (var o in level.obstacles) ctx.Cells[ctx.Idx(o.ToVector())] = Obstacle;
            for (int i = 0; i < ctx.Starts.Length; i++)
            {
                ctx.Cells[ctx.Idx(ctx.Starts[i])] = i;
                ctx.Cells[ctx.Idx(ctx.Ends[i])] = i;
                ctx.Current[i] = new List<Vector2Int>();
            }
            // Longest pairs first: they are the most constrained, which prunes earlier.
            ctx.Order = Enumerable.Range(0, ctx.Starts.Length)
                .OrderByDescending(i => Mathf.Abs(ctx.Starts[i].x - ctx.Ends[i].x)
                                      + Mathf.Abs(ctx.Starts[i].y - ctx.Ends[i].y))
                .ToArray();
            return ctx;
        }

        static void Search(Ctx ctx, int orderIdx)
        {
            if (ctx.Found >= ctx.MaxCount || ctx.Budget <= 0) return;
            if (orderIdx == ctx.Order.Length)
            {
                if (!ctx.RequireCoverage || IsFull(ctx)) Record(ctx);
                return;
            }
            int color = ctx.Order[orderIdx];
            var path = ctx.Current[color];
            path.Clear();
            path.Add(ctx.Starts[color]);
            Route(ctx, orderIdx, color, ctx.Starts[color]);
            path.Clear();
        }

        static void Route(Ctx ctx, int orderIdx, int color, Vector2Int pos)
        {
            if (ctx.Found >= ctx.MaxCount || --ctx.Budget <= 0) return;
            var end = ctx.Ends[color];
            var path = ctx.Current[color];

            foreach (var dir in GridUtils.Directions)
            {
                var next = pos + dir;
                if (!ctx.InBounds(next)) continue;

                if (next == end)
                {
                    path.Add(next);
                    if (Prune(ctx, orderIdx + 1)) Search(ctx, orderIdx + 1);
                    path.RemoveAt(path.Count - 1);
                    continue;
                }

                int idx = ctx.Idx(next);
                if (ctx.Cells[idx] != Empty) continue;
                ctx.Cells[idx] = color;
                path.Add(next);
                Route(ctx, orderIdx, color, next);
                path.RemoveAt(path.Count - 1);
                ctx.Cells[idx] = Empty;
            }
        }

        /// <summary>
        /// Necessary-condition check after completing a pair: every unfinished pair's
        /// endpoints must share an empty region (or be adjacent), and with coverage on,
        /// every empty region must touch at least one unfinished endpoint.
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

            var touched = new bool[regionCount + 1];
            for (int oi = fromOrderIdx; oi < ctx.Order.Length; oi++)
            {
                int c = ctx.Order[oi];
                if (!EndpointsConnected(ctx, region, c, touched)) return false;
            }

            if (ctx.RequireCoverage)
                for (int r = 1; r <= regionCount; r++)
                    if (!touched[r]) return false;

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

        static bool EndpointsConnected(Ctx ctx, int[] region, int color, bool[] touched)
        {
            var a = ctx.Starts[color];
            var b = ctx.Ends[color];
            if (GridUtils.AreAdjacent(a, b))
            {
                MarkAdjacentRegions(ctx, region, a, touched, null);
                MarkAdjacentRegions(ctx, region, b, touched, null);
                return true;
            }
            var regionsOfA = new List<int>(4);
            MarkAdjacentRegions(ctx, region, a, touched, regionsOfA);
            bool connected = false;
            foreach (var dir in GridUtils.Directions)
            {
                var c = b + dir;
                if (!ctx.InBounds(c)) continue;
                int r = region[ctx.Idx(c)];
                if (r == 0) continue;
                touched[r] = true;
                if (regionsOfA.Contains(r)) connected = true;
            }
            return connected;
        }

        static void MarkAdjacentRegions(Ctx ctx, int[] region, Vector2Int cell, bool[] touched, List<int> collect)
        {
            foreach (var dir in GridUtils.Directions)
            {
                var c = cell + dir;
                if (!ctx.InBounds(c)) continue;
                int r = region[ctx.Idx(c)];
                if (r == 0) continue;
                touched[r] = true;
                collect?.Add(r);
            }
        }

        static bool IsFull(Ctx ctx)
        {
            for (int i = 0; i < ctx.Cells.Length; i++)
                if (ctx.Cells[i] == Empty) return false;
            return true;
        }

        static void Record(Ctx ctx)
        {
            if (ctx.Found == 0)
            {
                ctx.First = new List<Vector2Int>[ctx.Current.Length];
                for (int i = 0; i < ctx.Current.Length; i++)
                    ctx.First[i] = new List<Vector2Int>(ctx.Current[i]);
            }
            ctx.Found++;
        }
    }
}
