using System.Collections;
using UnityEngine;

public class TreeShaker : MonoBehaviour
{
    [Tooltip("Duration of the shake in seconds.")]
    public float duration = 0.2f;
    [Tooltip("Magnitude of the shake motion.")]
    public float magnitude = 0.1f;

    private Vector3 _originalPos;

    void Awake()
    {
        // Cache the starting local position
        _originalPos = transform.localPosition;
    }

    /// <summary>
    /// Public method to trigger the shake coroutine.
    /// </summary>
    public void Shake()
    {
        StopAllCoroutines();
        StartCoroutine(DoShake());
    }

    /// <summary>
    /// Smoothly shakes the transform around its original position.
    /// </summary>
    private IEnumerator DoShake()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = _originalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Reset to original position
        transform.localPosition = _originalPos;
    }
}
