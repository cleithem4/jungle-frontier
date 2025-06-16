using UnityEngine;
using System.Collections;

public enum WoodState
{
    FlyingToBack,
    OnBack,
    FlyingToZone
}

public class Wood : MonoBehaviour
{
    private PlayerScript playerScript;
    private Transform targetStackPoint;
    private float stackOffsetY;
    private float flyDuration = 0.5f;
    private float delayBeforeFly = 0f;
    private ResourceType resourceType;
    private WoodState state = WoodState.FlyingToBack;
    private Vector3 sellTargetPos;
    private System.Action sellOnArrive;

    public WoodState CurrentState => state;

    public void Init(PlayerScript player, Transform targetPoint, float delay, ResourceType resource)
    {
        playerScript = player;
        targetStackPoint = targetPoint;
        delayBeforeFly = delay;
        resourceType = resource;
        state = WoodState.FlyingToBack;

        StartCoroutine(PrepareAndFly());
    }

    private IEnumerator PrepareAndFly()
    {
        yield return new WaitForSeconds(delayBeforeFly);

        // Now compute target offset Y at this moment
        stackOffsetY = playerScript.GetNextStackDepth(resourceType);

        Debug.Log($"[Wood] Preparing to fly. StackOffsetY: {stackOffsetY}, Delay: {delayBeforeFly}");

        // Stop physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Now start moving toward player
        Vector3 targetPos = targetStackPoint.position + Vector3.up * stackOffsetY;
        Quaternion targetRot = targetStackPoint.rotation;

        yield return StartCoroutine(FlyTo(targetPos, targetRot, () =>
        {
            // Final snap and stack
            transform.SetParent(targetStackPoint);
            transform.localPosition = new Vector3(0, stackOffsetY, 0);
            Debug.Log($"[Wood] Arrived at stack position Y: {stackOffsetY}");
            transform.localRotation = Quaternion.identity;
            state = WoodState.OnBack;
            playerScript.AddResource(resourceType);
            // Register this piece for visual stacking
            playerScript.RegisterBackedWood(this);
        }));
    }

    public void FlyToTargetPoint(Vector3 destination, System.Action onComplete = null)
    {
        // Stop physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        StartCoroutine(FlyTo(destination, Quaternion.identity, onComplete));
    }

    private IEnumerator FlyTo(Vector3 targetPos, Quaternion targetRot, System.Action onComplete)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float elapsed = 0f;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flyDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        onComplete?.Invoke();
    }

    public void StartSellTo(BuyZone zone)
    {
        if (state != WoodState.OnBack) return;
        state = WoodState.FlyingToZone;
        sellTargetPos = zone.transform.position;
        sellOnArrive = () => zone.ReceiveResource();
        StartCoroutine(FlyTo(sellTargetPos, Quaternion.identity, () =>
        {
            sellOnArrive?.Invoke();
            // Remove from player inventory and backed list
            if (playerScript != null)
            {
                playerScript.RemoveResource(resourceType);
                playerScript.RemoveBackedWood(this);
            }
            Destroy(gameObject);
        }));
    }
}
