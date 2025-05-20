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

    void Start()
    {
        // Hide tooltip initially
        apCostTooltip.gameObject.SetActive(false);
    }

    void Update()
    {
        UIManager.Instance.UpdatePlayerVitals();
        // Always check for clicks first
        HandleClickInput();
        HandleKeyInput();

        // Then update preview only on player's combat turn
        if (GameManager.CurrentMode == GameMode.Combat && playerCharacter.IsMyTurn)
            HandleAPPreview();
        else if (isPreviewing)
            CancelPreview();
    }

    private void HandleClickInput()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        Vector3 world3D = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 world2D = new Vector2(world3D.x, world3D.y);

        // 1) OverlapPoint to detect any collider at the mouse
        Collider2D hitCollider = Physics2D.OverlapPoint(world2D);
        if (hitCollider != null)
        {
            var other = hitCollider.GetComponent<Character>();
            if (other != null && !other.IsPlayerControlled)
            {
                // Clicked on enemy
                if (GameManager.CurrentMode == GameMode.Exploration)
                {
                    if (enableDebugLogging) Debug.Log($"[PlayerController] RequestCombatStart vs {other.name}");
                    GameManager.Instance.RequestCombatStart(playerCharacter, other);
                }
                else if (GameManager.CurrentMode == GameMode.Combat && playerCharacter.IsMyTurn)
                {
                    if (enableDebugLogging) Debug.Log($"[PlayerController] HandleAttack on {other.name}");
                    playerCharacter.HandleAttack(other);
                }

                CancelPreview();
                return;
            }
        }

        // 2) Otherwise, click on empty space = movement
        if (GameManager.CurrentMode == GameMode.Exploration)
        {
            if (enableDebugLogging) Debug.Log($"[PlayerController] Exploration move to {world2D}");
            playerCharacter.HandleMovement(world3D);
        }
        else if (GameManager.CurrentMode == GameMode.Combat && playerCharacter.IsMyTurn)
        {
            // Use previewPath to queue a combat move, avoiding canceled-by-script errors
            if (previewPath != null)
            {
                int segments = Mathf.Max(1, previewPath.vectorPath.Count - 1);
                int cost = segments * apCostPerPathNode;

                if (cost <= playerCharacter.CurrentActionPoints)
                {
                    if (enableDebugLogging) Debug.Log($"[PlayerController] QueueMoveAction to {previewTarget}");
                    playerCharacter.QueueMoveAction(previewPath, cost);
                }
                else if (enableDebugLogging)
                {
                    Debug.Log($"[PlayerController] Not enough AP ({playerCharacter.CurrentActionPoints}) for cost {cost}");
                }
            }
            // Clear preview state regardless
            CancelPreview();
        }
    }

    private void HandleKeyInput()
    {
        // Only skip turn when it's truly the player's turn
        if (!playerCharacter.IsMyTurn)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            // GameManager.Instance.OnGameModeChanged += GameManager.Instance.ChangeMode;
            Debug.Log("[CharacterPathfindingController] Player pressed Space to end turn.");
            playerCharacter.EndTurn();
            TurnManager.Instance.NextTurn();
        }
    }

    private void HandleAPPreview()
    {
        Vector3 mouseScreen = Input.mousePosition;
        Vector3 world3D = cam.ScreenToWorldPoint(mouseScreen);
        Vector2 world2D = new Vector2(world3D.x, world3D.y);

        // Position tooltip near cursor
        apCostTooltip.rectTransform.position = mouseScreen + new Vector3(12f, -12f, 0f);

        // Throttle path requests
        if (Time.time - lastPreviewTime < previewInterval)
            return;
        lastPreviewTime = Time.time;

        // Request preview path
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
