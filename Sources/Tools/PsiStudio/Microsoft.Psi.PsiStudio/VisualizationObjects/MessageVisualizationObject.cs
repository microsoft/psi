// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Class implements a generic message visualization object view model.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class MessageVisualizationObject : PlotVisualizationObject<object, PlotVisualizationObjectConfiguration>
    {
        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(MessageVisualizationObjectView));

        /// <inheritdoc/>
        public override double GetDoubleValue(object data)
        {
            return 0;
        }

        /// <inheritdoc/>
        public override string GetStringValue(object data)
        {
            var format = $"{{0:{this.Configuration.LegendFormat}}}";
            return string.Format(format, data);
        }
    }
}
