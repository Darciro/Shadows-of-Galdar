using UnityEngine;
using UnityEngine.Tilemaps;
using Edgar.Unity;

[CreateAssetMenu(
    fileName = "SetupFloorCollider",
    menuName = "Edgar/PostProcessing/Setup Floor Collider"
)]
public class SetupFloorCollider : DungeonGeneratorPostProcessingGrid2D
{
    [Header("Shared Tilemap Settings")]
    [SerializeField]
    private string sharedFloorTilemapName = "Level 0 - Floor";
    [SerializeField]
    private bool enableSetupDebugLogging = true;

    public override void Run(DungeonGeneratorLevelGrid2D generatedLevel)
    {
        // 1) grab the root of the generated level
        var root = generatedLevel.RootGameObject;
        if (root == null)
        {
            Debug.LogError("[SetupFloorCollider] RootGameObject is null.");
            return;
        }

        // 2) find the Tilemaps container (or fallback to the root)
        Transform tilemapsRoot = root.transform.Find("Tilemaps") ?? root.transform;

        // 3) find our specific floor layer
        Transform floorTf = tilemapsRoot.Find(sharedFloorTilemapName);
        if (floorTf == null)
        {
            if (enableSetupDebugLogging)
                Debug.LogWarning(
                    $"[SetupFloorCollider] '{sharedFloorTilemapName}' not found."
                );
            return;
        }

        // 4) get the Tilemap component
        var tm = floorTf.GetComponent<Tilemap>();
        if (tm == null)
        {
            Debug.LogWarning(
                $"[SetupFloorCollider] '{sharedFloorTilemapName}' has no Tilemap."
            );
            return;
        }

        // 5) add a TilemapCollider2D if it doesn’t already exist
        if (tm.GetComponent<TilemapCollider2D>() == null)
        {
            var col = tm.gameObject.AddComponent<TilemapCollider2D>();
            col.usedByComposite = true;   // if you later want to add a CompositeCollider2D
            if (enableSetupDebugLogging)
                Debug.Log(
                    $"[SetupFloorCollider] TilemapCollider2D added to '{tm.name}'."
                );
        }
    }
}