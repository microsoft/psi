// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an audio visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AudioVisualizationObjectConfiguration : PlotVisualizationObjectConfiguration
    {
        /// <summary>
        /// The audio channel to plot.
        /// </summary>
        private short channel;

        /// <summary>
        /// Gets or sets the audio channel to plot.
        /// </summary>
        [DataMember]
        public short Channel
        {
            get { return this.channel; }
            set { this.Set(nameof(this.Channel), ref this.channel, value); }
        }
    }
}
