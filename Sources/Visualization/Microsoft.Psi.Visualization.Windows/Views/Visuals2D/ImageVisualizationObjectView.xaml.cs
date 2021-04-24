// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for ImageVisualizationObjectView.xaml.
    /// </summary>
    public partial class ImageVisualizationObjectView : VisualizationObjectView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageVisualizationObjectView"/> class.
        /// </summary>
        public ImageVisualizationObjectView()
        {
            this.InitializeComponent();
            this.DisplayImage = new DisplayImage();
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
        }

        /// <inheritdoc/>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            System.Windows.Point mousePosition = e.GetPosition(this.Image);

            if (this.ImageVisualizationObject.CurrentValue != null && this.ImageVisualizationObject.CurrentValue.Value.Data != null && this.ImageVisualizationObject.CurrentValue.Value.Data.Resource != null)
            {
                int x = (int)(mousePosition.X * this.ImageVisualizationObject.CurrentValue.Value.Data.Resource.Width / this.Image.ActualWidth);
                int y = (int)(mousePosition.Y * this.ImageVisualizationObject.CurrentValue.Value.Data.Resource.Height / this.Image.ActualHeight);
                this.ImageVisualizationObject.SetMousePosition(new System.Windows.Point(x, y));
            }

            base.OnMouseMove(e);
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
        }
    }
}
