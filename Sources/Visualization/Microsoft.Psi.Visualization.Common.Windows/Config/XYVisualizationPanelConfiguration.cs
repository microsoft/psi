// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation (xAxis, yAxis, etc.)

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Common;

    /// <summary>
    /// Represents the XY visualization panel configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class XYVisualizationPanelConfiguration : VisualizationPanelConfiguration
    {
        private AxisInfo xAxis;
        private AxisInfo xAxis2;
        private AxisInfo yAxis;
        private AxisInfo yAxis2;

        /// <summary>
        /// Initializes a new instance of the <see cref="XYVisualizationPanelConfiguration"/> class.
        /// </summary>
        public XYVisualizationPanelConfiguration()
        {
            this.Name = "2D Panel";
        }

        /// <summary>
        /// Gets or sets X axis.
        /// </summary>
        [DataMember]
        public AxisInfo XAxis
        {
            get { return this.xAxis; }
            set { this.Set(nameof(this.XAxis), ref this.xAxis, value); }
        }

        /// <summary>
        /// Gets or sets second X axis.
        /// </summary>
        [DataMember]
        public AxisInfo XAxis2
        {
            get { return this.xAxis2; }
            set { this.Set(nameof(this.XAxis2), ref this.xAxis2, value); }
        }

        /// <summary>
        /// Gets or sets Y axis.
        /// </summary>
        [DataMember]
        public AxisInfo YAxis
        {
            get { return this.yAxis; }
            set { this.Set(nameof(this.YAxis), ref this.yAxis, value); }
        }

        /// <summary>
        /// Gets or sets second Y axis.
        /// </summary>
        [DataMember]
        public AxisInfo YAxis2
        {
            get { return this.yAxis2; }
            set { this.Set(nameof(this.YAxis2), ref this.yAxis2, value); }
        }
    }
}