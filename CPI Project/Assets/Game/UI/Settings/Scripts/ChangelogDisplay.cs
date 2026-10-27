using ClubPenguin.Accessibility;
using Disney.LaunchPadFramework;
using Disney.MobileNetwork;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChangelogDisplay : MonoBehaviour
{
    private sealed class ScaledText
    {
        public TMP_Text Text;
        public float BaseFontSize;
    }

    private sealed class ScaledEntry
    {
        public LayoutElement Layout;
        public TMP_Text Notes;
    }

    [Serializable]
    private class StringValue
    {
        public string value;
    }

    public GameObject entryGameObject;
    public TMP_Text dateText;
    public TMP_Text notesText;

    [Header("Entry Colors")]
    public GameObject colorObjectOne;
    public GameObject colorObjectTwo;
    public Color colorOne = new Color32(0x71, 0xC3, 0xFF, 0xFF);
    public Color colorTwo = new Color32(0x9C, 0xD5, 0xFF, 0xFF);

    private readonly List<ScaledText> scaledTexts = new List<ScaledText>();
    private readonly List<ScaledEntry> scaledEntries = new List<ScaledEntry>();
    private EventDispatcher eventDispatcher;
    private float accessibilityScale = 1f;

    private void Start()
    {
        accessibilityScale = PlayerPrefs.GetFloat(
            GetPlatformKey(AccessibilityConstants.SCALE_PLAYERPREFS_KEY),
            AccessibilityConstants.SCALE_MEDIUM);
        eventDispatcher = Service.Get<EventDispatcher>();
        eventDispatcher.AddListener<AccessibilityEvents.AccessibilityScaleUpdated>(onAccessibilityScaleUpdated);

        TMP_Text[] screenTexts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in screenTexts)
        {
            if (entryGameObject == null || !text.transform.IsChildOf(entryGameObject.transform))
                RegisterText(text);
        }

        TextAsset json = Resources.Load<TextAsset>("Changelog/Changelog");
        if (json == null || entryGameObject == null || dateText == null || notesText == null)
            return;

        List<KeyValuePair<string, string>> entries = ParseEntries(json.text);
        entries.Reverse();
        Transform parent = entryGameObject.transform.parent;
        entryGameObject.SetActive(false);

        string datePath = GetRelativePath(entryGameObject.transform, dateText.transform);
        string notesPath = GetRelativePath(entryGameObject.transform, notesText.transform);
        string colorObjectOnePath = GetRelativePath(entryGameObject.transform, colorObjectOne != null ? colorObjectOne.transform : null);
        string colorObjectTwoPath = GetRelativePath(entryGameObject.transform, colorObjectTwo != null ? colorObjectTwo.transform : null);
        bool lastEntryUsedColorOne = true;

        for (int i = 0; i < entries.Count; i++)
        {
            GameObject instance = Instantiate(entryGameObject, parent);
            TMP_Text instanceDate = FindText(instance.transform, datePath);
            TMP_Text instanceNotes = FindText(instance.transform, notesPath);
            bool useColorOne = !lastEntryUsedColorOne;
            Color rowColor = useColorOne ? colorOne : colorTwo;
            ApplyImageColor(instance.transform, colorObjectOnePath, rowColor);
            ApplyImageColor(instance.transform, colorObjectTwoPath, rowColor);
            lastEntryUsedColorOne = useColorOne;
            
            if (instanceDate != null)
            {
                instanceDate.text = entries[i].Key;
                RegisterText(instanceDate);
            }
            if (instanceNotes != null)
            {
                instanceNotes.text = entries[i].Value;
                RegisterText(instanceNotes);
            }

            instance.SetActive(true);
            LayoutElement layout = instance.GetComponent<LayoutElement>();
            if (instanceNotes != null && layout != null)
                scaledEntries.Add(new ScaledEntry { Layout = layout, Notes = instanceNotes });
        }

        ApplyAccessibilityScale(accessibilityScale);
    }

    private void OnDestroy()
    {
        if (eventDispatcher != null)
            eventDispatcher.RemoveListener<AccessibilityEvents.AccessibilityScaleUpdated>(onAccessibilityScaleUpdated);
    }

    private bool onAccessibilityScaleUpdated(AccessibilityEvents.AccessibilityScaleUpdated evt)
    {
        accessibilityScale = Mathf.Clamp(
            evt.Scale,
            AccessibilityConstants.SCALE_SMALL,
            AccessibilityConstants.SCALE_LARGE);
        ApplyAccessibilityScale(accessibilityScale);
        return false;
    }

    private void RegisterText(TMP_Text text)
    {
        if (text == null)
            return;

        scaledTexts.Add(new ScaledText { Text = text, BaseFontSize = text.fontSize });
    }

    private void ApplyAccessibilityScale(float scale)
    {
        scale = Mathf.Clamp(scale, AccessibilityConstants.SCALE_SMALL, AccessibilityConstants.SCALE_LARGE);
        foreach (ScaledText scaledText in scaledTexts)
        {
            if (scaledText.Text != null)
                scaledText.Text.fontSize = scaledText.BaseFontSize * scale;
        }

        foreach (ScaledEntry entry in scaledEntries)
            UpdateEntryLayout(entry);

        RectTransform layoutRoot = entryGameObject != null && entryGameObject.transform.parent != null
            ? entryGameObject.transform.parent as RectTransform
            : transform as RectTransform;
        if (layoutRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }

    private static void UpdateEntryLayout(ScaledEntry entry)
    {
        if (entry.Layout == null || entry.Notes == null)
            return;

        entry.Notes.ForceMeshUpdate();
        float notesHeight = entry.Notes.GetPreferredValues(entry.Notes.text, entry.Notes.rectTransform.rect.width, 0f).y;
        float notesBottomY = Mathf.Abs(entry.Notes.rectTransform.anchoredPosition.y) + notesHeight;
        entry.Layout.minHeight = notesBottomY + 20f;
    }

    private static string GetPlatformKey(string key)
    {
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        if (Application.isEditor)
            return "Editor_" + key;
#endif
        return key;
    }

    private static TMP_Text FindText(Transform root, string path)
    {
        Transform target = path == "." ? root : root.Find(path);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static void ApplyImageColor(Transform root, string path, Color color)
    {
        if (path == null)
            return;

        Transform target = path == "." ? root : root.Find(path);
        if (target == null)
            return;

        Image image = target.GetComponent<Image>();
        if (image == null)
            image = target.GetComponentInChildren<Image>(true);
        if (image != null)
            image.color = color;
    }

    private static string GetRelativePath(Transform root, Transform target)
    {
        if (target == null)
            return null;
        if (root == target)
            return ".";

        string path = target.name;
        Transform current = target.parent;
        while (current != null && current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return current == root ? path : null;
    }

    private static List<KeyValuePair<string, string>> ParseEntries(string json)
    {
        List<KeyValuePair<string, string>> entries = new List<KeyValuePair<string, string>>();
        int position = 0;
        while (position < json.Length)
        {
            SkipSeparators(json, ref position);
            if (position >= json.Length || json[position] == '}')
                break;

            string date = ReadString(json, ref position);
            SkipWhitespace(json, ref position);
            if (position >= json.Length || json[position++] != ':')
                break;
            SkipWhitespace(json, ref position);
            string notes = ReadString(json, ref position);
            entries.Add(new KeyValuePair<string, string>(date, notes));
        }
        return entries;
    }

    private static void SkipSeparators(string json, ref int position)
    {
        while (position < json.Length && (char.IsWhiteSpace(json[position]) || json[position] == '{' || json[position] == ','))
            position++;
    }

    private static void SkipWhitespace(string json, ref int position)
    {
        while (position < json.Length && char.IsWhiteSpace(json[position]))
            position++;
    }

    private static string ReadString(string json, ref int position)
    {
        if (position >= json.Length || json[position] != '"')
            return string.Empty;

        int start = position++;
        bool escaped = false;
        while (position < json.Length)
        {
            char current = json[position++];
            if (current == '"' && !escaped)
                break;
            if (current == '\\' && !escaped)
                escaped = true;
            else
                escaped = false;
        }

        StringValue decoded = JsonUtility.FromJson<StringValue>("{\"value\":" + json.Substring(start, position - start) + "}");
        return decoded != null ? decoded.value : string.Empty;
    }
}
