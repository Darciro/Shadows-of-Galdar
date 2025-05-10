using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WallTransparencyController : MonoBehaviour
{
    public static WallTransparencyController Instance { get; private set; }

    [Header("Player Settings")]
    [SerializeField] private string playerTag = "Player";
    private Transform playerTransform;

    [Header("Fade Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float fadedAlpha = 0.4f;
    [SerializeField] private float fadeRadius = 2.5f;
    [SerializeField] private float fadeSpeed = 10f;

    [Header("Debugging")]
    [SerializeField] private bool enableDebugLogging = false;

    // Dados internos para cada tilemap registado
    private class ManagedTilemapData
    {
        public Tilemap Tilemap;
        public Dictionary<Vector3Int, Color> originalColors = new Dictionary<Vector3Int, Color>();
        public HashSet<Vector3Int> fadedCells = new HashSet<Vector3Int>();
        public Color fadedColor;

        public ManagedTilemapData(Tilemap tilemap, float alpha)
        {
            Tilemap = tilemap;
            var baseColor = tilemap.color;
            fadedColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }

    private List<ManagedTilemapData> managedTilemaps = new List<ManagedTilemapData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        FindPlayer();
        RegisterAllMarkedTilemaps();
    }

    private void FindPlayer()
    {
        var playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
            playerTransform = playerObj.transform;
        else if (enableDebugLogging)
            Debug.LogWarning($"[WallTransparencyController] Jogador com tag '{playerTag}' não encontrado.");
    }

    private void RegisterAllMarkedTilemaps()
    {
        var markers = FindObjectsOfType<MarkAsTransparentWall>();
        foreach (var marker in markers)
        {
            var tm = marker.GetComponent<Tilemap>();
            if (tm != null)
                RegisterWallTilemap(tm);
        }
        if (enableDebugLogging)
            Debug.Log($"[WallTransparencyController] {managedTilemaps.Count} tilemaps registadas.");
    }

    public void RegisterWallTilemap(Tilemap tilemap)
    {
        if (managedTilemaps.Exists(x => x.Tilemap == tilemap))
            return;

        managedTilemaps.Add(new ManagedTilemapData(tilemap, fadedAlpha));
        if (enableDebugLogging)
            Debug.Log($"[WallTransparencyController] Tilemap '{tilemap.name}' registada.");
    }

    private void LateUpdate()
    {
        if (playerTransform == null)
            return;

        var playerPos = playerTransform.position;
        foreach (var data in managedTilemaps)
        {
            var tm = data.Tilemap;
            int cellRadius = Mathf.CeilToInt(fadeRadius / Mathf.Max(tm.cellSize.x, tm.cellSize.y));
            Vector3Int playerCell = tm.WorldToCell(playerPos);

            var toFade = new HashSet<Vector3Int>();

            // Determinar que células devem desvanecer
            for (int dx = -cellRadius; dx <= cellRadius; dx++)
                for (int dy = -cellRadius; dy <= cellRadius; dy++)
                {
                    var cell = new Vector3Int(playerCell.x + dx, playerCell.y + dy, playerCell.z);
                    if (!tm.HasTile(cell)) continue;
                    var worldCenter = tm.GetCellCenterWorld(cell);
                    if (Vector3.Distance(worldCenter, playerPos) <= fadeRadius)
                        toFade.Add(cell);
                }

            // Restaurar células que já não estão no raio
            var toRestore = new List<Vector3Int>();
            foreach (var cell in data.fadedCells)
                if (!toFade.Contains(cell))
                    toRestore.Add(cell);

            foreach (var cell in toRestore)
            {
                tm.SetTileFlags(cell, TileFlags.None);
                var orig = data.originalColors.TryGetValue(cell, out var c) ? c : tm.color;
                var current = tm.GetColor(cell);
                var col = Color.Lerp(current, orig, fadeSpeed * Time.deltaTime);
                tm.SetColor(cell, col);
                if (Mathf.Approximately(col.a, orig.a))
                {
                    tm.SetColor(cell, orig);
                    data.fadedCells.Remove(cell);
                    data.originalColors.Remove(cell);
                }
            }

            // Aplicar fade às novas células
            foreach (var cell in toFade)
            {
                tm.SetTileFlags(cell, TileFlags.None);
                if (!data.fadedCells.Contains(cell))
                {
                    data.originalColors[cell] = tm.GetColor(cell);
                    data.fadedCells.Add(cell);
                }
                var curr = tm.GetColor(cell);
                var target = data.fadedColor;
                tm.SetColor(cell, Color.Lerp(curr, target, fadeSpeed * Time.deltaTime));
            }
        }
    }
}