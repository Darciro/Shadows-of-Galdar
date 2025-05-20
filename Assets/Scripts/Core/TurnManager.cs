using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    private List<Character> combatants = new List<Character>();
    private int currentCombatantIndex = -1;
    public Character CurrentCombatant => (currentCombatantIndex >= 0 && currentCombatantIndex < combatants.Count && combatants[currentCombatantIndex] != null) ? combatants[currentCombatantIndex] : null;

    public delegate void TurnChangedHandler(Character newCurrentCombatant);
    public event TurnChangedHandler OnTurnChanged;

    private bool isCombatActive = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        // Nothing to do if combat isn’t running or there is no current turn
        if (!isCombatActive || CurrentCombatant == null)
            return;

        if (CurrentCombatant.CurrentActionPoints <= 0)
        {
            Debug.Log($"[TurnManager] {CurrentCombatant.name} has no AP—ending turn.");
            EndCurrentTurn();
            return;
        }

        // 2) Player pressed Space to end their turn early
        if (CurrentCombatant.IsPlayerControlled && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"[TurnManager] Player pressed Space—ending {CurrentCombatant.name}’s turn.");
            EndCurrentTurn();
            return;
        }
    }

    public void StartCombat(List<Character> initialParticipants)
    {
        if (isCombatActive)
        {
            Debug.LogWarning("[TurnManager] StartCombat called while combat is already active.");
            return;
        }

        Debug.Log("[TurnManager] Starting new combat sequence.");
        combatants = initialParticipants
            .Where(c => c != null && c.gameObject.activeInHierarchy && c.CurrentHealth > 0)
            .OrderByDescending(c => c.Initiative)
            .ToList();

        foreach (var combatant in combatants)
        {
            UIManager.Instance.AddCharToTurnOrder(combatant.name);
        }

        if (!combatants.Any())
        {
            Debug.LogError("[TurnManager] No valid (active and alive) combatants to start combat with!");
            GameManager.Instance.OnEndCombat();
            return;
        }

        isCombatActive = true;
        currentCombatantIndex = -1;
        combatants[0].IsMyTurn = true;
        NextTurn();
    }

    public void NextTurn()
    {
        if (!isCombatActive) return;

        if (CurrentCombatant != null)
        {
            CurrentCombatant.IsMyTurn = false;
        }

        if (CheckCombatEndCondition())
        {
            return;
        }

        currentCombatantIndex++;
        if (currentCombatantIndex >= combatants.Count)
        {
            currentCombatantIndex = 0;
            Debug.Log("[TurnManager] New round started.");
        }

        // Skip dead or inactive combatants
        while (combatants[currentCombatantIndex] == null || !combatants[currentCombatantIndex].gameObject.activeInHierarchy || combatants[currentCombatantIndex].CurrentHealth <= 0)
        {
            Debug.Log($"[TurnManager] Skipping dead/inactive combatant: {combatants[currentCombatantIndex]?.name ?? "NULL_OR_DESTROYED"}. Removing from turn order.");
            combatants.RemoveAt(currentCombatantIndex);

            if (!combatants.Any()) // If list becomes empty
            {
                if (!CheckCombatEndCondition()) // Re-check, should trigger end.
                {
                    Debug.LogWarning("[TurnManager] All combatants removed or dead, but combat end not triggered. Forcing end.");
                    GameManager.Instance.OnEndCombat();
                }
                return;
            }
            // Adjust index: if we removed an element, the next element is now at currentCombatantIndex.
            // So, if currentCombatantIndex was valid before removal, it might be out of bounds or pointing to the wrong next element.
            // Easiest is to reset to 0 if it goes out of bounds, or just let the loop condition handle it.
            if (currentCombatantIndex >= combatants.Count)
            {
                currentCombatantIndex = 0; // Loop back if we removed the last elements
            }
            // No need to increment index here as RemoveAt shifts elements. The loop continues.
            if (!combatants.Any()) break; // Break if list became empty after removal
        }

        if (!combatants.Any()) // All combatants might have been skipped
        {
            if (!CheckCombatEndCondition())
            {
                Debug.LogWarning("[TurnManager] No valid combatants left to take a turn. Forcing end.");
                GameManager.Instance.OnEndCombat();
            }
            return;
        }

        // Now currentCombatantIndex should point to a valid, active, alive combatant
        combatants[currentCombatantIndex].StartTurn();
        OnTurnChanged?.Invoke(CurrentCombatant);
        Debug.Log($"[TurnManager] Next turn: {CurrentCombatant.name} (Index: {currentCombatantIndex})");
    }

    public void EndCurrentTurn()
    {
        if (!isCombatActive || CurrentCombatant == null) return;
        Debug.Log($"[TurnManager] {CurrentCombatant.name} officially ended their turn via EndCurrentTurn().");
        NextTurn();
    }

    public void RemoveCombatant(Character combatantToRemove)
    {
        if (!isCombatActive || combatantToRemove == null) return;

        bool wasCurrentTurn = (CurrentCombatant == combatantToRemove);
        int removedIndex = combatants.IndexOf(combatantToRemove);

        if (combatants.Remove(combatantToRemove))
        {
            Debug.Log($"[TurnManager] Character {combatantToRemove.name} removed from turn order (e.g. died).");
            if (wasCurrentTurn)
            {
                currentCombatantIndex--;
                NextTurn();
            }
            else if (removedIndex != -1 && removedIndex < currentCombatantIndex)
            {
                currentCombatantIndex--;
            }
            CheckCombatEndCondition();
        }
    }

    private bool CheckCombatEndCondition()
    {
        // Ensure we only work with active, non-null combatants for faction check
        var aliveCombatants = combatants.Where(c => c != null && c.gameObject.activeInHierarchy && c.CurrentHealth > 0).ToList();

        if (!aliveCombatants.Any())
        {
            Debug.Log("[TurnManager] Combat ended: No alive combatants left.");
            if (isCombatActive) GameManager.Instance.OnEndCombat(); // Only call if combat was active
            return true;
        }

        bool playerTeamAlive = aliveCombatants.Any(c => c.IsPlayerControlled);
        bool enemyTeamAlive = aliveCombatants.Any(c => !c.IsPlayerControlled);

        if (!playerTeamAlive)
        {
            Debug.Log("[TurnManager] Combat ended: Player team defeated.");
            if (isCombatActive) GameManager.Instance.OnEndCombat();
            return true;
        }
        if (!enemyTeamAlive)
        {
            Debug.Log("[TurnManager] Combat ended: Enemy team defeated.");
            if (isCombatActive) GameManager.Instance.OnEndCombat();
            return true;
        }
        return false;
    }
}
