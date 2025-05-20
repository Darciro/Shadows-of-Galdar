using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(IAstarAI))]
[RequireComponent(typeof(Seeker))]
public class Character : CharacterStats
{
    [Header("Core Stats")]
    public float viewRadius = 2;
    public float attackRange = 0.25f;
    public int attackDamage = 10;
    public bool IsMyTurn = false;
    public bool IsPlayerControlled { get; set; } = false;

    public delegate bool CombatAction();
    private Queue<CombatAction> actionQueue = new Queue<CombatAction>();
    public int ActionQueueCount => actionQueue.Count;
    private bool isPerformingAction = false;
    private EnemyAIController enemyAIController;
    private Path currentPathForAP; // Store path to calculate AP cost
    public int apCostPerPathNode = 1;
    private bool waitingForPathForAPCost = false;
    private LineRenderer pathLineRenderer;

    protected Animator animator;
    protected Rigidbody rb;
    private IAstarAI ai;
    private Seeker seeker;

    protected override void Awake()
    {
        base.Awake();
        ai = GetComponent<IAstarAI>();
        seeker = GetComponent<Seeker>();
        pathLineRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemyAIController = GetComponent<EnemyAIController>();

        ai.canSearch = true;
        ai.canMove = true;
    }

    void LateUpdate()
    {
        ai.MovementUpdate(Time.deltaTime, out Vector3 nextPos, out Quaternion nextRot);
        ai.FinalizeMovement(nextPos, nextRot);
    }

    public void OnCombatStart()
    {
        Debug.Log($"[Character] {gameObject.name} entering combat mode. Initiative roll {Initiative}");
        RollInitiative();

        actionQueue.Clear();
        isPerformingAction = false;
        if (ai != null)
        {
            ai.isStopped = true;
            ai.canMove = false;
        }
    }

    public void StartTurn()
    {
        Debug.Log($"[Character] {gameObject.name}'s turn (Instance ID: {this.GetInstanceID()}). AP: {MaxActionPoints}");
        IsMyTurn = true;
        CurrentActionPoints = MaxActionPoints;
        actionQueue.Clear();
        isPerformingAction = false;
        ai.isStopped = true;
        ai.canMove = false;

        if (!IsPlayerControlled && enemyAIController != null)
        {
            enemyAIController.PlanCombatTurn();
        }
        else if (IsPlayerControlled)
        {
            // Player's turn: UI should enable, input script listens for commands
            Debug.Log($"[Character] {gameObject.name} (Player) turn started. Waiting for input.");
        }
    }

    public void OnCombatEnd()
    {
        Debug.Log($"[Character] {gameObject.name} is exiting combat mode.");
        IsMyTurn = false;
        actionQueue.Clear();
        isPerformingAction = false;
        if (ai != null)
        {
            ai.isStopped = false;
            ai.canMove = true;
            ai.SetPath(null);
        }
    }

    public void EndTurn()
    {
        if (!IsMyTurn) return;
        Debug.Log($"[Character] {gameObject.name} ending turn. AP Left: {CurrentActionPoints}");
        IsMyTurn = false;
        ai.isStopped = true;
        ai.canMove = false;
        ai.SetPath(null);
        TurnManager.Instance?.EndCurrentTurn(); // Null check for safety
    }

