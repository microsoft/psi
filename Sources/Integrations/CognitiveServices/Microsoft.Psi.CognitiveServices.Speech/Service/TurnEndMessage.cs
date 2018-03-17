// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    /// <summary>
    /// Represents a turn.end message from the service.
    /// </summary>
    internal class TurnEndMessage : SpeechServiceTextMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TurnEndMessage"/> class.
        /// </summary>
        public TurnEndMessage()
           : base("turn.end")
        {
        }
    }
}
