using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ForceAssetReserialization
{
    [MenuItem("Project/Force Asset Reserialization")]
    public static void ReserializeAllAssets()
    {
        string[] allPaths = AssetDatabase.GetAllAssetPaths();
        List<string> assets = new List<string>();

        for (int i = 0; i < allPaths.Length; i++)
        {
            string path = allPaths[i];

            if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                continue;

            if (AssetDatabase.IsValidFolder(path))
                continue;

            assets.Add(path);
        }

        if (assets.Count == 0)
            return;

        try
        {
            Dictionary<string, List<string>> assetsByType = new Dictionary<string, List<string>>();

            for (int i = 0; i < assets.Count; i++)
            {
                string path = assets[i];
                string extension = System.IO.Path.GetExtension(path).ToLowerInvariant();

                if (string.IsNullOrEmpty(extension))
                    extension = "<no extension>";

                if (!assetsByType.TryGetValue(extension, out List<string> typeAssets))
                {
                    typeAssets = new List<string>();
                    assetsByType.Add(extension, typeAssets);
                }

                typeAssets.Add(path);
            }

            int processed = 0;

            foreach (KeyValuePair<string, List<string>> type in assetsByType)
            {
                List<string> typeAssets = type.Value;

                for (int i = 0; i < typeAssets.Count; i++)
                {
                    string path = typeAssets[i];

                    float progress = (float)processed / assets.Count;

                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Force Asset Reserialization",
                        $"Reserializing {type.Key} ({i + 1}/{typeAssets.Count})",
                        progress))
                    {
                        return;
                    }

                    AssetDatabase.ForceReserializeAssets(
                        new[] { path },
                        ForceReserializeAssetsOptions.ReserializeAssets);

                    processed++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
}