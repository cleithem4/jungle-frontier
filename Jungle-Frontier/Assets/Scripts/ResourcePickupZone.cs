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

    // Pickup zone mode: player-triggered or resource-triggered
    public enum PickupZoneMode { PlayerTriggered, ResourceTriggered }
    public PickupZoneMode pickupMode = PickupZoneMode.PlayerTriggered;

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
        if (pickupMode == PickupZoneMode.PlayerTriggered)
        {
            Debug.Log($"[ResourcePickupZone] OnTriggerEnter by {other.name}. _isPickingUp={_isPickingUp}");
            if (_isPickingUp)
            {
                Debug.Log("[ResourcePickupZone] Already picking up, ignoring trigger.");
                return;
            }
            _isPickingUp = true;

            if (other.GetComponent<PlayerScript>() == null)
            {
                Debug.Log("[ResourcePickupZone] Trigger ignored: not player.");
                _isPickingUp = false;
                return;
            }

            var receiver = GetComponent<ResourceReceiver>();
            if (receiver == null || receiver.stackPoint == null)
            {
                Debug.LogWarning("[ResourcePickupZone] No ResourceReceiver or stackPoint found.");
                _isPickingUp = false;
                return;
            }

            var items = new List<Transform>();
            foreach (Transform t in receiver.stackPoint)
                items.Add(t);
            items.Reverse();

            Debug.Log($"[ResourcePickupZone] Starting pickup routine. Items in stack: {receiver.stackPoint.childCount}");
            StartCoroutine(PickupRoutine(items, receiver));
        }
        else if (pickupMode == PickupZoneMode.ResourceTriggered)
        {
            var resBehavior = other.GetComponent<ResourceBehavior>();
            if (resBehavior == null)
            {
                Debug.Log("[ResourcePickupZone] Trigger ignored: not resource.");
                return;
            }

            var resData = resBehavior.GetComponent<Resource>();
            if (resData == null || (acceptedTypes != null && !acceptedTypes.Contains(resData.resourceType)))
            {
                Debug.Log($"[ResourcePickupZone] Ignored resource {other.name}: invalid type.");
                return;
            }

            Debug.Log($"[ResourcePickupZone] Picking up {other.name} via trigger.");
            resBehavior.DepositTo(gameObject);
        }
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
            Debug.Log($"[ResourcePickupZone] Processing item {t.name}");
            if (slotsLeft <= 0)
                break;

            var resBehavior = t.GetComponent<ResourceBehavior>();
            if (resBehavior == null)
                continue;

            var resData = resBehavior.GetComponent<Resource>();
            if (acceptedTypes != null && !acceptedTypes.Contains(resData.resourceType))
            {
                Debug.Log($"[ResourcePickupZone] Skipped {t.name}: unacceptable resource type.");
                continue;
            }

            // Detach this resource from the zone’s stack
            t.SetParent(null, worldPositionStays: true);
            receiver.currentCollected = Mathf.Max(0, receiver.currentCollected - 1);

            Debug.Log($"[ResourcePickupZone] Picking up {t.name}");

            // Stop early if now full
            if (carrier != null && carrier.IsFull)
                break;

            // Fly this resource to the player’s back via ResourceBehavior
            resBehavior.Pickup(player, pickupDuration);

            slotsLeft--;

            // Wait before next pickup
            yield return new WaitForSeconds(0.05f);
        }
        Debug.Log("[ResourcePickupZone] Pickup routine complete.");
        _isPickingUp = false;
    }
}
