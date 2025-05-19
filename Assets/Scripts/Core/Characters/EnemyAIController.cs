using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Character))]
[RequireComponent(typeof(IAstarAI))]
[RequireComponent(typeof(Seeker))]
public class EnemyAIController : MonoBehaviour
{
    [Header("Patrol Settings")]
    public AIState State = AIState.Patrolling;
    public float patrolRadius = 2.5f;
    public float patrolInterval = 3f;

    [Header("Chase & Attack")]
    public float chaseRadius = 2;

    private IAstarAI ai;
    private Seeker seeker;
    private Character enemyCharacter;
    private Transform playerTransform;
    private float patrolTimer;

    [Header("Debugging")]
    [SerializeField] private bool enableDebugLogging = false;

    void Awake()
    {
        ai = GetComponent<IAstarAI>();
        seeker = GetComponent<Seeker>();
        enemyCharacter = GetComponent<Character>();
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        ai.canSearch = false;
    }

    void Update()
    {
        switch (State)
        {
            case AIState.Patrolling: DoPatrol(); break;
            case AIState.Searching: DoChase(); break;
            case AIState.Attacking: DoAttack(); break;
        }
        EvaluateState();
    }

    private void OnPathComplete(Path path)
    {
        if (path.error)
        {
            Debug.LogWarning("Path calculation failed: " + path.errorLog);
            return;
        }
        // Assign the path to the AI movement script
        ai.SetPath(path);
        ai.canSearch = true;
    }

    void LateUpdate()
    {
        ai.MovementUpdate(Time.deltaTime, out Vector3 nextPos, out Quaternion nextRot);
        ai.FinalizeMovement(nextPos, nextRot);
    }

    private void EvaluateState()
    {
        float dist = playerTransform != null
            ? Vector3.Distance(transform.position, playerTransform.position)
            : Mathf.Infinity;

        if (GameManager.CurrentMode != GameMode.Combat)
        {
            /* if (dist <= enemyCharacter.attackRange) State = AIState.Attacking;
            else if (dist <= chaseRadius) State = AIState.Searching;
            else State = AIState.Patrolling; */

            if (dist <= chaseRadius)
            {
                State = AIState.Searching;
            }
            else
            {
                State = AIState.Patrolling;
            }
        }
        else
        {
            if (enableDebugLogging) Debug.Log($"[EnemyAIController] The enemy {this.name} is waiting for it's turn to start");
            // UIManager.Instance.AddLog($"[EnemyAIController] The enemy {this.name} is waiting for it's turn to start");
            State = AIState.Idle;
        }
    }

    private void DoPatrol()
    {
        patrolTimer += Time.deltaTime;
        if (patrolTimer >= patrolInterval)
        {
            Vector3 rnd = transform.position + Random.insideUnitSphere * patrolRadius;
            rnd.y = transform.position.y;
            seeker.StartPath(transform.position, rnd, OnPathComplete);
            ai.canSearch = true;
            patrolTimer = 0f;
        }
    }

    private void DoChase()
    {
        if (playerTransform == null) return;

        if (enableDebugLogging) Debug.Log($"[EnemyAIController] The enemy {this.name} has saw the player and will request combat.");
        UIManager.Instance.AddLog($"[EnemyAIController] The enemy {this.name} has saw the player and will request combat.");

        Character playerCombatant = playerTransform.GetComponent<Character>();
        if (playerCombatant != null)
        {
            GameManager.Instance.RequestCombatStart(enemyCharacter, playerCombatant);
        }

        // PlanCombatTurn();

        // seeker.StartPath(transform.position, playerTransform.position, OnPathComplete);
        // ai.canSearch = true;
    }

    public void PlanCombatTurn()
    {
        if (enableDebugLogging) Debug.Log($"[EnemyAIController] {name} (Combatant: {enemyCharacter.name}) planning combat turn. AP: {enemyCharacter.CurrentActionPoints}");
        if (playerTransform == null)
        {
            enemyCharacter.EndTurn();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        Character playerCombatant = playerTransform.GetComponent<Character>();

        if (playerCombatant == null || playerCombatant.CurrentHealth <= 0)
        {
            if (enableDebugLogging) Debug.Log($"[EnemyAIController] {name}: Player target is null or dead. Ending turn.");
            enemyCharacter.EndTurn();
            return;
        }

        // if (distanceToPlayer <= enemyCharacter.attackRange && enemyCharacter.CurrentActionPoints >= enemyCharacter.baseAttackAPCost)
        if (distanceToPlayer <= enemyCharacter.attackRange)
        {
            if (enableDebugLogging) Debug.Log($"[EnemyAIController] {name} attempting to attack player.");
            enemyCharacter.HandleAttack(playerCombatant);
            return;
        }

        int apForShortMove = 2; // Example: cost for moving a couple of nodes
        if (distanceToPlayer > enemyCharacter.attackRange && enemyCharacter.CurrentActionPoints >= apForShortMove)
        {
            if (enableDebugLogging) Debug.Log($"[EnemyAIController] {name} player out of range, attempting to move closer.");
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            Vector3 targetMovePos = playerTransform.position - directionToPlayer * (enemyCharacter.attackRange * 0.8f);

            enemyCharacter.HandleMovement(targetMovePos);
            // Similar to attack, this queues the move. Combatant handles execution.
            return;
        }

        // 3. If no action was queued (e.g., not enough AP, or already in range but couldn't attack), end turn.
        // The Combatant's ProcessActionQueue will call EndTurn if it's an AI and the queue is empty.
        if (enemyCharacter.ActionQueueCount == 0)
        {
            if (enableDebugLogging) Debug.Log($"[EnemyAIController] {name} has no suitable actions queued or AP. Ending turn.");
            enemyCharacter.EndTurn();
        }
    }

    private void DoAttack()
    {
        ai.canSearch = false;
        transform.rotation = Quaternion.LookRotation((playerTransform.position - transform.position).normalized);
        // Character player = ;
        enemyCharacter.HandleAttack(GameObject.FindGameObjectWithTag("Player").GetComponent<Character>());
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
    }
}
