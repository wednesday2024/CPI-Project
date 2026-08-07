#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Reflection;
using Discord.Sdk;

[InitializeOnLoad]
public static class DiscordRPCEditor
{
    private static Discord.Sdk.Client client;
    private static bool initialized;
    private static double nextUpdateTime;
    private static ulong rpcStartTimestampMs;
    private static MethodInfo runCallbacksStatic;

    private const ulong defaultApplicationId = 1372515579461636126UL;
    private const string largeImageKey = "default_icon";

    private const string EnabledKey = "DiscordRPC_Enabled";
    private const string CustomAppIDKey = "DiscordRPC_CustomAppID";
    private const string ShowProjectNameKey = "DiscordRPC_ShowProjectName";
    private const string ShowSceneNameKey = "DiscordRPC_ShowSceneName";
    private const string ShowUnityVersionKey = "DiscordRPC_ShowUnityVersion";
    private const string ShowPlatformKey = "DiscordRPC_ShowPlatform";
    private const string ShowGraphicsAPIKey = "DiscordRPC_ShowGraphicsAPI";
    private const string ShowStartTimeKey = "DiscordRPC_ShowStartTime";
    private const string LoggingEnabledKey = "DiscordRPC_LoggingEnabled";

    static DiscordRPCEditor()
    {
        if (!EditorPrefs.HasKey(EnabledKey)) EditorPrefs.SetBool(EnabledKey, true);
        if (!EditorPrefs.HasKey(ShowProjectNameKey)) EditorPrefs.SetBool(ShowProjectNameKey, true);
        if (!EditorPrefs.HasKey(ShowSceneNameKey)) EditorPrefs.SetBool(ShowSceneNameKey, true);
        if (!EditorPrefs.HasKey(ShowUnityVersionKey)) EditorPrefs.SetBool(ShowUnityVersionKey, true);
        if (!EditorPrefs.HasKey(ShowPlatformKey)) EditorPrefs.SetBool(ShowPlatformKey, true);
        if (!EditorPrefs.HasKey(ShowGraphicsAPIKey)) EditorPrefs.SetBool(ShowGraphicsAPIKey, true);
        if (!EditorPrefs.HasKey(ShowStartTimeKey)) EditorPrefs.SetBool(ShowStartTimeKey, true);
        if (!EditorPrefs.HasKey(LoggingEnabledKey)) EditorPrefs.SetBool(LoggingEnabledKey, false);

        EditorApplication.update -= Update;
        EditorApplication.update += Update;

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        EditorApplication.quitting -= Shutdown;
        EditorApplication.quitting += Shutdown;

        AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
        AssemblyReloadEvents.beforeAssemblyReload += Shutdown;

        EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChangedInEditMode;
        EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChangedInEditMode;

        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;

        if (EditorPrefs.GetBool(EnabledKey))
            TryInitializeDiscord();
    }

    private static void TryInitializeDiscord()
    {
        try
        {
            ulong appId = defaultApplicationId;
            string customIdStr = EditorPrefs.GetString(CustomAppIDKey, "");

            if (!string.IsNullOrEmpty(customIdStr) && ulong.TryParse(customIdStr, out ulong parsedId))
                appId = parsedId;

            client = new Discord.Sdk.Client();
            client.AddLogCallback(OnDiscordLog, LoggingSeverity.Info);
            client.SetStatusChangedCallback(OnStatusChanged);
            client.SetApplicationId(appId);

            try
            {
                int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                client.SetGameWindowPid(pid);
            }
            catch { }

            rpcStartTimestampMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            CacheOptionalRunCallbacks();

            initialized = true;
            nextUpdateTime = 0;

            UnityEngine.Debug.Log("[DiscordRPCEditor] Discord Social SDK initialized.");
        }
        catch (Exception e)
        {
            initialized = false;
            client = null;
            UnityEngine.Debug.Log("[DiscordRPCEditor] Discord was not detected or failed to initialize. The RPC will not start.\n" + e.Message);
        }
    }

