using UnityEngine;
using Pathfinding;
using System.Collections.Generic;

[RequireComponent(typeof(Character))]
public class PlayerController : MonoBehaviour
{
    private Character playerCharacter;
    private Camera cam;

    [Header("Movement")]
    [Tooltip("AP cost per node/segment in the A* path.")]
    public int apCostPerPathNode = 1;
    public float moveSpeedInCombat = 3f;
    private LineRenderer pathLineRenderer;

    [Header("Debugging")]
    [SerializeField] private bool enableDebugLogging = false;

    void Awake()
    {
        playerCharacter = GetComponent<Character>();
        pathLineRenderer = GetComponent<LineRenderer>();
        cam = Camera.main;
    }

    void Start()
    {
        if (playerCharacter != null)
            playerCharacter.IsPlayerControlled = true;

        UIManager.Instance.UpdatePlayerVitals();
    }

    void Update()
    {
        UIManager.Instance.UpdatePlayerVitals();
        HandleMovementInput();
    }

    private void HandleMovementInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

            if (hit.collider != null)
            {
                Character enemyCombatant = hit.collider.GetComponent<Character>();
                if (enemyCombatant != null && !enemyCombatant.IsPlayerControlled)
                {
                    if (GameManager.CurrentMode == GameMode.Exploration)
                    {
                        if (enableDebugLogging) Debug.Log($"[PlayerController] Clicked on enemy {enemyCombatant.name} in exploration. Requesting combat.");
                        UIManager.Instance.AddLog($"[PlayerController] Clicked on enemy {enemyCombatant.name} in exploration. Requesting combat.");
                        GameManager.Instance.RequestCombatStart(playerCharacter, enemyCombatant);
                    }
                    else if (GameManager.CurrentMode == GameMode.Combat)
                    {
                        if (enableDebugLogging) Debug.Log($"[PlayerController] Clicked on enemy {enemyCombatant.name} in combat. Starting attack");
                        playerCharacter.HandleAttack(enemyCombatant);
                    }

                    return;
                }
            }
            else
            {
                if (enableDebugLogging) Debug.Log($"[PlayerController] Player is just moving around.");
                UIManager.Instance.AddLog($"[PlayerController] Player is just moving around.");
                playerCharacter.HandleMovement(mouseWorldPos);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameManager.Instance.OnGameModeChanged += GameManager.Instance.ChangeMode;
            Debug.Log("[CharacterPathfindingController] Player pressed Space to end turn.");
            playerCharacter.EndTurn();
            TurnManager.Instance.NextTurn();
        }
    }
}
