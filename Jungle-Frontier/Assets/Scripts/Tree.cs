using System.Collections;
using UnityEngine;
using static ResourceType;

public class Tree : MonoBehaviour
{
    public float chopTime = 2f; // seconds to chop the tree

    [Header("Respawn Settings")]
    public GameObject stumpPrefab;
    public float respawnDelay = 15f;
    public float growDuration = 1f;
    public AnimationCurve spawnScaleCurve;

    private Renderer[] _renderers;
    private Collider[] _colliders;
    private Vector3 _initialPosition;
    private Vector3 _initialScale;

    private PlayerScript player;
    public GameObject woodPiecePrefab; // assign in inspector

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _colliders = GetComponentsInChildren<Collider>();
        _initialPosition = transform.position;
        _initialScale = transform.localScale;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isChopped)
        {
            player = other.GetComponent<PlayerScript>();
            if (player != null)
            {
                player.SetNearTree(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && player != null)
        {
            player.ClearNearTree(this);
        }
    }

    private bool isChopped = false;

    public void ChopTree()
    {
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

            if (this.player != null)
            {
                float delay = Random.Range(0f, 0.3f); // random delay so they don't fly all at once

                Wood woodComponent = woodPiece.GetComponent<Wood>();
                if (woodComponent != null)
                {
                    woodComponent.Init(this.player, this.player.stackPoint, delay, ResourceType.Wood);
                }
            }
        }

        // Spawn stump and hide this tree
        Instantiate(stumpPrefab, _initialPosition + Vector3.down * 2.3f, transform.rotation);
        SetTreeActive(false);
        StartCoroutine(RespawnAndGrow());
    }

    public float GetChopTime()
    {
        return chopTime;
    }

    private void SetTreeActive(bool on)
    {
        foreach (var r in _renderers) r.enabled = on;
        foreach (var c in _colliders) c.enabled = on;
        transform.localScale = on ? _initialScale : Vector3.zero;
        isChopped = !on;
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
