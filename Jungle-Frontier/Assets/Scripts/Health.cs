using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Generic health component that can be added to any character (player, worker, enemy, etc.).
/// Provides damage, healing, and death events.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health value.")]
    public float maxHealth = 100f;

    /// <summary>Current health, clamped between 0 and maxHealth.</summary>
    public float CurrentHealth { get; private set; }

    [Header("Events")]
    [Tooltip("Invoked after health changes: (currentHealth, maxHealth)")]
    public UnityEvent<float, float> onHealthChanged;

    [Tooltip("Invoked when health reaches zero.")]
    public UnityEvent onDeath;

    [Header("Regeneration")]
    [Tooltip("Time in seconds without damage before regeneration starts.")]
    public float regenerationDelay = 10f;
    [Tooltip("Time in seconds between each regeneration tick.")]
    public float regenerationInterval = 1f;
    [Tooltip("Fraction of max health restored each tick (e.g., 0.2 for 1/5 per second).")]
    public float regenerationFraction = 0.2f;
    private float timeSinceLastDamage = 0f;
    private float regenTimer = 0f;

    void Awake()
    {
        CurrentHealth = maxHealth;
        onHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    [Header("Health Bar")]
    [Tooltip("Drag the HealthBar prefab here.")]
    public GameObject healthBarPrefab;
    [Tooltip("Local offset from the entity for the health bar.")]
    public Vector3 healthBarOffset = new Vector3(0f, 2f, 0f);

    private HealthBar _healthBar;
    private CanvasGroup _barGroup;

    void Start()
    {
        if (healthBarPrefab != null)
        {
            // Instantiate as child so it follows the entity automatically
            GameObject barGO = Instantiate(healthBarPrefab, transform);
            barGO.transform.localPosition = healthBarOffset;
            _healthBar = barGO.GetComponent<HealthBar>();
            if (_healthBar != null)
                _healthBar.Initialize(this);
            // Ensure we have a CanvasGroup to control visibility without deactivating
            _barGroup = barGO.GetComponent<CanvasGroup>();
            if (_barGroup == null)
                _barGroup = barGO.AddComponent<CanvasGroup>();
            // Hide visuals initially until first damage
            _barGroup.alpha = 0f;
            // Toggle health bar visibility on health changes
            onHealthChanged.AddListener((current, maximum) =>
            {
                if (_healthBar != null && _barGroup != null)
                    _barGroup.alpha = (current < maximum) ? 1f : 0f;
            });
        }
    }

    /// <summary>
    /// Applies damage to this object. Triggers death if health falls to zero.
    /// </summary>
    /// <param name="amount">Amount of damage to take.</param>
    public void Damage(AttackData atk)
    {
        if (atk.damage <= 0f || CurrentHealth <= 0f) return;
        // Reset regeneration timers on damage
        timeSinceLastDamage = 0f;
        regenTimer = 0f;

        CurrentHealth = Mathf.Max(CurrentHealth - atk.damage, 0f);
        onHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// Heals this object by the specified amount, without exceeding maxHealth.
    /// </summary>
    /// <param name="amount">Amount of health to restore.</param>
    public void Heal(float amount)
    {
        if (amount <= 0f || CurrentHealth <= 0f) return;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        onHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    /// <summary>
    /// Called once when health hits zero. Default behavior is to destroy the GameObject.
    /// Override this in a subclass for custom death behavior.
    /// </summary>
    protected virtual void Die()
    {
        onDeath?.Invoke();
        Destroy(gameObject);
    }

    void Update()
    {
        if (CurrentHealth < maxHealth)
        {
            timeSinceLastDamage += Time.deltaTime;
            if (timeSinceLastDamage >= regenerationDelay)
            {
                regenTimer += Time.deltaTime;
                if (regenTimer >= regenerationInterval)
                {
                    regenTimer = 0f;
                    // Regenerate health
                    float amount = maxHealth * regenerationFraction;
                    CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
                    onHealthChanged?.Invoke(CurrentHealth, maxHealth);
                    // If at full health, reset timers
                    if (CurrentHealth >= maxHealth)
                    {
                        timeSinceLastDamage = 0f;
                        regenTimer = 0f;
                    }
                }
            }
        }
    }
}