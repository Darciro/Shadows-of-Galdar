using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool Init { get; private set; } = false;
    public static bool IsPaused { get; private set; } = false;

    [Header("Managers")]
    [SerializeField] private DungeonMasterBook dungeonMasterBook;
    [SerializeField] private GameObject gameplayRoot;
    public GameObject SceneTransition;

    [Header("Current Mode")]
    [SerializeField] private GameMode currentMode;
    public static GameMode CurrentMode { get; private set; } = GameMode.Exploration;

    [Header("Debug")]
    public bool ShadowDebugger = false;

    public delegate void GameModeChangedHandler(GameMode newMode);
    public event GameModeChangedHandler OnGameModeChanged;

    private List<Character> allCombatantsInScene = new List<Character>();


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        dungeonMasterBook.gameObject.SetActive(false);
        gameplayRoot.SetActive(false);
        SceneTransition.SetActive(false);
    }

    void Start()
    {
        Init = true;
        if (ShadowDebugger)
            gameplayRoot.SetActive(true);
        else
            dungeonMasterBook.OpenBook();
    }

    /// <summary>
    /// Populate allCombatantsInScene with only those Characters whose renderers
    /// are inside the main camera’s view frustum.
    /// </summary>
    public void RefreshCombatantList()
    {
        allCombatantsInScene.Clear();

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[GameManager] No main camera found – cannot refresh visible combatants.");
            return;
        }

        // build the 6 planes of the camera frustum
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);

        // find every Character in the scene
        foreach (Character c in FindObjectsOfType<Character>())
        {
            if (c == null) continue;

            // assume each Character has at least one Renderer in its children
            Renderer rend = c.GetComponentInChildren<Renderer>();
            if (rend != null && GeometryUtility.TestPlanesAABB(frustumPlanes, rend.bounds))
            {
                allCombatantsInScene.Add(c);
            }
        }

        Debug.Log($"[GameManager] Refreshed combatant list. Visible: {allCombatantsInScene.Count} combatants.");
    }

    /// <summary>
    /// Called when someone (player or enemy) actually wants to fight.
    /// We rebuild the list to only “in‐view” combatants, then pick
    /// initiator, target + everyone else in view.
    /// </summary>
    public void RequestCombatStart(Character initiator, Character target)
    {
        if (currentMode == GameMode.Combat) return;

        Debug.Log($"[GameManager] Combat requested by {initiator.name} against {target.name}");

        // only build the list of combatants now, and only those on‐screen
        RefreshCombatantList();

        // build our participants list
        List<Character> participants = new List<Character>();
        if (initiator != null) participants.Add(initiator);
        if (target != null && !participants.Contains(target))
            participants.Add(target);

        // add any other visible combatants
        foreach (var c in allCombatantsInScene)
            if (c != null && !participants.Contains(c))
                participants.Add(c);

        // ensure no duplicates and kick off combat
        OnStartCombat(participants.Distinct().ToList());
    }

    public List<Character> GetAllCombatants()
    {
        // Ensure list is up-to-date, removing any destroyed combatants
        allCombatantsInScene.RemoveAll(item => item == null);
        return new List<Character>(allCombatantsInScene); // Return a copy
    }

    /// <summary>
    /// Initiates combat with a specific group of participants.
    /// </summary>
    /// <param name="participants">The combatants involved in this specific combat encounter.</param>
    public void OnStartCombat(List<Character> participants)
    {
        if (currentMode == GameMode.Combat)
        {
            Debug.LogWarning("[GameManager] Attempted to start combat while already in combat mode.");
            return;
        }

        if (participants == null || !participants.Any())
        {
            Debug.LogWarning("[GameManager] Attempted to start combat with no participants.");
            return;
        }

        // Ensure all participants are valid
        participants.RemoveAll(item => item == null);
        if (!participants.Any())
        {
            Debug.LogWarning("[GameManager] Attempted to start combat but all provided participants were null.");
            return;
        }

        Debug.Log($"[GameManager] Starting Combat with {participants.Count} participants.");
        ChangeMode(GameMode.Combat);

        // Notify all combatants that combat has started
        foreach (Character combatant in allCombatantsInScene) // Notify all, not just participants
        {
            if (combatant != null) combatant.OnCombatStart();
        }

        // Initialize and start the TurnManager
        if (TurnManager.Instance != null)
        {
            // UIManager.Instance.ShowTurnPhase("Combat mode");
            TurnManager.Instance.StartCombat(participants);
        }
        else
        {
            Debug.LogError("[GameManager] TurnManager instance not found! Combat cannot begin.");
            ChangeMode(GameMode.Exploration); // Revert if TurnManager is missing
        }
    }

    /// <summary>
    /// Ends the current combat and switches back to Exploration mode.
    /// </summary>
    public void OnEndCombat()
    {
        if (currentMode == GameMode.Exploration)
        {
            Debug.LogWarning("[GameManager] Attempted to end combat while already in exploration mode.");
            return;
        }

        // Notify all combatants that combat has ended
        foreach (Character combatant in allCombatantsInScene)
        {
            if (combatant != null) combatant.OnCombatEnd();
        }

        Debug.Log("[GameManager] Ending Combat.");
        ChangeMode(GameMode.Exploration);
    }

    public void ChangeMode(GameMode newMode)
    {
        if (currentMode == newMode) return;

        currentMode = newMode;
        if (currentMode == GameMode.Combat)
        {
            UIManager.Instance.ShowTurnPhase("Combat mode");
        }
        else
        {
            UIManager.Instance.ShowTurnPhase("Exploration mode");
        }

        Debug.Log($"[GameManager] Game mode changed to: {currentMode}");
        OnGameModeChanged?.Invoke(currentMode);
    }

    public void PauseGame()
    {
        gameplayRoot.SetActive(false);
        Time.timeScale = 0f;             // stop all time‐based updates
        AudioListener.pause = true;      // pause all audio
        IsPaused = true;
    }

    public void ResumeGame()
    {
        gameplayRoot.SetActive(true);
        Time.timeScale = 1f;             // restore normal time
        AudioListener.pause = false;     // resume audio
        IsPaused = false;
    }
}
