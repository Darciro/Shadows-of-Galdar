
using UnityEngine;

public class TooltipTrigger : MonoBehaviour
{
    private Combatant character;

    private void Awake()
    {
        character = GetComponent<Combatant>();
    }

    private void OnMouseEnter()
    {
        UIManager.Instance.ShowEnemyTooltip(character, transform.position);
    }

    private void OnMouseExit()
    {
        UIManager.Instance.HideEnemyTooltip();
    }
}