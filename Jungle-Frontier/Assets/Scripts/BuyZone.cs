using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; // Important for UnityEvent

public class BuyZone : MonoBehaviour
{
    public float fillTime = 2f;
    private float currentFill = 0f;
    private bool playerInZone = false;

    public Image fillImage;

    [Header("Action")]
    public UnityEvent onBuyComplete; // This is what makes it reusable!

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            Debug.Log("player in buy zone");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
        }
    }

    private void Update()
    {
        if (playerInZone)
        {
            currentFill += Time.deltaTime;
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(currentFill / fillTime);

            if (currentFill >= fillTime)
            {
                onBuyComplete.Invoke(); // Call whatever action is assigned!
                currentFill = 0f;
                playerInZone = false;
            }
        }
        else
        {
            // Optional: reset or deplete fill
            currentFill = Mathf.Max(0f, currentFill - Time.deltaTime * 2f);
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(currentFill / fillTime);
        }
    }
}