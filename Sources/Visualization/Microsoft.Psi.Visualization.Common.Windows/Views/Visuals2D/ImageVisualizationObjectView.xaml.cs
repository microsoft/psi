// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for ImageVisualizationObjectView.xaml.
    /// </summary>
    public partial class ImageVisualizationObjectView : UserControl
    {
        // A blank image to display when there is no data.
        private static Shared<Imaging.Image> blankImage = Shared.Create(new Imaging.Image(0, 0, Imaging.PixelFormat.Undefined));

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
            if (e.PropertyName == nameof(this.ImageVisualizationObject.CurrentValue) || e.PropertyName == nameof(this.ImageVisualizationObject.HorizontalFlip))
            {
                this.ShowCurrentImage();
            }
        }

        private void ShowCurrentImage()
        {
            // Get the current image
            Shared<Imaging.Image> image = this.ImageVisualizationObject.CurrentValue.GetValueOrDefault().Data;

            if (image == null)
            {
                // There's no data, so use the default blank image
                using (Shared<Imaging.Image> defaultImage = blankImage.AddRef())
                {
                    this.DisplayImage.UpdateImage(defaultImage);
                }
            }
            else if (this.ImageVisualizationObject.HorizontalFlip)
            {
                // Flip the image before displaying it
                Bitmap bitmap = image.Resource.ToManagedImage(true);
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                using (Shared<Imaging.Image> flippedImage = Shared.Create(Imaging.Image.FromManagedImage(bitmap)))
                {
                    this.DisplayImage.UpdateImage(flippedImage);
                }
            }
            else
            {
                // Display the image normally
                this.DisplayImage.UpdateImage(image);
            }
        }
    }
}
