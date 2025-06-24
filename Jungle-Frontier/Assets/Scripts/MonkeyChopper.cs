using System.Collections;
using UnityEngine;
using UnityEngine.AI;



/// <summary>
/// Worker monkey that chops down trees, collects wood, and delivers it to a stockpile.
/// </summary>

[RequireComponent(typeof(NavMeshAgent), typeof(ResourceCarrier))]
[RequireComponent(typeof(ResourceSender))]
public class MonkeyChopper : WorkerBase, Agent
{
    [Header("Chop Settings")]
    [Tooltip("Damage dealt to a tree per chop hit.")]
    public float chopDamage = 1f;
    [Tooltip("Time in seconds between each chop hit.")]
    public float chopInterval = 1f;

    [Header("Wood Delivery")]
    [Tooltip("The stockpile receiver that accepts wood.")]
    public ResourceReceiver woodStockpileReceiver;

    private ResourceSender resourceSender;
    private Animator animator;
    private ResourceCarrier carrier;

    // Agent interface implementation
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;


    /// <summary>
    /// Finds the nearest un-chopped Tree to this monkey.
    /// </summary>
    private Tree FindNearestTree()
    {
        // Faster lookup by tag
        GameObject[] allGO = GameObject.FindGameObjectsWithTag("Tree");
        Tree[] all = new Tree[allGO.Length];
        for (int i = 0; i < allGO.Length; i++)
            all[i] = allGO[i].GetComponent<Tree>();

        Tree nearest = null;
        float minDistSq = float.MaxValue;
        Vector3 pos = transform.position;
        foreach (var t in all)
        {
            if (t.isChopped) continue;
            float dsq = (t.transform.position - pos).sqrMagnitude;
            if (dsq < minDistSq)
            {
                minDistSq = dsq;
                nearest = t;
            }
        }
        return nearest;
    }

    protected override void Awake()
    {
        base.Awake();
        resourceSender = GetComponent<ResourceSender>();
        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogWarning("[MonkeyChopper] No Animator component found on " + name);
        carrier = GetComponent<ResourceCarrier>();
        if (carrier == null)
            Debug.LogWarning("[MonkeyChopper] No ResourceCarrier found on " + name);
    }

    protected override void Start()
    {
        // Start the worker loop
        base.Start();
        // Register with CombatManager after initialization
        CombatManager.Instance.RegisterAgent(this);
    }

    private void OnDestroy()
    {
        // Unregister from CombatManager when destroyed
        if (CombatManager.Instance != null)
            CombatManager.Instance.UnregisterAgent(this);
    }

    protected override IEnumerator WorkLoop()
    {
        while (true)
        {
            // 1) Gather wood until full
            while (!carrier.IsFull)
            {
                // Find nearest tree
                Tree tree = FindNearestTree();
                if (tree == null)
                {
                    // No tree found, wait then retry
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                // Move to chop point
                animator.CrossFade("Run", 0.1f, 0);
                yield return MoveTo(tree.chopPoint.position, 1f);

                // Chop until the tree falls
                animator.CrossFade("Chop", 0.1f, 0);
                while (!tree.isChopped)
                {
                    animator.CrossFade("Chop", 0.1f, 0);
                    // Create an AttackData instance for chopping
                    var attack = new AttackData(chopDamage, carrier, "chop");
                    tree.Damage(attack);
                    yield return new WaitForSeconds(chopInterval);
                }
            }

            // 2) Deliver wood to stockpile
            animator.CrossFade("Run", 0.1f, 0);
            yield return MoveTo(woodStockpileReceiver.stackPoint.position, 1f);

            // Send each piece
            while (carrier.HeldCount > 0)
            {
                GameObject piece = carrier.ProvideResource(ResourceType.Wood);
                if (piece != null)
                    resourceSender.SendTo(piece, woodStockpileReceiver.gameObject);
                else
                    break;
                // slight delay to avoid spamming
                yield return new WaitForSeconds(0.1f);
            }

            animator.CrossFade("Idle", 0.1f, 0);

            // Loop back to gather more
            yield return null;
        }
    }
}
