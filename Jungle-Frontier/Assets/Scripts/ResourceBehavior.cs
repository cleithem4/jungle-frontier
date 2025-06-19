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
    private PlayerScript player;
    private Transform playerStackPoint;

    void Awake()
    {
        resourceData = GetComponent<Resource>();
    }

    /// <summary>
    /// Kicks off the pickup routine: the resource flies to the player's stack.
    /// </summary>
    /// <param name="who">The player instance picking this up.</param>
    /// <param name="stackPoint">Transform where the resource should land.</param>
    /// <param name="delay">Optional delay before starting the pickup.</param>
    public void Pickup(PlayerScript who, Transform stackPoint, float delay = 0f)
    {
        player = who;
        playerStackPoint = stackPoint;
        StartCoroutine(PickupRoutine(delay));
    }

    private IEnumerator PickupRoutine(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

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
            float yOffset = player.GetNextStackDepth(resourceData.resourceType);
            Vector3 currentTarget = playerStackPoint.position + Vector3.up * yOffset;
            transform.position = Vector3.Lerp(startPos, currentTarget, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Snap to player's current stack height
        float finalYOffset = player.GetNextStackDepth(resourceData.resourceType);
        transform.position = playerStackPoint.position + Vector3.up * finalYOffset;

        // Parent to stackPoint
        transform.SetParent(playerStackPoint, worldPositionStays: false);
        transform.localPosition = new Vector3(0f, finalYOffset, 0f);
        transform.localRotation = Quaternion.identity;

        // Notify player of new resource
        player.AddResource(resourceData.resourceType);
        player.RegisterBackedResource(this);
    }

    /// <summary>
    /// Deposits this resource to a given receiver: flies there and then delivers it.
    /// </summary>
    /// <param name="receiverGO">GameObject with a ResourceReceiver component.</param>
    /// <param name="onComplete">Callback after deposit completes.</param>
    public void DepositTo(GameObject receiverGO, Action onComplete = null)
    {
        // Remove from player stack immediately
        player.RemoveResource(resourceData.resourceType);
        player.RemoveBackedResource(this);

        // Use or add ResourceSender to handle flight and delivery
        var sender = GetComponent<ResourceSender>() ?? gameObject.AddComponent<ResourceSender>();
        sender.SendTo(gameObject, receiverGO, onComplete);
    }
}
