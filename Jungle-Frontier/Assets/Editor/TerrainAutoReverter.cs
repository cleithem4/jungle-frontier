#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

[InitializeOnLoad]
public static class TerrainAutoReverter
{
    static TerrainAutoReverter()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            foreach (var terrain in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            {
                string path = AssetDatabase.GetAssetPath(terrain.terrainData);
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
}
#endif