    private static void Update()
    {
        bool enabled = EditorPrefs.GetBool(EnabledKey);
        bool editorOwnsPresence = !EditorApplication.isPlayingOrWillChangePlaymode;

        if (!enabled || !editorOwnsPresence)
        {
            if (initialized)
                Shutdown();
            return;
        }

        if (!initialized || client == null)
        {
            TryInitializeDiscord();
            if (!initialized || client == null)
                return;
        }

        TryRunCallbacksIfExposed();

        if (EditorApplication.timeSinceStartup >= nextUpdateTime)
        {
            UpdateActivity();
            nextUpdateTime = EditorApplication.timeSinceStartup + 15;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode ||
            state == PlayModeStateChange.EnteredPlayMode ||
            state == PlayModeStateChange.ExitingPlayMode)
        {
            Shutdown();
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
            ForceImmediateUpdate();
    }

    private static void OnActiveSceneChangedInEditMode(Scene previousScene, Scene nextScene)
    {
        ForceImmediateUpdate();
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        ForceImmediateUpdate();
    }

    private static void ForceImmediateUpdate()
    {
        nextUpdateTime = 0;

        if (!EditorPrefs.GetBool(EnabledKey))
            return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (!initialized || client == null)
            TryInitializeDiscord();

        if (!initialized || client == null)
            return;

        UpdateActivity();
        nextUpdateTime = EditorApplication.timeSinceStartup + 15;
    }

    private static void UpdateActivity()
    {
        if (client == null)
            return;

        string unityVersion = Application.unityVersion;
        string sceneName = SceneManager.GetActiveScene().name;
        string platform = Application.platform.ToString();
        string graphicsAPI = SystemInfo.graphicsDeviceType.ToString();
        string projectName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Application.dataPath));

        string details = "";

        if (EditorPrefs.GetBool(ShowProjectNameKey))
            details += $"Project: {projectName}";

        if (EditorPrefs.GetBool(ShowSceneNameKey))
        {
            if (!string.IsNullOrEmpty(details))
                details += " | ";
            details += $"Scene: {sceneName}.unity";
        }

        string state = "";

        if (EditorPrefs.GetBool(ShowUnityVersionKey))
            state += $"Unity {unityVersion}";

        if (EditorPrefs.GetBool(ShowPlatformKey))
            state += (string.IsNullOrEmpty(state) ? "" : " | ") + platform;

        if (EditorPrefs.GetBool(ShowGraphicsAPIKey))
            state += (string.IsNullOrEmpty(state) ? "" : ", ") + graphicsAPI;

        Activity activity = new Activity();
        activity.SetType(ActivityTypes.Playing);
        activity.SetDetails(details);
        activity.SetState(state);

        var assets = new ActivityAssets();
        assets.SetLargeImage(largeImageKey);
        assets.SetLargeText("Unity Editor Discord RPC | (Made by the GitHub user: wednesday2024)");
        activity.SetAssets(assets);

        if (EditorPrefs.GetBool(ShowStartTimeKey))
        {
            var timestamps = new ActivityTimestamps();
            timestamps.SetStart(rpcStartTimestampMs);
            activity.SetTimestamps(timestamps);
        }

