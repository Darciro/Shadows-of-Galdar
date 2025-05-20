using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Vitals")]
    public TMP_Text hpText;
    public TMP_Text apText;
    public TMP_Text hungerText;
    public TMP_Text thirstText;

    [Header("Vitals Sliders")]
    public Slider healthSlider;
    public Slider hungerSlider;
    public Slider thirstSlider;


    [Header("Logs")]
    public TMP_Text logText;
    [SerializeField] private ScrollRect logScrollRect;
    [SerializeField] private RectTransform logContent;
    public TMP_Text turnOrderList;

    [Header("Buttons")]
    public Button moveButton;
    public Button attackButton;
    public Button itemButton;
    public Button endTurnButton;

    [Header("Fade Settings")]
    [SerializeField] private Image fadePanel;
    [SerializeField] private float fadeDuration = 2f;

    [Header("UI Labels (Optional)")]
    [SerializeField] private GameObject gameOverLabel;

    [Header("Turn Phase UI")]
    public TextMeshProUGUI turnsPhaseText;
    public float phaseDisplayDelay = 1f;

    [Header("Enemy Tooltip")]
    public GameObject enemyTooltip;
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyHpText;
    public TextMeshProUGUI enemyActionPointsText;
    public TextMeshProUGUI enemyDamageText;

    [Header("Damage Popup")]
    public GameObject damagePopupPrefab;
    private RectTransform canvasRoot;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (fadePanel != null)
            fadePanel.color = new Color(0, 0, 0, 0); // Start transparent

        if (gameOverLabel != null)
            gameOverLabel.SetActive(false);

        if (canvasRoot == null)
        {
            canvasRoot = GameObject.FindGameObjectWithTag("GameUI")?.GetComponent<RectTransform>();
        }

        HideEnemyTooltip();
        turnOrderList.text = "Combat order\n";
    }

    public void UpdatePlayerVitals()
    {
        Character player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Character>();

        if (hpText != null)
            hpText.text = $"Health Points: {player.CurrentHealth}/{player.MaxHealth}";

        if (hungerText != null)
            hungerText.text = $"Hunger: {player.Hunger}/{player.MaxHunger}";

        if (thirstText != null)
            thirstText.text = $"Thirst: {player.Thirst}/{player.MaxThirst}";

        if (apText != null)
            apText.text = $"Action Points: {player.CurrentActionPoints}/{player.MaxActionPoints}";
    }

    public void AddLog(string message)
    {
        logText.text += message + "\n";
        StartCoroutine(ScrollToBottom());
    }

    public void AddCharToTurnOrder(string character)
    {
        turnOrderList.text += character + "\n";
    }

    private IEnumerator ScrollToBottom()
    {
        // Wait for end of frame to ensure layout is updated
        yield return new WaitForEndOfFrame();

        if (logScrollRect != null)
            logScrollRect.verticalNormalizedPosition = 0f;
    }

    public void ShowTurnPhase(string message)
    {
        turnsPhaseText.text = message;
        // StopAllCoroutines();
        // StartCoroutine(ShowPhaseRoutine(message));
    }

    public void ShowDamagePopup(Vector3 worldPos, int damage)
    {
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        GameObject popup = Instantiate(damagePopupPrefab, canvasRoot);
        popup.GetComponent<RectTransform>().position = screenPos;

        popup.GetComponent<DamagePopup>().Setup(damage);
    }

    private IEnumerator ShowPhaseRoutine(string message)
    {
        turnsPhaseText.text = message;
        turnsPhaseText.gameObject.SetActive(true);

        yield return new WaitForSeconds(phaseDisplayDelay);

        turnsPhaseText.gameObject.SetActive(false);
    }

    public void ShowEnemyTooltip(Character enemy, Vector3 worldPosition)
    {
        if (enemy == null) return;

        enemyNameText.text = enemy.name;
        enemyHpText.text = $"Health Points: {enemy.CurrentHealth} / {enemy.MaxHealth}";
        enemyActionPointsText.text = $"Action Points: {enemy.CurrentActionPoints} / {enemy.MaxActionPoints}";
        enemyDamageText.text = $"Damage: {enemy.attackDamage}";

        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        screenPos += new Vector2(75, -75);
        enemyTooltip.transform.position = screenPos;

        enemyTooltip.SetActive(true);
    }

    public void HideEnemyTooltip()
    {
        if (enemyTooltip == null) return;

        enemyTooltip.SetActive(false);
    }

    public void FadeOutToBlack(System.Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(1f, onComplete));
    }

    public void FadeInFromBlack(System.Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(0f, onComplete));
    }

    private IEnumerator FadeRoutine(float targetAlpha, System.Action onComplete)
    {
        if (fadePanel == null)
        {
            Debug.LogWarning("[DM]: No fadePanel assigned.");
            yield break;
        }

        Color startColor = fadePanel.color;
        Color endColor = new Color(0, 0, 0, targetAlpha);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            fadePanel.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        fadePanel.color = endColor;
        onComplete?.Invoke();
    }

    public void ShowGameOverLabel()
    {
        if (gameOverLabel != null)
        {
            gameOverLabel.SetActive(true);
        }
    }
}