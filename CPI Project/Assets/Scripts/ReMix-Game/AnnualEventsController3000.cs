using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using Disney.Kelowna.Common;

public class AnnualEventsController3000 : MonoBehaviour
{
    public static AnnualEventsController3000 Instance { get; private set; }

    private enum OverrideMode
    {
        Automatic = 0,
        ForceRegular = 1,
        ForceEvent = 2
    }

    private const string PLAYERPREFS_OVERRIDE_MODE_KEY = "AnnualEventsController.OverrideMode";
    private const string PLAYERPREFS_FORCED_EVENT_KEY = "AnnualEventsController.ForcedEventKey";

    private OverrideMode overrideMode = OverrideMode.Automatic;
    private string forcedEventKey = null;

    private static string GetPlatformKey(string key)
    {
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        if (Application.isEditor)
        {
            return "Editor_" + key;
        }
#endif
        return key;
    }

    [Serializable]
    public class EventInfo
    {
        [Tooltip("Use the event ID from the ScheduledEventDate asset. (example: date_22_halloween2018 results to ID 22.)")]
        public string eventID;

        [Tooltip("Use the ScheduledEventDate name that appears in the embedded_content_manifest.txt (example: date_22_halloween2018 results to halloween2018).")]
        public string eventName;

        [Tooltip("If enabled, this event will be excluded from annual events. Use this for the party switcher.")]
        public bool excludeFromDateSystem = false;

        [Tooltip("Start date of the event (set at midnight). Example: October 31 00:00.")]
        public int startMonth;
        public int startDay;

        [Tooltip("End date of the event (also set at midnight). To keep the event active through November 1st, set this to November 2 00:00.")]
        public int endMonth;
        public int endDay;

        [Tooltip("Map scene names to event specific audio keys.")]
        public SceneAudioMapping[] SceneAudioMappings;

        [Tooltip("Optional material swap prefab for this event.")]
        public GameObject SnowballPrefab;
        public Material OriginalMaterial;
        public Material EventMaterial;
    }

    [Serializable]
    public class SceneAudioMapping
    {
        [Tooltip("Scene name this mapping applies to.")]
        public string SceneName;

        [Tooltip("Audio key used when the event is not active.")]
        public string DefaultAudioKey;

        [Tooltip("Audio key used while the event is active.")]
        public string EventAudioKey;
    }

    [Header("Annual events (loops every year).")]
    public EventInfo[] events;


