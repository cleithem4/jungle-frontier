using UnityEngine;

[CreateAssetMenu(menuName = "Game/Resource Data", fileName = "ResourceData")]
public class ResourceData : ScriptableObject
{
    public ResourceType type;       // The enum value for this resource
    public Sprite icon;       // UI icon to show in buy zones and inventory
    public string displayName; // Human-readable name (e.g. "Wood", "Stone")
}
