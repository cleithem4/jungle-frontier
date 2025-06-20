using UnityEngine;
using UnityEngine.Events;
using System.Linq;

[System.Serializable]
public class ResourceEvent : UnityEvent<GameObject> { }

// NOTE: This requires a separate `Resource` MonoBehaviour on each resource prefab,
//       which holds a `public ResourceType resourceType` field.
public class ResourceReceiver : MonoBehaviour
{
    [Tooltip("Which resource types this receiver accepts.")]
    public ResourceType[] acceptedResources;

    [Tooltip("Maximum number of resources this receiver will accept.")]
    public int maxCapacity;

    [HideInInspector]
    public int currentCollected;  // tracks count so far

    [Tooltip("Transform under which incoming resources will be parented.")]
    public Transform stackPoint;

    [Tooltip("Invoked when a resource GameObject arrives.")]
    public ResourceEvent onResourceReceived;

    /// <summary>
    /// Call this method to deliver a resource GameObject to this receiver.
    /// </summary>
    public void ReceiveResource(GameObject resourceGO)
    {
        if (resourceGO == null) return;
        // Preserve the resource's original local scale
        Vector3 originalLocalScale = resourceGO.transform.localScale;

        // Enforce maximum capacity
        if (maxCapacity > 0 && currentCollected >= maxCapacity)
        {
            Debug.LogWarning($"ResourceReceiver on {gameObject.name} at capacity ({currentCollected}/{maxCapacity}).");
            return;
        }

        // Optionally validate resource type (requires a Resource component on the prefab)
        var resComponent = resourceGO.GetComponent<Resource>();
        if (resComponent != null && (acceptedResources == null || !acceptedResources.Contains(resComponent.resourceType)))
        {
            Debug.LogWarning($"ResourceReceiver on {gameObject.name} rejected {resourceGO.name} (invalid type).");
            return;
        }

        // Parent the resource under the stack point for visual stacking
        if (stackPoint != null)
        {
            resourceGO.transform.SetParent(stackPoint, worldPositionStays: false);
            // Restore the original local scale so the wood stays its authored size
            resourceGO.transform.localScale = originalLocalScale;
        }

        // Track how many have been collected
        currentCollected++;

        // Invoke any hooked-up callbacks (e.g., positioning, inventory count, UI updates)
        onResourceReceived?.Invoke(resourceGO);
    }

    /// <summary>
    /// True if this receiver has reached its maxCapacity.
    /// </summary>
    public bool IsFull => maxCapacity > 0 && currentCollected >= maxCapacity;

    /// <summary>
    /// Number of resource GameObjects currently stacked under stackPoint.
    /// </summary>
    public int HeldCount => stackPoint != null ? stackPoint.childCount : currentCollected;

    /// <summary>
    /// Removes and returns the top resource GameObject from this receiver’s stack.
    /// </summary>
    public GameObject ProvideResource()
    {
        if (stackPoint == null || stackPoint.childCount == 0)
            return null;

        // Take the last child as the top of the stack
        Transform top = stackPoint.GetChild(stackPoint.childCount - 1);
        GameObject go = top.gameObject;

        // Unparent and update count
        top.SetParent(null, worldPositionStays: true);
        currentCollected = Mathf.Max(0, currentCollected - 1);

        return go;
    }
}
