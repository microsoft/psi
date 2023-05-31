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
    /// Implements a visualization object for nullable ints.
    /// </summary>
    [VisualizationObject("Int", typeof(NullableIntRangeSummarizer))]
    public class NullableIntVisualizationObject : PlotVisualizationObject<int?>
    {
        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(NullableIntVisualizationObjectView));

        /// <inheritdoc/>
        public override double GetNumericValue(int? data)
        {
            return data ?? double.NaN;
        }

        /// <inheritdoc/>
        public override string GetStringValue(int? data)
        {
            var format = $"{{0:{this.LegendFormat}}}";
            return data.HasValue ? string.Format(format, data) : "<null>";
        }
    }
}
