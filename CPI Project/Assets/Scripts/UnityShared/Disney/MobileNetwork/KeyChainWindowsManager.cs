using LitJson;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.Cryptography;
using Disney.Mix.SDK;
using UnityEngine;
using System.Text;

namespace Disney.MobileNetwork
{
    public class KeyChainWindowsManager : KeyChainManager
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX_ARM || UNITY_EDITOR_OSX_ARM || UNITY_ANDROID || UNITY_EDITOR_ANDROID || UNITY_IOS || UNITY_EDITOR_IOS || UNITY_WEBGL //Ported from Patch

        private const string APP_DATA_KEY = "cp.AppData";

        // Store a copy of the encrypted blob on disk as well so it remains stable across
        // Unity Editor vs Player (PlayerPrefs are isolated between them).
        private const string APP_DATA_FILE_NAME = "cp.AppData.dat";

        private readonly IKeychain keychain;

        private const int InitializationVectorSize = 16;

        private static readonly RandomNumberGenerator rng = new RNGCryptoServiceProvider();

        private static readonly byte[] tempInitializationVector = new byte[16];

        private readonly AesManaged symmetricAlgorithm;

        private KeyChainManager keyChainManager;


        public static byte[] getChainKey;

        private static byte[] localStorageKey = new byte[32];

        // store loaded appData
        private new Dictionary<string, string> appData; //Ported from Patch

        public KeyChainWindowsManager()
        {
            symmetricAlgorithm = new AesManaged();
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private const string DLL_NAME = "KeyChainWindows";
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        private const string DLL_NAME = "libKeyChainLinux";
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        // _cryptProtectData: input string -> out size and pointer to protected bytes
        // Return int success (1) or 0
        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int _cryptProtectData(string dataIn, ref int dataOutSize, out IntPtr dataOut);

        // _cryptUnprotectData: input byte[] + length -> out pointer to ANSI string
        // Return int success (1) or 0
        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int _cryptUnprotectData(byte[] dataIn, int dataInLength, out IntPtr dataOut);

#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _keyChainFree(IntPtr ptr);
#endif

        private static void FreeNativePtr(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return;
            }
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            _keyChainFree(ptr);
#else
            Marshal.FreeCoTaskMem(ptr);
#endif
        }

        // Helper wrapper for unprotecting bytes -> string (handles ptr marshalling & free)
        private static string CryptUnprotect(byte[] data)
        {
            if (data == null || data.Length == 0) return null;
            IntPtr ptr = IntPtr.Zero;
            int res = _cryptUnprotectData(data, data.Length, out ptr);
            if (res == 0 || ptr == IntPtr.Zero) return null;

            // Marshal ANSI string
            string s = Marshal.PtrToStringAnsi(ptr);
            FreeNativePtr(ptr);
            return s;
        }
#endif


#if UNITY_WEBGL && !UNITY_EDITOR //Ported from Patch
        [DllImport("__Internal")] //Ported from Patch
        private static extern void KeyChainWebGL_SetString(string key, string value); //Ported from Patch

        [DllImport("__Internal")] //Ported from Patch
        private static extern IntPtr KeyChainWebGL_GetStringPtr(string key); //Ported from Patch

        [DllImport("__Internal")] //Ported from Patch
        private static extern void KeyChainWebGL_RemoveString(string key); //Ported from Patch

        [DllImport("__Internal")] //Ported from Patch
        private static extern int KeyChainWebGL_HasKey(string key); //Ported from Patch

        [DllImport("__Internal")] //Ported from Patch
        private static extern void KeyChainWebGL_Free(IntPtr ptr); //Ported from Patch

        private static bool WebGLKeyChainEnabled = true; //Ported from Patch

