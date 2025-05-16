using UnityEngine;
using Pathfinding;
using System.Collections;
using System.Collections.Generic; // For List

[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(IAstarAI))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Character))]
public class EnemyAIController : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private AIState currentState = AIState.Exploring_Patrolling;

    [Header("Pathfinding")]
    private IAstarAI aiAgent;
    private Seeker seeker;
    private Character combatant;

    [Header("Exploration Patrol")]
    public float patrolRadius = 10f;
    public float patrolWaitMin = 2f, patrolWaitMax = 5f;
    private Vector3 patrolOrigin;
    private Coroutine explorationPatrolCoroutine;

    [Header("Exploration Detection & Pursuit")]
    public float viewRadius = 8f;
    [Range(0, 360)] public float viewAngle = 90f;
    public LayerMask obstacleMask;
    public Vector3 localForward = Vector3.up;
    public float pursuitMemoryExploration = 3f;
    private Transform playerTransform;
    private float timeSinceLastSeenPlayerExploration = 0f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false; // Renamed from debugLogs for consistency
    public bool drawGizmos = true;

    void Awake()
    {
        aiAgent = GetComponent<IAstarAI>();
        seeker = GetComponent<Seeker>();
        combatant = GetComponent<Character>();

        if (aiAgent == null || seeker == null || combatant == null)
        {
            Debug.LogError($"[EnemyAIController] Missing Seeker, IAstarAI, or Combatant on {name}. Disabling.", this);
            enabled = false;
        }
    }

    void Start()
    {
        patrolOrigin = transform.position;
        combatant.IsPlayerControlled = false;

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerTransform = playerGO.transform;
        }

        aiAgent.canSearch = true;
        aiAgent.canMove = true;

        if (GameManager.Instance != null && GameManager.CurrentMode == GameMode.Combat)
        {
            combatant.OnCombatStart();
        }
        else
        {
            ChangeExplorationState(AIState.Exploring_Patrolling);
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) playerTransform = playerGO.transform;
            else return;
        }

        if (GameManager.CurrentMode == GameMode.Exploration)
        {
            HandleExplorationBehavior();
        }
        else
        {
            if (combatant.IsMyTurn && enableDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[EnemyAIController] {name} in Combat Mode. IsMyTurn: {combatant.IsMyTurn}. AP: {combatant.CurrentActionPoints}");
            }
        }
    }

    void HandleExplorationBehavior()
    {
        if (!aiAgent.canMove) aiAgent.canMove = true;
        if (aiAgent.isStopped) aiAgent.isStopped = false;

        bool playerDetected = DetectPlayerExploration();

        if (playerDetected)
        {
            if (currentState != AIState.Exploring_Pursuing)
            {
                ChangeExplorationState(AIState.Exploring_Pursuing);
            }
            // Attempt to initiate combat if close enough after detecting
            if (Vector3.Distance(transform.position, playerTransform.position) < viewRadius * 0.5f) // Example engagement distance
            {
                Character playerCombatant = playerTransform.GetComponent<Character>();
                if (playerCombatant != null)
                {
                    GameManager.Instance.RequestCombatStart(combatant, playerCombatant);
                    // RequestCombatStart will change the game mode, which should then
                    // cause this enemy's Combatant.OnCombatStart() to be called,
                    // and then its turn will eventually come via TurnManager.
                }
            }
        }
        else if (!playerDetected && currentState == AIState.Exploring_Pursuing)
        {
            timeSinceLastSeenPlayerExploration += Time.deltaTime;
            if (timeSinceLastSeenPlayerExploration > pursuitMemoryExploration)
            {
                ChangeExplorationState(AIState.Exploring_Patrolling);
            }
        }

        switch (currentState)
        {
            case AIState.Exploring_Patrolling:
                if (!aiAgent.pathPending && (aiAgent.reachedDestination || !aiAgent.hasPath) && explorationPatrolCoroutine == null)
                {
                    explorationPatrolCoroutine = StartCoroutine(ExplorationPatrolRoutine());
                }
                break;
            case AIState.Exploring_Pursuing:
                if (playerTransform != null)
                {
                    aiAgent.destination = playerTransform.position;
                }
                break;
        }
    }

    public void ReturnToExplorationBehavior()
    {
        ChangeExplorationState(AIState.Exploring_Patrolling);
    }

    IEnumerator ExplorationPatrolRoutine()
    {
        if (enableDebugLogs) Debug.Log($"[EnemyAIController] {name} starting EXPLORATION patrol routine.");
        float waitTime = Random.Range(patrolWaitMin, patrolWaitMax);
        yield return new WaitForSeconds(waitTime);

        Vector3 randomPoint = GetRandomExplorationPatrolPoint();
        aiAgent.destination = randomPoint;
        if (aiAgent.canSearch) aiAgent.SearchPath();
        if (enableDebugLogs) Debug.Log($"[EnemyAIController] {name} EXPLORATION patrolling to {randomPoint}");

        explorationPatrolCoroutine = null;
    }

    Vector3 GetRandomExplorationPatrolPoint()
    {
        for (int i = 0; i < 15; i++)
        {
            Vector2 d = Random.insideUnitCircle * patrolRadius;
            Vector3 p = patrolOrigin + new Vector3(d.x, d.y, 0);
            p.z = transform.position.z;
            NNConstraint constraint = NNConstraint.Default;
            constraint.walkable = true;
            GraphNode node = AstarPath.active.GetNearest(p, constraint).node;
            if (node != null && node.Walkable) return (Vector3)node.position;
        }
        if (enableDebugLogs) Debug.LogWarning($"[EnemyAIController] {name} could not find walkable exploration patrol point. Returning current pos.");
        return transform.position;
    }

    bool DetectPlayerExploration()
    {
        if (playerTransform == null) return false;
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist > viewRadius) return false;

        Vector3 worldForward = transform.TransformDirection(localForward.normalized);
        Vector3 toPlayer = (playerTransform.position - transform.position).normalized;
        if (Vector3.Angle(worldForward, toPlayer) > viewAngle * 0.5f) return false;

        if (!Physics2D.Linecast(transform.position, playerTransform.position, obstacleMask))
        {
            if (enableDebugLogs) Debug.Log($"[EnemyAIController] {name} detected player in exploration mode!");
            timeSinceLastSeenPlayerExploration = 0f;
            return true;
        }
        return false;
    }

    public void PlanCombatTurn()
    {
        if (enableDebugLogs) Debug.Log($"[EnemyAIController] {name} (Combatant: {combatant.characterName}) planning combat turn. AP: {combatant.CurrentActionPoints}");
        if (playerTransform == null)
        {
            combatant.EndTurn();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        Character playerCombatant = playerTransform.GetComponent<Character>();

        if (playerCombatant == null || playerCombatant.CurrentHealth <= 0)
        {
            if (enableDebugLogs) Debug.Log($"[EnemyAIController] {name}: Player target is null or dead. Ending turn.");
            combatant.EndTurn();
            return;
        }

        // 1. Try to Attack if in range and enough AP
        if (distanceToPlayer <= combatant.attackRange && combatant.CurrentActionPoints >= combatant.baseAttackAPCost)
        {
            if (enableDebugLogs) Debug.Log($"[EnemyAIController] {name} attempting to attack player.");
            combatant.TryAttack(playerCombatant);
            // TryAttack queues the action. Combatant processes its queue.
            // If AI has more AP and wants to do more, it needs to wait for this action to complete
            // or queue more actions. For simplicity, one main "intent" (attack or move) per PlanCombatTurn call.
            // The Combatant's action queue will call EndTurn if it's empty and it's AI.
            return;
        }

        // 2. Try to Move into Attack Range if not already there and enough AP for a move
        // A more complex AI would calculate if it has AP for BOTH move and attack.
        int apForShortMove = combatant.apCostPerPathNode * 2; // Example: cost for moving a couple of nodes
        if (distanceToPlayer > combatant.attackRange && combatant.CurrentActionPoints >= apForShortMove)
        {
            if (enableDebugLogs) Debug.Log($"[EnemyAIController] {name} player out of range, attempting to move closer.");
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            Vector3 targetMovePos = playerTransform.position - directionToPlayer * (combatant.attackRange * 0.8f);

            combatant.RequestMoveTo(targetMovePos);
            // Similar to attack, this queues the move. Combatant handles execution.
            return;
        }

        // 3. If no action was queued (e.g., not enough AP, or already in range but couldn't attack), end turn.
        // The Combatant's ProcessActionQueue will call EndTurn if it's an AI and the queue is empty.
        if (combatant.ActionQueueCount == 0) // Accessing the new public property
        {
            if (enableDebugLogs) Debug.Log($"[EnemyAIController] {name} has no suitable actions queued or AP. Ending turn.");
            combatant.EndTurn();
        }
    }

    private void ChangeExplorationState(AIState newState)
    {
        // Prevent re-entering the same state if patrol coroutine is what's "ending"
        if (currentState == newState && newState == AIState.Exploring_Patrolling && explorationPatrolCoroutine != null) return;
        // Allow re-entering pursuing if player is re-detected
        if (currentState == newState && newState == AIState.Exploring_Pursuing) { /* allow re-triggering pursuit logic */ }
        else if (currentState == newState) return;


        if (enableDebugLogs) Debug.Log($"[EnemyAIController] {name} changing EXPLORATION state from {currentState} to {newState}");

        if (explorationPatrolCoroutine != null && (newState == AIState.Exploring_Pursuing))
        {
            StopCoroutine(explorationPatrolCoroutine);
            explorationPatrolCoroutine = null;
        }
        currentState = newState;

        if (newState == AIState.Exploring_Patrolling && explorationPatrolCoroutine == null)
        {
            explorationPatrolCoroutine = StartCoroutine(ExplorationPatrolRoutine());
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.green;
        if (Application.isPlaying) Gizmos.DrawWireSphere(patrolOrigin, patrolRadius);
        else Gizmos.DrawWireSphere(transform.position, patrolRadius); // Show around current pos if not playing

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 worldForward = Application.isPlaying ? transform.TransformDirection(localForward.normalized) : transform.up;
        if (Application.isPlaying && localForward == Vector3.zero) worldForward = transform.up;

        Vector3 viewAngleA = Quaternion.AngleAxis(-viewAngle / 2, transform.forward) * worldForward;
        Vector3 viewAngleB = Quaternion.AngleAxis(viewAngle / 2, transform.forward) * worldForward;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);

        if (aiAgent != null && aiAgent.hasPath && GameManager.CurrentMode == GameMode.Exploration) // Only show exploration path
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, aiAgent.destination);
        }
    }
}