using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using System.Text;
using Disney.Kelowna.Common;

public class CreateAssetBundles : MonoBehaviour
{
    private sealed class PlatformGuidPair
    {
        public readonly string Landscape;
        public readonly string Portrait;

        public PlatformGuidPair(string landscape, string portrait)
        {
            Landscape = landscape;
            Portrait = portrait;
        }
    }

    private static readonly PlatformGuidPair[] platformGuidMap =
    {
        new PlatformGuidPair("da198753b02579940800ff30f3448aea", "bb5e05892a61daf45bf46e634114c649"),
        new PlatformGuidPair("63f16fc003895eb4f862d5051eeeb0e9", "6b96bc0f6b303c0449f8b58e46771881"),
        new PlatformGuidPair("76b050ca715275e4596879ba176c9fd3", "0ab5c1ee1574c57499a7f37d8e46218c"),
        new PlatformGuidPair("d595192883b992546be6f86ff8feb8ac", "252ff1bc9c9161c47b44d33032878290"),
        new PlatformGuidPair("0f139580c8cf9a7478563d532e65e6bf", "8d4785b95d210ea42b97188ee165d003"),
        new PlatformGuidPair("bfdb4e3cf87607141bb1af4934fef6b4", "48e89a6caac727f479217b789ce0c553"),
        new PlatformGuidPair("d6209b187784b3342a61ae6c048ae3e7", "d52e10999d0071d4a93bec1a3c9a38da")
    };

    [MenuItem("Project/AssetBundles/Generated/Generate client side AssetBundles")]
    static void BuildAllAssetBundles()
    {
        if (!SaveProject())
        {
            return;
        }

        string platform = DetectAndSwitchPlatform();

        if (platform == "unknown")
        {
            Debug.LogError("Unknown platform, aborting Asset Bundle generation.");
            return;
        }

        if (!SwitchPlatformReferences(platform))
        {
            return;
        }

        ModifyClientInfoAsset(platform);
        ModifyTextFile(platform);
        AssetDatabase.SaveAssets();

        List<AssetBundleBuild> validAssetBundles = new List<AssetBundleBuild>();

        foreach (var assetBundleName in AssetDatabase.GetAllAssetBundleNames())
        {
            if (!assetBundleName.StartsWith("CDN/", System.StringComparison.OrdinalIgnoreCase))
            {
                AssetBundleBuild build = new AssetBundleBuild();
                build.assetBundleName = assetBundleName;
                build.assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
                validAssetBundles.Add(build);
            }
        }

        string assetBundleDirectory = "";

#if UNITY_ANDROID
        assetBundleDirectory = "Assets/StreamingAssets/assetbundles/generated/android";
        EnsureAndClearDirectory(assetBundleDirectory);
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, validAssetBundles.ToArray(), BuildAssetBundleOptions.None, BuildTarget.Android);

#elif UNITY_IOS
        assetBundleDirectory = "Assets/StreamingAssets/assetbundles/generated/ios";
        EnsureAndClearDirectory(assetBundleDirectory);
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, validAssetBundles.ToArray(), BuildAssetBundleOptions.None, BuildTarget.iOS);

#elif UNITY_STANDALONE_OSX
        assetBundleDirectory = "Assets/StreamingAssets/assetbundles/generated/standaloneosx";
        EnsureAndClearDirectory(assetBundleDirectory);
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, validAssetBundles.ToArray(), BuildAssetBundleOptions.None, BuildTarget.StandaloneOSX);

#elif UNITY_STANDALONE_WIN
        assetBundleDirectory = "Assets/StreamingAssets/assetbundles/generated/standalonewindows64";
        EnsureAndClearDirectory(assetBundleDirectory);
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, validAssetBundles.ToArray(), BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

#elif UNITY_STANDALONE_LINUX || UNITY_STANDALONE_LINUX64
        assetBundleDirectory = "Assets/StreamingAssets/assetbundles/generated/standalonelinux64";
        EnsureAndClearDirectory(assetBundleDirectory);
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, validAssetBundles.ToArray(), BuildAssetBundleOptions.None, BuildTarget.StandaloneLinux64);

