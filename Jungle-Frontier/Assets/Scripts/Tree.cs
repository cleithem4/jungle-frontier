using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ResourceType;


public class Tree : MonoBehaviour
{


    [Header("Chop Health")]
    public float maxHealth = 5f;
    private float currentHealth;

    [Header("Chop Settings")]
    [Tooltip("Position where workers should stand to chop this tree.")]
    public Transform chopPoint;

    [Header("Respawn Settings")]
    public GameObject stumpPrefab;
    public float respawnDelay = 15f;
    public float growDuration = 1f;
    public AnimationCurve spawnScaleCurve;

    private Renderer[] _renderers;
    private Collider[] _colliders;
    private Vector3 _initialPosition;
    private Vector3 _initialScale;

    // Tracks whoever last chopped this tree
    private ResourceCollector lastHarvester;
    public GameObject woodPiecePrefab; // assign in inspector

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _colliders = GetComponentsInChildren<Collider>();
        _initialPosition = transform.position;
        _initialScale = transform.localScale;

        // Initialize health
        currentHealth = maxHealth;

        // Ensure there is always a valid chop point
        if (chopPoint == null)
            chopPoint = this.transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Tree] OnTriggerEnter with {other.gameObject.name}. IsChopped: {isChopped}");
        if (!isChopped)
        {
            var collector = other.GetComponent<ResourceCollector>();
            if (collector != null)
            {
                lastHarvester = collector;
                // Optionally notify collector if needed
            }
        }
    }

    public bool isChopped = false;

    public void ChopTree()
    {
        Debug.Log($"[Tree] ChopTree() invoked. LastHarvester: {lastHarvester}");
        isChopped = true;
        Debug.Log("Tree chopped!");

        // Spawn 5 wood pieces with random force
        for (int i = 0; i < 5; i++)
        {
            Quaternion spawnRotation = Quaternion.Euler(90f, 90f, 0f);
            GameObject woodPiece = Instantiate(woodPiecePrefab, transform.position + Vector3.up * 1f, spawnRotation);

            Rigidbody rb = woodPiece.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 randomForce = new Vector3(Random.Range(-2f, 2f), Random.Range(2f, 5f), Random.Range(-2f, 2f));
                rb.AddForce(randomForce, ForceMode.Impulse);
            }

            if (lastHarvester != null)
            {
                float delay = Random.Range(0f, 0.3f); // randomize so pieces don't all fly at once
                var resBehavior = woodPiece.GetComponent<ResourceBehavior>();
                if (resBehavior != null)
                    resBehavior.Pickup(lastHarvester, delay);
            }
        }

        // Spawn stump and hide this tree
        Instantiate(stumpPrefab, _initialPosition + Vector3.down * 2.3f, transform.rotation);
        SetTreeActive(false);
        StartCoroutine(RespawnAndGrow());
    }

    /// <summary>
    /// Deal damage to the tree; shakes on hit and chops when health depletes.
    /// </summary>
    public void Damage(float amount, ResourceCollector harvester)
    {
        // Record exactly who dealt the last hit
        lastHarvester = harvester;

        Debug.Log($"[Tree] Damage called. Amount: {amount}, CurrentHealth before: {currentHealth}");
        if (isChopped) return;

        // Reduce health and trigger shake if available
        currentHealth -= amount;
        Debug.Log($"[Tree] CurrentHealth after: {currentHealth}");
        var shaker = GetComponent<TreeShaker>();
        if (shaker != null)
            shaker.Shake();

        // When health is gone, chop the tree
        if (currentHealth <= 0f)
            ChopTree();
    }


    private void SetTreeActive(bool on)
    {
        Debug.Log($"[Tree] SetTreeActive({on}) called.");
        foreach (var r in _renderers) r.enabled = on;
        foreach (var c in _colliders) c.enabled = on;
        transform.localScale = on ? _initialScale : Vector3.zero;
        isChopped = !on;
        if (on)
            currentHealth = maxHealth;
    }

    private IEnumerator RespawnAndGrow()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Re-enable tree at zero scale
        SetTreeActive(true);

        // Animate scale-up
        float t = 0f;
        while (t < 1f)
        {
            float s = spawnScaleCurve.Evaluate(t);
            transform.localScale = _initialScale * s;
            t += Time.deltaTime / growDuration;
            yield return null;
        }
        transform.localScale = _initialScale;
    }
}
