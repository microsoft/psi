// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for ImageVisualizationObjectView.xaml.
    /// </summary>
    public partial class ImageVisualizationObjectView : ImageVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageVisualizationObjectView"/> class.
        /// </summary>
        public ImageVisualizationObjectView()
        {
            this.InitializeComponent();
            this.DisplayImage = new DisplayImage();
            this.Canvas = this._DynamicCanvas;
        }

        /// <summary>
        /// Gets the display image.
        /// </summary>
        public DisplayImage DisplayImage { get; private set; }

        /// <summary>
        /// Gets the image visualization object.
        /// </summary>
        public ImageVisualizationObject ImageVisualizationObject => this.VisualizationObject as ImageVisualizationObject;

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.ImageVisualizationObject.CurrentValue) || e.PropertyName == nameof(this.ImageVisualizationObject.HorizontalFlip))
            {
                this.ShowCurrentImage();
            }

            base.OnVisualizationObjectPropertyChanged(sender, e);
        }

        /// <inheritdoc/>
        protected override void UpdateView()
        {
        }

        private void ShowCurrentImage()
        {
            // Get the current image
            Shared<Imaging.Image> image = this.ImageVisualizationObject.CurrentValue.GetValueOrDefault().Data;

            if (image == null)
            {
                this.Image.Visibility = Visibility.Hidden;
                this.DisplayImage.UpdateImage(default(Shared<Imaging.Image>));
            }
            else
            {
                if (this.ImageVisualizationObject.HorizontalFlip)
                {
                    // Flip the image before displaying it
                    Bitmap bitmap = image.Resource.ToBitmap(true);
                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    using Shared<Imaging.Image> flippedImage = Shared.Create(Imaging.Image.FromBitmap(bitmap));
                    this.DisplayImage.UpdateImage(flippedImage);
                }
                else
                {
                    this.DisplayImage.UpdateImage(image);
                }

                if (this.Image.Visibility != Visibility.Visible)
                {
                    this.Image.Visibility = Visibility.Visible;
                }
            }

            // Update the image size if it's changed
            if ((this.DisplayImage.Image != null) &&
                ((this.Image.Width != this.DisplayImage.Image.PixelWidth) || (this.Image.Height != this.DisplayImage.Image.PixelHeight)))
            {
                this.Image.Width = this.DisplayImage.Image.PixelWidth;
                this.Image.Height = this.DisplayImage.Image.PixelHeight;
            }
        }
    }
}
