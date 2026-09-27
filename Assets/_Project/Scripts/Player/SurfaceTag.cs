using UnityEngine;

namespace SyncRooms.Player
{
    public enum SurfaceType { Concrete, Carpet, Tile, Wood, Grass, Water, Metal }

    /// <summary>Marks a collider with its footstep surface. Falls back to material-name heuristics.</summary>
    public class SurfaceTag : MonoBehaviour
    {
        public SurfaceType type = SurfaceType.Concrete;

        public static SurfaceType Resolve(Collider col)
        {
            if (col == null) return SurfaceType.Concrete;
            if (col.TryGetComponent(out SurfaceTag tag)) return tag.type;
            var parentTag = col.GetComponentInParent<SurfaceTag>();
            if (parentTag) return parentTag.type;
            if (col.TryGetComponent(out Renderer r) && r.sharedMaterial)
            {
                string n = r.sharedMaterial.name;
                if (n.Contains("Carpet")) return SurfaceType.Carpet;
                if (n.Contains("Tile") || n.Contains("Marble")) return SurfaceType.Tile;
                if (n.Contains("Parquet") || n.Contains("Wood") || n.Contains("Walnut")) return SurfaceType.Wood;
                if (n.Contains("Grass") || n.Contains("Path")) return SurfaceType.Grass;
                if (n.Contains("Metal") || n.Contains("Rust")) return SurfaceType.Metal;
            }
            return SurfaceType.Concrete;
        }

        public static string FootstepSet(SurfaceType t) => t switch
        {
            SurfaceType.Carpet => "Footsteps/fs_carpet",
            SurfaceType.Tile => "Footsteps/fs_tile",
            SurfaceType.Wood => "Footsteps/fs_wood",
            SurfaceType.Grass => "Footsteps/fs_grass",
            SurfaceType.Water => "Footsteps/fs_water",
            _ => "Footsteps/fs_concrete",
        };
    }
}
