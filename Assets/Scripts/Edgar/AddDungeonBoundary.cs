using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Edgar.Unity;

[CreateAssetMenu(fileName = "AddDungeonBoundary", menuName = "Edgar/PostProcessing/Add Dungeon Boundary")]
public class AddDungeonBoundary : DungeonGeneratorPostProcessingGrid2D
{
    [Header("Tilemap Settings")]
    [SerializeField] private string sharedWallTilemapName = "Level 0 - Walls";

    [Header("Physics Settings")]
    [SerializeField] private string boundaryLayerName = "WorldBoundary";

    [Tooltip("Tolerance when matching segment endpoints.")]
    [SerializeField] private float epsilon = 0.01f;

    public override void Run(DungeonGeneratorLevelGrid2D generatedLevel)
    {
        var root = generatedLevel.RootGameObject;
        if (root == null)
        {
            Debug.LogError("[AddDungeonBoundary] RootGameObject is null.");
            return;
        }

        // find the final shared wall tilemap
        var wallTf = root.transform.Find($"Tilemaps/{sharedWallTilemapName}")
                  ?? root.transform.Find(sharedWallTilemapName);
        if (wallTf == null)
        {
            Debug.LogWarning($"[AddDungeonBoundary] '{sharedWallTilemapName}' not found.");
            return;
        }

        var tm = wallTf.GetComponent<Tilemap>();
        if (tm == null)
        {
            Debug.LogWarning("[AddDungeonBoundary] Tilemap component missing.");
            return;
        }

        // gather raw edges around every wall tile
        tm.CompressBounds();
        var b = tm.cellBounds;
        var origin = tm.CellToWorld(new Vector3Int(b.xMin, b.yMin, 0));
        var right = tm.CellToWorld(new Vector3Int(b.xMin + 1, b.yMin, 0)) - origin;
        var up = tm.CellToWorld(new Vector3Int(b.xMin, b.yMin + 1, 0)) - origin;

        var rawEdges = new List<(Vector2 a, Vector2 b)>();
        for (int x = b.xMin; x < b.xMax; x++)
            for (int y = b.yMin; y < b.yMax; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (!tm.HasTile(cell)) continue;

                var bl = tm.CellToWorld(cell);
                var br = bl + right;
                var tl = bl + up;
                var tr = bl + right + up;

                if (!tm.HasTile(cell + Vector3Int.down)) rawEdges.Add((bl, br));
                if (!tm.HasTile(cell + Vector3Int.left)) rawEdges.Add((bl, tl));
                if (!tm.HasTile(cell + Vector3Int.right)) rawEdges.Add((br, tr));
                if (!tm.HasTile(cell + Vector3Int.up)) rawEdges.Add((tl, tr));
            }

        // stitch segments into loops
        var loops = BuildEdgeLoops(rawEdges, epsilon);
        if (loops.Count == 0)
        {
            Debug.LogError("[AddDungeonBoundary] No boundary loops found.");
            return;
        }

        // pick the outermost loop by maximum perimeter
        var outer = loops
            .Select(loop => (points: loop, perim: ComputePerimeter(loop)))
            .OrderByDescending(t => t.perim)
            .First().points;

        // create a single EdgeCollider2D
        var go = new GameObject("DungeonBoundary");
        go.transform.SetParent(root.transform, false);
        var edge = go.AddComponent<EdgeCollider2D>();
        edge.points = outer.ToArray();

        if (!string.IsNullOrEmpty(boundaryLayerName))
        {
            int layer = LayerMask.NameToLayer(boundaryLayerName);
            if (layer >= 0) go.layer = layer;
            else Debug.LogWarning($"[AddDungeonBoundary] Layer '{boundaryLayerName}' not found.");
        }
    }

    private List<List<Vector2>> BuildEdgeLoops(List<(Vector2 a, Vector2 b)> edges, float eps)
    {
        var loops = new List<List<Vector2>>();
        var remaining = new List<(Vector2 a, Vector2 b)>(edges);

        while (remaining.Count > 0)
        {
            var chain = new List<Vector2>();
            var e = remaining[0];
            remaining.RemoveAt(0);
            chain.Add(e.a);
            chain.Add(e.b);

            var current = e.b;
            bool extended;
            do
            {
                extended = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    var seg = remaining[i];
                    if (Vector2.Distance(seg.a, current) < eps)
                    {
                        current = seg.b;
                        chain.Add(current);
                        remaining.RemoveAt(i);
                        extended = true;
                        break;
                    }
                    if (Vector2.Distance(seg.b, current) < eps)
                    {
                        current = seg.a;
                        chain.Add(current);
                        remaining.RemoveAt(i);
                        extended = true;
                        break;
                    }
                }
            } while (extended);

            loops.Add(chain);
        }

        return loops;
    }

    private float ComputePerimeter(List<Vector2> loop)
    {
        float sum = 0;
        for (int i = 1; i < loop.Count; i++)
            sum += Vector2.Distance(loop[i - 1], loop[i]);
        return sum;
    }
}
