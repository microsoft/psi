// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;

    /// <summary>
    /// Provides the base class for a message for the speech service.
    /// </summary>
    internal abstract class SpeechServiceMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechServiceMessage"/> class.
        /// </summary>
        /// <param name="path">The path header value.</param>
        /// <param name="contentType">The content type header value.</param>
        protected SpeechServiceMessage(string path, string contentType)
        {
            this.Headers = new Dictionary<string, string>() { { "Path", path }, { "Content-Type", contentType } };
        }

        /// <summary>
        /// Gets the set of message headers.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// Gets the message path header value.
        /// </summary>
        [JsonIgnore]
        public string Path => this.Headers["path"];

        /// <summary>
        /// Gets the request id header value.
        /// </summary>
        [JsonIgnore]
        public string RequestId => this.Headers["x-requestid"];

        /// <summary>
        /// Deserializes the raw text message from the service into its equivalent message type.
        /// </summary>
        /// <param name="message">The string representing the raw message.</param>
        /// <returns>The deserialized message object.</returns>
        public static SpeechServiceMessage Deserialize(string message)
        {
            // Raw message contains everything, including the headers which we need
            // in order to discover the message type (from the Path header field).
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Parse the message text to extract the headers
            StringReader reader = new StringReader(message);
            string line = null;
            do
            {
                line = reader.ReadLine();
                if (line == string.Empty)
                {
                    break;
                }
                else
                {
                    var index = line.IndexOf(':');
                    if (index > -1)
                    {
                        headers.Add(line.Substring(0, index), line.Substring(index + 1));
                    }
                }
            }
            while (line != null);

            // Now read the body of the message, which should be in JSON format
            string body = reader.ReadToEnd();

            // Deserialize the body, using the Path header to determine the message type
            var serviceMessage = DeserializeBody(headers["path"], body);

            // Manually add the headers
            serviceMessage.Headers = headers;

            return serviceMessage;
        }

        /// <summary>
        /// Gets a byte array representation of the message.
        /// </summary>
        /// <returns>A byte array representation of the message.</returns>
        public abstract byte[] GetBytes();

        /// <summary>
        /// Deserializes the JSON body of the message.
        /// </summary>
        /// <param name="path">The Path header value.</param>
        /// <param name="body">The JSON message body.</param>
        /// <returns>The deserialized message object.</returns>
        private static SpeechServiceMessage DeserializeBody(string path, string body)
        {
            switch (path.ToLower())
            {
                case "turn.start":
                    return JsonConvert.DeserializeObject<TurnStartMessage>(body);

                case "turn.end":
                    return JsonConvert.DeserializeObject<TurnEndMessage>(body);

                case "speech.enddetected":
                    return JsonConvert.DeserializeObject<SpeechEndDetectedMessage>(body);

                case "speech.phrase":
                    return JsonConvert.DeserializeObject<SpeechPhraseMessage>(body);

                case "speech.hypothesis":
                    return JsonConvert.DeserializeObject<SpeechHypothesisMessage>(body);

                case "speech.fragment":
                    return JsonConvert.DeserializeObject<SpeechFragmentMessage>(body);

                case "speech.startdetected":
                    return JsonConvert.DeserializeObject<SpeechStartDetectedMessage>(body);

                default:
                    throw new NotSupportedException(path);
            }
        }
    }
}
