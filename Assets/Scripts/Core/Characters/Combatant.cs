using UnityEngine;
using Pathfinding; // For IAstarAI and Seeker
using System.Collections.Generic;
using System.Collections; // For IEnumerator

public class Combatant : MonoBehaviour
{
    [Header("Stats")]
    public string characterName = "Character";
    public int maxHealth = 100;
    public int currentHealth;
    public int maxActionPoints = 10;
    public int currentActionPoints;
    public int initiative = 10;

    [Header("Movement")]
    [Tooltip("AP cost per node/segment in the A* path.")]
    public int apCostPerPathNode = 1;
    public float moveSpeedInCombat = 3f;
    private float originalAISpeed;
    private Path currentPathForAP; // Store path to calculate AP cost

    [Header("Attack")]
    public int baseAttackAPCost = 2;
    public int attackDamage = 10;
    public float attackRange = 1.5f;

    [Header("Debugging")]
    [SerializeField] private bool enableDebugLogging = false;

    public bool IsPlayerControlled { get; set; } = false;
    public bool IsMyTurn { get; set; } = false;

    private IAstarAI aiAgent;
    private Seeker seeker;
    private EnemyAIController enemyAIController; // Link to enemy AI logic
    // private CharacterPathfindingController playerController; // Link to player input logic (optional here)

    public delegate bool CombatAction();
    private Queue<CombatAction> actionQueue = new Queue<CombatAction>();
    private bool isPerformingAction = false;
    private bool waitingForPathForAPCost = false;

    public int ActionQueueCount => actionQueue.Count;

    void Awake()
    {
        aiAgent = GetComponent<IAstarAI>();
        seeker = GetComponent<Seeker>();
        enemyAIController = GetComponent<EnemyAIController>(); // Get if this is an enemy
        // playerController = GetComponent<CharacterPathfindingController>(); // Get if this is the player

        if (aiAgent == null) Debug.LogError($"[Combatant] {gameObject.name} is missing an IAstarAI component!", this);
        if (seeker == null) Debug.LogError($"[Combatant] {gameObject.name} is missing a Seeker component!", this);

        currentHealth = maxHealth;
    }

    void Start()
    {
        if (aiAgent != null)
        {
            originalAISpeed = aiAgent.maxSpeed;
        }
        // Register with GameManager if it exists and this combatant is added dynamically
        // GameManager.Instance?.RefreshCombatantList(); // Or have a dedicated register method
    }

    public void OnCombatStart()
    {
        Debug.Log($"[Combatant] {characterName} ({gameObject.name}) entering combat mode.");
        actionQueue.Clear();
        isPerformingAction = false;
        if (aiAgent != null)
        {
            aiAgent.isStopped = true;
            aiAgent.canMove = false;
            aiAgent.maxSpeed = moveSpeedInCombat;
        }
    }

    public void OnCombatEnd()
    {
        Debug.Log($"[Combatant] {characterName} ({gameObject.name}) exiting combat mode.");
        IsMyTurn = false;
        actionQueue.Clear();
        isPerformingAction = false;
        if (aiAgent != null)
        {
            aiAgent.isStopped = false;
            aiAgent.canMove = true;
            aiAgent.maxSpeed = originalAISpeed;
            aiAgent.SetPath(null);

            if (enemyAIController != null)
            {
                enemyAIController.ReturnToExplorationBehavior();
            }
            // If player, CharacterPathfindingController will resume its normal update
        }
    }

    public void StartTurn()
    {
        Debug.Log($"[Combatant] {characterName}'s turn (Instance ID: {this.GetInstanceID()}). AP: {maxActionPoints}");
        IsMyTurn = true;
        currentActionPoints = maxActionPoints;
        actionQueue.Clear();
        isPerformingAction = false;

        if (aiAgent != null)
        {
            aiAgent.isStopped = true; // Start stopped, actions will enable movement
            aiAgent.canMove = false;  // AIPath won't move on its own
        }

        if (!IsPlayerControlled && enemyAIController != null)
        {
            enemyAIController.PlanCombatTurn();
        }
        else if (IsPlayerControlled)
        {
            // Player's turn: UI should enable, input script listens for commands
            Debug.Log($"[Combatant] {characterName} (Player) turn started. Waiting for input.");
        }
    }

    public void EndTurn()
    {
        if (!IsMyTurn) return;
        Debug.Log($"[Combatant] {characterName} ending turn. AP Left: {currentActionPoints}");
        IsMyTurn = false;
        if (aiAgent != null)
        {
            aiAgent.isStopped = true;
            aiAgent.canMove = false;
            aiAgent.SetPath(null);
        }
        TurnManager.Instance?.EndCurrentTurn(); // Null check for safety
    }

    /// <summary>
    /// Calculates AP cost for a path and then, if affordable, queues the move action.
    /// </summary>
    public void RequestMoveTo(Vector3 targetPosition)
    {
        if (!IsMyTurn || isPerformingAction || aiAgent == null || seeker == null || waitingForPathForAPCost) return;

        waitingForPathForAPCost = true;
        seeker.StartPath(transform.position, targetPosition, (Path p) =>
        {
            OnPathReceivedForAPCost(p, targetPosition);
        });
    }

