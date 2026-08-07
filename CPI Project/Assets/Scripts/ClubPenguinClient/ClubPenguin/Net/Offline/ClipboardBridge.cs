#if UNITY_WEBGL

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ClubPenguin.Net.Offline
{
    public class ClipboardBridge : MonoBehaviour
    {
#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void CopyToClipboard(string text);

    [DllImport("__Internal")]
    private static extern void PasteFromClipboard(string gameObject, string method);
#endif

        public static ClipboardBridge Instance { get; private set; }
        public Action<string> PasteCompleted;


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

        public void Copy(string text)
        {
#if UNITY_WEBGL
        CopyToClipboard(text);
#else
            GUIUtility.systemCopyBuffer = text;
#endif
        }

        public void RequestPaste(Action<string> callback)
        {
            PasteCompleted = callback;

#if UNITY_WEBGL
        PasteFromClipboard(gameObject.name, nameof(OnClipboardText));
#else
            OnClipboardText(GUIUtility.systemCopyBuffer);
#endif
        }

        public void OnClipboardText(string text)
        {
            PasteCompleted?.Invoke(text);
            PasteCompleted = null;
        }
    }
}
#else
#endif
