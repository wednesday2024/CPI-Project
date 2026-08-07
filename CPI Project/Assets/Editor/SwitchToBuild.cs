using System.IO;
using UnityEditor;
using UnityEngine;
using Disney.Kelowna.Common;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class SwitchToBuild : MonoBehaviour
{
    [MenuItem("Project/Run Before Build")]
    public static void SwitchPlatform()
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

        if (platform == "unknown")
        {
            Debug.LogError("Unknown platform, aborting switch.");
            return;
        }

        ModifyClientInfoAsset(platform);
        ModifyTextFile(platform);
        AssetDatabase.SaveAssets();

        Debug.Log("SwitchToBuild: Platform switch completed for " + platform);
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
            Debug.Log("Platform set to: " + platform + " in client_info.asset");
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
            Debug.Log("Text file updated with platform: " + platform);
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
}

public class SwitchToBuildHook : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        SwitchToBuild.SwitchPlatform();
    }
}
