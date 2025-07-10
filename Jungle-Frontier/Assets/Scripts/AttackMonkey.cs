using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(ResourceCarrier), typeof(ResourceSender))]
[RequireComponent(typeof(Collider))]
public class AttackMonkey : WorkerBase, Agent
{
    [Header("Attack Settings")]
    [Tooltip("Radius within which the monkey will search for enemies to attack.")]
    public float attackRadius = 3f;
    [Tooltip("Radius within which the monkey will detect enemies.")]
    public float scanRadius = 5f;
    [Tooltip("Time in seconds between consecutive attacks.")]
    public float attackInterval = 1f;
    [Tooltip("Damage dealt per attack.")]
    public int attackDamage = 10;
    [Tooltip("Force of the knockback applied to enemies.")]
    public float knockbackForce = 5f;
    [Tooltip("Which layers count as enemies.")]
    public LayerMask enemyLayerMask;

    [Header("Animation Settings")]

    [Header("Animation Timing")]
    [Tooltip("Duration of the Chop animation in seconds.")]
    [SerializeField] private float attackAnimDuration = 0.5f;

    [Header("Loot Delivery Settings")]
    [Tooltip("Type of resource the monkey collects from defeated enemies.")]
    [SerializeField] private ResourceType lootResourceType;
    [Tooltip("Receiver to deliver collected loot.")]
    [SerializeField] public ResourceReceiver lootReceiver;

    private Animator _anim;

    private ResourceCarrier _carrier;
    private ResourceSender _sender;

    // Holds the target for the upcoming attack event
    private Agent _attackEventTarget;

    private float _attackTimer;

    [Header("Movement Settings")]
    [Tooltip("Run speed of the monkey.")]
    public float runSpeed = 3f;
    [Tooltip("Acceleration of the NavMeshAgent.")]
    public float acceleration = 8f;
    [Tooltip("Turn speed when facing the target.")]
    public float turnSpeed = 5f;
    [Tooltip("Stopping distance while chasing.")]
    public float chaseStoppingDistance = 2f;

    private bool _isAttacking = false;

    // REMEMBER last known enemy position to return after depositing
    private Vector3 _lastEnemyPosition;

    // Tracks whether we've deposited during idle (no enemies) to avoid repeated deposits
    private bool _depositedIdle = false;

    // Agent interface implementation
    public UnityEngine.Transform Transform => this.transform;
    public UnityEngine.GameObject GameObject => this.gameObject;

    protected override void Awake()
    {
        base.Awake();
        _anim = GetComponent<Animator>();
        _carrier = GetComponent<ResourceCarrier>();
        _sender = GetComponent<ResourceSender>();

    }

    protected override void Start()
    {
        base.Start();
        // Register this monkey as an Agent
        CombatManager.Instance.RegisterAgent(this);
    }

    private void OnDestroy()
    {
        if (CombatManager.Instance != null)
            CombatManager.Instance.UnregisterAgent(this);
    }

