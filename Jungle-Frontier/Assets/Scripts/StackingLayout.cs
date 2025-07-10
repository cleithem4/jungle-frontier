using UnityEngine;
using UnityEngine.Events;
using System.Linq;

[RequireComponent(typeof(ResourceReceiver))]
[DisallowMultipleComponent]
public class StackingLayout : MonoBehaviour
{
    [Tooltip("Number of columns in your stack (1 = single column).")]
    public int columns = 1;

    [Tooltip("Spacing between items in local space (X = horizontal, Y = vertical).")]
    public Vector2 spacing = new Vector2(0.3f, 0.3f);

    [Tooltip("Transform under which incoming resources are parented.")]
    public Transform stackPoint;

    [Tooltip("Local Euler angles to rotate each stacked item.")]
    public Vector3 itemRotation = Vector3.zero;

    private ResourceReceiver receiver;

    void Awake()
    {
        receiver = GetComponent<ResourceReceiver>();
        receiver.onResourceReceived.AddListener(_ => Restack());
    }

    /// <summary>
    /// Returns the local position for the nth child in the stack.
    /// </summary>
    public Vector3 GetLocalPosition(int index)
    {
        int col = index % columns;
        int row = index / columns;
        return new Vector3(col * spacing.x, row * spacing.y, 0f);
    }

    /// <summary>
    /// Recomputes the local position of every child under stackPoint.
    /// </summary>
    private void Restack()
    {
        // Get all direct children of stackPoint (skip the parent itself)
        var items = stackPoint.Cast<Transform>().Where(t => t != stackPoint).ToList();
        for (int i = 0; i < items.Count; i++)
        {
            // Ensure correct visible size regardless of parent scaling
            items[i].localPosition = GetLocalPosition(i);
            // Apply uniform rotation to each item
            items[i].localRotation = Quaternion.Euler(itemRotation);
        }
    }
}
