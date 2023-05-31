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
    /// Implements a visualization object for a series of ints, represented as a dictionary keyed on the series name.
    /// </summary>
    [VisualizationObject("Int Series", typeof(IntSeriesRangeSummarizer))]
    public class IntSeriesVisualizationObject : PlotSeriesVisualizationObject<string, int>
    {
        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(IntSeriesVisualizationObjectView));

        /// <inheritdoc/>
        public override double GetNumericValue(int data)
        {
            return Convert.ToDouble(data);
        }

        /// <inheritdoc/>
        public override string GetStringValue(int data)
        {
            var format = $"{{0:{this.LegendFormat}}}";
            return string.Format(format, data);
        }
    }
}
