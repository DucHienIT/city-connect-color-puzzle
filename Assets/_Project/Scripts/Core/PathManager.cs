using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Mutable road state for the current level plus the drawing rules from
    /// GAME_SPEC.md section 2.2. Each house owns one branch: an ordered cell list
    /// starting at the house; the branch is complete when it reaches the color's
    /// city, and a color is complete when every one of its houses is connected.
    /// Branches never overlap anything (not even siblings); they only share the
    /// city cell, where they all terminate.
    /// </summary>
    public class PathManager
    {
        readonly GridModel grid;
        readonly List<Vector2Int>[][] branches;   // [color][houseIndex]

        /// <summary>Raised with the color index whose roads changed (-1 = every color).</summary>
        public event Action<int> PathChanged;

        /// <summary>Increases every time any branch changes; lets callers detect edits cheaply.</summary>
        public int Version { get; private set; }

        public PathManager(GridModel grid)
        {
            this.grid = grid;
            branches = new List<Vector2Int>[grid.ColorCount][];
            for (int c = 0; c < branches.Length; c++)
            {
                branches[c] = new List<Vector2Int>[grid.Houses(c).Count];
                for (int h = 0; h < branches[c].Length; h++)
                    branches[c][h] = new List<Vector2Int>();
            }
        }

        public int BranchCount(int color) => branches[color].Length;

        public IReadOnlyList<Vector2Int> GetBranch(int color, int house) => branches[color][house];

        public bool BranchComplete(int color, int house)
        {
            var b = branches[color][house];
            return b.Count >= 2 && b[b.Count - 1] == grid.City(color);
        }

        /// <summary>A color is complete when every house is connected to the city.</summary>
        public bool IsComplete(int color)
        {
            for (int h = 0; h < branches[color].Length; h++)
                if (!BranchComplete(color, h)) return false;
            return true;
        }

        public bool AllComplete()
        {
            for (int c = 0; c < branches.Length; c++)
                if (!IsComplete(c)) return false;
            return true;
        }

        /// <summary>Finds the branch covering a cell (cities are excluded — they are shared).</summary>
        bool FindBranchAt(Vector2Int cell, out int color, out int house)
        {
            for (int c = 0; c < branches.Length; c++)
            {
                if (cell == grid.City(c)) continue;
                for (int h = 0; h < branches[c].Length; h++)
                {
                    if (branches[c][h].Contains(cell))
                    {
                        color = c;
                        house = h;
                        return true;
                    }
                }
            }
            color = house = -1;
            return false;
        }

        /// <summary>True if any branch of any color occupies this road cell.</summary>
        public bool IsRoadAt(Vector2Int cell) => FindBranchAt(cell, out _, out _);

        /// <summary>
        /// Starts a drag. On a house the house's branch restarts from it; on a branch
        /// road cell the branch is truncated to that cell and drawing resumes. Cities
        /// cannot start a drag. Returns false when nothing can be dragged here.
        /// </summary>
        public bool BeginDrag(Vector2Int cell, out int color, out int house)
        {
            color = house = -1;
            if (!grid.InBounds(cell) || grid.IsObstacle(cell)) return false;

            int node = grid.NodeColorAt(cell);
            if (node >= 0)
            {
                if (grid.IsCityAt(cell)) return false;
                color = node;
                house = grid.HouseIndexAt(node, cell);
                var b = branches[color][house];
                if (b.Count != 1 || b[0] != cell)
                {
                    b.Clear();
                    b.Add(cell);
                    Notify(color);
                }
                return true;
            }

            if (FindBranchAt(cell, out color, out house))
            {
                TruncateTo(color, house, cell);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to extend a house's branch by one adjacent cell, applying the rules:
        /// backtracking over the own branch truncates it, every other node/road blocks,
        /// and reaching the own city completes the branch. Returns true if changed.
        /// </summary>
        public bool Extend(int color, int house, Vector2Int cell)
        {
            var b = branches[color][house];
            if (b.Count == 0 || BranchComplete(color, house)) return false;
            if (!grid.InBounds(cell) || grid.IsObstacle(cell)) return false;

            var head = b[b.Count - 1];
            if (cell == head || !GridUtils.AreAdjacent(head, cell)) return false;

            int existing = b.IndexOf(cell);
            if (existing >= 0)
            {
                b.RemoveRange(existing + 1, b.Count - existing - 1);
                Notify(color);
                return true;
            }

            if (cell == grid.City(color))
            {
                b.Add(cell);
                Notify(color);
                return true;
            }

            if (grid.NodeColorAt(cell) >= 0) return false;   // houses and other cities block
            if (IsRoadAt(cell)) return false;                // all roads block, siblings included

            b.Add(cell);
            Notify(color);
            return true;
        }

        void TruncateTo(int color, int house, Vector2Int cell)
        {
            var b = branches[color][house];
            int idx = b.IndexOf(cell);
            if (idx < 0 || idx == b.Count - 1) return;
            b.RemoveRange(idx + 1, b.Count - idx - 1);
            Notify(color);
        }

        public void SetBranch(int color, int house, IEnumerable<Vector2Int> cells)
        {
            branches[color][house].Clear();
            branches[color][house].AddRange(cells);
            Notify(color);
        }

        public void ClearAll()
        {
            foreach (var color in branches)
                foreach (var b in color)
                    b.Clear();
            Notify(-1);
        }

        public List<Vector2Int>[][] Snapshot()
        {
            var copy = new List<Vector2Int>[branches.Length][];
            for (int c = 0; c < branches.Length; c++)
            {
                copy[c] = new List<Vector2Int>[branches[c].Length];
                for (int h = 0; h < branches[c].Length; h++)
                    copy[c][h] = new List<Vector2Int>(branches[c][h]);
            }
            return copy;
        }

        public void Restore(List<Vector2Int>[][] snapshot)
        {
            for (int c = 0; c < branches.Length; c++)
            {
                for (int h = 0; h < branches[c].Length; h++)
                {
                    branches[c][h].Clear();
                    branches[c][h].AddRange(snapshot[c][h]);
                }
            }
            Notify(-1);
        }

        void Notify(int color)
        {
            Version++;
            PathChanged?.Invoke(color);
        }
    }
}
