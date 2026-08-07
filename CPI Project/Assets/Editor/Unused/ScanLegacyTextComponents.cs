using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

public class ScanLegacyTextComponents : EditorWindow
{
    private Vector2 scroll;
    private static List<Result> results = new List<Result>();

    public class Result
    {
        public string path;
        public int line;
        public string content;
    }

    static readonly string[] patterns = new string[]
    {
        @"\bGUIText\b",
        @"\bTextMesh\b(?!Pro)",
        @"\bUnityEngine\.UI\.Text\b",
        @"\bText\s+[a-zA-Z0-9_]+\b",
        @"GetComponent\s*<\s*Text\s*>"
    };

    [MenuItem("Project/Temp/Scan All Assets for Legacy Text")]
    public static void Scan()
    {
        results.Clear();

        var files = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
            .Where(f =>
                (f.EndsWith(".cs") || f.EndsWith(".unity") || f.EndsWith(".prefab") || f.EndsWith(".asset")) &&
                !IsIgnored(f)
            )
            .ToArray();

        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];

            if (EditorUtility.DisplayCancelableProgressBar(
                "Scanning Assets",
                file,
                (float)i / files.Length))
            {
                break;
            }

            string[] lines;

            try
            {
                lines = File.ReadAllLines(file);
            }
            catch
            {
                continue;
            }

            bool foundInFile = false;

            for (int line = 0; line < lines.Length && !foundInFile; line++)
            {
                foreach (var pattern in patterns)
                {
                    if (Regex.IsMatch(lines[line], pattern))
                    {
                        results.Add(new Result
                        {
                            path = file.Replace(Application.dataPath, "Assets"),
                            line = line + 1,
                            content = lines[line].Trim()
                        });

                        foundInFile = true;
                        break;
                    }
                }
            }
        }

        EditorUtility.ClearProgressBar();

        GetWindow<ScanLegacyTextComponents>("Legacy Text Results");
    }

    static bool IsIgnored(string path)
    {
        string normalized = path.Replace("\\", "/");

        return
            normalized.Contains("/PlayMaker/") ||
            normalized.Contains("/TextMesh Pro/") ||
            normalized.Contains("/Editor/");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField($"Results: {results.Count}", EditorStyles.boldLabel);

        if (GUILayout.Button("Rescan", GUILayout.Width(100)))
        {
            Scan();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (results.Count == 0)
        {
            EditorGUILayout.HelpBox("No matches found.", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var r in results)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField(r.path, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Line {r.line}: {r.content}", EditorStyles.wordWrappedLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Go To Asset"))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(r.path);
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }

            if (r.path.EndsWith(".unity"))
            {
                if (GUILayout.Button("Open Scene"))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(r.path, OpenSceneMode.Single);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }
}