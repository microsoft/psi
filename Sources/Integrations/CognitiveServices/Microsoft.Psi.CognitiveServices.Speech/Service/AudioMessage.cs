// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Represents a message containing audio to be sent to the service.
    /// </summary>
    internal class AudioMessage : SpeechServiceBinaryMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioMessage"/> class.
        /// </summary>
        /// <remarks>
        /// This overload creates an empty <see cref="AudioMessage"/> which is typically used to denote the end of the audio stream.
        /// </remarks>
        internal AudioMessage()
            : this(new byte[0])
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioMessage"/> class.
        /// </summary>
        /// <param name="format">The format of the audio.</param>
        /// <remarks>
        /// This overload creates an <see cref="AudioMessage"/> carrying the serialized format data.
        /// This should be sent once, before any audio data is sent to the service.
        /// </remarks>
        internal AudioMessage(WaveFormat format)
            : this(format.GetBytes())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioMessage"/> class.
        /// </summary>
        /// <param name="data">An array of bytes containing the message data.</param>
        internal AudioMessage(byte[] data)
            : this(data, 0, data.Length)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioMessage"/> class.
        /// </summary>
        /// <param name="data">An array of bytes containing the message data.</param>
        /// <param name="offset">The offset into the array where the message data starts.</param>
        /// <param name="count">The number of bytes of data to include in the message.</param>
        internal AudioMessage(byte[] data, int offset, int count)
            : base("audio", data, offset, count)
        {
        }
    }
}
