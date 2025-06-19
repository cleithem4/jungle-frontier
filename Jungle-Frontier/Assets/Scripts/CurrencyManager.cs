using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the player's currency. Provides methods to add and spend currency,
/// and broadcasts changes via an event.
/// </summary>
[DisallowMultipleComponent]
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Initial Settings")]
    [Tooltip("Starting amount of currency when the game begins.")]
    public int startingCurrency = 0;

    [Header("Events")]
    [Tooltip("Invoked whenever the currency total changes. Provides new total.")]
    public UnityEvent<int> onCurrencyChanged;

    private int _currentCurrency;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize currency
        _currentCurrency = startingCurrency;
        onCurrencyChanged?.Invoke(_currentCurrency);
    }

    /// <summary>
    /// Gets the current amount of currency.
    /// </summary>
    public int CurrentCurrency => _currentCurrency;

    /// <summary>
    /// Adds the specified amount to the currency total.
    /// </summary>
    public void Add(int amount)
    {
        if (amount == 0) return;
        _currentCurrency += amount;
        onCurrencyChanged?.Invoke(_currentCurrency);
    }

    /// <summary>
    /// Attempts to spend the specified amount. Returns true on success.
    /// </summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (_currentCurrency < amount) return false;
        _currentCurrency -= amount;
        onCurrencyChanged?.Invoke(_currentCurrency);
        return true;
    }
}