        private static string WebGL_GetString(string key) //Ported from Patch
        { //Ported from Patch
            if (!WebGLKeyChainEnabled) //Ported from Patch
            { //Ported from Patch
                return null; //Ported from Patch
            } //Ported from Patch

            try //Ported from Patch
            { //Ported from Patch
                if (KeyChainWebGL_HasKey(key) == 0) //Ported from Patch
                { //Ported from Patch
                    return null; //Ported from Patch
                } //Ported from Patch

                IntPtr ptr = KeyChainWebGL_GetStringPtr(key); //Ported from Patch
                if (ptr == IntPtr.Zero) //Ported from Patch
                { //Ported from Patch
                    return null; //Ported from Patch
                } //Ported from Patch

                try //Ported from Patch
                { //Ported from Patch
                    return PtrToStringUtf8(ptr); //Ported from Patch
                } //Ported from Patch
                finally //Ported from Patch
                { //Ported from Patch
                    KeyChainWebGL_Free(ptr); //Ported from Patch
                } //Ported from Patch
            } //Ported from Patch
            catch //Ported from Patch
            { //Ported from Patch
                WebGLKeyChainEnabled = false; //Ported from Patch
                return null; //Ported from Patch
            } //Ported from Patch
        } //Ported from Patch

        private static void WebGL_SetString(string key, string value) //Ported from Patch
        { //Ported from Patch
            if (!WebGLKeyChainEnabled) //Ported from Patch
            { //Ported from Patch
                return; //Ported from Patch
            } //Ported from Patch

            try //Ported from Patch
            { //Ported from Patch
                KeyChainWebGL_SetString(key, value ?? ""); //Ported from Patch
            } //Ported from Patch
            catch //Ported from Patch
            { //Ported from Patch
                WebGLKeyChainEnabled = false; //Ported from Patch
            } //Ported from Patch
        } //Ported from Patch

        private static void WebGL_RemoveString(string key) //Ported from Patch
        { //Ported from Patch
            if (!WebGLKeyChainEnabled) //Ported from Patch
            { //Ported from Patch
                return; //Ported from Patch
            } //Ported from Patch

            try //Ported from Patch
            { //Ported from Patch
                KeyChainWebGL_RemoveString(key); //Ported from Patch
            } //Ported from Patch
            catch //Ported from Patch
            { //Ported from Patch
                WebGLKeyChainEnabled = false; //Ported from Patch
            } //Ported from Patch
        } //Ported from Patch

        private static string PtrToStringUtf8(IntPtr ptr) //Ported from Patch
        { //Ported from Patch
            if (ptr == IntPtr.Zero) //Ported from Patch
            { //Ported from Patch
                return null; //Ported from Patch
            } //Ported from Patch

            int len = 0; //Ported from Patch
            while (Marshal.ReadByte(ptr, len) != 0) //Ported from Patch
            { //Ported from Patch
                len++; //Ported from Patch
            } //Ported from Patch

            if (len <= 0) //Ported from Patch
            { //Ported from Patch
                return ""; //Ported from Patch
            } //Ported from Patch

            byte[] buffer = new byte[len]; //Ported from Patch
            Marshal.Copy(ptr, buffer, 0, len); //Ported from Patch
            return Encoding.UTF8.GetString(buffer, 0, len); //Ported from Patch
        } //Ported from Patch
#endif //Ported from Patch

        public byte[] Decrypt2(byte[] bytes)
        {
            string text = keyChainManager.GetString("SessionUnlockKey");

            byte[] key;
            if (string.IsNullOrEmpty(text))
            {
                key = new byte[32];
                getChainKey = key;
                key = getChainKey;
            }
            else
            {
                key = Convert.FromBase64String(text);
            }

            if (bytes.Length <= 16)
            {
                throw new ArgumentException("Invalid byte array: " + BitConverter.ToString(bytes) + ". Must be over 16 bytes long.");
            }
            if (key.Length != 32)
            {
                throw new ArgumentException("Invalid key: " + BitConverter.ToString(key) + ". Must be 32 bytes long.");
            }

            symmetricAlgorithm.Key = key;
            Array.Copy(bytes, 0, tempInitializationVector, 0, 16);
            symmetricAlgorithm.IV = tempInitializationVector;
            ICryptoTransform cryptoTransform = symmetricAlgorithm.CreateDecryptor();
            return cryptoTransform.TransformFinalBlock(bytes, 16, bytes.Length - 16);
        }

