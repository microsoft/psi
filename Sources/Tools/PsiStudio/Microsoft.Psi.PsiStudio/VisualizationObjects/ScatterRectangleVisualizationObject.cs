// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Represents a scatter rectangle visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class ScatterRectangleVisualizationObject : InstantVisualizationObject<List<Tuple<Rectangle, string>>, ScatterRectangleVisualizationObjectConfiguration>
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(ScatterRectangleVisualizationObjectView));

        /// <inheritdoc />
        protected override void InitNew()
        {
            base.InitNew();
            this.Configuration.Height = 1080;
            this.Configuration.Color = System.Windows.Media.Color.FromArgb(255, 70, 85, 198);
            this.Configuration.LineWidth = 1;
            this.Configuration.Width = 1920;
        }
    }
}