        client.UpdateRichPresence(activity, OnUpdateRichPresence);
    }

    private static void OnUpdateRichPresence(ClientResult result)
    {
        if (!result.Successful())
            UnityEngine.Debug.LogWarning($"[DiscordRPCEditor] Failed to update activity: {result.Error()}");
    }

    private static void Shutdown()
    {
        try
        {
            if (client != null)
            {
                try
                {
                    client.ClearRichPresence();
                }
                catch { }

                TryRunCallbacksIfExposed();

                try
                {
                    client.Disconnect();
                }
                catch { }

                TryRunCallbacksIfExposed();

                try
                {
                    client.Dispose();
                }
                catch { }
            }

            client = null;
            initialized = false;

            UnityEngine.Debug.Log("[DiscordRPCEditor] The Discord RPC has shutdown successfully.");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("[DiscordRPCEditor] The Discord RPC failed to shutdown successfully: " + e.Message);
        }
    }

    private static void OnDiscordLog(string message, LoggingSeverity severity)
    {
        if (!EditorPrefs.GetBool(LoggingEnabledKey)) return;
        UnityEngine.Debug.Log($"[DiscordRPCEditor] {severity}: {message}");
    }

    private static void OnStatusChanged(Discord.Sdk.Client.Status status, Discord.Sdk.Client.Error error, int errorCode)
    {
        if (error != Discord.Sdk.Client.Error.None)
            UnityEngine.Debug.LogWarning($"[DiscordRPCEditor] Status: {status} Error: {error} (code {errorCode})");
    }

    [MenuItem("Project/Editor/Unity Editor Discord RPC Settings")]
    private static void ShowSettings()
    {
        EditorWindow.GetWindow<DiscordRPCSettingsWindow>("Discord RPC Settings");
    }

    public class DiscordRPCSettingsWindow : EditorWindow
    {
        private string customAppId;

        private void OnEnable()
        {
            customAppId = EditorPrefs.GetString(CustomAppIDKey, "");
        }

        private void OnGUI()
        {
            GUILayout.Label("Discord RPC Settings", EditorStyles.boldLabel);

            bool prevEnabled = EditorPrefs.GetBool(EnabledKey);
            bool newEnabled = EditorGUILayout.Toggle("Enable Discord RPC", prevEnabled);
            if (newEnabled != prevEnabled)
                EditorPrefs.SetBool(EnabledKey, newEnabled);

            GUILayout.Space(10);
            GUILayout.Label("Display Options", EditorStyles.boldLabel);

            EditorPrefs.SetBool(ShowProjectNameKey, EditorGUILayout.Toggle("Show Project Name", EditorPrefs.GetBool(ShowProjectNameKey)));
            EditorPrefs.SetBool(ShowSceneNameKey, EditorGUILayout.Toggle("Show Scene Name", EditorPrefs.GetBool(ShowSceneNameKey)));
            EditorPrefs.SetBool(ShowUnityVersionKey, EditorGUILayout.Toggle("Show Unity Version", EditorPrefs.GetBool(ShowUnityVersionKey)));
            EditorPrefs.SetBool(ShowPlatformKey, EditorGUILayout.Toggle("Show Platform", EditorPrefs.GetBool(ShowPlatformKey)));
            EditorPrefs.SetBool(ShowGraphicsAPIKey, EditorGUILayout.Toggle("Show Graphics API", EditorPrefs.GetBool(ShowGraphicsAPIKey)));
            EditorPrefs.SetBool(ShowStartTimeKey, EditorGUILayout.Toggle("Show Start Time", EditorPrefs.GetBool(ShowStartTimeKey)));
            EditorPrefs.SetBool(LoggingEnabledKey, EditorGUILayout.Toggle("Enable Logging", EditorPrefs.GetBool(LoggingEnabledKey)));

            GUILayout.Space(10);
            GUILayout.Label("Application ID Override", EditorStyles.boldLabel);

            customAppId = EditorGUILayout.TextField("Custom App ID", customAppId);

            if (GUILayout.Button("Save App ID"))
            {
                EditorPrefs.SetString(CustomAppIDKey, customAppId);
                if (initialized)
                    Shutdown();
                UnityEngine.Debug.Log("[DiscordRPCEditor] Custom Application ID saved.");
            }
        }
    }

    private static void CacheOptionalRunCallbacks()
    {
        if (runCallbacksStatic != null)
            return;

        string[] candidateTypeNames =
        {
            "discordpp",
            "discordpp.discordpp",
            "Discord.Sdk.discordpp",
            "Discord.Sdk.NativeMethods"
        };

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var tn in candidateTypeNames)
            {
                try
                {
                    var t = asm.GetType(tn, false);
                    if (t == null) continue;

                    var mi = t.GetMethod("RunCallbacks", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                    if (mi != null)
                    {
                        runCallbacksStatic = mi;
                        return;
                    }
                }
                catch { }
            }
        }
    }

    private static void TryRunCallbacksIfExposed()
    {
        if (runCallbacksStatic == null)
            return;

        try
        {
            runCallbacksStatic.Invoke(null, null);
        }
        catch { }
    }

}
#endif
