using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum GameMode
{
    Exploration,
    Combat
}

/// <summary>
/// Manages the overall game mode (Exploration or Combat) and handles transitions.
/// Also responsible for initiating combat.
/// </summary>
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
    [SerializeField] private GameMode currentMode = GameMode.Exploration;
    public GameMode CurrentMode => currentMode;

    public delegate void GameModeChangedHandler(GameMode newMode);
    public event GameModeChangedHandler OnGameModeChanged;

    // List of all potential combatants in the scene
    // Consider making this dynamic if enemies spawn/despawn frequently during exploration.
    private List<Character> allCombatantsInScene = new List<Character>();


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] Duplicate instance found, destroying self.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Consider if this manager needs to persist across scenes

        Debug.Log("[GameManager] Initialized. Current Mode: Exploration");
        dungeonMasterBook.gameObject.SetActive(false);
        gameplayRoot.SetActive(false);
        SceneTransition.SetActive(false);
    }

    void Start()
    {
        Init = true;
        dungeonMasterBook.OpenBook();

        if (!IsPaused)
            RefreshCombatantList();
    }

    public void RefreshCombatantList()
    {
        allCombatantsInScene = FindObjectsOfType<Character>().ToList();
        Debug.Log($"[GameManager] Refreshed combatant list. Found: {allCombatantsInScene.Count} combatants.");
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
    public void StartCombat(List<Character> participants)
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
            UIManager.Instance.ShowTurnPhase("Combat mode");
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
    public void EndCombat()
    {
        if (currentMode == GameMode.Exploration)
        {
            Debug.LogWarning("[GameManager] Attempted to end combat while already in exploration mode.");
            return;
        }

        Debug.Log("[GameManager] Ending Combat.");
        ChangeMode(GameMode.Exploration);

        // Notify all combatants that combat has ended
        foreach (Character combatant in allCombatantsInScene)
        {
            if (combatant != null) combatant.OnCombatEnd();
        }

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.EndCombat();
        }
    }

    private void ChangeMode(GameMode newMode)
    {
        if (currentMode == newMode) return;

        currentMode = newMode;
        Debug.Log($"[GameManager] Game mode changed to: {currentMode}");
        OnGameModeChanged?.Invoke(currentMode);
    }

    // Example trigger: Call this from player attack script or enemy detection script
    public void RequestCombatStart(Character initiator, Character target)
    {
        if (currentMode == GameMode.Combat) return;

        Debug.Log($"[GameManager] Combat requested by {initiator.gameObject.name} against {target.gameObject.name}");

        // For now, let's assume combat involves the initiator, the target,
        // and any other nearby enemies or allies. This logic can be expanded.
        List<Character> participants = new List<Character>();
        if (initiator != null) participants.Add(initiator);
        if (target != null && !participants.Contains(target)) participants.Add(target);

        // Simple: Add all other combatants for now. 
        // TODO: Implement more sophisticated logic to determine actual participants (e.g., based on factions, aggro radius)
        foreach (var combatant in allCombatantsInScene)
        {
            if (combatant != null && !participants.Contains(combatant))
            {
                // Example: Add all other enemies if player initiates, or all allies if enemy initiates
                // For now, just add everyone to test the system
                participants.Add(combatant);
            }
        }
        participants = participants.Distinct().ToList(); // Ensure no duplicates

        StartCombat(participants);
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
