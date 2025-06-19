using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls an NPC that runs to the TradePost, exchanges a resource for player currency,
/// does a dance animation, then exits the scene.
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class NPCTrader : MonoBehaviour
{
    [Header("Trade Settings")]
    [Tooltip("Resource type this NPC wants to buy.")]
    public ResourceType resourceWanted;
    [Tooltip("Amount of currency awarded to the player when trading.")]
    public int currencyReward = 10;
    [Tooltip("Reference to the TradePost for resource exchange.")]
    public TradePost tradePost;
    [Tooltip("Transform where the NPC should go to trade.")]
    public Transform tradePoint;
    [Tooltip("Transform where the NPC should exit after trading.")]
    public Transform exitPoint;

    [Tooltip("Prefab of the crystal to spawn after trading.")]
    public GameObject crystalPrefab;

    [Tooltip("ResourceReceiver that will accept the spawned crystals.")]
    public ResourceReceiver crystalReceiver;

    [Header("Movement & Animation")]
    [Tooltip("Run speed of the NPC.")]
    public float runSpeed = 5f;
    [Tooltip("Name of the run animation state.")]
    public string runAnimState = "Run";
    [Tooltip("Name of the dance animation state.")]
    public string danceAnimState = "Dance";
    [Tooltip("Duration of the dance in seconds.")]
    public float danceDuration = 2f;
    [Tooltip("Name of the idle animation state.")]
    public string idleAnimState = "Idle";

    private enum State { RunningIn, Trading, Dancing, Exiting }
    private State _state;
    private NavMeshAgent _agent;
    private Animator _anim;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
    }

    void Start()
    {
        BeginTrade();
    }

    /// <summary>
    /// Kicks off the run to the TradePost.
    /// </summary>
    public void BeginTrade()
    {
        if (tradePost == null || tradePoint == null)
        {
            Destroy(gameObject);
            return;
        }

        _state = State.RunningIn;
        _agent.speed = runSpeed;
        _agent.SetDestination(tradePoint.position);
        _anim.Play(runAnimState);
    }

    void Update()
    {
        switch (_state)
        {
            case State.RunningIn:
                if (!_agent.pathPending && _agent.remainingDistance < _agent.stoppingDistance + 0.1f)
                {
                    StartCoroutine(DoTradeRoutine());
                }
                break;

            case State.Exiting:
                if (!_agent.pathPending && _agent.remainingDistance < _agent.stoppingDistance + 0.1f)
                {
                    Destroy(gameObject);
                }
                break;
        }
    }

    private IEnumerator DoTradeRoutine()
    {
        _state = State.Trading;

        GameObject resourceGO = null;
        // Wait until the desired resource is available
        while (resourceGO == null)
        {
            // Play idle animation while waiting
            _state = State.Trading;
            _anim.Play(idleAnimState);
            yield return new WaitForSeconds(1f);  // polling interval
            resourceGO = tradePost.ProvideResource(resourceWanted);
        }
        // Resource acquired: award currency
        CurrencyManager.Instance.Add(currencyReward);

        // Spawn a crystal and have it fly to the crystal receiver
        if (crystalPrefab != null && crystalReceiver != null)
        {
            GameObject crystal = Instantiate(crystalPrefab, transform.position, Quaternion.identity);
            // Ensure the crystal has a ResourceSender
            var sender = crystal.GetComponent<ResourceSender>() ?? crystal.AddComponent<ResourceSender>();
            sender.SendTo(crystal, crystalReceiver.gameObject);
        }

        // Do the dance animation
        _state = State.Dancing;
        _anim.Play(danceAnimState);
        yield return new WaitForSeconds(danceDuration);

        // Exit the scene
        _state = State.Exiting;
        if (exitPoint != null)
        {
            _agent.SetDestination(exitPoint.position);
            _anim.Play(runAnimState);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