    /// <summary>
    /// Moves to the nearest target then performs the attack and loot logic.
    /// </summary>
    private IEnumerator AttackRoutine(Agent nearestAgent)
    {
        // Temporarily use attack range for approaching target
        float originalStop = agent.stoppingDistance;
        agent.stoppingDistance = attackRadius;

        if (nearestAgent == null || nearestAgent.GameObject == null)
            yield break;

        // Cache the target for the animation event
        _attackEventTarget = nearestAgent;

        // Approach target dynamically to attack range
        yield return MoveToDynamic(() => nearestAgent.Transform.position, attackRadius);

        // Face the enemy
        Vector3 toEnemy = nearestAgent.Transform.position - transform.position;
        toEnemy.y = 0f;
        if (toEnemy.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(toEnemy.normalized);

        // Restore chase stopping distance
        agent.stoppingDistance = originalStop;

        // Play attack animation and wait for it to finish
        if (_anim != null)
        {
            _anim.Play("Chop");
            yield return new WaitForSeconds(attackAnimDuration);
        }

        // Clear the cached target after attack
        _attackEventTarget = null;
    }

    protected override IEnumerator WorkLoop()
    {
        while (true)
        {
            // Reset idle-deposit flag when enemies reappear
            if (CombatManager.Instance.QueryNearby(this, scanRadius, enemyLayerMask)?.Count > 0)
            {
                if (_depositedIdle)
                {
                    _depositedIdle = false;
                }
            }

            // Gather targets within scan and attack radii
            List<Agent> scanTargets = CombatManager.Instance.QueryNearby(this, scanRadius, enemyLayerMask);
            bool scanNearby = scanTargets != null && scanTargets.Count > 0;

            List<Agent> attackTargets = CombatManager.Instance.QueryNearby(this, attackRadius, enemyLayerMask);
            bool canAttack = attackTargets != null && attackTargets.Count > 0;

            // Track last enemy position when one is in scan range
            if (scanNearby && scanTargets.Count > 0)
                _lastEnemyPosition = scanTargets[0].Transform.position;

            // Deposit if full, or if idle (no enemies) and not yet deposited this idle period
            bool noEnemies = !CombatManager.Instance.QueryNearby(this, scanRadius, enemyLayerMask)?.Any() ?? true;
            if (_carrier != null && _carrier.HeldCount > 0 && (_carrier.IsFull || (noEnemies && !_depositedIdle)) && lootReceiver != null && _sender != null)
            {
                // Temporarily tighten stopping distance so MoveTo can complete
                float originalStop = agent.stoppingDistance;
                agent.stoppingDistance = 0.1f;
                // Move to stockpile
                yield return MoveTo(lootReceiver.stackPoint.position, 0.1f);
                // Restore original stopping distance
                agent.stoppingDistance = originalStop;

                // Deposit all held loot pieces until none remain
                GameObject piece;
                while ((piece = _carrier.ProvideResource(lootResourceType)) != null)
                {
                    _sender.SendTo(piece, lootReceiver.gameObject);
                    yield return new WaitForSeconds(0.1f);
                }
                _depositedIdle = noEnemies;

                // After depositing, play idle animation
                if (_anim != null)
                    _anim.Play("Idle");

                // After depositing, return to last known enemy position to resume chase
                if (_lastEnemyPosition != default(Vector3))
                {
                    // Restore stopping distance in case it changed
                    agent.stoppingDistance = originalStop;
                    yield return MoveToDynamic(() => _lastEnemyPosition, chaseStoppingDistance);
                }

                // Restart loop
                yield return null;
                continue;
            }


            // Smoothly face nearest target if attacking or chasing
            if (!_isAttacking)
            {
                if (scanNearby)
                {
                    Vector3 dir = (scanTargets[0].Transform.position - transform.position);
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
                    }
                }
            }



            _attackTimer += Time.deltaTime;

            // Chase the nearest enemy when not attacking using WorkerBase.MoveToDynamic
            if (scanNearby && !_isAttacking)
            {
                Agent chaseTarget = scanTargets
                    .OrderBy(a => (a.Transform.position - transform.position).sqrMagnitude)
                    .FirstOrDefault();
                if (chaseTarget != null)
                {
                    // Move to within chaseStoppingDistance of the target
                    yield return MoveToDynamic(() => chaseTarget.Transform.position, chaseStoppingDistance);
                }
            }

            // Attempt attack if target within attack radius, not already attacking, and cooldown elapsed
            if (!_isAttacking && canAttack && _attackTimer >= attackInterval)
            {
                _isAttacking = true;
                _attackTimer = 0f;

                // Find nearest agent within attack range
                Agent nearestAgent = attackTargets
                    .OrderBy(a => (a.Transform.position - transform.position).sqrMagnitude)
                    .FirstOrDefault();

                // Move and attack
                yield return AttackRoutine(nearestAgent);

                // Finished attack
                _isAttacking = false;
            }

            yield return null;
        }
    }

    void Update()
    {
        if (!_isAttacking && _anim != null)
        {
            // Play Run when actually moving, otherwise Idle
            if (agent.velocity.sqrMagnitude > 0.01f)
                _anim.Play("Run");
            else
                _anim.Play("Idle");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }

    /// <summary>
    /// Called via Animation Event at the hit frame of the Chop animation.
    /// Applies damage and loot logic precisely when the animation lands.
    /// </summary>
    public void OnAttackHit()
    {
        if (_attackEventTarget == null) return;

        // Apply damage
        var enemy = _attackEventTarget.GameObject.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            Vector3 dir = (_attackEventTarget.Transform.position - transform.position).normalized;
            Vector3 knockback = dir * knockbackForce;
            var attackData = new AttackData(attackDamage, _carrier, "monkey_attack", knockback, 0.2f);
            enemy.Damage(attackData);


        }
    }
}
