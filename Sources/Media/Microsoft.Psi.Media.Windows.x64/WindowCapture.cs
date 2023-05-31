// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CA1060 // Move pinvokes to native methods class
#pragma warning disable SA1305 // Field names should not use Hungarian notation

namespace Microsoft.Psi.Media
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
    using PsiImage = Microsoft.Psi.Imaging.Image;

    /// <summary>
    /// Component that streams video from Window handle (default=desktop window/primary screen).
    /// </summary>
    public class WindowCapture : Generator, IProducer<Shared<PsiImage>>
    {
        // see https://docs.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-bitblt
        private const int CaptureBlt = 0x40000000; // include layered windows
        private const int SourceCopy = 0x00CC0020; // copy source rectangle directly to the destination

        private readonly TimeSpan interval;
        private readonly IntPtr hWnd;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowCapture"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">Interval at which to render and emit frames of the window.</param>
        /// <param name="hWnd">Window handle to capture (default=desktop window/primary screen).</param>
        /// <param name="name">An optional name for the component.</param>
        public WindowCapture(Pipeline pipeline, TimeSpan interval, IntPtr hWnd, string name = nameof(WindowCapture))
            : base(pipeline, true, name)
        {
            this.interval = interval;
            this.hWnd = hWnd == IntPtr.Zero ? GetDesktopWindow() : hWnd;
            this.Out = pipeline.CreateEmitter<Shared<PsiImage>>(this, nameof(this.Out));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowCapture"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">Interval at which to render and emit frames of the window.</param>
        /// <param name="name">An optional name for the component.</param>
        public WindowCapture(Pipeline pipeline, TimeSpan interval, string name = nameof(WindowCapture))
            : this(pipeline, interval, IntPtr.Zero, name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowCapture"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">Configuration values for the window capture component.</param>
        /// <param name="name">An optional name for the component.</param>
        public WindowCapture(Pipeline pipeline, WindowCaptureConfiguration configuration, string name = nameof(WindowCapture))
            : this(pipeline, configuration.Interval, configuration.WindowHandle, name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowCapture"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for the component.</param>
        public WindowCapture(Pipeline pipeline, string name = nameof(WindowCapture))
            : this(pipeline, WindowCaptureConfiguration.Default, name)
        {
        }

        /// <summary>
        /// Gets the emitter that generates images from window.
        /// </summary>
        public Emitter<Shared<PsiImage>> Out { get; private set; }

        /// <inheritdoc />
        protected override DateTime GenerateNext(DateTime currentTime)
        {
            GetWindowRect(this.hWnd, out RECT rect);
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            var win = GetWindowDC(this.hWnd);
            var dest = CreateCompatibleDC(win);
            var hBmp = CreateCompatibleBitmap(win, width, height);
            var sel = SelectObject(dest, hBmp);
            BitBlt(dest, 0, 0, width, height, win, 0, 0, SourceCopy | CaptureBlt);
            var bitmap = Bitmap.FromHbitmap(hBmp);

            using (var sharedImage = ImagePool.GetOrCreate(width, height, PixelFormat.BGRA_32bpp))
            {
                var resource = sharedImage.Resource;
                resource.CopyFrom(bitmap);
                this.Out.Post(sharedImage, currentTime);
            }

            bitmap.Dispose();
            SelectObject(dest, sel);
            DeleteObject(hBmp);
            DeleteDC(dest);
            ReleaseDC(this.hWnd, win);

            return currentTime + this.interval;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr ptr);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, uint rop);

        [DllImport("gdi32.dll")]
        private static extern IntPtr DeleteObject(IntPtr hDc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr DeleteDC(IntPtr hDc);

        [DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
             public int Left;
             public int Top;
             public int Right;
             public int Bottom;
        }
    }
}
