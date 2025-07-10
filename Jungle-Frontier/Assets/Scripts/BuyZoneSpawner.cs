using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Spawns BuyZone prefabs at requested world positions.
/// </summary>
public class BuyZoneSpawner : MonoBehaviour
{
    [Header("Buy Zone Settings")]
    [Tooltip("Prefab for the BuyZone to spawn.")]
    public GameObject buyZonePrefab;

    [Header("Spawn Settings")]
    [Tooltip("If assigned, the BuyZone will spawn at this Transform's position.")]
    public Transform spawnTarget;

    /// <summary>
    /// Spawns a new BuyZone instance at the given world position.
    /// </summary>
    /// <param name="position">The world-space position to place the BuyZone.</param>
    /// <returns>The spawned BuyZone GameObject, or null if prefab is not set.</returns>
    public GameObject SpawnBuyZone(Vector3 position)
    {
        if (buyZonePrefab == null)
        {
            Debug.LogError("[BuyZoneSpawner] buyZonePrefab is not assigned!", this);
            return null;
        }

        GameObject instance = Instantiate(buyZonePrefab, position, Quaternion.identity);
        return instance;
    }

    /// <summary>
    /// Spawns a new BuyZone instance at the configured spawnTarget's position or zero if none assigned.
    /// </summary>
    public GameObject SpawnBuyZone()
    {
        Vector3 position = spawnTarget != null
            ? spawnTarget.position
            : Vector3.zero; // fallback if no target assigned
        return SpawnBuyZone(position);
    }

    /// <summary>
    /// UnityEvent to allow inspector-based callbacks when spawning is requested.
    /// </summary>
    public UnityEvent onSpawnRequested;

    /// <summary>
    /// C# Action to allow code-based callbacks when spawning is requested.
    /// </summary>
    public System.Action SpawnAction;

    private void Awake()
    {
        // Assign a lambda to match the Action signature (discarding the returned GameObject)
        SpawnAction = () => { SpawnBuyZone(); };
    }

    /// <summary>
    /// Requests a spawn via both the UnityEvent and the C# Action.
    /// </summary>
    public void RequestSpawn()
    {
        // Invoke any inspector-set listeners
        onSpawnRequested?.Invoke();
        // Invoke any code-based listeners or direct spawn
        SpawnAction?.Invoke();
    }


    /// <summary>
    /// UnityEvent-compatible wrapper to spawn a BuyZone at the supplied position.
    /// This method will appear under OnBuyComplete(Vector3) in the Inspector.
    /// </summary>
    public void SpawnBuyZoneAt(Vector3 position)
    {
        SpawnBuyZone(position);
    }
}