    public void QueueAction(CombatAction action)
    {
        if (!IsMyTurn)
        {
            Debug.LogWarning($"[Character] {gameObject.name} tried to queue action but it's not their turn.");
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
        if (actionQueue.Count == 0)
        {
            Debug.LogWarning($"[Character] {gameObject.name}: actionQueue empty on ExecuteAction start; aborting.");
            isPerformingAction = false;
            yield break;
        }

        bool actionCompleted = false;
        ai.canMove = true;

        while (!actionCompleted)
        {
            // If the turn was ended prematurely, stop everything
            if (!IsMyTurn)
            {
                Debug.LogWarning($"[Character] {gameObject.name}'s turn ended mid-action.");
                isPerformingAction = false;
                yield break;
            }

            actionCompleted = action.Invoke();
            if (!actionCompleted)
                yield return null;
        }

        ai.canMove = false;
        if (actionQueue.Count > 0)
            actionQueue.Dequeue();

        isPerformingAction = false;

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

    /// <summary>
    /// Trigger attack animation.
    /// </summary>
    public virtual bool HandleAttack(Character target)
    {
        // animator.SetTrigger("Attack");
        // if (CurrentActionPoints >= baseAttackAPCost) {
        if (Vector3.Distance(transform.position, target.transform.position) <= attackRange)
        {
            QueueAction(() =>
            {
                Debug.Log($"[Character ActionQueue - Attack] {gameObject.name} attacking {target.name} for {attackDamage} damage. Cost: baseAttackAPCost AP.");
                UIManager.Instance.ShowDamagePopup(target.transform.position, attackDamage);
                target.TakeDamage(attackDamage);
                return true; // Action complete
            });
            // SpendAP(baseAttackAPCost);
            return true;
        }
        else
        {
            Debug.LogWarning($"[Character] {gameObject.name} cannot attack {target.name}. Target out of range.");
            return false;
        }
        // }
    }

    /// <summary>
    /// Called via AnimationEvent to apply damage.
    /// </summary>
    public void DealDamage(Character target)
    {
        if (target == null || target.IsDead) return;
        target.TakeDamage(attackDamage);
    }

    /// <summary>
    /// Play hit reaction & check for death.
    /// </summary>
    public override void TakeDamage(int amount)
    {
        // First apply the base damage logic
        base.TakeDamage(amount);

        // Then play hit animation, and die if health dropped to zero
        animator.SetTrigger("Hit");
        if (IsDead)
            Die();
    }

    public void SpendAP(int amount)
    {
        CurrentActionPoints -= amount;
        if (CurrentActionPoints < 0) CurrentActionPoints = 0;
        Debug.Log($"[Character] {name} spent {amount} AP. AP Remaining: {CurrentActionPoints}");
    }

    private void OnPathComplete(Path path)
    {
        if (path.error) return;

        // Reactivate AI for exploration
        ai.isStopped = false;
        ai.canSearch = true;
        ai.canMove = true;
        ai.SetPath(path);
    }

    public void HandleMovement(Vector3 targetPosition)
    {
        if (GameManager.CurrentMode == GameMode.Exploration)
        {
            seeker.StartPath(transform.position, targetPosition, OnPathComplete);
        }
        else if (GameManager.CurrentMode == GameMode.Combat && IsMyTurn)
        {
            waitingForPathForAPCost = true;
            seeker.StartPath(transform.position, targetPosition, (Path p) =>
            {
                OnPathReceivedForAPCost(p, targetPosition);
            });
        }
    }

    /// <summary>
    /// Queues a movement action using the given precomputed A* path and spends the specified AP.
    /// </summary>
    /// <param name="path">The A* Path returned by Seeker.StartPath()</param>
    /// <param name="apCost">The action point cost of this movement</param>
    public void QueueMoveAction(Path path, int apCost)
    {
        // Deduct AP immediately
        SpendAP(apCost);

        // Enqueue the actual move action
        QueueAction(() =>
        {
            // First time through: kick off the movement
            if (ai.isStopped || !ai.hasPath)
            {
                ai.isStopped = false;
                ai.canMove = true;
                ai.SetPath(path);
                Debug.Log($"[Character ActionQueue - Move] {gameObject.name} beginning move. AP spent: {apCost}");
            }

            // Check for arrival
            if (ai.reachedDestination || !ai.hasPath)
            {
                ai.isStopped = true;
                ai.canMove = false;
                Debug.Log($"[Character ActionQueue - Move] {gameObject.name} reached destination.");
                return true;    // signal that this action is complete
            }

            return false;       // still moving
        });
    }

    void DrawPath(List<Vector3> waypoints)
    {
        Vector3[] positions = waypoints.ToArray();
        positions[0] = transform.position + Vector3.down * 0.25f;
        pathLineRenderer.positionCount = positions.Length;
        pathLineRenderer.SetPositions(positions);
    }

    void OnPathReceivedForAPCost(Path p, Vector3 targetPosition)
    {
        // DrawPath(p.vectorPath);
        // ShowMoveMarker(p.vectorPath[p.vectorPath.Count - 1]);

        waitingForPathForAPCost = false;
        if (!IsMyTurn) return; // Turn might have ended while waiting for path

        if (p.error)
        {
            Debug.LogError($"[Character] {gameObject.name} path error to {targetPosition}: {p.errorLog}");
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
            Debug.Log($"[Character] {gameObject.name} moving to {targetPosition}. Path Nodes: {p.path.Count}, Cost: {apCost} AP. Remaining AP: {CurrentActionPoints - apCost}");

            QueueAction(() =>
            {
                // This action executes the move using the pre-calculated path
                if (ai.isStopped || !ai.hasPath) // If not already moving or path invalidated
                {
                    ai.isStopped = false;
                    ai.canMove = true;
                    ai.SetPath(currentPathForAP); // Use the path we got for AP calculation
                    Debug.Log($"[Character ActionQueue - Move] {gameObject.name} starting A* movement. HasPath: {ai.hasPath}");
                }

                if (ai.reachedDestination || !ai.hasPath)
                {
                    Debug.Log($"[Character ActionQueue - Move] {gameObject.name} finished move action. Reached: {ai.reachedDestination}, HasPath: {ai.hasPath}");
                    ai.isStopped = true;
                    ai.canMove = false;
                    currentPathForAP = null; // Clear stored path
                    return true; // Action complete
                }
                return false; // Action ongoing
            });

            SpendAP(apCost);
        }
        else
        {
            Debug.LogWarning($"[Character] {gameObject.name} cannot move to {targetPosition}. Not enough AP. Need: {apCost}, Have: {CurrentActionPoints}");
            currentPathForAP = null;
        }
    }

    /// <summary>
    /// Common death logic: animation, disable collider & scripts, destroy.
    /// </summary>
    protected virtual void Die()
    {
        animator.SetTrigger("Die");
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        enabled = false;
        Destroy(gameObject, 2f);
        TurnManager.Instance.RemoveCombatant(this);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
