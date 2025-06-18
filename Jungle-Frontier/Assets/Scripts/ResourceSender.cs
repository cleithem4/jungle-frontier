
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles flying a resource GameObject to a receiver and delivering it.
/// </summary>
public class ResourceSender : MonoBehaviour
{
    [Tooltip("Duration of the flight animation in seconds.")]
    public float flightDuration = 1f;

    [Tooltip("Curve to control the flight easing.")]
    public AnimationCurve flightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    /// <summary>
    /// Begins the send process: animates the resource from its current position
    /// to the receiver's stackPoint, then calls ReceiveResource on the receiver.
    /// </summary>
    /// <param name="resourceGO">The resource GameObject to send.</param>
    /// <param name="receiverGO">The GameObject with a ResourceReceiver component.</param>
    /// <param name="onComplete">Optional callback invoked after delivery.</param>
    public void SendTo(GameObject resourceGO, GameObject receiverGO, Action onComplete = null)
    {
        if (resourceGO == null || receiverGO == null)
        {
            Debug.LogWarning("ResourceSender.SendTo called with null args.");
            return;
        }
        StartCoroutine(SendRoutine(resourceGO, receiverGO, onComplete));
    }

    private IEnumerator SendRoutine(GameObject resourceGO, GameObject receiverGO, Action onComplete)
    {
        var receiver = receiverGO.GetComponent<ResourceReceiver>();
        if (receiver == null)
        {
            Debug.LogWarning($"ResourceSender: No ResourceReceiver found on {receiverGO.name}");
            yield break;
        }

        Vector3 startPos = resourceGO.transform.position;
        Vector3 endPos = receiver.stackPoint.position;
        float elapsed = 0f;

        // Animate the flight
        while (elapsed < flightDuration)
        {
            float t = flightCurve.Evaluate(elapsed / flightDuration);
            resourceGO.transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        resourceGO.transform.position = endPos;

        // Deliver to receiver
        receiver.ReceiveResource(resourceGO);

        // Invoke completion callback
        onComplete?.Invoke();
    }
}
