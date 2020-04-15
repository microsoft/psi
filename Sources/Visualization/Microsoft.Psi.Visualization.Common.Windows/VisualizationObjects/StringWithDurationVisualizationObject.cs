// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Represents a string with duration visualization object.
    /// </summary>
    [VisualizationObject("Visualize Strings With Duration")]
    public abstract class StringWithDurationVisualizationObject : TimelineVisualizationObject<Tuple<string, TimeSpan>>
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(StringWithDurationVisualizationObjectView));
    }
}
