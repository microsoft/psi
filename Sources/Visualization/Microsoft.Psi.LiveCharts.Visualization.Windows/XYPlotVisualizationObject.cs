// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.LiveCharts
{
    /// <summary>
    /// Implements a XY-plot visualization object.
    /// </summary>
    public class XYPlotVisualizationObject : CartesianChartVisualizationObject<(double, double)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XYPlotVisualizationObject"/> class.
        /// </summary>
        public XYPlotVisualizationObject()
            : base((data, seriesName, index) => data[seriesName][index])
        {
        }
    }
}
