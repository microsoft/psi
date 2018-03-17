// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Samples.OpenCV
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi;

    /// <summary>
    /// DisplayImage is a helper class that is used to bind a WPF <image/> to a Psi image.
    /// </summary>
    public class DisplayImage : INotifyPropertyChanged
    {
        private Shared<Imaging.Image> psiImage;
        private FrameCounter renderedFrames = new FrameCounter();
        private FrameCounter receivedFrames = new FrameCounter();
        private WriteableBitmap image;

        public DisplayImage()
            : base()
        {
            System.Windows.Threading.DispatcherTimer dt = new System.Windows.Threading.DispatcherTimer();
            dt.Interval = new TimeSpan(0, 0, 0, 0, 16);
            dt.Tick += this.Dt_Tick;
            dt.Start();
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the frame rate of display.
        /// </summary>
        public FrameCounter RenderedFrames
        {
            get => this.renderedFrames;

            set
            {
                this.renderedFrames = value;
                this.OnPropertyChanged(nameof(this.ReceivedFrames));
            }
        }

        /// <summary>
        /// Gets or sets the framerate of images coming from the pipeline.
        /// </summary>
        public FrameCounter ReceivedFrames
        {
            get => this.receivedFrames;

            set
            {
                this.receivedFrames = value;
                this.OnPropertyChanged(nameof(this.ReceivedFrames));
            }
        }

        /// <summary>
        /// Gets or sets the WriteableBitmap that we will display in a WPF control
        /// </summary>
        public WriteableBitmap Image
        {
            get => this.image;

            set
            {
                this.image = value;
                this.OnPropertyChanged(nameof(this.Image));
            }
        }

        /// <summary>
        /// UpdateImage is called each time a new image is received from the Psi pipeline
        /// This method will convert the image into a WriteableBitmap and inform WPF to
        /// update its display.
        /// </summary>
        /// <param name="dispImage">The image to display.</param>
        public void UpdateImage(Shared<Imaging.Image> dispImage)
        {
            lock (this)
            {
                this.receivedFrames.Increment();
                this.psiImage?.Dispose();
                this.psiImage = dispImage.AddRef();
            }
        }

        /// <summary>
        /// Callback for handling of dispatch timer that will drive our UI update
        /// </summary>
        /// <param name="sender">Timer that triggered this callback</param>
        /// <param name="e">Event args for the callback</param>
        private void Dt_Tick(object sender, EventArgs e)
        {
            if (this.psiImage != null && this.psiImage.Resource != null)
            {
                lock (this)
                {
                    if (this.Image == null
                            || this.Image.PixelWidth != this.psiImage.Resource.Width
                            || this.Image.PixelHeight != this.psiImage.Resource.Height
                            || this.Image.BackBufferStride != this.psiImage.Resource.Stride)
                    {
                        PixelFormat pixelFormat;
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
                            default:
                                throw new Exception("Unexpected PixelFormat in DisplayImage");
                        }

                        this.Image = new WriteableBitmap(this.psiImage.Resource.Width, this.psiImage.Resource.Height, 300, 300, pixelFormat, null);
                    }

                    this.Image.WritePixels(new Int32Rect(0, 0, this.psiImage.Resource.Width, this.psiImage.Resource.Height), this.psiImage.Resource.ImageData, this.psiImage.Resource.Stride * this.psiImage.Resource.Height, this.psiImage.Resource.Stride);
                    this.renderedFrames.Increment();
                }
            }
        }

        /// <summary>
        /// Helper function for firing an event when the image property changes
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
