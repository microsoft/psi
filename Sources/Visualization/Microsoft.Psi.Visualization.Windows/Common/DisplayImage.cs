// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Base;

    /// <summary>
    /// Represents an displayable image based on a <see cref="Microsoft.Psi.Imaging.Image"/>.
    /// </summary>
    public class DisplayImage : ObservableObject
    {
        private object imageLock = new object();
        private Shared<Image> psiImage;
        private FrameCounter renderedFrames = new FrameCounter();
        private FrameCounter receivedFrames = new FrameCounter();
        private WriteableBitmap image;

        /// <summary>
        /// Gets underlying <see cref="PsiImage"/>.
        /// </summary>
        public Shared<Image> PsiImage => this.psiImage;

        /// <summary>
        /// Gets or sets the frame counter of rendered frames.
        /// </summary>
        public FrameCounter RenderedFrames
        {
            get => this.renderedFrames;
            set { this.Set(nameof(this.RenderedFrames), ref this.renderedFrames, value); }
        }

        /// <summary>
        /// Gets or sets the frame counter of received frames.
        /// </summary>
        public FrameCounter ReceivedFrames
        {
            get => this.receivedFrames;
            set { this.Set(nameof(this.ReceivedFrames), ref this.receivedFrames, value); }
        }

        /// <summary>
        /// Gets or sets the display image.
        /// </summary>
        public WriteableBitmap Image
        {
            get => this.image;
            set { this.Set(nameof(this.Image), ref this.image, value); }
        }

        /// <summary>
        /// Update the underlying image with the specified image.
        /// </summary>
        /// <param name="image">New image.</param>
        public void UpdateImage(Shared<Image> image)
        {
            lock (this.imageLock)
            {
                this.psiImage?.Dispose();

                if (image == null || image.Resource == null)
                {
                    this.psiImage = null;
                    return;
                }

                this.psiImage = image.AddRef();
            }

            this.UpdateBitmap();
        }

        /// <summary>
        /// Update the underlying image with the specified image.
        /// </summary>
        /// <param name="encodedImage">New encoded image.</param>
        public void UpdateImage(Shared<EncodedImage> encodedImage)
        {
            lock (this.imageLock)
            {
                if (encodedImage == null || encodedImage.Resource == null)
                {
                    this.psiImage?.Dispose();
                    this.psiImage = null;
                    return;
                }

                if (this.psiImage == null)
                {
                    this.psiImage = Shared.Create(new Imaging.Image(encodedImage.Resource.Width, encodedImage.Resource.Height, Imaging.PixelFormat.BGR_24bpp));
                }

                var decoder = new ImageFromStreamDecoder();
                decoder.DecodeFromStream(encodedImage.Resource.ToStream(), this.psiImage.Resource);
            }

            this.UpdateBitmap();
        }

        /// <summary>
        /// Crop image to specified dimensions and return newly cropped image. Does not alter current image.
        /// </summary>
        /// <param name="left">Left border of crop.</param>
        /// <param name="top">Top border of crop.</param>
        /// <param name="width">Width of crop.</param>
        /// <param name="height">Height of crop.</param>
        /// <returns>The newly cropped image.</returns>
        public DisplayImage Crop(int left, int top, int width, int height)
        {
            Shared<Image> croppedCopy = this.psiImage.SharedPool.GetOrCreate();
            this.psiImage.Resource.Crop(croppedCopy.Resource, left, top, width, height);
            var displayImage = new DisplayImage();
            displayImage.UpdateImage(croppedCopy);
            return displayImage;
        }

        private void UpdateBitmap()
        {
            if ((Application.Current != null) && (this.psiImage != null) && (this.psiImage.Resource != null))
            {
                Application.Current.Dispatcher.BeginInvoke(
                    (Action)(() =>
                    {
                        lock (this.imageLock)
                        {
                            if (this.Image == null
                                    || this.Image.PixelWidth != this.psiImage.Resource.Width
                                    || this.Image.PixelHeight != this.psiImage.Resource.Height
                                    || this.Image.BackBufferStride != this.psiImage.Resource.Stride)
                            {
                                System.Windows.Media.PixelFormat pixelFormat;
                                switch (this.psiImage.Resource.PixelFormat)
                                {
                                    case Imaging.PixelFormat.Gray_8bpp:
                                        pixelFormat = PixelFormats.Gray8;
                                        break;

                                    case Imaging.PixelFormat.Gray_16bpp:
                                        pixelFormat = PixelFormats.Gray16;
                                        break;

                                    case Imaging.PixelFormat.BGR_24bpp:
                                        pixelFormat = PixelFormats.Bgr24;
                                        break;

                                    case Imaging.PixelFormat.BGRX_32bpp:
                                        pixelFormat = PixelFormats.Bgr32;
                                        break;

                                    case Imaging.PixelFormat.BGRA_32bpp:
                                        pixelFormat = PixelFormats.Bgra32;
                                        break;

                                    case Imaging.PixelFormat.RGBA_64bpp:
                                        pixelFormat = PixelFormats.Rgba64;
                                        break;

                                    default:
                                        this.Image = null;
                                        return;
                                }

                                this.Image = new WriteableBitmap(this.psiImage.Resource.Width, this.psiImage.Resource.Height, 300, 300, pixelFormat, null);
                            }

                            this.Image.WritePixels(new Int32Rect(0, 0, this.psiImage.Resource.Width, this.psiImage.Resource.Height), this.psiImage.Resource.ImageData, this.psiImage.Resource.Stride * this.psiImage.Resource.Height, this.psiImage.Resource.Stride);
                            this.renderedFrames.Increment();
                        }
                    }),
                    System.Windows.Threading.DispatcherPriority.Render);
            }
        }
    }
}
