using System.Collections.Generic;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Renders every color's path as pooled sprite segments (roads) with round joints,
    /// rebuilding a color's visuals whenever its path changes. Also fires the
    /// pair-completed feedback (node pulse + SFX) on completion transitions.
    /// </summary>
    public class PathRenderer : MonoBehaviour
    {
        const float RoadWidth = 0.34f;

        GridModel grid;
        PathManager paths;
        GridView view;
        Transform[] containers;
        List<SpriteRenderer>[] joints;
        List<SpriteRenderer>[] segments;
        bool[] wasComplete;

        public void Init(GridModel gridModel, PathManager pathManager, GridView gridView)
        {
            grid = gridModel;
            paths = pathManager;
            view = gridView;

            int n = grid.ColorCount;
            containers = new Transform[n];
            joints = new List<SpriteRenderer>[n];
            segments = new List<SpriteRenderer>[n];
            wasComplete = new bool[n];
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject($"Path_{i}");
                go.transform.SetParent(transform, false);
                containers[i] = go.transform;
                joints[i] = new List<SpriteRenderer>();
                segments[i] = new List<SpriteRenderer>();
            }

            paths.PathChanged += OnPathChanged;
        }

        void OnDestroy()
        {
            if (paths != null) paths.PathChanged -= OnPathChanged;
        }

        void OnPathChanged(int color)
        {
            if (color < 0)
            {
                for (int i = 0; i < grid.ColorCount; i++) Rebuild(i);
            }
            else
            {
                Rebuild(color);
            }
        }

        void Rebuild(int color)
        {
            var path = paths.GetPath(color);
            var tint = grid.DisplayColor(color);
            var roadColor = new Color(tint.r, tint.g, tint.b, 0.85f);

            EnsurePool(joints[color], path.Count, color, SpriteFactory.Circle, 5);
            EnsurePool(segments[color], Mathf.Max(path.Count - 1, 0), color, SpriteFactory.Square, 4);

            for (int i = 0; i < path.Count; i++)
            {
                var joint = joints[color][i];
                joint.transform.position = view.CellToWorld(path[i]);
                joint.transform.localScale = Vector3.one * RoadWidth;
                joint.color = roadColor;
            }

            for (int i = 0; i < path.Count - 1; i++)
            {
                var a = view.CellToWorld(path[i]);
                var b = view.CellToWorld(path[i + 1]);
                var seg = segments[color][i];
                seg.transform.position = (a + b) * 0.5f;
                bool horizontal = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.y - a.y);
                seg.transform.localScale = horizontal
                    ? new Vector3(1f, RoadWidth, 1f)
                    : new Vector3(RoadWidth, 1f, 1f);
                seg.color = roadColor;
            }

            bool complete = paths.IsComplete(color);
            if (complete && !wasComplete[color])
            {
                view.PulsePair(color);
                AudioManager.Instance?.PlayPairComplete();
            }
            wasComplete[color] = complete;
        }

        void EnsurePool(List<SpriteRenderer> pool, int needed, int color, Sprite sprite, int order)
        {
            while (pool.Count < needed)
            {
                var go = new GameObject(sprite.name);
                go.transform.SetParent(containers[color], false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = order;
                pool.Add(sr);
            }
            for (int i = 0; i < pool.Count; i++)
                pool[i].gameObject.SetActive(i < needed);
        }
    }
}
