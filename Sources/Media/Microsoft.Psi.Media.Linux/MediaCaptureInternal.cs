// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// Internal media capture class.
    /// </summary>
    /// <remarks>
    /// Interfaces with Linux video driver, but outside of \psi.
    /// </remarks>
    internal class MediaCaptureInternal : IDisposable
    {
        private const uint NumberOfDriverBuffers = 3;

        private readonly string device;
        private BufferInfo[] buffers = new BufferInfo[NumberOfDriverBuffers];
        private FileStream file = null;
        private int handle = 0;
        private LinuxVideoInterop.Capability capabilities;
        private Thread background;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCaptureInternal"/> class.
        /// </summary>
        /// <param name="device">Device name (e.g. "/dev/video0").</param>
        public MediaCaptureInternal(string device)
        {
            this.device = device;
        }

        /// <summary>
        /// Image frame received event.
        /// </summary>
        public event EventHandler<ImageFrame> OnFrame;

        /// <summary>
        /// Gets device name (e.g. "/dev/video0").
        /// </summary>
        public string Device => this.device;

        /// <summary>
        /// Gets bus name.
        /// </summary>
        public string Bus => this.capabilities.Bus;

        /// <summary>
        /// Gets video card.
        /// </summary>
        public string Card => this.capabilities.Card;

        /// <summary>
        /// Gets driver description.
        /// </summary>
        public string Driver => this.capabilities.Driver;

        /// <summary>
        /// Open device.
        /// </summary>
        /// <remarks>
        /// This also queries the device capabilites and ensures that it supports video capture and streaming.
        /// </remarks>
        public void Open()
        {
            this.file = File.Open(this.device, FileMode.Open, FileAccess.ReadWrite);
            this.handle = this.file.SafeFileHandle.DangerousGetHandle().ToInt32();

            this.capabilities = LinuxVideoInterop.QueryCapabilities(this.handle);
            if ((this.capabilities.Caps & LinuxVideoInterop.CapsFlags.VideoCapture) != LinuxVideoInterop.CapsFlags.VideoCapture)
            {
                throw new IOException("Device doesn't support video capture");
            }

            if ((this.capabilities.Caps & LinuxVideoInterop.CapsFlags.Streaming) != LinuxVideoInterop.CapsFlags.Streaming)
            {
                throw new IOException("Device doesn't support streaming");
            }
        }

        /// <summary>
        /// Enumerate supported pixel formats.
        /// </summary>
        /// <returns>Supported formats.</returns>
        public IEnumerable<PixelFormat> SupportedPixelFormats()
        {
            foreach (var format in LinuxVideoInterop.EnumerateFormats(this.handle))
            {
                if (format.Type == LinuxVideoInterop.BufferType.VideoCapture)
                {
                    yield return new PixelFormat(format);
                }
            }
        }

        /// <summary>
        /// Get current video format.
        /// </summary>
        /// <returns>Video format.</returns>
        public VideoFormat GetVideoFormat()
        {
            var format = new LinuxVideoInterop.VideoFormat() { Type = LinuxVideoInterop.BufferType.VideoCapture }; // note: only video capture supported
            LinuxVideoInterop.GetFormat(this.handle, ref format);
            return new VideoFormat(format);
        }

        /// <summary>
        /// Set video format.
        /// </summary>
        /// <param name="width">Format width in pixels.</param>
        /// <param name="height">Format height in pixels.</param>
        /// <param name="format">Pixel format.</param>
        public void SetVideoFormat(int width, int height, PixelFormat format)
        {
#pragma warning disable SA1118 // Parameter must not span multiple lines
            LinuxVideoInterop.SetFormat(
                this.handle,
                new LinuxVideoInterop.VideoFormat()
                {
                    Type = LinuxVideoInterop.BufferType.VideoCapture,
                    Pixel = new LinuxVideoInterop.PixFormat()
                    {
                        Width = (uint)width,
                        Height = (uint)height,
                        PixelFormatId = format.InternalFormat.PixelFormatId,
                        Field = LinuxVideoInterop.PixelField.None, // note: only supporting none
                    },
                });
#pragma warning restore SA1118 // Parameter must not span multiple lines
        }

        /// <summary>
        /// Begin streaming buffers.
        /// </summary>
        public unsafe void StreamBuffers()
        {
            if (LinuxVideoInterop.ReqBufs(this.handle, NumberOfDriverBuffers, LinuxVideoInterop.Memory.MemoryMapping) != NumberOfDriverBuffers)
            {
                throw new IOException("Could not allocate buffers.");
            }

            try
            {
                // enque and memory map all the buffers
                for (var i = 0u; i < NumberOfDriverBuffers; i++)
                {
                    var buf = LinuxVideoInterop.QueryBuf(this.handle, i);
                    LinuxVideoInterop.EnqueBuffer(this.handle, buf);
                    this.buffers[i] = new BufferInfo()
                    {
                        Initialized = true,
                        Start = LinuxVideoInterop.MemoryMap(this.handle, buf),
                        Buffer = buf,
                    };
                }

                LinuxVideoInterop.StreamOn(this.handle);
                this.background = new Thread(new ThreadStart(this.ProcessFrames)) { IsBackground = true };
                this.background.Start();
            }
            catch (Exception ex)
            {
                for (var i = 0u; i < NumberOfDriverBuffers; i++)
                {
                    var buf = this.buffers[i];
                    if (buf.Initialized)
                    {
                        LinuxVideoInterop.MemoryUnmap(buf.Buffer);
                    }
                }

                throw ex;
            }
        }

        /// <summary>
        /// Stop streaming buffers and close device.
        /// </summary>
        public void Close()
        {
            LinuxVideoInterop.StreamOff(this.handle);
            if (this.file != null)
            {
                this.file.Close();
                this.file.Dispose();
            }
        }

        /// <summary>
        /// Dispose of device.
        /// </summary>
        public void Dispose()
        {
            this.Close();
        }

        private unsafe void ProcessFrames()
        {
            while (true)
            {
                if (this.OnFrame != null)
                {
                    var buffer = LinuxVideoInterop.DequeBuffer(this.handle);
                    var data = this.buffers[buffer.Index];
                    var time = buffer.TimeStamp;
                    var frame = new ImageFrame(new IntPtr(data.Start), (int)buffer.BytesUsed, time.Seconds, time.MicroSeconds, this.handle, buffer); // re-EnqueBuffer upon dispose
                    this.OnFrame.Invoke(this, frame);
                }
                else
                {
                    Thread.Sleep(10); // wait for event subscriber
                }
            }
        }

        /// <summary>
        /// Driver buffer info.
        /// </summary>
        private unsafe struct BufferInfo
        {
            public bool Initialized;
            public void* Start;
            public LinuxVideoInterop.Buffer Buffer;
        }

        /// <summary>
        /// Pixel format.
        /// </summary>
        public class PixelFormat
        {
            private readonly LinuxVideoInterop.FormatDescription internalFormat;

            /// <summary>
            /// Initializes a new instance of the <see cref="PixelFormat"/> class.
            /// </summary>
            /// <param name="internalFormat">Internal format provided by the driver.</param>
            internal PixelFormat(LinuxVideoInterop.FormatDescription internalFormat)
            {
                this.internalFormat = internalFormat;
            }

            /// <summary>
            /// Gets pixel format ID.
            /// </summary>
            public PixelFormatId Pixels => this.InternalFormat.PixelFormatId;

            /// <summary>
            /// Gets human-readable pixel format description.
            /// </summary>
            public string Description => this.InternalFormat.Description;

            /// <summary>
            /// Gets a value indicating whether whether pixel format is compressed.
            /// </summary>
            public bool IsCompressed => (this.InternalFormat.Flags & LinuxVideoInterop.FormatFlags.Compressed) == LinuxVideoInterop.FormatFlags.Compressed;

            /// <summary>
            /// Gets a value indicating whether whether pixel format is emulated (non-native).
            /// </summary>
            public bool IsEmulated => (this.InternalFormat.Flags & LinuxVideoInterop.FormatFlags.Emulated) == LinuxVideoInterop.FormatFlags.Emulated;

            /// <summary>
            /// Gets internal format description (used to pass back to driver).
            /// </summary>
            internal LinuxVideoInterop.FormatDescription InternalFormat => this.internalFormat;
        }

        /// <summary>
        /// Video format.
        /// </summary>
        public class VideoFormat
        {
            private readonly LinuxVideoInterop.VideoFormat internalFormat;

            /// <summary>
            /// Initializes a new instance of the <see cref="VideoFormat"/> class.
            /// </summary>
            /// <param name="internalFormat">Internal format provided by the driver.</param>
            internal VideoFormat(LinuxVideoInterop.VideoFormat internalFormat)
            {
                this.internalFormat = internalFormat;
            }

            /// <summary>
            /// Gets format width in pixels.
            /// </summary>
            public uint Width => this.InternalFormat.Pixel.Width;

            /// <summary>
            /// Gets format height in pixels.
            /// </summary>
            public uint Height => this.InternalFormat.Pixel.Height;

            /// <summary>
            /// Gets format pixel format ID.
            /// </summary>
            public PixelFormatId Pixels => this.InternalFormat.Pixel.PixelFormatId;

            /// <summary>
            /// Gets format color space.
            /// </summary>
            public ColorSpace ColorSpace => this.InternalFormat.Pixel.ColorSpace;

            /// <summary>
            /// Gets format image size.
            /// </summary>
            public uint Size => this.InternalFormat.Pixel.SizeImage;

            /// <summary>
            /// Gets internal vidio format (used to pass back to driver).
            /// </summary>
            internal LinuxVideoInterop.VideoFormat InternalFormat => this.internalFormat;
        }

        /// <summary>
        /// Image frame containing buffer and time info.
        /// </summary>
        public unsafe class ImageFrame : IDisposable
        {
            private readonly LinuxVideoInterop.Buffer buffer;
            private readonly long seconds;
            private readonly long microSeconds;
            private readonly IntPtr start;
            private readonly int length;
            private int handle;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImageFrame"/> class.
            /// </summary>
            /// <param name="start">Start of buffer shared with driver.</param>
            /// <param name="length">Length of buffer shared with driver.</param>
            /// <param name="seconds">Time stamp seconds of image frame.</param>
            /// <param name="useconds">Time stamp microseconds of image frame.</param>
            /// <param name="handle">Device file handle.</param>
            /// <param name="buffer">Internal driver buffer struct.</param>
            internal ImageFrame(IntPtr start, int length, long seconds, long useconds, int handle, LinuxVideoInterop.Buffer buffer)
            {
                this.start = start;
                this.length = length;
                this.seconds = seconds;
                this.microSeconds = useconds;
                this.handle = handle;
                this.buffer = buffer;
            }

            /// <summary>
            /// Gets start of buffer shared with driver.
            /// </summary>
            public unsafe IntPtr Start => this.start;

            /// <summary>
            /// Gets length of buffer shared with driver.
            /// </summary>
            public int Length => this.length;

            /// <summary>
            /// Gets time stamp seconds of image frame.
            /// </summary>
            public long Seconds => this.seconds;

            /// <summary>
            /// Gets time stamp microseconds of image frame.
            /// </summary>
            public long MicroSeconds => this.microSeconds;

            /// <summary>
            /// Dispose image frame (re-enqueuing underlying driver buffer).
            /// </summary>
            public void Dispose()
            {
                LinuxVideoInterop.EnqueBuffer(this.handle, this.buffer);
            }
        }
    }
}
