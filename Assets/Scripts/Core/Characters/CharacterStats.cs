using UnityEngine;

[System.Serializable]
public class CharacterStats
{
    [Header("Attributes")]
    public int Strength = 10;
    public int Dexterity = 10;
    public int Constitution = 10;
    public int Intelligence = 10;
    public int Perception = 10;
    public int Charisma = 10;

    public int Initiative { get; private set; }

    // HP = Constitution * 5
    public int MaximumHP = 100;
    public int CurrentHP = 100;

    // AP = 3 + Dexterity / 10
    public int MaxAP = 5;
    public int CurrentAP = 5;

    // 0 = starving, 100 = full
    public int MaxHunger = 100;
    public int Hunger = 100;

    // 0 = dehydrated, 100 = quenched
    public int MaxThirst = 100;
    public int Thirst = 100;

    // Initiative = Dexterity + 1d10

    public CharacterStats()
    {
        MaximumHP = 100;
        CurrentHP = 100;
        MaxAP = 2;
        CurrentAP = 0;

        Hunger = MaxHunger;
        Thirst = MaxThirst;
    }

    /* public void SetCharacterStats()
    {
        MaximumHP = Constitution * 5;
        CurrentHP = MaximumHP;
    } */

    /* public void CalculateStatsFromAttributes(CharacterAttributes attributes)
    {
        MaximumHP = attributes.Constitution * 5;
        CurrentHP = MaximumHP;

        MaxAP = 3 + (attributes.Dexterity / 10);
        CurrentAP = MaxAP;

        // Initiative is calculated later at the start of combat
        Initiative = 0;
    } */

    public void RollInitiative(int dexterity)
    {
        Initiative = dexterity + Random.Range(1, 11); // 1 to 10 inclusive
    }

    public void RestoreAP()
    {
        CurrentAP = MaxAP;
    }
}