using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshProUGUI damageText;

    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private Vector3 floatOffset = new Vector3(0, 1.5f, 0);
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float fadeSpeed = 2f;

    private CanvasGroup canvasGroup;
    private Vector3 moveDirection;

    private void Awake()
    {
        // Auto-assign references
        damageText = GetComponentInChildren<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (damageText == null)
            Debug.LogError("[DM]: Missing TextMeshProUGUI in DamagePopup prefab.");

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(int damage)
    {
        if (damageText != null)
            damageText.text = damage.ToString();

        moveDirection = floatOffset;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += moveDirection * floatSpeed * Time.deltaTime;

        if (canvasGroup != null)
        {
            canvasGroup.alpha -= fadeSpeed * Time.deltaTime;
        }
    }
}