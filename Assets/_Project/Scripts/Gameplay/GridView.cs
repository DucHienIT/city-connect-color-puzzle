using System.Collections.Generic;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Builds the 3D board (cream base plate on the XZ ground plane, cell dots,
    /// obstacle blocks, building nodes) and converts between screen rays and grid
    /// coordinates. Cell size is 1 world unit; the board is centered on the world
    /// origin with the playable surface at y = 0.
    /// </summary>
    public class GridView : MonoBehaviour
    {
        GridModel grid;
        Vector2 origin;
        readonly Dictionary<int, List<NodeView>> nodes = new Dictionary<int, List<NodeView>>();

        public PathRenderer Paths { get; private set; }

        static readonly Color PlateColor = new Color(0.937f, 0.902f, 0.847f);
        static readonly Color DotColor = new Color(0.782f, 0.737f, 0.667f);
        static readonly Color ObstacleColor = new Color(0.752f, 0.700f, 0.616f);
        const float PlateHeight = 0.3f;
        const float PlateMargin = 0.7f;

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

            var plate = MeshFactory.CreatePart(transform, "Plate",
                MeshFactory.RoundedBlock(grid.Width + PlateMargin, PlateHeight, grid.Height + PlateMargin, 0.45f, 6),
                PlateColor);
            plate.transform.localPosition = new Vector3(0f, -PlateHeight, 0f);

            var dotMesh = MeshFactory.Disc(0.055f, 0.02f, 12);
            var obstacleMesh = MeshFactory.RoundedBlock(0.78f, 0.26f, 0.78f, 0.16f, 4);
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    var part = grid.IsObstacle(cell)
                        ? MeshFactory.CreatePart(transform, $"Obstacle_{x}_{y}", obstacleMesh, ObstacleColor)
                        : MeshFactory.CreatePart(transform, $"Dot_{x}_{y}", dotMesh, DotColor);
                    part.transform.position = CellToWorld(cell);
                }
            }

            for (int i = 0; i < grid.ColorCount; i++)
            {
                var display = grid.DisplayColor(i);
                nodes[i] = new List<NodeView>
                {
                    NodeView.CreateCity(transform, i, display, CellToWorld(grid.City(i)), grid.Houses(i).Count),
                };
                foreach (var house in grid.Houses(i))
                    nodes[i].Add(NodeView.CreateHouse(transform, i, display, CellToWorld(house)));
            }

            Paths = gameObject.AddComponent<PathRenderer>();
            Paths.Init(grid, paths, this);
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3(origin.x + cell.x + 0.5f, 0f, origin.y + cell.y + 0.5f);
        }

        public bool ScreenToCell(Vector2 screenPos, Camera cam, out Vector2Int cell)
        {
            cell = default;
            var ray = cam.ScreenPointToRay(screenPos);
            if (Mathf.Abs(ray.direction.y) < 1e-5f) return false;
            float t = -ray.origin.y / ray.direction.y;
            if (t < 0f) return false;
            var world = ray.GetPoint(t);
            cell = new Vector2Int(Mathf.FloorToInt(world.x - origin.x), Mathf.FloorToInt(world.z - origin.y));
            return grid.InBounds(cell);
        }

        public void PulsePair(int color)
        {
            foreach (var node in nodes[color]) node.Pulse();
        }

        /// <summary>Pulses only the city building — used to point out the drag target.</summary>
        public void PulseCity(int color)
        {
            nodes[color][0].Pulse(); // the city is created first for every color
        }
    }
}
