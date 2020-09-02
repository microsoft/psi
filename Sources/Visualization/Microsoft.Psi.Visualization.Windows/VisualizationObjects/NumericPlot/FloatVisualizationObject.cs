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
    /// Implements a visualization object for floats.
    /// </summary>
    [VisualizationObject("Float", typeof(FloatRangeSummarizer))]
    public class FloatVisualizationObject : PlotVisualizationObject<float>
    {
        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(FloatVisualizationObjectView));

        /// <inheritdoc/>
        public override double GetNumericValue(float data)
        {
            return data;
        }

        /// <inheritdoc/>
        public override string GetStringValue(float data)
        {
            var format = $"{{0:{this.LegendFormat}}}";
            return string.Format(format, data);
        }
    }
}