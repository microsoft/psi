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
    /// Implements a visualization object for nullable decimal.
    /// </summary>
    [VisualizationObject("Decimal", typeof(NullableDecimalRangeSummarizer))]
    public class NullableDecimalVisualizationObject : PlotVisualizationObject<decimal?>
    {
        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(NullableDecimalVisualizationObjectView));

        /// <inheritdoc/>
        public override double GetNumericValue(decimal? data)
        {
            return data == null ? double.NaN : Convert.ToDouble(data.Value);
        }

        /// <inheritdoc/>
        public override string GetStringValue(decimal? data)
        {
            var format = $"{{0:{this.LegendFormat}}}";
            return data.HasValue ? string.Format(format, data) : "<null>";
        }
    }
}
