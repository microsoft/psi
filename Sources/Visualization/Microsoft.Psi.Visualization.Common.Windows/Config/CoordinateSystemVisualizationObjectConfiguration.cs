// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a scatter coordinate systems visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class CoordinateSystemVisualizationObjectConfiguration : Instant3DVisualizationObjectConfiguration
    {
        private double size = 150;

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        [DataMember]
        public double Size
        {
            get { return this.size; }
            set { this.Set(nameof(this.Size), ref this.size, value); }
        }
    }
}
