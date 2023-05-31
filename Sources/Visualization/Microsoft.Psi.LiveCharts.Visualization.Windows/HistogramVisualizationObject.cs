// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.LiveCharts
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Implements a histogram visualization object.
    /// </summary>
    public class HistogramVisualizationObject : CartesianChartVisualizationObject<(string, double)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HistogramVisualizationObject"/> class.
        /// </summary>
        public HistogramVisualizationObject()
            : base((data, seriesName, index) => (index, data[seriesName][index].Item2))
        {
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // For histograms, we need to do specific mapping of the axis labels since they are
            // not automatically determined by the underlying cartesian chart. Specifically, we
            // populate the AxisXLabels and AxisYLabels (depending on the chart type), with the
            // category labels extracted from the first series in the histogram
            if (e != null && (e.PropertyName == nameof(this.CurrentData) || e.PropertyName == nameof(this.CartesianChartType)))
            {
                if (this.CurrentData != null)
                {
                    // traverse all the series and accumulate labels in a hashset
                    var labelsHashSet = new HashSet<string>();
                    if (this.CurrentData.Values != null)
                    {
                        foreach (var series in this.CurrentData.Values)
                        {
                            foreach (var label in series.Select(tuple => tuple.Item1))
                            {
                                labelsHashSet.Add(label);
                            }
                        }
                    }

                    var labelsArray = labelsHashSet.ToArray();

                    this.AxisXLabels = this.CartesianChartType switch
                    {
                        CartesianChartType.Line => labelsArray,
                        CartesianChartType.VerticalLine => null,
                        CartesianChartType.Column => labelsArray,
                        CartesianChartType.Row => null,
                        CartesianChartType.StackedArea => labelsArray,
                        CartesianChartType.VerticalStackedArea => null,
                        CartesianChartType.StackedColumn => labelsArray,
                        CartesianChartType.StackedRow => null,
                        _ => throw new Exception(UnknownCartesianChartTypeMessage)
                    };

                    this.AxisYLabels = this.CartesianChartType switch
                    {
                        CartesianChartType.Line => null,
                        CartesianChartType.VerticalLine => labelsArray,
                        CartesianChartType.Column => null,
                        CartesianChartType.Row => labelsArray,
                        CartesianChartType.StackedArea => null,
                        CartesianChartType.VerticalStackedArea => labelsArray,
                        CartesianChartType.StackedColumn => null,
                        CartesianChartType.StackedRow => labelsArray,
                        _ => throw new Exception(UnknownCartesianChartTypeMessage)
                    };
                }
            }

            base.OnPropertyChanged(sender, e);
        }
    }
}
