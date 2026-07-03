using System.Collections.Generic;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Renders every color's road branches as pooled 3D pieces — asphalt segments
    /// with round joints and white center dashes — rebuilding a color's visuals
    /// whenever its roads change. Each house has its own car: parked at the drawing
    /// head while dragging, and looping house → city forever once its branch
    /// connects. City buildings pulse when their whole demand is met.
    /// </summary>
    public class PathRenderer : MonoBehaviour
    {
        const float RoadWidth = 0.34f;
        const float RoadHeight = 0.05f;
        const float DashHeight = 0.062f;

        static readonly Color AsphaltColor = new Color(0.32f, 0.33f, 0.37f);
        static readonly Color DashColor = new Color(0.95f, 0.93f, 0.88f);

        GridModel grid;
        PathManager paths;
        GridView view;
        Transform[] containers;
        List<MeshRenderer>[] joints;
        List<MeshRenderer>[] segments;
        List<MeshRenderer>[] dashes;
        CarView[][] cars;
        bool[][] branchWasComplete;
        bool[] colorWasComplete;
        Mesh jointMesh;
        Mesh boxMesh;

        readonly HashSet<Vector2Int> jointSeen = new HashSet<Vector2Int>();
        readonly List<Vector2Int> jointCells = new List<Vector2Int>();

        public void Init(GridModel gridModel, PathManager pathManager, GridView gridView)
        {
            grid = gridModel;
            paths = pathManager;
            view = gridView;
            // Slightly taller than the segments so overlapping top faces never z-fight.
            jointMesh = MeshFactory.Disc(RoadWidth * 0.5f, RoadHeight + 0.004f, 16);
            boxMesh = MeshFactory.Block(1f, 1f, 1f);

            int n = grid.ColorCount;
            containers = new Transform[n];
            joints = new List<MeshRenderer>[n];
            segments = new List<MeshRenderer>[n];
            dashes = new List<MeshRenderer>[n];
            cars = new CarView[n][];
            branchWasComplete = new bool[n][];
            colorWasComplete = new bool[n];
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject($"Roads_{i}");
                go.transform.SetParent(transform, false);
                containers[i] = go.transform;
                joints[i] = new List<MeshRenderer>();
                segments[i] = new List<MeshRenderer>();
                dashes[i] = new List<MeshRenderer>();
                int branches = paths.BranchCount(i);
                cars[i] = new CarView[branches];
                branchWasComplete[i] = new bool[branches];
                for (int h = 0; h < branches; h++)
                    cars[i][h] = CarView.Create(containers[i], grid.DisplayColor(i));
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
            // Strong color share so roads of nearby hues stay tellable apart.
            var roadColor = Color.Lerp(AsphaltColor, grid.DisplayColor(color), 0.55f);
            int branchCount = paths.BranchCount(color);

            // Joints: one per covered cell, deduped (branches share the city cell).
            jointSeen.Clear();
            jointCells.Clear();
            int segmentCount = 0;
            for (int h = 0; h < branchCount; h++)
            {
                var branch = paths.GetBranch(color, h);
                foreach (var cell in branch)
                    if (jointSeen.Add(cell)) jointCells.Add(cell);
                segmentCount += Mathf.Max(branch.Count - 1, 0);
            }

            EnsurePool(joints[color], jointCells.Count, color, jointMesh, "Joint");
            EnsurePool(segments[color], segmentCount, color, boxMesh, "Segment");
            EnsurePool(dashes[color], segmentCount * 2, color, boxMesh, "Dash");

            for (int i = 0; i < jointCells.Count; i++)
            {
                var joint = joints[color][i];
                joint.transform.position = view.CellToWorld(jointCells[i]);
                MeshFactory.SetTint(joint, roadColor);
            }

            int s = 0;
            for (int h = 0; h < branchCount; h++)
            {
                var branch = paths.GetBranch(color, h);
                for (int i = 0; i < branch.Count - 1; i++, s++)
                {
                    var a = view.CellToWorld(branch[i]);
                    var b = view.CellToWorld(branch[i + 1]);
                    bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);

                    var seg = segments[color][s];
                    seg.transform.position = (a + b) * 0.5f;
                    seg.transform.localScale = alongX
                        ? new Vector3(1f, RoadHeight, RoadWidth)
                        : new Vector3(RoadWidth, RoadHeight, 1f);
                    MeshFactory.SetTint(seg, roadColor);

                    for (int d = 0; d < 2; d++)
                    {
                        var dash = dashes[color][s * 2 + d];
                        dash.transform.position = Vector3.Lerp(a, b, d == 0 ? 0.25f : 0.75f);
                        dash.transform.localScale = alongX
                            ? new Vector3(0.18f, DashHeight, 0.055f)
                            : new Vector3(0.055f, DashHeight, 0.18f);
                        MeshFactory.SetTint(dash, DashColor);
                    }
                }
            }

            for (int h = 0; h < branchCount; h++) UpdateCar(color, h);

            bool colorComplete = paths.IsComplete(color);
            if (colorComplete && !colorWasComplete[color]) view.PulsePair(color);
            colorWasComplete[color] = colorComplete;
        }

        void UpdateCar(int color, int house)
        {
            var branch = paths.GetBranch(color, house);
            var car = cars[color][house];
            bool complete = paths.BranchComplete(color, house);

            if (complete)
            {
                if (!branchWasComplete[color][house])
                    AudioManager.Instance?.PlayPairComplete();
                // Connected roads stay alive: the car loops the route forever.
                if (!branchWasComplete[color][house] || !car.IsDriving)
                {
                    var waypoints = new Vector3[branch.Count];
                    for (int i = 0; i < branch.Count; i++) waypoints[i] = RoadPoint(branch[i]);
                    car.Drive(waypoints);
                }
            }
            else if (branch.Count >= 2)
            {
                var head = RoadPoint(branch[branch.Count - 1]);
                var prev = RoadPoint(branch[branch.Count - 2]);
                car.Park(head, head - prev);
            }
            else
            {
                car.Hide();
            }

            branchWasComplete[color][house] = complete;
        }

        Vector3 RoadPoint(Vector2Int cell)
        {
            return view.CellToWorld(cell) + Vector3.up * RoadHeight;
        }

        void EnsurePool(List<MeshRenderer> pool, int needed, int color, Mesh mesh, string partName)
        {
            while (pool.Count < needed)
                pool.Add(MeshFactory.CreatePart(containers[color], partName, mesh, Color.white));
            for (int i = 0; i < pool.Count; i++)
                pool[i].gameObject.SetActive(i < needed);
        }
    }
}
