// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;

    /// <summary>
    /// Represents a visualization panel that time based visualizers can be rendered in.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimelineVisualizationPanel : VisualizationPanel<TimelineVisualizationPanelConfiguration>
    {
        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(TimelineVisualizationPanelView));

        /// <inheritdoc />
        protected override void InitNew()
        {
            base.InitNew();
            this.Configuration.Name = "Timeline Panel";
            this.Configuration.Height = 70;
        }
    }
}