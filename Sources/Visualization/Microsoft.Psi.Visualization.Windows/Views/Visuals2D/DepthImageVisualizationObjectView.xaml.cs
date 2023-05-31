// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for ImageVisualizationObjectView.xaml.
    /// </summary>
    public partial class DepthImageVisualizationObjectView : DepthImageVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageVisualizationObjectView"/> class.
        /// </summary>
        public DepthImageVisualizationObjectView()
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
        /// Gets the depth image visualization object.
        /// </summary>
        public DepthImageVisualizationObject DepthImageVisualizationObject => this.VisualizationObject as DepthImageVisualizationObject;

        /// <inheritdoc/>
        protected override void UpdateView()
        {
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.DepthImageVisualizationObject.CurrentValue) ||
                e.PropertyName == nameof(this.DepthImageVisualizationObject.HorizontalFlip) ||
                e.PropertyName == nameof(this.DepthImageVisualizationObject.RangeMin) ||
                e.PropertyName == nameof(this.DepthImageVisualizationObject.RangeMax) ||
                e.PropertyName == nameof(this.DepthImageVisualizationObject.RangeMode) ||
                e.PropertyName == nameof(this.DepthImageVisualizationObject.InvalidAsTransparent) ||
                e.PropertyName == nameof(this.DepthImageVisualizationObject.InvalidValue))
            {
                this.ShowCurrentImage();
            }
        }

        private void ShowCurrentImage()
        {
            // Get the current image
            var sharedDepthImage = this.DepthImageVisualizationObject.CurrentValue.GetValueOrDefault().Data;

            if (sharedDepthImage == null || sharedDepthImage.Resource == null)
            {
                this.Image.Visibility = Visibility.Hidden;
            }
            else
            {
                // Update the display image
                using var sharedColorizedImage = ImagePool.GetOrCreate(
                    sharedDepthImage.Resource.Width,
                    sharedDepthImage.Resource.Height,
                    PixelFormat.BGRA_32bpp);

                var minRange = this.DepthImageVisualizationObject.RangeMin;
                var maxRange = this.DepthImageVisualizationObject.RangeMax;
                if (this.DepthImageVisualizationObject.RangeMode == DepthImageRangeMode.Auto)
                {
                    (minRange, maxRange) = sharedDepthImage.Resource.GetPixelRange();
                }

                sharedDepthImage.Resource.PseudoColorize(
                    sharedColorizedImage.Resource,
                    ((ushort)minRange, (ushort)maxRange),
                    (this.DepthImageVisualizationObject.InvalidValue < 0) ? null : (ushort)this.DepthImageVisualizationObject.InvalidValue,
                    this.DepthImageVisualizationObject.InvalidAsTransparent);

                if (this.DepthImageVisualizationObject.HorizontalFlip)
                {
                    // Flip the image before displaying it
                    Bitmap bitmap = sharedColorizedImage.Resource.ToBitmap(true);
                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    using var flippedColorizedImage = Shared.Create(Imaging.Image.FromBitmap(bitmap));
                    this.DisplayImage.UpdateImage(flippedColorizedImage);
                }
                else
                {
                    this.DisplayImage.UpdateImage(sharedColorizedImage);
                }

                if (this.Image.Visibility != Visibility.Visible)
                {
                    this.Image.Visibility = Visibility.Visible;
                }

                // Update the image size if it's changed
                if ((this.Image.Width != this.DisplayImage.Image.PixelWidth) || (this.Image.Height != this.DisplayImage.Image.PixelHeight))
                {
                    this.Image.Width = this.DisplayImage.Image.PixelWidth;
                    this.Image.Height = this.DisplayImage.Image.PixelHeight;
                }
            }
        }
    }
}
