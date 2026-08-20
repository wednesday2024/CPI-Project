// FileName: /StreamingAssetBundleDevice.cs
// FileContents:

#if UNITY_ANDROID
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Disney.Kelowna.Common
{
    public class StreamingAssetBundleDevice : Device
    {
        private string streamingAssetsPath;

        public const string DEVICE_TYPE = "sa-bundle";

        public override string DeviceType
        {
            get
            {
                return "sa-bundle";
            }
        }

        public StreamingAssetBundleDevice(DeviceManager deviceManager)
            : base(deviceManager)
        {
            // For Android, assets are typically accessed via JAR file paths or OBBs.
            // This path construction is specific to how Android handles streaming assets.
            streamingAssetsPath = "jar:file://" + Application.dataPath + "!/assets";
            Debug.Log("Android StreamingAssets Path: " + streamingAssetsPath);
        }

        public override AssetRequest<TAsset> LoadAsync<TAsset>(string deviceList, ref ContentManifest.AssetEntry entry, AssetLoadedHandler<TAsset> handler = null)
        {
            StreamingAssetBundleWrapper streamingAssetBundleWrapper = new StreamingAssetBundleWrapper();
            AsyncStreamingAssetBundleRequest<TAsset> result = new AsyncStreamingAssetBundleRequest<TAsset>(entry.Key, streamingAssetBundleWrapper);
            CoroutineRunner.StartPersistent(loadBundleFromStreamingAssets(entry, streamingAssetBundleWrapper, handler), this, "loadBundleFromStreamingAssets");
            return result;
        }

        private IEnumerator loadBundleFromStreamingAssets<TAsset>(ContentManifest.AssetEntry entry, StreamingAssetBundleWrapper wrapper, AssetLoadedHandler<TAsset> handler) where TAsset : class
        {
            string key = entry.Key;

            // Validate the entry key
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Asset entry key is null or empty.");
                yield break; // Exit the coroutine if the key is invalid
            }

            // Construct the URL for Android. Path.Combine handles platform-specific separators.
            // Ensure 'key' is correctly formatted for the asset bundle path.
            string url = Path.Combine(streamingAssetsPath, key + ".txt");
            Debug.Log("Loading asset from URL (Android): " + url);

            wrapper.LoadFromDownload(url);
            yield return wrapper.WebRequest;
            AssetBundle assetBundle = wrapper.AssetBundle;
            if (handler != null)
            {
                handler(key, (TAsset)(object)assetBundle);
            }
            yield return null;
        }

        public override TAsset LoadImmediate<TAsset>(string deviceList, ref ContentManifest.AssetEntry entry)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            // In a browser StreamingAssets is an http URL, so there is nothing to read
            // synchronously. Callers have to go through LoadAsync
            throw new InvalidOperationException("Streaming asset bundles must be loaded asynchronously.");
#else
            // On a desktop player StreamingAssets is a plain directory, so a bundle that
            // is not mounted yet can be read right here. Refusing to do so is what makes
            // Content.LoadImmediate fail for anything living in a bundle: whether it
            // worked depended on the zone the player happened to be standing in.
            // BundleDevice mounts whatever comes back, so the file is opened once
            string path = Path.Combine(Application.streamingAssetsPath, entry.Key + ".txt");
            AssetBundle assetBundle = AssetBundle.LoadFromFile(path);
            if (assetBundle == null)
            {
                throw new InvalidOperationException("Could not read streaming asset bundle '" + path + "' synchronously.");
            }
            return (TAsset)(object)assetBundle;
#endif
        }
    }
}
#elif UNITY_IOS || UNITY_IPHONE
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Disney.Kelowna.Common
{
    public class StreamingAssetBundleDevice : Device
    {
        private string streamingAssetsPath;

        public const string DEVICE_TYPE = "sa-bundle";

        public override string DeviceType
        {
            get
            {
                return "sa-bundle";
            }
        }

        public StreamingAssetBundleDevice(DeviceManager deviceManager)
            : base(deviceManager)
        {
            // For iOS, streaming assets are typically in the /raw folder within the app bundle.
            streamingAssetsPath = "file://" + Application.dataPath + "/raw";
            Debug.Log("iOS StreamingAssets Path: " + streamingAssetsPath);
        }

        public override AssetRequest<TAsset> LoadAsync<TAsset>(string deviceList, ref ContentManifest.AssetEntry entry, AssetLoadedHandler<TAsset> handler = null)
        {
            StreamingAssetBundleWrapper streamingAssetBundleWrapper = new StreamingAssetBundleWrapper();
            AsyncStreamingAssetBundleRequest<TAsset> result = new AsyncStreamingAssetBundleRequest<TAsset>(entry.Key, streamingAssetBundleWrapper);
            CoroutineRunner.StartPersistent(loadBundleFromStreamingAssets(entry, streamingAssetBundleWrapper, handler), this, "loadBundleFromStreamingAssets");
            return result;
        }

        private IEnumerator loadBundleFromStreamingAssets<TAsset>(ContentManifest.AssetEntry entry, StreamingAssetBundleWrapper wrapper, AssetLoadedHandler<TAsset> handler) where TAsset : class
        {
            string key = entry.Key;

            // Validate the entry key
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Asset entry key is null or empty.");
                yield break; // Exit the coroutine if the key is invalid
            }

            // Construct the URL for iOS. Path.Combine handles platform-specific separators.
            // Ensure 'key' is correctly formatted for the asset bundle path.
            string url = Path.Combine(streamingAssetsPath, key + ".txt");
            Debug.Log("Loading asset from URL (iOS): " + url);

            wrapper.LoadFromDownload(url);
            yield return wrapper.WebRequest;
            AssetBundle assetBundle = wrapper.AssetBundle;
            if (handler != null)
            {
                handler(key, (TAsset)(object)assetBundle);
            }
            yield return null;
        }

        public override TAsset LoadImmediate<TAsset>(string deviceList, ref ContentManifest.AssetEntry entry)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            // In a browser StreamingAssets is an http URL, so there is nothing to read
            // synchronously. Callers have to go through LoadAsync
            throw new InvalidOperationException("Streaming asset bundles must be loaded asynchronously.");
#else
            // On a desktop player StreamingAssets is a plain directory, so a bundle that
            // is not mounted yet can be read right here. Refusing to do so is what makes
            // Content.LoadImmediate fail for anything living in a bundle: whether it
            // worked depended on the zone the player happened to be standing in.
            // BundleDevice mounts whatever comes back, so the file is opened once
            string path = Path.Combine(Application.streamingAssetsPath, entry.Key + ".txt");
            AssetBundle assetBundle = AssetBundle.LoadFromFile(path);
            if (assetBundle == null)
            {
                throw new InvalidOperationException("Could not read streaming asset bundle '" + path + "' synchronously.");
            }
            return (TAsset)(object)assetBundle;
#endif
        }
    }
}
#else // This block is for Editor, Windows, Mac, Linux, WebGL, etc.
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Disney.Kelowna.Common
{
    public class StreamingAssetBundleDevice : Device
    {
        public const string DEVICE_TYPE = "sa-bundle";

        private string streamingAssetsPath;

        public override string DeviceType
        {
            get
            {
                return "sa-bundle";
            }
        }

        public StreamingAssetBundleDevice(DeviceManager deviceManager)
            : base(deviceManager)
        {
            // Get the current base URL dynamically instead of hardcoding
            streamingAssetsPath = GetStreamingAssetsBaseURL();
            Debug.Log("StreamingAssets Base URL: " + streamingAssetsPath);
        }

        private string GetStreamingAssetsBaseURL()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // For WebGL builds, use Application.streamingAssetsPath
            string streamingAssetsUrl = Application.streamingAssetsPath;
            
            // Ensure it ends with a forward slash for URL concatenation
            if (!streamingAssetsUrl.EndsWith("/"))
            {
                streamingAssetsUrl += "/";
            }
            
            Debug.Log("WebGL StreamingAssets URL from Application.streamingAssetsPath: " + streamingAssetsUrl);
            return streamingAssetsUrl;
#else
            // For standalone builds (Windows, Mac, Linux)
            string dataPath = Application.dataPath;
            
            if (dataPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                // Already a web URL (shouldn't happen in standalone, but handle it)
                if (!dataPath.EndsWith("/"))
                {
                    dataPath += "/";
                }
                return dataPath + "StreamingAssets/";
            }
            else
            {
                // Local file path, convert to file:// URL
                string streamingPath = Path.Combine(dataPath, "StreamingAssets");
                // Normalize path separators to forward slashes for URLs
                streamingPath = streamingPath.Replace('\\', '/');
                return "file:///" + streamingPath + "/";
            }
#endif
        }

        public override AssetRequest<TAsset> LoadAsync<TAsset>(string deviceList, ref ContentManifest.AssetEntry entry, AssetLoadedHandler<TAsset> handler = null)
        {
            StreamingAssetBundleWrapper streamingAssetBundleWrapper = new StreamingAssetBundleWrapper();
            AsyncStreamingAssetBundleRequest<TAsset> result = new AsyncStreamingAssetBundleRequest<TAsset>(entry.Key, streamingAssetBundleWrapper);
            CoroutineRunner.StartPersistent(loadBundleFromStreamingAssets(entry, streamingAssetBundleWrapper, handler), this, "loadBundleFromStreamingAssets");
            return result;
        }

        private IEnumerator loadBundleFromStreamingAssets<TAsset>(ContentManifest.AssetEntry entry, StreamingAssetBundleWrapper wrapper, AssetLoadedHandler<TAsset> handler) where TAsset : class
        {
            string key = entry.Key;

            // Validate the entry key
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Asset entry key is null or empty.");
                yield break;
            }

            // Construct the URL by concatenating base path with asset key
            // The key should contain the relative path within StreamingAssets
            string url = streamingAssetsPath + key + ".txt";

            // Clean up any URL formatting issues
            url = url.Replace("//StreamingAssets/", "/StreamingAssets/")
                     .Replace(":///", "://");

            Debug.Log($"Loading asset bundle: key='{key}', url='{url}'");

            // Load the asset
            wrapper.LoadFromDownload(url);
            yield return wrapper.WebRequest;

            AssetBundle assetBundle = wrapper.AssetBundle;
            
            if (assetBundle == null)
            {
                Debug.LogError($"Failed to load AssetBundle from URL: {url}");
            }
            
            if (handler != null)
            {
                handler(key, (TAsset)(object)assetBundle);
            }
            
            yield return null;
        }

        public override TAsset LoadImmediate<TAsset>(string deviceList, ref ContentManifest.AssetEntry entry)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            // In a browser StreamingAssets is an http URL, so there is nothing to read
            // synchronously. Callers have to go through LoadAsync
            throw new InvalidOperationException("Streaming asset bundles must be loaded asynchronously.");
#else
            // On a desktop player StreamingAssets is a plain directory, so a bundle that
            // is not mounted yet can be read right here. Refusing to do so is what makes
            // Content.LoadImmediate fail for anything living in a bundle: whether it
            // worked depended on the zone the player happened to be standing in.
            // BundleDevice mounts whatever comes back, so the file is opened once
            string path = Path.Combine(Application.streamingAssetsPath, entry.Key + ".txt");
            AssetBundle assetBundle = AssetBundle.LoadFromFile(path);
            if (assetBundle == null)
            {
                throw new InvalidOperationException("Could not read streaming asset bundle '" + path + "' synchronously.");
            }
            return (TAsset)(object)assetBundle;
#endif
        }
    }
}
#endif
