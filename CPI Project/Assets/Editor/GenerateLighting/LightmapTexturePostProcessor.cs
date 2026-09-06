using UnityEditor;
using UnityEngine;

internal static class LightmapTexturePostProcessor
{
    private const int LightmapTextureSize = 1024;

    internal static void SetLightmapTextureSize()
    {
        foreach (LightmapData lightmap in LightmapSettings.lightmaps)
        {
            SetTextureSize(lightmap.lightmapColor);
            SetTextureSize(lightmap.lightmapDir);
            SetTextureSize(lightmap.shadowMask);
        }
    }

    private static void SetTextureSize(Texture texture)
    {
        if (texture == null)
        {
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null || importer.maxTextureSize == LightmapTextureSize)
        {
            return;
        }

        importer.maxTextureSize = LightmapTextureSize;
        importer.SaveAndReimport();
    }
}