        public string Decrypt(string text, string key)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }
            byte[] array = Convert.FromBase64String(text);
            byte[] array2 = new byte[32];
            byte[] array3 = new byte[array.Length - 32];
            Array.Copy(array, array2, 32);
            Array.ConstrainedCopy(array, 32, array3, 0, array.Length - 32);
            Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(key, array2);
            byte[] bytes = rfc2898DeriveBytes.GetBytes(32);
            byte[] bytes2 = rfc2898DeriveBytes.GetBytes(16);
            using (AesManaged aesManaged = new AesManaged())
            {
                aesManaged.Mode = CipherMode.CBC;
                aesManaged.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform transform = aesManaged.CreateDecryptor(bytes, bytes2))
                {
                    using (MemoryStream stream = new MemoryStream(array3))
                    {
                        using (CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader(stream2))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }

        public byte[] Encrypt2(byte[] bytes)
        {
            string text = null;

            byte[] key = null;

            text = getAppData()["SessionUnlockKey"];
            Debug.Log("Keys3: " + text);
            byte[] unused = Convert.FromBase64String(text);
            if (string.IsNullOrEmpty(text))
            {
                key = new byte[32];
                getChainKey = key;
                key = getChainKey;
            }
            else
            {
                key = Convert.FromBase64String(text);
            }

            if (unused.Length != 32)
            {
                throw new ArgumentException("Invalid key: " + BitConverter.ToString(unused) + ". Must be 32 bytes long.");
            }
            symmetricAlgorithm.Key = Convert.FromBase64String(getAppData()["SessionUnlockKey"]);
            rng.GetBytes(tempInitializationVector);
            symmetricAlgorithm.IV = tempInitializationVector;
            ICryptoTransform cryptoTransform = symmetricAlgorithm.CreateEncryptor();
            byte[] array = cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length);
            byte[] array2 = new byte[16 + array.Length];
            Array.Copy(tempInitializationVector, 0, array2, 0, 16);
            Array.Copy(array, 0, array2, 16, array.Length);
            return array2;
        }

        public string Encrypt(string text, string key)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }
            Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(key, 32);
            byte[] array = rfc2898DeriveBytes.Salt;
            byte[] bytes = rfc2898DeriveBytes.GetBytes(32);
            byte[] bytes2 = rfc2898DeriveBytes.GetBytes(16);
            using (AesManaged aesManaged = new AesManaged())
            {
                aesManaged.Mode = CipherMode.CBC;
                aesManaged.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform transform = aesManaged.CreateEncryptor(bytes, bytes2))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream stream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(stream))
                            {
                                streamWriter.Write(text);
                            }
                        }
                        byte[] array2 = memoryStream.ToArray();
                        Array.Resize(ref array, array.Length + array2.Length);
                        Array.Copy(array2, 0, array, 32, array2.Length);
                        return Convert.ToBase64String(array);
                    }
                }
            }
        }

        private Dictionary<string, string> getAppData()
        {
            Dictionary<string, string> strs;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            string str = null;
            try
            {
                string appDataFilePath = Path.Combine(Application.persistentDataPath, APP_DATA_FILE_NAME);
                if (File.Exists(appDataFilePath))
                {
                    str = File.ReadAllText(appDataFilePath);
                }
            }
            catch
            {
                str = null;
            }

            if (string.IsNullOrEmpty(str))
            {
                str = PlayerPrefs.GetString(APP_DATA_KEY, null);
            }
            else if (!PlayerPrefs.HasKey(APP_DATA_KEY))
            {
                // Keep a copy in PlayerPrefs so existing code paths still work.
                PlayerPrefs.SetString(APP_DATA_KEY, str);
                PlayerPrefs.Save();
            }
            if (string.IsNullOrEmpty(str))
            {
                strs = new Dictionary<string, string>();
            }
            else
            {
                byte[] numArray = null;
                try
                {
                    numArray = Convert.FromBase64String(str);
                }
                catch
                {
                    numArray = null;
                }

                // use CryptUnprotect wrapper which calls native _cryptUnprotectData
                string str1 = (numArray != null) ? CryptUnprotect(numArray) : null;
                if (string.IsNullOrEmpty(str1))
                {
                    try
                    {
                        str1 = Decrypt(str, "4C906C6AAF5C2CB4B581411A91091A8D");
                    }
                    catch
                    {
                        str1 = null;
                    }
                }

                if (string.IsNullOrEmpty(str1))
                {
                    // Last-chance: treat the stored value as plaintext JSON.
                    str1 = str;
                }

                if (string.IsNullOrEmpty(str1))
                {
                    strs = new Dictionary<string, string>();
                }
                else
                {
                    try
                    {
                        strs = JsonMapper.ToObject<Dictionary<string, string>>(str1);
                    }
                    catch
                    {
                        strs = new Dictionary<string, string>();
                    }
                }
            }
#elif UNITY_WEBGL //Ported from Patch
            string str = null; //Ported from Patch
            if (WebGLKeyChainEnabled) //Ported from Patch
            { //Ported from Patch
                str = WebGL_GetString(APP_DATA_KEY); //Ported from Patch
            } //Ported from Patch

            if (string.IsNullOrEmpty(str)) //Ported from Patch
            { //Ported from Patch
                str = PlayerPrefs.GetString(APP_DATA_KEY, null); //Ported from Patch
            } //Ported from Patch

            if (string.IsNullOrEmpty(str)) //Ported from Patch
            { //Ported from Patch
                strs = new Dictionary<string, string>(); //Ported from Patch
            } //Ported from Patch
            else //Ported from Patch
            { //Ported from Patch
                Dictionary<string, string> parsed = null; //Ported from Patch

                try //Ported from Patch
                { //Ported from Patch
                    string decrypted = Decrypt(str, "4C906C6AAF5C2CB4B581411A91091A8D"); //Ported from Patch
                    if (!string.IsNullOrEmpty(decrypted)) //Ported from Patch
                    { //Ported from Patch
                        parsed = JsonMapper.ToObject<Dictionary<string, string>>(decrypted); //Ported from Patch
                    } //Ported from Patch
                } //Ported from Patch
                catch //Ported from Patch
                { //Ported from Patch
                    parsed = null; //Ported from Patch
                } //Ported from Patch

                if (parsed == null) //Ported from Patch
                { //Ported from Patch
                    try //Ported from Patch
                    { //Ported from Patch
                        parsed = JsonMapper.ToObject<Dictionary<string, string>>(str); //Ported from Patch
                    } //Ported from Patch
                    catch //Ported from Patch
                    { //Ported from Patch
                        parsed = null; //Ported from Patch
                    } //Ported from Patch
                } //Ported from Patch

                if (parsed == null) //Ported from Patch
                { //Ported from Patch
                    try //Ported from Patch
                    { //Ported from Patch
                        byte[] bytes = Convert.FromBase64String(str); //Ported from Patch
                        string json = Encoding.UTF8.GetString(bytes); //Ported from Patch
                        parsed = JsonMapper.ToObject<Dictionary<string, string>>(json); //Ported from Patch
                    } //Ported from Patch
                    catch //Ported from Patch
                    { //Ported from Patch
                        parsed = null; //Ported from Patch
                    } //Ported from Patch
                } //Ported from Patch

                strs = parsed ?? new Dictionary<string, string>(); //Ported from Patch
            } //Ported from Patch
#else
            string str = PlayerPrefs.GetString(APP_DATA_KEY, null);
            if (string.IsNullOrEmpty(str))
            {
                strs = new Dictionary<string, string>();
            }
            else
            {
                string str2 = Decrypt(str, "4C906C6AAF5C2CB4B581411A91091A8D");
                strs = JsonMapper.ToObject<Dictionary<string, string>>(str2);
            }
#endif
            return strs;
        }

        public override string GetString(string key)
        {
            string str;
            this.appData = this.getAppData();
            this.appData.TryGetValue(key, out str);
            return str;
        }

        protected override void Init()
        {
            this.appData = this.getAppData();

#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            if (this.appData.TryGetValue("SessionUnlockKey", out string unlockKey) && !string.IsNullOrEmpty(unlockKey))
            {
                PlayerPrefs.SetString("SessionUnlockKey", unlockKey);
                PlayerPrefs.Save();
            }
#endif
        }

        public override void PutString(string key, string value)
        {
            this.appData[key] = value;
            this.setAppData(this.appData);
        }

        public override void RemoveString(string key)
        {
            if (this.appData.ContainsKey(key))
            {
                this.appData.Remove(key);
                this.setAppData(this.appData);
            }
        }

        private void setAppData(Dictionary<string, string> data)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            if (data == null)
            {
                PlayerPrefs.DeleteKey(APP_DATA_KEY);
                PlayerPrefs.Save();
                try
                {
                    string appDataFilePath = Path.Combine(Application.persistentDataPath, APP_DATA_FILE_NAME);
                    if (File.Exists(appDataFilePath))
                    {
                        File.Delete(appDataFilePath);
                    }
                }
                catch
                {
                }

                return;
            }

            string json = JsonMapper.ToJson(data);
            string appDataFilePath2 = null;
            try
            {
                appDataFilePath2 = Path.Combine(Application.persistentDataPath, APP_DATA_FILE_NAME);
            }
            catch
            {
            }

            // Native protect: get bytes & store base64
            IntPtr ptr = IntPtr.Zero;
            int size = 0;
            int res = _cryptProtectData(json, ref size, out ptr);
            if (res == 0 || ptr == IntPtr.Zero || size <= 0)
            {
                // fallback: use Encrypt() to store playable data (so nothing is lost)
                string fallback = Encrypt(json, "4C906C6AAF5C2CB4B581411A91091A8D");
                PlayerPrefs.SetString(APP_DATA_KEY, fallback);
                PlayerPrefs.Save();
                if (!string.IsNullOrEmpty(appDataFilePath2))
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(appDataFilePath2));
                        File.WriteAllText(appDataFilePath2, fallback);
                    }
                    catch
                    {
                    }
                }
                return;
            }

            try
            {
                byte[] numArray = new byte[size];
                Marshal.Copy(ptr, numArray, 0, size);
                string stored = Convert.ToBase64String(numArray);
                PlayerPrefs.SetString(APP_DATA_KEY, stored);
                PlayerPrefs.Save();
                if (!string.IsNullOrEmpty(appDataFilePath2))
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(appDataFilePath2));
                        File.WriteAllText(appDataFilePath2, stored);
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                FreeNativePtr(ptr);
            }

