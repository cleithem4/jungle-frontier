using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Generic behavior for any resource prefab: handles pickup by the player
/// and depositing to any ResourceReceiver via ResourceSender.
/// Requires a Resource component (holding ResourceType) on the same GameObject.
/// </summary>
[RequireComponent(typeof(Resource))]
public class ResourceBehavior : MonoBehaviour
{
    [Tooltip("Duration for flying onto the player's back.")]
    public float pickupDuration = 0.5f;

    private Resource resourceData;
    private ResourceCollector collector;

    void Awake()
    {
        resourceData = GetComponent<Resource>();
    }

    /// <summary>
    /// Kicks off the pickup routine: the resource flies to the player's stack.
    /// </summary>
    /// <param name="who">The player instance picking this up.</param>
    /// <param name="delay">Optional delay before starting the pickup.</param>
    public void Pickup(ResourceCollector who, float delay = 0f)
    {
        if (who == null) { Debug.LogWarning("[ResourceBehavior] Pickup called with null collector"); return; }
        collector = who;
        StopAllCoroutines();
        StartCoroutine(PickupRoutine(delay));
    }

    private IEnumerator PickupRoutine(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // Abort if no valid collector
        if (collector == null)
            yield break;

        // Disable physics if present
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Animate flight onto player back
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        while (elapsed < pickupDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / pickupDuration);
            // Recalculate stack height each frame
            float yOffset = collector.GetNextStackDepth(resourceData.resourceType);
            Vector3 currentTarget = collector.StackPoint.position + Vector3.up * yOffset;
            transform.position = Vector3.Lerp(startPos, currentTarget, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // At end of flight, hand off to the carrier
        if (collector != null)
        {
            collector.Pickup(gameObject);
        }
    }

    /// <summary>
    /// Deposits this resource to a given receiver: flies there and then delivers it.
    /// </summary>
    /// <param name="receiverGO">GameObject with a ResourceReceiver component.</param>
    /// <param name="onComplete">Callback after deposit completes.</param>
    public void DepositTo(GameObject receiverGO, Action onComplete = null)
    {
        // Use or add ResourceSender to handle flight and delivery
        var sender = GetComponent<ResourceSender>() ?? gameObject.AddComponent<ResourceSender>();
        sender.SendTo(gameObject, receiverGO, onComplete);
    }
}
