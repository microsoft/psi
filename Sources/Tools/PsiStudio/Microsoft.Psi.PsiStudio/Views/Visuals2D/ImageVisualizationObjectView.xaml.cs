// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for ImageVisualizationObjectView.xaml.
    /// </summary>
    public partial class ImageVisualizationObjectView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageVisualizationObjectView"/> class.
        /// </summary>
        public ImageVisualizationObjectView()
        {
            this.InitializeComponent();
            this.DisplayImage = new DisplayImage();
            this.DataContextChanged += this.ImageVisualizationObjectView_DataContextChanged;
            this.Unloaded += this.ImageVisualizationObjectView_Unloaded;
        }

        /// <summary>
        /// Gets the display image.
        /// </summary>
        public DisplayImage DisplayImage { get; private set; }

        /// <summary>
        /// Gets the image visualization object.
        /// </summary>
        public ImageVisualizationObject ImageVisualizationObject { get; private set; }

        private void ImageVisualizationObjectView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.ImageVisualizationObject = (ImageVisualizationObject)this.DataContext;
            this.ImageVisualizationObject.PropertyChanged += this.ImageVisualizationObject_PropertyChanged;
        }

        private void ImageVisualizationObjectView_Unloaded(object sender, RoutedEventArgs e)
        {
            this.ImageVisualizationObject.PropertyChanged -= this.ImageVisualizationObject_PropertyChanged;
        }

        private void ImageVisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.ImageVisualizationObject.CurrentValue))
            {
                var image = this.ImageVisualizationObject.CurrentValue.GetValueOrDefault().Data;
                if (this.ImageVisualizationObject.Configuration.HorizontalFlip)
                {
                    if (image != null && image.Resource != null)
                    {
                        var bitmap = image.Resource.ToManagedImage(true);
                        bitmap.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipX);
                        var sh = Shared.Create(Imaging.Image.FromManagedImage(bitmap));
                        this.DisplayImage.UpdateImage(sh);
                    }
                    else
                    {
                        this.DisplayImage.UpdateImage(image);
                    }
                }
                else if (image != null)
                {
                    this.DisplayImage.UpdateImage(image);
                }
            }
        }
    }
}
