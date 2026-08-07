using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.IO;

public class SeaCavesLightingBake : MonoBehaviour
{
    [MenuItem("Project/Generate lighting/Lightmap baking/Sea Caves")]
    static void OpenScene()
    {
        BakeSeaCaves();
    }

    static double bakeStartTime; // Timer for elapsed seconds

    static void BakeSeaCaves()
    {
        EditorSceneManager.OpenScene("Assets/Game/World/Scenes/Diving.unity");
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.IsValid())
        {
            Debug.Log("Current Scene Name: " + activeScene.name);
            Debug.Log("Current Scene Path: " + activeScene.path);
        }
        else
        {
            Debug.LogError("No active scene found.");
        }

        if (activeScene.IsValid())
        {
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

            if (targetObject != null)
            {
                Debug.Log("Found GameObject: " + targetObject.name);
                GameObjectLocations Gol = targetObject.GetComponent<GameObjectLocations>();

                // Delete only .exr files and LightingData.asset inside the Diving folder
                string folderPath = "Assets/Game/World/Scenes/Diving";
                string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    if (file.EndsWith(".exr") || file.EndsWith("LightingData.asset"))
                    {
                        Debug.Log("Deleting: " + file);
                        AssetDatabase.DeleteAsset(file.Replace(Application.dataPath, "Assets"));
                    }
                }

                // Set for baking
                Gol.ChangeSkybox(Gol.LightmappingSkybox);

                Gol.ChangeSource(AmbientMode.Skybox);

                Gol.StaticObject1.isStatic = false;
                SetStaticRecursively(Gol.StaticObject1, false);

                Gol.StaticObject2.isStatic = false;
                SetStaticRecursively(Gol.StaticObject2, false);

                Gol.StaticObject3.isStatic = false;
                SetStaticRecursively(Gol.StaticObject3, false);

                Gol.StaticObject4.isStatic = false;
                SetStaticRecursively(Gol.StaticObject4, false);

                Gol.StaticObject5.isStatic = false;
                SetStaticRecursively(Gol.StaticObject5, false);

                Gol.StaticObject6.isStatic = false;
                SetStaticRecursively(Gol.StaticObject6, false);

                Gol.StaticObject7.isStatic = true;
                SetStaticRecursively(Gol.StaticObject7, true);

                // ========== Progress Bar Patch with Elapsed Time ==========
                _postBakeAction = () =>
                {
                    Gol.ChangeSkybox(Gol.DivingCubemap);

                    Gol.ChangeSource(AmbientMode.Flat);

                    Gol.StaticObject1.isStatic = true;
                    SetStaticRecursively(Gol.StaticObject1, true);

                    Gol.StaticObject2.isStatic = true;
                    SetStaticRecursively(Gol.StaticObject2, true);

                    Gol.StaticObject3.isStatic = true;
                    SetStaticRecursively(Gol.StaticObject3, true);

                    Gol.StaticObject4.isStatic = true;
                    SetStaticRecursively(Gol.StaticObject4, true);

                    Gol.StaticObject5.isStatic = true;
                    SetStaticRecursively(Gol.StaticObject5, true);

                    Gol.StaticObject6.isStatic = true;
                    SetStaticRecursively(Gol.StaticObject6, true);

                    Gol.StaticObject7.isStatic = true;
                    SetStaticRecursively(Gol.StaticObject7, true);

                    Debug.Log("Lightmap baking completed.");
                };

                bakeStartTime = EditorApplication.timeSinceStartup; // Start timer
                EditorApplication.update += UpdateProgressBar;
                Lightmapping.bakeCompleted += OnBakeCompleted;
                Lightmapping.BakeAsync();
                return;
                // ========================================================
            }
            else
            {
                Debug.LogError("GameObject not found: " + gameObjectName);
            }
        }
        else
        {
            Debug.LogError("No active scene found.");
        }
    }

    private static void SetStaticRecursively(GameObject parent, bool flag)
    {
        foreach (Transform child in parent.transform)
        {
            child.gameObject.isStatic = flag;
            SetStaticRecursively(child.gameObject, flag);
        }
    }

    // ===== Progress Bar Support =====
    private static System.Action _postBakeAction = null;

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