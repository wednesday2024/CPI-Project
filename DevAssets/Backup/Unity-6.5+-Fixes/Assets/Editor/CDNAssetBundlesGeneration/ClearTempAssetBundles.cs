using UnityEditor;
using UnityEngine;

public class ClearTempAssetBundles : MonoBehaviour
{
    [MenuItem("Project/AssetBundles/Generated/Clear decompiled CDN temp names")]
    private static void ClearTempAssetBundlesNames()
    {
        string[] assetPaths = AssetDatabase.GetAllAssetPaths();

        foreach (string assetPath in assetPaths)
        {
            AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);

            if (assetImporter != null && assetImporter.assetBundleName.ToLower().StartsWith("temp_"))
            {
                assetImporter.assetBundleName = string.Empty;
                Debug.Log($"Cleared AssetBundle name for: {assetPath}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Completed clearing temp_ AssetBundle names.");
    }
}