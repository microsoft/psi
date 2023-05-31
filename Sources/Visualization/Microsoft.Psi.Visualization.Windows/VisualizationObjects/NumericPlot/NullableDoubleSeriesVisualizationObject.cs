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
    /// Implements a visualization object for a series of nullable doubles, represented as a dictionary keyed on the series name.
    /// </summary>
    [VisualizationObject("Double Series", typeof(NullableDoubleSeriesRangeSummarizer))]
    public class NullableDoubleSeriesVisualizationObject : PlotSeriesVisualizationObject<string, double?>
    {
        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(NullableDoubleSeriesVisualizationObjectView));

        /// <inheritdoc/>
        public override double GetNumericValue(double? data)
        {
            return data == null ? double.NaN : Convert.ToDouble(data.Value);
        }

        /// <inheritdoc/>
        public override string GetStringValue(double? data)
        {
            var format = $"{{0:{this.LegendFormat}}}";
            return string.Format(format, data);
        }
    }
}
