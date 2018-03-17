// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;

    /// <summary>
    /// Represents a visualization panel that 2D visualizers can be rendered in.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class XYVisualizationPanel : VisualizationPanel<XYVisualizationPanelConfiguration>
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(XYVisualizationPanelView));

        /// <inheritdoc />
        protected override void InitNew()
        {
            base.InitNew();
            this.Configuration.Name = "2D Panel";
        }
    }
}