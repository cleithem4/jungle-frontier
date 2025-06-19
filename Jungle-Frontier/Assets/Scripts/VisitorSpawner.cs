using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns visitor NPCs at random intervals and positions.
/// Each visitor prefab must have an NPCTrader component configured in the Inspector.
/// </summary>
public class VisitorSpawner : MonoBehaviour
{
    [Header("Visitor Prefabs")]
    [Tooltip("List of visitor NPC prefabs (must have NPCTrader component).")]
    public GameObject[] visitorPrefabs;

    [Header("Spawn Points")]
    [Tooltip("Transforms representing spawn locations for visitors.")]
    public Transform[] spawnPoints;

    [Header("Spawn Settings")]
    [Tooltip("Time in seconds between spawn attempts.")]
    public float spawnInterval = 5f;
    [Tooltip("Maximum number of visitors active at once.")]
    public int maxActiveVisitors = 5;

    [Header("References")]
    [Tooltip("TradePost instance that NPCs will use for trading.")]
    public TradePost tradePost;

    [Tooltip("Transform where NPCs go to trade.")]
    public Transform tradePoint;

    [Tooltip("Transform where NPCs go after trading.")]
    public Transform exitPoint;

    [Header("Crystal Reward")]
    [Tooltip("Prefab of the crystal to spawn after trading.")]
    public GameObject crystalPrefab;

    [Tooltip("ResourceReceiver that will accept the spawned crystals.")]
    public ResourceReceiver crystalReceiver;

    [Header("Control")]
    [Tooltip("When true, will spawn visitors on interval.")]
    public bool allowSpawning = false;

    private int _activeCount = 0;

    void Start()
    {
        if (visitorPrefabs == null || visitorPrefabs.Length == 0)
            Debug.LogWarning("[VisitorSpawner] No visitorPrefabs assigned.");
        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogWarning("[VisitorSpawner] No spawnPoints assigned.");

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            if (allowSpawning &&
                _activeCount < maxActiveVisitors &&
                visitorPrefabs.Length > 0 &&
                spawnPoints.Length > 0)
            {
                SpawnVisitor();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnVisitor()
    {
        // Choose a random prefab and spawn point
        var prefab = visitorPrefabs[Random.Range(0, visitorPrefabs.Length)];
        var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Instantiate the visitor
        var instance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        var trader = instance.GetComponent<NPCTrader>();
        if (trader != null)
        {
            // Configure the NPCTrader with the TradePost and points
            trader.tradePost = tradePost;
            trader.tradePoint = tradePoint;
            trader.exitPoint = exitPoint;

            // Configure the NPC's crystal reward
            trader.crystalPrefab = crystalPrefab;
            trader.crystalReceiver = crystalReceiver;

            // Hook into the NPC's destruction to track active count
            _activeCount++;
            // Start the NPC's behavior
            trader.BeginTrade();
            // When the NPC destroys itself on exit, decrement count
            StartCoroutine(TrackVisitorLifecycle(trader.gameObject));
        }
        else
        {
            Debug.LogWarning("[VisitorSpawner] Spawned prefab missing NPCTrader: " + prefab.name);
        }
    }

    private IEnumerator TrackVisitorLifecycle(GameObject visitor)
    {
        // Wait until the visitor GameObject is destroyed
        while (visitor != null)
            yield return null;
        _activeCount = Mathf.Max(0, _activeCount - 1);
    }

    /// <summary>
    /// Enables visitor spawning.
    /// </summary>
    public void EnableSpawning()
    {
        allowSpawning = true;
    }

    /// <summary>
    /// Disables visitor spawning.
    /// </summary>
    public void DisableSpawning()
    {
        allowSpawning = false;
    }

    /// <summary>
    /// Toggles the spawning state on or off.
    /// </summary>
    public void ToggleSpawning()
    {
        allowSpawning = !allowSpawning;
    }
}
