using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Prime31
{
    public class P31RestKit
    {
        protected string _baseUrl;

        public bool debugRequests = false;

        protected bool forceJsonResponse;

        private GameObject _surrogateGameObject;

        private MonoBehaviour _surrogateMonobehaviour;

        protected virtual GameObject surrogateGameObject
        {
            get
            {
                if (_surrogateGameObject == null)
                {
                    _surrogateGameObject = GameObject.Find("P31CoroutineSurrogate");

                    if (_surrogateGameObject == null)
                    {
                        _surrogateGameObject = new GameObject("P31CoroutineSurrogate");
                        UnityEngine.Object.DontDestroyOnLoad(_surrogateGameObject);
                    }
                }

                return _surrogateGameObject;
            }
            set
            {
                _surrogateGameObject = value;
            }
        }

        protected MonoBehaviour surrogateMonobehaviour
        {
            get
            {
                if (_surrogateMonobehaviour == null)
                {
                    _surrogateMonobehaviour = surrogateGameObject.AddComponent<P31CoroutineSurrogate>();
                }

                return _surrogateMonobehaviour;
            }
            set
            {
                _surrogateMonobehaviour = value;
            }
        }

        protected virtual IEnumerator send(string path, HTTPVerb httpVerb, Dictionary<string, object> parameters, Action<string, object> onComplete)
        {
            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }

            UnityWebRequest request = processRequest(path, httpVerb, parameters);

            yield return request.SendWebRequest();

            if (debugRequests)
            {
                Debug.Log("response error: " + request.error);
                Debug.Log("response text: " + request.downloadHandler.text);

                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.Append("Response Headers:\n");

                foreach (KeyValuePair<string, string> responseHeader in request.GetResponseHeaders())
                {
                    stringBuilder.AppendFormat("{0}: {1}\n", responseHeader.Key, responseHeader.Value);
                }

                Debug.Log(stringBuilder.ToString());
            }

            if (onComplete != null)
            {
                processResponse(request, onComplete);
            }

            request.Dispose();
        }

        protected virtual UnityWebRequest processRequest(string path, HTTPVerb httpVerb, Dictionary<string, object> parameters)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (!path.StartsWith("http"))
            {
                stringBuilder.Append(_baseUrl).Append(path);
            }
            else
            {
                stringBuilder.Append(path);
            }

            bool hasBody = httpVerb != HTTPVerb.GET;
            WWWForm form = hasBody ? new WWWForm() : null;

            if (parameters != null && parameters.Count > 0)
            {
                if (hasBody)
                {
                    foreach (KeyValuePair<string, object> parameter in parameters)
                    {
                        if (parameter.Value is string)
                        {
                            form.AddField(parameter.Key, parameter.Value as string);
                        }
                        else if (parameter.Value is byte[])
                        {
                            form.AddBinaryData(parameter.Key, parameter.Value as byte[]);
                        }
                    }
                }
                else
                {
                    bool first = !path.Contains("?");

                    foreach (KeyValuePair<string, object> parameter in parameters)
                    {
                        if (parameter.Value is string)
                        {
                            stringBuilder.AppendFormat(
                                "{0}{1}={2}",
                                first ? "?" : "&",
                                Uri.EscapeDataString(parameter.Key),
                                Uri.EscapeDataString(parameter.Value as string)
                            );

                            first = false;
                        }
                    }
                }
            }

            if (debugRequests)
            {
                Debug.Log("url: " + stringBuilder.ToString());
            }

            Dictionary<string, string> headers = null;

            if (hasBody)
            {
                IDictionary headersFromForm = getHeadersFromForm(form);

                if (headersFromForm != null)
                {
                    headers = new Dictionary<string, string>();

                    if (headersFromForm.Contains("Content-Type"))
                    {
                        headers.Add("Content-Type", headersFromForm["Content-Type"].ToString());
                    }

                    if (debugRequests)
                    {
                        Debug.Log("Found a request body. Fetching headers from WWWForm and starting with these as a base: ");
                        Utils.logObject(headers);
                    }
                }
            }

            headers = headersForRequest(httpVerb, headers);

            UnityWebRequest request;

            if (!hasBody)
            {
                request = UnityWebRequest.Get(stringBuilder.ToString());
            }
            else
            {
                request = new UnityWebRequest(stringBuilder.ToString(), httpVerb.ToString());

                request.uploadHandler = new UploadHandlerRaw(form.data);
                request.downloadHandler = new DownloadHandlerBuffer();
            }

            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    if (header.Key != "METHOD")
                    {
                        request.SetRequestHeader(header.Key, header.Value);
                    }
                }
            }

            return request;
        }

        protected virtual Dictionary<string, string> headersForRequest(HTTPVerb httpVerb, Dictionary<string, string> headers = null)
        {
            headers = headers ?? new Dictionary<string, string>();

            switch (httpVerb)
            {
                case HTTPVerb.GET:
                    headers["METHOD"] = "GET";
                    break;

                case HTTPVerb.POST:
                    headers["METHOD"] = "POST";
                    break;

                case HTTPVerb.PUT:
                    headers["METHOD"] = "PUT";
                    headers["X-HTTP-Method-Override"] = "PUT";
                    break;

                case HTTPVerb.DELETE:
                    headers["METHOD"] = "DELETE";
                    headers["X-HTTP-Method-Override"] = "DELETE";
                    break;
            }

            return headers;
        }

        protected virtual void processResponse(UnityWebRequest request, Action<string, object> onComplete)
        {
#if UNITY_2020_1_OR_NEWER
            bool hasError = request.result == UnityWebRequest.Result.ConnectionError ||
                            request.result == UnityWebRequest.Result.ProtocolError;
#else
			bool hasError = request.isHttpError || request.isNetworkError;
#endif

            if (hasError)
            {
                onComplete(request.error, null);
            }
            else if (isResponseJson(request))
            {
                object obj = Json.decode(request.downloadHandler.text);

                if (obj == null)
                {
                    obj = request.downloadHandler.text;
                }

                onComplete(null, obj);
            }
            else
            {
                onComplete(null, request.downloadHandler.text);
            }
        }

        protected bool isResponseJson(UnityWebRequest request)
        {
            bool flag = false;

            if (forceJsonResponse)
            {
                flag = true;
            }

            if (!flag)
            {
                string contentType = request.GetResponseHeader("Content-Type");

                if (!string.IsNullOrEmpty(contentType))
                {
                    contentType = contentType.ToLower();

                    if (contentType.Contains("/json") || contentType.Contains("/javascript"))
                    {
                        flag = true;
                    }
                }
            }

            string text = request.downloadHandler.text;

            if (flag && !text.StartsWith("[") && !text.StartsWith("{"))
            {
                return false;
            }

            return flag;
        }

        protected virtual IDictionary getHeadersFromForm(WWWForm form)
        {
            try
            {
                PropertyInfo property = form.GetType().GetProperty("headers");

                if (property != null)
                {
                    return property.GetValue(form, null) as IDictionary;
                }

                Debug.Log("couldnt find the 'headers' property on the WWWForm object: " + form);
            }
            catch (Exception arg)
            {
                Debug.Log("ran into a problem transferring headers from WWWForm to the WWW request: " + arg);
            }

            return null;
        }

        public void setBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public void get(string path, Action<string, object> completionHandler)
        {
            get(path, null, completionHandler);
        }

        public void get(string path, Dictionary<string, object> parameters, Action<string, object> completionHandler)
        {
            surrogateMonobehaviour.StartCoroutine(send(path, HTTPVerb.GET, parameters, completionHandler));
        }

        public void post(string path, Action<string, object> completionHandler)
        {
            post(path, null, completionHandler);
        }

        public void post(string path, Dictionary<string, object> parameters, Action<string, object> completionHandler)
        {
            surrogateMonobehaviour.StartCoroutine(send(path, HTTPVerb.POST, parameters, completionHandler));
        }

        public void put(string path, Action<string, object> completionHandler)
        {
            put(path, null, completionHandler);
        }

        public void put(string path, Dictionary<string, object> parameters, Action<string, object> completionHandler)
        {
            surrogateMonobehaviour.StartCoroutine(send(path, HTTPVerb.PUT, parameters, completionHandler));
        }
    }

    public class P31CoroutineSurrogate : MonoBehaviour
    {
    }
}