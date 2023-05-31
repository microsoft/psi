// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    /// <summary>
    /// Represents a set of error codes for the speech service.
    /// </summary>
    public enum SpeechClientStatus
    {
        /// <summary>
        /// The client sent a WebSocket connection request that was incorrect.
        /// </summary>
        HttpBadRequest = 400,

        /// <summary>
        /// The client did not include the required authorization information.
        /// </summary>
        HttpUnauthorized = 401,

        /// <summary>
        /// The client sent authorization information, but it was invalid.
        /// </summary>
        HttpForbidden = 403,

        /// <summary>
        /// The client attempted to access a URL path that is not supported.
        /// </summary>
        HttpNotFound = 404,

        /// <summary>
        /// The service encountered an internal error and could not satisfy the request.
        /// </summary>
        HttpServerError = 500,

        /// <summary>
        /// The service was unavailable to handle the request.
        /// </summary>
        HttpServiceUnavailable = 503,

        /// <summary>
        /// The service closed the WebSocket connection without an error.
        /// </summary>
        WebSocketNormalClosure = 1000,

        /// <summary>
        /// The client failed to adhere to protocol requirements.
        /// </summary>
        WebSocketProtocolError = 1002,

        /// <summary>
        /// The client sent an invalid payload in a protocol message.
        /// </summary>
        WebSocketInvalidPayloadData = 1007,

        /// <summary>
        /// The service encountered an internal error and could not satisfy the request.
        /// </summary>
        WebSocketServerError = 1011,
    }
}
