using System.Collections.Generic;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Builds the board visuals (background, cells, obstacles, nodes) and converts
    /// between screen/world space and grid coordinates. Cell size is 1 world unit;
    /// the board is centered on the world origin.
    /// </summary>
    public class GridView : MonoBehaviour
    {
        GridModel grid;
        Vector2 origin;
        readonly Dictionary<int, List<NodeView>> nodes = new Dictionary<int, List<NodeView>>();

        public PathRenderer Paths { get; private set; }

        static readonly Color BoardColor = new Color(0.10f, 0.12f, 0.18f);
        static readonly Color CellColorA = new Color(0.16f, 0.18f, 0.26f);
        static readonly Color CellColorB = new Color(0.18f, 0.21f, 0.29f);
        static readonly Color ObstacleColor = new Color(0.07f, 0.08f, 0.12f);

        public static GridView Create(GridModel grid, PathManager paths)
        {
            var go = new GameObject("Board");
            var view = go.AddComponent<GridView>();
            view.Build(grid, paths);
            return view;
        }

        void Build(GridModel gridModel, PathManager paths)
        {
            grid = gridModel;
            origin = new Vector2(-grid.Width * 0.5f, -grid.Height * 0.5f);

            var bg = NewSprite(transform, "Background", SpriteFactory.RoundedSquare, BoardColor, -2);
            bg.transform.localScale = new Vector3(grid.Width + 0.4f, grid.Height + 0.4f, 1f);

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    bool blocked = grid.IsObstacle(cell);
                    var color = blocked ? ObstacleColor : ((x + y) % 2 == 0 ? CellColorA : CellColorB);
                    var sr = NewSprite(transform, $"Cell_{x}_{y}", SpriteFactory.RoundedSquare, color, 0);
                    sr.transform.position = CellToWorld(cell);
                    sr.transform.localScale = Vector3.one * 0.94f;
                }
            }

            for (int i = 0; i < grid.ColorCount; i++)
            {
                var display = grid.DisplayColor(i);
                nodes[i] = new List<NodeView>
                {
                    NodeView.Create(transform, i, display, CellToWorld(grid.NodeA(i))),
                    NodeView.Create(transform, i, display, CellToWorld(grid.NodeB(i))),
                };
            }

            Paths = gameObject.AddComponent<PathRenderer>();
            Paths.Init(grid, paths, this);
        }

        static SpriteRenderer NewSprite(Transform parent, string name, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3(origin.x + cell.x + 0.5f, origin.y + cell.y + 0.5f, 0f);
        }

        public bool ScreenToCell(Vector2 screenPos, Camera cam, out Vector2Int cell)
        {
            var world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            cell = new Vector2Int(Mathf.FloorToInt(world.x - origin.x), Mathf.FloorToInt(world.y - origin.y));
            return grid.InBounds(cell);
        }

        public void PulsePair(int color)
        {
            foreach (var node in nodes[color]) node.Pulse();
        }
    }
}
