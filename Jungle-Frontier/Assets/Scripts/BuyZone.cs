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

    // Only one selling coroutine at a time
    private bool _isSelling = false;
    private Coroutine _sellRoutine;

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
        var collector = other.GetComponent<ResourceCollector>();
        onPlayerEnter?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isSelling && _sellRoutine != null)
        {
            StopCoroutine(_sellRoutine);
            _isSelling = false;
            _sellRoutine = null;
        }

        if (other.CompareTag("Player"))
        {
            onPlayerExit?.Invoke();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_isSelling)
        {
            return;
        }
        // Only start selling if zone has room and player holds needed resource
        var collector = other.GetComponent<ResourceCollector>();
        int held = (collector != null ? collector.HeldCount : 0);
        bool hasResource = requiredResource == ResourceType.Crystal
            ? (CurrencyManager.Instance.CurrentCurrency > 0)
            : (collector != null && held > 0);
        if (currentCollected >= amountNeeded || !hasResource)
        {
            return;
        }
        _sellRoutine = StartCoroutine(SellRoutine(other));
        _isSelling = true;
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
    /// Resets all internal state and UI of this BuyZone.
    /// </summary>
    private void ResetZone()
    {
        // Reset collected count
        currentCollected = 0;
        // Reset fill image
        if (fillImage != null)
            fillImage.fillAmount = 0f;
        // Reset UI text
        if (amountText != null)
            amountText.text = $"{amountNeeded}";
        // Reset canvas visibility
        if (panelCanvas != null)
            panelCanvas.alpha = 1f;
        // Stop any pending animations
        StopAllCoroutines();
        // Play tick particles reset (optional)
        // if (tickParticles != null) tickParticles.Clear();
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
            // reset internal state/UI before removal
            ResetZone();
            // notify and then destroy
            onBuyComplete.Invoke(transform.position + paintOffset);
            Destroy(gameObject);
        }
    }

    private IEnumerator SellRoutine(Collider playerCollider)
    {
        var collector = playerCollider.GetComponent<ResourceCollector>();
        int slotsLeft = amountNeeded - currentCollected;
        int held = (requiredResource == ResourceType.Crystal)
            ? CurrencyManager.Instance.CurrentCurrency
            : (collector != null ? collector.HeldCount : 0);
        int toSell = Mathf.Min(slotsLeft, held);
        float totalDuration = 1.8f;
        float interval = (amountNeeded > 0) ? (totalDuration / amountNeeded) : 0f;
        int iterations = amountNeeded - currentCollected;
        int sold = 0;
        for (int i = 0; i < iterations; i++)
        {
            // Check for available resources each iteration
            int heldNow = (requiredResource == ResourceType.Crystal)
                ? CurrencyManager.Instance.CurrentCurrency
                : (collector != null ? collector.HeldCount : 0);
            if (heldNow <= 0)
                break;

            if (requiredResource == ResourceType.Crystal)
            {
                CurrencyManager.Instance.TrySpend(1);
                ReceiveResource(null);
            }
            else
            {
                GameObject piece = collector.ProvideResource(requiredResource);
                if (piece == null) break;
                var behavior = piece.GetComponent<ResourceBehavior>();
                if (behavior == null || behavior.resourceType != requiredResource)
                {
                    Destroy(piece);
                    break;
                }
                // Set flight duration if ResourceSender is present
                var sender = piece.GetComponent<ResourceSender>();
                if (sender != null)
                {
                    sender.flightDuration = interval;
                }
                behavior.DepositTo(gameObject);
            }
            yield return new WaitForSeconds(interval);
            sold++;
        }
        _isSelling = false;
        _sellRoutine = null;
    }
}