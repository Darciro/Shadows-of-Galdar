using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class PlayerLightController : MonoBehaviour
{
    [Header("Basic Light Settings")]
    [Tooltip("How far the light reaches.")]
    public float outerRadius = 5f;
    [Tooltip("Dark center radius (optional).")]
    public float innerRadius = 0f;
    [Tooltip("Brightness multiplier.")]
    public float intensity = 1f;
    public Color lightColor = Color.white;

    [Header("Optional Flicker")]
    public bool enableFlicker = false;
    [Tooltip("Speed of flicker noise.")]
    public float flickerSpeed = 1f;
    [Tooltip("Max intensity deviation.")]
    public float flickerAmount = 0.2f;

    private Light2D lt;

    void Awake()
    {
        lt = GetComponent<Light2D>();
        lt.lightType = Light2D.LightType.Point;
        ApplySettings();
    }

    void Update()
    {
        // Always keep it centered on the player
        transform.position = transform.parent.position;

        // Flicker logic
        if (enableFlicker)
        {
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
            lt.intensity = intensity + (noise - 0.5f) * flickerAmount;
        }
        else
        {
            lt.intensity = intensity;
        }

        // Apply any runtime tweaks
        lt.pointLightOuterRadius = outerRadius;
        lt.pointLightInnerRadius = innerRadius;
        lt.color = lightColor;
    }

    private void ApplySettings()
    {
        lt.intensity = intensity;
        lt.pointLightOuterRadius = outerRadius;
        lt.pointLightInnerRadius = innerRadius;
        lt.color = lightColor;
    }
}
