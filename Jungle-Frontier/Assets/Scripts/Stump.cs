using UnityEngine;

public class Stump : MonoBehaviour
{
    [Tooltip("Time in seconds before the stump is removed and the tree respawns.")]
    public float lifespan = 15f;

    void Start()
    {
        // Destroy this stump after the specified lifespan
        Destroy(gameObject, lifespan);
    }
}
