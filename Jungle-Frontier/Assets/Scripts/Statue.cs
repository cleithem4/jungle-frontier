using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(ResourceReceiver))]
public class Statue : MonoBehaviour
{
    [Header("Statue Settings")]
    [Tooltip("Which resource type this statue accepts.")]
    public ResourceType requiredResource;

    [Tooltip("Number of crystals to reward per resource inserted.")]
    public int crystalsPerResource = 1;

    [Tooltip("Maximum number of resources the statue can accept before completion.")]
    public int maxCapacity = 10;

    // Store the original ground position
    private Vector3 groundPosition;

    private ResourceReceiver resourceReceiver;
    private int currentCollected = 0;

    [Header("Events")]
    [Tooltip("Fired each time the statue rewards crystals.")]
    public UnityEvent<int> onReward; // passes number of crystals rewarded

    [Tooltip("Fired when the statue has reached its max capacity.")]
    public UnityEvent onCapacityReached;

    [Header("Drop-In Settings")]
    [Tooltip("Height above the statue to start the drop animation.")]
    public float dropInHeight = 10f;
    [Tooltip("Duration of the drop animation.")]
    public float dropInDuration = 1f;

    [Header("Reward VFX")]
    [Tooltip("Crystal prefab to instantiate and fly to the player.")]
    public GameObject crystalPrefab;
    [Tooltip("Time in seconds for the crystal to travel to the player.")]
    public float crystalFlightDuration = 1f;
    [Tooltip("Local offset from the statue where the crystal spawns.")]
    public Vector3 crystalSpawnOffset = Vector3.zero;

    [Header("Collection Settings")]
    [Tooltip("Seconds between each resource taken from the player while inside the statue trigger.")]
    public float collectInterval = 0.5f;
    private Coroutine _collectCoroutine;

    void Awake()
    {
        // Position statue above ground initially
        groundPosition = transform.position;
        transform.position = groundPosition + Vector3.up * dropInHeight;

        resourceReceiver = GetComponent<ResourceReceiver>();
        // Configure receiver
        resourceReceiver.acceptedResources = new[] { requiredResource };
        resourceReceiver.maxCapacity = maxCapacity;
        resourceReceiver.currentCollected = currentCollected;
        resourceReceiver.onResourceReceived.AddListener(HandleResourceReceived);
    }

    /// <summary>
    /// Plays a drop-in animation from initial above-ground position down to the statue's original position.
    /// Call this after instantiating the statue.
    /// </summary>
    public void InitializeDropIn()
    {
        StartCoroutine(DropInCoroutine(groundPosition));
    }

    private IEnumerator DropInCoroutine(Vector3 targetPos)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        while (elapsed < dropInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropInDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        transform.position = targetPos;
    }

    /// <summary>
    /// Called by the ResourceReceiver when a valid resource is delivered.
    /// Rewards crystals and invokes events.
    /// </summary>
    /// <param name="resourceGO">The GameObject of the delivered resource.</param>
    protected virtual void HandleResourceReceived(GameObject resourceGO)
    {
        Debug.Log($"[Statue] HandleResourceReceived called with resourceGO='{resourceGO.name}', preCount={currentCollected}");
        currentCollected++;

        // Default crystal reward logic (optional)
        var cm = CurrencyManager.Instance;
        if (cm != null)
        {
            // You may need to adjust this call to match your CurrencyManager API
            cm.Add(crystalsPerResource);
        }

        // Notify any listeners
        onReward?.Invoke(crystalsPerResource);

        // Give crystals to the player via their ResourceCarrier
        if (crystalPrefab != null)
        {
            GameObject crystal = Instantiate(crystalPrefab, transform.position, Quaternion.identity);
            var carrier = GameObject.FindWithTag("Player")?.GetComponent<ResourceCarrier>();
            if (carrier != null)
            {
                carrier.Pickup(crystal);
            }
            else
            {
                Destroy(crystal);
            }
        }

        Debug.Log($"[Statue] After reward, currentCollected={currentCollected}, maxCapacity={maxCapacity}");
        // Check for completion
        if (currentCollected >= maxCapacity)
        {
            onCapacityReached?.Invoke();
            // Optionally disable further input
            resourceReceiver.enabled = false;
        }

        // Consume the delivered resource object so it doesn’t remain frozen
        if (resourceGO != null)
            Destroy(resourceGO);
    }



    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_collectCoroutine != null) return;
        var collector = other.GetComponent<ResourceCollector>();
        if (collector != null)
            _collectCoroutine = StartCoroutine(CollectResources(collector));
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_collectCoroutine != null)
        {
            StopCoroutine(_collectCoroutine);
            _collectCoroutine = null;
        }
    }

    private IEnumerator CollectResources(ResourceCollector collector)
    {
        while (true)
        {
            yield return new WaitForSeconds(collectInterval);
            if (currentCollected >= maxCapacity) break;

            var piece = collector.ProvideResource(requiredResource);
            if (piece == null) break;

            var resBehavior = piece.GetComponent<ResourceBehavior>();
            if (resBehavior != null)
                resBehavior.DepositTo(gameObject);

            // Spawn and deliver a crystal to the player via their ResourceCarrier
            if (crystalPrefab != null)
            {
                GameObject crystal = Instantiate(crystalPrefab, transform.position + crystalSpawnOffset, Quaternion.identity);
                var playerCarrier = (collector as MonoBehaviour)?.GetComponent<ResourceCarrier>();
                if (playerCarrier != null)
                {
                    playerCarrier.Pickup(crystal);
                }
                else
                {
                    Destroy(crystal);
                }
            }
        }
        _collectCoroutine = null;
    }
}
