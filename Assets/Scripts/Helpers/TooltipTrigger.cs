
using UnityEngine;

public class TooltipTrigger : MonoBehaviour
{
    private Character character;

    private void Awake()
    {
        character = GetComponent<Character>();
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