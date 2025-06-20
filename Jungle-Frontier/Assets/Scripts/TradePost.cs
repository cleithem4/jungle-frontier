using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
// ensure ResourceCollector is in scope

[RequireComponent(typeof(ResourceReceiver))]
public class TradePost : MonoBehaviour
{
    [Header("Drop-In Settings")]
    [Tooltip("Height above target to start the drop from")]
    public float dropHeight = 20f;
    [Tooltip("Time in seconds for the drop animation")]
    public float dropDuration = 1f;
    [Tooltip("Curve to control drop easing")]
    public AnimationCurve dropCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Collection Settings")]
    [Tooltip("Resource type this post accepts (kept for inspector)")]
    public ResourceType acceptedResource;
    [Tooltip("Collision box to detect player entry")]
    public SphereCollider collectionZone;          // set as trigger
    [Tooltip("Transform where collected resources stack")]
    public Transform collectionStackPoint;
    [Tooltip("Vertical spacing between stacked items")]
    public float stackSpacing = 0.3f;
    [Tooltip("Min/max delay between each resource fly-in")]
    public float flyDelayMin = 0.05f;
    public float flyDelayMax = 0.15f;

    [Tooltip("Fires when the player has no more of the accepted resource")]
    public UnityEvent onCollectionComplete;

    private Vector3 _dropStartPos;
    private Vector3 _dropTargetPos;
    private Renderer[] _renderers;
    private Collider[] _colliders;
    private Coroutine _collectCoroutine;
    private float _nextStackHeight = 0f;
    private ResourceReceiver receiver;
    // Cached receiver for convenience

    // Remember original ground position for drop-in
    private Vector3 _groundPosition;

    void Awake()
    {
        // Cache visuals & colliders
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);

        receiver = GetComponent<ResourceReceiver>();
        // Cache the ground position before any movement
        _groundPosition = transform.position;
        // Configure the generic receiver
        receiver.acceptedResources = new[] { acceptedResource };
        receiver.stackPoint = collectionStackPoint;
    }

    void Start()
    {
        // Hide visuals/colliders until drop-in
        foreach (var r in _renderers) r.enabled = false;
        foreach (var c in _colliders) c.enabled = false;

        // Hide collection trigger
        if (collectionZone != null)
            collectionZone.enabled = false;

        // Place in air at startup, preserving ground position
        transform.position = _groundPosition + Vector3.up * dropHeight;

        receiver.onResourceReceived.AddListener(_ =>
        {
            // Each time we get a piece, bump our drop-stack height
            _nextStackHeight += stackSpacing;
        });
    }

    /// <summary>
    /// Triggers the drop-in animation; call from your BuyZone onComplete.
    /// </summary>
    public void InitializeDropIn()
    {
        // Show the structure
        foreach (var r in _renderers) r.enabled = true;
        foreach (var c in _colliders) c.enabled = true;

        // Activate trigger area
        if (collectionZone != null)
            collectionZone.enabled = true;

        // Prepare drop positions using cached ground position
        _dropTargetPos = _groundPosition;
        _dropStartPos = _groundPosition + Vector3.up * dropHeight;
        transform.position = _dropStartPos;

        StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        float elapsed = 0f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropDuration);
            float f = dropCurve.Evaluate(t);
            transform.position = Vector3.LerpUnclamped(_dropStartPos, _dropTargetPos, f);
            yield return null;
        }
        transform.position = _dropTargetPos;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_collectCoroutine != null) return;

        // Any ResourceCollector can deposit here
        var collector = other.GetComponent<ResourceCollector>();
        if (collector != null)
            _collectCoroutine = StartCoroutine(CollectResources(collector));
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_collectCoroutine != null)
        {
            StopCoroutine(_collectCoroutine);
            _collectCoroutine = null;
        }
    }

    private IEnumerator CollectResources(ResourceCollector collector)
    {
        _nextStackHeight = 0f;
        // Keep pulling from the collector until none of the accepted type remain
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(flyDelayMin, flyDelayMax));

            // Ask the collector for the next resource
            var piece = collector.ProvideResource();
            if (piece == null)
                break;
            // Ensure it’s the right type
            var res = piece.GetComponent<Resource>();
            if (res == null || res.resourceType != acceptedResource)
            {
                // return unwanted item
                collector.Pickup(piece);
                break;
            }

            // Deposit it into this post
            var resBehavior = piece.GetComponent<ResourceBehavior>();
            if (resBehavior != null)
                resBehavior.DepositTo(gameObject);
        }

        onCollectionComplete.Invoke();
        _collectCoroutine = null;
    }

    /// <summary>
    /// Provides the top-most resource of the given type from the trade post’s stack.
    /// Returns null if none are available.
    /// </summary>
    public GameObject ProvideResource(ResourceType type)
    {
        // Simply pull from the underlying receiver (which only accepts this type)
        return receiver.ProvideResource();
    }
}
