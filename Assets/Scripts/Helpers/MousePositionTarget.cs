using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(FollowerEntity))]
public class MousePositionTarget : MonoBehaviour
{
    // Note: use the interface type here
    private IAstarAI ai;

    void OnEnable()
    {
        // Grab whatever component implements IAstarAI
        ai = GetComponent<IAstarAI>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 dest = new Vector3(wp.x, wp.y, transform.position.z);
            ai.destination = dest;
            ai.SearchPath();
        }
    }

    void UpdateOLD()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("GetMouseButtonDown");
            // Convert click to world point
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 p = new Vector2(wp.x, wp.y);

            // Raycast against your 2D floor colliders (or drop this check if you just want 1:1 mapping)
            LayerMask floorMask = LayerMask.GetMask("Floor");
            RaycastHit2D hit = Physics2D.Raycast(p, Vector2.zero, 0f, floorMask);
            // var hit = Physics2D.Raycast(p, Vector2.zero);
            Debug.Log($"GetMouseButtonDown hit - {hit}");
            if (hit.collider != null)
            {
                // Preserve your isometric Z if you need it:
                Vector3 target = new Vector3(hit.point.x, hit.point.y, transform.position.z);
                Debug.Log($"GetMouseButtonDown hit target - {target}");

                // Now these members exist on IAstarAI
                ai.destination = target;
                ai.SearchPath();  // force an immediate re-scan
            }
        }
    }
}
