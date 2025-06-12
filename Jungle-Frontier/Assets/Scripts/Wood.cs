using UnityEngine;
using System.Collections;

public class Wood : MonoBehaviour
{
    private PlayerScript playerScript;
    private Transform targetStackPoint;
    private float stackOffsetY;
    private float flyDuration = 0.5f;
    private float delayBeforeFly = 0f;

    public void Init(PlayerScript player, Transform targetPoint, float delay)
    {
        playerScript = player;
        targetStackPoint = targetPoint;
        delayBeforeFly = delay;

        StartCoroutine(PrepareAndFly());
    }

    private IEnumerator PrepareAndFly()
    {
        yield return new WaitForSeconds(delayBeforeFly);

        // Now compute target offset Y at this moment
        stackOffsetY = playerScript.GetNextWoodStackDepth();

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
        yield return StartCoroutine(FlyToPlayer());
    }

    private IEnumerator FlyToPlayer()
    {
        Vector3 targetPos = targetStackPoint.position + Vector3.up * stackOffsetY;
        Quaternion targetRot = targetStackPoint.rotation;

        Debug.Log($"[Wood] Starting FlyToPlayer to position: {targetPos}");

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

        // Final snap and stack
        transform.SetParent(targetStackPoint);
        transform.localPosition = new Vector3(0, stackOffsetY, 0);
        Debug.Log($"[Wood] Arrived at stack position Y: {stackOffsetY}");
        transform.localRotation = Quaternion.identity;
    }
}
