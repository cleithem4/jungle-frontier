using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class Snake : EnemyBase
{
    [Header("Detection Settings")]
    [Tooltip("Distance at which the snake will detect and chase the player.")]
    public float detectRadius = 8f;


    private Animator animator;
    private Transform playerTransform;



    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
    }

    protected override void Update()
    {

        // Play patrol animation
        animator.CrossFade("Slither", 0.1f, 0);

        // Perform base movement/attack logic
        base.Update();

        // Play Idle if not moving or attacking
        if (agent.velocity.magnitude < 0.1f && target == null)
        {
            animator.CrossFade("Idle", 0.1f, 0);
        }
    }

    protected override void Attack()
    {
        base.Attack();
        animator.CrossFade("Bite", 0.1f, 0);
    }
}
