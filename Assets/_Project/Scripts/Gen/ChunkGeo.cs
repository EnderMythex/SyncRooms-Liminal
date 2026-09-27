using System.Collections.Generic;
using SyncRooms.Player;
using UnityEngine;

namespace SyncRooms.Gen
{
    /// <summary>
    /// Runtime geometry builder (same conventions as the editor GeoBuilder): quads with UVs in meters,
    /// batched per material, emitted as MeshRenderer + MeshCollider under a parent.
    /// Unity front faces are clockwise: a quad (a,b,c,d) faces cross(b-a, d-a).
    /// </summary>
    public class ChunkGeo
    {
        class Buf { public readonly List<Vector3> v = new(); public readonly List<Vector3> n = new(); public readonly List<Vector2> uv = new(); public readonly List<int> t = new(); }
        readonly Dictionary<string, Buf> _bufs = new();
        readonly Dictionary<string, SurfaceType> _surf = new();
        public void Surface(string mat, SurfaceType s) => _surf[mat] = s;

        Buf B(string m) { if (!_bufs.TryGetValue(m, out var b)) _bufs[m] = b = new Buf(); return b; }

        public void Quad(string mat, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var n = Vector3.Cross(b - a, d - a).normalized;
            var buf = B(mat);
            int i = buf.v.Count;
            foreach (var p in new[] { a, b, c, d }) { buf.v.Add(p); buf.n.Add(n); buf.uv.Add(UV(p, n)); }
            buf.t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }

        static Vector2 UV(Vector3 p, Vector3 n)
        {
            var a = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
            if (a.y >= a.x && a.y >= a.z) return new Vector2(p.x, n.y > 0 ? p.z : -p.z);
            if (a.x >= a.z) return new Vector2(n.x > 0 ? -p.z : p.z, p.y);
            return new Vector2(n.z > 0 ? p.x : -p.x, p.y);
        }

        public void Floor(string mat, Rect r, float y, bool up = true)
        {
            var a = new Vector3(r.xMin, y, r.yMin); var b = new Vector3(r.xMax, y, r.yMin);
            var c = new Vector3(r.xMax, y, r.yMax); var d = new Vector3(r.xMin, y, r.yMax);
            if (up) Quad(mat, a, d, c, b); else Quad(mat, a, b, c, d);
        }

        /// <summary>Vertical face from p0 to p1 (XZ) facing the left side of p0->p1.</summary>
        public void Face(string mat, Vector2 p0, Vector2 p1, float y0, float y1) =>
            Quad(mat, new Vector3(p0.x, y0, p0.y), new Vector3(p1.x, y0, p1.y), new Vector3(p1.x, y1, p1.y), new Vector3(p0.x, y1, p0.y));

        public void Box(string mat, Vector3 min, Vector3 max, bool top = true, bool bottom = true)
        {
            var p000 = min; var p111 = max;
            var p100 = new Vector3(max.x, min.y, min.z); var p010 = new Vector3(min.x, max.y, min.z);
            var p001 = new Vector3(min.x, min.y, max.z); var p110 = new Vector3(max.x, max.y, min.z);
            var p101 = new Vector3(max.x, min.y, max.z); var p011 = new Vector3(min.x, max.y, max.z);
            Quad(mat, p000, p010, p110, p100);
            Quad(mat, p001, p101, p111, p011);
            Quad(mat, p000, p001, p011, p010);
            Quad(mat, p101, p100, p110, p111);
            if (top) Quad(mat, p010, p011, p111, p110);
            if (bottom) Quad(mat, p000, p100, p101, p001);
        }

        /// <summary>Two-sided wall of thickness t along an axis-aligned segment, with openings (at along wall, width, top).</summary>
        public void Wall(string matL, string matR, Vector2 from, Vector2 to, float t, float y0, float y1, List<Vector3> openings = null, string baseboard = null)
        {
            float len = (to - from).magnitude;
            var dir = (to - from) / len;
            var nrm = new Vector2(-dir.y, dir.x);
            var ops = openings ?? new List<Vector3>();
            ops.Sort((a, b) => a.x.CompareTo(b.x));
            float cur = 0;
            void Seg(float s0, float s1, float z0, float z1)
            {
                if (s1 - s0 < 0.001f || z1 - z0 < 0.001f) return;
                var a = from + dir * s0; var b = from + dir * s1;
                Face(matL, a + nrm * t * .5f, b + nrm * t * .5f, z0, z1);
                Face(matR, b - nrm * t * .5f, a - nrm * t * .5f, z0, z1);
                if (baseboard != null && z0 <= y0 + 0.001f)
                {
                    Face(baseboard, a + nrm * (t * .5f + 0.015f), b + nrm * (t * .5f + 0.015f), y0, y0 + 0.1f);
                    Face(baseboard, b - nrm * (t * .5f + 0.015f), a - nrm * (t * .5f + 0.015f), y0, y0 + 0.1f);
                }
            }
            foreach (var o in ops)
            {
                float s0 = o.x - o.y * .5f, s1 = o.x + o.y * .5f, top = o.z;
                Seg(cur, s0, y0, y1);
                Seg(s0, s1, top, y1);
                var a = from + dir * s0; var b = from + dir * s1;
                var l = nrm * t * .5f;
                Face(matL, a + l, a - l, y0, top);    // reveal facing +dir
                Face(matL, b - l, b + l, y0, top);    // reveal facing -dir
                Quad(matL, V(a - l, top), V(b - l, top), V(b + l, top), V(a + l, top)); // head, faces down
                cur = s1;
            }
            Seg(cur, len, y0, y1);
        }

        static Vector3 V(Vector2 p, float y) => new(p.x, y, p.y);

        public GameObject Emit(Transform parent, string name, bool collide = true)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            foreach (var kv in _bufs)
            {
                var b = kv.Value;
                if (b.v.Count == 0) continue;
                var m = new Mesh { name = kv.Key };
                if (b.v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(b.v); m.SetNormals(b.n); m.SetUVs(0, b.uv); m.SetTriangles(b.t, 0);
                m.RecalculateTangents(); m.RecalculateBounds();
                var go = new GameObject(kv.Key);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = m;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = Registry.Mat(kv.Key);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                bool transparent = mr.sharedMaterial && mr.sharedMaterial.GetFloat("_SurfaceType") > 0.5f;
                if (collide && !transparent) go.AddComponent<MeshCollider>().sharedMesh = m;
                if (_surf.TryGetValue(kv.Key, out var st)) go.AddComponent<SurfaceTag>().type = st;
            }
            _bufs.Clear();
            return root;
        }
    }
}
