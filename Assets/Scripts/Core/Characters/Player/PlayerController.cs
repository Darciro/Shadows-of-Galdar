using UnityEngine;
using Pathfinding;
using TMPro;

[RequireComponent(typeof(Character), typeof(Seeker))]
public class PlayerController : MonoBehaviour
{
    private Character playerCharacter;
    private Camera cam;
    private Seeker seeker;

    [Header("Preview UI")]
    [Tooltip("UI TextMeshPro to show AP cost preview.")]
    public TextMeshProUGUI apCostTooltip;
    [Tooltip("Seconds between preview path requests.")]
    public float previewInterval = 0.1f;

    [Header("AP Settings")]
    [Tooltip("AP cost per node/segment in the A* path.")]
    public int apCostPerPathNode = 1;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogging = false;

    // Preview state
    private float lastPreviewTime;
    private Path previewPath;
    private Vector2 previewTarget;
    private bool isPreviewing;

    void Awake()
    {
        playerCharacter = GetComponent<Character>();
        seeker = GetComponent<Seeker>();
        cam = Camera.main;
        playerCharacter.IsPlayerControlled = true;
    }

    void Update()
    {
        UIManager.Instance.UpdatePlayerVitals();
        HandleClickInput();
        HandleKeyInput();
        if (GameManager.CurrentMode == GameMode.Combat && playerCharacter.IsMyTurn)
            HandleAPPreview();
        else if (isPreviewing)
            CancelPreview();
    }

    private void HandleClickInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        Vector3 world3D = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 world2D = new Vector2(world3D.x, world3D.y);

