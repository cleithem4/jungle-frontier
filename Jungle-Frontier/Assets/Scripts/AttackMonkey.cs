using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField] private ResourceReceiver lootReceiver;

    private Animator _anim;

    private ResourceCarrier _carrier;
    private ResourceSender _sender;

    private float _attackTimer;

    [Header("Movement Settings")]
    [Tooltip("Run speed of the monkey.")]
    public float runSpeed = 3f;
    [Tooltip("Acceleration of the NavMeshAgent.")]
    public float acceleration = 8f;
    [Tooltip("Turn speed when facing the target.")]
    public float turnSpeed = 5f;

    private NavMeshAgent _agent;
    private bool _isAttacking = false;

    // Agent interface implementation
    public UnityEngine.Transform Transform => this.transform;
    public UnityEngine.GameObject GameObject => this.gameObject;

    protected override void Awake()
    {
        base.Awake();
        _anim = GetComponent<Animator>();
        _carrier = GetComponent<ResourceCarrier>();
        _sender = GetComponent<ResourceSender>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = runSpeed;
        _agent.acceleration = acceleration;
        _agent.stoppingDistance = attackRadius;
        _agent.autoBraking = true;
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
        if (nearestAgent == null || nearestAgent.GameObject == null)
            yield break;

        // Approach target using NavMeshAgent
        _agent.SetDestination(nearestAgent.Transform.position);
        while (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance)
            yield return null;

        // Face the enemy
        Vector3 toEnemy = nearestAgent.Transform.position - transform.position;
        toEnemy.y = 0f;
        if (toEnemy.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(toEnemy.normalized);

        // Play attack animation and wait for it to finish
        if (_anim != null)
        {
            _anim.Play("Chop");
            yield return new WaitForSeconds(attackAnimDuration);
        }

        // Now apply damage
        var enemy = nearestAgent.GameObject.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            var knockback = toEnemy.normalized * knockbackForce;
            var attackData = new AttackData(attackDamage, this, "monkey_attack", knockback, 0.2f);
            enemy.Damage(attackData);

            // Loot logic

            bool noEnemiesNearby = CombatManager.Instance.QueryNearby(this, scanRadius, enemyLayerMask)?.Count == 0;
            if (_carrier.IsFull || noEnemiesNearby)
            { }
        }
    }

    protected override IEnumerator WorkLoop()
    {
        while (true)
        {
            // Smoothly face nearest target if attacking or chasing
            if (!_isAttacking)
            {
                List<Agent> rotateList = CombatManager.Instance.QueryNearby(this, scanRadius, enemyLayerMask);
                if (rotateList != null && rotateList.Count > 0)
                {
                    Vector3 dir = (rotateList[0].Transform.position - transform.position);
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
                    }
                }
            }

            // Update animator speed parameter if using blend tree
            if (_anim != null)
                _anim.SetFloat("Speed", _agent.velocity.magnitude);

            _attackTimer += Time.deltaTime;

            // Check for any enemy nearby via CombatManager
            List<Agent> nearbyEnemies = CombatManager.Instance.QueryNearby(this, scanRadius, enemyLayerMask);
            bool enemyNearby = nearbyEnemies != null && nearbyEnemies.Count > 0;

            // Deposit resources if carrier is full or no enemies nearby
            if ((_carrier != null && (_carrier.IsFull || !enemyNearby)) && _carrier.HeldCount > 0 && lootReceiver != null && _sender != null)
            {
                // Move to stockpile
                yield return MoveTo(lootReceiver.stackPoint.position, 1f);

                // Deposit all held loot pieces
                while (_carrier.HeldCount > 0)
                {
                    GameObject piece = _carrier.ProvideResource(lootResourceType);
                    if (piece != null)
                        _sender.SendTo(piece, lootReceiver.gameObject);
                    yield return new WaitForSeconds(0.1f);
                }

                // After depositing, play idle animation
                if (_anim != null)
                    _anim.Play("Idle");

                // Restart loop
                yield return null;
                continue;
            }

            // Only update run/idle when not mid-attack
            if (!_isAttacking && _anim != null)
            {
                // Play Run when actually moving
                if (_agent.velocity.magnitude > 0.1f)
                    _anim.Play("Run");
                else
                    _anim.Play("Idle");
            }

            // Attempt attack if enemy is in range, not already attacking, and cooldown elapsed
            if (!_isAttacking && enemyNearby && _attackTimer >= attackInterval)
            {
                _isAttacking = true;
                _attackTimer = 0f;
                if (_anim != null)
                    _anim.Play("Chop");

                // Find nearest agent
                Agent nearestAgent = nearbyEnemies
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
