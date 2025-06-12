using UnityEngine.EventSystems;
using UnityEngine;
using System.Collections;
public class PlayerScript : MonoBehaviour
{
    public JoyStick joystick; // Reference to your joystick
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f; // degrees per second
    private Animator animator;

    public Transform woodStackPoint; // empty GameObject on player's back
    private int woodStackCount = 0;

    private Tree nearTree = null;
    private bool isChopping = false;
    private float chopTimer = 0f;

    private float woodStackDepth = 0f; // replaces woodStackCount * depth
    private float woodPieceDepth = 0.0028f; // depth of 1 wood piece

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

    public float GetNextWoodStackDepth()
    {
        Debug.Log($"[PlayerScript] Providing next wood stack depth: {woodStackDepth}");
        float currentDepth = woodStackDepth;
        woodStackDepth += woodPieceDepth;
        return currentDepth;
    }
}