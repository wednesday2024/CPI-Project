using ClubPenguin;
using ClubPenguin.Adventure;
using ClubPenguin.Core;
using ClubPenguin.Game.PartyGames;
using ClubPenguin.MiniGames.TiltATube;
using ClubPenguin.PartyGames;
using ClubPenguin.Progression;
using ClubPenguin.UI;
using DevonLocalization.Core;
using Discord.Sdk;
using Disney.Kelowna.Common;
using Disney.Kelowna.Common.DataModel;
using Disney.LaunchPadFramework;
using Disney.MobileNetwork;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DiscordController : MonoBehaviour
{
    private const ulong applicationId = 1235329861980127343UL;

    public const string DiscordRpcPlayerPrefsKey = "discord_rpc_enabled";

    private Discord.Sdk.Client client;
    private bool initialized;

    private static ulong gameStartTimeMs;
    private static MethodInfo runCallbacksStatic;

    private EventChannel eventChannel;
    private bool questHooked;
    private bool tubeHooked;
    private bool marketplaceHooked;
    private bool cfcHooked;

    private static string currentBaseRoomName = "";
    private static string currentAdditiveRoomName = "";
    private static string questOnlyDetailsOverride = "";
    private static string areaNameOverride = "";
    private static bool marketplaceOpen = false;
    private static string marketplaceName = "";
    private static string marketplaceLabel = "";

    private static bool cfcDonationActive = false;

    private static bool tubeLobbyActive = false;
    private static bool tubeRaceActive = false;
    private static PartyGameDefinition.GameTypes tubeRaceType = PartyGameDefinition.GameTypes.TUBE_RACE_RED;

    private static bool tubeScoreValid = false;
    private static float tubeScoreValue = 0f;
    private static float tubeScoreUntilUnscaled = 0f;

    [SerializeField] private float statsSwapSeconds = 30f;

    private bool statsMode;
    private float nextStatsSwapUnscaled;
    private bool runtimeHooksRegistered;

    private static string cachedPlatformLabel = null;

    public static DiscordController Instance { get; private set; }
    public static string CurrentRoomName { get; private set; }
    public static string LastLoadedSceneName { get; private set; }
    public static string LastLoadedAdditiveSceneName { get; private set; }

    private static string GetPlatformKey(string key)
    {
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        if (UnityEngine.Application.isEditor)
        {
            return "Editor_" + key;
        }
#endif
        return key;
    }

    public static bool IsRpcEnabledInPrefs()
    {
        return PlayerPrefs.GetInt(GetPlatformKey(DiscordRpcPlayerPrefsKey), 1) == 1;
    }

    public static void SetRpcEnabledInPrefs(bool enabled)
    {
        PlayerPrefs.SetInt(GetPlatformKey(DiscordRpcPlayerPrefsKey), enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetEnabledGlobal(bool enabled)
    {
        SetRpcEnabledInPrefs(enabled);

        DiscordController target = FindExistingTarget();

        if (target == null)
            return;

        if (enabled)
        {
            if (!target.gameObject.activeSelf)
                target.gameObject.SetActive(true);

            if (!target.enabled)
                target.enabled = true;

            target.InitializeIfAllowed();
            target.SyncSceneStateFromUnity();
            target.RefreshPresenceOnly();
        }
        else
        {
            target.DisableRpcNow();
        }
    }

    public static void ShutdownGlobal()
    {
        DiscordController target = FindExistingTarget();
        if (target == null)
            return;

        target.ShutdownRpcNow(false);
    }

    public static void SetRoomGlobal(string roomNameOrQuestOverride)
    {
        if (Instance != null)
            Instance.SetRoom(roomNameOrQuestOverride);
    }

    public static void SetRoomFromSceneNameGlobal(string sceneName)
    {
        if (Instance != null)
            Instance.SetBaseRoom(Instance.GetCustomSceneName(sceneName));
    }

    public static void RefreshRoomFromLastLoadedSceneGlobal()
    {
        if (Instance != null)
            Instance.RefreshFromSceneState();
    }

    public static void SetAreaNameOverrideGlobal(string areaName)
    {
        if (Instance != null)
            Instance.SetAreaNameOverride(areaName);
    }

    public static void ClearAreaNameOverrideGlobal()
    {
        if (Instance != null)
            Instance.SetAreaNameOverride("");
    }

    public static void SetCFCDonationStatusGlobal(bool active)
    {
        if (Instance != null)
            Instance.SetCFCDonationStatus(active);
    }

    [Serializable]
    public class SceneNameMapping
    {
        public string sceneName;
        public string displayName;
    }

    public SceneNameMapping[] customSceneNames;

    private static DiscordController FindExistingTarget()
    {
        if (Instance != null)
            return Instance;

        try
        {
            var all = Resources.FindObjectsOfTypeAll<DiscordController>();
            if (all != null && all.Length > 0)
                return all[0];
        }
        catch { }

        return null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        InitializeIfAllowed();
    }

    private void Start()
    {
        InitializeIfAllowed();
    }

    private void InitializeIfAllowed()
    {
        if (initialized || client != null)
            return;

        if (!IsRpcEnabledInPrefs())
        {
            DisableRpcNow();
            return;
        }

        if (!IsDiscordRunning())
        {
            UnityEngine.Debug.LogWarning("Discord is not running or installed. Skipping Discord integration.");
            return;
        }

        try
        {
            client = new Discord.Sdk.Client();
            client.AddLogCallback(OnDiscordLog, LoggingSeverity.Error);
            client.SetStatusChangedCallback(OnStatusChanged);
            client.SetApplicationId(applicationId);

            try
            {
                int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                client.SetGameWindowPid(pid);
            }
            catch { }

            if (gameStartTimeMs == 0UL)
                gameStartTimeMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            CacheOptionalRunCallbacks();

            questOnlyDetailsOverride = "";
            areaNameOverride = "";
            marketplaceOpen = false;
            marketplaceName = "";
            marketplaceLabel = "";
            cfcDonationActive = false;

            tubeLobbyActive = false;
            tubeRaceActive = false;
            tubeScoreValid = false;
            tubeScoreValue = 0f;
            tubeScoreUntilUnscaled = 0f;

            statsMode = false;
            nextStatsSwapUnscaled = Time.unscaledTime + Mathf.Max(5f, statsSwapSeconds);

            RegisterRuntimeHooks();

            initialized = true;

            TryHookQuestEvents();
            TryHookTubeRaceEvents();
            TryHookMarketplaceEvents();
            TryHookCFCPopupEvents();

            SyncSceneStateFromUnity();

            UpdatePresence(BuildStateText(), "default_icon", BuildDetailsText(), GetPlatformLabel(), gameStartTimeMs);

            RefreshPresenceOnly();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Failed to initialize Discord Social SDK: " + e.Message);
            client = null;
            initialized = false;
        }
    }

    private void SyncSceneStateFromUnity()
    {
        try
        {
            Scene active = SceneManager.GetActiveScene();
            string activeName = active.name ?? "";

            if (!string.IsNullOrEmpty(activeName))
            {
                LastLoadedSceneName = activeName;
                currentBaseRoomName = NormalizeName(GetCustomSceneName(activeName));
            }
            else
            {
                if (string.IsNullOrEmpty(currentBaseRoomName))
                {
                    CurrentRoomName = "Loading...";
                    currentBaseRoomName = CurrentRoomName;
                }
            }

            string additiveName = "";
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (!s.isLoaded) continue;
                if (s == active) continue;

                string n = s.name ?? "";
                if (!string.IsNullOrEmpty(n))
                    additiveName = n;
            }

            if (!string.IsNullOrEmpty(additiveName))
            {
                LastLoadedAdditiveSceneName = additiveName;
                currentAdditiveRoomName = NormalizeName(GetCustomSceneName(additiveName));
            }
            else
            {
                LastLoadedAdditiveSceneName = "";
                currentAdditiveRoomName = "";
            }
        }
        catch { }
    }

    private void Update()
    {
        if (!IsRpcEnabledInPrefs())
        {
            if (initialized || client != null)
                DisableRpcNow();
            return;
        }

        if (!initialized || client == null)
            return;

        TryRunCallbacksIfExposed();

        if (!questHooked)
            TryHookQuestEvents();

        if (!tubeHooked)
            TryHookTubeRaceEvents();

        if (!marketplaceHooked)
            TryHookMarketplaceEvents();

        if (!cfcHooked)
            TryHookCFCPopupEvents();

        if (ShouldSwapStatsModeNow())
        {
            statsMode = !statsMode;
            nextStatsSwapUnscaled = Time.unscaledTime + Mathf.Max(5f, statsSwapSeconds);
            RefreshPresenceOnly();
        }

        if (tubeScoreValid && Time.unscaledTime > tubeScoreUntilUnscaled)
        {
            tubeScoreValid = false;
            tubeScoreValue = 0f;
            RefreshPresenceOnly();
        }
    }

    private void OnDestroy()
    {
        ShutdownRpcNow(false);

        if (Instance == this)
            Instance = null;
    }

    private void OnApplicationQuit()
    {
        ShutdownRpcNow(false);
    }

    private void DisableRpcNow()
    {
        ShutdownRpcNow(true);
    }

    private void RegisterRuntimeHooks()
    {
        UnregisterRuntimeHooks();

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        Application.quitting += OnApplicationQuitting;
        runtimeHooksRegistered = true;
    }

    private void UnregisterRuntimeHooks()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        Application.quitting -= OnApplicationQuitting;
        runtimeHooksRegistered = false;
    }

    private void ReleaseEventHooks()
    {
        if (eventChannel != null)
        {
            eventChannel.RemoveAllListeners();
            eventChannel = null;
        }

        questHooked = false;
        tubeHooked = false;
        marketplaceHooked = false;
        cfcHooked = false;
    }

    private void ShutdownRpcNow(bool disableComponent)
    {
        try
        {
            ReleaseEventHooks();
        }
        catch { }

        try
        {
            if (runtimeHooksRegistered)
                UnregisterRuntimeHooks();
        }
        catch { }

        SafeShutdown(true);

        if (!disableComponent)
            return;

        try
        {
            enabled = false;
        }
        catch { }
    }

    private void SafeShutdown(bool clearPresence)
    {
        if (client != null)
        {
            try
            {
                if (clearPresence)
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

            client = null;
        }

        initialized = false;
    }

    private void OnApplicationQuitting()
    {
        ShutdownRpcNow(false);
    }

    private void OnDiscordLog(string message, LoggingSeverity severity)
    {
    }

    private void OnStatusChanged(Discord.Sdk.Client.Status status, Discord.Sdk.Client.Error error, int errorCode)
    {
        if (error != Discord.Sdk.Client.Error.None)
            UnityEngine.Debug.LogError($"[Discord Social SDK] Error: {error} (code {errorCode})");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearMarketplaceState();

        if (mode == LoadSceneMode.Additive)
        {
            LastLoadedAdditiveSceneName = scene.name;
            currentAdditiveRoomName = NormalizeName(GetCustomSceneName(scene.name));
            RefreshPresenceOnly();
            return;
        }

        LastLoadedSceneName = scene.name;
        currentBaseRoomName = NormalizeName(GetCustomSceneName(scene.name));
        currentAdditiveRoomName = "";
        questOnlyDetailsOverride = "";
        RefreshPresenceOnly();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        ClearMarketplaceState();

        if (!string.IsNullOrEmpty(LastLoadedAdditiveSceneName) && scene.name == LastLoadedAdditiveSceneName)
        {
            LastLoadedAdditiveSceneName = "";
            currentAdditiveRoomName = "";
        }

        if (tubeLobbyActive || tubeRaceActive)
        {
            tubeLobbyActive = false;
            tubeRaceActive = false;
        }

        RefreshFromSceneState();
    }

    private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        SyncSceneStateFromUnity();
        ClearMarketplaceState();

        if (!IsQuestActiveNow())
            questOnlyDetailsOverride = "";

        RefreshPresenceOnly();
    }

    private string GetCustomSceneName(string sceneName)
    {
        if (customSceneNames != null)
        {
            foreach (var mapping in customSceneNames)
            {
                if (mapping != null && mapping.sceneName == sceneName)
                    return mapping.displayName;
            }
        }

        return sceneName;
    }

    private bool IsQuestActiveNow()
    {
        try
        {
            QuestService qs = Service.Get<QuestService>();
            return qs != null && qs.ActiveQuest != null;
        }
        catch
        {
            return false;
        }
    }

    private bool TryGetActiveQuestDefinition(out QuestDefinition def)
    {
        def = null;
        try
        {
            QuestService qs = Service.Get<QuestService>();
            if (qs == null || qs.ActiveQuest == null)
                return false;
            def = qs.ActiveQuest.Definition;
            return def != null;
        }
        catch
        {
            return false;
        }
    }

    private void TryHookQuestEvents()
    {
        try
        {
            EventDispatcher dispatcher = Service.Get<EventDispatcher>();
            if (dispatcher == null)
                return;

            if (eventChannel == null)
                eventChannel = new EventChannel(dispatcher);

            eventChannel.AddListener<QuestEvents.QuestStarted>(OnQuestStarted);
            eventChannel.AddListener<QuestEvents.QuestCompleted>(OnQuestCompleted);
            eventChannel.AddListener<QuestEvents.QuestSyncCompleted>(OnQuestSyncCompleted);

            questHooked = true;
        }
        catch
        {
            questHooked = false;
        }
    }

    private void TryHookTubeRaceEvents()
    {
        try
        {
            EventDispatcher dispatcher = Service.Get<EventDispatcher>();
            if (dispatcher == null)
                return;

            if (eventChannel == null)
                eventChannel = new EventChannel(dispatcher);

            eventChannel.AddListener<TubeRaceEvents.LocalPlayerJoinedLobby>(OnTubeLobbyJoin);
            eventChannel.AddListener<TubeRaceEvents.LocalPlayerLeftLobby>(OnTubeLobbyLeave);
            eventChannel.AddListener<TubeRaceEvents.CloseLobby>(OnTubeLobbyClose);
            eventChannel.AddListener<TubeRaceEvents.RaceStart>(OnTubeRaceStart);
            eventChannel.AddListener<TubeRaceEvents.RaceEnd>(OnTubeRaceEnd);
            eventChannel.AddListener<TubeRaceEvents.EndGameResultsReceived>(OnTubeEndGameResults);

            tubeHooked = true;
        }
        catch
        {
            tubeHooked = false;
        }
    }

    private void TryHookMarketplaceEvents()
    {
        try
        {
            EventDispatcher dispatcher = Service.Get<EventDispatcher>();
            if (dispatcher == null)
                return;

            if (eventChannel == null)
                eventChannel = new EventChannel(dispatcher);

            eventChannel.AddListener<MarketplaceEvents.MarketplaceOpened>(OnMarketplaceOpened);
            eventChannel.AddListener<MarketplaceEvents.MarketplaceClosed>(OnMarketplaceClosed);

            marketplaceHooked = true;
        }
        catch
        {
            marketplaceHooked = false;
        }
    }

    private void TryHookCFCPopupEvents()
    {
        try
        {
            EventDispatcher dispatcher = Service.Get<EventDispatcher>();
            if (dispatcher == null)
                return;

            if (eventChannel == null)
                eventChannel = new EventChannel(dispatcher);

            eventChannel.AddListener<CFCPopupEvents.CFCPopupOpened>(OnCFCPopupOpened);
            eventChannel.AddListener<CFCPopupEvents.CFCPopupClosed>(OnCFCPopupClosed);

            cfcHooked = true;
        }
        catch
        {
            cfcHooked = false;
        }
    }

    private bool OnQuestStarted(QuestEvents.QuestStarted evt)
    {
        RefreshPresenceOnly();
        return false;
    }

    private bool OnQuestCompleted(QuestEvents.QuestCompleted evt)
    {
        questOnlyDetailsOverride = "";
        RefreshPresenceOnly();
        return false;
    }

    private bool OnQuestSyncCompleted(QuestEvents.QuestSyncCompleted evt)
    {
        if (!IsQuestActiveNow())
            questOnlyDetailsOverride = "";
        RefreshPresenceOnly();
        return false;
    }

    private bool OnMarketplaceOpened(MarketplaceEvents.MarketplaceOpened evt)
    {
        marketplaceOpen = true;
        marketplaceName = (evt.MarketplaceName ?? "").Trim();
        marketplaceLabel = GetMarketplaceDisplayName(evt.MarketplaceName);
        RefreshPresenceOnly();
        return false;
    }

    private bool OnMarketplaceClosed(MarketplaceEvents.MarketplaceClosed evt)
    {
        ClearMarketplaceState();
        RefreshPresenceOnly();
        return false;
    }

    private void ClearTubeRaceState()
    {
        tubeLobbyActive = false;
        tubeRaceActive = false;
    }

    private bool OnTubeLobbyJoin(TubeRaceEvents.LocalPlayerJoinedLobby evt)
    {
        tubeLobbyActive = true;
        tubeRaceActive = false;
        RefreshPresenceOnly();
        return false;
    }

    private bool OnTubeLobbyLeave(TubeRaceEvents.LocalPlayerLeftLobby evt)
    {
        ClearTubeRaceState();
        RefreshFromSceneState();
        return false;
    }

    private bool OnTubeLobbyClose(TubeRaceEvents.CloseLobby evt)
    {
        ClearTubeRaceState();
        RefreshFromSceneState();
        return false;
    }

    private bool OnTubeRaceStart(TubeRaceEvents.RaceStart evt)
    {
        tubeLobbyActive = false;
        tubeRaceActive = true;
        tubeRaceType = evt.RaceType;
        tubeScoreValid = false;
        tubeScoreValue = 0f;
        RefreshPresenceOnly();
        return false;
    }

    private bool OnTubeRaceEnd(TubeRaceEvents.RaceEnd evt)
    {
        ClearTubeRaceState();
        RefreshFromSceneState();
        return false;
    }

    private bool OnTubeEndGameResults(TubeRaceEvents.EndGameResultsReceived evt)
    {
        try
        {
            long localId = 0L;
            try
            {
                CPDataEntityCollection c = Service.Get<CPDataEntityCollection>();
                if (c != null)
                    localId = c.LocalPlayerSessionId;
            }
            catch { }

            if (evt.PlayerResults != null && evt.PlayerResults.Count > 0 && localId > 0L)
            {
                var r = evt.PlayerResults.FirstOrDefault(x => x != null && x.PlayerId == localId);
                if (r != null)
                {
                    tubeScoreValue = r.OverallScore;
                    tubeScoreValid = true;
                    tubeScoreUntilUnscaled = Time.unscaledTime + 12f;
                }
            }
        }
        catch { }

        ClearTubeRaceState();
        RefreshFromSceneState();
        return false;
    }

    private void ClearMarketplaceState()
    {
        marketplaceOpen = false;
        marketplaceName = "";
        marketplaceLabel = "";
    }

    private bool OnCFCPopupOpened(CFCPopupEvents.CFCPopupOpened evt)
    {
        cfcDonationActive = true;
        RefreshPresenceOnly();
        return false;
    }

    private bool OnCFCPopupClosed(CFCPopupEvents.CFCPopupClosed evt)
    {
        cfcDonationActive = false;
        RefreshPresenceOnly();
        return false;
    }

    private static string GetPlatformLabel()
    {
        if (cachedPlatformLabel != null)
            return cachedPlatformLabel;

        switch (SystemInfo.operatingSystemFamily)
        {
            case OperatingSystemFamily.Windows:
                cachedPlatformLabel = "Playing on Windows.";
                break;
            case OperatingSystemFamily.MacOSX:
                cachedPlatformLabel = "Playing on macOS.";
                break;
            case OperatingSystemFamily.Linux:
                cachedPlatformLabel = "Playing on Linux.";
                break;
            default:
                cachedPlatformLabel = "Playing on Unknown.";
                break;
        }

        return cachedPlatformLabel;
    }

    private void RefreshPresenceOnly()
    {
        if (!IsRpcEnabledInPrefs())
            return;

        if (string.IsNullOrEmpty(currentBaseRoomName))
            return;

        if (!IsQuestActiveNow())
            questOnlyDetailsOverride = "";

        string detailsText = BuildDetailsText();
        string stateText = BuildStateText();

        string roomImage = "default_icon";
        string iconMatchName = BuildIconMatchName();

        bool iconFound = false;

        if (IsQuestActiveNow())
        {
            string questMascotImage = GetQuestMascotImageKey();
            if (!string.IsNullOrEmpty(questMascotImage))
            {
                roomImage = questMascotImage;
                iconFound = true;
            }
        }

        if (!iconFound)
        {
            if (iconMatchName.IndexOf("Halloween Construction", StringComparison.OrdinalIgnoreCase) >= 0 ||
                iconMatchName.IndexOf("Holiday Construction", StringComparison.OrdinalIgnoreCase) >= 0 ||
                iconMatchName.IndexOf("Summer Splash Construction", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                roomImage = "jackhammer";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Clothing Designer", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "designer";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Puffle Roundup", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "puffleroundup";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Ice Fishing", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "icefishing";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Bean Counters", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "beancounters";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Pizzatron", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "pizzatron";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Cookietron", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "cookietron";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Candy", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "candybc";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Smoothie Smash", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "smoothiesmash";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Home", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "default_icon";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Scorn", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "sc";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Iceberg Base", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "hb";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Beacon Boardwalk | The Migrator", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "rh";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Island Central | DJ Cadence's Studio", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "dj";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Island Central | Halloween 2018", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "town_halloween";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Island Central | Halloween 2025", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "town_halloween";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Island Central | Halloween 2026", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "town_halloween";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Island Central | Rainbow", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "rainbow_town";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Mt. Blizzard | Blizzard Beach", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "blizzardbeach";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Halloween", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "halloween";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Frozen", StringComparison.OrdinalIgnoreCase))
            {
                string[] holidayIcons = { "holiday", "cfc", "olaf" };
                roomImage = holidayIcons[new System.Random().Next(holidayIcons.Length)];
                iconFound = true;
            }
            else if (iconMatchName.Contains("Holiday", StringComparison.OrdinalIgnoreCase))
            {
                string[] holidayIcons = { "holiday", "cfc", "olaf" };
                roomImage = holidayIcons[new System.Random().Next(holidayIcons.Length)];
                iconFound = true;
            }
            else if (iconMatchName.Contains("Rainbow Migration", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "rainbow";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Anniversary", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "anniversary";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Valentines", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "valentines";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Arcade", StringComparison.OrdinalIgnoreCase) ||
                     iconMatchName.Contains("JetPack", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "arcade";
                iconFound = true;
            }
            else if (iconMatchName.Contains("April", StringComparison.OrdinalIgnoreCase) ||
                     iconMatchName.Contains("???", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "box_dimension";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Waddle", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "sunset";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Splash", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "summersplash";
                iconFound = true;
            }
            else if (iconMatchName.Contains("wpd", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "wpd";
                iconFound = true;
            }
            else if (iconMatchName.Contains("World", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "wpd";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Medieval", StringComparison.OrdinalIgnoreCase) ||
                     iconMatchName.Contains("Dungeon", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "medieval";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Credits", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "credits";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Penglantian", StringComparison.OrdinalIgnoreCase) ||
                     iconMatchName.Contains("Pirate", StringComparison.OrdinalIgnoreCase) ||
                     iconMatchName.Contains("Expedition", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "penglantian";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Summit", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "summit";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Island Central", StringComparison.OrdinalIgnoreCase))
            {
                string[] IslandCentralIcons = { "island_central", "mapislandcentral" };
                roomImage = IslandCentralIcons[new System.Random().Next(IslandCentralIcons.Length)];
                iconFound = true;
            }
            else if (iconMatchName.Contains("Sea Caves", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "mapseacaves";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Boardwalk", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "mapboardwalk";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Cove", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "mapcove";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Mt. Blizzard", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "mapmtblizzard";
                iconFound = true;
            }
            else if (iconMatchName.Contains("Igloo", StringComparison.OrdinalIgnoreCase))
            {
                roomImage = "igloo";
                iconFound = true;
            }

            if (!iconFound)
            {
                int currentMonth = DateTime.Now.Month;
                switch (currentMonth)
                {
                    case 2:
                        roomImage = "valentines";
                        break;
                    case 10:
                        roomImage = "halloween";
                        break;
                    case 12:
                        string[] holidayIcons = { "holiday", "cfc", "olaf" };
                        roomImage = holidayIcons[new System.Random().Next(holidayIcons.Length)];
                        break;
                    default:
                        roomImage = iconMatchName.Contains("Island Central", StringComparison.OrdinalIgnoreCase) ? "island_central" : "default_icon";
                        break;
                }
            }
        }

        CurrentRoomName = detailsText;

        if (initialized && client != null)
        {
            string largeText = IsQuestActiveNow() ? GetQuestTitle() : GetPlatformLabel();
            UpdatePresence(stateText, roomImage, detailsText, largeText, gameStartTimeMs);
        }
    }

    private string GetQuestTitle()
    {
        QuestDefinition def;
        if (!TryGetActiveQuestDefinition(out def))
            return GetPlatformLabel();

        try
        {
            Localizer localizer = Service.Get<Localizer>();
            if (localizer != null && !string.IsNullOrEmpty(def.Title))
            {
                string localizedTitle = localizer.GetTokenTranslation(def.Title);
                return !string.IsNullOrEmpty(localizedTitle) ? localizedTitle : GetPlatformLabel();
            }
        }
        catch { }

        return GetPlatformLabel();
    }

    private string GetQuestMascotImageKey()
    {
        QuestDefinition def;
        if (!TryGetActiveQuestDefinition(out def))
            return null;

        string mascotName = "";
        if (def.Mascot != null)
            mascotName = def.Mascot.name ?? "";

        if (mascotName.Equals("AuntArctic", StringComparison.OrdinalIgnoreCase)) return "aa";
        if (mascotName.Equals("Rockhopper", StringComparison.OrdinalIgnoreCase)) return "rh";
        if (mascotName.Equals("Rookie", StringComparison.OrdinalIgnoreCase)) return "rk";
        if (mascotName.Equals("Scorn", StringComparison.OrdinalIgnoreCase)) return "sc";
        if (mascotName.Equals("DJCadence", StringComparison.OrdinalIgnoreCase)) return "dj";
        if (mascotName.Equals("DJCadence2", StringComparison.OrdinalIgnoreCase)) return "dj";
        if (mascotName.Equals("Dot", StringComparison.OrdinalIgnoreCase)) return "dt";
        if (mascotName.Equals("Dot2", StringComparison.OrdinalIgnoreCase)) return "dt";
        if (mascotName.Equals("Gary", StringComparison.OrdinalIgnoreCase)) return "gy";
        if (mascotName.Equals("Herbert", StringComparison.OrdinalIgnoreCase)) return "hb";
        if (mascotName.Equals("JetpackGuy", StringComparison.OrdinalIgnoreCase)) return "jg";
        if (mascotName.Equals("Rory", StringComparison.OrdinalIgnoreCase)) return "ry";
        if (mascotName.Equals("Rory2", StringComparison.OrdinalIgnoreCase)) return "ry";
        if (mascotName.Equals("Shellbeard", StringComparison.OrdinalIgnoreCase)) return "sb";

        return null;
    }

    private string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        int idx;

        idx = name.IndexOf("| On a Quest:", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0) name = name.Substring(0, idx);

        idx = name.IndexOf("| On a Quest:", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0) name = name.Substring(0, idx);

        idx = name.IndexOf("| On a Quest", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0) name = name.Substring(0, idx);

        idx = name.IndexOf("| On a Quest", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0) name = name.Substring(0, idx);

        return name.Trim().TrimEnd('|').Trim();
    }

    private string BuildTubeStateText()
    {
        try
        {
            if (TiltATubeController.IsActiveTiltATube)
            {
                return "Playing Tilt-o-Tube.";
            }
        }
        catch { }

        if (tubeLobbyActive)
            return "Tube Race | Lobby";

        if (tubeRaceActive)
        {
            if (tubeRaceType == PartyGameDefinition.GameTypes.TUBE_RACE_RED)
                return "Tube Race | Red";
            if (tubeRaceType == PartyGameDefinition.GameTypes.TUBE_RACE_BLUE)
                return "Tube Race | Blue";
            return "Tube Race";
        }

        if (tubeScoreValid)
        {
            int s = Mathf.RoundToInt(tubeScoreValue);
            return "Tube Race | Score " + s;
        }

        return "";
    }

    private string BuildMarketplaceStateText()
    {
        if (!marketplaceOpen || string.IsNullOrEmpty(marketplaceLabel))
            return "";

        if (string.Equals(marketplaceName, "Exchange", StringComparison.OrdinalIgnoreCase))
            return "Exchanging Collectibles";

        return marketplaceLabel + " | Browsing";
    }

    private bool TryGetPlayerStats(out int ageDays, out int level, out int coins)
    {
        ageDays = 0;
        level = 0;
        coins = 0;

        try
        {
            CPDataEntityCollection collection = Service.Get<CPDataEntityCollection>();
            if (collection == null)
                return false;

            DataEntityHandle h = collection.LocalPlayerHandle;
            if (h.IsNull)
                return false;

            ProfileData pd;
            if (collection.TryGetComponent<ProfileData>(h, out pd))
                ageDays = pd.PenguinAgeInDays;

            try
            {
                ProgressionService ps = Service.Get<ProgressionService>();
                if (ps != null)
                    level = ps.Level;
            }
            catch { }

            CoinsData cd;
            if (collection.TryGetComponent<CoinsData>(h, out cd))
                coins = cd.Coins;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private string BuildRotatingNonQuestNonTubeStateText()
    {
        if (!statsMode)
            return $"Unity {Application.unityVersion} | Version {Application.version}";

        int age, lvl, c;
        if (TryGetPlayerStats(out age, out lvl, out c))
            return "Age " + age + "d | Level " + lvl + " | Coins " + c;

        return $"Unity {Application.unityVersion} | Version {Application.version}";
    }

    private bool ShouldSwapStatsModeNow()
    {
        if (Time.unscaledTime < nextStatsSwapUnscaled)
            return false;

        if (marketplaceOpen)
            return false;

        if (IsQuestActiveNow())
            return false;

        if (tubeLobbyActive || tubeRaceActive || tubeScoreValid)
            return false;

        return true;
    }

    private string BuildStateText()
    {
        if (cfcDonationActive)
            return "Donating coins to CFC.";

        try
        {
            var cpDataEntityCollection = Service.Get<ClubPenguin.Core.CPDataEntityCollection>();
            if (cpDataEntityCollection != null && !cpDataEntityCollection.LocalPlayerHandle.IsNull)
            {
                var heldObjectsData = cpDataEntityCollection.GetComponent<ClubPenguin.HeldObjectsData>(cpDataEntityCollection.LocalPlayerHandle);
                if (heldObjectsData != null && heldObjectsData.HeldObject != null)
                {
                    string heldObjectId = heldObjectsData.HeldObject.ObjectId ?? "";
                    if (heldObjectId.Equals("FishingRod", StringComparison.OrdinalIgnoreCase) && currentBaseRoomName.Contains("Boardwalk", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Fishing at the docks.";
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
        }

        try
        {
            if (FishingController.IsActiveFishing && currentBaseRoomName.Contains("Boardwalk", StringComparison.OrdinalIgnoreCase))
            {
                return "Fishing at the docks.";
            }
        }
        catch { }

        string marketplace = BuildMarketplaceStateText();
        if (!string.IsNullOrEmpty(marketplace))
            return marketplace;

        if (IsQuestActiveNow())
        {
            QuestDefinition def;
            if (TryGetActiveQuestDefinition(out def))
            {
                string mascotName = (def.Mascot != null) ? (def.Mascot.name ?? "Unknown") : "Unknown";
                int chapter = def.ChapterNumber;
                int episode = def.QuestNumber;
                return "On a Quest | " + mascotName + " | C" + chapter + "E" + episode;
            }

            return "On a Quest";
        }

        string tube = BuildTubeStateText();
        if (!string.IsNullOrEmpty(tube))
            return tube;

        return BuildRotatingNonQuestNonTubeStateText();
    }

    private static string GetMarketplaceDisplayName(string marketplaceName)
    {
        string normalizedName = (marketplaceName ?? "").Trim();
        if (string.IsNullOrEmpty(normalizedName))
            return "";

        switch (normalizedName)
        {
            case "DisneyStore":
                return "Disney Shop";
            case "IgloosAndInteriors":
                return "Igloos & Interiors";
            case "FunHutMarket":
                return "Welcome Hut";
            case "FrankysPizza":
                return "Franky's";
            case "FishDogMarket":
                return "Foodtrekker";
            case "CampfireMarket":
                return "SS Convenience";
            case "CrystalCaveMarket":
                return "Deep Sea Shop";
            case "IglooMarket":
                return "Vending Machine";
            case "ChocoliteMarket":
                return "Snowmelt Shop";
            case "2017HalloweenMarket":
                return "Halloween Shop";
            case "Marketplace_Color2018":
                return "Migration Shop";
            case "Marketplace_Holiday2017":
                return "Holiday Shop";
        }

        string cleaned = normalizedName;

        if (cleaned.StartsWith("Marketplace_", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned.Substring("Marketplace_".Length);

        if (cleaned.EndsWith("Marketplace", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned.Substring(0, cleaned.Length - "Marketplace".Length);
        else if (cleaned.EndsWith("Market", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned.Substring(0, cleaned.Length - "Market".Length);

        cleaned = HumanizeIdentifier(cleaned);
        return string.IsNullOrEmpty(cleaned) ? normalizedName : cleaned;
    }

    private static string HumanizeIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        StringBuilder builder = new StringBuilder(value.Length + 8);

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];

            if (c == '_' || c == '-')
            {
                if (builder.Length > 0 && builder[builder.Length - 1] != ' ')
                    builder.Append(' ');

                continue;
            }

            if (i > 0)
            {
                char prev = value[i - 1];
                bool shouldInsertSpace = (char.IsUpper(c) && (char.IsLower(prev) || char.IsDigit(prev))) ||
                                         (char.IsDigit(c) && !char.IsDigit(prev));

                if (shouldInsertSpace && builder.Length > 0 && builder[builder.Length - 1] != ' ')
                    builder.Append(' ');
            }

            builder.Append(c);
        }

        return builder.ToString().Trim();
    }

    private string FormatIglooDisplayName(string rawSceneName)
    {
        if (string.IsNullOrEmpty(rawSceneName))
            return rawSceneName;

        if (rawSceneName.Contains("DefaultIgloo", StringComparison.OrdinalIgnoreCase))
            return "Igloos | Default Igloo";
        if (rawSceneName.Contains("ForestIgloo", StringComparison.OrdinalIgnoreCase))
            return "Igloos | Forest Igloo";
        if (rawSceneName.Contains("IslandIgloo", StringComparison.OrdinalIgnoreCase))
            return "Igloos | Island Igloo";

        return rawSceneName;
    }

    private string BuildDetailsText()
    {
        bool questActive = IsQuestActiveNow();

        if (LastLoadedSceneName.Contains("Igloo", StringComparison.OrdinalIgnoreCase))
            return FormatIglooDisplayName(LastLoadedSceneName);

        string baseRoom = NormalizeName(currentBaseRoomName);
        string additive = NormalizeName(currentAdditiveRoomName);

        string roomForDetails = baseRoom;

        if (!string.IsNullOrEmpty(areaNameOverride))
            roomForDetails = areaNameOverride;

        if (marketplaceOpen)
        {
            if (!string.IsNullOrEmpty(areaNameOverride))
                return roomForDetails;

            return !string.IsNullOrEmpty(additive) ? additive : baseRoom;
        }

        if (questActive && !string.IsNullOrEmpty(questOnlyDetailsOverride))
            return questOnlyDetailsOverride;

        if (questActive)
            return roomForDetails;

        if (!string.IsNullOrEmpty(areaNameOverride))
            return roomForDetails;

        return !string.IsNullOrEmpty(additive) ? additive : baseRoom;
    }

    private string BuildIconMatchName()
    {
        string baseRoom = NormalizeName(currentBaseRoomName);
        if (!string.IsNullOrEmpty(areaNameOverride))
            return areaNameOverride;

        if (marketplaceOpen)
        {
            string marketplaceAdditive = NormalizeName(currentAdditiveRoomName);
            return !string.IsNullOrEmpty(marketplaceAdditive) ? marketplaceAdditive : baseRoom;
        }

        if (IsQuestActiveNow())
            return baseRoom;

        string additive = NormalizeName(currentAdditiveRoomName);
        return !string.IsNullOrEmpty(additive) ? additive : baseRoom;
    }

    private void RefreshFromSceneState()
    {
        if (!string.IsNullOrEmpty(LastLoadedSceneName))
            currentBaseRoomName = NormalizeName(GetCustomSceneName(LastLoadedSceneName));
        else
            SyncSceneStateFromUnity();

        if (!string.IsNullOrEmpty(LastLoadedAdditiveSceneName))
            currentAdditiveRoomName = NormalizeName(GetCustomSceneName(LastLoadedAdditiveSceneName));
        else
            currentAdditiveRoomName = "";

        RefreshPresenceOnly();
    }

    private void SetBaseRoom(string roomName)
    {
        currentBaseRoomName = NormalizeName(roomName);
        currentAdditiveRoomName = "";
        questOnlyDetailsOverride = "";
        RefreshPresenceOnly();
    }

    private void SetAreaNameOverride(string areaName)
    {
        areaNameOverride = (areaName ?? "").Trim();
        RefreshPresenceOnly();
    }

    private void SetCFCDonationStatus(bool active)
    {
        cfcDonationActive = active;
        RefreshPresenceOnly();
    }

    public void SetRoom(string roomNameOrQuestOverride)
    {
        if (IsQuestActiveNow())
        {
            questOnlyDetailsOverride = (roomNameOrQuestOverride ?? "").Trim();
            RefreshPresenceOnly();
            return;
        }

        currentBaseRoomName = NormalizeName(roomNameOrQuestOverride);
        currentAdditiveRoomName = "";
        questOnlyDetailsOverride = "";
        RefreshPresenceOnly();
    }

    private void UpdatePresence(string state, string imageKey, string details, string largeText, ulong startTimestampMs)
    {
        if (client == null)
            return;

        if (startTimestampMs == 0UL)
        {
            startTimestampMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            gameStartTimeMs = startTimestampMs;
        }

        Activity activity = new Activity();
        activity.SetType(ActivityTypes.Playing);
        activity.SetState(state);
        activity.SetDetails(details);

        var assets = new ActivityAssets();
        assets.SetLargeImage(imageKey);
        assets.SetLargeText(largeText ?? GetPlatformLabel());
        activity.SetAssets(assets);

        var timestamps = new ActivityTimestamps();
        timestamps.SetStart(startTimestampMs);
        activity.SetTimestamps(timestamps);

        client.UpdateRichPresence(activity, OnUpdateRichPresence);
    }

    private void OnUpdateRichPresence(ClientResult result)
    {
        if (!result.Successful())
            UnityEngine.Debug.LogError($"[Discord Social SDK] Failed to update rich presence: {result.Error()}");
    }

    private bool IsDiscordRunning()
    {
        try
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("Discord");
            return processes.Any();
        }
        catch
        {
            return false;
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