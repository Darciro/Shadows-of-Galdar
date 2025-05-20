using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(IAstarAI))]
[RequireComponent(typeof(Seeker))]
public class Character : CharacterStats
{
    [Header("Core Stats")]
    public float viewRadius = 2f;
    public float attackRange = 0.25f;
    public int attackDamage = 10;
    public bool IsMyTurn = false;
    public bool IsPlayerControlled { get; set; } = false;

    [Header("Combat Settings")]
    [SerializeField] private float moveSpeedInCombat = 3f;
    [SerializeField] public int baseAttackAPCost = 2;

    public delegate bool CombatAction();
    private Queue<CombatAction> actionQueue = new Queue<CombatAction>();
    public int ActionQueueCount => actionQueue.Count;
    private bool isPerformingAction = false;

    private EnemyAIController enemyAIController;
    private Path currentPathForAP;
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
        // Update AI movement each frame
        ai.MovementUpdate(Time.deltaTime, out Vector3 nextPos, out Quaternion nextRot);
        ai.FinalizeMovement(nextPos, nextRot);
    }

    public void OnCombatStart()
    {
        // Roll for initiative at the start of combat
        RollInitiative();
        Debug.Log($"[Character] {name} entering combat mode. Initiative roll {Initiative}");
        // Clear any leftover actions
        actionQueue.Clear();
        isPerformingAction = false;
        // Stop movement until turn begins
        if (ai != null)
        {
            ai.isStopped = true;
            ai.canMove = false;
        }
    }

    public void StartTurn()
    {
        Debug.Log($"[Character] {name}'s turn begins. Restoring AP to {MaxActionPoints}.");
        IsMyTurn = true;
        CurrentActionPoints = MaxActionPoints;
        actionQueue.Clear();
        isPerformingAction = false;
        // Halt movement at turn start – will resume when actions are issued
        ai.isStopped = true;
        ai.canMove = false;
        // Optionally, adjust movement speed for combat if using AIPath
        if (ai is Pathfinding.AIPath aiPath)
        {
            aiPath.maxSpeed = moveSpeedInCombat;
        }

        if (!IsPlayerControlled && enemyAIController != null)
        {
            // Enemy AI: plan out its actions for this turn
            enemyAIController.PlanCombatTurn();
        }
        else if (IsPlayerControlled)
        {
            // Player: await input
            Debug.Log($"[Character] {name} (Player) turn started. Awaiting player actions...");
        }
    }

    public void OnCombatEnd()
    {
        Debug.Log($"[Character] {name} exiting combat mode.");
        IsMyTurn = false;
        actionQueue.Clear();
        isPerformingAction = false;
        // Resume normal movement for AI agents
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
        Debug.Log($"[Character] {name} ending turn with {CurrentActionPoints} AP left.");
        IsMyTurn = false;
        ai.isStopped = true;
        ai.canMove = false;
        ai.SetPath(null);
        TurnManager.Instance?.EndCurrentTurn();
    }

    public void QueueAction(CombatAction action)
    {
        if (!IsMyTurn)
        {
            Debug.LogWarning($"[Character] {name} tried to queue an action outside of their turn!");
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
        if (isPerformingAction || actionQueue.Count == 0) return;
        isPerformingAction = true;
        CombatAction currentAction = actionQueue.Peek();
        StartCoroutine(ExecuteAction(currentAction));
    }

    private IEnumerator ExecuteAction(CombatAction action)
    {
        if (actionQueue.Count == 0)
        {
            isPerformingAction = false;
            yield break;
        }
        bool actionCompleted = false;
        // Allow AI movement during action execution
        ai.canMove = true;
        while (!actionCompleted)
        {
            // If turn somehow ended mid-action, abort
            if (!IsMyTurn)
            {
                Debug.LogWarning($"[Character] {name}'s turn ended before action completed.");
                isPerformingAction = false;
                yield break;
            }
            actionCompleted = action.Invoke();
            if (!actionCompleted)
                yield return null; // wait a frame and continue action (e.g., still moving)
        }
        // Action finished:
        ai.canMove = false;
        if (actionQueue.Count > 0)
        {
            actionQueue.Dequeue();
        }
        isPerformingAction = false;
        // Continue next action if available and AP remains
        if (actionQueue.Count > 0 && CurrentActionPoints > 0 && IsMyTurn)
        {
            ProcessActionQueue();
        }
        else if (IsMyTurn)
        {
            // No more queued actions, or no AP left
            if (!IsPlayerControlled)
            {
                // AI auto-ends turn when done
                EndTurn();
            }
            // Players can choose to end turn manually (or will auto-end if AP=0 via TurnManager)
        }
    }

    public virtual bool HandleAttack(Character target)
    {
        // Attempt to attack the target if in range and enough AP
        if (Vector3.Distance(transform.position, target.transform.position) <= attackRange)
        {
            if (CurrentActionPoints < baseAttackAPCost)
            {
                Debug.LogWarning($"[Character] {name} cannot attack {target.name} – not enough AP (need {baseAttackAPCost}).");
                return false;
            }
            // Queue the attack animation/action
            QueueAction(() =>
            {
                Debug.Log($"[Character ActionQueue] {name} attacks {target.name} for {attackDamage} damage (cost {baseAttackAPCost} AP).");
                UIManager.Instance.ShowDamagePopup(target.transform.position, attackDamage);
                target.TakeDamage(attackDamage);
                return true; // attack completes in one tick
            });
            SpendAP(baseAttackAPCost);
            return true;
        }
        else
        {
            Debug.LogWarning($"[Character] {name} cannot attack {target.name} – target out of range.");
            return false;
        }
    }

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount); // deduct health
        animator.SetTrigger("Hit");
        if (IsDead)
        {
            Die();
        }
    }

    public void SpendAP(int amount)
    {
        CurrentActionPoints -= amount;
        if (CurrentActionPoints < 0) CurrentActionPoints = 0;
        Debug.Log($"[Character] {name} spent {amount} AP (remaining AP: {CurrentActionPoints}).");
    }

    protected virtual void Die()
    {
        // Called when health drops to 0 or below
        Debug.Log($"[Character] {name} has died.");
        // **NEW:** Award XP to player if this is an enemy
        if (!IsPlayerControlled)
        {
            Character playerChar = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Character>();
            if (playerChar != null)
            {
                playerChar.Experience += 10;
                UIManager.Instance.AddLog($"Player gained 10 XP.");
            }
        }
        // Play death animation and disable character
        animator.SetTrigger("Die");
        if (TryGetComponent<Collider>(out var col))
        {
            col.enabled = false;
        }
        enabled = false;
        // Destroy the game object after a delay to allow animation to play
        Destroy(gameObject, 2f);
        // Remove this character from turn order
        TurnManager.Instance.RemoveCombatant(this);
    }

    // Movement methods:

    public void HandleMovement(Vector3 targetPosition)
    {
        if (GameManager.CurrentMode == GameMode.Exploration)
        {
            // Simple movement in exploration (continuous)
            seeker.StartPath(transform.position, targetPosition, OnPathComplete);
        }
        else if (GameManager.CurrentMode == GameMode.Combat && IsMyTurn)
        {
            // Begin pathfinding for combat movement (AP cost to be evaluated)
            waitingForPathForAPCost = true;
            seeker.StartPath(transform.position, targetPosition, (Path p) =>
            {
                OnPathReceivedForAPCost(p, targetPosition);
            });
        }
    }

    private void OnPathComplete(Path path)
    {
        if (path.error) return;
        // For exploration mode: directly set the AI path
        ai.isStopped = false;
        ai.canSearch = true;
        ai.canMove = true;
        ai.SetPath(path);
    }

    public void QueueMoveAction(Path path, int apCost)
    {
        // Queue a movement along the given path, deducting the specified AP cost
        SpendAP(apCost);
        QueueAction(() =>
        {
            // On first invocation, start moving along the path
            if (ai.isStopped || !ai.hasPath)
            {
                ai.isStopped = false;
                ai.canMove = true;
                ai.SetPath(path);
                Debug.Log($"[Character ActionQueue] {name} begins moving (cost {apCost} AP).");
            }
            // Each frame, check if destination reached
            if (ai.reachedDestination || !ai.hasPath)
            {
                ai.isStopped = true;
                ai.canMove = false;
                Debug.Log($"[Character ActionQueue] {name} finished moving.");
                return true; // movement action complete
            }
            return false; // still en route
        });
    }

    private void OnPathReceivedForAPCost(Path p, Vector3 targetPosition)
    {
        waitingForPathForAPCost = false;
        if (!IsMyTurn) return; // ignore if turn ended while path was calculating
        if (p.error)
        {
            Debug.LogError($"[Character] Path error when moving to {targetPosition}: {p.errorLog}");
            return;
        }
        currentPathForAP = p;
        int fullCost = (p.path.Count - 1) * apCostPerPathNode;
        if (fullCost == 0 && Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            fullCost = apCostPerPathNode;
        }
        if (CurrentActionPoints >= fullCost)
        {
            Debug.Log($"[Character] {name} will move to target (cost {fullCost} AP).");
            // Queue full move
            QueueAction(() =>
            {
                if (ai.isStopped || !ai.hasPath)
                {
                    ai.isStopped = false;
                    ai.canMove = true;
                    ai.SetPath(currentPathForAP);
                }
                if (ai.reachedDestination || !ai.hasPath)
                {
                    Debug.Log($"[Character] {name} reached movement destination.");
                    return true;
                }
                return false;
            });
            SpendAP(fullCost);
        }
        else
        {
            Debug.LogWarning($"[Character] {name} cannot move to target – not enough AP (need {fullCost}, have {CurrentActionPoints}).");
            // No partial move handled here; this is handled in PlayerController logic.
            currentPathForAP = null;
        }
    }

    // ... (OnDrawGizmosSelected for visualizing ranges, unchanged) ...
}
