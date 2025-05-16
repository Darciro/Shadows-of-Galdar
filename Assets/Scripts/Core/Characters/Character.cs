using UnityEngine;
using Pathfinding; // For IAstarAI and Seeker
using System.Collections.Generic;
using System.Collections; // For IEnumerator

public class Character : CharacterStats
{
    [Header("Character Status")]
    public string characterName = "Character";

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
    private EnemyAIController enemyAIController;
    public delegate bool CombatAction();
    private Queue<CombatAction> actionQueue = new Queue<CombatAction>();
    private bool isPerformingAction = false;
    private bool waitingForPathForAPCost = false;

    public int ActionQueueCount => actionQueue.Count;

    protected override void Awake()
    {
        base.Awake();
        aiAgent = GetComponent<IAstarAI>();
        seeker = GetComponent<Seeker>();
        enemyAIController = GetComponent<EnemyAIController>();

        if (aiAgent == null) Debug.LogError($"[Character] {gameObject.name} is missing an IAstarAI component!", this);
        if (seeker == null) Debug.LogError($"[Character] {gameObject.name} is missing a Seeker component!", this);

        // CurrentHealth = MaxHealth;
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
        Debug.Log($"[Character] {characterName} ({gameObject.name}) entering combat mode.");
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
        if (!gameObject.activeInHierarchy)
        {
            Debug.Log($"[Character] {characterName} is inactive; skipping OnCombatEnd.");
            return;
        }

        Debug.Log($"[Character] {characterName} ({gameObject.name}) exiting combat mode.");
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
        Debug.Log($"[Character] {characterName}'s turn (Instance ID: {this.GetInstanceID()}). AP: {MaxActionPoints}");
        IsMyTurn = true;
        CurrentActionPoints = MaxActionPoints;
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
            Debug.Log($"[Character] {characterName} (Player) turn started. Waiting for input.");
        }
    }

    public void EndTurn()
    {
        if (!IsMyTurn) return;
        Debug.Log($"[Character] {characterName} ending turn. AP Left: {CurrentActionPoints}");
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
            Debug.LogError($"[Character] {characterName} path error to {targetPosition}: {p.errorLog}");
            return;
        }

        currentPathForAP = p; // Store the path
        int apCost = (p.path.Count > 0) ? (p.path.Count - 1) * apCostPerPathNode : 0; // Cost based on number of nodes/segments
        if (apCost == 0 && Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            // If path is 0 nodes but target isn't current pos, maybe 1 AP for very short move
            apCost = apCostPerPathNode;
        }


        if (CurrentActionPoints >= apCost)
        {
            Debug.Log($"[Character] {characterName} moving to {targetPosition}. Path Nodes: {p.path.Count}, Cost: {apCost} AP. Remaining AP: {CurrentActionPoints - apCost}");

            QueueAction(() =>
            {
                // This action executes the move using the pre-calculated path
                if (aiAgent.isStopped || !aiAgent.hasPath) // If not already moving or path invalidated
                {
                    aiAgent.isStopped = false;
                    aiAgent.canMove = true;
                    aiAgent.SetPath(currentPathForAP); // Use the path we got for AP calculation
                    Debug.Log($"[Character ActionQueue - Move] {characterName} starting A* movement. HasPath: {aiAgent.hasPath}");
                }

                if (aiAgent.reachedDestination || !aiAgent.hasPath)
                {
                    Debug.Log($"[Character ActionQueue - Move] {characterName} finished move action. Reached: {aiAgent.reachedDestination}, HasPath: {aiAgent.hasPath}");
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
            Debug.LogWarning($"[Character] {characterName} cannot move to {targetPosition}. Not enough AP. Need: {apCost}, Have: {CurrentActionPoints}");
            currentPathForAP = null;
        }
    }


    public bool TryAttack(Character target)
    {
        if (!IsMyTurn || isPerformingAction || target == null) return false;

        if (CurrentActionPoints >= baseAttackAPCost)
        {
            if (Vector3.Distance(transform.position, target.transform.position) <= attackRange)
            {
                QueueAction(() =>
                {
                    Debug.Log($"[Character ActionQueue - Attack] {characterName} attacking {target.characterName} for {attackDamage} damage. Cost: {baseAttackAPCost} AP.");
                    UIManager.Instance.ShowDamagePopup(target.transform.position, attackDamage);
                    target.TakeDamage(attackDamage);
                    return true; // Action complete
                });
                SpendAP(baseAttackAPCost);
                return true;
            }
            else
            {
                Debug.LogWarning($"[Character] {characterName} cannot attack {target.characterName}. Target out of range.");
                return false;
            }
        }
        Debug.LogWarning($"[Character] {characterName} cannot attack. Not enough AP. Need: {baseAttackAPCost}, Have: {CurrentActionPoints}");
        return false;
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        Debug.Log($"[Character] {characterName} took {amount} damage. Health: {CurrentHealth}/{MaxHealth}");
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"[Character] {characterName} has died.");
        gameObject.SetActive(false);
        if (GameManager.Instance != null && GameManager.CurrentMode == GameMode.Combat)
        {
            TurnManager.Instance?.RemoveCombatant(this);
        }
    }

    public void SpendAP(int amount)
    {
        CurrentActionPoints -= amount;
        if (CurrentActionPoints < 0) CurrentActionPoints = 0;
        Debug.Log($"[Character] {characterName} spent {amount} AP. AP Remaining: {CurrentActionPoints}");
    }

    public void QueueAction(CombatAction action)
    {
        if (!IsMyTurn)
        {
            Debug.LogWarning($"[Character] {characterName} tried to queue action but it's not their turn.");
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
        if (enableDebugLogging)
            Debug.Log($"[Character] {characterName} starting to execute action from queue.");

        // Safety: if someone cleared the queue before we got here, bail out immediately
        if (actionQueue.Count == 0)
        {
            Debug.LogWarning($"[Character] {characterName}: actionQueue empty on ExecuteAction start; aborting.");
            isPerformingAction = false;
            yield break;
        }

        bool actionCompleted = false;

        // Ensure AI can move if this action needs movement
        if (aiAgent != null)
            aiAgent.canMove = true;

        // Run the action until it reports completion
        while (!actionCompleted)
        {
            // If the turn was ended prematurely, stop everything
            if (!IsMyTurn)
            {
                Debug.LogWarning($"[Character] {characterName}'s turn ended mid-action.");
                isPerformingAction = false;
                yield break;
            }

            actionCompleted = action.Invoke();
            if (!actionCompleted)
                yield return null;
        }

        // After the action finishes, lock movement again
        if (aiAgent != null)
            aiAgent.canMove = false;

        // Now remove this action from the queue, but only if it's still there
        if (actionQueue.Count > 0)
        {
            actionQueue.Dequeue();
        }
        else
        {
            Debug.LogWarning($"[Character] {characterName}: Tried to Dequeue but actionQueue was already empty.");
        }

        isPerformingAction = false;

        if (enableDebugLogging)
            Debug.Log($"[Character] {characterName} finished action. Remaining in queue: {actionQueue.Count}");

        // If there are more actions and we still have AP, keep processing
        if (actionQueue.Count > 0 && CurrentActionPoints > 0 && IsMyTurn)
        {
            ProcessActionQueue();
        }
        else if (IsMyTurn)
        {
            // If AI and no more actions/AP, automatically end turn
            if (!IsPlayerControlled)
                EndTurn();
            // If player, wait for explicit end-turn input
        }
    }

}
