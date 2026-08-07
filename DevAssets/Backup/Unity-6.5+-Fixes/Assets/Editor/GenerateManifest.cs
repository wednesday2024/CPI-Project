using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;

public class GenerateManifest
{
    public class ManifestData
    {
        public List<string> assets { get; set; }
    }

    private static readonly string[] localizedResourceLanguageSuffixes = new[]
    {
        "_en_us",
        "_pt_br",
        "_fr_fr",
        "_es_la"
    };

    private static readonly HashSet<string> localizedResourceGenericKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "disneyshop/images/disneyshop_bighero6_logo",
        "disneyshop/images/disneyshop_cars3_logo",
        "disneyshop/images/disneyshop_descendants2_logo",
        "disneyshop/images/disneyshop_ducktales_logo",
        "disneyshop/images/disneyshop_elena_logo",
        "disneyshop/images/disneyshop_findingdory_logo",
        "disneyshop/images/disneyshop_frozen_logo",
        "disneyshop/images/disneyshop_monstersinc_logo",
        "disneyshop/images/disneyshop_olaf_logo",
        "disneyshop/images/disneyshop_tangled_logo",
        "disneyshop/images/disneyshop_toystory_logo",
        "disneyshop/images/disneyshop_zootopia_logo",
        "images/sharedassets_cpilogo",
        "images/homescreen_cpilogo",
        "images/logincreate_disneylogo",
        "images/logincreate_disneylogo_gold",
        "textures/bighero6",
        "textures/disneystoresigndescendantscotillion",
        "textures/disneystoresignolafsfrozenadventure",
        "textures/ducktalessign"
    };

    // Bundle dependencies, quests uses these for loading stuff like images and such.
    private static readonly Dictionary<string, string> bundleDependenciesTemplate = new Dictionary<string, string>
    {
        { "assetbundles/generated/PLATFORM/quest.sa.unity3d", "assetbundles/generated/PLATFORM/mascot.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/questmascotitem_rookie.sa.unity3d", "assetbundles/generated/PLATFORM/questmascotitem_rockhopper.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/tasks.sa.unity3d", "assetbundles/generated/PLATFORM/mascot.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/auntarctic.sa.unity3d", "assetbundles/generated/PLATFORM/quest/auntarctic/c02q04.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/auntarctic/c01q01.sa.unity3d", "assetbundles/generated/PLATFORM/mascot.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/auntarctic/c01q02.sa.unity3d", "assetbundles/generated/PLATFORM/mascot.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/auntarctic/c01q04.sa.unity3d", "assetbundles/generated/PLATFORM/mascot.sa.unity3d,assetbundles/generated/PLATFORM/quest/rockhopper/c01q01.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/auntarctic/c02q03.sa.unity3d", "assetbundles/generated/PLATFORM/quest/rockhopper/c01q01.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/auntarctic/c02q04.sa.unity3d", "assetbundles/generated/PLATFORM/mascot.sa.unity3d,assetbundles/generated/PLATFORM/quest.sa.unity3d,assetbundles/generated/PLATFORM/quest/auntarctic.sa.unity3d,assetbundles/generated/PLATFORM/quest/auntarctic/c02q02.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/auntarctic/c02q06.sa.unity3d", "assetbundles/generated/PLATFORM/mascot.sa.unity3d,assetbundles/generated/PLATFORM/quest/auntarctic/c02q02.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/auntarctic/c02q07.sa.unity3d", "assetbundles/generated/PLATFORM/quest.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/auntarctic/c02q08.sa.unity3d", "assetbundles/generated/PLATFORM/quest/auntarctic/c02q01.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/auntarctic/c02q10.sa.unity3d", "assetbundles/generated/PLATFORM/quest/auntarctic/c02q02.sa.unity3d,assetbundles/generated/PLATFORM/quest/auntarctic/c02q04.sa.unity3d,assetbundles/generated/PLATFORM/quest/auntarctic/c02q05.sa.unity3d,assetbundles/generated/PLATFORM/quest/auntarctic/c02q06.sa.unity3d,assetbundles/generated/PLATFORM/quest/auntarctic/c02q07.sa.unity3d,assetbundles/generated/PLATFORM/quest/rockhopper/c01q01.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/djcadence/c01q01.sa.unity3d", "assetbundles/generated/PLATFORM/quest/djcadence/c01q05.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/djcadence/c01q02.sa.unity3d", "assetbundles/generated/PLATFORM/mascot.sa.unity3d,assetbundles/generated/PLATFORM/quest/djcadence/c01q03.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/djcadence/c01q05.sa.unity3d", "assetbundles/generated/PLATFORM/igloo.sa.unity3d,assetbundles/generated/PLATFORM/mascot.sa.unity3d,assetbundles/generated/PLATFORM/quest/rockhopper/c01q01.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/rockhopper/c01q03.sa.unity3d", "assetbundles/generated/PLATFORM/mascot.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/rockhopper/c01q06.sa.unity3d", "assetbundles/generated/PLATFORM/quest/rockhopper/c01q04.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/rockhopper/c01q08.sa.unity3d", "assetbundles/generated/PLATFORM/quest/rockhopper/c01q07.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/rookie/c01q01.sa.unity3d", "assetbundles/generated/PLATFORM/quest/rockhopper/c01q01.sa.unity3d,assetbundles/generated/PLATFORM/quest/rookie/c01q05.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/rookie/c01q05.sa.unity3d", "assetbundles/generated/PLATFORM/quest/auntarctic/c02q02.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/rookie/c02q03.sa.unity3d", "assetbundles/generated/PLATFORM/quest/rockhopper/c01q01.sa.unity3d" },
        { "assetbundles/generated/PLATFORM/quest/rookie/c02q05.sa.unity3d", "assetbundles/generated/PLATFORM/quest/rockhopper/c01q01.sa.unity3d,assetbundles/generated/PLATFORM/quest/rookie/c02q03.sa.unity3d" }
    };

    [MenuItem("Project/Generate Client Content Manifest")]
    public static void GenerateManifestFile()
    {
        string EmbeddedManifest = Application.dataPath + "/Generated/Resources/Configuration/";
        string manifestFilePath = Path.Combine(EmbeddedManifest, "embedded_content_manifest.txt");
        string manifestFileJsonPath = Path.Combine(EmbeddedManifest, "embedded_content_manifest.json.json");
        string ContentVersionPath = Application.dataPath + "/Game/Resources/Configuration/ContentVersion.txt";

        string platformFolder = GetPlatformFolderFromActiveBuildTarget();
        if (platformFolder == "unknown")
        {
            Debug.LogError("Unknown platform, aborting manifest generation.");
            return;
        }

        Dictionary<string, string> bundleDependencies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in bundleDependenciesTemplate)
        {
            string k = (kv.Key ?? "").Replace("PLATFORM", platformFolder).Replace("\\", "/").ToLower();

            string v = kv.Value ?? "";
            if (!string.IsNullOrEmpty(v))
            {
                var parts = v.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => s.Trim())
                             .Select(s => (s ?? "").Replace("PLATFORM", platformFolder).Replace("\\", "/").ToLower());
                v = string.Join(",", parts);
            }
            v = (v ?? "").Replace("\\", "/").ToLower();

            if (!string.IsNullOrEmpty(k))
                bundleDependencies[k] = v;
        }

        string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        using var sha1 = SHA1.Create();
        byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(timestamp));
        string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        try
        {
            HashSet<string> writtenLines = new HashSet<string>();
            List<string> assetLines = new List<string>();
            using (StreamWriter writer = new StreamWriter(manifestFilePath))
            {
                // Process Resources assets
                string[] guids = AssetDatabase.FindAssets("", new[] { "Assets" });
                for (int i = 0; i < guids.Length; i++)
                {
                    string guid = guids[i];
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    if (AssetDatabase.IsValidFolder(assetPath))
                        continue;

                    string lowerPath = assetPath.ToLower();
                    if (lowerPath.Contains("textmesh pro/resources") || lowerPath.Contains("zfbrowser/"))
                        continue;

                    if (!lowerPath.Contains("/resources/"))
                        continue;

                    string relative = GetRelativePathInResources(assetPath);
                    if (string.IsNullOrEmpty(relative))
                        continue;

                    string dir = Path.GetDirectoryName(relative)?.Replace("\\", "/");
                    string fileWithNoExt = Path.GetFileNameWithoutExtension(relative);
                    string ext = Path.GetExtension(relative).TrimStart('.').ToLower();

                    string assetPartNoExt = string.IsNullOrEmpty(dir)
                        ? fileWithNoExt.ToLower()
                        : $"{dir}/{fileWithNoExt}".ToLower();

                    string assetLine = $"asset:{assetPartNoExt}?dl=res&x={ext}".ToLowerInvariant();
                    if (TryGetLocalizedResourceGenericKey(assetPartNoExt, out string genericLocalizedKey))
                    {
                        string localizedAssetLine = GetLocalizedResourceManifestLine(genericLocalizedKey, ext);
                        if (!writtenLines.Contains(localizedAssetLine))
                        {
                            writtenLines.Add(localizedAssetLine);
                            writer.WriteLine(localizedAssetLine);
                            assetLines.Add(localizedAssetLine);
                        }
                    }

                    if (!writtenLines.Contains(assetLine))
                    {
                        writtenLines.Add(assetLine);
                        writer.WriteLine(assetLine);

                        assetLines.Add(assetLine);
                    }
                }

                string forcedMissingResource = "asset:rewards/rewardpopup/itemrewardpopupbg_default?dl=res&x=prefab".ToLower();
                if (!writtenLines.Contains(forcedMissingResource))
                {
                    writtenLines.Add(forcedMissingResource);
                    writer.WriteLine(forcedMissingResource);
                    assetLines.Add(forcedMissingResource);
                }

                // Process Bundle assets
                string[] allPaths = AssetDatabase.GetAllAssetPaths();
                var bundleAssets = allPaths
                    .Where(p => !AssetDatabase.IsValidFolder(p))
                    .Where(p =>
                    {
                        var imp = AssetImporter.GetAtPath(p);
                        return imp != null && !string.IsNullOrEmpty(imp.assetBundleName) && !imp.assetBundleName.StartsWith("cdn/");
                    })
                    .ToArray();

                for (int i = 0; i < bundleAssets.Length; i++)
                {
                    string assetPath = bundleAssets[i];
                    EditorUtility.DisplayProgressBar("Generating Manifest", $"Processing bundle assets {i + 1}/{bundleAssets.Length}", (float)(i + 1) / bundleAssets.Length);

                    var importer = AssetImporter.GetAtPath(assetPath);
                    if (importer == null) continue;

                    string bundleLabel = importer.assetBundleName;
                    string bundleVariant = importer.assetBundleVariant;

                    // Remove .txt from bundle labels
                    if (bundleLabel.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        bundleLabel = bundleLabel.Substring(0, bundleLabel.Length - 4);

                    if (!string.IsNullOrEmpty(bundleVariant) && bundleVariant.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        bundleVariant = bundleVariant.Substring(0, bundleVariant.Length - 4);

                    string bundleFileName = string.IsNullOrEmpty(bundleVariant) ? bundleLabel : $"{bundleLabel}.{bundleVariant}";

                    // Look for generated bundle files in StreamingAssets
                    string bundlesRoot = Path.Combine(Application.dataPath, "StreamingAssets/assetbundles/generated", platformFolder).Replace("\\", "/");
                    string foundBundlePath = null;
                    if (Directory.Exists(bundlesRoot))
                    {
                        var allFiles = Directory.GetFiles(bundlesRoot, "*.*", SearchOption.AllDirectories);
                        foreach (var f in allFiles)
                        {
                            string fNorm = f.Replace("\\", "/");

                            // Remove .txt from filename for matching
                            string fNormNoTxt = fNorm;
                            if (fNormNoTxt.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                                fNormNoTxt = fNormNoTxt.Substring(0, fNormNoTxt.Length - 4);

                            if (fNormNoTxt.EndsWith(bundleFileName, StringComparison.OrdinalIgnoreCase))
                            {
                                int idx = fNorm.IndexOf("assetbundles/");
                                if (idx >= 0)
                                {
                                    foundBundlePath = fNorm.Substring(idx).Replace("\\", "/").ToLower();

                                    // Remove .txt from found path
                                    if (foundBundlePath.EndsWith(".txt"))
                                        foundBundlePath = foundBundlePath.Substring(0, foundBundlePath.Length - 4);

                                    break;
                                }
                            }
                        }
                    }

                    string fallbackBundlePath = $"assetbundles/generated/{platformFolder}/{bundleFileName}".ToLower();
                    if (fallbackBundlePath.EndsWith(".txt"))
                        fallbackBundlePath = fallbackBundlePath.Substring(0, fallbackBundlePath.Length - 4);

                    string bundlePathForManifest = foundBundlePath ?? fallbackBundlePath;

                    // Correct assetPart: relative path after AssetBundles folder
                    string relAsset = assetPath.Replace("\\", "/");

                    string[] folders = relAsset.Split('/');
                    int idxAssetBundles = Array.FindIndex(folders, f => string.Equals(f, "AssetBundles", StringComparison.OrdinalIgnoreCase));
                    string assetPart = idxAssetBundles >= 0
                        ? string.Join("/", folders.Skip(idxAssetBundles + 1))
                        : Path.GetFileNameWithoutExtension(relAsset);

                    assetPart = Path.Combine(Path.GetDirectoryName(assetPart) ?? "", Path.GetFileNameWithoutExtension(assetPart))
                        .Replace("\\", "/").ToLower();

                    // Detect original asset extension for x=
                    string extForX = Path.GetExtension(assetPath).TrimStart('.').ToLower();
                    if (string.IsNullOrEmpty(extForX)) extForX = "";


                    //relAsset Assets/Game/UI/Exchange/AssetBundles/
                    //assetPart Images/Exchange_Collectible_WindChimes.
                    //extForX png
                    string pFolder = relAsset.ToLower().Remove(0, 7).Replace("/" + assetPart + "." + extForX, "");

                    // Generate manifest line for bundles with proper x= and d=
                    string dValue = "";
                    string lookupKey = (bundlePathForManifest ?? "").Replace("\\", "/").ToLower();
                    if (bundleDependencies.ContainsKey(lookupKey))
                        dValue = bundleDependencies[lookupKey];

                    string assetLine = $"asset:{assetPart}?dl=bundle:sa-bundle&x={extForX}&b={bundlePathForManifest}&p={pFolder}";

                    if (!writtenLines.Contains(assetLine))
                    {
                        writtenLines.Add(assetLine);
                        writer.WriteLine(assetLine);
                    }
                }

                EditorUtility.ClearProgressBar();

                // Write all bundle files for reference
                string bundlesRootDir = Path.Combine(Application.dataPath, "StreamingAssets/assetbundles/generated", platformFolder).Replace("\\", "/");
                if (Directory.Exists(bundlesRootDir))
                {
                    var bundleFiles = Directory.GetFiles(bundlesRootDir, "*.*", SearchOption.AllDirectories)
                        .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                        .Select(f => f.Replace("\\", "/").ToLower())
                        .ToArray();

                    Array.Sort(bundleFiles, StringComparer.Ordinal);

                    foreach (var f in bundleFiles)
                    {
                        // Remove .txt
                        string cleanPath = f;
                        if (cleanPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                            cleanPath = cleanPath.Substring(0, cleanPath.Length - 4);

                        int idx = cleanPath.IndexOf("assetbundles/");
                        if (idx == -1) continue;

                        string clean = cleanPath.Substring(idx);

                        string dValue = "";
                        string lookupKey = (clean ?? "").Replace("\\", "/").ToLower();
                        if (bundleDependencies.ContainsKey(lookupKey))
                            dValue = bundleDependencies[lookupKey];

                        string line = $"bundle:{clean}?d={dValue}&p=0";
                        if (!writtenLines.Contains(line))
                        {
                            writtenLines.Add(line);
                            writer.WriteLine(line);
                        }
                    }
                }

                writer.WriteLine("baseuri:");
                writer.WriteLine($"contentversion:{hash}");
                writer.WriteLine("contentmanifesthash:");
            }

            EditorUtility.ClearProgressBar();
            Debug.Log("Manifest generated: " + manifestFilePath);

            var manifest = new ManifestData { assets = assetLines };
            string jsonOutput = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            File.WriteAllText(manifestFileJsonPath, jsonOutput);

            Debug.Log("Manifest Json generated: " + manifestFileJsonPath);

            File.WriteAllText(ContentVersionPath, hash);

            Debug.Log($"ContentVersion got updated with new sha-1: {hash} and at path: {ContentVersionPath}");

            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("Error generating manifest file: " + e.Message);
        }
    }

    private static string GetRelativePathInResources(string assetPath)
    {
        int idx = assetPath.ToLower().IndexOf("/resources/");
        if (idx == -1) return null;
        return assetPath.Substring(idx + "/resources/".Length).Replace("\\", "/");
    }

    private static bool TryGetLocalizedResourceGenericKey(string assetPartNoExt, out string genericKey)
    {
        for (int i = 0; i < localizedResourceLanguageSuffixes.Length; i++)
        {
            string suffix = localizedResourceLanguageSuffixes[i];
            if (assetPartNoExt.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                string candidate = assetPartNoExt.Substring(0, assetPartNoExt.Length - suffix.Length);
                if (localizedResourceGenericKeys.Contains(candidate))
                {
                    genericKey = candidate.ToLowerInvariant();
                    return true;
                }
                break;
            }
        }

        genericKey = null;
        return false;
    }

    private static string GetLocalizedResourceManifestLine(string genericKey, string ext)
    {
        return $"asset:{genericKey}?dl=res&x={ext.ToLowerInvariant()}&l=true&ld=en_US";
    }

    private static string GetPlatformFolderFromActiveBuildTarget()
    {
        BuildTarget t = EditorUserBuildSettings.activeBuildTarget;

        if (t == BuildTarget.StandaloneWindows || t == BuildTarget.StandaloneWindows64)
            return "standalonewindows64";
        if (t == BuildTarget.StandaloneOSX)
            return "standaloneosx";
        if (t == BuildTarget.StandaloneLinux64)
            return "standalonelinux64";
        if (t == BuildTarget.Android)
            return "android";
        if (t == BuildTarget.iOS)
            return "ios";
        if (t == BuildTarget.WebGL)
            return "webgl";

        return "unknown";
    }
}
