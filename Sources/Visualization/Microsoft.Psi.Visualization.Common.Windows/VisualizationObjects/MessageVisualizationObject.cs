// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Class implements a generic message visualization object view model.
    /// </summary>
    [VisualizationObject("Visualize Messages", typeof(ObjectSummarizer), IconSourcePath.Messages, IconSourcePath.MessagesInPanel, "%StreamName% (Messages)", true)]
    public class MessageVisualizationObject : PlotVisualizationObject<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageVisualizationObject"/> class.
        /// </summary>
        public MessageVisualizationObject()
            : base()
        {
            this.MarkerSize = 4;
            this.MarkerStyle = MarkerStyle.Circle;
        }

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
            return data?.ToString();

            ////var format = $"{{0:{this.LegendFormat}}}";
            ////return string.Format(format, data);
        }
    }
}
