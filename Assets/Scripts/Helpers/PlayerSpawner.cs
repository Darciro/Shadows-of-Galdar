using System.Linq;
using Edgar.Unity;
using UnityEngine;

public class PlayerSpawner : DungeonGeneratorPostProcessingComponentGrid2D
{
    [Tooltip("If you want to instantiate a new player, assign the prefab here.  If null, will move existing tagged Player.")]
    public GameObject PlayerPrefab;

    public override void Run(DungeonGeneratorLevelGrid2D level)
    {
        // 1) Find the very first room in your graph (or filter by some condition)
        var firstRoom = level.RoomInstances.First();
        // 2) Grab its instantiated prefab
        var roomGO = firstRoom.RoomTemplateInstance;
        // 3) Find the spawn‐marker inside it
        var spawnMarker = roomGO.transform.Find("PlayerSpawn");
        if (spawnMarker == null)
        {
            Debug.LogWarning($"[{nameof(PlayerSpawner)}] 'PlayerSpawn' not found in {roomGO.name}");
            return;
        }

        // 4a) Move existing Player
        var existing = GameObject.FindWithTag("Player");
        if (existing != null && PlayerPrefab == null)
        {
            existing.transform.position = spawnMarker.position;
            return;
        }

        // 4b) Or instantiate a new Player
        if (PlayerPrefab != null)
        {
            var player = Instantiate(PlayerPrefab, spawnMarker.position, Quaternion.identity);
            player.tag = "Player";
        }
    }
}