#elif UNITY_WEBGL
        assetBundleDirectory = "Assets/StreamingAssets/assetbundles/generated/webgl";
        EnsureAndClearDirectory(assetBundleDirectory);
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, validAssetBundles.ToArray(), BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.WebGL);
#endif

        CleanupPlatformBundles(assetBundleDirectory, platform);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
       // GenerateManifest.GenerateManifestFile();
    }

    private static bool SaveProject()
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isDirty || string.IsNullOrEmpty(scene.path))
            {
                continue;
            }

            if (!EditorSceneManager.SaveScene(scene))
            {
                return false;
            }
        }

        AssetDatabase.SaveAssets();
        return true;
    }

    private static bool SwitchPlatformReferences(string platform)
    {
        bool usePortrait = platform == "android" || platform == "ios";
        string[] files = Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories);

        try
        {
            for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
            {
                string file = files[fileIndex];
                string relativePath = file.Substring(Application.dataPath.Length + 1).Replace('\\', '/');
                EditorUtility.DisplayProgressBar(
                    "Switching Platform References",
                    platform + "  " + relativePath,
                    files.Length == 0 ? 1f : (float)fileIndex / files.Length);

                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                byte[] contents = File.ReadAllBytes(file);
                bool changed = false;
                foreach (PlatformGuidPair pair in platformGuidMap)
                {
                    string sourceGuid = usePortrait ? pair.Landscape : pair.Portrait;
                    string targetGuid = usePortrait ? pair.Portrait : pair.Landscape;
                    changed |= ReplaceGuidReferences(contents, sourceGuid, targetGuid);
                }

                if (changed)
                {
                    File.WriteAllBytes(file, contents);
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        return true;
    }

    private static bool ReplaceGuidReferences(byte[] contents, string sourceGuid, string targetGuid)
    {
        byte[] source = Encoding.ASCII.GetBytes("guid: " + sourceGuid);
        byte[] replacement = Encoding.ASCII.GetBytes("guid: " + targetGuid);
        bool changed = false;

        for (int index = 0; index <= contents.Length - source.Length; index++)
        {
            int offset = 0;
            while (offset < source.Length && contents[index + offset] == source[offset])
            {
                offset++;
            }

            if (offset != source.Length)
            {
                continue;
            }

            Buffer.BlockCopy(replacement, 0, contents, index, replacement.Length);
            index += source.Length - 1;
            changed = true;
        }

        return changed;
    }

    private static string DetectAndSwitchPlatform()
    {
        string platform = "";

#if UNITY_STANDALONE_WIN
        platform = "standalonewindows64";
#elif UNITY_STANDALONE_OSX
        platform = "standaloneosx";
#elif UNITY_STANDALONE_LINUX || UNITY_STANDALONE_LINUX64
        platform = "standalonelinux64";
#elif UNITY_ANDROID
        platform = "android";
#elif UNITY_IOS
        platform = "ios";
#elif UNITY_WEBGL
        platform = "webgl";
#else
        platform = "unknown";
#endif

        if (platform != "unknown")
        {
        }

        return platform;
    }

    private static void ModifyClientInfoAsset(string platform)
    {
        var clientInfo = AssetDatabase.LoadAssetAtPath<ClientInfo>("Assets/Generated/Resources/Configuration/client_info.asset");

        if (clientInfo != null)
        {
            SerializedObject serializedObject = new SerializedObject(clientInfo);
            SerializedProperty platformProperty = serializedObject.FindProperty("Platform");
            platformProperty.stringValue = platform;

            serializedObject.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogError("client_info.asset not found.");
        }
    }

    private static void ModifyTextFile(string platform)
    {
        string txtFilePath = "Assets/Generated/Resources/Configuration/embedded_content_manifest.txt";

        if (File.Exists(txtFilePath))
        {
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
        else
        {
            Debug.LogError(".txt file not found.");
        }
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
            int idx = line.IndexOf(marker, searchFrom, System.StringComparison.Ordinal);
            if (idx < 0)
            {
                break;
            }

            int segStart = idx + marker.Length;
            int segEnd = FindNextDelimiter(line, segStart);

            if (segEnd <= segStart)
            {
                searchFrom = segStart;
                continue;
            }

            string currentPlatform = line.Substring(segStart, segEnd - segStart);

            if (IsKnownPlatform(currentPlatform))
            {
                line = line.Substring(0, segStart) + newPlatform + line.Substring(segEnd);
                segEnd = segStart + newPlatform.Length;
            }

            searchFrom = segEnd;
        }

        return line;
    }

    private static int FindNextDelimiter(string s, int startIndex)
    {
        int best = s.Length;

        int a = s.IndexOf('/', startIndex);
        if (a >= 0 && a < best) best = a;

        int b = s.IndexOf('\\', startIndex);
        if (b >= 0 && b < best) best = b;

        int c = s.IndexOf('?', startIndex);
        if (c >= 0 && c < best) best = c;

        int d = s.IndexOf('&', startIndex);
        if (d >= 0 && d < best) best = d;

        int e = s.IndexOf('#', startIndex);
        if (e >= 0 && e < best) best = e;

        return best;
    }

    private static bool IsKnownPlatform(string p)
    {
        return p == "standalonewindows64" ||
               p == "standalonelinux64" ||
               p == "standaloneosx" ||
               p == "android" ||
               p == "ios" ||
               p == "webgl";
    }

    static void EnsureAndClearDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        else
        {
            ClearDirectory(path);
        }
    }

    static void ClearDirectory(string path)
    {
        string[] files = Directory.GetFiles(path);
        foreach (string file in files)
        {
            File.Delete(file);
        }

        string[] directories = Directory.GetDirectories(path);
        foreach (string directory in directories)
        {
            Directory.Delete(directory, true);
        }
    }

    static void CleanupPlatformBundles(string rootPath, string currentPlatform)
    {
        string[] knownPlatforms = new string[]
        {
            "android",
            "ios",
            "standalonelinux64",
            "standaloneosx",
            "standalonewindows64",
            "webgl"
        };

        string generatedRoot = "";
        if (!string.IsNullOrEmpty(rootPath))
        {
            generatedRoot = Path.GetDirectoryName(rootPath);
        }

        if (!string.IsNullOrEmpty(generatedRoot) && Directory.Exists(generatedRoot))
        {
            for (int i = 0; i < knownPlatforms.Length; i++)
            {
                string p = knownPlatforms[i];
                if (string.Equals(p, currentPlatform, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string otherDir = Path.Combine(generatedRoot, p);
                if (Directory.Exists(otherDir))
                {
                    Directory.Delete(otherDir, true);
                    string otherMeta = otherDir + ".meta";
                    if (File.Exists(otherMeta))
                    {
                        File.Delete(otherMeta);
                    }
                }
            }
        }

        if (!Directory.Exists(rootPath)) return;

        string defaultBundle = Path.Combine(rootPath, currentPlatform);
        if (File.Exists(defaultBundle))
        {
            string defaultBundleMeta = defaultBundle + ".meta";
            if (File.Exists(defaultBundleMeta))
            {
                File.Delete(defaultBundleMeta);
            }
            File.Delete(defaultBundle);
        }

        List<string> unwantedKeywordsList = new List<string>();
        for (int i = 0; i < knownPlatforms.Length; i++)
        {
            string p = knownPlatforms[i];
            if (!string.Equals(p, currentPlatform, System.StringComparison.OrdinalIgnoreCase))
            {
                unwantedKeywordsList.Add(p);
            }
        }
        string[] unwantedKeywords = unwantedKeywordsList.ToArray();

        string[] allFiles = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories);
        foreach (string file in allFiles)
        {
            string fileName = Path.GetFileName(file).ToLower();

            bool shouldDelete = fileName.EndsWith(".manifest");
            if (!shouldDelete)
            {
                foreach (string keyword in unwantedKeywords)
                {
                    if (fileName.Contains(keyword))
                    {
                        shouldDelete = true;
                        break;
                    }
                }
            }

            if (shouldDelete)
            {
                string metaFilePath = file + ".meta";
                if (File.Exists(metaFilePath))
                {
                    File.Delete(metaFilePath);
                }

                File.Delete(file);

                string platformMetaFile = Path.Combine(rootPath, Path.GetFileNameWithoutExtension(file) + ".meta");
                if (File.Exists(platformMetaFile))
                {
                    File.Delete(platformMetaFile);
                }
            }
        }
    }
}
