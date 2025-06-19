using UnityEngine;

[CreateAssetMenu(fileName = "NPCData", menuName = "Scriptable Objects/NPCData")]
public class NPCData : ScriptableObject
{
    [Header("Trade Configuration")]
    [Tooltip("Resource type this NPC wants to buy.")]
    public ResourceType resourceWanted;

    [Tooltip("Amount of currency awarded to the player when trading.")]
    public int currencyReward = 10;

    [Header("Movement Settings")]
    [Tooltip("Run speed of the NPC in world units per second.")]
    public float runSpeed = 5f;

    [Header("Animation States")]
    [Tooltip("Name of the run animation state.")]
    public string runAnimState = "Run";

    [Tooltip("Name of the dance animation state.")]
    public string danceAnimState = "Dance";

    [Tooltip("Duration the NPC will dance after trading.")]
    public float danceDuration = 2f;
}
