using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;


/// <summary>
/// Base class for all enemies: handles health, movement, attacking, and resource drop on death.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBase : MonoBehaviour, Agent
{
    [Header("Hit Feedback")]
    [Tooltip("Tint color to use for hit-flash effect.")]
    public Color flashColor = Color.white;
    [Tooltip("Duration of the hit-flash effect in seconds.")]
    public float flashDuration = 0.1f;
    [Tooltip("Scale multiplier for punch-scale effect.")]
    public float punchScale = 1.2f;
    [Tooltip("Duration of the punch-scale effect in seconds.")]
    public float punchScaleDuration = 0.1f;

    [Header("Knockback")]
    [Tooltip("Direction and strength of knockback applied when attacking.")]
    public Vector3 knockbackForce = new Vector3(5f, 0f, 0f);

    // Internal storage for original materials
    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Color[][] originalColors;

    // Tracks who last hit this enemy
    private ResourceCollector lastHarvester;

    // Agent interface implementation
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;

    [Header("Stats")]
    [Tooltip("Maximum health for this enemy.")]
    public float maxHealth = 10f;
    [Tooltip("Damage dealt per attack.")]
    public float attackDamage = 2f;
    [Tooltip("Distance within which the enemy can attack.")]
    public float attackRange = 1.5f;
    [Tooltip("Time in seconds between attacks.")]
    public float attackCooldown = 1f;

    [Header("Movement")]
    [Tooltip("Movement speed when chasing a target.")]
    public float moveSpeed = 3.5f;
    protected NavMeshAgent agent;
    protected Transform target;

    [Header("Targeting Settings")]
    [Tooltip("Radius within which the enemy will detect new targets.")]
    public float detectionRadius = 5f;
    [Tooltip("Layer mask used to filter potential targets (e.g., Agent_Player, Agent_Friendly).")]
    public LayerMask targetLayerMask;

    [Header("Patrol Settings")]
    [Tooltip("Radius around the start position to patrol.")]
    public float patrolRadius = 5f;
    [Tooltip("Time in seconds between selecting a new patrol point.")]
    public float patrolInterval = 3f;

    private Vector3 homePosition;
    private Vector3 patrolTarget;
    private float lastPatrolTime;

    [Header("Drops")]
    [Tooltip("Prefab to spawn when this enemy dies.")]
    public GameObject dropPrefab;
    [Tooltip("Number of drop prefabs to spawn on death.")]
    public int dropCount = 1;

    // Internal state
    protected float currentHealth;
    private float lastAttackTime;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        currentHealth = maxHealth;
        // Cache renderers and original materials for flash
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
        }
        originalColors = new Color[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            var mats = renderers[i].materials;
            originalColors[i] = new Color[mats.Length];
            for (int j = 0; j < mats.Length; j++)
                originalColors[i][j] = mats[j].color;
        }

        // Initialize patrolling
        homePosition = transform.position;
        ChooseNewPatrolTarget();
        lastPatrolTime = Time.time;
    }

    void Start()
    {
        // Register with CombatManager once the singleton is initialized
        CombatManager.Instance.RegisterAgent(this);
    }

    protected virtual void Update()
    {
        // Acquire a new target if none is set
        if (target == null)
        {
            var nearbyAgents = CombatManager.Instance.QueryNearby(this, detectionRadius, targetLayerMask);
            if (nearbyAgents != null && nearbyAgents.Count > 0)
            {
                // Pick the closest agent
                var nearest = nearbyAgents
                    .OrderBy(a => (a.Transform.position - transform.position).sqrMagnitude)
                    .First();
                SetTarget(nearest.Transform);
            }
        }
        if (target == null)
        {
            // Patrol behavior
            if (Time.time >= lastPatrolTime + patrolInterval ||
                Vector3.Distance(transform.position, patrolTarget) < 1f)
            {
                ChooseNewPatrolTarget();
                lastPatrolTime = Time.time;
            }
            agent.isStopped = false;
            agent.SetDestination(patrolTarget);
        }
        else
        {
            // Chase or attack
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= attackRange)
                TryAttack();
            else
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
        }
    }

    /// <summary>
    /// Attempts an attack if the cooldown period has passed.
    /// </summary>
    protected virtual void TryAttack()
    {
        agent.isStopped = true;
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;
        }
    }

    /// <summary>
    /// Performs the attack. Override in subclasses for custom behavior.
    /// </summary>
    protected virtual void Attack()
    {
        if (target == null) return;

        var otherHealth = target.GetComponent<Health>();
        if (otherHealth != null)
        {
            // Compute knockback direction
            Vector3 knockDir = (target.position - transform.position).normalized;
            // Create and populate AttackData
            var atkData = new AttackData(attackDamage, this as ResourceCollector, "attack");
            atkData.knockbackForce = knockbackForce;
            atkData.knockbackDuration = punchScaleDuration;
            // Send the attack
            otherHealth.Damage(atkData);
        }
    }


    /// <summary>
    /// Inflicts damage using AttackData, recording the attacker.
    /// </summary>
    public virtual void Damage(AttackData atk)
    {
        float healthBefore = currentHealth;
        lastHarvester = atk.source as ResourceCollector;
        currentHealth -= atk.damage;
        // Apply knockback from AttackData
        if (atk.knockbackForce != Vector3.zero && TryGetComponent<Rigidbody>(out var rb))
        {
            StartCoroutine(HandleKnockback(atk.knockbackForce, atk.knockbackDuration));
        }

        // Trigger hit flash and punch-scale feedback
        StartCoroutine(FlashAndPunch());

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Legacy float-based damage, wraps into AttackData with no source.
    /// </summary>
    public void Damage(float amount)
    {
        Damage(new AttackData(amount));
    }

    /// <summary>
    /// Handles death: drops resources and destroys the GameObject.
    /// </summary>
    protected virtual void Die()
    {
        // Spawn and dispatch drop prefabs
        if (dropPrefab != null && dropCount > 0)
        {
            for (int i = 0; i < dropCount; i++)
            {
                // Instantiate at body position
                var go = Instantiate(dropPrefab, transform.position, Quaternion.identity);
                // Apply random impulse
                if (go.TryGetComponent<Rigidbody>(out var rb))
                    rb.AddForce(Random.onUnitSphere * 2f, ForceMode.Impulse);
                // Then have it fly to the harvester
                var resBehavior = go.GetComponent<ResourceBehavior>();
                if (resBehavior != null && lastHarvester != null)
                {
                    resBehavior.Pickup(lastHarvester, 0.2f);
                }
            }
        }
        // TODO: play death animation or effects here
        Destroy(gameObject);
    }

    /// <summary>
    /// Sets the target for chasing and attacking.
    /// </summary>
    public virtual void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private IEnumerator FlashAndPunch()
    {
        // Apply flash color tint
        for (int i = 0; i < renderers.Length; i++)
        {
            var mats = renderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
                mats[j].color = flashColor;
            renderers[i].materials = mats;
        }

        // Punch scale up
        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale * punchScale;

        // Wait for duration
        yield return new WaitForSeconds(Mathf.Max(flashDuration, punchScaleDuration));

        // Restore original colors
        for (int i = 0; i < renderers.Length; i++)
        {
            var mats = renderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
                mats[j].color = originalColors[i][j];
            renderers[i].materials = mats;
        }

        // Restore scale
        transform.localScale = originalScale;

        // Example: If you want to check scale vs punchScale elsewhere, use .magnitude:
        // if (transform.localScale.magnitude > punchScale) { ... }
    }

    private IEnumerator HandleKnockback(Vector3 force, float duration)
    {
        // Temporarily disable NavMeshAgent if present
        if (agent != null)
            agent.isStopped = true;

        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(force, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(duration);

        // After duration, re-enable movement
        if (agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }
    }

    /// <summary>
    /// Chooses a random point within patrolRadius around homePosition.
    /// </summary>
    private void ChooseNewPatrolTarget()
    {
        Vector2 circle = Random.insideUnitCircle * patrolRadius;
        patrolTarget = homePosition + new Vector3(circle.x, 0f, circle.y);
    }

    void OnDestroy()
    {
        // Unregister when this enemy is destroyed
        if (CombatManager.Instance != null)
            CombatManager.Instance.UnregisterAgent(this);
    }
}
