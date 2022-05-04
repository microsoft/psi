// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace HoloLensCaptureServer
{
    using System;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using ImageControl = System.Windows.Controls.Image;
    using MediaBrushes = System.Windows.Media.Brushes;

    /// <summary>
    /// Displays image stream in a window.
    /// </summary>
    public class ImageViewer : IConsumer<Shared<Image>>, IDisposable
    {
        // singleton app thread and app instance shared across ImageViewers
        private static readonly EventWaitHandle AppStarted = new (false, EventResetMode.ManualReset);
        private static Thread appThread;
        private static Application app = null;

        private readonly Pipeline pipeline;
        private readonly string name;

        // per-ImageViewer Window and ImageControl instances
        private Window win;
        private ImageControl imageControl;
        private WriteableBitmap bitmap;
        private IntPtr bitmapPtr = IntPtr.Zero;

        /// <summary>
        /// Initializes static members of the <see cref="ImageViewer"/> class.
        /// </summary>
        static ImageViewer()
        {
            // start app singleton
            appThread = new Thread(StartApp);
            appThread.SetApartmentState(ApartmentState.STA);
            appThread.Start();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageViewer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for the component.</param>
        /// <param name="showWindow">An optional flag indicating whether to show the window initially upon pipeline run.</param>
        public ImageViewer(Pipeline pipeline, string name = nameof(ImageViewer), bool showWindow = true)
        {
            this.pipeline = pipeline;
            this.name = name;
            this.In = pipeline.CreateReceiver<Shared<Image>>(this, this.ReceiveImage, nameof(this.In));
            this.ShowHideWindow = pipeline.CreateReceiver<bool>(this, this.ReceiveShowHideWindow, nameof(this.ShowHideWindow));

            this.InitWindow();
            if (showWindow)
            {
                pipeline.PipelineRun += (_, _) => this.ShowWindow();
            }
        }

        /// <inheritdoc />
        public Receiver<Shared<Image>> In { get; }

        /// <summary>
        /// Gets a receiver of flags indicating whether to show or hide the window.
        /// </summary>
        public Receiver<bool> ShowHideWindow { get; }

        /// <summary>
        /// Show image window.
        /// </summary>
        public void ShowWindow()
        {
            AppStarted.WaitOne();
            app?.Dispatcher.Invoke(this.win.Show);
        }

        /// <summary>
        /// Hide image window.
        /// </summary>
        public void HideWindow()
        {
            AppStarted.WaitOne();
            app?.Dispatcher.Invoke(this.win.Hide);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.imageControl = null;
            this.bitmap = null;
            this.bitmapPtr = IntPtr.Zero;
            app?.Dispatcher.Invoke(() =>
            {
                this.win?.Close();
                this.win = null;
            });
        }

        /// <inheritdoc />
        public override string ToString() => this.name;

        private static void StartApp()
        {
            app = new Application();
            app.Startup += (_, _) => AppStarted.Set();
            app.Exit += (_, _) =>
            {
                app = null;
                appThread = null;
            };
            app.Run();
        }

        private void ReceiveImage(Shared<Image> sharedImage, Envelope envelope)
        {
            // create a new bitmap if necessary
            if (this.bitmap == null)
            {
                // WriteableBitmap must be created on the UI thread
                app?.Dispatcher.Invoke(() =>
                {
                    this.bitmap = new WriteableBitmap(
                        sharedImage.Resource.Width,
                        sharedImage.Resource.Height,
                        300,
                        300,
                        sharedImage.Resource.PixelFormat.ToWindowsMediaPixelFormat(),
                        null);

                    this.imageControl.Source = this.bitmap;
                    this.bitmapPtr = this.bitmap.BackBuffer;
                });
            }

            // update the display bitmap's back buffer
            if (this.bitmapPtr != IntPtr.Zero)
            {
                sharedImage.Resource.CopyTo(this.bitmapPtr, sharedImage.Resource.Width, sharedImage.Resource.Height, sharedImage.Resource.Stride, sharedImage.Resource.PixelFormat);
            }

            app?.Dispatcher.Invoke(() =>
            {
                // invalidate the entire area of the bitmap to cause the display image to be redrawn
                this.bitmap.Lock();
                this.bitmap.AddDirtyRect(new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight));
                this.bitmap.Unlock();
            });
        }

        private void ReceiveShowHideWindow(bool showWindow)
        {
            if (showWindow)
            {
                this.ShowWindow();
            }
            else
            {
                this.HideWindow();
            }
        }

        private void InitWindow()
        {
            AppStarted.WaitOne();
            app?.Dispatcher.Invoke(() =>
            {
                this.imageControl = new ImageControl();
                this.win = new Window()
                {
                    Title = this.name,
                    Content = this.imageControl,
                    Background = MediaBrushes.DarkGray,
                    Width = 896,
                    Height = 532,
                };
                this.win.Closing += (_, e) =>
                {
                    this.win.Hide(); // hide instead of closing
                    e.Cancel = true;
                };
            });
        }
    }
}
