// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1310 // FieldNamesMustNotContainUnderscore

namespace Test.Psi.Imaging
{
    using System;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ImageTester
    {
        private Image testImage = Image.FromManagedImage(Properties.Resources.TestImage);
        private Image testImage_0_0_200_100 = Image.FromManagedImage(Properties.Resources.TestImage_Crop_0_0_200_100);
        private Image testImage_153_57_103_199 = Image.FromManagedImage(Properties.Resources.TestImage_Crop_153_57_103_199);
        private Image testImage_73_41_59_37 = Image.FromManagedImage(Properties.Resources.TestImage_Crop_73_41_59_37);
        private Image testImage_50_25 = Image.FromManagedImage(Properties.Resources.TestImage_Scale_50_25);
        private Image testImage_150_125 = Image.FromManagedImage(Properties.Resources.TestImage_Scale_150_125);
        private Image testImage_25_200 = Image.FromManagedImage(Properties.Resources.TestImage_Scale_25_200);

        [TestMethod]
        [Timeout(60000)]
        public void Image_CropViaOperator()
        {
            // Test that the pipeline's operator Crop() works on a stream of images and random rectangles
            using (var pipeline = Pipeline.Create("CropViaOperator"))
            {
                var generator = Generators.Sequence(pipeline, 1, x => x + 1, 100);
                var p = Microsoft.Psi.Operators.Process<int, (Shared<Image>, System.Drawing.Rectangle)>(
                    generator,
                    (d, e, s) =>
                    {
                        Random r = new Random();
                        System.Drawing.Rectangle rect = default(System.Drawing.Rectangle);
                        rect.X = r.Next() % this.testImage.Width;
                        rect.Y = r.Next() % this.testImage.Height;
                        rect.Width = r.Next() % (this.testImage.Width - rect.X);
                        rect.Height = r.Next() % (this.testImage.Height - rect.Y);
                        if (rect.Width > 0 && rect.Height > 0)
                        {
                            using (var sharedImage = ImagePool.GetOrCreate(this.testImage.Width, this.testImage.Height, this.testImage.PixelFormat))
                            {
                                this.testImage.CopyTo(sharedImage.Resource);
                                s.Post((sharedImage, rect), e.OriginatingTime);
                            }
                        }
                    }).Crop();
                pipeline.Run();
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_CropViaJoinOperator()
        {
            // Test that the pipeline's operator Crop() works on a stream of images and random rectangles
            using (var pipeline = Pipeline.Create("CropViaOperator"))
            {
                using (var sharedImage = ImagePool.GetOrCreate(this.testImage.Width, this.testImage.Height, this.testImage.PixelFormat))
                {
                    this.testImage.CopyTo(sharedImage.Resource);

                    // Use a non-insignificant interval for both Sequences to ensure that the Join processes all
                    // messages from both streams (default interval of 1-tick is too small to guarantee this).
                    var images = Generators.Sequence(pipeline, sharedImage, x => sharedImage, 100, TimeSpan.FromMilliseconds(1));
                    var rects = Generators.Sequence(
                        pipeline,
                        new System.Drawing.Rectangle(0, 0, 1, 1),
                        x =>
                            {
                                Random r = new Random();
                                System.Drawing.Rectangle rect = default(System.Drawing.Rectangle);
                                rect.X = r.Next(0, this.testImage.Width);
                                rect.Y = r.Next(0, this.testImage.Height);
                                rect.Width = r.Next(1, this.testImage.Width - rect.X);
                                rect.Height = r.Next(1, this.testImage.Height - rect.Y);

                                return rect;
                            },
                        100,
                        TimeSpan.FromMilliseconds(1));
                    images.Join(rects, Match.Best<System.Drawing.Rectangle>()).Crop();
                    pipeline.Run();
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_Crop()
        {
            // Crop the entire image region (a no-op) and verify that the original image is preserved
            using (var croppedImage = this.testImage.Crop(0, 0, this.testImage.Width, this.testImage.Height))
            {
                this.AssertAreImagesEqual(this.testImage, croppedImage.Resource);
            }

            // Crop an upper-left region and verify
            using (var croppedImage = this.testImage.Crop(0, 0, 200, 100))
            {
                this.AssertAreImagesEqual(this.testImage_0_0_200_100, croppedImage.Resource);
            }

            // Crop a lower-right region and verify
            using (var croppedImage = this.testImage.Crop(153, 57, 103, 199))
            {
                this.AssertAreImagesEqual(this.testImage_153_57_103_199, croppedImage.Resource);
            }

            // Crop an interior region and verify
            using (var croppedImage = this.testImage.Crop(73, 41, 59, 37))
            {
                this.AssertAreImagesEqual(this.testImage_73_41_59_37, croppedImage.Resource);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_CropDifferentRegions()
        {
            // Crop a slightly different interior region of the same size and verify that the data is different (as a sanity check)
            using (var sharedCroppedImage = this.testImage.Crop(74, 42, 59, 37))
            {
                var croppedImage_74_42_59_37 = sharedCroppedImage.Resource;
                CollectionAssert.AreNotEqual(
                    this.testImage_73_41_59_37.ReadBytes(this.testImage_73_41_59_37.Size),
                    croppedImage_74_42_59_37.ReadBytes(croppedImage_74_42_59_37.Size));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void EncodeImage()
        {
            // Crop a slightly different interior region of the same size and verify that the data is different (as a sanity check)
            EncodedImage encImg = new EncodedImage();
            encImg.EncodeFrom(this.testImage, new PngBitmapEncoder());
            Image target = new Image(this.testImage.Width, this.testImage.Height, this.testImage.Stride,  this.testImage.PixelFormat);
            encImg.DecodeTo(target);
            this.AssertAreImagesEqual(this.testImage, target);
        }

        [TestMethod]
        [Timeout(60000)]
        public void EncodeImageJpg()
        {
            // Crop a slightly different interior region of the same size and verify that the data is different (as a sanity check)
            EncodedImage encImg = new EncodedImage();
            encImg.EncodeFrom(this.testImage, new JpegBitmapEncoder());
            Image target = new Image(this.testImage.Width, this.testImage.Height, this.testImage.Stride, this.testImage.PixelFormat);
            encImg.DecodeTo(target);
            Image target2 = new Image(this.testImage.Width, this.testImage.Height, this.testImage.Stride, this.testImage.PixelFormat);
            encImg.DecodeTo(target2);
            this.AssertAreImagesEqual(target, target2);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_Scale()
        {
            // Scale using nearest-neighbor
            this.AssertAreImagesEqual(this.testImage_50_25, this.testImage.Scale(0.5f, 0.25f, SamplingMode.Point).Resource);

            // Scale using bilinear
            this.AssertAreImagesEqual(this.testImage_150_125, this.testImage.Scale(1.5f, 1.25f, SamplingMode.Bilinear).Resource);

            // Scale using bicubic
            this.AssertAreImagesEqual(this.testImage_25_200, this.testImage.Scale(0.25f, 2.0f, SamplingMode.Bicubic).Resource);

            // Attempt to scale 16bpp grayscale - should throw NotSupportedException
            var depthImage = Image.Create(100, 100, PixelFormat.Gray_16bpp);
            Assert.ThrowsException<NotSupportedException>(() => depthImage.Scale(0.5f, 0.5f, SamplingMode.Point));
        }

        private void AssertAreImagesEqual(Image referenceImage, Image subjectImage)
        {
            Assert.AreEqual(referenceImage.PixelFormat, subjectImage.PixelFormat);
            Assert.AreEqual(referenceImage.Width, subjectImage.Width);
            Assert.AreEqual(referenceImage.Height, subjectImage.Height);
            Assert.AreEqual(referenceImage.Size, subjectImage.Size);

            // compare one line of the image at a time since a stride may contain padding bytes
            for (int line = 0; line < referenceImage.Height; line++)
            {
                CollectionAssert.AreEqual(
                    referenceImage.ReadBytes(referenceImage.Width * referenceImage.BitsPerPixel / 8, line * referenceImage.Stride),
                    subjectImage.ReadBytes(subjectImage.Width * subjectImage.BitsPerPixel / 8, line * subjectImage.Stride));
            }
        }
    }
}

#pragma warning restore SA1310 // FieldNamesMustNotContainUnderscore
