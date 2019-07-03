// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a stream visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class StreamVisualizationObjectConfiguration : VisualizationObjectConfiguration
    {
        /// <summary>
        /// Gets or sets the epsilon around the cursor for which we show the instant visualization.
        /// </summary>
        private int cursorEpsilonMs = 500;

        /// <summary>
        /// The stream being visualized.
        /// </summary>
        private StreamBinding streamBinding;

        /// <summary>
        /// Gets or sets the stream binding.
        /// </summary>
        [Browsable(false)]
        [DataMember]
        public StreamBinding StreamBinding
        {
            get { return this.streamBinding; }
            set { this.Set(nameof(this.StreamBinding), ref this.streamBinding, value); }
        }

        /// <summary>
        /// Gets or sets the epsilon around the cursor for which we show the instant visualization.
        /// </summary>
        [DataMember]
        public int CursorEpsilonMs
        {
            get { return this.cursorEpsilonMs; }
            set { this.Set(nameof(this.CursorEpsilonMs), ref this.cursorEpsilonMs, value); }
        }
    }
}
