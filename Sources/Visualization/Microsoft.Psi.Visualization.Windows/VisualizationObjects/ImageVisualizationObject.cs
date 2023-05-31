// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Implements an image visualization object.
    /// </summary>
    [VisualizationObject("Image")]
    public class ImageVisualizationObject : ImageVisualizationObjectBase<Shared<Image>>
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(ImageVisualizationObjectView));

        /// <inheritdoc />
        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.CurrentData))
            {
                if (this.CurrentData != default && this.CurrentData.Resource != null)
                {
                    this.Resolution = new Size(this.CurrentData.Resource.Width, this.CurrentData.Resource.Height);
                }
                else
                {
                    this.Resolution = default;
                }
            }

            base.OnPropertyChanged(sender, e);
        }
    }
}
