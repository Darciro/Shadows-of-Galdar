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
public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    [Header("Current Mode")]
    [SerializeField] private GameMode currentMode = GameMode.Exploration;
    public GameMode CurrentMode => currentMode;

    public delegate void GameModeChangedHandler(GameMode newMode);
    public event GameModeChangedHandler OnGameModeChanged;

    // List of all potential combatants in the scene
    // Consider making this dynamic if enemies spawn/despawn frequently during exploration.
    private List<Combatant> allCombatantsInScene = new List<Combatant>();


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameModeManager] Duplicate instance found, destroying self.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // Consider if this manager needs to persist across scenes

        Debug.Log("[GameModeManager] Initialized. Current Mode: Exploration");
    }

    void Start()
    {
        // Initially find all combatants. This might need to be updated if characters spawn later.
        RefreshCombatantList();
    }

    public void RefreshCombatantList()
    {
        allCombatantsInScene = FindObjectsOfType<Combatant>().ToList();
        Debug.Log($"[GameModeManager] Refreshed combatant list. Found: {allCombatantsInScene.Count} combatants.");
    }

    public List<Combatant> GetAllCombatants()
    {
        // Ensure list is up-to-date, removing any destroyed combatants
        allCombatantsInScene.RemoveAll(item => item == null);
        return new List<Combatant>(allCombatantsInScene); // Return a copy
    }


    /// <summary>
    /// Initiates combat with a specific group of participants.
    /// </summary>
    /// <param name="participants">The combatants involved in this specific combat encounter.</param>
    public void StartCombat(List<Combatant> participants)
    {
        if (currentMode == GameMode.Combat)
        {
            Debug.LogWarning("[GameModeManager] Attempted to start combat while already in combat mode.");
            return;
        }

        if (participants == null || !participants.Any())
        {
            Debug.LogWarning("[GameModeManager] Attempted to start combat with no participants.");
            return;
        }

        // Ensure all participants are valid
        participants.RemoveAll(item => item == null);
        if (!participants.Any())
        {
            Debug.LogWarning("[GameModeManager] Attempted to start combat but all provided participants were null.");
            return;
        }

        Debug.Log($"[GameModeManager] Starting Combat with {participants.Count} participants.");
        ChangeMode(GameMode.Combat);

        // Notify all combatants that combat has started
        foreach (Combatant combatant in allCombatantsInScene) // Notify all, not just participants
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
            Debug.LogError("[GameModeManager] TurnManager instance not found! Combat cannot begin.");
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
            Debug.LogWarning("[GameModeManager] Attempted to end combat while already in exploration mode.");
            return;
        }

        Debug.Log("[GameModeManager] Ending Combat.");
        ChangeMode(GameMode.Exploration);

        // Notify all combatants that combat has ended
        foreach (Combatant combatant in allCombatantsInScene)
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
        Debug.Log($"[GameModeManager] Game mode changed to: {currentMode}");
        OnGameModeChanged?.Invoke(currentMode);
    }

    // Example trigger: Call this from player attack script or enemy detection script
    public void RequestCombatStart(Combatant initiator, Combatant target)
    {
        if (currentMode == GameMode.Combat) return;

        Debug.Log($"[GameModeManager] Combat requested by {initiator.gameObject.name} against {target.gameObject.name}");

        // For now, let's assume combat involves the initiator, the target,
        // and any other nearby enemies or allies. This logic can be expanded.
        List<Combatant> participants = new List<Combatant>();
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
}
