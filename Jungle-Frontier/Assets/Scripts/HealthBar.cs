using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Canvas))]
public class HealthBar : MonoBehaviour
{
    [Tooltip("Reference to the UI Image with Fill Method.")]
    public Image fillImage;

    [Header("Animation")]
    [Tooltip("Duration of fill amount animation in seconds.")]
    public float fillAnimationDuration = 0.5f;
    private Coroutine fillCoroutine;

    private Transform target;
    private Camera mainCam;
    [Tooltip("Local offset from the target's position.")]
    public Vector3 offset = new Vector3(0f, 2f, 0f);

    /// <summary>
    /// Initializes the health bar to follow the given Health component.
    /// </summary>
    /// <param name="health">The Health component to track.</param>
    public void Initialize(Health health)
    {
        // Set initial fill amount
        fillImage.fillAmount = health.CurrentHealth / health.maxHealth;

        // Subscribe to update events
        health.onHealthChanged.AddListener((current, maximum) =>
        {
            AnimateFill(current / maximum);
        });
        health.onDeath.AddListener(() =>
        {
            Destroy(gameObject);
        });

        // Remember the target transform and camera
        target = health.transform;
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (target == null || mainCam == null) return;

        // Position the bar above the target
        Vector3 worldPos = target.position + offset;
        transform.position = worldPos;

        // Keep health bar upright in world space, ignoring parent rotation
        transform.rotation = Quaternion.identity;

        // Always face the camera
        // transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
    }

    /// <summary>
    /// Animate the fillImage from its current value to targetFill over time.
    /// </summary>
    private void AnimateFill(float targetFill)
    {
        if (fillCoroutine != null)
            StopCoroutine(fillCoroutine);
        fillCoroutine = StartCoroutine(FillRoutine(targetFill));
    }

    private IEnumerator FillRoutine(float targetFill)
    {
        float startFill = fillImage.fillAmount;
        float elapsed = 0f;
        while (elapsed < fillAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fillAnimationDuration);
            fillImage.fillAmount = Mathf.Lerp(startFill, targetFill, t);
            yield return null;
        }
        fillImage.fillAmount = targetFill;
        fillCoroutine = null;
    }
}
