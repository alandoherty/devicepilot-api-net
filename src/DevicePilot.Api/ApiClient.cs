// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DevicePilot.Api
{
    /// <summary>
    /// Provides access to the Device Pilot API services and systems.
    /// </summary>
    public class ApiClient
    {
        #region Constants
        internal const string ApiUrl = "https://api.devicepilot.com";
        #endregion

        #region Fields
        private string _token;
        private HttpClient _client = null;
        private int _retryCount = 3;
        private TimeSpan _retryDelay = TimeSpan.FromSeconds(3);
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the retry count for transient failures.
        /// </summary>
        public int RetryCount {
            get {
                return _retryCount;
            }
            set {
                _retryCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the retry delay.
        /// </summary>
        public TimeSpan RetryDelay {
            get {
                return _retryDelay;
            }
            set {
                _retryDelay = value;
            }
        }

        /// <summary>
        /// Gets or sets the token;
        /// </summary>
        public string Token {
            get {
                return _token;
            }
            set {
                _token = value;
            }
        }

        /// <summary>
        /// Gets or sets the timeout for requests.
        /// </summary>
        public TimeSpan Timeout {
            get {
                return _client.Timeout;
            }
            set {
                _client.Timeout = value;
            }
        }

        /// <summary>
        /// Gets the underlying http client.
        /// </summary>
        public HttpClient Client {
            get {
                return _client;
            }
        }

        /// <summary>
        /// Gets or sets the base address.
        /// </summary>
        public Uri BaseAddress {
            get {
                return _client.BaseAddress;
            }
            set {
                _client.BaseAddress = value;
            }
        }
        #endregion

        #region Rest Methods
        /// <summary>
        /// Sends a REST request to the API, includes retry logic and reauthorisation.
        /// </summary>
        /// <param name="method">The target method.</param>
        /// <param name="path">The target path.</param>
        /// <param name="content">The request content, or null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response message.</returns>
        private async Task<HttpResponseMessage> RequestAsync(HttpMethod method, string path, HttpContent content, CancellationToken cancellationToken) {
            TimeSpan retryDelay = _retryDelay;

            for (int i = 0; i < _retryCount; i++) {
                try {
                    return await RawRequestAsync(method, path, content, cancellationToken).ConfigureAwait(false);
                } catch (ApiException ex) {
                    if ((ex.StatusCode == HttpStatusCode.BadGateway || ex.StatusCode == HttpStatusCode.GatewayTimeout || ex.StatusCode == HttpStatusCode.ServiceUnavailable) && i < _retryCount - 1) {
                        // wait retry delay and add (delay / 2)
                        await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
                        retryDelay += TimeSpan.FromTicks(retryDelay.Ticks / 2);

                        continue;
                    } else {
                        throw;
                    }
                }
            }

            throw new NotImplementedException("Unreachable");
        }

        /// <summary>
        /// Sends the raw REST request to the API, includes error handling logic but no reauthorisation or retry logic.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="path"></param>
        /// <param name="content"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> RawRequestAsync(HttpMethod method, string path, HttpContent content, CancellationToken cancellationToken) {
            // throw if cancelled
            cancellationToken.ThrowIfCancellationRequested();

            // create a request message
            HttpRequestMessage req = new HttpRequestMessage(method, path);

            // assign authentication
            req.Headers.Authorization = new AuthenticationHeaderValue("Token", _token);

            // assign body
            if (method != HttpMethod.Get)
                req.Content = content;

            // send
            HttpResponseMessage response = await _client.SendAsync(req, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode) {
                return response;
            } else {
                // throw if cancelled
                cancellationToken.ThrowIfCancellationRequested();

                // parse failure object
                string errMessage;

                try {
                    // parse the response error object
                    JObject obj = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

                    errMessage = (string)obj["message"];
                } catch (Exception) {
                    throw new ApiException(string.Format("Invalid server response - {0} {1}", (int)response.StatusCode, response.StatusCode), response);
                }

                // throw api exception
                throw new ApiException(errMessage, response);
            }
        }

        /// <summary>
        /// Requests a string response.
        /// </summary>
        /// <param name="method">The target method.</param>
        /// <param name="path">The target path.</param>
        /// <param name="content">The request content, or null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response string.</returns>
        private async Task<string> RequestStringAsync(HttpMethod method, string path, HttpContent content, CancellationToken cancellationToken) {
            return await (
                await RequestAsync(method, path, content, cancellationToken).ConfigureAwait(false))
                .Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Requests a JSON object response.
        /// </summary>
        /// <param name="method">The target method.</param>
        /// <param name="path">The target path.</param>
        /// <param name="content">The request content, or null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response message.</returns>
        /// <returns>The JSON object response.</returns>
        private async Task<JObject> RequestJsonObjectAsync(HttpMethod method, string path, HttpContent content, CancellationToken cancellationToken) {
            HttpResponseMessage response = await RequestAsync(method, path, content, cancellationToken).ConfigureAwait(false);

            if (!response.Content.Headers.ContentType.MediaType.StartsWith("application/json", StringComparison.CurrentCultureIgnoreCase))
                throw new ApiException("Invalid server response", response);

            return JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        }

        /// <summary>
        /// Requests a JSON object with no response.
        /// </summary>
        /// <param name="method">The target method.</param>
        /// <param name="path">The target path.</param>
        /// <param name="body">The JSON body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private Task RequestJsonAsync(HttpMethod method, string path, JObject body, CancellationToken cancellationToken) {
            return RequestAsync(method, path, new StringContent(body.ToString(), Encoding.UTF8, "application/json"), cancellationToken);
        }

        /// <summary>
        /// Requests a JSON object with no response.
        /// </summary>
        /// <param name="method">The target method.</param>
        /// <param name="path">The target path.</param>
        /// <param name="body">The JSON body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private Task RequestJsonAsync(HttpMethod method, string path, JArray body, CancellationToken cancellationToken) {
            return RequestAsync(method, path, new StringContent(body.ToString(), Encoding.UTF8, "application/json"), cancellationToken);
        }

        /// <summary>
        /// Requests a JSON object with a JSON object response.
        /// </summary>
        /// <param name="method">The target method.</param>
        /// <param name="path">The target path.</param>
        /// <param name="body">The JSON body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The JSON object response.</returns>
        private Task<JObject> RequestJsonObjectAsync(HttpMethod method, string path, JObject body, CancellationToken cancellationToken) {
            return RequestJsonObjectAsync(method, path, new StringContent(body.ToString(), Encoding.UTF8, "application/json"), cancellationToken);
        }

        /// <summary>
        /// Requests a serialized object with no response.
        /// </summary>
        /// <typeparam name="TReq">The request object type.</typeparam>
        /// <param name="method">The target method.</param>
        /// <param name="path">The target path.</param>
        /// <param name="body">The object to be serialized.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private Task RequestJsonSerializedAsync<TReq>(HttpMethod method, string path, TReq body, CancellationToken cancellationToken) {
            return RequestAsync(method, path, new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"), cancellationToken);
        }

        /// <summary>
        /// Requests a serialized object with a JSON object response.
        /// </summary>
        /// <typeparam name="TReq">The request object type.</typeparam>
        /// <param name="method">The target method.</param>
        /// <param name="path">The target path.</param>
        /// <param name="body">The object to be serialized.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The JSON object response.</returns>
        private async Task<JObject> RequestJsonSerializedObjectAsync<TReq>(HttpMethod method, string path, TReq body, CancellationToken cancellationToken) {
            HttpResponseMessage response = await RequestAsync(method, path, new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"), cancellationToken).ConfigureAwait(false);

            if (!response.Content.Headers.ContentType.MediaType.StartsWith("application/json", StringComparison.CurrentCultureIgnoreCase))
                throw new ApiException("Invalid server response", response);

            return JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        }

        /// <summary>
        /// Requests a JSON object with a serialized object response.
        /// </summary>
        /// <typeparam name="TRes">The response object type.</typeparam>
        /// <param name="method">The target method.</param>
        /// <param name="path">The target path.</param>
        /// <param name="body">The JSON body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response object.</returns>
        private async Task<TRes> RequestJsonSerializedAsync<TRes>(HttpMethod method, string path, JObject body, CancellationToken cancellationToken) {
            HttpResponseMessage response = await RequestAsync(method, path, new StringContent(body.ToString(), Encoding.UTF8, "application/json"), cancellationToken).ConfigureAwait(false);

            if (!response.Content.Headers.ContentType.MediaType.StartsWith("application/json", StringComparison.CurrentCultureIgnoreCase))
                throw new ApiException("Invalid server response", response);

            return JsonConvert.DeserializeObject<TRes>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        }

        /// <summary>
        /// Requests a serialized object with a serialized object response.
        /// </summary>
        /// <typeparam name="TReq">The request object type.</typeparam>
        /// <typeparam name="TRes">The response object type.</typeparam>
        /// <param name="method">The target method.</param>
        /// <param name="path">The target path.</param>
        /// <param name="body">The JSON body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response object.</returns>
        private async Task<TRes> RequestJsonSerializedAsync<TReq, TRes>(HttpMethod method, string path, TReq body, CancellationToken cancellationToken) {
            string s = JsonConvert.SerializeObject(body);
            HttpResponseMessage response = await RequestAsync(method, path, new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"), cancellationToken).ConfigureAwait(false);

            if (!response.Content.Headers.ContentType.MediaType.StartsWith("application/json", StringComparison.CurrentCultureIgnoreCase))
                throw new ApiException("Invalid server response", response);

            return JsonConvert.DeserializeObject<TRes>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        }

        /// <summary>
        /// Requests a serialized object response.
        /// </summary>
        /// <typeparam name="TRes">The response object type.</typeparam>
        /// <param name="method">The target method.</param>
        /// <param name="path">The target path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response object.</returns>
        private async Task<TRes> RequestJsonSerializedAsync<TRes>(HttpMethod method, string path, CancellationToken cancellationToken) {
            HttpResponseMessage response = await RequestAsync(method, path, new StringContent("", Encoding.UTF8), cancellationToken).ConfigureAwait(false);

            if (!response.Content.Headers.ContentType.MediaType.StartsWith("application/json", StringComparison.CurrentCultureIgnoreCase))
                throw new ApiException("Invalid server response", response);

            return JsonConvert.DeserializeObject<TRes>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        }
        #endregion

        #region API Methods
        /// <summary>
        /// Ingests a device into the device pilot API.
        /// </summary>
        /// <param name="id">The device ID.</param>
        /// <param name="properties">The device properties.</param>
        /// <param name="timestamp">The optional timestamp.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public virtual Task IngestAsync(string id, IReadOnlyDictionary<string, object> properties, DateTime? timestamp = null, CancellationToken cancellationToken = default) {
            return IngestAsync(new DeviceData() {
                Id = id,
                Properties = properties,
                Timestamp = timestamp
            }, cancellationToken);
        }

        /// <summary>
        /// Ingests a device into the device pilot API.
        /// </summary>
        /// <param name="data">The device data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public virtual Task IngestAsync(DeviceData data, CancellationToken cancellationToken = default) {
            // validate
            data.ThrowIfInvalid();

            // get serialized data
            JObject obj = data.AsJObject();

            return RequestJsonAsync(HttpMethod.Post, "devices", obj, cancellationToken);
        }

        /// <summary>
        /// Ingests multiple devices into the device pilot API, will automatically split up requests larger than 500 devices.
        /// </summary>
        /// <param name="data">The device data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <remarks>If the you request more than 500 devices and cancel, the cancellation may occur between requests.</remarks>
        /// <returns></returns>
        public virtual async Task BulkIngestAsync(IEnumerable<DeviceData> data, CancellationToken cancellationToken = default) {
            List<DeviceData> datas = new List<DeviceData>();
            int i = 0;

            // get enumerator for input data, split into chunks of 500
            var enumerator = data.GetEnumerator();

            while (true) {
                // clear list
                datas.Clear();

                // get up to 500 rows
                for (i = 0; i < 500; i++) {
                    if (!enumerator.MoveNext())
                        break;

                    // throw if invalid otherwise add to list for this chunk
                    enumerator.Current.ThrowIfInvalid();
                    datas.Add(enumerator.Current);
                }

                // build array and request this chunk
                if (datas.Count > 0) {
                    JArray arr = new JArray(datas.Select(d => d.AsJObject()).ToArray());

                    await RequestJsonAsync(HttpMethod.Post, "devices", arr, cancellationToken)
                        .ConfigureAwait(false);
                }

                if (i != 500)
                    break;
            }
        }

        /// <summary>
        /// Ingests a device into the device pilot API.
        /// </summary>
        /// <param name="data">The device data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="T">The device data type.</typeparam>
        /// <returns></returns>
        public virtual Task IngestAsync<T>(T data, CancellationToken cancellationToken = default) {
            TypeInfo ti = typeof(T).GetTypeInfo();

            // get all valid members
            IEnumerable<MemberInfo> validMembers = ti.DeclaredFields.Cast<MemberInfo>()
                .Concat(ti.DeclaredProperties.Cast<MemberInfo>());

            // find the id member
            MemberInfo memberId = validMembers.Where(m => m.GetCustomAttribute<DeviceIdAttribute>() != null).FirstOrDefault();

            if (memberId == null)
                throw new InvalidOperationException("The device object must have an ID member");

            // find the timestamp member
            MemberInfo memberTs = validMembers.Where(m => m.GetCustomAttribute<DeviceTimestampAttribute>() != null).FirstOrDefault();

            if (memberTs != null) {
                Type valueType = null;

                if (memberTs is FieldInfo)
                    valueType = ((FieldInfo)memberTs).FieldType;
                else
                    valueType = ((PropertyInfo)memberTs).PropertyType;

                if (valueType != typeof(DateTime?))
                    throw new InvalidOperationException("The device timestamp member must be a nullable DateTime type");
            }

            // find any property members
            IEnumerable<MemberInfo> memberProperties = validMembers.Where(m => m.GetCustomAttribute<DevicePropertyAttribute>() != null);

            // obtain id, properties and timestamp
            string id = null;
            DateTime? timestamp = null;
            Dictionary<string, object> properties = new Dictionary<string, object>();

            if (memberId is FieldInfo)
                id = ((FieldInfo)memberId).GetValue(data).ToString();
            else
                id = ((PropertyInfo)memberId).GetValue(data).ToString();

            if (memberTs != null) {
                if (memberTs is FieldInfo)
                    timestamp = (DateTime?)((FieldInfo)memberTs).GetValue(data);
                else
                    timestamp = (DateTime?)((PropertyInfo)memberTs).GetValue(data);
            }

            foreach(MemberInfo memberProperty in memberProperties) {
                // get attribute and obtain the overriden name if specified
                DevicePropertyAttribute attr = memberProperty.GetCustomAttribute<DevicePropertyAttribute>();
                string name = memberProperty.Name;

                if (attr.Name != null)
                    name = attr.Name;

                // add to properties
                if (memberProperty is FieldInfo)
                    properties[name] = ((FieldInfo)memberProperty).GetValue(data);
                else
                    properties[name] = ((PropertyInfo)memberProperty).GetValue(data);
            }

            // process
            return IngestAsync(new DeviceData() {
                Id = id,
                Properties = properties,
                Timestamp = timestamp
            }, cancellationToken);
        }

        /// <summary>
        /// Ingests multiple devices into the device pilot API, will automatically split up requests larger than 500 devices.
        /// </summary>
        /// <param name="datas">The device data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <remarks>If the you request more than 500 devices and cancel, the cancellation may occur between requests.</remarks>
        /// <typeparam name="T">The device data type.</typeparam>
        /// <returns></returns>
        public virtual Task BulkIngestAsync<T>(IEnumerable<T> datas, CancellationToken cancellationToken = default) {
            TypeInfo ti = typeof(T).GetTypeInfo();

            // get all valid members
            IEnumerable<MemberInfo> validMembers = ti.DeclaredFields.Cast<MemberInfo>()
                .Concat(ti.DeclaredProperties.Cast<MemberInfo>());

            // find the id member
            MemberInfo memberId = validMembers.Where(m => m.GetCustomAttribute<DeviceIdAttribute>() != null).FirstOrDefault();

            if (memberId == null)
                throw new InvalidOperationException("The device object must have an ID member");

            // find the timestamp member
            MemberInfo memberTs = validMembers.Where(m => m.GetCustomAttribute<DeviceTimestampAttribute>() != null).FirstOrDefault();

            if (memberTs != null) {
                Type valueType = null;

                if (memberTs is FieldInfo)
                    valueType = ((FieldInfo)memberTs).FieldType;
                else
                    valueType = ((PropertyInfo)memberTs).PropertyType;

                if (valueType != typeof(DateTime?))
                    throw new InvalidOperationException("The device timestamp member must be a nullable DateTime type");
            }

            // find any property members
            IEnumerable<MemberInfo> memberProperties = validMembers.Where(m => m.GetCustomAttribute<DevicePropertyAttribute>() != null);

            // build all the datas for each input
            List<DeviceData> outputDatas = new List<DeviceData>();

            foreach(T data in datas) {
                // obtain id, properties and timestamp
                string id = null;
                DateTime? timestamp = null;
                Dictionary<string, object> properties = new Dictionary<string, object>();

                if (memberId is FieldInfo)
                    id = ((FieldInfo)memberId).GetValue(data).ToString();
                else
                    id = ((PropertyInfo)memberId).GetValue(data).ToString();

                if (memberTs != null) {
                    if (memberTs is FieldInfo)
                        timestamp = (DateTime?)((FieldInfo)memberTs).GetValue(data);
                    else
                        timestamp = (DateTime?)((PropertyInfo)memberTs).GetValue(data);
                }

                foreach (MemberInfo memberProperty in memberProperties) {
                    // get attribute and obtain the overriden name if specified
                    DevicePropertyAttribute attr = memberProperty.GetCustomAttribute<DevicePropertyAttribute>();
                    string name = memberProperty.Name;

                    if (attr.Name != null)
                        name = attr.Name;

                    // add to properties
                    if (memberProperty is FieldInfo)
                        properties[name] = ((FieldInfo)memberProperty).GetValue(data);
                    else
                        properties[name] = ((PropertyInfo)memberProperty).GetValue(data);
                }

                // process
                outputDatas.Add(new DeviceData() {
                    Id = id,
                    Properties = properties,
                    Timestamp = timestamp
                });
            }

            return BulkIngestAsync(outputDatas, cancellationToken);
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Device Pilot client without a API key or secret.
        /// </summary>
        /// <param name="token">The token.</param>
        public ApiClient(string token) : this(token, null) {
        }

        /// <summary>
        /// Create a new Device Pilot client.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="apiUrl">The custom base path of the API.</param>
        public ApiClient(string token, string apiUrl) {
            // setup client
            _client = new HttpClient();
            _client.BaseAddress = new Uri(apiUrl == null ? ApiUrl : apiUrl);
            _client.DefaultRequestHeaders.Add("X-API-Client", "devicepilot-api-client/1.0");

            // set authentication
            _token = token ?? throw new ArgumentNullException(nameof(token), "The token cannot be null");
        }
        #endregion
    }
}
