using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent), typeof(ResourceReceiver))]
public abstract class WorkerBase : MonoBehaviour
{
    [Header("Worker Settings")]
    [Tooltip("Maximum number of resources this worker can hold before depositing.")]
    public int capacity = 3;

    protected NavMeshAgent agent;
    protected ResourceReceiver receiver;

    protected virtual void Awake()
    {
        receiver = GetComponent<ResourceReceiver>();
        agent = GetComponent<NavMeshAgent>();

        // Configure the receiver to match this worker's capacity
        receiver.maxCapacity = capacity;
        Debug.Log($"[WorkerBase] Awake on {gameObject.name}: capacity={capacity}");
    }

    protected virtual void Start()
    {
        Debug.Log($"[WorkerBase] Starting WorkLoop on {gameObject.name}");
        // Begin the worker's main loop
        StartCoroutine(WorkLoop());
    }

    /// <summary>
    /// Moves the agent to the given position, yielding until arrival.
    /// </summary>
    protected IEnumerator MoveTo(Vector3 destination, float stoppingDistance = 0.1f)
    {
        agent.isStopped = false;
        Debug.Log($"[WorkerBase] MoveTo: {gameObject.name} -> {destination} (stoppingDistance={stoppingDistance})");
        agent.SetDestination(destination);
        // Wait until path is computed
        while (agent.pathPending)
            yield return null;
        // Travel until within stopping distance
        while (agent.remainingDistance > stoppingDistance)
            yield return null;
        agent.isStopped = true;
        Debug.Log($"[WorkerBase] Arrived: {gameObject.name} at {destination}");
    }

    /// <summary>
    /// Moves the agent dynamically towards a changing target position,
    /// updating each frame until within stoppingDistance.
    /// </summary>
    protected IEnumerator MoveToDynamic(Func<Vector3> getDestination, float stoppingDistance = 1f)
    {
        agent.isStopped = false;
        // Continuously update destination until we arrive
        while (Vector3.Distance(transform.position, getDestination()) > stoppingDistance)
        {
            Vector3 dest = getDestination();
            agent.SetDestination(dest);
            // Wait until path is computed (if needed)
            while (agent.pathPending)
                yield return null;
            // Step one frame
            yield return null;
        }
        agent.isStopped = true;
    }

    /// <summary>
    /// Defines the repeating task cycle for the worker.
    /// Subclasses must implement this coroutine.
    /// </summary>
    protected abstract IEnumerator WorkLoop();
}
