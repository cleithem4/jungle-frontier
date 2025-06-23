using static ResourceType;
using UnityEngine.EventSystems;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PlayerScript : MonoBehaviour, ResourceCollector, Agent
{
    public JoyStick joystick; // Reference to your joystick
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f; // degrees per second
    private Animator animator;

    [Header("Chopping Settings")]
    [Tooltip("Damage dealt to a tree per chop hit.")]
    public float chopDamage = 1f;
    [Tooltip("Time interval (in seconds) between each chop hit.")]
    public float chopInterval = 1f;

    [Header("Combat Knockback")]
    [Tooltip("Force applied to enemies when attacked.")]
    public float attackKnockbackForce = 5f;

    [Header("Area Attack Settings")]
    [Tooltip("Radius within which to auto-attack all enemies.")]
    public float attackRadius = 2f;

    [Header("Enemy Detection")]
    [Tooltip("Which layers count as enemies for area attacks.")]
    public LayerMask enemyLayerMask;

    private Dictionary<ResourceType, int> inventory = new();

    [Header("Carry Settings")]
    [Tooltip("Maximum number of resources the player can carry.")]
    public int capacity = 10;

    // ResourceCollector interface
    public int Capacity => capacity;
    public int HeldCount => GetComponent<ResourceCarrier>().HeldCount;
    public bool IsFull => GetComponent<ResourceCarrier>().IsFull;

    // ResourceCollector: where picked-up items should be parented
    public Transform CarryPivot => GetComponent<ResourceCarrier>().carryPivot;

    // ResourceCollector alias for legacy naming
    public Transform StackPoint => CarryPivot;

    // ResourceCollector: compute next stack height
    public float GetNextStackDepth(ResourceType type)
    {
        return GetComponent<ResourceCarrier>().GetNextStackDepth(type);
    }

    // Agent interface implementation
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;

    private void OnEnable()
    {
        CombatManager.Instance.RegisterAgent(this);
    }

    private void OnDisable()
    {
        CombatManager.Instance.UnregisterAgent(this);
    }

    // Direct animation state management
    private enum PlayerAnimState { Idle, Running, Chopping }
    private PlayerAnimState _currentAnimState = PlayerAnimState.Idle;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        Vector2 input = joystick.InputDirection;

        // Move the player
        Vector3 move = new Vector3(input.x, 0, input.y);

        // Calculate speed (magnitude of movement)
        float speed = move.magnitude;

        // Only perform area attack if any enemy is within range
        attackTimer += Time.deltaTime;
        if (attackTimer >= chopInterval)
        {
            // Quick check for any enemy in radius using layer mask
            if (Physics.CheckSphere(transform.position, attackRadius, enemyLayerMask))
            {
                // Fetch all colliders in range, but only those on enemy layers
                Collider[] hits = Physics.OverlapSphere(transform.position, attackRadius, enemyLayerMask);
                foreach (var hit in hits)
                {
                    var enemy = hit.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        Vector3 knockback = (enemy.transform.position - transform.position).normalized * attackKnockbackForce;
                        var attackData = new AttackData(chopDamage, this, "chop", knockback, 0.2f);
                        enemy.Damage(attackData);
                    }
                }
            }
            attackTimer = 0f;
        }

        // If the current tree has been chopped down, stop chopping
        if (nearTree != null && nearTree.isChopped)
        {
            nearTree = null;
            chopTimer = 0f;
        }

        if (nearTree != null)
        {
            chopTimer += Time.deltaTime;
            if (chopTimer >= chopInterval)
            {
                Vector3 knockback = (nearTree.transform.position - transform.position).normalized * attackKnockbackForce;
                var attack = new AttackData(chopDamage, this, "chop");
                nearTree.Damage(attack);
                chopTimer = 0f;
            }
        }
        else
        {
            // Not near a tree: reset timer
            chopTimer = 0f;
        }

        if (move.magnitude > 0.01f) // only move/rotate if input is significant
        {
            // Move
            transform.position += move * moveSpeed * Time.deltaTime;

            // Rotate to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Determine if any enemy is within attack range for chopping animation
        bool enemyInRange = Physics.CheckSphere(transform.position, attackRadius, enemyLayerMask);

        // Override animator states directly
        PlayerAnimState newState;
        if (enemyInRange || nearTree != null)
            newState = PlayerAnimState.Chopping;
        else if (move.magnitude > 0.01f)
            newState = PlayerAnimState.Running;
        else
            newState = PlayerAnimState.Idle;

        if (newState != _currentAnimState)
        {
            _currentAnimState = newState;
            switch (_currentAnimState)
            {
                case PlayerAnimState.Idle:
                    animator.CrossFade("Idle", 0.1f, 0);
                    break;
                case PlayerAnimState.Running:
                    animator.CrossFade("Run", 0.1f, 0);
                    break;
                case PlayerAnimState.Chopping:
                    animator.CrossFade("Chop", 0.1f, 0);
                    break;
            }
        }
    }

    private Tree nearTree = null;
    private float chopTimer = 0f;

    private float attackTimer = 0f;

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
        }
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
            return true;
        }
        return false;
    }

    public void AddResource(ResourceType type)
    {
        if (!inventory.ContainsKey(type))
            inventory[type] = 0;
        inventory[type]++;
    }

    public int GetResourceCount(ResourceType type)
    {
        return inventory.ContainsKey(type) ? inventory[type] : 0;
    }

    /// <summary>
    /// Generic entry point for ResourcePickupZone; routes per-type behavior.
    /// </summary>
    public void CollectResource(ResourceBehavior resBehavior, float pickupDuration)
    {
        var type = resBehavior.GetComponent<Resource>().resourceType;

        switch (type)
        {
            case ResourceType.Wood:
                // Use generic carrier pickup
                Pickup(resBehavior.gameObject);
                break;

            default:
                // For any other resource (e.g. Crystal), fly to player then award immediately
                StartCoroutine(CollectAndConsume(resBehavior, pickupDuration, type));
                break;
        }
    }

    private IEnumerator CollectAndConsume(ResourceBehavior resBehavior, float duration, ResourceType type)
    {
        // Fly to player
        resBehavior.pickupDuration = duration;
        // Initiate pickup using the assigned pickupDuration
        resBehavior.Pickup(this);

        // Wait until it lands on the player's back
        while (resBehavior != null && resBehavior.transform.parent != null)
            yield return null;

        // Award or process resource
        switch (type)
        {
            case ResourceType.Crystal:
                CurrencyManager.Instance.Add(1);
                break;
                // add other resource types here if needed
        }

        // Cleanup the GameObject
        if (resBehavior != null && resBehavior.gameObject != null)
            Destroy(resBehavior.gameObject);
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

        var available = GetComponent<ResourceCarrier>()
            .DrainResources(needed);

        float totalDuration = 3f;
        float interval = totalDuration / (available.Count > 0 ? available.Count : 1);

        foreach (var resGO in available)
        {
            // Stop if the receiver is already full
            if (receiver.currentCollected >= receiver.maxCapacity)
                yield break;

            var resBehavior = resGO.GetComponent<ResourceBehavior>();
            if (resBehavior != null)
            {
                resBehavior.DepositTo(receiver.gameObject);
            }
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>
    /// Picks up a resource GameObject via the generic interface.
    /// </summary>
    public bool Pickup(GameObject resourceGO)
    {
        if (resourceGO == null) return false;
        // Delegate to ResourceCarrier
        return GetComponent<ResourceCarrier>().Pickup(resourceGO);
    }

    /// <summary>
    /// Provides (removes and returns) the top resource GameObject.
    /// </summary>
    public GameObject ProvideResource(ResourceType resourceType)
    {
        // Delegate to ResourceCarrier
        return GetComponent<ResourceCarrier>().ProvideResource(resourceType);
    }
    // Detect when entering a tree’s trigger zone
    private void OnTriggerEnter(Collider other)
    {
        // If this collider belongs to a Tree and it’s not already chopped
        var tree = other.GetComponent<Tree>();
        if (tree != null && !tree.isChopped)
        {
            SetNearTree(tree);
        }
    }

    // Detect when leaving a tree’s trigger zone
    private void OnTriggerExit(Collider other)
    {
        var tree = other.GetComponent<Tree>();
        if (tree != null)
        {
            ClearNearTree(tree);
        }
    }
}