    void OnPathReceivedForAPCost(Path p, Vector3 targetPosition)
    {
        waitingForPathForAPCost = false;
        if (!IsMyTurn) return; // Turn might have ended while waiting for path

        if (p.error)
        {
            Debug.LogError($"[Combatant] {characterName} path error to {targetPosition}: {p.errorLog}");
            return;
        }

        currentPathForAP = p; // Store the path
        int apCost = (p.path.Count > 0) ? (p.path.Count - 1) * apCostPerPathNode : 0; // Cost based on number of nodes/segments
        if (apCost == 0 && Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            // If path is 0 nodes but target isn't current pos, maybe 1 AP for very short move
            apCost = apCostPerPathNode;
        }


        if (currentActionPoints >= apCost)
        {
            Debug.Log($"[Combatant] {characterName} moving to {targetPosition}. Path Nodes: {p.path.Count}, Cost: {apCost} AP. Remaining AP: {currentActionPoints - apCost}");

            QueueAction(() =>
            {
                // This action executes the move using the pre-calculated path
                if (aiAgent.isStopped || !aiAgent.hasPath) // If not already moving or path invalidated
                {
                    aiAgent.isStopped = false;
                    aiAgent.canMove = true;
                    aiAgent.SetPath(currentPathForAP); // Use the path we got for AP calculation
                    Debug.Log($"[Combatant ActionQueue - Move] {characterName} starting A* movement. HasPath: {aiAgent.hasPath}");
                }

                if (aiAgent.reachedDestination || !aiAgent.hasPath)
                {
                    Debug.Log($"[Combatant ActionQueue - Move] {characterName} finished move action. Reached: {aiAgent.reachedDestination}, HasPath: {aiAgent.hasPath}");
                    aiAgent.isStopped = true;
                    aiAgent.canMove = false;
                    currentPathForAP = null; // Clear stored path
                    return true; // Action complete
                }
                return false; // Action ongoing
            });

            SpendAP(apCost);
        }
        else
        {
            Debug.LogWarning($"[Combatant] {characterName} cannot move to {targetPosition}. Not enough AP. Need: {apCost}, Have: {currentActionPoints}");
            currentPathForAP = null;
        }
    }


    public bool TryAttack(Combatant target)
    {
        if (!IsMyTurn || isPerformingAction || target == null) return false;

        if (currentActionPoints >= baseAttackAPCost)
        {
            if (Vector3.Distance(transform.position, target.transform.position) <= attackRange)
            {
                QueueAction(() =>
                {
                    Debug.Log($"[Combatant ActionQueue - Attack] {characterName} attacking {target.characterName} for {attackDamage} damage. Cost: {baseAttackAPCost} AP.");
                    UIManager.Instance.ShowDamagePopup(target.transform.position, attackDamage);
                    target.TakeDamage(attackDamage);
                    return true; // Action complete
                });
                SpendAP(baseAttackAPCost);
                return true;
            }
            else
            {
                Debug.LogWarning($"[Combatant] {characterName} cannot attack {target.characterName}. Target out of range.");
                return false;
            }
        }
        Debug.LogWarning($"[Combatant] {characterName} cannot attack. Not enough AP. Need: {baseAttackAPCost}, Have: {currentActionPoints}");
        return false;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"[Combatant] {characterName} took {amount} damage. Health: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"[Combatant] {characterName} has died.");
        gameObject.SetActive(false);
        if (GameManager.Instance != null && GameManager.Instance.CurrentMode == GameMode.Combat)
        {
            TurnManager.Instance?.RemoveCombatant(this);
        }
    }

    public void SpendAP(int amount)
    {
        currentActionPoints -= amount;
        if (currentActionPoints < 0) currentActionPoints = 0;
        Debug.Log($"[Combatant] {characterName} spent {amount} AP. AP Remaining: {currentActionPoints}");
    }

    public void QueueAction(CombatAction action)
    {
        if (!IsMyTurn)
        {
            Debug.LogWarning($"[Combatant] {characterName} tried to queue action but it's not their turn.");
            return;
        }
        actionQueue.Enqueue(action);
        if (!isPerformingAction)
        {
            ProcessActionQueue();
        }
    }

    private void ProcessActionQueue()
    {
        if (isPerformingAction || actionQueue.Count == 0)
        {
            return;
        }

        isPerformingAction = true;
        CombatAction currentAction = actionQueue.Peek();

        StartCoroutine(ExecuteAction(currentAction));
    }

    private IEnumerator ExecuteAction(CombatAction action)
    {
        if (enableDebugLogging) Debug.Log($"[Combatant] {characterName} starting to execute action from queue.");
        bool actionCompleted = false;

        // Ensure AI can move if the action might involve it (like TryMoveTo's queued action)
        if (aiAgent != null) aiAgent.canMove = true;

        while (!actionCompleted)
        {
            if (!IsMyTurn) // Safety check if turn ended prematurely
            {
                Debug.LogWarning($"[Combatant] {characterName}'s turn ended while action was executing.");
                isPerformingAction = false;
                yield break;
            }
            actionCompleted = action.Invoke();
            if (!actionCompleted)
            {
                yield return null;
            }
        }

        if (aiAgent != null) aiAgent.canMove = false; // Default to not moving after an action

        actionQueue.Dequeue();
        isPerformingAction = false;
        if (enableDebugLogging) Debug.Log($"[Combatant] {characterName} finished executing action. Actions in queue: {actionQueue.Count}");


        if (actionQueue.Count > 0 && currentActionPoints > 0 && IsMyTurn)
        {
            ProcessActionQueue();
        }
        else if (IsMyTurn) // No more actions or no AP
        {
            if (enableDebugLogging) Debug.Log($"[Combatant] {characterName} has no more actions or AP. IsPlayer: {IsPlayerControlled}");
            if (!IsPlayerControlled) // AI automatically ends turn if out of actions/AP
            {
                EndTurn();
            }
            // Player waits for explicit "End Turn" button press
        }
    }
}
