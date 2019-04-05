// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
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
    /// <typeparam name="TConfig">The plot visualization object configuration.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public abstract class PlotVisualizationObject<TConfig> : TimelineVisualizationObject<double, TConfig>
        where TConfig : PlotVisualizationObjectConfiguration, new()
    {
        private static Color[] colorChoice = new[] { Colors.LightBlue, Colors.Pink, Colors.LightGreen, Colors.Blue, Colors.Yellow, Colors.Green, Colors.Red };
        private static int nextColorChoice;
        private AxisComputeMode currentYAxisComputeMode;

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override Color LegendColor => this.Configuration.Color;

        /// <inheritdoc/>
        protected override InterpolationStyle InterpolationStyle => this.Configuration.InterpolationStyle;

        /// <inheritdoc />
        protected override void InitNew()
        {
            base.InitNew();
            var color = colorChoice[nextColorChoice % colorChoice.Length];
            this.Configuration.Color = color;
            this.Configuration.LineWidth = 1;
            this.Configuration.InterpolationStyle = InterpolationStyle.Direct;
            this.Configuration.MarkerColor = color;
            this.Configuration.MarkerSize = 4;
            this.Configuration.MarkerStyle = MarkerStyle.None;
            this.Configuration.RangeColor = color;
            this.Configuration.RangeWidth = 1;
            this.Configuration.SetYRange(0, 0);
            this.Configuration.YAxisComputeMode = AxisComputeMode.Auto;
            Interlocked.Increment(ref nextColorChoice);
        }

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
                        if (value.Data < min)
                        {
                            min = value.Data;
                        }

                        if (value.Data > max)
                        {
                            max = value.Data;
                        }
                    }
                }
                else if ((this.SummaryData != null) && (this.SummaryData.Count > 0))
                {
                    foreach (var value in this.SummaryData)
                    {
                        if (value.Minimum < min)
                        {
                            min = value.Minimum;
                        }

                        if (value.Maximum > max)
                        {
                            max = value.Maximum;
                        }
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
