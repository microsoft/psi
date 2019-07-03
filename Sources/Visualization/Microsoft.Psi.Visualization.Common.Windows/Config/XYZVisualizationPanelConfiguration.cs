// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents the XYZ visualization panel configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class XYZVisualizationPanelConfiguration : VisualizationPanelConfiguration
    {
        private double majorDistance = 5;
        private double minorDistance = 5;
        private double thickness = 0.01;

        /// <summary>
        /// Initializes a new instance of the <see cref="XYZVisualizationPanelConfiguration"/> class.
        /// </summary>
        public XYZVisualizationPanelConfiguration()
        {
            this.Name = "3D Panel";
        }

        /// <summary>
        /// Gets or sets the major distance.
        /// </summary>
        [DataMember]
        public double MajorDistance
        {
            get { return this.majorDistance; }
            set { this.Set(nameof(this.MajorDistance), ref this.majorDistance, value); }
        }

        /// <summary>
        /// Gets or sets the minor distance.
        /// </summary>
        [DataMember]
        public double MinorDistance
        {
            get { return this.minorDistance; }
            set { this.Set(nameof(this.MinorDistance), ref this.minorDistance, value); }
        }

        /// <summary>
        /// Gets or sets the thickness.
        /// </summary>
        [DataMember]
        public double Thickness
        {
            get { return this.thickness; }
            set { this.Set(nameof(this.Thickness), ref this.thickness, value); }
        }
    }
}