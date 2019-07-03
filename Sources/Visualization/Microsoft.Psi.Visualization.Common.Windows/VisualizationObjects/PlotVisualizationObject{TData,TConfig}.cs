// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Config;

    /// <summary>
    /// Represents a plot visualization object.
    /// </summary>
    /// <typeparam name="TData">The type of the data for the plot visualization object.</typeparam>
    /// <typeparam name="TConfig">The plot visualization object configuration.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public abstract class PlotVisualizationObject<TData, TConfig> : TimelineVisualizationObject<TData, TConfig>
        where TConfig : PlotVisualizationObjectConfiguration, new()
    {
        private AxisComputeMode currentYAxisComputeMode;

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override Color LegendColor => this.Configuration.Color;

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override string LegendValue => this.CurrentValue.HasValue ? this.GetStringValue(this.CurrentValue.Value.Data) : string.Empty;

        /// <inheritdoc/>
        protected override InterpolationStyle InterpolationStyle => this.Configuration.InterpolationStyle;

        /// <summary>
        /// Converts the data to a double value to use for plotting.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The double value correspoding to the data.</returns>
        public abstract double GetDoubleValue(TData data);

        /// <summary>
        /// Converts the data to a string value to display.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The string value correspoding to the data.</returns>
        public abstract string GetStringValue(TData data);

        /// <inheritdoc />
        protected override void OnConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnConfigurationPropertyChanged(sender, e);

            if (e.PropertyName == nameof(PlotVisualizationObjectConfiguration.YAxisComputeMode))
            {
                // Take action only if value changed
                if (this.Configuration.YAxisComputeMode != this.currentYAxisComputeMode)
                {
                    this.currentYAxisComputeMode = this.Configuration.YAxisComputeMode;
                    this.AutoComputeYAxis();
                }
            }
            else if (e.PropertyName == nameof(PlotVisualizationObjectConfiguration.Color))
            {
                this.RaisePropertyChanged(nameof(this.LegendColor));
            }
        }

        /// <inheritdoc />
        protected override void OnDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // When collection changes, update the axis if in auto mode
            base.OnDataCollectionChanged(e);
            this.AutoComputeYAxis();
        }

        /// <inheritdoc />
        protected override void OnSummaryDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // When collection changes, update the axis if in auto mode
            base.OnSummaryDataCollectionChanged(e);
            this.AutoComputeYAxis();
        }

        // Auto compute the y axis limits
        private void AutoComputeYAxis()
        {
            if (this.currentYAxisComputeMode == AxisComputeMode.Auto)
            {
                double min = double.MaxValue;
                double max = double.MinValue;

                if ((this.Data != null) && (this.Data.Count > 0))
                {
                    foreach (var value in this.Data)
                    {
                        var doubleValue = this.GetDoubleValue(value.Data);
                        min = Math.Min(min, doubleValue);
                        max = Math.Max(max, doubleValue);
                    }
                }
                else if ((this.SummaryData != null) && (this.SummaryData.Count > 0))
                {
                    foreach (var value in this.SummaryData)
                    {
                        min = Math.Min(min, this.GetDoubleValue(value.Minimum));
                        max = Math.Max(max, this.GetDoubleValue(value.Maximum));
                    }
                }

                if (max >= min)
                {
                    this.Configuration.SetYRange(min, max);
                }
            }
        }
    }
}
