// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Imaging
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Image = Microsoft.Psi.Imaging.Image;

    /// <summary>
    /// Contains tests for the <see cref="Microsoft.Psi.Imaging.Operators.AverageColor()"/> extension methods.
    /// </summary>
    [TestClass]
    public class AverageColorTests
    {
        private static readonly Image SolidColorImage = Image.FromBitmap(Properties.Resources.SolidColors);
        private static readonly Dictionary<System.Drawing.Rectangle, Color> SolidColorAverages = new Dictionary<System.Drawing.Rectangle, Color>
        {
            // These values are based directly from SolidColors.bmp
            { new System.Drawing.Rectangle(000, 000, 100, 100), Color.FromArgb(000, 000, 000) },
            { new System.Drawing.Rectangle(100, 100, 100, 100), Color.FromArgb(255, 000, 000) },
            { new System.Drawing.Rectangle(200, 200, 100, 100), Color.FromArgb(000, 255, 000) },
            { new System.Drawing.Rectangle(300, 100, 100, 100), Color.FromArgb(000, 000, 255) },
            { new System.Drawing.Rectangle(100, 150, 200, 050), Color.FromArgb(128, 128, 000) },
            { new System.Drawing.Rectangle(200, 150, 200, 050), Color.FromArgb(000, 128, 128) },
            { new System.Drawing.Rectangle(100, 150, 300, 050), Color.FromArgb(085, 085, 085) },
            { new System.Drawing.Rectangle(100, 100, 100, 200), Color.FromArgb(255, 128, 128) },
            { new System.Drawing.Rectangle(100, 200, 200, 100), Color.FromArgb(128, 255, 128) },
            { new System.Drawing.Rectangle(300, 100, 100, 200), Color.FromArgb(128, 128, 255) },
            { new System.Drawing.Rectangle(100, 200, 100, 100), Color.FromArgb(255, 255, 255) },
            { new System.Drawing.Rectangle(100, 100, 300, 200), Color.FromArgb(138, 149, 138) },
            { new System.Drawing.Rectangle(000, 000, 500, 400), Color.FromArgb(041, 045, 041) },
        };

        /// <summary>
        /// Common code shared by the average color tests.
        /// </summary>
        /// <param name="format">The format in which to perform the test.</param>
        private static void AverageSolidColorTestRunner(PixelFormat format)
        {
            var shared = ImagePool.GetOrCreate(SolidColorImage.Width, SolidColorImage.Height, format);
            var image = shared.Resource;
            SolidColorImage.CopyTo(image);
            var results = new Dictionary<System.Drawing.Rectangle, Color>();

            using (var pipeline = Pipeline.Create())
            {
                var source = Generators.Sequence(
                    pipeline,
                    SolidColorAverages.Keys.Select(rect => (rect, image.AverageColor(rect.Left, rect.Top, rect.Width, rect.Height))),
                    TimeSpan.FromTicks(1))
                    .Do(r =>
                {
                    (var rect, var color) = r;

                    // Simply store the resulting value and compare it later at the bottom of this method.
                    results[rect] = color;
                });
                pipeline.Run(new ReplayDescriptor(DateTime.UtcNow, DateTime.MaxValue));
            }

            foreach (var expectation in SolidColorAverages)
            {
                var expectedColor = expectation.Value;
                var returnedColor = results[expectation.Key];

                // Compensate for grayscale formats.
                if (format == PixelFormat.Gray_16bpp || format == PixelFormat.Gray_8bpp)
                {
                    var gray = Microsoft.Psi.Imaging.Operators.Rgb2Gray(expectedColor.R, expectedColor.G, expectedColor.B);
                    expectedColor = Color.FromArgb((int)gray, (int)gray, (int)gray);
                }

                // Assert.AreEqual(expectedColor, returnedColor, $"Region {expectation.Key}");
                Assert.AreEqual(expectedColor.R, returnedColor.R, 1, $"Average red channel for solid color test region {expectation.Key}");
                Assert.AreEqual(expectedColor.G, returnedColor.G, 1, $"Average green channel for solid color test region {expectation.Key}");
                Assert.AreEqual(expectedColor.B, returnedColor.B, 1, $"Average blue channel for solid color test region {expectation.Key}");
            }
        }

        /// <summary>
        /// Tests the <see cref="Microsoft.Psi.Imaging.Operators.AverageColor(Image)"/> extension methods over a combination of solid red/green/blue/black/white colored blocks in BGRA_32bpp format.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void AverageSolidColors_BGRA_32bpp()
        {
            AverageSolidColorTestRunner(PixelFormat.BGRA_32bpp);
        }

        /// <summary>
        /// Tests the <see cref="Microsoft.Psi.Imaging.Operators.AverageColor(Image)"/> extension methods over a combination of solid red/green/blue/black/white colored blocks in BGRX_32bpp format.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void AverageSolidColors_BGRX_32bpp()
        {
            AverageSolidColorTestRunner(PixelFormat.BGRX_32bpp);
        }

        /// <summary>
        /// Tests the <see cref="Microsoft.Psi.Imaging.Operators.AverageColor(Image)"/> extension methods over a combination of solid red/green/blue/black/white colored blocks in BGR_24bpp format.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void AverageSolidColors_BGR_24bpp()
        {
            AverageSolidColorTestRunner(PixelFormat.BGR_24bpp);
        }

        /// <summary>
        /// Tests the <see cref="Microsoft.Psi.Imaging.Operators.AverageColor(Image)"/> extension methods over a combination of solid red/green/blue/black/white colored blocks in RGBA_64bpp format.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void AverageSolidColors_RGBA_64bpp()
        {
            AverageSolidColorTestRunner(PixelFormat.RGBA_64bpp);
        }

        /// <summary>
        /// Tests the <see cref="Microsoft.Psi.Imaging.Operators.AverageColor(Image)"/> extension methods over a combination of solid red/green/blue/black/white colored blocks in Gray_16bpp format.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void AverageSolidColors_Gray_16bpp()
        {
            AverageSolidColorTestRunner(PixelFormat.Gray_16bpp);
        }

        /// <summary>
        /// Tests the <see cref="Microsoft.Psi.Imaging.Operators.AverageColor(Image)"/> extension methods over a combination of solid red/green/blue/black/white colored blocks in Gray_8bpp format.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void AverageSolidColors_Gray_8bpp()
        {
            AverageSolidColorTestRunner(PixelFormat.Gray_8bpp);
        }
    }
}
