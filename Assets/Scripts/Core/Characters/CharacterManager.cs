using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    [Header("Stats")]
    public CharacterStats Stats = new CharacterStats();

    protected Animator animator;
    protected SpriteRenderer spriteRenderer;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        // Stats.SetCharacterStats();
    }

    public virtual void StartTurn()
    {
        Debug.LogWarning($"{name}: StartTurn() not implemented.");
        // TurnManager.Instance.EndTurn(); // Safety fallback
    }

    public virtual void Attack(CharacterManager target)
    {
        // UIManager.Instance.AddLog($"{name} is attacking {target.name}");
        // Trigger animation
        if (animator != null)
        {
            animator.SetTrigger("isAttacking");
        }

        // Action point check
        if (Stats.CurrentAP < 1)
        {
            // UIManager.Instance.AddLog($"{name} has no AP to attack.");
            return;
        }

        int baseDamage = Stats.Strength;
        target.TakeDamage(baseDamage);
        UseActionPoints(1);
    }


    public virtual void TakeDamage(int damage)
    {
        if (animator != null)
            animator.SetTrigger("wasHit");

        Stats.CurrentHP -= damage;
        // UIManager.Instance.AddLog($"{name} took {damage} damage. HP left: {Stats.CurrentHP}");

        // Show popup
        // UIManager.Instance.ShowDamagePopup(transform.position, damage);

        if (Stats.CurrentHP <= 0)
            Die();
    }

    public virtual void UseActionPoints(int cost)
    {
        Stats.CurrentAP = Mathf.Max(0, Stats.CurrentAP - cost);
    }

    public virtual void RestoreActionPoints()
    {
        Stats.CurrentAP = Stats.MaxAP;
    }

    public virtual void ConsumeResources(int hungerCost = 1, int thirstCost = 1)
    {
        Stats.Hunger = Mathf.Max(0, Stats.Hunger - hungerCost);
        Stats.Thirst = Mathf.Max(0, Stats.Thirst - thirstCost);
    }

    protected virtual void Die()
    {
        // UIManager.Instance.AddLog($"{gameObject.name} has died.");

        // Player-specific fade
        if (CompareTag("Player") && UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverLabel();
        }

        Destroy(gameObject);
    }

}