using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;  // for Image, CanvasGroup, etc.
using UnityEngine.Events;
using TMPro;

public class BuyZone : MonoBehaviour
{
    public Image fillImage;

    [Header("Icon")]
    public Texture2D iconTexture;  // assign your Texture2D asset here
    public UnityEngine.UI.Image iconImage; // assign your UI Image component here

    [Header("Resource Prefab")]
    public GameObject resourcePrefab; // assign the prefab for the required resource

    [Header("Action")]
    public UnityEvent onBuyComplete;

    [Header("UI Text")]
    public TMP_Text amountText; // assign your UI Text component here

    public ResourceType requiredResource;
    public int amountNeeded = 5;
    private int currentCollected = 0;

    [Header("Player Detection")]
    public UnityEvent onPlayerEnter;
    public UnityEvent onPlayerExit;
    private bool playerInZone = false;

    [Header("Juicy Feedback")]
    public ParticleSystem tickParticles;    // Particle burst on each tick
    public CanvasGroup panelCanvas; // add a CanvasGroup component to your BuyZone UI root and assign it here
    public float shakeDuration = 0.3f; // Duration of shake on completion

    [Header("Camera Shake")]
    public CameraFollow cameraFollow;  // assign your CameraFollow component here

    void Start()
    {
        // Create a Sprite from the Texture2D at runtime and assign to the Image
        if (iconTexture != null && iconImage != null)
        {
            Rect rect = new Rect(0, 0, iconTexture.width, iconTexture.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Sprite runtimeSprite = Sprite.Create(iconTexture, rect, pivot);
            iconImage.sprite = runtimeSprite;
        }

        if (amountText != null)
            amountText.text = $"{currentCollected}/{amountNeeded}";
    }

    private void OnTriggerEnter(Collider other)
    {
        // Detect player entering
        if (!other.CompareTag("Player")) return;
        playerInZone = true;
        onPlayerEnter?.Invoke();

        // Grab the PlayerScript to access the stackPoint
        var playerScript = other.GetComponent<PlayerScript>();
        if (playerScript == null) return;

        // Calculate how many more resources we need
        int needed = amountNeeded - currentCollected;
        if (needed <= 0) return;

        // Select the top 'needed' wood pieces by their Y offset
        var woods = playerScript.stackPoint
            .GetComponentsInChildren<Wood>()
            .Where(w => w.CurrentState == WoodState.OnBack)
            .OrderByDescending(w => w.transform.localPosition.y)
            .Take(needed)
            .ToList();

        // Start staggered selling of just those pieces
        StartCoroutine(SellAllWoodsCoroutine(woods));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            onPlayerExit?.Invoke();
        }
    }

    public void ReceiveResource()
    {
        if (currentCollected >= amountNeeded)
            return;

        currentCollected++;
        Debug.Log($"[BuyZone] Received 1 {requiredResource}. Total: {currentCollected}/{amountNeeded}");

        // 1) Smooth, eased fill animation
        float target = (float)currentCollected / amountNeeded;
        StartCoroutine(AnimateFill(target, 0.5f));

        // 2) Particle burst on tick
        if (tickParticles != null)
            tickParticles.Play();

        // 5) Flash/tint the panel briefly
        if (panelCanvas != null)
        {
            StartCoroutine(AnimateCanvasFade(0.5f, 0.1f, 1f, 0.2f));
        }

        // Update amount text
        if (amountText != null)
            amountText.text = $"{currentCollected}/{amountNeeded}";

        // 4) Shake on full completion
        if (currentCollected >= amountNeeded)
        {
            if (panelCanvas != null)
            {
                // Shake the UI element's RectTransform for a better UI shake
                var rect = panelCanvas.GetComponent<RectTransform>();
                StartCoroutine(AnimateShake(rect, shakeDuration, 0.2f));
            }
            // World-space camera shake
            if (cameraFollow != null)
            {
                cameraFollow.Shake(shakeDuration, 0.1f);
            }
            onBuyComplete.Invoke();
        }
    }

    private IEnumerator SellAllWoodsCoroutine(List<Wood> woods)
    {
        float totalDuration = 3f; // total time over which to stagger all woods
        int count = woods.Count;
        if (count == 0) yield break;
        float interval = totalDuration / count;

        for (int i = 0; i < count; i++)
        {
            yield return new WaitForSeconds(interval);
            // Stop if player has exited the zone
            if (!playerInZone) yield break;
            woods[i].StartSellTo(this);
        }
    }

    private IEnumerator AnimateFill(float target, float duration)
    {
        float start = fillImage.fillAmount;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Elastic ease-out approximation
            t = Mathf.Sin(-13f * (t + 1f) * Mathf.PI / 2f) * Mathf.Pow(2f, -10f * t) + 1f;
            fillImage.fillAmount = Mathf.Lerp(start, target, t);
            yield return null;
        }
        fillImage.fillAmount = target;
    }

    private IEnumerator AnimateCanvasFade(float startAlpha, float startDuration, float endAlpha, float endDuration)
    {
        float elapsed = 0f;
        // fade to startAlpha
        while (elapsed < startDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvas.alpha = Mathf.Lerp(1f, startAlpha, elapsed / startDuration);
            yield return null;
        }
        panelCanvas.alpha = startAlpha;
        elapsed = 0f;
        // fade back to endAlpha
        while (elapsed < endDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvas.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / endDuration);
            yield return null;
        }
        panelCanvas.alpha = endAlpha;
    }

    private IEnumerator AnimateShake(RectTransform rect, float duration, float strength)
    {
        Vector2 originalPos = rect.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float damper = 1f - (elapsed / duration);
            float x = originalPos.x + (Random.value * 2f - 1f) * strength * damper;
            float y = originalPos.y + (Random.value * 2f - 1f) * strength * damper;
            rect.anchoredPosition = new Vector2(x, y);
            yield return null;
        }
        rect.anchoredPosition = originalPos;
    }
}