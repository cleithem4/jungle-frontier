using UnityEngine;

/// <summary>
/// Attach this to any resource prefab to identify its type at runtime.
/// </summary>
public class Resource : MonoBehaviour
{
    [Tooltip("The type of this resource (matches ResourceType enum).")]
    public ResourceType resourceType;
}
