using static ResourceType;
using UnityEngine.EventSystems;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class PlayerScript : MonoBehaviour
{
    public JoyStick joystick; // Reference to your joystick
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f; // degrees per second
    private Animator animator;

    public Transform stackPoint; // general stacking point
    private Dictionary<ResourceType, float> resourceStackDepth = new();
    private float pieceDepth = 0.35f;

    private Tree nearTree = null;
    private bool isChopping = false;
    private float chopTimer = 0f;

    private Dictionary<ResourceType, int> inventory = new();

    // Tracks resource GameObjects currently on the player’s back
    private List<ResourceBehavior> backedResources = new List<ResourceBehavior>();

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator not found on Player or its children!");
        }
        else
        {
            Debug.Log("Animator found: " + animator.gameObject.name);
        }

        if (stackPoint == null)
        {
            Debug.LogError("stackPoint is not assigned on PlayerScript! Please assign it in the Inspector.");
        }
    }

    void Update()
    {
        Vector2 input = joystick.InputDirection;

        // Move the player
        Vector3 move = new Vector3(input.x, 0, input.y);

        // Calculate speed (magnitude of movement)
        float speed = move.magnitude;

        if (nearTree != null)
        {
            // Standing near tree
            if (!isChopping)
            {
                PlayChopAnimation();
            }

            // Update chop timer
            chopTimer += Time.deltaTime;
            if (chopTimer >= nearTree.GetChopTime())
            {
                nearTree.ChopTree();
                ClearNearTree(nearTree);
            }
        }
        else
        {
            // Moving or not near tree → stop chopping
            if (isChopping)
            {
                StopChopAnimation();
            }
        }

        // Set animator Speed parameter
        animator.SetFloat("Speed", speed);

        if (move.magnitude > 0.01f) // only move/rotate if input is significant
        {
            // Move
            transform.position += move * moveSpeed * Time.deltaTime;

            // Rotate to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public void SetNearTree(Tree tree)
    {
        nearTree = tree;
        chopTimer = 0f;
    }

    public void ClearNearTree(Tree tree)
    {
        if (nearTree == tree)
        {
            nearTree = null;
            StopChopAnimation();
        }
    }

    public void PlayChopAnimation()
    {
        isChopping = true;
        animator.SetBool("isChopping", true);
    }

    public void StopChopAnimation()
    {
        isChopping = false;
        animator.SetBool("isChopping", false);
    }

    public float GetNextStackDepth(ResourceType type)
    {
        if (!resourceStackDepth.ContainsKey(type))
            resourceStackDepth[type] = 0f;

        float currentDepth = resourceStackDepth[type];
        resourceStackDepth[type] += pieceDepth;

        Debug.Log($"[PlayerScript] Next stack depth for {type}: {currentDepth}");
        return currentDepth;
    }

    public bool HasResource(ResourceType type)
    {
        return inventory.ContainsKey(type) && inventory[type] > 0;
    }

    public void RemoveResource(ResourceType type)
    {
        if (HasResource(type))
        {
            inventory[type]--;
        }
    }

    /// <summary>
    /// Attempts to spend one unit of the given resource.
    /// Returns true if the player had the resource and it was removed.
    /// </summary>
    public bool TrySpendResource(ResourceType type)
    {
        if (HasResource(type))
        {
            inventory[type]--;
            Debug.Log($"[PlayerScript] Spent 1 {type}. Remaining: {inventory[type]}");
            return true;
        }
        Debug.LogWarning($"[PlayerScript] Not enough {type} to spend.");
        return false;
    }

    public void AddResource(ResourceType type)
    {
        if (!inventory.ContainsKey(type))
            inventory[type] = 0;
        inventory[type]++;
        Debug.Log($"[PlayerScript] Resource {type} count is now: {inventory[type]}");
    }

    public int GetResourceCount(ResourceType type)
    {
        return inventory.ContainsKey(type) ? inventory[type] : 0;
    }

    /// <summary>
    /// Removes the top-most resource of the given type from the player’s back stack
    /// and returns its GameObject for handing off to a receiver.
    /// </summary>
    public GameObject ProvideResource(ResourceType type)
    {
        // Find the highest-stacked resource of this type
        var res = backedResources
            .Where(r => r.GetComponent<Resource>().resourceType == type)
            .OrderByDescending(r => r.transform.localPosition.y)
            .FirstOrDefault();
        if (res == null)
            return null;

        // Detach from player stack
        RemoveBackedResource(res);

        // Unparent so flight coroutine can move it freely
        res.transform.SetParent(null, worldPositionStays: true);
        return res.gameObject;
    }

    /// <summary>
    /// Registers a resource piece on the player’s back and reflows the stack.
    /// </summary>
    public void RegisterBackedResource(ResourceBehavior resource)
    {
        if (!backedResources.Contains(resource))
        {
            backedResources.Add(resource);
            RebuildStack();
        }
    }

    /// <summary>
    /// Unregisters a resource piece (e.g., when it sells itself) and reflows the stack.
    /// </summary>
    public void RemoveBackedResource(ResourceBehavior resource)
    {
        if (backedResources.Remove(resource))
        {
            RebuildStack();
        }
    }

    /// <summary>
    /// Repositions all backed resource pieces under the stackPoint, spacing them by pieceDepth.
    /// </summary>
    private void RebuildStack()
    {
        for (int i = 0; i < backedResources.Count; i++)
        {
            var res = backedResources[i].transform;
            res.SetParent(stackPoint, worldPositionStays: false);
            res.localPosition = new Vector3(0, pieceDepth * i, 0);
            res.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Starts selling the needed resources into the specified BuyZone.
    /// </summary>
    public void SellToBuyZone(BuyZone zone)
    {
        StartCoroutine(SellToReceiverCoroutine(zone.GetComponent<ResourceReceiver>()));
    }

    private IEnumerator SellToReceiverCoroutine(ResourceReceiver receiver)
    {
        int needed = receiver.maxCapacity - receiver.currentCollected;
        if (needed <= 0) yield break;

        var available = backedResources
            .OrderByDescending(r => r.transform.localPosition.y)
            .Take(needed)
            .ToList();

        float totalDuration = 3f;
        float interval = totalDuration / (available.Count > 0 ? available.Count : 1);

        foreach (var res in available)
        {
            res.DepositTo(receiver.gameObject);
            RemoveBackedResource(res);
            yield return new WaitForSeconds(interval);
        }
    }
}