using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Spawns worker monkey prefabs when a BuyZone is filled.
/// </summary>
public class WorkerSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The worker monkey prefab to spawn.")]
    public GameObject workerPrefab;

    [Tooltip("Reference to the BuyZone whose filled event triggers a spawn.")]
    public BuyZone buyZone;

    [Tooltip("The stockpile receiver where spawned workers deposit wood.")]
    public ResourceReceiver resourceStackReceiver;


    /// <summary>
    /// Spawns the given worker prefab at the BuyZone's location.
    /// </summary>
    public void SpawnWorker(GameObject prefabToSpawn)
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"[{nameof(WorkerSpawner)}] Prefab passed to SpawnWorker is null on {gameObject.name}");
            return;
        }
        var instance = Instantiate(prefabToSpawn, buyZone.transform.position, buyZone.transform.rotation);

        // If the spawned monkey has a MonkeyChopper component, set its wood stockpile
        var chopper = instance.GetComponent<MonkeyChopper>();
        if (chopper != null && resourceStackReceiver != null)
        {
            chopper.woodStockpileReceiver = resourceStackReceiver;
        }

        // If the spawned monkey has an AttackMonkey component, set its stone pile receiver
        var attacker = instance.GetComponent<AttackMonkey>();
        if (attacker != null && resourceStackReceiver != null)
        {
            attacker.lootReceiver = resourceStackReceiver;
        }

    }


}
