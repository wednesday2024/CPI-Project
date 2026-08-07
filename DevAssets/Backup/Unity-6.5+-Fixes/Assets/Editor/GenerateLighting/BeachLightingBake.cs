using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

public class BeachLightingBake : MonoBehaviour
{
    [MenuItem("Project/Generate lighting/Lightmap baking/Beach")]
    static void OpenScene()
    {
        BakeBeach();
    }

    static double bakeStartTime; // Timer for elapsed seconds
    static List<GameObject> temporarilyStaticObjects = new List<GameObject>();
    static System.Action _postBakeAction = null;

    static void BakeBeach()
    {
        string scenePath = "Assets/Game/World/Scenes/Beach.unity";
        EditorSceneManager.OpenScene(scenePath);
        Scene activeScene = SceneManager.GetActiveScene();

        if (!activeScene.IsValid())
        {
            Debug.LogError("No active scene found.");
            return;
        }

        Debug.Log("Current Scene Name: " + activeScene.name);
        Debug.Log("Current Scene Path: " + activeScene.path);

        string gameObjectName = "GameObjectLocations";
        GameObject[] rootObjects = activeScene.GetRootGameObjects();
        GameObject targetObject = null;

        foreach (GameObject obj in rootObjects)
        {
            if (obj.name == gameObjectName)
            {
                targetObject = obj;
                break;
            }
        }

        if (targetObject == null)
        {
            Debug.LogError("GameObject not found: " + gameObjectName);
            return;
        }

        Debug.Log("Found GameObject: " + targetObject.name);
        GameObjectLocations Gol = targetObject.GetComponent<GameObjectLocations>();

        // Delete only .exr files and LightingData.asset inside scene folder
        string FolderPath = "Assets/Game/World/Scenes/Beach";
        string[] files = Directory.GetFiles(FolderPath, "*", SearchOption.AllDirectories);

        foreach (string file in files)
        {
            if (file.EndsWith(".exr") || file.EndsWith("LightingData.asset"))
            {
                Debug.Log("Deleting: " + file);
                AssetDatabase.DeleteAsset(file.Replace(Application.dataPath, "Assets"));
            }
        }

        // Set up lighting configuration
        Gol.ChangeSkybox(Gol.LightmappingSkybox);

        // Gol.Animated.isStatic = true;
        // SetStaticRecursively(Gol.Animated, true);

        Gol.Animated2.isStatic = true;
        SetStaticRecursively(Gol.Animated2, true);

        Gol.StaticObject1.isStatic = false;
        SetStaticRecursively(Gol.StaticObject1, false);

        Gol.StaticObject2.isStatic = false;
        SetStaticRecursively(Gol.StaticObject2, false);

        Gol.StaticObject3.isStatic = false;
        SetStaticRecursively(Gol.StaticObject3, false);

        Gol.StaticObject4.isStatic = false;
        SetStaticRecursively(Gol.StaticObject4, false);

        // Set "MARK4STATIC_" objects to static
        temporarilyStaticObjects.Clear();
        GameObject[] allGameObjects = Object.FindObjectsByType<GameObject>();
        foreach (GameObject go in allGameObjects)
        {
            if (go.name.StartsWith("MARK4STATIC_") && !go.isStatic)
            {
                go.isStatic = true;
                temporarilyStaticObjects.Add(go);
                Debug.Log("Temporarily set static: " + go.name);
            }
        }

        Gol.ChangeSource(AmbientMode.Skybox);

        // Setup post-bake cleanup
        _postBakeAction = () =>
        {
            Gol.ChangeSkybox(Gol.DayCubemap);

            // Gol.Animated.isStatic = false;
            // SetStaticRecursively(Gol.Animated, false);

            Gol.Animated2.isStatic = false;
            SetStaticRecursively(Gol.Animated2, false);

            Gol.StaticObject1.isStatic = true;
            SetStaticRecursively(Gol.StaticObject1, true);

            Gol.StaticObject3.isStatic = true;
            SetStaticRecursively(Gol.StaticObject3, true);

            Gol.StaticObject4.isStatic = true;
            SetStaticRecursively(Gol.StaticObject4, true);

            // Revert "MARK4STATIC_" objects
            foreach (GameObject go in temporarilyStaticObjects)
            {
                if (go != null)
                {
                    go.isStatic = false;
                    Debug.Log("Reverted static: " + go.name);
                }
            }
            temporarilyStaticObjects.Clear();

            Gol.StaticObject2.isStatic = true;
            SetStaticRecursively(Gol.StaticObject2, true);

            Gol.ChangeSource(AmbientMode.Flat);

            Debug.Log("Lightmap baking completed.");
        };

        // Begin baking with progress bar
        bakeStartTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += UpdateProgressBar;
        Lightmapping.bakeCompleted += OnBakeCompleted;
        Lightmapping.BakeAsync();
    }

    private static void SetStaticRecursively(GameObject parent, bool flag)
    {
        foreach (Transform child in parent.transform)
        {
            child.gameObject.isStatic = flag;
            SetStaticRecursively(child.gameObject, flag);
        }
    }

    static void OnBakeCompleted()
    {
        EditorUtility.ClearProgressBar();
        EditorApplication.update -= UpdateProgressBar;
        Lightmapping.bakeCompleted -= OnBakeCompleted;

        _postBakeAction?.Invoke();
        _postBakeAction = null;
    }

    static void UpdateProgressBar()
    {
        if (Lightmapping.isRunning)
        {
            double elapsed = EditorApplication.timeSinceStartup - bakeStartTime;
            EditorUtility.DisplayProgressBar(
                "Baking Lightmaps...",
                $"Busy for {elapsed:F1} seconds.\nPlease wait while Unity bakes the lightmaps.",
                0.5f
            );
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
        else
        {
            EditorUtility.ClearProgressBar();
            EditorApplication.update -= UpdateProgressBar;
        }
    }
}
