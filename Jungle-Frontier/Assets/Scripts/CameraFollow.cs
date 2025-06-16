using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // The player to follow
    public Vector3 offset = new Vector3(0f, 10f, -10f); // Position offset from player
    public float smoothSpeed = 5f; // Higher = faster follow

    private Vector3 shakeOffset = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition + shakeOffset;

        // Optional: If you want camera to always look at player:
        // transform.LookAt(target);
    }

    /// <summary>
    /// Shakes the camera for a short duration.
    /// </summary>
    public void Shake(float duration = 0.3f, float magnitude = 0.5f)
    {
        StopAllCoroutines();
        StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float damper = 1f - (elapsed / duration);
            shakeOffset = Random.insideUnitSphere * magnitude * damper;
            yield return null;
        }
        shakeOffset = Vector3.zero;
    }
}