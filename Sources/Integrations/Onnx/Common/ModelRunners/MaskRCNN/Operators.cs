// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System;
    using System.Drawing;
    using System.Linq;
    using Microsoft.Psi.Imaging;
    using Image = Microsoft.Psi.Imaging.Image;

    /// <summary>
    /// Implements operators for processing Mask R-CNN types.
    /// </summary>
    public static partial class Operators
    {
        private const float FONTSIZE = 9f;
        private static Font font = new Font(FontFamily.GenericSansSerif, FONTSIZE, FontStyle.Regular);
        private static Bitmap mask = new Bitmap(28, 28);
        private static Brush labelBrush = Brushes.White;
        private static (Pen, Color)[] rectangleAndMaskColors = new[]
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
        /// Render <see cref="MaskRCNNDetectionResults"/> to a transparent image with object
        /// bounding boxes, labels and masks.
        /// </summary>
        /// <param name="results"><see cref="MaskRCNNDetectionResults"/> to render.</param>
        /// <param name="sharedImageAllocator ">Optional image allocator for creating new shared image.</param>
        /// <returns>Rendered results.</returns>
        public static Shared<Image> Render(
            MaskRCNNDetectionResults results,
            Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null)
        {
            // create image and preprare to draw on it
            var sharedImage = (sharedImageAllocator ?? ImagePool.GetOrCreate)(results.ImageWidth, results.ImageHeight, PixelFormat.BGRA_32bpp);
            sharedImage.Resource.Clear(Color.Transparent);
            using var bmp = sharedImage.Resource.ToBitmap(false);
            using var graphics = Graphics.FromImage(bmp);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None; // no antialiasing
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            var colorIndex = -1;
            foreach (var detection in results.Detections.OrderBy(d => d.Confidence))
            {
                (var pen, var color) = rectangleAndMaskColors[++colorIndex % rectangleAndMaskColors.Length];
                var rect = new Rectangle(
                    (int)(detection.Bounds.X + 0.5f),
                    (int)(detection.Bounds.Y + 0.5f),
                    (int)(detection.Bounds.Width + 0.5f),
                    (int)(detection.Bounds.Height + 0.5f));

                for (var y = 0; y < 28; y++)
                {
                    for (var x = 0; x < 28; x++)
                    {
                        var m = (int)(128 * detection.Mask[x + y * 28]);
                        mask.SetPixel(x, y, Color.FromArgb(m, color));
                    }
                }

                graphics.DrawImage(mask, rect);
                graphics.DrawRectangle(pen, rect);
                graphics.DrawString($"{detection.Label} {(int)(detection.Confidence * 100)}", font, labelBrush, rect.X, rect.Y - 1.8f * FONTSIZE);

                sharedImage.Resource.CopyFrom(bmp);
            }

            return sharedImage;
        }

        /// <summary>
        /// Render <see cref="MaskRCNNDetectionResults"/> to a stream of transparent images
        /// with object bounding boxes, labels and masks.
        /// </summary>
        /// <param name="source">Stream of <see cref="MaskRCNNDetectionResults"/>.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="sharedImageAllocator ">Optional image allocator for creating new shared image.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>Stream of rendered results.</returns>
        public static IProducer<Shared<Image>> Render(
            this IProducer<MaskRCNNDetectionResults> source,
            DeliveryPolicy<MaskRCNNDetectionResults> deliveryPolicy = null,
            Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null,
            string name = nameof(Render))
        {
            return source.Select(r => Render(r, sharedImageAllocator), deliveryPolicy, name);
        }
    }
}
