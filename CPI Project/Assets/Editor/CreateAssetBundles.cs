using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.Build.Pipeline;
using UnityEngine;
using Disney.Kelowna.Common;

public static class CreateAssetBundles
{
    [MenuItem("Project/AssetBundles/Generated/Generate client side AssetBundles")]
    private static void BuildAllAddressables()
    {
        string platform = GetPlatformName(EditorUserBuildSettings.activeBuildTarget);
        string outputDirectory = "Assets/StreamingAssets/assetbundles/generated/" + platform;
        EnsureDirectoryIsEmpty(outputDirectory);
        ModifyClientInfoAsset(platform);
        ModifyTextFile(platform);

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        ClearCdnAssetBundleNames();
        ConvertLegacyBundleAssignments(settings);
        ConfigureLocalGroups(settings, new List<AddressableAssetGroup>(settings.groups), platform);
        AssetDatabase.SaveAssets();

        int legacyBuilderIndex = GetLegacyBundleBuilderIndex(settings);
        settings.ActivePlayerDataBuilderIndex = legacyBuilderIndex;
        AssetDatabase.SaveAssets();

        AddressablesPlayerBuildResult result;
        AddressableAssetSettings.BuildPlayerContent(out result);
        if (!string.IsNullOrEmpty(result.Error))
        {
            throw new InvalidOperationException(result.Error);
        }
        CleanupPlatformBundles(outputDirectory, platform);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ClearCdnAssetBundleNames()
    {
        foreach (string bundleName in AssetDatabase.GetAllAssetBundleNames())
        {
            if (!bundleName.StartsWith("CDN/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (string assetPath in AssetDatabase.GetAssetPathsFromAssetBundle(bundleName))
            {
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                if (importer != null)
                {
                    importer.assetBundleName = string.Empty;
                    AssetDatabase.WriteImportSettingsIfDirty(assetPath);
                }
            }
        }
    }

    private static int GetLegacyBundleBuilderIndex(AddressableAssetSettings settings)
    {
        for (int i = 0; i < settings.DataBuilders.Count; i++)
        {
            if (settings.DataBuilders[i] is LegacyAddressableBundleBuilder)
            {
                return i;
            }
        }

        LegacyAddressableBundleBuilder builder = ScriptableObject.CreateInstance<LegacyAddressableBundleBuilder>();
        builder.name = nameof(LegacyAddressableBundleBuilder);
        AssetDatabase.AddObjectToAsset(builder, settings);
        if (!settings.AddDataBuilder(builder, false))
        {
            throw new InvalidOperationException("Failed to add the legacy Addressables bundle builder.");
        }
        AssetDatabase.SaveAssets();
        return settings.DataBuilders.Count - 1;
    }

    private static void ConvertLegacyBundleAssignments(AddressableAssetSettings settings)
    {
        string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
        foreach (string bundleName in bundleNames)
        {
            if (bundleName.StartsWith("CDN/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AddressableAssetGroup group = settings.FindGroup(bundleName);
            if (group == null)
            {
                group = settings.CreateGroup(bundleName, false, false, false, null, typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
            }

            BundledAssetGroupSchema schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.AppendHash;

            string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            foreach (string assetPath in assetPaths)
            {
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = GetAddress(assetPath);
                entry.SetLabel(bundleName, true, true);

                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                if (importer != null && !string.IsNullOrEmpty(importer.assetBundleName))
                {
                    importer.SetAssetBundleNameAndVariant(string.Empty, string.Empty);
                    AssetDatabase.WriteImportSettingsIfDirty(assetPath);
                }
            }
        }
        MergeGroupsWithMatchingBundleLabels(settings);
        RemoveEmptyLegacyGroups(settings);
    }

    private static void RemoveEmptyLegacyGroups(AddressableAssetSettings settings)
    {
        List<AddressableAssetGroup> emptyLegacyGroups = new List<AddressableAssetGroup>();
        foreach (AddressableAssetGroup group in settings.groups)
        {
            if (group != null &&
                !IsCdnGroup(group) &&
                group.Name.IndexOf(".sa.unity3d.txt", StringComparison.OrdinalIgnoreCase) >= 0 &&
                group.entries.Count == 0)
            {
                emptyLegacyGroups.Add(group);
            }
        }

        foreach (AddressableAssetGroup group in emptyLegacyGroups)
        {
            settings.RemoveGroup(group);
        }
    }

    private static void MergeGroupsWithMatchingBundleLabels(AddressableAssetSettings settings)
    {
        Dictionary<string, AddressableAssetGroup> groupsByBundleLabel = new Dictionary<string, AddressableAssetGroup>(StringComparer.OrdinalIgnoreCase);
        List<AddressableAssetGroup> groupsToRemove = new List<AddressableAssetGroup>();
        foreach (AddressableAssetGroup group in settings.groups)
        {
            if (group == null || IsCdnGroup(group))
            {
                continue;
            }

            foreach (AddressableAssetEntry entry in new List<AddressableAssetEntry>(group.entries))
            {
                string bundleLabel = GetLegacyBundleLabel(entry);
                if (bundleLabel == null)
                {
                    continue;
                }

                AddressableAssetGroup canonicalGroup;
                if (!groupsByBundleLabel.TryGetValue(bundleLabel, out canonicalGroup))
                {
                    groupsByBundleLabel.Add(bundleLabel, group);
                    continue;
                }
                if (canonicalGroup == group)
                {
                    continue;
                }

                settings.CreateOrMoveEntry(entry.guid, canonicalGroup);
                if (!groupsToRemove.Contains(group))
                {
                    groupsToRemove.Add(group);
                }
            }

        }

        foreach (AddressableAssetGroup group in groupsToRemove)
        {
            if (group.entries.Count == 0)
            {
                settings.RemoveGroup(group);
            }
        }
    }

    private static string GetLegacyBundleLabel(AddressableAssetEntry entry)
    {
        foreach (string label in entry.labels)
        {
            if (label.EndsWith(".sa.unity3d.txt", StringComparison.OrdinalIgnoreCase))
            {
                return label;
            }
        }
        return null;
    }

    private static bool IsCdnGroup(AddressableAssetGroup group)
    {
        return group.Name.StartsWith("CDN/", StringComparison.OrdinalIgnoreCase) ||
               group.Name.StartsWith("CDN-", StringComparison.OrdinalIgnoreCase);
    }

    private static void ConfigureLocalGroups(AddressableAssetSettings settings, List<AddressableAssetGroup> groups, string platform)
    {
        string buildPath = Path.Combine(Application.dataPath, "StreamingAssets", "assetbundles", "generated", platform);
        string loadPath = "{UnityEngine.Application.streamingAssetsPath}/assetbundles/generated/" + platform;
        BundledAssetGroupSchema.BundleCompressionMode compression = platform == "webgl"
            ? BundledAssetGroupSchema.BundleCompressionMode.LZ4
            : BundledAssetGroupSchema.BundleCompressionMode.LZMA;
        foreach (AddressableAssetGroup group in groups)
        {
            if (group == null || IsCdnGroup(group))
            {
                continue;
            }
            BundledAssetGroupSchema schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null)
            {
                continue;
            }
            schema.Compression = compression;
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
        }

        settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath, buildPath);
        settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath, loadPath);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.ProfileModified, settings, true, true);
    }

    private static string GetAddress(string assetPath)
    {
        const string assetsPrefix = "Assets/";
        if (!assetPath.StartsWith(assetsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Addressable asset path is outside the Assets folder: " + assetPath);
        }
        return ("assets/" + assetPath.Substring(assetsPrefix.Length)).ToLowerInvariant();
    }

    private static string GetPlatformName(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return "android";
            case BuildTarget.iOS:
                return "ios";
            case BuildTarget.StandaloneOSX:
                return "standaloneosx";
            case BuildTarget.StandaloneWindows64:
                return "standalonewindows64";
            case BuildTarget.StandaloneLinux64:
                return "standalonelinux64";
            case BuildTarget.WebGL:
                return "webgl";
            default:
                throw new InvalidOperationException("Unsupported Addressables build target: " + target);
        }
    }

    private static void ModifyClientInfoAsset(string platform)
    {
        const string assetPath = "Assets/Generated/Resources/Configuration/client_info.asset";
        ClientInfo clientInfo = AssetDatabase.LoadAssetAtPath<ClientInfo>(assetPath);
        if (clientInfo == null)
        {
            throw new FileNotFoundException("Client info asset was not found.", assetPath);
        }

        SerializedObject serializedObject = new SerializedObject(clientInfo);
        SerializedProperty platformProperty = serializedObject.FindProperty("Platform");
        if (platformProperty == null)
        {
            throw new InvalidOperationException("Client info asset does not contain the Platform property.");
        }
        platformProperty.stringValue = platform;
        serializedObject.ApplyModifiedProperties();
    }

    private static void ModifyTextFile(string platform)
    {
        const string txtFilePath = "Assets/Generated/Resources/Configuration/embedded_content_manifest.txt";
        if (!File.Exists(txtFilePath))
        {
            throw new FileNotFoundException("Embedded content manifest was not found.", txtFilePath);
        }

        string[] lines = File.ReadAllLines(txtFilePath);
        for (int i = 0; i < lines.Length; i++)
        {
            if (ShouldSkipLine(lines[i]))
            {
                continue;
            }
            if (!lines[i].Contains("assetbundles/generated/") && !lines[i].Contains("assetbundles\\generated\\"))
            {
                continue;
            }
            lines[i] = ReplaceGeneratedPlatformSegments(lines[i], platform, "assetbundles/generated/");
            lines[i] = ReplaceGeneratedPlatformSegments(lines[i], platform, "assetbundles\\generated\\");
        }
        File.WriteAllLines(txtFilePath, lines);
    }

    private static bool ShouldSkipLine(string line)
    {
        return line.Contains("asset:worldtraynodeprefabs/androidcontainer?dl=res&x=prefab") ||
               line.Contains("asset:mockauthproject/assetbundles/android/test_cube?dl=res&x=unity3d") ||
               line.Contains("asset:swrveassets.xcassets/app_icons.imageset/1.0.0_android?dl=res&x=png") ||
               line.Contains("asset:swrveassets.xcassets/app_icons.imageset/androidgo?dl=res&x=png") ||
               line.Contains("asset:swrveassets.xcassets/app_icons.imageset/round_android?dl=res&x=png") ||
               line.Contains("asset:swrveassets.xcassets/app_icons.imageset/1.0.0_ios?dl=res&x=png") ||
               line.Contains("asset:definitions/disneystoreitems/disneystoreitem_137_rainbowkiosk?dl=res&x=asset") ||
               line.Contains("asset:definitions/disneystoreitems/disneystoreitem_137_rainbowkiosk.reward?dl=res&x=asset") ||
               line.Contains("asset:definitions/decorations/decoration148_rainbowkiosk?dl=res&x=asset");
    }

    private static string ReplaceGeneratedPlatformSegments(string line, string newPlatform, string marker)
    {
        int searchFrom = 0;
        while (true)
        {
            int index = line.IndexOf(marker, searchFrom, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }
            int segmentStart = index + marker.Length;
            int segmentEnd = FindNextDelimiter(line, segmentStart);
            if (segmentEnd <= segmentStart)
            {
                searchFrom = segmentStart;
                continue;
            }
            string currentPlatform = line.Substring(segmentStart, segmentEnd - segmentStart);
            if (IsKnownPlatform(currentPlatform))
            {
                line = line.Substring(0, segmentStart) + newPlatform + line.Substring(segmentEnd);
                segmentEnd = segmentStart + newPlatform.Length;
            }
            searchFrom = segmentEnd;
        }
        return line;
    }

    private static int FindNextDelimiter(string value, int startIndex)
    {
        int best = value.Length;
        char[] delimiters = { '/', '\\', '?', '&', '#' };
        foreach (char delimiter in delimiters)
        {
            int index = value.IndexOf(delimiter, startIndex);
            if (index >= 0 && index < best)
            {
                best = index;
            }
        }
        return best;
    }

    private static bool IsKnownPlatform(string platform)
    {
        return platform == "standalonewindows64" ||
               platform == "standalonelinux64" ||
               platform == "standaloneosx" ||
               platform == "android" ||
               platform == "ios" ||
               platform == "webgl";
    }

    private static void EnsureDirectoryIsEmpty(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            return;
        }
        foreach (string file in Directory.GetFiles(path))
        {
            File.Delete(file);
        }
        foreach (string directory in Directory.GetDirectories(path))
        {
            Directory.Delete(directory, true);
        }
    }

    private static void CleanupPlatformBundles(string rootPath, string currentPlatform)
    {
        string[] knownPlatforms =
        {
            "android",
            "ios",
            "standalonelinux64",
            "standaloneosx",
            "standalonewindows64",
            "webgl"
        };
        string generatedRoot = Path.GetDirectoryName(rootPath);
        if (!string.IsNullOrEmpty(generatedRoot) && Directory.Exists(generatedRoot))
        {
            foreach (string platform in knownPlatforms)
            {
                if (string.Equals(platform, currentPlatform, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                string otherDirectory = Path.Combine(generatedRoot, platform);
                if (Directory.Exists(otherDirectory))
                {
                    Directory.Delete(otherDirectory, true);
                    string otherMeta = otherDirectory + ".meta";
                    if (File.Exists(otherMeta))
                    {
                        File.Delete(otherMeta);
                    }
                }
            }
        }
        if (!Directory.Exists(rootPath))
        {
            return;
        }
        List<string> unwantedPlatforms = new List<string>();
        foreach (string platform in knownPlatforms)
        {
            if (!string.Equals(platform, currentPlatform, StringComparison.OrdinalIgnoreCase))
            {
                unwantedPlatforms.Add(platform);
            }
        }
        foreach (string file in Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileName(file).ToLowerInvariant();
            bool shouldDelete = fileName.EndsWith(".manifest", StringComparison.Ordinal) ||
                                IsHashedAddressablesBundle(fileName);
            if (!shouldDelete)
            {
                foreach (string platform in unwantedPlatforms)
                {
                    if (fileName.Contains(platform))
                    {
                        shouldDelete = true;
                        break;
                    }
                }
            }
            if (shouldDelete)
            {
                string metaFile = file + ".meta";
                if (File.Exists(metaFile))
                {
                    File.Delete(metaFile);
                }
                File.Delete(file);
            }
        }
    }

    private static bool IsHashedAddressablesBundle(string fileName)
    {
        const string marker = "_assets_all_";
        int markerIndex = fileName.LastIndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0 || !fileName.EndsWith(".bundle", StringComparison.Ordinal))
        {
            return false;
        }

        string hash = fileName.Substring(markerIndex + marker.Length, fileName.Length - markerIndex - marker.Length - ".bundle".Length);
        if (hash.Length != 32)
        {
            return false;
        }
        foreach (char character in hash)
        {
            if (!Uri.IsHexDigit(character))
            {
                return false;
            }
        }
        return true;
    }
}

public sealed class LegacyAddressableBundleBuilder : BuildScriptPackedMode
{
    public override string Name
    {
        get { return nameof(LegacyAddressableBundleBuilder); }
    }

    protected override string ConstructAssetBundleName(AddressableAssetGroup assetGroup, BundledAssetGroupSchema schema, BundleDetails info, string assetBundleName)
    {
        if (assetGroup != null)
        {
            foreach (AddressableAssetEntry entry in assetGroup.entries)
            {
                foreach (string label in entry.labels)
                {
                    if (label.EndsWith(".sa.unity3d.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        return label.Substring(0, label.Length - ".txt".Length);
                    }
                }
            }
        }

        return base.ConstructAssetBundleName(assetGroup, schema, info, assetBundleName);
    }
}
