using UnityEngine;
using UnityEngine.Tilemaps;
using Edgar.Unity;

[CreateAssetMenu(fileName = "SetupWallTransparency", menuName = "Edgar/PostProcessing/Setup Wall Transparency")]
public class SetupWallTransparency : DungeonGeneratorPostProcessingGrid2D
{
    [Header("Shared Tilemap Settings")]
    [SerializeField] private string sharedWallTilemapName = "Level 0 - Walls";
    [SerializeField] private bool enableSetupDebugLogging = true;

    public override void Run(DungeonGeneratorLevelGrid2D generatedLevel)
    {
        var root = generatedLevel.RootGameObject;
        if (root == null)
        {
            Debug.LogError("[SetupWallTransparency] RootGameObject é null.");
            return;
        }

        Transform tilemapsRoot = root.transform.Find("Tilemaps") ?? root.transform;
        Transform wallTf = tilemapsRoot.Find(sharedWallTilemapName);
        if (wallTf == null)
        {
            if (enableSetupDebugLogging)
                Debug.LogWarning($"[SetupWallTransparency] '{sharedWallTilemapName}' não encontrado.");
            return;
        }

        var tm = wallTf.GetComponent<Tilemap>();
        if (tm == null)
        {
            Debug.LogWarning($"[SetupWallTransparency] '{sharedWallTilemapName}' não tem Tilemap.");
            return;
        }

        if (tm.GetComponent<MarkAsTransparentWall>() == null)
        {
            tm.gameObject.AddComponent<MarkAsTransparentWall>();
            if (enableSetupDebugLogging)
                Debug.Log($"[SetupWallTransparency] Marcador adicionado em '{tm.name}'.");
        }
    }
}
