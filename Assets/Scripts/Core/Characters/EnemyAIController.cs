using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Character), typeof(IAstarAI), typeof(Seeker))]
public class EnemyAIController : MonoBehaviour
{
    [Header("Patrol & Chase Settings")]
    public AIState State = AIState.Patrolling;
    public float patrolRadius = 2.5f;
    public float patrolInterval = 3f;
    public float chaseRadius = 5f;

    [Header("Debugging")]
    [SerializeField] private bool enableDebugLogging = false;

    private IAstarAI ai;
    private Seeker seeker;
    private Character enemyCharacter;
    private Transform playerTransform;
    private float patrolTimer;

    void Awake()
    {
        ai = GetComponent<IAstarAI>();
        seeker = GetComponent<Seeker>();
        enemyCharacter = GetComponent<Character>();
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Initially halt the AI until paths are assigned or combat starts
        ai.isStopped = true;
        ai.canMove = false;
        ai.canSearch = false;
    }

    void Update()
    {
        // Exploration mode: patrol or detect player
        if (GameManager.CurrentMode == GameMode.Exploration)
        {
            switch (State)
            {
                case AIState.Patrolling:
                    DoPatrol();
                    break;
                case AIState.Searching:
                    DoChase();
                    break;
            }
            EvaluateState();
        }
    }

    private void DoPatrol()
    {
        patrolTimer += Time.deltaTime;
        if (patrolTimer < patrolInterval)
            return;

        patrolTimer = 0f;
        Vector2 randomPoint = (Vector2)transform.position + Random.insideUnitCircle * patrolRadius;
        seeker.StartPath(transform.position, randomPoint, OnPathComplete);
    }

    private void DoChase()
    {
        if (playerTransform == null)
            return;

        // Upon seeing the player, initiate combat
        if (enableDebugLogging) Debug.Log($"[EnemyAIController] {name} spotted player; requesting combat.");
        UIManager.Instance.AddLog($"[EnemyAIController] {name} spotted player; requesting combat.");

        Character playerCombatant = playerTransform.GetComponent<Character>();
        if (playerCombatant != null)
        {
            GameManager.Instance.RequestCombatStart(enemyCharacter, playerCombatant);
        }
    }

    private void OnPathComplete(Path path)
    {
        if (path.error)
        {
            Debug.LogWarning($"[EnemyAIController] Path error: {path.errorLog}");
            return;
        }

        ai.canSearch = true;
        ai.canMove = true;
        ai.isStopped = false;
        ai.SetPath(path);
    }

    private void EvaluateState()
    {
        if (playerTransform == null)
        {
            State = AIState.Patrolling;
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        State = (dist <= chaseRadius) ? AIState.Searching : AIState.Patrolling;
    }

    /// <summary>
    /// Called at the start of this enemy's combat turn.
    /// Queues either an attack or a movement action (and ends turn if no valid actions).
    /// </summary>
    public void PlanCombatTurn()
    {
        if (enableDebugLogging)
            Debug.Log($"[EnemyAIController] {name} planning combat turn. AP: {enemyCharacter.CurrentActionPoints}");

        // Ensure we have a player target
        if (playerTransform == null)
        {
            enemyCharacter.EndTurn();
            return;
        }

        Character playerCombatant = playerTransform.GetComponent<Character>();
        if (playerCombatant == null || playerCombatant.CurrentHealth <= 0)
        {
            enemyCharacter.EndTurn();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // 1) If in attack range, queue attack and done
        if (distanceToPlayer <= enemyCharacter.attackRange)
        {
            if (enableDebugLogging)
                Debug.Log($"[EnemyAIController] {name} attacking player.");

            enemyCharacter.HandleAttack(playerCombatant);
            return;
        }

        // 2) Otherwise, plan a movement action
        int apPerNode = enemyCharacter.apCostPerPathNode;
        int availableAP = enemyCharacter.CurrentActionPoints;

        // Move target just outside attack range
        Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 moveTarget = (Vector2)playerTransform.position - dir * (enemyCharacter.attackRange * 0.8f);

        seeker.StartPath(transform.position, moveTarget, (Path p) =>
        {
            if (p.error)
            {
                Debug.LogWarning($"[EnemyAIController] Combat path error: {p.errorLog}");
                enemyCharacter.EndTurn();
                return;
            }

            int segments = Mathf.Max(1, p.vectorPath.Count - 1);
            int cost = segments * apPerNode;

            if (cost <= availableAP)
            {
                if (enableDebugLogging)
                    Debug.Log($"[EnemyAIController] Queuing move ({segments} nodes, {cost} AP)");

                enemyCharacter.QueueMoveAction(p, cost);
            }
            else
            {
                if (enableDebugLogging)
                    Debug.Log($"[EnemyAIController] Not enough AP ({availableAP}) for move ({cost}). Ending turn.");

                enemyCharacter.EndTurn();
            }
        });

        // Do NOT end turn immediately - wait for the path callback to queue or end
    }
}
