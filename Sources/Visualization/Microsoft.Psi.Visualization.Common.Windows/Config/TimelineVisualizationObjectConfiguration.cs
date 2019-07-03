// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a timeline visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimelineVisualizationObjectConfiguration : StreamVisualizationObjectConfiguration
    {
        private long samplingTicks;
        private string legendFormat = string.Empty;

        /// <summary>
        /// Gets or sets the sampling ticks.
        /// </summary>
        [Browsable(false)]
        [DataMember]
        public long SamplingTicks
        {
            get => this.samplingTicks;
            set
            {
                if (value > 0)
                {
                    value = 1L << (int)Math.Log(value, 2);
                }

                this.Set(nameof(this.SamplingTicks), ref this.samplingTicks, value);
            }
        }

        /// <summary>
        /// Gets or sets a format specifier string used in displaying the live legend value.
        /// </summary>
        [DataMember]
        public string LegendFormat
        {
            get { return this.legendFormat; }
            set { this.Set(nameof(this.LegendFormat), ref this.legendFormat, value); }
        }
    }
}
