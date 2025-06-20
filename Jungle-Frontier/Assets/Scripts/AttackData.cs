using UnityEngine;

/// <summary>
/// Encapsulates all relevant information for a single attack event.
/// </summary>
public class AttackData
{
    /// <summary>Amount of damage to apply.</summary>
    public float damage;

    /// <summary>
    /// The source of the attack (who performed it), if any.
    /// </summary>
    public object source;

    /// <summary>
    /// Optional attack category (e.g. "bite", "chop", "fire").
    /// </summary>
    public string type;


    /// <summary> Direction and strength of any knockback to apply. </summary>
    public Vector3 knockbackForce;

    /// <summary> Duration over which knockback is applied. </summary>
    public float knockbackDuration;

    /// <summary>
    /// Create a new AttackData instance.
    /// </summary>
    public AttackData(float damage, object source = null, string type = null,
                      Vector3 knockbackForce = default, float knockbackDuration = 0f)
    {
        this.damage = damage;
        this.source = source;
        this.type = type;
        this.knockbackForce = knockbackForce;
        this.knockbackDuration = knockbackDuration;
    }
}
