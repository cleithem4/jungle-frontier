using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic pickup zone for any resource type. When a ResourceBehavior enters the trigger,
/// hands it off to the PlayerScript to handle (fly to player, add currency or stack).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ResourcePickupZone : MonoBehaviour
{
    [Tooltip("Which resource types this zone will hand off to the player.")]
    public ResourceType[] acceptedTypes;

    [Tooltip("Reference to the player's script that processes collected resources.")]
    public PlayerScript player;

    [Tooltip("Duration of the fly-to-player animation.")]
    public float pickupDuration = 0.5f;

    // Only one pickup coroutine at a time
    private bool _isPickingUp = false;

    void Awake()
    {
        // Ensure this collider is a trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_isPickingUp)
            return;
        _isPickingUp = true;

        // Only trigger when the player enters the zone
        if (other.GetComponent<PlayerScript>() == null)
            return;

        var receiver = GetComponent<ResourceReceiver>();
        if (receiver == null || receiver.stackPoint == null)
            return;

        // Gather current items
        var items = new List<Transform>();
        foreach (Transform t in receiver.stackPoint)
            items.Add(t);

        // Start coroutine to pick them up with delay
        StartCoroutine(PickupRoutine(items, receiver));
    }

    private IEnumerator PickupRoutine(List<Transform> items, ResourceReceiver receiver)
    {
        // Get the player's carrier to check capacity
        var carrier = player.GetComponent<ResourceCarrier>();

        int slotsLeft = carrier != null
            ? (carrier.Capacity - carrier.HeldCount)
            : int.MaxValue;

        foreach (var t in items)
        {
            if (slotsLeft <= 0)
                break;

            var resBehavior = t.GetComponent<ResourceBehavior>();
            if (resBehavior == null)
                continue;

            var resData = resBehavior.GetComponent<Resource>();
            if (acceptedTypes != null && !acceptedTypes.Contains(resData.resourceType))
                continue;

            // Detach this resource from the zone’s stack
            t.SetParent(null, worldPositionStays: true);
            receiver.currentCollected = Mathf.Max(0, receiver.currentCollected - 1);

            // Stop early if now full
            if (carrier != null && carrier.IsFull)
                break;

            // Fly this resource to the player’s back via ResourceBehavior
            resBehavior.Pickup(player, pickupDuration);

            slotsLeft--;

            // Wait before next pickup
            yield return new WaitForSeconds(0.05f);
        }
        _isPickingUp = false;
    }
}
