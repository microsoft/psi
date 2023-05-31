// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Implements a visualization object for nullable floats.
    /// </summary>
    [VisualizationObject("Float", typeof(NullableFloatRangeSummarizer))]
    public class NullableFloatVisualizationObject : PlotVisualizationObject<float?>
    {
        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(NullableFloatVisualizationObjectView));

        /// <inheritdoc/>
        public override double GetNumericValue(float? data)
        {
            return data ?? double.NaN;
        }

        /// <inheritdoc/>
        public override string GetStringValue(float? data)
        {
            var format = $"{{0:{this.LegendFormat}}}";
            return data.HasValue ? string.Format(format, data) : "<null>";
        }
    }
}
