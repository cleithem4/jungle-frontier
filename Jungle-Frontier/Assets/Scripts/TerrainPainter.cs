using System.Collections;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Terrain))]
public class TerrainPainter : MonoBehaviour
{
    [Header("Paint Settings")]
    [Tooltip("Which terrain layer index is grass (to fade out)")]
    public int grassLayer = 0;
    [Tooltip("Which terrain layer index is dirt (to fade in)")]
    public int dirtLayer = 1;
    [Tooltip("Radius (in world units) around the point to paint")]
    public float paintRadius = 5f;
    [Tooltip("Duration of the fade in seconds")]
    public float paintDuration = 1f;
    [Tooltip("World-space offset to apply to the paint center")]
    public Vector3 paintOffset = Vector3.zero;

    private Terrain _terrain;
    private TerrainData _terrainData;
    private int _alphaMapWidth, _alphaMapHeight;
    private float _terrainWidth, _terrainLength;

    void Awake()
    {
        _terrain = GetComponent<Terrain>();

        // Clone the TerrainData so play-mode edits are sandboxed
        _terrain.terrainData = Instantiate(_terrain.terrainData);

        _terrainData = _terrain.terrainData;
        _alphaMapWidth = _terrainData.alphamapWidth;
        _alphaMapHeight = _terrainData.alphamapHeight;
        _terrainWidth = _terrainData.size.x;
        _terrainLength = _terrainData.size.z;
    }

    /// <summary>
    /// Call this to begin painting the dirt layer around the given world position.
    /// </summary>
    public void PaintAt(Vector3 worldPos)
    {
        // apply configured offset before painting
        Vector3 offsetPos = worldPos + paintOffset;
        StartCoroutine(FadeGrassToDirt(offsetPos));
    }

    private IEnumerator FadeGrassToDirt(Vector3 worldPos)
    {
        // Convert world pos to terrain alpha-map coordinates
        Vector3 terrainPos = worldPos - _terrain.transform.position;
        int centerX = Mathf.RoundToInt((terrainPos.x / _terrainWidth) * _alphaMapWidth);
        int centerZ = Mathf.RoundToInt((terrainPos.z / _terrainLength) * _alphaMapHeight);
        int radiusAlpha = Mathf.RoundToInt((paintRadius / _terrainWidth) * _alphaMapWidth);

        // Calculate the rectangle to modify
        int x0 = Mathf.Clamp(centerX - radiusAlpha, 0, _alphaMapWidth - 1);
        int z0 = Mathf.Clamp(centerZ - radiusAlpha, 0, _alphaMapHeight - 1);
        int x1 = Mathf.Clamp(centerX + radiusAlpha, 0, _alphaMapWidth - 1);
        int z1 = Mathf.Clamp(centerZ + radiusAlpha, 0, _alphaMapHeight - 1);
        int width = x1 - x0 + 1;
        int height = z1 - z0 + 1;

        // Read the current alphamaps
        float[,,] alphaMaps = _terrainData.GetAlphamaps(x0, z0, width, height);

        // Remove detail prototypes (e.g., bushes) within paint radius
        int detailWidth = _terrainData.detailWidth;
        int detailHeight = _terrainData.detailHeight;
        int detailRadius = Mathf.RoundToInt((paintRadius / _terrainWidth) * detailWidth);
        int detailCenterX = Mathf.RoundToInt((terrainPos.x / _terrainWidth) * detailWidth);
        int detailCenterZ = Mathf.RoundToInt((terrainPos.z / _terrainLength) * detailHeight);
        int dx0 = Mathf.Clamp(detailCenterX - detailRadius, 0, detailWidth - 1);
        int dz0 = Mathf.Clamp(detailCenterZ - detailRadius, 0, detailHeight - 1);
        int dWidth = Mathf.Clamp(detailCenterX + detailRadius, 0, detailWidth - 1) - dx0 + 1;
        int dHeight = Mathf.Clamp(detailCenterZ + detailRadius, 0, detailHeight - 1) - dz0 + 1;
        for (int layer = 0; layer < _terrainData.detailPrototypes.Length; layer++)
        {
            int[,] detailZeros = new int[dHeight, dWidth];
            _terrainData.SetDetailLayer(dx0, dz0, layer, detailZeros);
        }

        // Remove tree instances within paint radius
        List<TreeInstance> trees = new List<TreeInstance>(_terrainData.treeInstances);
        for (int i = trees.Count - 1; i >= 0; i--)
        {
            TreeInstance tree = trees[i];
            Vector3 treeWorldPos = new Vector3(
                tree.position.x * _terrainData.size.x,
                0f,
                tree.position.z * _terrainData.size.z
            ) + _terrain.transform.position;
            if ((treeWorldPos - worldPos).sqrMagnitude <= paintRadius * paintRadius)
                trees.RemoveAt(i);
        }
        _terrainData.treeInstances = trees.ToArray();

        float elapsed = 0f;
        while (elapsed < paintDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / paintDuration);

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Only paint within circular radius
                    float dx = (x + x0 - centerX) / (float)radiusAlpha;
                    float dz = (z + z0 - centerZ) / (float)radiusAlpha;
                    if (dx * dx + dz * dz > 1f) continue;

                    // Crossfade grass → dirt
                    alphaMaps[z, x, grassLayer] = Mathf.Lerp(1f, 0f, t);
                    alphaMaps[z, x, dirtLayer] = Mathf.Lerp(0f, 1f, t);

                    // Re-normalize all layers so they sum to 1
                    float sum = 0f;
                    int layers = _terrainData.alphamapLayers;
                    for (int L = 0; L < layers; L++)
                        sum += alphaMaps[z, x, L];
                    for (int L = 0; L < layers; L++)
                        alphaMaps[z, x, L] /= sum;
                }
            }

            // Write back the updated block
            _terrainData.SetAlphamaps(x0, z0, alphaMaps);
            yield return null;
        }
    }
}
