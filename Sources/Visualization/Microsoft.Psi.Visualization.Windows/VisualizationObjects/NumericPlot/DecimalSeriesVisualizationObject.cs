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
    /// Implements a visualization object for a series of decimals, represented as a dictionary keyed on the series name.
    /// </summary>
    [VisualizationObject("Decimal Series", typeof(DecimalSeriesRangeSummarizer))]
    public class DecimalSeriesVisualizationObject : PlotSeriesVisualizationObject<string, decimal>
    {
        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(DecimalSeriesVisualizationObjectView));

        /// <inheritdoc/>
        public override double GetNumericValue(decimal data)
        {
            return Convert.ToDouble(data);
        }

        /// <inheritdoc/>
        public override string GetStringValue(decimal data)
        {
            var format = $"{{0:{this.LegendFormat}}}";
            return string.Format(format, data);
        }
    }
}
