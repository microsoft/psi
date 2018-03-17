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

        [TestMethod]
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
                            var sharedImage = Microsoft.Psi.Imaging.ImagePool.GetOrCreate(this.testImage.Width, this.testImage.Height, this.testImage.PixelFormat);
                            this.testImage.CopyTo(sharedImage.Resource);
                            s.Post((sharedImage, rect), e.OriginatingTime);
                        }
                    }).Crop();
                pipeline.Run();
            }
        }

        [TestMethod]
        public void Image_CropViaJoinOperator()
        {
            // Test that the pipeline's operator Crop() works on a stream of images and random rectangles
            using (var pipeline = Pipeline.Create("CropViaOperator"))
            {
                var sharedImage = Microsoft.Psi.Imaging.ImagePool.GetOrCreate(this.testImage.Width, this.testImage.Height, this.testImage.PixelFormat);
                this.testImage.CopyTo(sharedImage.Resource);
                var images = Generators.Sequence(pipeline, sharedImage, x => sharedImage, 100);
                var rects = Generators.Sequence(
                    pipeline,
                    default(System.Drawing.Rectangle),
                    x =>
                        {
                            Random r = new Random();
                            System.Drawing.Rectangle rect = default(System.Drawing.Rectangle);
                            rect.X = r.Next() % this.testImage.Width;
                            rect.Y = r.Next() % this.testImage.Height;
                            rect.Width = r.Next() % (this.testImage.Width - rect.X);
                            rect.Height = r.Next() % (this.testImage.Height - rect.Y);
                            if (rect.Width <= 0)
                            {
                                rect.Width = 1;
                            }

                            if (rect.Height <= 0)
                            {
                                rect.Height = 1;
                            }

                            return rect;
                        },
                    100);
                images.Crop(rects, Match.Best<System.Drawing.Rectangle>());
                pipeline.Run();
            }
        }

        [TestMethod]
        public void Image_Crop()
        {
            // Crop the entire image region (a no-op) and verify that the original image is preserved
            this.AssertAreImagesEqual(this.testImage, this.testImage.Crop(0, 0, this.testImage.Width, this.testImage.Height).Resource);

            // Crop an upper-left region and verify
            this.AssertAreImagesEqual(this.testImage_0_0_200_100, this.testImage.Crop(0, 0, 200, 100).Resource);

            // Crop a lower-right region and verify
            this.AssertAreImagesEqual(this.testImage_153_57_103_199, this.testImage.Crop(153, 57, 103, 199).Resource);

            // Crop an interior region and verify
            this.AssertAreImagesEqual(this.testImage_73_41_59_37, this.testImage.Crop(73, 41, 59, 37).Resource);
        }

        [TestMethod]
        public void Image_CropDifferentRegions()
        {
            // Crop a slightly different interior region of the same size and verify that the data is different (as a sanity check)
            Image croppedImage_74_42_59_37 = this.testImage.Crop(74, 42, 59, 37).Resource;
            CollectionAssert.AreNotEqual(
                this.testImage_73_41_59_37.ReadBytes(this.testImage_73_41_59_37.Size),
                croppedImage_74_42_59_37.ReadBytes(croppedImage_74_42_59_37.Size));
        }

        [TestMethod]
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

        private void AssertAreImagesEqual(Image referenceImage, Image subjectImage)
        {
            Assert.AreEqual(referenceImage.PixelFormat, subjectImage.PixelFormat);
            Assert.AreEqual(referenceImage.Width, subjectImage.Width);
            Assert.AreEqual(referenceImage.Height, subjectImage.Height);
            Assert.AreEqual(referenceImage.Size, subjectImage.Size);
            CollectionAssert.AreEqual(referenceImage.ReadBytes(referenceImage.Size), subjectImage.ReadBytes(subjectImage.Size));
        }
    }
}

#pragma warning restore SA1310 // FieldNamesMustNotContainUnderscore
