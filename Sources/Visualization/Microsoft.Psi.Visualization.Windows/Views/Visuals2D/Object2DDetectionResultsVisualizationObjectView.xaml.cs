// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Linq;
    using System.Windows;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for Object2DDetectionResultsVisualizationObjectView.xaml.
    /// </summary>
    public partial class Object2DDetectionResultsVisualizationObjectView : Object2DDetectionResultsVisualizationObjectViewBase, IDisposable
    {
        private readonly Font font = new (System.Drawing.FontFamily.GenericSansSerif, 9f, System.Drawing.FontStyle.Regular);
        private readonly (Pen, Color)[] colorPalette = new[]
        {
            (Pens.Red, Color.Red),
            (Pens.Blue, Color.Blue),
            (Pens.Green, Color.Green),
            (Pens.Orange, Color.Orange),
            (Pens.Purple, Color.Purple),
            (Pens.Cyan, Color.Cyan),
            (Pens.Magenta, Color.Magenta),
            (Pens.Yellow, Color.Yellow),
            (Pens.White, Color.White),
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="Object2DDetectionResultsVisualizationObjectView"/> class.
        /// </summary>
        public Object2DDetectionResultsVisualizationObjectView()
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
        /// Gets the visualization object.
        /// </summary>
        public Object2DDetectionResultsVisualizationObject Object2DDetectionResultsVisualizationObject
            => this.VisualizationObject as Object2DDetectionResultsVisualizationObject;

        /// <inheritdoc />
        public void Dispose()
            => this.font?.Dispose();

        /// <inheritdoc/>
        protected override void UpdateView()
        {
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.Object2DDetectionResultsVisualizationObject.CurrentValue) ||
                e.PropertyName == nameof(this.Object2DDetectionResultsVisualizationObject.HorizontalFlip) ||
                e.PropertyName == nameof(this.Object2DDetectionResultsVisualizationObject.ShowMask) ||
                e.PropertyName == nameof(this.Object2DDetectionResultsVisualizationObject.ShowBoundingBox) ||
                e.PropertyName == nameof(this.Object2DDetectionResultsVisualizationObject.ShowClass) ||
                e.PropertyName == nameof(this.Object2DDetectionResultsVisualizationObject.ShowInstanceId))
            {
                this.ShowCurrentImage();
            }
        }

        private void ShowCurrentImage()
        {
            // Get the current image
            var object2DDetectionResults = this.Object2DDetectionResultsVisualizationObject.CurrentValue.GetValueOrDefault().Data;

            if (object2DDetectionResults == null)
            {
                this.Image.Visibility = Visibility.Hidden;
            }
            else
            {
                // Update the display image
                using var sharedImage = ImagePool.GetOrCreate(
                    object2DDetectionResults.ImageSize.Width,
                    object2DDetectionResults.ImageSize.Height,
                    PixelFormat.BGRA_32bpp);

                // Draw the object
                sharedImage.Resource.Clear(System.Drawing.Color.Transparent);
                using var bmp = sharedImage.Resource.ToBitmap(false);
                using var graphics = Graphics.FromImage(bmp);
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                var colorIndex = -1;
                foreach (var detection in object2DDetectionResults.Detections.OrderBy(d => d.DetectionScore))
                {
                    (var pen, var color) = this.colorPalette[++colorIndex % this.colorPalette.Length];
                    var rect = new Rectangle(
                        (int)(detection.BoundingBox.X + 0.5f),
                        (int)(detection.BoundingBox.Y + 0.5f),
                        (int)(detection.BoundingBox.Width + 0.5f),
                        (int)(detection.BoundingBox.Height + 0.5f));

                    var mask = new Bitmap(rect.Width, rect.Height);
                    for (var y = 0; y < detection.BoundingBox.Height; y++)
                    {
                        for (var x = 0; x < detection.BoundingBox.Width; x++)
                        {
                            mask.SetPixel(x, y, Color.FromArgb((int)(detection.Mask[y][x] * 128), color));
                        }
                    }

                    if (this.Object2DDetectionResultsVisualizationObject.ShowMask)
                    {
                        graphics.DrawImage(mask, rect);
                    }

                    if (this.Object2DDetectionResultsVisualizationObject.ShowBoundingBox)
                    {
                        graphics.DrawRectangle(pen, rect);
                    }

                    if (this.Object2DDetectionResultsVisualizationObject.ShowClass)
                    {
                        if (this.Object2DDetectionResultsVisualizationObject.ShowInstanceId)
                        {
                            graphics.DrawString($"{detection.Class} [{detection.InstanceId}] {(int)(detection.DetectionScore * 100)}", this.font, Brushes.White, rect.X, rect.Y - 1.8f * this.font.Size);
                        }
                        else
                        {
                            graphics.DrawString($"{detection.Class} {(int)(detection.DetectionScore * 100)}", this.font, Brushes.White, rect.X, rect.Y - 1.8f * this.font.Size);
                        }
                    }
                    else
                    {
                        if (this.Object2DDetectionResultsVisualizationObject.ShowInstanceId)
                        {
                            graphics.DrawString($"[{detection.InstanceId}]", this.font, Brushes.White, rect.X, rect.Y - 1.8f * this.font.Size);
                        }
                    }
                }

                sharedImage.Resource.CopyFrom(bmp);

                if (this.Object2DDetectionResultsVisualizationObject.HorizontalFlip)
                {
                    // Flip the image before displaying it
                    Bitmap bitmap = sharedImage.Resource.ToBitmap(true);
                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    using var flippedColorizedImage = Shared.Create(Imaging.Image.FromBitmap(bitmap));
                    this.DisplayImage.UpdateImage(flippedColorizedImage);
                }
                else
                {
                    this.DisplayImage.UpdateImage(sharedImage);
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
