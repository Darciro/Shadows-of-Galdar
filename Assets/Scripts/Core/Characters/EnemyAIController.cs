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
            Debug.Log($"[EnemyAIController] {name} planning turn. AP available: {enemyCharacter.CurrentActionPoints}");

        // If no player or player already dead, end turn
        if (playerTransform == null || playerTransform.GetComponent<Character>() == null || playerTransform.GetComponent<Character>().CurrentHealth <= 0)
        {
            enemyCharacter.EndTurn();
            return;
        }
        Character playerCombatant = playerTransform.GetComponent<Character>();

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= enemyCharacter.attackRange)
        {
            // Player is within attack range – perform attack
            if (enableDebugLogging)
                Debug.Log($"[EnemyAIController] {name} is in range and will attack the player.");
            enemyCharacter.HandleAttack(playerCombatant);
            return;
        }

        // Player is out of range – move closer
        int apPerNode = enemyCharacter.apCostPerPathNode;
        int availableAP = enemyCharacter.CurrentActionPoints;
        // Target a point just within attack range of the player
        Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 moveTarget = (Vector2)playerTransform.position - dir * (enemyCharacter.attackRange * 0.8f);

        // Find path toward the player (within attack range distance)
        seeker.StartPath(transform.position, moveTarget, (Path p) =>
        {
            if (p.error)
            {
                Debug.LogWarning($"[EnemyAIController] Path error: {p.errorLog}");
                enemyCharacter.EndTurn();
                return;
            }
            int segments = Mathf.Max(1, p.vectorPath.Count - 1);
            int moveCost = segments * apPerNode;
            if (moveCost <= availableAP)
            {
                // Can reach the intended point with available AP
                if (enableDebugLogging)
                    Debug.Log($"[EnemyAIController] {name} moving toward player (cost {moveCost} AP).");
                enemyCharacter.QueueMoveAction(p, moveCost);
                // After moving, attempt an attack if enough AP remains
                if (enemyCharacter.CurrentActionPoints >= enemyCharacter.baseAttackAPCost)
                {
                    if (enableDebugLogging)
                        Debug.Log($"[EnemyAIController] {name} will attack after moving (has {enemyCharacter.CurrentActionPoints} AP left).");
                    enemyCharacter.QueueAction(() =>
                    {
                        // Attack action
                        UIManager.Instance.ShowDamagePopup(playerTransform.position, enemyCharacter.attackDamage);
                        playerCombatant.TakeDamage(enemyCharacter.attackDamage);
                        return true;
                    });
                    enemyCharacter.SpendAP(enemyCharacter.baseAttackAPCost);
                }
            }
            else
            {
                // Cannot reach player this turn – move as far as possible
                if (enableDebugLogging)
                    Debug.Log($"[EnemyAIController] {name} cannot reach player this turn (need {moveCost} AP, have {availableAP}). Moving partially.");
                if (availableAP <= 0)
                {
                    enemyCharacter.EndTurn();
                    return;
                }
                int maxIndex = Mathf.Min(p.vectorPath.Count - 1, availableAP);
                Vector3 reachablePos = p.vectorPath[maxIndex];
                seeker.StartPath(transform.position, reachablePos, (Path partialPath) =>
                {
                    if (partialPath.error)
                    {
                        Debug.LogWarning($"[EnemyAIController] Partial path error: {partialPath.errorLog}");
                        enemyCharacter.EndTurn();
                        return;
                    }
                    int partSegments = Mathf.Max(1, partialPath.vectorPath.Count - 1);
                    int partCost = partSegments * apPerNode;
                    if (partCost > availableAP) partCost = availableAP;
                    if (enableDebugLogging)
                        Debug.Log($"[EnemyAIController] {name} moves partially toward player (cost {partCost} AP).");
                    enemyCharacter.QueueMoveAction(partialPath, partCost);
                    // No attack this turn (not in range yet or no AP left).
                });
            }
        });
        // Note: EndTurn will be called by Character after actions complete.
    }
}
