using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Mutable path state for the current level plus the drawing rules from
    /// GAME_SPEC.md section 2.2. Each color's path is an ordered cell list that
    /// always starts at one of its two nodes.
    /// </summary>
    public class PathManager
    {
        readonly GridModel grid;
        readonly List<Vector2Int>[] paths;

        /// <summary>Raised with the color index whose path changed (-1 = every color).</summary>
        public event Action<int> PathChanged;

        /// <summary>Increases every time any path changes; lets callers detect edits cheaply.</summary>
        public int Version { get; private set; }

        public PathManager(GridModel grid)
        {
            this.grid = grid;
            paths = new List<Vector2Int>[grid.ColorCount];
            for (int i = 0; i < paths.Length; i++) paths[i] = new List<Vector2Int>();
        }

        public IReadOnlyList<Vector2Int> GetPath(int color) => paths[color];

        public bool IsComplete(int color)
        {
            var p = paths[color];
            if (p.Count < 2) return false;
            var a = grid.NodeA(color);
            var b = grid.NodeB(color);
            var first = p[0];
            var last = p[p.Count - 1];
            return (first == a && last == b) || (first == b && last == a);
        }

        public bool AllComplete()
        {
            for (int i = 0; i < paths.Length; i++)
                if (!IsComplete(i)) return false;
            return true;
        }

        /// <summary>Color whose path covers this cell, or -1.</summary>
        public int OwnerAt(Vector2Int cell)
        {
            for (int i = 0; i < paths.Length; i++)
                if (paths[i].Contains(cell)) return i;
            return -1;
        }

        /// <summary>
        /// Starts a drag at a cell. On a node the color's path restarts from that node;
        /// on an existing path the path is truncated to that cell and drawing resumes.
        /// Returns the active color, or -1 if the cell cannot start a drag.
        /// </summary>
        public int BeginDrag(Vector2Int cell)
        {
            if (!grid.InBounds(cell) || grid.IsObstacle(cell)) return -1;

            int node = grid.NodeColorAt(cell);
            if (node >= 0)
            {
                var p = paths[node];
                if (p.Count != 1 || p[0] != cell)
                {
                    p.Clear();
                    p.Add(cell);
                    Notify(node);
                }
                return node;
            }

            int owner = OwnerAt(cell);
            if (owner >= 0)
            {
                TruncateTo(owner, cell);
                return owner;
            }
            return -1;
        }

        /// <summary>
        /// Tries to extend a color's path by one adjacent cell, applying the rules:
        /// backtracking over the own path truncates it, other colors and obstacles block,
        /// reaching the far node completes the pair. Returns true if the path changed.
        /// </summary>
        public bool Extend(int color, Vector2Int cell)
        {
            var p = paths[color];
            if (p.Count == 0 || IsComplete(color)) return false;
            if (!grid.InBounds(cell) || grid.IsObstacle(cell)) return false;

            var head = p[p.Count - 1];
            if (cell == head || !GridUtils.AreAdjacent(head, cell)) return false;

            int existing = p.IndexOf(cell);
            if (existing >= 0)
            {
                p.RemoveRange(existing + 1, p.Count - existing - 1);
                Notify(color);
                return true;
            }

            int node = grid.NodeColorAt(cell);
            if (node >= 0 && node != color) return false;
            if (node < 0 && OwnerAt(cell) >= 0) return false;

            p.Add(cell);
            Notify(color);
            return true;
        }

        void TruncateTo(int color, Vector2Int cell)
        {
            var p = paths[color];
            int idx = p.IndexOf(cell);
            if (idx < 0 || idx == p.Count - 1) return;
            p.RemoveRange(idx + 1, p.Count - idx - 1);
            Notify(color);
        }

        public void SetPath(int color, IEnumerable<Vector2Int> cells)
        {
            paths[color].Clear();
            paths[color].AddRange(cells);
            Notify(color);
        }

        public void Clear(int color)
        {
            if (paths[color].Count == 0) return;
            paths[color].Clear();
            Notify(color);
        }

        public void ClearAll()
        {
            foreach (var p in paths) p.Clear();
            Notify(-1);
        }

        public List<Vector2Int>[] Snapshot()
        {
            var copy = new List<Vector2Int>[paths.Length];
            for (int i = 0; i < paths.Length; i++) copy[i] = new List<Vector2Int>(paths[i]);
            return copy;
        }

        public void Restore(List<Vector2Int>[] snapshot)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i].Clear();
                paths[i].AddRange(snapshot[i]);
            }
            Notify(-1);
        }

        public int CoveredCellCount()
        {
            var covered = new HashSet<Vector2Int>();
            foreach (var p in paths) covered.UnionWith(p);
            return covered.Count;
        }

        void Notify(int color)
        {
            Version++;
            PathChanged?.Invoke(color);
        }
    }
}
