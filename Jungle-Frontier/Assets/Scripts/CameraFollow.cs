using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // The player to follow
    public Vector3 offset = new Vector3(0f, 10f, -10f); // Position offset from player
    public float smoothSpeed = 5f; // Higher = faster follow

    [Header("Aspect Framing")]
    [Tooltip("Aspect ratio (width/height) you designed the camera offset for (e.g. 16/9).")]
    public float referenceAspect = 16f / 9f;
    [Tooltip("Maintain the same world-space framing by adjusting offset based on current aspect.")]
    public bool maintainFraming = true;

    [Header("FOV Mapping")]
    [Tooltip("Aspect ratio at which fieldOfView = fovAtMinAspect")]
    public float minAspect = 0.5f;
    [Tooltip("Aspect ratio at which fieldOfView = fovAtMaxAspect")]
    public float maxAspect = 2.16f;
    public float fovAtMinAspect = 60f;
    public float fovAtMaxAspect = 40f;

    private Camera cam;

    private Vector3 shakeOffset = Vector3.zero;
    private int lastScreenWidth;
    private int lastScreenHeight;

    // For dynamic perspective zoom
    private float basePlanarDistance;
    private Vector2 planarDir;

    void Awake()
    {
        // Compute baseline horizontal (XZ) distance and direction
        basePlanarDistance = new Vector2(offset.x, offset.z).magnitude;
        planarDir = new Vector2(offset.x, offset.z).normalized;

        // Initialize reference aspect from the actual screen size
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        referenceAspect = (float)lastScreenWidth / lastScreenHeight;

        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Determine effective offset to keep framing consistent
        Vector3 effectiveOffset = offset;
        if (maintainFraming)
        {
            float currentAspect = (float)Screen.width / Screen.height;
            // Map aspect to FOV via linear interpolation between min/max
            float t = Mathf.InverseLerp(minAspect, maxAspect, currentAspect);
            cam.fieldOfView = Mathf.Lerp(fovAtMinAspect, fovAtMaxAspect, t);
            float factor = referenceAspect / currentAspect;

            // Keep vertical offset constant, adjust horizontal distance
            effectiveOffset.y = offset.y;
            float newPlanar = basePlanarDistance * factor;
            effectiveOffset.x = planarDir.x * newPlanar;
            effectiveOffset.z = planarDir.y * newPlanar;
        }

        Vector3 desiredPosition = target.position + effectiveOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition + shakeOffset;

        // Optional: If you want camera to always look at player:
        // transform.LookAt(target);
    }

    void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            referenceAspect = (float)lastScreenWidth / lastScreenHeight;
        }
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