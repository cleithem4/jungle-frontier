using UnityEngine;

/// <summary>
/// Represents any combat-capable agent (player, enemy, friendly) in the game.
/// Used for spatial queries and proximity checks.
/// </summary>
public interface Agent
{
    /// <summary>Transform of the agent for position queries.</summary>
    Transform Transform { get; }

    /// <summary>GameObject of the agent, for layer and component checks.</summary>
    GameObject GameObject { get; }
}
