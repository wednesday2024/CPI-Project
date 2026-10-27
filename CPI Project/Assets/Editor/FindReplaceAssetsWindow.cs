using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FindReplaceAssetsWindow : EditorWindow
{
	private static readonly HashSet<string> TextExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		".asmdef", ".asmref", ".asset", ".c", ".cginc", ".compute", ".config", ".csv", ".cs", ".glsl",
		".h", ".hlsl", ".html", ".ini", ".js", ".md", ".shader", ".sql", ".text",
		".txt", ".uss", ".uxml", ".xml", ".yaml", ".yml"
	};

	private string findText = "";
	private string replaceText = "";
	private string resultText = "";

	[MenuItem("Project/Editor/Find and Replace Text")]
	private static void Open()
	{
		GetWindow<FindReplaceAssetsWindow>("Find and Replace Text");
	}

	private void OnGUI()
	{
		EditorGUILayout.LabelField("Find and replace text", EditorStyles.boldLabel);
		findText = EditorGUILayout.TextField("Find", findText);
		replaceText = EditorGUILayout.TextField("Replace with", replaceText);

		EditorGUILayout.HelpBox("Scans common text file types. Binary assets are skipped.", MessageType.Info);
		if (!string.IsNullOrEmpty(resultText))
			EditorGUILayout.LabelField(resultText);

		using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(findText)))
		{
			if (GUILayout.Button("Replace All"))
				ReplaceAll();
		}
	}

	private void ReplaceAll()
	{
		string projectPath = Directory.GetParent(Application.dataPath).FullName;
		List<string> files = new List<string>(Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories));
		string projectSettingsPath = Path.Combine(projectPath, "ProjectSettings");
		if (Directory.Exists(projectSettingsPath))
			files.AddRange(Directory.GetFiles(projectSettingsPath, "*", SearchOption.AllDirectories));
		files.Sort(StringComparer.OrdinalIgnoreCase);
		int changedFiles = 0;
		int replacements = 0;

		try
		{
			for (int i = 0; i < files.Count; i++)
			{
				string file = files[i];
				EditorUtility.DisplayProgressBar("Find and Replace", Path.GetFileName(file), files.Count == 0 ? 1f : (float)i / files.Count);
				if (!TextExtensions.Contains(Path.GetExtension(file)))
					continue;

				string original;
				try
				{
					original = File.ReadAllText(file);
				}
				catch
				{
					continue;
				}

				int count = CountOccurrences(original, findText);
				if (count == 0)
					continue;

				File.WriteAllText(file, original.Replace(findText, replaceText));
				changedFiles++;
				replacements += count;
			}
		}
		finally
		{
			EditorUtility.ClearProgressBar();
		}

		AssetDatabase.Refresh();
		resultText = "Replaced " + replacements + " occurrence(s) in " + changedFiles + " file(s).";
	}

	private static int CountOccurrences(string text, string value)
	{
		int count = 0;
		int index = 0;
		while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
		{
			count++;
			index += value.Length;
		}
		return count;
	}
}
