// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Implements a visualization object for a series of longs, represented as a dictionary keyed on the series name.
    /// </summary>
    [VisualizationObject("Long Series", typeof(LongSeriesRangeSummarizer))]
    public class LongSeriesVisualizationObject : PlotSeriesVisualizationObject<string, long>
    {
        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(LongSeriesVisualizationObjectView));

        /// <inheritdoc/>
        public override double GetNumericValue(long data)
        {
            return Convert.ToDouble(data);
        }

        /// <inheritdoc/>
        public override string GetStringValue(long data)
        {
            var format = $"{{0:{this.LegendFormat}}}";
            return string.Format(format, data);
        }
    }
}
