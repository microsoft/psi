// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1310 // FieldNamesMustNotContainUnderscore

namespace Test.Psi.Imaging
{
    using System;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ImageTester
    {
        private Image testImage_Gray = Image.FromBitmap(Properties.Resources.TestImage_Gray);
        private Image testImage_GrayDrawCircle = Image.FromBitmap(Properties.Resources.TestImage_GrayDrawCircle);
        private Image testImage_GrayDrawLine = Image.FromBitmap(Properties.Resources.TestImage_GrayDrawLine);
        private Image testImage_GrayDrawRect = Image.FromBitmap(Properties.Resources.TestImage_GrayDrawRect);
        private Image testImage_GrayDrawText = Image.FromBitmap(Properties.Resources.TestImage_GrayDrawText);
        private Image testImage_GrayFlip = Image.FromBitmap(Properties.Resources.TestImage_GrayFlip);
        private Image testImage_GrayResized = Image.FromBitmap(Properties.Resources.TestImage_GrayResized);
        private Image testImage_GrayRotate = Image.FromBitmap(Properties.Resources.TestImage_GrayRotate);
        private Image testImage = Image.FromBitmap(Properties.Resources.TestImage);
        private Image testImage2 = Image.FromBitmap(Properties.Resources.TestImage2);
        private Image testImage2_Threshold = Image.FromBitmap(Properties.Resources.TestImage2_Threshold);
        private Image testImage2_RedChannel = Image.FromBitmap(Properties.Resources.TestImage2_RedChannel);
        private Image testImage2_GreenChannel = Image.FromBitmap(Properties.Resources.TestImage2_GreenChannel);
        private Image testImage2_BlueChannel = Image.FromBitmap(Properties.Resources.TestImage2_BlueChannel);
        private Image testImage2_CopyImage = Image.FromBitmap(Properties.Resources.TestImage2_CopyImage);
        private Image testImage2_Invert = Image.FromBitmap(Properties.Resources.TestImage2_Invert);
        private Image testImage2_Mask = Image.FromBitmap(Properties.Resources.TestImage2_Mask);
        private Image testImage2_FlipHoriz = Image.FromBitmap(Properties.Resources.TestImage2_FlipHoriz);
        private Image testImage2_FlipVert = Image.FromBitmap(Properties.Resources.TestImage2_FlipVert);
        private Image testImage2_Rotate_Neg10 = Image.FromBitmap(Properties.Resources.TestImage2_Rotate_Neg10);
        private Image testImage2_Rotate_Neg10_Loose = Image.FromBitmap(Properties.Resources.TestImage2_Rotate_Neg10_Loose);
        private Image testImage2_Rotate_110 = Image.FromBitmap(Properties.Resources.TestImage2_Rotate_110);
        private Image testImage2_Rotate_110_Loose = Image.FromBitmap(Properties.Resources.TestImage2_Rotate_110_Loose);
        private Image testImage2_DrawRect = Image.FromBitmap(Properties.Resources.TestImage2_DrawRect);
        private Image testImage2_DrawLine = Image.FromBitmap(Properties.Resources.TestImage2_DrawLine);
        private Image testImage2_DrawCircle = Image.FromBitmap(Properties.Resources.TestImage2_DrawCircle);
        private Image testImage2_DrawText = Image.FromBitmap(Properties.Resources.TestImage2_DrawText);
        private Image testImage2_AbsDiff = Image.FromBitmap(Properties.Resources.TestImage2_AbsDiff);
        private Image testImage_0_0_200_100 = Image.FromBitmap(Properties.Resources.TestImage_Crop_0_0_200_100);
        private Image testImage_153_57_103_199 = Image.FromBitmap(Properties.Resources.TestImage_Crop_153_57_103_199);
        private Image testImage_73_41_59_37 = Image.FromBitmap(Properties.Resources.TestImage_Crop_73_41_59_37);
        private Image testImage_50_25_Cubic = Image.FromBitmap(Properties.Resources.TestImage_Scale_50_25_Cubic);
        private Image testImage_150_125_Point = Image.FromBitmap(Properties.Resources.TestImage_Scale_150_125_Point);
        private Image testImage_25_200_Linear = Image.FromBitmap(Properties.Resources.TestImage_Scale_25_200_Linear);

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayDrawCircle()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            sharedImage.Resource.DrawCircle(new System.Drawing.Point(0, 0), 100, System.Drawing.Color.Red, 3);
            this.AssertAreImagesEqual(this.testImage_GrayDrawCircle, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayDrawLine()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            sharedImage.Resource.DrawLine(new System.Drawing.Point(0, 0), new System.Drawing.Point(100, 100), System.Drawing.Color.Red, 3);
            this.AssertAreImagesEqual(this.testImage_GrayDrawLine, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayDrawRect()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            sharedImage.Resource.DrawRectangle(new System.Drawing.Rectangle(0, 0, 20, 20), System.Drawing.Color.White, 3);
            this.AssertAreImagesEqual(this.testImage_GrayDrawRect, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayDrawText()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            sharedImage.Resource.DrawText("Test", new System.Drawing.Point(0, 20), System.Drawing.Color.Red);
            this.AssertAreImagesEqual(this.testImage_GrayDrawText, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayFlip()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.Flip(sharedImage.Resource, FlipMode.AlongHorizontalAxis);
            this.AssertAreImagesEqual(this.testImage_GrayFlip, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayResize()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            this.testImage_Gray.Resize(100, 100, SamplingMode.Bilinear);
            this.AssertAreImagesEqual(this.testImage_GrayResized, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_GrayRotate()
        {
            using var sharedImage = ImagePool.GetOrCreate(this.testImage_Gray.Width, this.testImage_Gray.Height, this.testImage_Gray.PixelFormat);
            this.testImage_Gray.CopyTo(sharedImage.Resource);
            sharedImage.Resource.Rotate(20.0f, SamplingMode.Bilinear);
            this.AssertAreImagesEqual(this.testImage_GrayRotate, sharedImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_FlipViaOperator()
        {
            using (var pipeline = Pipeline.Create("FlipViaOperator"))
            {
                using (var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, this.testImage2.PixelFormat))
                {
                    this.testImage2.CopyTo(sharedImage.Resource);
                    Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Flip(FlipMode.None).Do((img) =>
                    {
                        this.AssertAreImagesEqual(this.testImage2, img.Resource);
                    });
                    Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Flip(FlipMode.AlongHorizontalAxis).Do((img) =>
                    {
                        this.AssertAreImagesEqual(this.testImage2_FlipHoriz, img.Resource);
                    });
                    Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Flip(FlipMode.AlongVerticalAxis).Do((img) =>
                    {
                        this.AssertAreImagesEqual(this.testImage2_FlipVert, img.Resource);
                    });
                    pipeline.Run();
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_RotateViaOperator()
        {
            using (var pipeline = Pipeline.Create("RotateViaOperator"))
            {
                using (var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, this.testImage2.PixelFormat))
                {
                    this.testImage2.CopyTo(sharedImage.Resource);
                    Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Rotate(-10.0f, SamplingMode.Point).Do((img) =>
                    {
                        this.AssertAreImagesEqual(this.testImage2_Rotate_Neg10, img.Resource);
                    });
                    Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Rotate(110.0f, SamplingMode.Point).Do((img) =>
                    {
                        this.AssertAreImagesEqual(this.testImage2_Rotate_110, img.Resource);
                    });
                    Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Rotate(-10.0f, SamplingMode.Point, RotationFitMode.Loose).Do((img) =>
                    {
                        this.AssertAreImagesEqual(this.testImage2_Rotate_Neg10_Loose, img.Resource);
                    });
                    Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Rotate(110.0f, SamplingMode.Point, RotationFitMode.Loose).Do((img) =>
                    {
                        this.AssertAreImagesEqual(this.testImage2_Rotate_110_Loose, img.Resource);
                    });
                    pipeline.Run();
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_DrawRectangleViaOperator()
        {
            using (var pipeline = Pipeline.Create("DrawRectangleViaOperator"))
            {
                using (var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, this.testImage2.PixelFormat))
                {
                    this.testImage2.CopyTo(sharedImage.Resource);
                    Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).DrawRectangle(new System.Drawing.Rectangle(20, 20, 255, 255), System.Drawing.Color.White, 3).Do((img) =>
                    {
                        this.AssertAreImagesEqual(this.testImage2_DrawRect, img.Resource);
                    });
                    pipeline.Run();
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_DrawLineViaOperator()
        {
            using (var pipeline = Pipeline.Create("DrawLineViaOperator"))
            {
                using (var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, this.testImage2.PixelFormat))
                {
                    this.testImage2.CopyTo(sharedImage.Resource);
                    Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).DrawLine(new System.Drawing.Point(0, 0), new System.Drawing.Point(255, 255), System.Drawing.Color.White, 3).Do((img) =>
                    {
                        this.AssertAreImagesEqual(this.testImage2_DrawLine, img.Resource);
                    });
                    pipeline.Run();
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_DrawCircleViaOperator()
        {
            using (var pipeline = Pipeline.Create("DrawCircleViaOperator"))
            {
                using (var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, this.testImage2.PixelFormat))
                {
                    this.testImage2.CopyTo(sharedImage.Resource);
                    Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).DrawCircle(new System.Drawing.Point(250, 250), 100, System.Drawing.Color.White, 3).Do((img) =>
                    {
                        this.AssertAreImagesEqual(this.testImage2_DrawCircle, img.Resource);
                    });
                    pipeline.Run();
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_DrawTextViaOperator()
        {
            using (var pipeline = Pipeline.Create("DrawTextViaOperator"))
            {
                using (var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, this.testImage2.PixelFormat))
                {
                    this.testImage2.CopyTo(sharedImage.Resource);
                    Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).DrawText("Testing", new System.Drawing.Point(100, 100), System.Drawing.Color.White).Do((img) =>
                    {
                        this.AssertAreImagesEqual(this.testImage2_DrawText, img.Resource);
                    });
                    pipeline.Run();
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_CopyImage()
        {
            using var destImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, this.testImage2.PixelFormat);
            destImage.Resource.Clear(System.Drawing.Color.Black);
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(50, 300, 100, 255);
            this.testImage2.CopyTo(rect, destImage.Resource, new System.Drawing.Point(-10, 0), this.testImage2_Mask);
            this.AssertAreImagesEqual(this.testImage2_CopyImage, destImage.Resource);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_Invert()
        {
            using var pipeline = Pipeline.Create("ImageInvert");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, this.testImage2.PixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Invert().Do((img) =>
            {
                this.AssertAreImagesEqual(this.testImage2_Invert, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_AbsDiff()
        {
            using var pipeline = Pipeline.Create("ImageAbsDiff");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2_DrawCircle.Width, this.testImage2_DrawCircle.Height, this.testImage2_DrawCircle.PixelFormat);
            this.testImage2_DrawCircle.CopyTo(sharedImage.Resource);
            using var sharedImage2 = ImagePool.GetOrCreate(this.testImage2_DrawRect.Width, this.testImage2_DrawRect.Height, this.testImage2_DrawRect.PixelFormat);
            this.testImage2_DrawRect.CopyTo(sharedImage2.Resource);
            Generators.Sequence(pipeline, new[] { (sharedImage, sharedImage2) }, default, null, keepOpen: false).AbsDiff().Do((img) =>
            {
                this.AssertAreImagesEqual(this.testImage2_AbsDiff, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_Threshold()
        {
            using var pipeline = Pipeline.Create("ImageThreshold");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, this.testImage2.PixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false).Threshold(10, 170, Threshold.Binary).Do((img) =>
            {
                this.AssertAreImagesEqual(this.testImage2_Threshold, img.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_ExtractChannels()
        {
            using var pipeline = Pipeline.Create("ImageExtractChannel");
            using var sharedImage = ImagePool.GetOrCreate(this.testImage2.Width, this.testImage2.Height, this.testImage2.PixelFormat);
            this.testImage2.CopyTo(sharedImage.Resource);
            var seq = Generators.Sequence(pipeline, new[] { sharedImage }, default, null, keepOpen: false);
            var rchannel = seq.ExtractChannel(0);
            var gchannel = seq.ExtractChannel(1);
            var bchannel = seq.ExtractChannel(2);
            rchannel.Join(gchannel.Join(bchannel)).Do((imgs) =>
            {
                this.AssertAreImagesEqual(this.testImage2_RedChannel, imgs.Item1.Resource);
                this.AssertAreImagesEqual(this.testImage2_GreenChannel, imgs.Item2.Resource);
                this.AssertAreImagesEqual(this.testImage2_BlueChannel, imgs.Item3.Resource);
            });
            pipeline.Run();
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_CropViaOperator()
        {
            // Test that the pipeline's operator Crop() works on a stream of images and random rectangles
            using (var pipeline = Pipeline.Create("CropViaOperator"))
            {
                var generator = Generators.Sequence(pipeline, 1, x => x + 1, 100, TimeSpan.FromTicks(1));
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
                    images.Join(rects, Reproducible.Nearest<System.Drawing.Rectangle>()).Crop();
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
                this.AssertAreImagesEqual(this.testImage, croppedImage);
            }

            // Crop an upper-left region and verify
            using (var croppedImage = this.testImage.Crop(0, 0, 200, 100))
            {
                this.AssertAreImagesEqual(this.testImage_0_0_200_100, croppedImage);
            }

            // Crop a lower-right region and verify
            using (var croppedImage = this.testImage.Crop(153, 57, 103, 199))
            {
                this.AssertAreImagesEqual(this.testImage_153_57_103_199, croppedImage);
            }

            // Crop an interior region and verify
            using (var croppedImage = this.testImage.Crop(73, 41, 59, 37))
            {
                this.AssertAreImagesEqual(this.testImage_73_41_59_37, croppedImage);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_CropDifferentRegions()
        {
            // Crop a slightly different interior region of the same size and verify that the data is different (as a sanity check)
            using (var croppedImage = this.testImage.Crop(74, 42, 59, 37))
            {
                var croppedImage_74_42_59_37 = croppedImage;
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
            var encodedImage = this.testImage.Encode(new ImageToPngStreamEncoder());
            var decodedImage = encodedImage.Decode(new ImageFromStreamDecoder());
            this.AssertAreImagesEqual(this.testImage, decodedImage);
        }

        [TestMethod]
        [Timeout(60000)]
        public void EncodeImageJpg()
        {
            // Crop a slightly different interior region of the same size and verify that the data is different (as a sanity check)
            var encodedImage = this.testImage.Encode(new ImageToJpegStreamEncoder());
            var decodedImage = encodedImage.Decode(new ImageFromStreamDecoder());
            var decodedImage2 = encodedImage.Decode(new ImageFromStreamDecoder());
            this.AssertAreImagesEqual(decodedImage, decodedImage2);
        }

        [TestMethod]
        [Timeout(60000)]
        public void EncodedImage_Serialize()
        {
            // encode an image with low compression (higher quality)
            var jpegEncoder = new ImageToJpegStreamEncoder() { QualityLevel = 100 };
            jpegEncoder.QualityLevel = 100;
            var encodedImage = this.testImage.Encode(jpegEncoder);

            // serialize the encoded image
            var bw = new BufferWriter(0);
            Serializer.Serialize(bw, encodedImage, new SerializationContext());
            int serializedLengthHq = bw.Position;

            // deserialize the encoded image and verify the data
            EncodedImage targetEncodedImage = null;
            var br = new BufferReader(bw.Buffer);
            Serializer.Deserialize(br, ref targetEncodedImage, new SerializationContext());
            var decodedImage = encodedImage.Decode(new ImageFromStreamDecoder());
            var targetDecodedImage = targetEncodedImage.Decode(new ImageFromStreamDecoder());
            this.AssertAreImagesEqual(decodedImage, targetDecodedImage);

            // encode an image with high compression (lower quality)
            jpegEncoder = new ImageToJpegStreamEncoder() { QualityLevel = 10 };
            encodedImage = this.testImage.Encode(jpegEncoder);

            // serialize the encoded image
            bw = new BufferWriter(0);
            Serializer.Serialize(bw, encodedImage, new SerializationContext());
            int serializedLengthLq = bw.Position;

            // deserialize the encoded image (into recycled target) and verify the data
            br = new BufferReader(bw.Buffer);
            Serializer.Deserialize(br, ref targetEncodedImage, new SerializationContext());
            decodedImage = encodedImage.Decode(new ImageFromStreamDecoder());
            targetDecodedImage = targetEncodedImage.Decode(new ImageFromStreamDecoder());
            this.AssertAreImagesEqual(decodedImage, targetDecodedImage);

            // verify serialized length is smaller for the more compressed image
            Assert.IsTrue(serializedLengthLq < serializedLengthHq);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Test_Resize()
        {
            // Resize using nearest-neighbor
            this.AssertAreImagesEqual(this.testImage_50_25_Cubic, this.testImage.Resize(
                this.testImage_50_25_Cubic.Width,
                this.testImage_50_25_Cubic.Height,
                SamplingMode.Bicubic));

            // Scale using bilinear
            this.AssertAreImagesEqual(this.testImage_150_125_Point, this.testImage.Resize(
                this.testImage_150_125_Point.Width,
                this.testImage_150_125_Point.Height,
                SamplingMode.Point));

            // Scale using bicubic
            this.AssertAreImagesEqual(this.testImage_25_200_Linear, this.testImage.Resize(
                this.testImage_25_200_Linear.Width,
                this.testImage_25_200_Linear.Height,
                SamplingMode.Bilinear));
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_Compare()
        {
            this.AssertAreImagesEqual(this.testImage, this.testImage);
            this.AssertAreImagesEqual(this.testImage_Gray, this.testImage_Gray);
            ImageError err = new ImageError();
            Assert.IsFalse(this.testImage2.Compare(this.testImage2_DrawRect, 2.0, 0.01, ref err));
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_Scale()
        {
            // Scale using nearest-neighbor
            this.AssertAreImagesEqual(this.testImage_50_25_Cubic, this.testImage.Scale(
                (float)this.testImage_50_25_Cubic.Width / (float)this.testImage.Width,
                (float)this.testImage_50_25_Cubic.Height / (float)this.testImage.Height,
                SamplingMode.Bicubic));

            // Scale using bilinear
            this.AssertAreImagesEqual(this.testImage_150_125_Point, this.testImage.Scale(
                (float)this.testImage_150_125_Point.Width / (float)this.testImage.Width,
                (float)this.testImage_150_125_Point.Height / (float)this.testImage.Height,
                SamplingMode.Point));

            // Scale using bicubic
            this.AssertAreImagesEqual(this.testImage_25_200_Linear, this.testImage.Scale(
                (float)this.testImage_25_200_Linear.Width / (float)this.testImage.Width,
                (float)this.testImage_25_200_Linear.Height / (float)this.testImage.Height,
                SamplingMode.Bilinear));

            // Attempt to scale 16bpp grayscale - should throw NotSupportedException
            var depthImage = new Image(100, 100, PixelFormat.Gray_16bpp);
            Assert.ThrowsException<InvalidOperationException>(() => depthImage.Scale(0.5f, 0.5f, SamplingMode.Point));
        }

        [TestMethod]
        [Timeout(60000)]
        public void Image_Serialize()
        {
            // create the serialization context
            var knownSerializers = new KnownSerializers();
            var context = new SerializationContext(knownSerializers);

            // serialize the image
            var writer = new BufferWriter(this.testImage.Size);
            Serializer.Serialize(writer, this.testImage, context);

            // verify the image type schema
            string contract = TypeSchema.GetContractName(typeof(Image), knownSerializers.RuntimeVersion);
            Assert.IsTrue(knownSerializers.Schemas.ContainsKey(contract));

            // deserialize the image and verify the data
            Image targetImage = null;
            var reader = new BufferReader(writer.Buffer);
            Serializer.Deserialize(reader, ref targetImage, context);
            this.AssertAreImagesEqual(this.testImage, targetImage);
        }

        [TestMethod]
        [Timeout(60000)]
        public void DepthImage_Serialize()
        {
            // generate a "depth image" for testing
            var testImage16bpp = new Image(this.testImage.Width, this.testImage.Height, PixelFormat.Gray_16bpp);
            this.testImage.CopyTo(testImage16bpp);
            var testDepthImage = DepthImage.CreateFrom(testImage16bpp.ToBitmap());

            // create the serialization context
            var knownSerializers = new KnownSerializers();
            var context = new SerializationContext(knownSerializers);

            // serialize the image
            var writer = new BufferWriter(testDepthImage.Size);
            Serializer.Serialize(writer, testDepthImage, context);

            // verify the image type schema
            string contract = TypeSchema.GetContractName(typeof(DepthImage), knownSerializers.RuntimeVersion);
            Assert.IsTrue(knownSerializers.Schemas.ContainsKey(contract));

            // deserialize the image and verify the data
            DepthImage targetDepthImage = null;
            var reader = new BufferReader(writer.Buffer);
            Serializer.Deserialize(reader, ref targetDepthImage, context);
            this.AssertAreImagesEqual(testDepthImage, targetDepthImage);
        }

        private void AssertAreImagesEqual(ImageBase referenceImage, ImageBase subjectImage)
        {
            ImageError err = new ImageError();
            Assert.IsTrue(referenceImage.Compare(subjectImage, 6.0, 0.01, ref err));
        }
    }
}

#pragma warning restore SA1310 // FieldNamesMustNotContainUnderscore