    public bool IsPartySwitchForced
    {
        get
        {
            if (overrideMode != OverrideMode.ForceEvent)
            {
                return false;
            }

            if (string.IsNullOrEmpty(forcedEventKey))
            {
                return false;
            }

            if (events == null)
            {
                return false;
            }

            for (int i = 0; i < events.Length; i++)
            {
                var e = events[i];
                if (e == null)
                {
                    continue;
                }

                if (!e.excludeFromDateSystem)
                {
                    continue;
                }

                string key = BuildEventKey(e);
                if (forcedEventKey == key || forcedEventKey == e.eventName || forcedEventKey == e.eventID)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private Dictionary<string, string> activeSceneKeys = new Dictionary<string, string>();
    private HashSet<string> seenScenes = new HashSet<string>();
    private float refreshInterval = 900f;
    private float nextRefreshTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadOverrideState();

        if (events.Length == 0)
        {
            Debug.LogError("No events configured.");
            return;
        }

        ApplyEvents();
        nextRefreshTime = Time.time + refreshInterval;
    }

    private void Update()
    {
        if (Time.time >= nextRefreshTime)
        {
            ApplyEvents();
            nextRefreshTime = Time.time + refreshInterval;
        }
    }

    public void ForceStartParty()
    {
        if (events == null || events.Length == 0)
        {
            Debug.LogError("No events configured.");
            return;
        }

        overrideMode = OverrideMode.ForceEvent;

        string best = FindBestEventKeyToForce(DateTimeOffset.UtcNow, DateTime.UtcNow.Year);
        
        if (string.IsNullOrEmpty(best))
        {
            foreach (var e in events)
            {
                if (e != null && !e.excludeFromDateSystem)
                {
                    forcedEventKey = BuildEventKey(e);
                    break;
                }
            }
            if (string.IsNullOrEmpty(forcedEventKey) && events.Length > 0)
            {
                forcedEventKey = BuildEventKey(events[0]);
            }
        }
        else
        {
            forcedEventKey = best;
        }

        SaveOverrideState();
        ApplyEvents();
    }

    public void ForceEndParty()
    {
        overrideMode = OverrideMode.ForceRegular;
        SaveOverrideState();
        ApplyEvents();
    }

    public void SetDefaultMode()
    {
        overrideMode = OverrideMode.Automatic;
        SaveOverrideState();
        ApplyEvents();
    }

    public void ForceParty(string eventID, string eventName)
    {
        overrideMode = OverrideMode.ForceEvent;
        forcedEventKey = (eventID ?? "") + "|" + (eventName ?? "");
        SaveOverrideState();
        ApplyEvents();
    }

    public void ForcePartyKey(string partyKey)
    {
        if (string.IsNullOrEmpty(partyKey))
        {
            Debug.LogError("ForcePartyKey called with empty partyKey");
            return;
        }

        string targetKey = partyKey;

        if (events != null)
        {
            foreach (var e in events)
            {
                if (e == null)
                {
                    continue;
                }

                string key = BuildEventKey(e);
                if (key == partyKey)
                {
                    targetKey = key;
                    break;
                }

                if (!string.IsNullOrEmpty(e.eventName) && e.eventName == partyKey)
                {
                    targetKey = key;
                    break;
                }

                if (!string.IsNullOrEmpty(e.eventID) && e.eventID == partyKey)
                {
                    targetKey = key;
                    break;
                }
            }
        }

        overrideMode = OverrideMode.ForceEvent;
        forcedEventKey = targetKey;
        SaveOverrideState();
        ApplyEvents();
    }

    private void LoadOverrideState()
    {
        int mode = PlayerPrefs.GetInt(GetPlatformKey(PLAYERPREFS_OVERRIDE_MODE_KEY), 0);
        if (mode < 0 || mode > 2)
        {
            mode = 0;
        }
        overrideMode = (OverrideMode)mode;

        forcedEventKey = PlayerPrefs.GetString(GetPlatformKey(PLAYERPREFS_FORCED_EVENT_KEY), null);
        if (string.IsNullOrEmpty(forcedEventKey))
        {
            forcedEventKey = null;
        }
    }

    private void SaveOverrideState()
    {
        PlayerPrefs.SetInt(GetPlatformKey(PLAYERPREFS_OVERRIDE_MODE_KEY), (int)overrideMode);

        if (string.IsNullOrEmpty(forcedEventKey))
        {
            PlayerPrefs.DeleteKey(GetPlatformKey(PLAYERPREFS_FORCED_EVENT_KEY));
        }
        else
        {
            PlayerPrefs.SetString(GetPlatformKey(PLAYERPREFS_FORCED_EVENT_KEY), forcedEventKey);
        }

        PlayerPrefs.Save();
    }

    private string BuildEventKey(EventInfo info)
    {
        if (info == null)
        {
            return null;
        }
        string id = info.eventID ?? "";
        string name = info.eventName ?? "";
        return id + "|" + name;
    }

    private string FindBestEventKeyToForce(DateTimeOffset nowUtc, int currentYear)
    {
        if (events == null || events.Length == 0)
        {
            return null;
        }

        foreach (var eventInfo in events)
        {
            if (eventInfo == null)
            {
                continue;
            }

            if (eventInfo.excludeFromDateSystem)
            {
                continue;
            }

            int startYear = currentYear;
            int endYear = (eventInfo.endMonth < eventInfo.startMonth) ? currentYear + 1 : currentYear;

            DateTimeOffset startDateTime;
            DateTimeOffset endDateTime;

            try
            {
                startDateTime = new DateTimeOffset(startYear, eventInfo.startMonth, eventInfo.startDay, 0, 0, 0, TimeSpan.Zero);
                endDateTime = new DateTimeOffset(endYear, eventInfo.endMonth, eventInfo.endDay, 0, 0, 0, TimeSpan.Zero);
            }
            catch
            {
                continue;
            }

            bool active = nowUtc >= startDateTime && nowUtc < endDateTime;
            if (active)
            {
                return BuildEventKey(eventInfo);
            }
        }

        return BuildEventKey(events[0]);
    }

    private void ApplyEvents()
    {
        activeSceneKeys.Clear();
        seenScenes.Clear();

        int currentYear = DateTime.UtcNow.Year;
        DateTimeOffset currentDateUtc = DateTimeOffset.UtcNow;

        if (overrideMode == OverrideMode.ForceEvent && string.IsNullOrEmpty(forcedEventKey) && events != null && events.Length > 0)
        {
            foreach (var e in events)
            {
                if (e != null && !e.excludeFromDateSystem)
                {
                    forcedEventKey = BuildEventKey(e);
                    break;
                }
            }
            if (string.IsNullOrEmpty(forcedEventKey))
            {
                forcedEventKey = BuildEventKey(events[0]);
            }
            SaveOverrideState();
        }

        foreach (var eventInfo in events)
        {
            string currentEventKey = BuildEventKey(eventInfo);
            
            if (eventInfo.excludeFromDateSystem && overrideMode == OverrideMode.ForceEvent && currentEventKey != forcedEventKey)
            {
                continue;
            }

            string path = $"definitions/scheduledeventdates/date_{eventInfo.eventID}_{eventInfo.eventName}";
            var eventAsset = Resources.Load<ScriptableObject>(path);
            
            if (eventAsset == null)
            {
                path = $"definitions/scheduledeventdates/holiday/date_{eventInfo.eventID}_{eventInfo.eventName}";
                eventAsset = Resources.Load<ScriptableObject>(path);
            }

            if (eventAsset != null)
            {
                var type = eventAsset.GetType();
                var datesField = type.GetField("Dates", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                var datesValue = datesField.GetValue(eventAsset);
                if (datesValue != null)
                {
                    var startDateField = datesValue.GetType().GetField("StartDate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var endDateField = datesValue.GetType().GetField("EndDate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (startDateField != null && endDateField != null)
                    {
                        var startDate = startDateField.GetValue(datesValue);
                        var endDate = endDateField.GetValue(datesValue);

                        if (startDate != null && endDate != null)
                        {
                            bool isEventActive = false;
                            
                            if (overrideMode == OverrideMode.ForceEvent && currentEventKey == forcedEventKey)
                            {
                                isEventActive = true;
                            }
                            else if (overrideMode == OverrideMode.ForceRegular)
                            {
                                isEventActive = false;
                            }
                            else
                            {
                                isEventActive = false;
                            }
                            
                            int startYear, endYear;
                            int startMonth, startDay, endMonth, endDay;
                            
                            if (overrideMode == OverrideMode.ForceEvent)
                            {
                                if (isEventActive)
                                {
                                    DateTime now = DateTime.UtcNow;
                                    startYear = now.Year;
                                    startMonth = now.Month;
                                    startDay = now.Day;
                                    
                                    DateTime endDateCalc = now.AddDays(2);
                                    endYear = endDateCalc.Year;
                                    endMonth = endDateCalc.Month;
                                    endDay = endDateCalc.Day;
                                }
                                else
                                {
                                    startYear = 2024;
                                    startMonth = 1;
                                    startDay = 1;
                                    endYear = 2024;
                                    endMonth = 2;
                                    endDay = 1;
                                }
                            }
                            else if (overrideMode == OverrideMode.ForceRegular)
                            {
                                startYear = 2024;
                                startMonth = 1;
                                startDay = 1;
                                endYear = 2024;
                                endMonth = 2;
                                endDay = 1;
                            }
                            else if (eventInfo.excludeFromDateSystem)
                            {
                                startYear = 2024;
                                startMonth = 1;
                                startDay = 1;
                                endYear = 2024;
                                endMonth = 2;
                                endDay = 1;
                            }
                            else
                            {
                                startYear = currentYear;
                                endYear = (eventInfo.endMonth < eventInfo.startMonth) ? currentYear + 1 : currentYear;
                                startMonth = eventInfo.startMonth;
                                startDay = eventInfo.startDay;
                                endMonth = eventInfo.endMonth;
                                endDay = eventInfo.endDay;
                            }

                            SetFieldOrPropertyValue(startDate, "day", startDay);
                            SetFieldOrPropertyValue(startDate, "month", startMonth);
                            SetFieldOrPropertyValue(startDate, "year", startYear);

                            SetFieldOrPropertyValue(endDate, "day", endDay);
                            SetFieldOrPropertyValue(endDate, "month", endMonth);
                            SetFieldOrPropertyValue(endDate, "year", endYear);

                            DateTimeOffset startDateTime = new DateTimeOffset(startYear, startMonth, startDay, 0, 0, 0, TimeSpan.Zero);
                            DateTimeOffset endDateTime = new DateTimeOffset(endYear, endMonth, endDay, 0, 0, 0, TimeSpan.Zero);

                            long startTimestamp = startDateTime.ToUnixTimeMilliseconds();
                            long endTimestamp = endDateTime.ToUnixTimeMilliseconds();

                            SetFieldOrPropertyValue(startDate, "TimeStampInMilliseconds", startTimestamp);
                            SetFieldOrPropertyValue(endDate, "TimeStampInMilliseconds", endTimestamp);

                            bool scheduleActive = currentDateUtc >= startDateTime && currentDateUtc < endDateTime;

                            bool isActive;
                            if (overrideMode == OverrideMode.ForceRegular)
                            {
                                isActive = false;
                            }
                            else if (overrideMode == OverrideMode.ForceEvent)
                            {
                                isActive = (BuildEventKey(eventInfo) == forcedEventKey);
                            }
                            else
                            {
                                isActive = scheduleActive;
                            }

                            foreach (var mapping in eventInfo.SceneAudioMappings)
                            {
                                if (isActive)
                                {
                                    activeSceneKeys[mapping.SceneName] = mapping.EventAudioKey;
                                }
                                else
                                {
                                    if (!activeSceneKeys.ContainsKey(mapping.SceneName))
                                    {
                                        activeSceneKeys[mapping.SceneName] = mapping.DefaultAudioKey;
                                    }
                                }
                                seenScenes.Add(mapping.SceneName);
                            }

                            if (eventInfo.SnowballPrefab != null)
                            {
                                var tr = eventInfo.SnowballPrefab.GetComponent<TrailRenderer>();
                                if (tr != null)
                                {
                                    tr.material = isActive ? eventInfo.EventMaterial : eventInfo.OriginalMaterial;
                                    Debug.Log($"Material set on prefab '{eventInfo.SnowballPrefab.name}' for eventID '{eventInfo.eventID}' ({eventInfo.eventName}) Active: {isActive}");
                                }
                            }

                            Debug.Log($"Updated event {eventInfo.eventID} ({eventInfo.eventName}) dates. Active: {isActive}");
                        }
                    }
                }
            }
        }

        ApplySceneAudioKeys();
    }

    private void ApplySceneAudioKeys()
    {
        foreach (var sceneName in seenScenes)
        {
            string scenePath = $"definitions/scene/scene_{sceneName}";
            var sceneAsset = Resources.Load<ScriptableObject>(scenePath);

            if (sceneAsset != null)
            {
                var type = sceneAsset.GetType();
                var audioKeyField = type.GetField("SceneAudioContentKey", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (audioKeyField != null)
                {
                    string keyToUse = activeSceneKeys.ContainsKey(sceneName) ? activeSceneKeys[sceneName] : null;
                    if (!string.IsNullOrEmpty(keyToUse))
                    {
                        var contentKey = new PrefabContentKey(keyToUse);
                        contentKey.Key = keyToUse;
                        audioKeyField.SetValue(sceneAsset, contentKey);
                        Debug.Log($"Scene '{sceneName}' audio key set to '{keyToUse}'");
                    }
                }
                else
                {
                    Debug.LogError($"SceneAudioContentKey field not found in scene '{sceneName}'");
                }
            }
            else
            {
                Debug.LogError($"Scene asset not found at path: {scenePath}");
            }
        }
    }

    private void SetFieldOrPropertyValue(object target, string name, object newValue)
    {
        var type = target.GetType();

        var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, newValue);
            return;
        }

        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null && property.CanWrite)
        {
            property.SetValue(target, newValue);
            return;
        }

        Debug.LogError($"Field or property {name} not found in {type.Name}!");
    }
}

/* Example:
   Halloween Event
   Start Date: October 31 at midnight (00:00 UTC)
   End Date: November 2 at midnight (00:00 UTC)
   This means the event is active for the full day of October 31 and November 1.
*/
