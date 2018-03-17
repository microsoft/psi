// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    using System;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Provides the base class for a binary message for the speech service.
    /// </summary>
    internal class SpeechServiceBinaryMessage : SpeechServiceMessage
    {
        private ArraySegment<byte> data;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechServiceBinaryMessage"/> class.
        /// </summary>
        /// <param name="path">The path header value.</param>
        /// <param name="data">An array containing the message payload.</param>
        /// <param name="offset">The offset into the array where the message payload starts.</param>
        /// <param name="count">The number of bytes to include in the message payload.</param>
        protected SpeechServiceBinaryMessage(string path, byte[] data, int offset, int count)
            : base(path, "audio/x-wav")
        {
            this.data = new ArraySegment<byte>(data, offset, count);
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

            return messageBuilder.ToString();
        }

        /// <inheritdoc />
        public override byte[] GetBytes()
        {
            // Headers of binary messages are encoded as ASCII rather than UTF-8
            var headerBytes = Encoding.ASCII.GetBytes(this.ToString());

            // Prefix with the byte count of the headers
            var headerLength = BitConverter.GetBytes((ushort)headerBytes.Length);
            var headerPrefix = BitConverter.IsLittleEndian ?
                new byte[] { headerLength[1], headerLength[0] } :
                new byte[] { headerLength[0], headerLength[1] };

            return headerPrefix.Concat(headerBytes).Concat(this.data).ToArray();
        }
    }
}
