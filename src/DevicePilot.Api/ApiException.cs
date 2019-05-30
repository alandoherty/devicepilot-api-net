// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace DevicePilot.Api
{
    /// <summary>
    /// Represents a failure response from the API.
    /// </summary>
    public sealed class ApiException : Exception
    {
        private HttpResponseMessage _response;

        /// <summary>
        /// Gets the status code of the response that generated the exception, if any.
        /// </summary>
        public HttpStatusCode StatusCode {
            get {
                return _response == null ? 0 : _response.StatusCode;
            }
        }

        /// <summary>
        /// Gets the response that generated the exception, if any.
        /// </summary>
        public HttpResponseMessage Response {
            get {
                return _response;
            }
        }

        internal ApiException(string message, HttpResponseMessage res)
            : base(message) {
            _response = res;
        }

        internal ApiException(string message, HttpResponseMessage res, Exception innerException)
            : base(message, innerException) {
            _response = res;
        }
    }
}
