using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns snake prefabs at random positions within a specified radius around this GameObject.
/// Maintains a maximum number of active snakes at any time.
/// </summary>
public class SnakeSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefab of the snake to spawn.")]
    public GameObject snakePrefab;

    [Tooltip("Radius around the spawner within which snakes will appear.")]
    public float spawnRadius = 5f;

    [Tooltip("Maximum number of snakes allowed at once.")]
    public int maxSnakes = 6;

    [Tooltip("Time interval (in seconds) between spawn attempts.")]
    public float spawnInterval = 3f;

    [Header("Spawning Control")]
    [Tooltip("Enable or disable snake spawning.")]
    public bool spawningEnabled = true;

    private List<GameObject> spawnedSnakes = new List<GameObject>();
    private float spawnTimer = 0f;

    void Start()
    {
        // Allow immediate spawn on start
        spawnTimer = spawnInterval;
        DisableSpawning();
    }

    void Update()
    {
        // Remove any destroyed snakes from the list
        spawnedSnakes.RemoveAll(snake => snake == null);

        // If spawning is disabled, skip spawn logic
        if (!spawningEnabled)
            return;

        // Accumulate time
        spawnTimer += Time.deltaTime;

        // If it's time and we're below capacity, spawn a new snake
        if (spawnTimer >= spawnInterval && spawnedSnakes.Count < maxSnakes)
        {
            SpawnSnake();
            spawnTimer = 0f;
        }
    }

    /// <summary>
    /// Instantiate a snake at a random point within the spawn radius.
    /// </summary>
    private void SpawnSnake()
    {
        if (snakePrefab == null)
        {
            Debug.LogWarning("SnakeSpawner: No snakePrefab assigned.");
            return;
        }

        // Pick a random point in a circle on the XZ plane
        Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomPoint.x, 0f, randomPoint.y);

        GameObject snake = Instantiate(snakePrefab, spawnPosition, Quaternion.identity);
        spawnedSnakes.Add(snake);
    }

    /// <summary>Enable snake spawning.</summary>
    public void EnableSpawning() => spawningEnabled = true;

    /// <summary>Disable snake spawning.</summary>
    public void DisableSpawning() => spawningEnabled = false;
}
