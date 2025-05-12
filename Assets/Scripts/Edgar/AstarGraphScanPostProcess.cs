using UnityEngine;
using UnityEngine.Tilemaps;
using Edgar.Unity; // Your Edgar Pro namespace
using Pathfinding; // A* Pathfinding Project namespace
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Edgar Pro Post-Processing ScriptableObject to scan the A* Pathfinding Project graph
/// after a dungeon has been generated. It configures a GridGraph to fit the generated level.
/// </summary>
[CreateAssetMenu(fileName = "AstarGraphScanPostProcess", menuName = "Edgar/PostProcessing/A* Graph Scan")]
public class AstarGraphScanPostProcess : DungeonGeneratorPostProcessingGrid2D // Or your specific base class
{
    [Header("A* Graph Settings")]
    [Tooltip("The physics layer(s) that represent obstacles for pathfinding (e.g., walls).")]
    public LayerMask obstacleLayerMask;

    [Tooltip("Node size for the GridGraph. Should match your dungeon's cell size.")]
    public float nodeSize = 1f;

    [Tooltip("Diameter of the agent for collision checking. Usually slightly less than nodeSize.")]
    public float collisionDiameter = 0.9f;

    [Tooltip("Padding to add around the calculated bounds of the dungeon, in number of nodes.")]
    public int paddingNodes = 2;

    [Tooltip("The Z-position of the graph. For 2D XY games, this is often 0.")]
    public float graphZPosition = 0f;


    public override void Run(DungeonGeneratorLevelGrid2D generatedLevel)
    {

        if (AstarPath.active == null)
        {
            Debug.LogError("[AstarGraphScanPostProcess] AstarPath.active is null. Is there a Pathfinder GameObject in the scene with the AstarPath component?");
            return;
        }

        GridGraph gridGraph = AstarPath.active.data.gridGraph;
        if (gridGraph == null && AstarPath.active.data.graphs != null && AstarPath.active.data.graphs.Length > 0)
        {
            gridGraph = AstarPath.active.data.graphs[0] as GridGraph;
        }

        if (gridGraph == null)
        {
            Debug.LogError("[AstarGraphScanPostProcess] No GridGraph found in A* Pathfinding Project settings. Please add one to the Pathfinder GameObject.");
            return;
        }

        Bounds dungeonBounds = CalculateDungeonBounds(generatedLevel);
        if (dungeonBounds.size == Vector3.zero)
        {
            Debug.LogWarning("[AstarGraphScanPostProcess] Could not determine dungeon bounds. Skipping graph update.");
            return;
        }

        // --- Configure GridGraph ---
        gridGraph.nodeSize = this.nodeSize;

        // Collision settings
        // The line below caused CS0234: 'ColliderType' does not exist in the namespace 'Pathfinding'.
        // This indicates an issue with your A* Pathfinding Project Pro version or import.
        // For now, ensure 'Collider Type' (e.g., Capsule or Sphere) is set manually in the Unity Editor on your GridGraph.
        // gridGraph.collision.type = Pathfinding.ColliderType.Capsule; 
        Debug.LogWarning("[AstarGraphScanPostProcess] gridGraph.collision.type is NOT being set by script due to a compile error with Pathfinding.ColliderType. Please ensure it's configured correctly in the Unity Editor on the GridGraph component.");

        gridGraph.collision.diameter = this.collisionDiameter;
        // gridGraph.collision.height = this.nodeSize; // Only relevant for Capsule type if not 2D. For 2D Capsule, diameter is key.
        gridGraph.collision.use2D = true;
        gridGraph.collision.mask = this.obstacleLayerMask;

        gridGraph.is2D = true;
        gridGraph.rotation = Vector3.zero;
        gridGraph.aspectRatio = 1f;
        gridGraph.isometricAngle = 0f;
        gridGraph.uniformEdgeCosts = true;
        gridGraph.neighbours = NumNeighbours.Eight;

        Vector3 graphCenter = dungeonBounds.center;
        graphCenter.z = graphZPosition;
        gridGraph.center = graphCenter;

        int widthNodes = Mathf.Max(1, Mathf.CeilToInt(dungeonBounds.size.x / gridGraph.nodeSize) + (paddingNodes * 2));
        int depthNodes = Mathf.Max(1, Mathf.CeilToInt(dungeonBounds.size.y / gridGraph.nodeSize) + (paddingNodes * 2));

        gridGraph.SetDimensions(widthNodes, depthNodes, gridGraph.nodeSize);

        string obstacleLayerName = "";
        for (int i = 0; i < 32; i++)
        {
            if ((obstacleLayerMask.value & (1 << i)) != 0)
            {
                obstacleLayerName = LayerMask.LayerToName(i);
                break;
            }
        }
        if (string.IsNullOrEmpty(obstacleLayerName) && obstacleLayerMask.value != 0) obstacleLayerName = "Multiple/Mixed";
        else if (obstacleLayerMask.value == 0) obstacleLayerName = "Nothing";


        AstarPath.active.Scan();
    }

    private Bounds CalculateDungeonBounds(DungeonGeneratorLevelGrid2D generatedLevel)
    {
        var allSharedTilemaps = generatedLevel.GetSharedTilemaps();
        if (allSharedTilemaps == null || !allSharedTilemaps.Any())
        {
            Debug.LogWarning("[AstarGraphScanPostProcess - CalculateBounds] No shared tilemaps found.");
            return new Bounds();
        }

        Bounds combinedBounds = new Bounds();
        bool firstTilemap = true;

        foreach (Tilemap tilemap in allSharedTilemaps)
        {
            if (tilemap == null) continue;

            tilemap.CompressBounds();
            BoundsInt cellBounds = tilemap.cellBounds;

            if (cellBounds.size.x == 0 && cellBounds.size.y == 0 && cellBounds.size.z == 0) continue;

            Vector3 minWorld = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxWorld = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool hasTilesInThisMap = false;

            for (int x = cellBounds.xMin; x < cellBounds.xMax; x++)
            {
                for (int y = cellBounds.yMin; y < cellBounds.yMax; y++)
                {
                    Vector3Int localPos = new Vector3Int(x, y, cellBounds.zMin);
                    if (tilemap.HasTile(localPos))
                    {
                        hasTilesInThisMap = true;
                        Vector3 worldCellPos = tilemap.CellToWorld(localPos);

                        minWorld.x = Mathf.Min(minWorld.x, worldCellPos.x);
                        minWorld.y = Mathf.Min(minWorld.y, worldCellPos.y);

                        maxWorld.x = Mathf.Max(maxWorld.x, worldCellPos.x + tilemap.cellSize.x);
                        maxWorld.y = Mathf.Max(maxWorld.y, worldCellPos.y + tilemap.cellSize.y);
                    }
                }
            }

            if (hasTilesInThisMap)
            {
                Bounds currentTilemapWorldBounds = new Bounds();
                currentTilemapWorldBounds.SetMinMax(minWorld, maxWorld);
                if (firstTilemap)
                {
                    combinedBounds = currentTilemapWorldBounds;
                    firstTilemap = false;
                }
                else
                {
                    combinedBounds.Encapsulate(currentTilemapWorldBounds);
                }
            }
        }
        if (firstTilemap) Debug.LogWarning("[AstarGraphScanPostProcess - CalculateBounds] No tiles found in any shared tilemaps to calculate bounds.");
        return combinedBounds;
    }
}
