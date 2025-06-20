using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class Snake : EnemyBase
{
    [Header("Patrol Settings")]
    [Tooltip("Radius around the start position to patrol.")]
    public float patrolRadius = 5f;
    [Tooltip("Time in seconds between selecting a new patrol point.")]
    public float patrolInterval = 3f;

    [Header("Detection Settings")]
    [Tooltip("Distance at which the snake will detect and chase the player.")]
    public float detectRadius = 8f;


    private Animator animator;
    private Vector3 homePosition;
    private Vector3 patrolTarget;
    private float lastPatrolTime;
    private Transform playerTransform;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        homePosition = transform.position;
        ChooseNewPatrolTarget();
        // Find the player by tag; ensure your player GameObject is tagged "Player"
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            playerTransform = playerGO.transform;
    }

    protected override void Update()
    {
        // Determine whether to chase player or patrol
        if (playerTransform != null &&
            Vector3.Distance(transform.position, playerTransform.position) <= detectRadius)
        {
            // Chase and attack via base behavior
            SetTarget(playerTransform);
        }
        else
        {
            // Patrol behavior
            target = null; // stop chasing
            animator.CrossFade("Slither", 0.1f, 0);

            if (Time.time >= lastPatrolTime + patrolInterval ||
                Vector3.Distance(transform.position, patrolTarget) < 1f)
            {
                ChooseNewPatrolTarget();
                lastPatrolTime = Time.time;
            }
            agent.isStopped = false;
            agent.SetDestination(patrolTarget);
        }

        // Play Idle if not moving or attacking
        if (agent.velocity.magnitude < 0.1f && target == null)
        {
            animator.CrossFade("Idle", 0.1f, 0);
        }

        base.Update();
    }

    protected override void Attack()
    {
        base.Attack();
        animator.CrossFade("Bite", 0.1f, 0);
    }

    /// <summary>
    /// Chooses a random point within patrolRadius around homePosition.
    /// </summary>
    private void ChooseNewPatrolTarget()
    {
        Vector2 circle = Random.insideUnitCircle * patrolRadius;
        patrolTarget = homePosition + new Vector3(circle.x, 0f, circle.y);
    }
}
