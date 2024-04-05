// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    /// <summary>
    /// Represents object detection results over an image.
    /// </summary>
    public class Object2DDetectionResults
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Object2DDetectionResults"/> class.
        /// </summary>
        /// <param name="imageSize">The image size.</param>
        /// <param name="detections">The set of detections.</param>
        public Object2DDetectionResults(Size imageSize, List<Object2DDetection> detections = null)
        {
            this.ImageSize = imageSize;
            this.Detections = detections ?? new ();
        }

        /// <summary>
        /// Gets the image size.
        /// </summary>
        public Size ImageSize { get; }

        /// <summary>
        /// Gets the list of detections.
        /// </summary>
        public List<Object2DDetection> Detections { get; } = new ();

        /// <summary>
        /// Render <see cref="Object2DDetectionResults"/> to a transparent image with object
        /// bounding boxes, labels and masks.
        /// </summary>
        /// <param name="font">The font to use.</param>
        /// <param name="fontBrush">The font color brush.</param>
        /// <param name="colorPalette">The color palette to use.</param>
        /// <param name="sharedImageAllocator ">Optional image allocator for creating new shared image.</param>
        /// <returns>Rendered results.</returns>
        public Shared<Image> Render(
            Font font,
            Brush fontBrush,
            (Pen, Color)[] colorPalette,
            Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            // create image and preprare to draw on it
            var sharedImage = (sharedImageAllocator ?? ImagePool.GetOrCreate)(this.ImageSize.Width, this.ImageSize.Height, PixelFormat.BGRA_32bpp);
            sharedImage.Resource.Clear(Color.Transparent);
            using var bmp = sharedImage.Resource.ToBitmap(false);
            using var graphics = Graphics.FromImage(bmp);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None; // no antialiasing
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            var colorIndex = -1;
            foreach (var detection in this.Detections.OrderBy(d => d.DetectionScore))
            {
                (var pen, var color) = colorPalette[++colorIndex % colorPalette.Length];
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

                graphics.DrawImage(mask, rect);
                graphics.DrawRectangle(pen, rect);
                graphics.DrawString($"{detection.Class} {(int)(detection.DetectionScore * 100)}", font, fontBrush, rect.X, rect.Y - 1.8f * font.Size);

                sharedImage.Resource.CopyFrom(bmp);
            }

            return sharedImage;
        }
    }
}