#elif UNITY_WEBGL //Ported from Patch
            if (data == null) //Ported from Patch
            { //Ported from Patch
                if (WebGLKeyChainEnabled) //Ported from Patch
                { //Ported from Patch
                    try //Ported from Patch
                    { //Ported from Patch
                        WebGL_RemoveString(APP_DATA_KEY); //Ported from Patch
                    } //Ported from Patch
                    catch //Ported from Patch
                    { //Ported from Patch
                        WebGLKeyChainEnabled = false; //Ported from Patch
                    } //Ported from Patch
                } //Ported from Patch

                PlayerPrefs.DeleteKey(APP_DATA_KEY); //Ported from Patch
                return; //Ported from Patch
            } //Ported from Patch

            string json = JsonMapper.ToJson(data); //Ported from Patch
            string stored; //Ported from Patch

            try //Ported from Patch
            { //Ported from Patch
                stored = Encrypt(json, "4C906C6AAF5C2CB4B581411A91091A8D"); //Ported from Patch
            } //Ported from Patch
            catch //Ported from Patch
            { //Ported from Patch
                stored = json; //Ported from Patch
            } //Ported from Patch

            if (WebGLKeyChainEnabled) //Ported from Patch
            { //Ported from Patch
                try //Ported from Patch
                { //Ported from Patch
                    WebGL_SetString(APP_DATA_KEY, stored); //Ported from Patch
                } //Ported from Patch
                catch //Ported from Patch
                { //Ported from Patch
                    WebGLKeyChainEnabled = false; //Ported from Patch
                } //Ported from Patch
            } //Ported from Patch

            PlayerPrefs.SetString(APP_DATA_KEY, stored); //Ported from Patch
#else
            if (data == null)
            {
                PlayerPrefs.DeleteKey(APP_DATA_KEY);
                return;
            }

            string json = JsonMapper.ToJson(data);
            PlayerPrefs.SetString(APP_DATA_KEY, Encrypt(json, "4C906C6AAF5C2CB4B581411A91091A8D"));
#endif
        }

#endif
    }
}