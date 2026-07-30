using System.Collections.Generic;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Procedural 3D meshes for the toy-city look: rounded blocks, discs and the car.
    /// A fixed "sun" is baked into vertex colors at build time (flat cartoon shading),
    /// rendered by the unlit TinyTownRoads/VertexColor shader. Meshes are cached and
    /// shared; per-object tint goes through a MaterialPropertyBlock so a single
    /// material serves everything. Blocks sit on their local origin (base at y = 0).
    /// </summary>
    public static class MeshFactory
    {
        static readonly Vector3 Sun = new Vector3(-0.4f, 0.75f, -0.55f).normalized;
        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static MaterialPropertyBlock mpb;
        static Material material;

        public static Material SharedMaterial
        {
            get
            {
                if (material == null)
                    material = new Material(Shader.Find("TinyTownRoads/VertexColor"));
                return material;
            }
        }

        /// <summary>
        /// Inject the shared material (scene-serialized in GameBootstrap). Luna only
        /// exports shaders reachable from scene assets, so Shader.Find returns null there.
        /// </summary>
        public static void SetMaterial(Material injected)
        {
            if (injected != null) material = injected;
        }

        /// <summary>MeshFilter + MeshRenderer child using the shared unlit material.</summary>
        public static MeshRenderer CreatePart(Transform parent, string name, Mesh mesh, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = SharedMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            SetTint(mr, tint);
            return mr;
        }

        public static void SetTint(Renderer renderer, Color tint)
        {
#if UNITY_LUNA
            // MaterialPropertyBlock is unreliable in Luna — tint a per-renderer
            // material instance instead (fine at playable scale).
            renderer.material.SetColor(ColorId, Encode(tint));
#else
            if (mpb == null) mpb = new MaterialPropertyBlock();
            mpb.SetColor(ColorId, Encode(tint));
            renderer.SetPropertyBlock(mpb);
#endif
        }

        /// <summary>Block with vertically rounded corners and a flat top, base-centered.</summary>
        public static Mesh RoundedBlock(float sizeX, float height, float sizeZ, float radius, int cornerSegments = 5)
        {
            string key = $"rblock:{sizeX}:{height}:{sizeZ}:{radius}:{cornerSegments}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var b = new Builder();
            AddPrism(b, sizeX, height, sizeZ, radius, cornerSegments, Vector3.zero);
            return Cache[key] = b.Build(key);
        }

        public static Mesh Block(float sizeX, float height, float sizeZ)
        {
            return RoundedBlock(sizeX, height, sizeZ, 0f);
        }

        /// <summary>Flat cylinder (side wall + top cap), base-centered at the origin.</summary>
        public static Mesh Disc(float radius, float height, int segments = 20)
        {
            string key = $"disc:{radius}:{height}:{segments}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var b = new Builder();
            var ring = new List<Vector3>(segments);
            for (int i = 0; i < segments; i++)
            {
                float ang = i * Mathf.PI * 2f / segments;
                ring.Add(new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius));
            }
            b.Wall(ring, height);
            b.TopCap(ring, height);
            return Cache[key] = b.Build(key);
        }

        /// <summary>Untinted car body (colored via MaterialPropertyBlock). Faces +Z.</summary>
        public static Mesh CarBody()
        {
            const string key = "car-body";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var b = new Builder();
            AddPrism(b, 0.32f, 0.15f, 0.56f, 0.09f, 3, new Vector3(0f, 0.02f, 0f));
            return Cache[key] = b.Build(key);
        }

        /// <summary>Window band, roof and wheels with their colors baked in. Faces +Z.</summary>
        public static Mesh CarDetails()
        {
            const string key = "car-details";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var b = new Builder();

            b.Tint = new Color(0.17f, 0.19f, 0.24f); // window band
            AddPrism(b, 0.29f, 0.07f, 0.36f, 0.07f, 3, new Vector3(0f, 0.17f, -0.03f));

            b.Tint = new Color(0.97f, 0.95f, 0.90f); // roof
            AddPrism(b, 0.25f, 0.06f, 0.30f, 0.06f, 3, new Vector3(0f, 0.24f, -0.03f));

            b.Tint = new Color(0.15f, 0.15f, 0.17f); // wheels
            var wheelOffsets = new[]
            {
                new Vector3(-0.15f, 0f, 0.17f), new Vector3(0.15f, 0f, 0.17f),
                new Vector3(-0.15f, 0f, -0.17f), new Vector3(0.15f, 0f, -0.17f),
            };
            foreach (var offset in wheelOffsets)
                AddPrism(b, 0.07f, 0.08f, 0.13f, 0f, 1, offset);

            return Cache[key] = b.Build(key);
        }

        /// <summary>Flat sun term: tops ~1, camera-facing sides mid, far sides dark.</summary>
        static float Shade(Vector3 normal)
        {
            return Mathf.Clamp01(0.62f + 0.52f * Mathf.Max(0f, Vector3.Dot(normal.normalized, Sun)));
        }

        static Color Encode(Color c)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
        }

        static void AddPrism(Builder b, float sizeX, float height, float sizeZ, float radius, int cornerSegments, Vector3 baseCenter)
        {
            var ring = RoundedRect(sizeX * 0.5f, sizeZ * 0.5f, radius, cornerSegments);
            for (int i = 0; i < ring.Count; i++) ring[i] += baseCenter;
            b.Wall(ring, height);
            b.TopCap(ring, height);
        }

        /// <summary>Rounded-rect outline on the XZ plane, counter-clockwise seen from above.</summary>
        static List<Vector3> RoundedRect(float halfX, float halfZ, float radius, int cornerSegments)
        {
            radius = Mathf.Min(radius, halfX, halfZ);
            if (radius <= 0f)
            {
                return new List<Vector3>
                {
                    new Vector3(halfX, 0f, halfZ), new Vector3(-halfX, 0f, halfZ),
                    new Vector3(-halfX, 0f, -halfZ), new Vector3(halfX, 0f, -halfZ),
                };
            }

            var pts = new List<Vector3>(4 * (cornerSegments + 1));
            var centers = new[]
            {
                new Vector2(halfX - radius, halfZ - radius),
                new Vector2(-(halfX - radius), halfZ - radius),
                new Vector2(-(halfX - radius), -(halfZ - radius)),
                new Vector2(halfX - radius, -(halfZ - radius)),
            };
            for (int corner = 0; corner < 4; corner++)
            {
                float start = corner * 90f;
                for (int k = 0; k <= cornerSegments; k++)
                {
                    float ang = (start + 90f * k / cornerSegments) * Mathf.Deg2Rad;
                    pts.Add(new Vector3(
                        centers[corner].x + Mathf.Cos(ang) * radius,
                        0f,
                        centers[corner].y + Mathf.Sin(ang) * radius));
                }
            }
            return pts;
        }

        class Builder
        {
            readonly List<Vector3> verts = new List<Vector3>();
            readonly List<Color> colors = new List<Color>();
            readonly List<int> tris = new List<int>();

            /// <summary>Base color multiplied into the baked shade (white for tintable meshes).</summary>
            public Color Tint = Color.white;

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                var normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude < 1e-10f) return; // degenerate (r = 0 corner duplicates)
                var col = Shaded(normal);
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
                for (int k = 0; k < 4; k++) colors.Add(col);
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
                tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
            }

            /// <summary>Vertical wall along a base outline ordered counter-clockwise from above.</summary>
            public void Wall(IReadOnlyList<Vector3> bottom, float height)
            {
                var up = Vector3.up * height;
                for (int i = 0; i < bottom.Count; i++)
                {
                    int j = (i + 1) % bottom.Count;
                    Quad(bottom[j], bottom[i], bottom[i] + up, bottom[j] + up);
                }
            }

            /// <summary>Upward-facing cap fanned from the outline centroid.</summary>
            public void TopCap(IReadOnlyList<Vector3> bottom, float height)
            {
                var up = Vector3.up * height;
                var center = Vector3.zero;
                for (int i = 0; i < bottom.Count; i++) center += bottom[i];
                center = center / bottom.Count + up;

                var col = Shaded(Vector3.up);
                int ci = verts.Count;
                verts.Add(center); colors.Add(col);
                int start = verts.Count;
                for (int i = 0; i < bottom.Count; i++)
                {
                    verts.Add(bottom[i] + up);
                    colors.Add(col);
                }
                for (int i = 0; i < bottom.Count; i++)
                {
                    int j = (i + 1) % bottom.Count;
                    tris.Add(ci); tris.Add(start + j); tris.Add(start + i);
                }
            }

            Color Shaded(Vector3 normal)
            {
                var col = Encode(Tint * Shade(normal));
                col.a = 1f;
                return col;
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh();
#if !UNITY_LUNA
                // Luna's bridged Object.name is getter-only; the name is cosmetic anyway.
                mesh.name = name;
#endif
                // Array-property assignment: Luna's bridge doesn't implement the
                // List<> Set* overloads (colors silently drop → everything white).
                mesh.vertices = verts.ToArray();
                mesh.colors = colors.ToArray();
                mesh.triangles = tris.ToArray();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
