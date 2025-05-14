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

    // Initiative = Dexterity + 1d10
    public int Initiative { get; private set; }

    protected virtual void Awake()
    {
        MaxHealth = Constitution * 5;
        CurrentHealth = MaxHealth;
        Hunger = MaxHunger;
        Thirst = MaxThirst;
    }

    /* public void SetCharacterStats()
    {
        MaxHealth = Constitution * 5;
        CurrentHealth = MaxHealth;
    } */

    /* public void CalculateStatsFromAttributes(CharacterAttributes attributes)
    {
        MaxHealth = attributes.Constitution * 5;
        CurrentHealth = MaxHealth;

        MaxActionPoints = 3 + (attributes.Dexterity / 10);
        CurrentActionPoints = MaxActionPoints;

        // Initiative is calculated later at the start of combat
        Initiative = 0;
    } */

    public void RollInitiative(int dexterity)
    {
        Initiative = dexterity + Random.Range(1, 11); // 1 to 10 inclusive
    }

    public void RestoreAP()
    {
        CurrentActionPoints = MaxActionPoints;
    }
}