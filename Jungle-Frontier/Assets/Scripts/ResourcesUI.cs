using UnityEngine;
using TMPro;

public class ResourcesUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TMP Text component to display current currency.")]
    public TextMeshProUGUI currencyText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Subscribe to currency updates
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.onCurrencyChanged.AddListener(UpdateCurrencyText);

        // Initialize with current value
        UpdateCurrencyText(CurrencyManager.Instance?.CurrentCurrency ?? 0);
    }

    /// <summary>
    /// Updates the TMP text to show the current currency amount.
    /// </summary>
    private void UpdateCurrencyText(int amount)
    {
        if (currencyText != null)
            currencyText.text = amount.ToString();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
