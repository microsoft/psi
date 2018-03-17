// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation (xMax, xMin, etc.)

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a scatter plot visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class ScatterPlotVisualizationObjectConfiguration : InstantVisualizationObjectConfiguration
    {
        private System.Drawing.Color fillColor;
        private int radius;
        private double xMax;
        private double xMin;
        private double yMax;
        private double yMin;

        /// <summary>
        /// Gets or sets the fill color.
        /// </summary>
        [DataMember]
        public System.Drawing.Color FillColor
        {
            get { return this.fillColor; }
            set { this.Set(nameof(this.FillColor), ref this.fillColor, value); }
        }

        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        [DataMember]
        public int Radius
        {
            get { return this.radius; }
            set { this.Set(nameof(this.Radius), ref this.radius, value); }
        }

        /// <summary>
        /// Gets or sets X max.
        /// </summary>
        [DataMember]
        public double XMax
        {
            get { return this.xMax; }
            set { this.Set(nameof(this.XMax), ref this.xMax, value); }
        }

        /// <summary>
        /// Gets or sets X min.
        /// </summary>
        [DataMember]
        public double XMin
        {
            get { return this.xMin; }
            set { this.Set(nameof(this.XMin), ref this.xMin, value); }
        }

        /// <summary>
        /// Gets or sets Y max.
        /// </summary>
        [DataMember]
        public double YMax
        {
            get { return this.yMax; }
            set { this.Set(nameof(this.YMax), ref this.yMax, value); }
        }

        /// <summary>
        /// Gets or sets Y min.
        /// </summary>
        [DataMember]
        public double YMin
        {
            get { return this.yMin; }
            set { this.Set(nameof(this.YMin), ref this.yMin, value); }
        }
    }
}
