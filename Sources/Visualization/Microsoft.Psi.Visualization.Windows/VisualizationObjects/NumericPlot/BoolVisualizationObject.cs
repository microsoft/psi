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
    /// Implements a visualization object for boolean values.
    /// </summary>
    [VisualizationObject("Bool", typeof(BoolRangeSummarizer))]
    public class BoolVisualizationObject : PlotVisualizationObject<bool>
    {
        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(BoolVisualizationObjectView));

        /// <inheritdoc/>
        public override double GetNumericValue(bool data)
        {
            return Convert.ToDouble(data);
        }

        /// <inheritdoc/>
        public override string GetStringValue(bool data)
        {
            var format = $"{{0:{this.LegendFormat}}}";
            return string.Format(format, data);
        }
    }
}