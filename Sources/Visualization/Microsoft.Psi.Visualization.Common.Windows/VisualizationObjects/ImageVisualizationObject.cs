// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Class implements a image visualization object.
    /// </summary>
    [VisualizationObject("Visualize Images")]
    public class ImageVisualizationObject : ImageVisualizationObjectBase<Shared<Image>>
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(ImageVisualizationObjectView));
    }
}
