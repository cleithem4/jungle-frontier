using System.Collections.Generic;
using UnityEngine;
using System.Linq; // For potential debug list printing

/// <summary>
/// Manages spatial bucketing of Agents for efficient proximity queries.
/// </summary>
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Tooltip("Size of each spatial grid cell.")]
    public float cellSize = 10f;

    // Mapping from grid coordinates to list of Agents in that cell
    private readonly Dictionary<Vector2Int, List<Agent>> grid = new Dictionary<Vector2Int, List<Agent>>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Converts world position to grid cell coordinates.
    /// </summary>
    public Vector2Int GetCellCoord(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int z = Mathf.FloorToInt(worldPos.z / cellSize);
        return new Vector2Int(x, z);
    }

    /// <summary>
    /// Registers an Agent into the grid.
    /// </summary>
    public void RegisterAgent(Agent agent)
    {
        var coord = GetCellCoord(agent.Transform.position);
        if (!grid.TryGetValue(coord, out var list))
        {
            list = new List<Agent>();
            grid[coord] = list;
        }
        if (!list.Contains(agent))
        {
            list.Add(agent);
        }
    }

    /// <summary>
    /// Unregisters an Agent from the grid.
    /// </summary>
    public void UnregisterAgent(Agent agent)
    {
        var coord = GetCellCoord(agent.Transform.position);
        if (grid.TryGetValue(coord, out var list))
        {
            list.Remove(agent);
        }
    }

    /// <summary>
    /// Queries for Agents within a given radius around the specified Agent, filtered by layer mask.
    /// </summary>
    public List<Agent> QueryNearby(Agent agent, float radius, LayerMask layerMask)
    {
        // Purge any destroyed or unloaded agents from the internal grid
        foreach (var cellList in grid.Values)
            cellList.RemoveAll(a =>
            {
                // Purge any destroyed or unloaded agents
                var obj = a as UnityEngine.Object;
                return obj == null;
            });

        var centre = GetCellCoord(agent.Transform.position);
        int range = Mathf.CeilToInt(radius / cellSize);

        var result = new List<Agent>();

        float rsq = radius * radius;
        for (int dx = -range; dx <= range; dx++)
            for (int dz = -range; dz <= range; dz++)
            {
                var cell = new Vector2Int(centre.x + dx, centre.y + dz);
                if (!grid.TryGetValue(cell, out var list)) continue;
                foreach (var other in list)
                {
                    if (other == agent) continue;
                    // Layer check
                    if ((layerMask & (1 << other.GameObject.layer)) == 0)
                        continue;
                    // Distance check
                    if ((other.Transform.position - agent.Transform.position).sqrMagnitude <= rsq)
                        result.Add(other);
                }
            }

        // Purge any destroyed or unloaded agents from the result list
        result.RemoveAll(a =>
        {
            // Purge any destroyed or unloaded agents
            var obj = a as UnityEngine.Object;
            return obj == null;
        });

        return result;
    }
}
