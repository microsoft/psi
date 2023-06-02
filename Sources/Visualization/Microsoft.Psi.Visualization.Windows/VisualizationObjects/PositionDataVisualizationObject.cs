// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Numerics;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using PipelineRejeuxDonnees;

    /// <summary>
    /// Implements a visualization object for PositionData.
    /// </summary>
    [VisualizationObject("PositionData")]
    [VisualizationPanelType(VisualizationPanelType.Canvas)]
    public class PositionDataVisualizationObject : StreamValueVisualizationObject<PositionData>
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(PositionDataVisualizationObjectView));
    }
}