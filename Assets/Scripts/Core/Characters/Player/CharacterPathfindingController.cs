using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(IAstarAI))]
[RequireComponent(typeof(Character))] // Ensure Character component is present
public class CharacterPathfindingController : MonoBehaviour
{
    private IAstarAI aiAgent;
    private Camera mainCamera;
    private Character combatant; // Reference to the Character component

    [Tooltip("The Z-coordinate of the plane on which pathfinding should occur (e.g., ground plane).")]
    public float pathfindingPlaneZ = 0f;

    // Example: For selecting an enemy to attack
    public Character selectedTargetEnemy = null;

    void Awake()
    {
        aiAgent = GetComponent<IAstarAI>();
        combatant = GetComponent<Character>(); // Get the Character component

        if (aiAgent == null)
        {
            Debug.LogError("[CharacterPathfindingController] No IAstarAI component found!", this);
            enabled = false;
            return;
        }
        if (combatant == null)
        {
            Debug.LogError("[CharacterPathfindingController] No Character component found!", this);
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[CharacterPathfindingController] Main Camera not found. Tag your camera 'MainCamera'.", this);
            enabled = false;
        }
    }

    void Start()
    {
        combatant.IsPlayerControlled = true; // Mark this as player controlled
    }

    void Update()
    {
        if (aiAgent == null || combatant == null) return;

        if (GameManager.Instance.CurrentMode == GameMode.Exploration)
        {
            HandleExplorationMovement();
        }
        else if (GameManager.Instance.CurrentMode == GameMode.Combat)
        {
            HandleCombatInput();
        }
    }

    void HandleExplorationMovement()
    {
        if (!aiAgent.canMove) aiAgent.canMove = true; // Ensure AI can move in exploration

        if (Input.GetMouseButtonDown(0))
        {
            // Potentially check if clicking on an enemy to initiate combat
            RaycastHit2D hit = Physics2D.Raycast(mainCamera.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                Character enemyCombatant = hit.collider.GetComponent<Character>();
                if (enemyCombatant != null && !enemyCombatant.IsPlayerControlled)
                {
                    Debug.Log($"[CharacterPathfindingController] Clicked on enemy {enemyCombatant.characterName} in exploration. Requesting combat.");
                    GameManager.Instance.RequestCombatStart(combatant, enemyCombatant);
                    return; // Don't move, combat will start
                }
            }
            // If not clicking an enemy, move
            SetDestinationToMousePosition(false);
        }
    }

    void HandleCombatInput()
    {
        if (!combatant.IsMyTurn)
        {
            // It's not our turn, AIPath should be stopped by Character script
            if (aiAgent.canMove) aiAgent.canMove = false;
            if (!aiAgent.isStopped) aiAgent.isStopped = true;
            return;
        }

        // It IS our turn
        Debug.Log($"[CharacterPathfindingController] Player's turn. AP: {combatant.CurrentActionPoints}. Waiting for input.");

        if (Input.GetMouseButtonDown(0)) // Left click for actions
        {
            RaycastHit2D hit = Physics2D.Raycast(mainCamera.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                Character targetEnemy = hit.collider.GetComponent<Character>();
                if (targetEnemy != null && !targetEnemy.IsPlayerControlled)
                {
                    Debug.Log($"[CharacterPathfindingController] Player clicked on enemy {targetEnemy.characterName} during combat turn.");
                    selectedTargetEnemy = targetEnemy;
                    // UI could show attack options, or directly try to attack
                    combatant.TryAttack(selectedTargetEnemy);
                    return;
                }
            }
            // If not clicking an enemy, or clicking empty ground
            SetDestinationToMousePosition(true); // true for combat mode move
        }

        // Example: End turn with a key press (e.g., Space bar)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[CharacterPathfindingController] Player pressed Space to end turn.");
            combatant.EndTurn();
        }
    }

    void SetDestinationToMousePosition(bool isCombatMove)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.forward, new Vector3(0, 0, pathfindingPlaneZ));

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 worldPoint = ray.GetPoint(enter);

            if (isCombatMove)
            {
                if (combatant.IsMyTurn)
                {
                    Debug.Log($"[CharacterPathfindingController] Player requesting move to {worldPoint} in combat.");
                    combatant.RequestMoveTo(worldPoint); // This will handle AP and pathing
                }
            }
            else // Exploration mode
            {
                aiAgent.destination = worldPoint;
                if (aiAgent.canSearch)
                {
                    aiAgent.SearchPath();
                }
            }
        }
        else
        {
            Debug.LogWarning("[CharacterPathfindingController] Mouse click ray did not intersect the pathfinding plane.");
        }
    }
}
