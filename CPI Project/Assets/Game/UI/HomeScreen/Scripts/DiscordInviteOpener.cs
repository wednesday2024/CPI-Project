using System;
using System.Collections;
using System.IO;
using UnityEngine;

#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
using System.Diagnostics;
#endif

public class DiscordInviteOpener : MonoBehaviour
{
    [Header("Invite")]
    [SerializeField] private string inviteUrl = "https://discord.gg/jkWbd3uqTS";

    [Header("App first behavior")]
    [Tooltip("If Discord appears installed, try opening the invite in the Discord app first.")]
    [SerializeField] private bool preferDiscordAppIfInstalled = true;

    [Header("Optional safety fallback")]
    [Tooltip("If we tried the app but your game never lost focus, open the website after this delay.")]
    [SerializeField] private bool fallbackToWebIfAppDidNotOpen = true;

    [Min(0f)]
    [SerializeField] private float fallbackDelaySeconds = 1.25f;

    private bool attemptedAppThisClick;
    private bool lostFocusSinceAttempt;

    public void OpenDiscordInvite()
    {
        string webUrl = NormalizeWebInviteUrl(inviteUrl);
        if (string.IsNullOrWhiteSpace(webUrl))
        {
            Application.OpenURL("https://discord.com/");
            return;
        }

        attemptedAppThisClick = false;
        lostFocusSinceAttempt = false;

        bool canDetect = CanReasonablyDetectDiscordInstalled();
        bool isInstalled = canDetect && IsDiscordInstalled();

        if (preferDiscordAppIfInstalled && isInstalled)
        {
            attemptedAppThisClick = true;

            string code = ExtractInviteCode(webUrl);
            string deepLink = BuildDiscordDeepLink(code);

            if (!string.IsNullOrWhiteSpace(deepLink))
            {
                Application.OpenURL(deepLink);

                if (fallbackToWebIfAppDidNotOpen && fallbackDelaySeconds > 0f)
                {
                    StopAllCoroutines();
                    StartCoroutine(FallbackToWebIfStillFocused(webUrl, fallbackDelaySeconds));
                }

                return;
            }
        }

        Application.OpenURL(webUrl);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (attemptedAppThisClick && !hasFocus)
        {
            lostFocusSinceAttempt = true;
        }
    }

    private IEnumerator FallbackToWebIfStillFocused(string webUrl, float delaySeconds)
    {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < delaySeconds)
            yield return null;

        if (!lostFocusSinceAttempt)
        {
            Application.OpenURL(webUrl);
        }
    }

    private static string NormalizeWebInviteUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        url = url.Trim();

        if (!url.Contains("://") && !url.Contains("/"))
            return "https://discord.gg/" + url;

        if (url.StartsWith("discord://", StringComparison.OrdinalIgnoreCase))
        {
            string code = ExtractInviteCode(url);
            return string.IsNullOrWhiteSpace(code) ? "https://discord.gg/" : "https://discord.gg/" + code;
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url.Substring("http://".Length);

        if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return null;

        return url;
    }

    private static string ExtractInviteCode(string invite)
    {
        if (string.IsNullOrWhiteSpace(invite))
            return null;

        invite = invite.Trim();

        try
        {
            if (!invite.Contains("://"))
            {
                return invite;
            }

            if (invite.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                Uri uri = new Uri(invite);
                string path = uri.AbsolutePath.Trim('/');
                if (string.IsNullOrWhiteSpace(path))
                    return null;

                string[] parts = path.Split('/');
                if (parts.Length == 0)
                    return null;

                if (parts.Length >= 2 && parts[0].Equals("invite", StringComparison.OrdinalIgnoreCase))
                    return parts[1];

                return parts[parts.Length - 1];
            }

            int lastSlash = invite.LastIndexOf('/');
            if (lastSlash >= 0 && lastSlash + 1 < invite.Length)
                return invite.Substring(lastSlash + 1).Trim();
        }
        catch
        {
        }

        return null;
    }

    private static string BuildDiscordDeepLink(string inviteCode)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            return null;

        inviteCode = inviteCode.Trim();
        return "discord://-/invite/" + inviteCode;
    }

    private static bool CanReasonablyDetectDiscordInstalled()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        return true;
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        return true;
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        return true;
#else
        return false;
#endif
    }

    private static bool IsDiscordInstalled()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        return IsDiscordInstalled_Windows();
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        return IsDiscordInstalled_Mac();
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        return IsDiscordInstalled_Linux();
#else
        return false;
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private static bool IsDiscordInstalled_Windows()
    {
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                if (LooksLikeDiscordInstallFolder(Path.Combine(localAppData, "Discord"))) return true;
                if (LooksLikeDiscordInstallFolder(Path.Combine(localAppData, "DiscordPTB"))) return true;
                if (LooksLikeDiscordInstallFolder(Path.Combine(localAppData, "DiscordCanary"))) return true;
            }

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                if (Directory.Exists(Path.Combine(programFiles, "Discord"))) return true;
                if (Directory.Exists(Path.Combine(programFiles, "DiscordPTB"))) return true;
                if (Directory.Exists(Path.Combine(programFiles, "DiscordCanary"))) return true;
            }

            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrWhiteSpace(programFilesX86))
            {
                if (Directory.Exists(Path.Combine(programFilesX86, "Discord"))) return true;
                if (Directory.Exists(Path.Combine(programFilesX86, "DiscordPTB"))) return true;
                if (Directory.Exists(Path.Combine(programFilesX86, "DiscordCanary"))) return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool LooksLikeDiscordInstallFolder(string baseDir)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(baseDir) || !Directory.Exists(baseDir))
                return false;

            if (File.Exists(Path.Combine(baseDir, "Update.exe")))
                return true;

            string[] appDirs = Directory.GetDirectories(baseDir, "app-*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < appDirs.Length; i++)
            {
                string exePath = Path.Combine(appDirs[i], "Discord.exe");
                if (File.Exists(exePath))
                    return true;
            }
        }
        catch
        {
        }

        return false;
    }
#endif

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    private static bool IsDiscordInstalled_Mac()
    {
        try
        {
            if (Directory.Exists("/Applications/Discord.app")) return true;
            if (Directory.Exists("/Applications/Discord PTB.app")) return true;
            if (Directory.Exists("/Applications/Discord Canary.app")) return true;

            string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (!string.IsNullOrWhiteSpace(home))
            {
                string userApps = Path.Combine(home, "Applications");
                if (Directory.Exists(Path.Combine(userApps, "Discord.app"))) return true;
                if (Directory.Exists(Path.Combine(userApps, "Discord PTB.app"))) return true;
                if (Directory.Exists(Path.Combine(userApps, "Discord Canary.app"))) return true;
            }
        }
        catch
        {
        }

        return false;
    }
#endif

#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
    private static bool IsDiscordInstalled_Linux()
    {
        try
        {
            if (WhichExists("discord")) return true;
            if (WhichExists("discord-ptb")) return true;
            if (WhichExists("discord-canary")) return true;

            if (File.Exists("/usr/share/applications/discord.desktop")) return true;
            if (File.Exists("/usr/share/applications/discord-ptb.desktop")) return true;
            if (File.Exists("/usr/share/applications/discord-canary.desktop")) return true;

            string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (!string.IsNullOrWhiteSpace(home))
            {
                string localApps = Path.Combine(home, ".local/share/applications/discord.desktop");
                if (File.Exists(localApps)) return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool WhichExists(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var p = Process.Start(psi))
            {
                if (p == null) return false;

                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(200);

                return p.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
            }
        }
        catch
        {
            return false;
        }
    }
#endif
}
