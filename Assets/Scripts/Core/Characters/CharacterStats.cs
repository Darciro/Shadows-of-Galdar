using UnityEngine;

[System.Serializable]
public class CharacterStats : MonoBehaviour
{
    [Header("Attributes")]
    public int Strength = 10;
    public int Dexterity = 10;
    public int Constitution = 10;
    public int Intelligence = 10;
    public int Perception = 10;
    public int Charisma = 10;

    [Header("Vitals")]
    // HP = Constitution * 5
    public int MaxHealth;
    public int CurrentHealth;

    // AP = 3 + Dexterity / 10
    public int MaxActionPoints = 5;
    public int CurrentActionPoints = 5;

    // 0 = starving, 100 = full
    public int MaxHunger = 100;
    public int Hunger = 100;

    // 0 = dehydrated, 100 = quenched
    public int MaxThirst = 100;
    public int Thirst = 100;

    public bool IsDead { get; private set; } = false;

    // Initiative = Dexterity + 1d10
    public int Initiative { get; private set; }

    [Header("Progression")]
    public int Experience = 0;
    public int Level = 1;

    protected virtual void Awake()
    {
        MaxHealth = Constitution * 5;
        CurrentHealth = MaxHealth;
        CurrentActionPoints = MaxActionPoints;
        Hunger = MaxHunger;
        Thirst = MaxThirst;
    }

    public virtual void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        if (CurrentHealth < 0) CurrentHealth = 0;

        Debug.Log($"[CharacterStats] {name} took {amount} damage (HP: {CurrentHealth}/{MaxHealth}).");

        // Only mark dead when health is zero (and only once)
        if (CurrentHealth <= 0 && !IsDead)
        {
            Debug.Log($"[CharacterStats] ... And, is dead!");
            IsDead = true;
        }
    }

    public void RollInitiative()
    {
        Initiative = Dexterity + Random.Range(1, 11); // 1 to 10 inclusive
    }

    public void RestoreAP()
    {
        CurrentActionPoints = MaxActionPoints;
    }
}