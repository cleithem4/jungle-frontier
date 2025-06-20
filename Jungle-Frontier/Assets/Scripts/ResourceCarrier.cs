using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Interface for any component that can collect and provide world resources.
/// </summary>
public interface ResourceCollector
{
    /// <summary>Where collected resources should be parented.</summary>
    Transform CarryPivot { get; }

    /// <summary>Max number of resources this collector can hold.</summary>
    int Capacity { get; }

    /// <summary>Current number of held resources.</summary>
    int HeldCount { get; }

    /// <summary>True if at full capacity.</summary>
    bool IsFull { get; }

    /// <summary>
    /// Where collected resources should be parented. Alias for CarryPivot.
    /// </summary>
    Transform StackPoint { get; }

    /// <summary>
    /// Calculates the next vertical offset at which a new resource will stack.
    /// </summary>
    float GetNextStackDepth(ResourceType type);

    /// <summary>
    /// Attempt to pick up the given resource GameObject.
    /// </summary>
    bool Pickup(GameObject resourceGO);

    /// <summary>
    /// Provide (remove and return) a held resource GameObject.
    /// </summary>
    GameObject ProvideResource();
}

/// <summary>
/// Handles picking up, storing, and providing resources for a worker monkey or player.
/// </summary>
public class ResourceCarrier : MonoBehaviour, ResourceCollector
{
    [Header("Carrier Settings")]
    [Tooltip("Maximum number of resources this carrier can hold.")]
    public int capacity = 5;

    [Tooltip("Transform under which held resources will be parented.")]
    public Transform carryPivot;

    [Tooltip("Vertical spacing between stacked resources.")]
    public float stackSpacing = 0.5f;

    // Internal list of held resource GameObjects
    private List<GameObject> heldResources = new List<GameObject>();

    // ResourceCollector interface implementation
    public Transform CarryPivot => carryPivot;
    public int Capacity => capacity;
    public int HeldCount => heldResources.Count;
    public bool IsFull => heldResources.Count >= capacity;

    /// <summary>
    /// Alias for the carry pivot, for compatibility with ResourceCollector consumers.
    /// </summary>
    public Transform StackPoint => carryPivot;

    /// <summary>
    /// Calculates the next vertical offset at which a new resource will stack.
    /// </summary>
    public float GetNextStackDepth(ResourceType type)
    {
        return heldResources.Count * stackSpacing;
    }

    /// <summary>
    /// Attempts to pick up the given resource. Returns true on success.
    /// </summary>
    public bool Pickup(GameObject resourceGO)
    {
        if (resourceGO == null || heldResources.Count >= capacity)
            return false;

        // If this is a crystal, convert to currency instead of stacking
        var resComp = resourceGO.GetComponent<Resource>();
        if (resComp != null && resComp.resourceType == ResourceType.Crystal)
        {
            // Add to global currency
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.Add(1);
            // Destroy the crystal object and do not stack it
            Destroy(resourceGO);
            return true;
        }

        // Preserve the resource's authored scale so it doesn’t inherit parent scale
        Vector3 originalLocalScale = resourceGO.transform.localScale;

        // Parent under pivot and add to list
        resourceGO.transform.SetParent(carryPivot, worldPositionStays: false);

        // Restore the original local scale
        resourceGO.transform.localScale = originalLocalScale;

        heldResources.Add(resourceGO);

        // Disable physics and collider if present
        if (resourceGO.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = true;
        if (resourceGO.TryGetComponent<Collider>(out var col))
            col.enabled = false;

        Restack();
        return true;
    }

    /// <summary>
    /// Removes and returns one held resource (last in). Returns null if empty.
    /// </summary>
    public GameObject ProvideResource()
    {
        if (heldResources.Count == 0)
            return null;

        // Take the top item
        int last = heldResources.Count - 1;
        GameObject resourceGO = heldResources[last];
        heldResources.RemoveAt(last);

        // Unparent and restore physics/collider
        resourceGO.transform.SetParent(null, worldPositionStays: true);
        if (resourceGO.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = false;
        if (resourceGO.TryGetComponent<Collider>(out var col))
            col.enabled = true;

        Restack();
        return resourceGO;
    }

    /// <summary>
    /// Removes up to <paramref name="count"/> resources from the top of the stack
    /// and returns them as a list of GameObjects.
    /// </summary>
    public List<GameObject> DrainResources(int count)
    {
        var drained = new List<GameObject>();
        for (int i = 0; i < count && heldResources.Count > 0; i++)
        {
            int lastIndex = heldResources.Count - 1;
            GameObject go = heldResources[lastIndex];
            heldResources.RemoveAt(lastIndex);

            // Unparent and restore physics/collider
            go.transform.SetParent(null, worldPositionStays: true);
            if (go.TryGetComponent<Rigidbody>(out var rb))
                rb.isKinematic = false;
            if (go.TryGetComponent<Collider>(out var col))
                col.enabled = true;

            drained.Add(go);
        }

        // Rebuild the remaining stack positions
        Restack();
        return drained;
    }

    /// <summary>
    /// Repositions all held resources in a neat vertical stack.
    /// </summary>
    private void Restack()
    {
        for (int i = 0; i < heldResources.Count; i++)
        {
            var go = heldResources[i];
            go.transform.localPosition = new Vector3(0f, i * stackSpacing, 0f);
            go.transform.localRotation = Quaternion.identity;
        }
    }

}
