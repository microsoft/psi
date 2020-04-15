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
    /// Class implements a plot visualization object view model.
    /// </summary>
    [VisualizationObject("Visualize Data", typeof(RangeSummarizer))]
    public class PlotVisualizationObject : PlotVisualizationObject<double>
    {
        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(PlotVisualizationObjectView));

        /// <inheritdoc/>
        public override double GetDoubleValue(double data)
        {
            return data;
        }

        /// <inheritdoc/>
        public override string GetStringValue(double data)
        {
            var format = $"{{0:{this.LegendFormat}}}";
            return string.Format(format, data);
        }
    }
}
