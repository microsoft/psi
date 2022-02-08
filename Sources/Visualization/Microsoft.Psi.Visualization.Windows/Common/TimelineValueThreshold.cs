// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a threshold in a timeline panel above or below
    /// which the data is rendered with a different opacity.
    /// </summary>
    public class TimelineValueThreshold : ObservableObject
    {
        private TimelineThresholdType thresholdType = TimelineThresholdType.None;
        private double thresholdValue = 0.0d;
        private double opacity = 0.25d;

        /// <summary>
        /// Defines the types of threshold display that are available.
        /// </summary>
        public enum TimelineThresholdType
        {
            /// <summary>
            /// Threshold display is disabled.
            /// </summary>
            None,

            /// <summary>
            /// Values below the threshold value are highlighted.
            /// </summary>
            Minimum,

            /// <summary>
            /// Values above the threshold value are highlighted.
            /// </summary>
            Maximum,
        }

        /// <summary>
        /// Gets or sets the threshold type is use.
        /// </summary>
        [DataMember]
        [PropertyOrder(1)]
        [DisplayName("Threshold Type")]
        [Description("The type of threshold to use.")]
        public TimelineThresholdType ThresholdType
        {
            get => this.thresholdType;
            set => this.Set(nameof(this.ThresholdType), ref this.thresholdType, value);
        }

        /// <summary>
        /// Gets or sets the value of the threshold.
        /// </summary>
        [DataMember]
        [PropertyOrder(2)]
        [DisplayName("Threshold Value")]
        [Description("The value of the threshold.")]
        public double ThresholdValue
        {
            get => this.thresholdValue;
            set => this.Set(nameof(this.ThresholdValue), ref this.thresholdValue, value);
        }

        /// <summary>
        /// Gets or sets the opacity of the data that does not exceed the threshold.
        /// </summary>
        [DataMember]
        [PropertyOrder(3)]
        [DisplayName("Opacity")]
        [Description("The opacity to use when rendering data that does not exceed the threshold.")]
        public double Opacity
        {
            get => this.opacity;
            set => this.Set(nameof(this.Opacity), ref this.opacity, value);
        }
    }
}
