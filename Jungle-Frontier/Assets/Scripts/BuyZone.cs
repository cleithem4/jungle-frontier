using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class BuyZone : MonoBehaviour
{
    public Image fillImage;

    [Header("Resource Prefab")]
    public GameObject resourcePrefab; // assign the prefab for the required resource

    [Header("Action")]
    public UnityEvent onBuyComplete;

    public ResourceType requiredResource;
    public int amountNeeded = 5;
    private int currentCollected = 0;

    [Header("Player Detection")]
    public UnityEvent onPlayerEnter;
    public UnityEvent onPlayerExit;
    private bool playerInZone = false;

    private void OnTriggerEnter(Collider other)
    {
        // Detect player entering
        if (!other.CompareTag("Player")) return;
        playerInZone = true;
        onPlayerEnter?.Invoke();

        // Grab the PlayerScript to access the stackPoint
        var playerScript = other.GetComponent<PlayerScript>();
        if (playerScript == null) return;

        // Calculate how many more resources we need
        int needed = amountNeeded - currentCollected;
        if (needed <= 0) return;

        // Select the top 'needed' wood pieces by their Y offset
        var woods = playerScript.stackPoint
            .GetComponentsInChildren<Wood>()
            .Where(w => w.CurrentState == WoodState.OnBack)
            .OrderByDescending(w => w.transform.localPosition.y)
            .Take(needed)
            .ToList();

        // Start staggered selling of just those pieces
        StartCoroutine(SellAllWoodsCoroutine(woods));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            onPlayerExit?.Invoke();
        }
    }

    public void ReceiveResource()
    {
        if (currentCollected >= amountNeeded)
            return;

        currentCollected++;
        Debug.Log($"[BuyZone] Received 1 {requiredResource}. Total: {currentCollected}/{amountNeeded}");

        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01((float)currentCollected / amountNeeded);

        if (currentCollected >= amountNeeded)
        {
            onBuyComplete.Invoke();
        }
    }

    private IEnumerator SellAllWoodsCoroutine(List<Wood> woods)
    {
        float totalDuration = 3f; // total time over which to stagger all woods
        int count = woods.Count;
        if (count == 0) yield break;
        float interval = totalDuration / count;

        for (int i = 0; i < count; i++)
        {
            yield return new WaitForSeconds(interval);
            // Stop if player has exited the zone
            if (!playerInZone) yield break;
            woods[i].StartSellTo(this);
        }
    }
}