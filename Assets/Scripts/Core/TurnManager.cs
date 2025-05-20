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
            Debug.LogError("[TurnManager] No valid combatants to start combat with!");
            GameManager.Instance.OnEndCombat();
            return;
        }
        isCombatActive = true;
        currentCombatantIndex = -1;
        // **Removed**: no need to pre-set IsMyTurn here
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
            return; // Combat might end here
        }
        currentCombatantIndex++;
        if (currentCombatantIndex >= combatants.Count)
        {
            currentCombatantIndex = 0;
            Debug.Log("[TurnManager] New round started.");
        }
        // Skip any dead or inactive combatants
        while (combatants.Count > 0 &&
               (combatants[currentCombatantIndex] == null ||
                !combatants[currentCombatantIndex].gameObject.activeInHierarchy ||
                combatants[currentCombatantIndex].CurrentHealth <= 0))
        {
            Debug.Log($"[TurnManager] Skipping dead/inactive combatant: {combatants[currentCombatantIndex]?.name ?? "NULL_OR_DESTROYED"}. Removing from turn order.");
            combatants.RemoveAt(currentCombatantIndex);
            if (!combatants.Any())
            {
                if (!CheckCombatEndCondition())
                {
                    Debug.LogWarning("[TurnManager] All combatants removed or dead, forcing combat end.");
                    GameManager.Instance.OnEndCombat();
                }
                return;
            }
            if (currentCombatantIndex >= combatants.Count)
                currentCombatantIndex = 0;
        }
        if (!combatants.Any())
        {
            // (Additional safety check - if no combatants left, end combat)
            if (!CheckCombatEndCondition())
            {
                Debug.LogWarning("[TurnManager] No valid combatants left, forcing combat end.");
                GameManager.Instance.OnEndCombat();
            }
            return;
        }
        // Start the next combatant's turn
        combatants[currentCombatantIndex].StartTurn();
        OnTurnChanged?.Invoke(CurrentCombatant);
        Debug.Log($"[TurnManager] Next turn: {CurrentCombatant.name} (Index: {currentCombatantIndex})");
    }

    public void EndCurrentTurn()
    {
        if (!isCombatActive || CurrentCombatant == null) return;
        Debug.Log($"[TurnManager] {CurrentCombatant.name} ended their turn.");
        NextTurn();
    }

    public void RemoveCombatant(Character combatantToRemove)
    {
        if (!isCombatActive || combatantToRemove == null) return;
        bool wasCurrentTurn = (CurrentCombatant == combatantToRemove);
        int removedIndex = combatants.IndexOf(combatantToRemove);
        if (combatants.Remove(combatantToRemove))
        {
            Debug.Log($"[TurnManager] Removed {combatantToRemove.name} from turn order.");
            if (wasCurrentTurn)
            {
                currentCombatantIndex--;
                NextTurn(); // Immediately move to next turn if the active combatant died
            }
            else if (removedIndex != -1 && removedIndex < currentCombatantIndex)
            {
                currentCombatantIndex--; // adjust index if a combatant earlier in list was removed
            }
            CheckCombatEndCondition();
        }
    }

    public void EndCombat()
    {
        // **NEW:** Reset combat state after combat ends
        isCombatActive = false;
        combatants.Clear();
        currentCombatantIndex = -1;
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
