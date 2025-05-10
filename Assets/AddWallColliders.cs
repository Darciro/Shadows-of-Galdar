using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps; // Ensure this is present
using Edgar.Unity;

public class AddWallColliders : DungeonGeneratorPostProcessingComponentGrid2D
{
    private const string WallsLayerName = "Level 0 - Walls";

    public override void Run(DungeonGeneratorLevelGrid2D level)
    {
        Debug.Log($"[AddWallColliders] Run method executed for level: {level}");

        var wallsTilemapComponent = level
            .GetSharedTilemaps()
            .FirstOrDefault(tm => tm.gameObject.name == WallsLayerName);

        if (wallsTilemapComponent == null)
        {
            Debug.LogWarning($"[AddWallColliders] Tilemap on GameObject named '{WallsLayerName}' not found.");
            Debug.Log($"[AddWallColliders] Available shared tilemap GameObjects during this Run call:");
            foreach (var tmComponent in level.GetSharedTilemaps())
            {
                if (tmComponent != null && tmComponent.gameObject != null)
                {
                    Debug.Log($" - GameObject Name: {tmComponent.gameObject.name}, Tilemap: {tmComponent}");
                }
                else
                {
                    Debug.LogWarning($" - Found a null TilemapComponent or its GameObject in GetSharedTilemaps().");
                }
            }
            return;
        }

        GameObject wallsGameObject = wallsTilemapComponent.gameObject;
        Debug.Log($"[AddWallColliders] Found GameObject: '{wallsGameObject.name}'. Is GameObject null? {wallsGameObject == null}. Is active in hierarchy? {wallsGameObject.activeInHierarchy}. Is active self? {wallsGameObject.activeSelf}");

        if (wallsGameObject.GetComponent<Tilemap>() == null)
        {
            Debug.LogError($"[AddWallColliders] CRITICAL: The base Tilemap component is MISSING from '{wallsGameObject.name}' right before trying to add TilemapCollider2D.");
            return;
        }

        TilemapCollider2D tmc = wallsGameObject.GetComponent<TilemapCollider2D>();
        if (tmc == null)
        {
            Debug.Log($"[AddWallColliders] TilemapCollider2D not found on '{wallsGameObject.name}'. Attempting to add component...");
            tmc = wallsGameObject.AddComponent<TilemapCollider2D>();
            if (tmc == null)
            {
                Debug.LogError($"[AddWallColliders] CRITICAL FAILURE: wallsGameObject.AddComponent<TilemapCollider2D>() returned NULL for '{wallsGameObject.name}'.");
                return;
            }
            Debug.Log($"[AddWallColliders] TilemapCollider2D successfully ADDED to '{wallsGameObject.name}'. Component instance: {tmc}");
        }
        else
        {
            Debug.Log($"[AddWallColliders] TilemapCollider2D ALREADY EXISTS on '{wallsGameObject.name}'. Component instance: {tmc}");
        }

        if (tmc == null)
        {
            Debug.LogError($"[AddWallColliders] CRITICAL: 'tmc' is NULL right before setting usedByComposite on '{wallsGameObject.name}'.");
            return;
        }

        Debug.Log($"[AddWallColliders] Attempting to set 'usedByComposite = true' on tmc instance {tmc} for GameObject '{wallsGameObject.name}'.");
        try
        {
            tmc.usedByComposite = true;
            Debug.Log($"[AddWallColliders] Successfully set 'usedByComposite = true' for tmc on '{wallsGameObject.name}'.");
        }
        catch (MissingComponentException mce)
        {
            Debug.LogError($"[AddWallColliders] CATCHING MissingComponentException: When setting usedByComposite on '{wallsGameObject.name}'. TMC instance: {tmc}. GameObject still valid? {wallsGameObject != null}. Exception: {mce}");
            if (wallsGameObject != null && wallsGameObject.GetComponent<TilemapCollider2D>() == null)
            {
                Debug.LogError($"[AddWallColliders] Confirmed: TilemapCollider2D is indeed missing from '{wallsGameObject.name}' at the time of exception.");
            }
            else if (wallsGameObject != null)
            {
                Debug.LogWarning($"[AddWallColliders] Odd: TilemapCollider2D is reported as present by GetComponent after the exception was thrown for it being missing. Current TMC: {wallsGameObject.GetComponent<TilemapCollider2D>()}");
            }
            throw;
        }

        // --- MODIFIED ORDER ---
        // 4) Add (or get) a static Rigidbody2D FIRST
        Rigidbody2D rb = wallsGameObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.Log($"[AddWallColliders] Rigidbody2D not found on '{wallsGameObject.name}'. Adding component...");
            rb = wallsGameObject.AddComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError($"[AddWallColliders] Failed to add Rigidbody2D to '{wallsGameObject.name}'! AddComponent returned null.");
                return; // Critical if Rigidbody2D cannot be added
            }
            Debug.Log($"[AddWallColliders] Rigidbody2D added to '{wallsGameObject.name}'.");
        }
        else
        {
            Debug.Log($"[AddWallColliders] Rigidbody2D already exists on '{wallsGameObject.name}'.");
        }
        rb.bodyType = RigidbodyType2D.Static;
        Debug.Log($"[AddWallColliders] Rigidbody2D configured for '{wallsGameObject.name}'.");


        // 3) Add (or get) the CompositeCollider2D AFTER Rigidbody2D
        CompositeCollider2D cc = wallsGameObject.GetComponent<CompositeCollider2D>();
        if (cc == null)
        {
            Debug.Log($"[AddWallColliders] CompositeCollider2D not found on '{wallsGameObject.name}'. Adding component...");
            cc = wallsGameObject.AddComponent<CompositeCollider2D>();
            if (cc == null)
            { // This is where your log at line 99 was triggered
                Debug.LogError($"[AddWallColliders] Failed to add CompositeCollider2D to '{wallsGameObject.name}'! AddComponent returned null. This is after ensuring Rigidbody2D exists.");
                // Additional check: Is the GameObject still valid?
                if (wallsGameObject == null) Debug.LogError("[AddWallColliders] wallsGameObject became null before adding CompositeCollider2D!");
                else Debug.Log($"[AddWallColliders] wallsGameObject '{wallsGameObject.name}' still exists. Active: {wallsGameObject.activeInHierarchy}");
                return;
            }
            Debug.Log($"[AddWallColliders] CompositeCollider2D added to '{wallsGameObject.name}'.");
        }
        else
        {
            Debug.Log($"[AddWallColliders] CompositeCollider2D already exists on '{wallsGameObject.name}'.");
        }
        cc.geometryType = CompositeCollider2D.GeometryType.Outlines;
        cc.generationType = CompositeCollider2D.GenerationType.Synchronous;
        Debug.Log($"[AddWallColliders] CompositeCollider2D configured for '{wallsGameObject.name}'.");
        // --- END OF MODIFIED ORDER ---

        Debug.Log($"[AddWallColliders] All colliders successfully configured for '{WallsLayerName}'.");
    }
}