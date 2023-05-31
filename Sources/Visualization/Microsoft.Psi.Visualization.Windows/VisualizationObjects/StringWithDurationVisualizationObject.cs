// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Implements a visualization object for a tuple of string and duration.
    /// </summary>
    [VisualizationObject("Strings with Duration")]
    [VisualizationPanelType(VisualizationPanelType.Timeline)]
    public class StringWithDurationVisualizationObject : StreamIntervalVisualizationObject<Tuple<string, TimeSpan>>
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(StringWithDurationVisualizationObjectView));
    }
}
