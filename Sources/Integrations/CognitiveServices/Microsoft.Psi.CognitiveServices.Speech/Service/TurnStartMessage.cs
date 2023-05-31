// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    /// <summary>
    /// Represents a turn.start message from the service.
    /// </summary>
    internal class TurnStartMessage : SpeechServiceTextMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TurnStartMessage"/> class.
        /// </summary>
        public TurnStartMessage()
           : base("turn.start")
        {
        }

        /// <summary>
        /// Gets or sets the context for the start of the turn.
        /// </summary>
        public ContextInfo Context { get; set; }

        /// <summary>
        /// Represents the context for the start of the turn.
        /// </summary>
        internal class ContextInfo
        {
            /// <summary>
            /// Gets or sets a tag value that the service has associated with the turn.
            /// </summary>
            public string ServiceTag { get; set; }
        }
    }
}