        // Check if clicking on a character (enemy)
        Collider2D hitCollider = Physics2D.OverlapPoint(world2D);
        if (hitCollider != null)
        {
            Character other = hitCollider.GetComponent<Character>();
            if (other != null && !other.IsPlayerControlled)
            {
                // Clicked on an enemy
                if (GameManager.CurrentMode == GameMode.Exploration)
                {
                    if (enableDebugLogging)
                        Debug.Log($"[PlayerController] RequestCombatStart vs {other.name}");
                    GameManager.Instance.RequestCombatStart(playerCharacter, other);
                }
                else if (GameManager.CurrentMode == GameMode.Combat && playerCharacter.IsMyTurn)
                {
                    if (enableDebugLogging)
                        Debug.Log($"[PlayerController] Attempting attack on {other.name}");
                    float distance = Vector3.Distance(playerCharacter.transform.position, other.transform.position);
                    if (distance <= playerCharacter.attackRange)
                    {
                        // In range: attempt direct attack
                        bool success = playerCharacter.HandleAttack(other);
                        if (!success && enableDebugLogging)
                            Debug.Log("[PlayerController] Attack failed (out of AP?)");
                    }
                    else
                    {
                        // Out of range: move toward the enemy, then attack if possible
                        Vector2 dirToTarget = ((Vector2)other.transform.position - (Vector2)playerCharacter.transform.position).normalized;
                        Vector2 moveGoal = (Vector2)other.transform.position - dirToTarget * (playerCharacter.attackRange * 0.8f);
                        // Calculate path to just within attack range
                        seeker.StartPath(playerCharacter.transform.position, moveGoal, (Path path) =>
                        {
                            if (path.error)
                            {
                                Debug.LogWarning("[PlayerController] Pathfinding error toward target: " + path.errorLog);
                                // Cannot find path (e.g., obstacle) – just end turn
                                playerCharacter.EndTurn();
                                return;
                            }
                            int segments = Mathf.Max(1, path.vectorPath.Count - 1);
                            int moveCost = segments * apCostPerPathNode;
                            if (moveCost <= playerCharacter.CurrentActionPoints)
                            {
                                if (enableDebugLogging)
                                    Debug.Log($"[PlayerController] Moving into range of {other.name} (cost {moveCost} AP)");
                                playerCharacter.QueueMoveAction(path, moveCost);
                                // After moving, attempt attack if we have enough AP left
                                int remainingAP = playerCharacter.CurrentActionPoints;
                                if (remainingAP >= playerCharacter.baseAttackAPCost)
                                {
                                    // Queue attack to execute after movement
                                    playerCharacter.QueueAction(() =>
                                    {
                                        Debug.Log($"[PlayerController] Automatically attacking {other.name} after moving.");
                                        UIManager.Instance.ShowDamagePopup(other.transform.position, playerCharacter.attackDamage);
                                        other.TakeDamage(playerCharacter.attackDamage);
                                        return true;
                                    });
                                    playerCharacter.SpendAP(playerCharacter.baseAttackAPCost);
                                }
                            }
                            else
                            {
                                if (enableDebugLogging)
                                    Debug.Log($"[PlayerController] Not enough AP to reach {other.name} this turn (need {moveCost}, have {playerCharacter.CurrentActionPoints}). Moving as far as possible.");
                                int availableAP = playerCharacter.CurrentActionPoints;
                                if (availableAP <= 0)
                                {
                                    // No AP (shouldn't happen here since we're in combat and out of range)
                                    playerCharacter.EndTurn();
                                    return;
                                }
                                // Determine how far we can get with available AP
                                int maxIndex = Mathf.Min(path.vectorPath.Count - 1, availableAP);
                                Vector3 partialTarget = path.vectorPath[maxIndex];
                                // Find a partial path to that reachable point
                                seeker.StartPath(playerCharacter.transform.position, partialTarget, (Path partialPath) =>
                                {
                                    if (partialPath.error)
                                    {
                                        Debug.LogWarning("[PlayerController] Partial path error: " + partialPath.errorLog);
                                        playerCharacter.EndTurn();
                                        return;
                                    }
                                    int partSegments = Mathf.Max(1, partialPath.vectorPath.Count - 1);
                                    int partCost = partSegments * apCostPerPathNode;
                                    if (partCost > availableAP) partCost = availableAP;
                                    if (enableDebugLogging)
                                        Debug.Log($"[PlayerController] Queuing partial move toward {other.name} (cost {partCost} AP)");
                                    playerCharacter.QueueMoveAction(partialPath, partCost);
                                    // No AP will remain, so no attack this turn.
                                });
                            }
                        });
                    }
                }
                CancelPreview();
                return;
            }
        }
        // Otherwise, clicked empty ground – treat as movement command
        if (GameManager.CurrentMode == GameMode.Exploration)
        {
            if (enableDebugLogging)
                Debug.Log($"[PlayerController] Moving to {world2D} in exploration mode");
            playerCharacter.HandleMovement(world3D);
        }
        else if (GameManager.CurrentMode == GameMode.Combat && playerCharacter.IsMyTurn)
        {
            // Use pre-computed preview path for movement in combat (to avoid path calc delay)
            if (previewPath != null)
            {
                int segments = Mathf.Max(1, previewPath.vectorPath.Count - 1);
                int cost = segments * apCostPerPathNode;
                if (cost <= playerCharacter.CurrentActionPoints)
                {
                    if (enableDebugLogging)
                        Debug.Log($"[PlayerController] Moving along preview path to {previewTarget} (cost {cost} AP)");
                    playerCharacter.QueueMoveAction(previewPath, cost);
                }
                else if (enableDebugLogging)
                {
                    Debug.Log($"[PlayerController] Not enough AP to move (need {cost}, have {playerCharacter.CurrentActionPoints})");
                }
            }
            CancelPreview();
        }
    }

    private void HandleKeyInput()
    {
        if (!playerCharacter.IsMyTurn) return;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[PlayerController] Player pressed End Turn (Space).");
            playerCharacter.EndTurn();
            TurnManager.Instance.NextTurn();
        }
    }

    private void HandleAPPreview()
    {
        // Continuously update the projected AP cost to move to current mouse position
        Vector3 mouseScreen = Input.mousePosition;
        Vector3 world3D = cam.ScreenToWorldPoint(mouseScreen);
        Vector2 world2D = new Vector2(world3D.x, world3D.y);
        // Position the tooltip near cursor
        apCostTooltip.rectTransform.position = mouseScreen + new Vector3(12f, -12f, 0f);
        // Throttle path recalculations
        if (Time.time - lastPreviewTime < previewInterval) return;
        lastPreviewTime = Time.time;
        previewTarget = world2D;
        seeker.CancelCurrentPathRequest();
        seeker.StartPath(playerCharacter.transform.position, previewTarget, OnPreviewPathComplete);
        isPreviewing = true;
    }

    private void OnPreviewPathComplete(Path p)
    {
        if (p.error)
        {
            apCostTooltip.gameObject.SetActive(false);
            return;
        }
        previewPath = p;
        int segments = Mathf.Max(1, p.vectorPath.Count - 1);
        int cost = segments * apCostPerPathNode;
        apCostTooltip.text = $"{cost} AP";
        apCostTooltip.gameObject.SetActive(true);
    }

    private void CancelPreview()
    {
        isPreviewing = false;
        previewPath = null;
        apCostTooltip.gameObject.SetActive(false);
        seeker.CancelCurrentPathRequest();
    }
}
