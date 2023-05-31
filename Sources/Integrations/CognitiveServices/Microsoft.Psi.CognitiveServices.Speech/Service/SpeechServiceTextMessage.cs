// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Provides the base class for a text message for the speech service.
    /// </summary>
    internal class SpeechServiceTextMessage : SpeechServiceMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechServiceTextMessage"/> class.
        /// </summary>
        /// <param name="path">The path header value.</param>
        protected SpeechServiceTextMessage(string path)
            : base(path, "application/json; charset=utf-8")
        {
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var messageBuilder = new StringBuilder();

            // Append the message headers.
            foreach (var header in this.Headers)
            {
                messageBuilder.Append($"{header.Key}: {header.Value}\r\n");
            }

            messageBuilder.Append("\r\n");

            // Append the JSON-serialized properties of the object to the string representation.
            var messageBody = JsonConvert.SerializeObject(
                this,
                Formatting.None,
                new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() });

            messageBuilder.Append(messageBody);

            return messageBuilder.ToString();
        }

        /// <inheritdoc />
        public override byte[] GetBytes()
        {
            return Encoding.UTF8.GetBytes(this.ToString());
        }
    }
}
