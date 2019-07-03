// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation (xMax, xMin, etc.)

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using System.Windows.Media;

    /// <summary>
    /// Represents a scatter plot visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class ScatterPlotVisualizationObjectConfiguration : InstantVisualizationObjectConfiguration
    {
        private Color fillColor = Colors.Red;
        private Color labelColor = Colors.White;

        private int radius = 3;
        private double xMax = 0;
        private double xMin = 0;
        private double yMax = 0;
        private double yMin = 0;
        private bool showLabels = true;

        /// <summary>
        /// Gets or sets the fill color.
        /// </summary>
        [DataMember]
        public Color FillColor
        {
            get { return this.fillColor; }
            set { this.Set(nameof(this.FillColor), ref this.fillColor, value); }
        }

        /// <summary>
        /// Gets or sets the label color.
        /// </summary>
        [DataMember]
        public Color LabelColor
        {
            get { return this.labelColor; }
            set { this.Set(nameof(this.LabelColor), ref this.labelColor, value); }
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

        /// <summary>
        /// Gets or sets a value indicating whether we're showing the labels.
        /// </summary>
        [DataMember]
        public bool ShowLabels
        {
            get { return this.showLabels; }
            set { this.Set(nameof(this.ShowLabels), ref this.showLabels, value); }
        }
    }
}
