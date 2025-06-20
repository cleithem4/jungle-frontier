using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;  // for Image, CanvasGroup, etc.
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(ResourceReceiver))]
// Custom UnityEvent subclass that passes a Vector3 argument
[System.Serializable]
public class Vector3Event : UnityEngine.Events.UnityEvent<Vector3> { }

public class BuyZone : MonoBehaviour
{
    public Image fillImage;

    [Header("Icon")]
    public Texture2D iconTexture;  // assign your Texture2D asset here
    public UnityEngine.UI.Image iconImage; // assign your UI Image component here

    [Header("Resource Prefab")]
    public GameObject resourcePrefab; // assign the prefab for the required resource

    [Header("Action")]
    public Vector3Event onBuyComplete;
    public Vector3 paintOffset = Vector3.zero;

    [Header("UI Text")]
    [Tooltip("Text element showing how many more resources are needed.")]
    public TMP_Text amountText;
    [Tooltip("Which resource type this BuyZone accepts (set on ResourceReceiver).")]
    public ResourceType requiredResource;
    public int amountNeeded = 5;
    public int currentCollected = 0;

    [Header("Player Detection")]
    public UnityEvent onPlayerEnter;
    public UnityEvent onPlayerExit;

    [Header("Juicy Feedback")]
    public ParticleSystem tickParticles;    // Particle burst on each tick
    public CanvasGroup panelCanvas; // add a CanvasGroup component to your BuyZone UI root and assign it here
    public float shakeDuration = 0.3f; // Duration of shake on completion

    [Header("Camera Shake")]
    public CameraFollow cameraFollow;  // assign your CameraFollow component here

    [Header("Sell Settings")]
    [Tooltip("Time interval between each resource sale while standing in the zone.")]
    public float sellInterval = 0.5f;
    [Tooltip("Total time (in seconds) to fill the buyzone when using crystals.")]
    public float crystalSellDuration = 2f;
    private float sellTimer = 0f;

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
            amountText.text = $"{amountNeeded - currentCollected}";

        // Configure ResourceReceiver
        var receiver = GetComponent<ResourceReceiver>();
        receiver.acceptedResources = new[] { requiredResource };
        receiver.maxCapacity = amountNeeded;
        receiver.currentCollected = currentCollected;
        receiver.onResourceReceived.AddListener(ReceiveResource);
        //receiver.amountNeeded = amountNeeded;
        //receiver.currentCollected = currentCollected;
        //receiver.onResourceReceived.AddListener(_ => OnResourceReceived());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        onPlayerEnter?.Invoke();

        // If this BuyZone is priced in global currency (crystals), handle spending directly
        if (requiredResource == ResourceType.Crystal)
        {
            var cm = CurrencyManager.Instance;
            if (cm == null || cm.CurrentCurrency <= 0)
                return;

            // Subtract one unit of currency and register a purchase
            cm.TrySpend(1);
            ReceiveResource(null);
            return;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            onPlayerExit?.Invoke();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        // Exit early if this BuyZone is already full
        if (currentCollected >= amountNeeded) return;

        // Accumulate time
        sellTimer += Time.deltaTime;

        // Crystal-priced zone: dynamic interval based on remaining need
        if (requiredResource == ResourceType.Crystal)
        {
            int remaining = amountNeeded - currentCollected;
            if (remaining <= 0) return;
            float interval = crystalSellDuration / remaining;
            if (sellTimer < interval) return;
            sellTimer = 0f;

            var cm = CurrencyManager.Instance;
            if (cm != null && cm.CurrentCurrency > 0)
            {
                cm.TrySpend(1);
                ReceiveResource(null);
            }
            return;
        }

        // Physical-resource zone: fixed interval per item
        if (sellTimer < sellInterval)
            return;
        sellTimer = 0f;

        var collector = other.GetComponent<ResourceCollector>();
        if (collector == null || collector.HeldCount <= 0)
            return;

        // Pull one resource
        GameObject piece = collector.ProvideResource();
        if (piece == null)
            return;

        // Verify it's the right type; destroy if not matching
        var resBehavior = piece.GetComponent<ResourceBehavior>();
        if (resBehavior == null || resBehavior.resourceType != requiredResource)
        {
            Destroy(piece);
            return;
        }

        // Deposit the resource into this zone
        resBehavior.DepositTo(gameObject);
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

    /// <summary>
    /// Called by the player to sell wood pieces into this zone.
    /// </summary>
    public void AttemptPurchase()
    {
        // Will trigger ReceiveResource() for each arriving piece.
        // PlayerScript handles which pieces and the timing.
    }

    /// <summary>
    /// Called whenever ResourceReceiver delivers a resource to this zone.
    /// </summary>
    public void ReceiveResource(GameObject resourceGO)
    {
        if (resourceGO != null)
        {
            var behavior = resourceGO.GetComponent<ResourceBehavior>();
            if (behavior == null || behavior.resourceType != requiredResource || currentCollected + 1 > amountNeeded)
            {
                Destroy(resourceGO);
                return;
            }
        }

        // Update count
        currentCollected++;
        // Fill animation
        float target = (float)currentCollected / amountNeeded;
        StartCoroutine(AnimateFill(target, 0.5f));
        // Tick particles
        if (tickParticles != null)
            tickParticles.Play();
        // Canvas flash
        if (panelCanvas != null)
            StartCoroutine(AnimateCanvasFade(0.5f, 0.1f, 1f, 0.2f));
        // Update UI text
        if (amountText != null)
            amountText.text = $"{amountNeeded - currentCollected}";
        // If full, shake, invoke, destroy
        if (currentCollected >= amountNeeded)
        {
            if (panelCanvas != null)
            {
                var rect = panelCanvas.GetComponent<RectTransform>();
                StartCoroutine(AnimateShake(rect, shakeDuration, 0.2f));
            }
            if (cameraFollow != null)
                cameraFollow.Shake(shakeDuration, 0.1f);
            onBuyComplete.Invoke(transform.position + paintOffset);
            Destroy(gameObject);
        }
    }
}