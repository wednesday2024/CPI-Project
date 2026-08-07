using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(DiscordController))]
public class DiscordControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DiscordController discordController = (DiscordController)target;

        if (discordController.customSceneNames == null)
            discordController.customSceneNames = new DiscordController.SceneNameMapping[0];

        EditorGUILayout.LabelField("Scene Name Mappings", EditorStyles.boldLabel);

        for (int i = 0; i < discordController.customSceneNames.Length; i++)
        {
            if (discordController.customSceneNames[i] == null)
                discordController.customSceneNames[i] = new DiscordController.SceneNameMapping();

            EditorGUILayout.BeginHorizontal();

            discordController.customSceneNames[i].sceneName =
                EditorGUILayout.TextField("Scene Name", discordController.customSceneNames[i].sceneName);

            discordController.customSceneNames[i].displayName =
                EditorGUILayout.TextField("Display Name", discordController.customSceneNames[i].displayName);

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                var list = new List<DiscordController.SceneNameMapping>(discordController.customSceneNames);
                list.RemoveAt(i);
                discordController.customSceneNames = list.ToArray();
                EditorGUILayout.EndHorizontal();
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Scene Mapping"))
        {
            var list = new List<DiscordController.SceneNameMapping>(discordController.customSceneNames);
            list.Add(new DiscordController.SceneNameMapping());
            discordController.customSceneNames = list.ToArray();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(discordController);
        }

        DrawDefaultInspector();
    }
}
