// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using System.Windows.Media;

    /// <summary>
    /// Represents a time interval history visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimeIntervalHistoryVisualizationObjectConfiguration : TimelineVisualizationObjectConfiguration
    {
        private Color color = Colors.LightSkyBlue;

        /// <summary>
        /// Gets or sets the name of the visualization object
        /// </summary>
        [DataMember]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }
    }
}
