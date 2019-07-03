// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Structs, enums and static methods for interacting with Video4Linux drivers (V4L2).
    /// </summary>
    /// <remarks>
    /// This implimentation is based on this spec: https://www.linuxtv.org/downloads/legacy/video4linux/API/V4L2_API/spec-single/v4l2.html
    /// The only dependencies are POSIX ioctl, mmap and munmap.
    /// </remarks>
    internal static class LinuxVideoInterop
    {
        private const uint V = (uint)'V' << 8; // video ioctl type

        private const uint IOCREAD = 2U << 30; // userland reading, kernel writing
        private const uint IOCWRITE = 1U << 30; // userland writing, kernel reading

        /// <summary>
        /// Capability flags.
        /// </summary>
        [Flags]
        internal enum CapsFlags : uint
        {
            /// <summary>
            /// V4L2_CAP_VIDEO_CAPTURE
            /// </summary>
            VideoCapture = 0x00000001,

            /// <summary>
            /// V4L2_CAP_VIDEO_OUTPUT
            /// </summary>
            VideoOutput = 0x00000002,

            /// <summary>
            /// V4L2_CAP_VIDEO_OVERLAY
            /// </summary>
            VideoOverlay = 0x00000004,

            /// <summary>
            /// V4L2_CAP_VBI_CAPTURE
            /// </summary>
            VBICapture = 0x00000010,

            /// <summary>
            /// V4L2_CAP_VBI_OUTPUT
            /// </summary>
            VBIOutput = 0x00000020,

            /// <summary>
            /// V4L2_CAP_SLICED_VBI_CAPTURE
            /// </summary>
            SlicedVBICapture = 0x00000040,

            /// <summary>
            /// V4L2_CAP_SLICED_VBI_OUTPUT
            /// </summary>
            SlicedVBIOutput = 0x00000080,

            /// <summary>
            /// V4L2_CAP_RDS_CAPTURE
            /// </summary>
            RDSCapture = 0x00000100,

            /// <summary>
            /// V4L2_CAP_VIDEO_OUTPUT_OVERLAY
            /// </summary>
            VideoOutputOverlay = 0x00000200,

            /// <summary>
            /// V4L2_CAP_HW_FREQ_SEEK
            /// </summary>
            HWFrequenceySeek = 0x00000400,

            /// <summary>
            /// V4L2_CAP_RDS_OUTPUT
            /// </summary>
            RDSOutput = 0x00000800,

            /// <summary>
            /// V4L2_CAP_VIDEO_CAPTURE_MPLANE
            /// </summary>
            VideoCaptureMPlane = 0x00001000,

            /// <summary>
            /// V4L2_CAP_VIDEO_OUTPUT_MPLANE
            /// </summary>
            VideoOutputMPlane = 0x00002000,

            /// <summary>
            /// V4L2_CAP_VIDEO_M2M_MPLANE
            /// </summary>
            VideoMem2MemMPlane = 0x00004000,

            /// <summary>
            /// V4L2_CAP_VIDEO_M2M
            /// </summary>
            VideoMem2Mem = 0x00008000,

            /// <summary>
            /// V4L2_CAP_TUNER
            /// </summary>
            Tuner = 0x00010000,

            /// <summary>
            /// V4L2_CAP_AUDIO
            /// </summary>
            Audio = 0x00020000,

            /// <summary>
            /// V4L2_CAP_RADIO
            /// </summary>
            Radio = 0x00040000,

            /// <summary>
            /// V4L2_CAP_MODULATOR
            /// </summary>
            Modulator = 0x00080000,

            /// <summary>
            /// V4L2_CAP_SDR_CAPTURE
            /// </summary>
            SDRCapture = 0x00100000,

            /// <summary>
            /// V4L2_CAP_EXT_PIX_FORMAT
            /// </summary>
            ExtendedPixelFormat = 0x00200000,

            /// <summary>
            /// V4L2_CAP_SDR_OUTPUT
            /// </summary>
            SDROutput = 0x00400000,

            /// <summary>
            /// V4L2_CAP_META_CAPTURE
            /// </summary>
            MetadataCapture = 0x00800000,

            /// <summary>
            /// V4L2_CAP_READWRITE
            /// </summary>
            ReadWrite = 0x01000000,

            /// <summary>
            /// V4L2_CAP_ASYNCIO
            /// </summary>
            AsyncIO = 0x02000000,

            /// <summary>
            /// V4L2_CAP_STREAMING
            /// </summary>
            Streaming = 0x04000000,

            /// <summary>
            /// V4L2_CAP_TOUCH
            /// </summary>
            Touch = 0x10000000,

            /// <summary>
            /// V4L2_CAP_DEVICE_CAPS
            /// </summary>
            DeviceCaps = 0x80000000,
        }

        /// <summary>
        /// Buffer type (v4l2_buf_type).
        /// </summary>
        internal enum BufferType : uint
        {
            /// <summary>
            /// V4L2_BUF_TYPE_VIDEO_CAPTURE
            /// </summary>
            VideoCapture = 1,

            /// <summary>
            /// V4L2_BUF_TYPE_VIDEO_OUTPUT
            /// </summary>
            VideoOutput = 2,

            /// <summary>
            /// V4L2_BUF_TYPE_VIDEO_OVERLAY
            /// </summary>
            VideoOverlay = 3,

            /// <summary>
            /// V4L2_BUF_TYPE_VBI_CAPTURE
            /// </summary>
            VBICapture = 4,

            /// <summary>
            /// V4L2_BUF_TYPE_VBI_OUTPUT
            /// </summary>
            VBIOutput = 5,

            /// <summary>
            /// V4L2_BUF_TYPE_SLICED_VBI_CAPTURE
            /// </summary>
            SlicedVBICapture = 6,

            /// <summary>
            /// V4L2_BUF_TYPE_SLICED_VBI_OUTPUT
            /// </summary>
            SlicedVBIOutput = 7,

            /// <summary>
            /// V4L2_BUF_TYPE_VIDEO_OUTPUT_OVERLAY
            /// </summary>
            VideoOutputOverlay = 8,

            /// <summary>
            /// V4L2_BUF_TYPE_VIDEO_CAPTURE_MPLANE
            /// </summary>
            VideoCaptureMultiPlane = 9,

            /// <summary>
            /// V4L2_BUF_TYPE_VIDEO_OUTPUT_MPLANE
            /// </summary>
            VideoOutputMultiPlane = 10,

            /// <summary>
            /// V4L2_BUF_TYPE_SDR_CAPTURE
            /// </summary>
            SDRCapture = 11,

            /// <summary>
            /// V4L2_BUF_TYPE_SDR_OUTPUT
            /// </summary>
            SDROutput = 12,

            /// <summary>
            /// V4L2_BUF_TYPE_META_CAPTURE
            /// </summary>
            MetaCapture = 13,

            /// <summary>
            /// V4L2_BUF_TYPE_PRIVATE
            /// </summary>
            Private = 80,
        }

        /// <summary>
        /// Format flags.
        /// </summary>
        internal enum FormatFlags : uint
        {
            /// <summary>
            /// V4L2_FMT_FLAG_COMPRESSED - native compressed format (high performance)
            /// </summary>
            Compressed = 1,

            /// <summary>
            /// V4L2_FMT_FLAD_EMULATED - not native to device, emulated in software (poor performance)
            /// </summary>
            Emulated = 2,
        }

        /// <summary>
        /// Pixel field (v4l2_field).
        /// </summary>
        internal enum PixelField : uint
        {
            /// <summary>
            /// V2L2_FIELD_ANY
            /// </summary>
            Any = 0,

            /// <summary>
            /// V2L2_FIELD_NONE
            /// </summary>
            None = 1,

            /// <summary>
            /// V2L2_FIELD_TOP
            /// </summary>
            Top = 2,

            /// <summary>
            /// V2L2_FIELD_BOTTOM
            /// </summary>
            Bottom = 3,

            /// <summary>
            /// V2L2_FIELD_INTERLACED
            /// </summary>
            Interlaced = 4,

            /// <summary>
            /// V4L2_FIELD_SEQ_TB
            /// </summary>
            SequentialTopBottom = 5,

            /// <summary>
            /// V4L2_FIELD_SEQ_BT
            /// </summary>
            SequentialBottomTop = 6,

            /// <summary>
            /// V4L2_FIELD_ALTERNATE
            /// </summary>
            Alternate = 7,

            /// <summary>
            /// V4L2_FIELD_INTERLACED_TB
            /// </summary>
            InterlacedTopBottom = 8,

            /// <summary>
            /// V4L2_FIELD_INTERLACED_BT
            /// </summary>
            InterlacedBottomTop = 9,
        }

        /// <summary>
        /// Driver memory sharing model (v4l2_memory).
        /// </summary>
        internal enum Memory
        {
            /// <summary>
            /// Memory mapping from driver-allocated buffers.
            /// </summary>
            MemoryMapping = 1,

            /// <summary>
            /// Pointers to user-allocated buffers.
            /// </summary>
            UserPointer = 2,

            /// <summary>
            /// Memory overlay.
            /// </summary>
            MemoryOverlay = 3,
        }

        /// <summary>
        /// Query device capabilities (POSIX ioctl VIDIOC_QUERYCAP).
        /// </summary>
        /// <param name="fd">Device name (e.g. "/dev/video0").</param>
        /// <returns>Device capabilites.</returns>
        public static Capability QueryCapabilities(int fd)
        {
            var caps = default(Capability);
            if (QueryCapabilities(fd, VIDIOC(IOCREAD, Marshal.SizeOf(caps), 0) /* VIDIOC_QUERYCAP */, ref caps) != 0)
            {
                throw new IOException("QueryCapabilities failed.");
            }

            return caps;
        }

        /// <summary>
        /// Enumerate formats by repeatedly calling driver with increasing indexes.
        /// </summary>
        /// <remarks>
        /// Only video capture (BufferType.VideoCapture) supported.
        /// </remarks>
        /// <param name="fd">Device file descriptor.</param>
        /// <returns>Format descriptions.</returns>
        public static IEnumerable<FormatDescription> EnumerateFormats(int fd)
        {
            var format = default(FormatDescription);
            format.Type = BufferType.VideoCapture; // note: only supporting video capture
            for (var i = 0u; true; i++)
            {
                format.Index = i;
                if (!EnumFormats(fd, ref format))
                {
                    break;
                }

                yield return format;
            }
        }

        /// <summary>
        /// Enumerate single (indexed) format (POSIX ioctl VIDIOC_ENUM_FMT).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="format">Format description struct to be populated.</param>
        /// <returns>Success flag.</returns>
        public static bool EnumFormats(int fd, ref FormatDescription format)
        {
            return EnumFormats(fd, VIDIOC(IOCREAD | IOCWRITE, Marshal.SizeOf(format), 2) /* VIDIOC_ENUM_FMT */, ref format) >= 0;
        }

        /// <summary>
        /// Get format (POSIX ioctl VIDIOC_G_FMT).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="format">Video format struct to be populated.</param>
        internal static void GetFormat(int fd, ref VideoFormat format)
        {
            if (GetFormat(fd, VIDIOC(IOCREAD | IOCWRITE, Marshal.SizeOf(format), 4) /* VIDIOC_G_FMT */, ref format) != 0)
            {
                throw new IOException("GetFormat failed.");
            }

            if (format.Type != BufferType.VideoCapture)
            {
                throw new ArgumentException("Formats other than video capture are not supported.");
            }
        }

        /// <summary>
        /// Set format (POSIX ioctl VIDIOC_S_FMT).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="format">Video format.</param>
        internal static void SetFormat(int fd, VideoFormat format)
        {
            if (format.Type != BufferType.VideoCapture)
            {
                throw new ArgumentException("Formats other than video capture are not supported.");
            }

            if (SetFormat(fd, VIDIOC(IOCREAD | IOCWRITE, Marshal.SizeOf(format), 5) /* VIDIOC_S_FMT */, ref format) != 0)
            {
                throw new IOException("SetFormat failed.");
            }
        }

        /// <summary>
        /// Request buffers (POSIX ioctl VIDIOC_REQBUFS).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="count">Number of buffers requested.</param>
        /// <param name="memory">Memory sharing model.</param>
        /// <returns>Number of buffers returned by driver.</returns>
        internal static uint ReqBufs(int fd, uint count, Memory memory)
        {
            var req = default(RequestBuffers);
            req.Count = count;
            req.Memory = memory;
            req.Type = BufferType.VideoCapture; // note: only video capture supported
            if (ReqBufs(fd, VIDIOC(IOCREAD | IOCWRITE, Marshal.SizeOf(req), 8) /* VIDIOC_REQBUFS */, ref req) != 0)
            {
                throw new ArgumentException("ReqBufs failed.");
            }

            return req.Count;
        }

        /// <summary>
        /// Query buffer (POSIX ioctl VIDIOC_QUERYBUF).
        /// </summary>
        /// <remarks>
        /// Note: Memory mapped from driver space assumed and only video capture (BufferType.VideoCapture) supported.
        /// </remarks>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="index">Buffer index.</param>
        /// <returns>Buffer returned by driver.</returns>
        internal static Buffer QueryBuf(int fd, uint index)
        {
            var buffer = new Buffer()
            {
                Index = index,
                Type = BufferType.VideoCapture, // note: only video capture supported
                Memory = Memory.MemoryMapping, // note: memory mapped assumed
            };

            if (QueryBuf(fd, VIDIOC(IOCREAD | IOCWRITE, Marshal.SizeOf(buffer), 9) /* VIDIOC_QUERYBUF */, ref buffer) != 0)
            {
                throw new ArgumentException($"QueryBuf failed (index={index}).");
            }

            return buffer;
        }

        /// <summary>
        /// Memory map (POSIX mmap).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="buffer">Buffer to be mapped.</param>
        /// <returns>Pointer to mapped memory.</returns>
        internal static unsafe void* MemoryMap(int fd, Buffer buffer)
        {
            var start = MemMap(IntPtr.Zero, buffer.Length, 0x1 /* PROT_READ */ | 0x2 /* PROT_WRITE */, 0x1 /* MAP_SHARED */, fd, buffer.Pointer);
            if (start == (void*)-1)
            {
                throw new ArgumentException("Memory map failed.");
            }

            return start;
        }

        /// <summary>
        /// Memory unmap (POSIX munmap).
        /// </summary>
        /// <param name="buffer">Buffer to unmap.</param>
        internal static unsafe void MemoryUnmap(Buffer buffer)
        {
            if (MemUnmap(buffer.Pointer, buffer.Length) != 0)
            {
                throw new ArgumentException("Memory unmap failed.");
            }
        }

        /// <summary>
        /// Enqueue buffer (POSIX ioctl VIDIOC_QBUF).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="buffer">Buffer struct to enqueue.</param>
        internal static void EnqueBuffer(int fd, Buffer buffer)
        {
            if (EnqueBuffer(fd, VIDIOC(IOCREAD | IOCWRITE, Marshal.SizeOf(buffer), 15) /* VIDIOC_QBUF */, ref buffer) != 0)
            {
                throw new ArgumentException("Enque buffer failed.");
            }
        }

        /// <summary>
        /// Dequeue buffer (POSIX ioctl VIDIOC_DQBUF).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <returns>Buffer dequeued.</returns>
        internal static Buffer DequeBuffer(int fd)
        {
            var buffer = default(Buffer);
            buffer.Type = LinuxVideoInterop.BufferType.VideoCapture;
            if (DequeBuffer(fd, VIDIOC(IOCREAD | IOCWRITE, Marshal.SizeOf(buffer), 17) /* VIDIOC_DQBUF */, ref buffer) != 0)
            {
                throw new ArgumentException("Deque buffer failed.");
            }

            return buffer;
        }

        /// <summary>
        /// Streaming on (POSIX ioctl VIDIOC_STREAMON).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        internal static void StreamOn(int fd)
        {
            var type = BufferType.VideoCapture; // note: only video capture supported
            if (StreamOn(fd, VIDIOC(IOCWRITE, sizeof(uint), 18) /* VIDIOC_STREAMON */, ref type) != 0)
            {
                throw new ArgumentException("StreamOn failed.");
            }
        }

        /// <summary>
        /// Streaming off (POSIX ioctl VIDIOC_STREAMOFF).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        internal static void StreamOff(int fd)
        {
            var type = BufferType.VideoCapture; // note: only video capture supported
            if (StreamOff(fd, VIDIOC(IOCWRITE, Marshal.SizeOf(type), 19) /* VIDIOC_STREAMOFF */, ref type) != 0)
            {
                throw new ArgumentException("StreamOn failed.");
            }
        }

        /// <summary>
        /// Compute VIDIOC_* ioctl number (see ioctl.h).
        /// </summary>
        /// <param name="readWrite">Read/write flag (IOC_READ, IOC_WRITE).</param>
        /// <param name="size">Size of struct parameter.</param>
        /// <param name="command">Command number.</param>
        /// <returns>IOC number.</returns>
        private static uint VIDIOC(uint readWrite, int size, int command)
        {
            return readWrite | ((uint)size << 16) | V | (uint)command;
        }

        /// <summary>
        /// Query capabilites (POSIX ioctl VIDIOC_QUERYCAP).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="request">Request type (VIDIOC_QUERYCAP).</param>
        /// <param name="caps">Capabilities struct to be populated.</param>
        /// <returns>Result flag.</returns>
        [DllImport("libc", EntryPoint="ioctl", SetLastError=true)]
        private static extern int QueryCapabilities(int fd, uint request, ref Capability caps);

        /// <summary>
        /// Enumerate formats (POSIX ioctl VIDIOC_ENUM_FMT).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="request">Request type (VIDIOC_ENUM_FMT).</param>
        /// <param name="format">Format descrition struct to be populated.</param>
        /// <returns>Result flag.</returns>
        [DllImport("libc", EntryPoint="ioctl", SetLastError=true)]
        private static extern int EnumFormats(int fd, uint request, ref FormatDescription format);

        /// <summary>
        /// Get format (POSIX ioctl VIDIOC_G_FMT).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="request">Request type (VIDIOC_G_FMT).</param>
        /// <param name="format">Video format struct to be populated.</param>
        /// <returns>Result flag.</returns>
        [DllImport("libc", EntryPoint="ioctl", SetLastError=true)]
        private static extern int GetFormat(int fd, uint request, ref VideoFormat format);

        /// <summary>
        /// Set format (POSIX ioctl VIDIOC_S_FMT).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="request">Request type (VIDIOC_S_FMT).</param>
        /// <param name="format">Video format.</param>
        /// <returns>Result flag.</returns>
        [DllImport("libc", EntryPoint="ioctl", SetLastError=true)]
        private static extern int SetFormat(int fd, uint request, ref VideoFormat format);

        /// <summary>
        /// Request buffers (POSIX ioctl VIDIOC_REQBUFS).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="request">Request type (VIDIOC_REQBUFS).</param>
        /// <param name="req">Request buffers struct to be populated.</param>
        /// <returns>Result flag.</returns>
        [DllImport("libc", EntryPoint="ioctl", SetLastError=true)]
        private static extern int ReqBufs(int fd, uint request, ref RequestBuffers req);

        /// <summary>
        /// Query buffer (POSIX ioctl VIDIOC_QUERYBUF).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="request">Request type (VIDIOC_QUERYBUF).</param>
        /// <param name="buf">Buffer struct to be populated.</param>
        /// <returns>Result flag.</returns>
        [DllImport("libc", EntryPoint="ioctl", SetLastError=true)]
        private static extern int QueryBuf(int fd, uint request, ref Buffer buf);

        /// <summary>
        /// Memory map (POSIX mmap).
        /// </summary>
        /// <param name="start">Buffer start.</param>
        /// <param name="length">Buffer length.</param>
        /// <param name="prot">Protection (PROT_EXEC/READ/WRITE/NONE).</param>
        /// <param name="flags">Flags (MAP_SHARED/PRIVATE).</param>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="offset">Offset within buffer.</param>
        /// <returns>Pointer to mapped memory.</returns>
        [DllImport("libc", EntryPoint="mmap", SetLastError=true)]
        private static unsafe extern void* MemMap(IntPtr start, ulong length, int prot, int flags, int fd, ulong offset);

        /// <summary>
        /// Memory unmap (POSIX munmap).
        /// </summary>
        /// <param name="start">Buffer start.</param>
        /// <param name="length">Buffer length.</param>
        /// <returns>Result flag.</returns>
        [DllImport("libc", EntryPoint="munmap", SetLastError=true)]
        private static unsafe extern int MemUnmap(ulong start, ulong length);

        /// <summary>
        /// Enqueue buffer (POSIX ioctl VIDIOC_QBUF).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="request">Request type (VIDIOC_QBUF).</param>
        /// <param name="buffer">Buffer struct to enqueue.</param>
        /// <returns>Result flag.</returns>
        [DllImport("libc", EntryPoint="ioctl", SetLastError=true)]
        private static extern int EnqueBuffer(int fd, uint request, ref Buffer buffer);

        /// <summary>
        /// Dequeue buffer (POSIX ioctl VIDIOC_DQBUF).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="request">Request type (VIDIOC_DQBUF).</param>
        /// <param name="buffer">Buffer struct into which to dequeue.</param>
        /// <returns>Result flag.</returns>
        [DllImport("libc", EntryPoint="ioctl", SetLastError=true)]
        private static extern int DequeBuffer(int fd, uint request, ref Buffer buffer);

        /// <summary>
        /// Streaming on (POSIX ioctl VIDIOC_STREAMON).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="request">Request type (VIDIOC_STREAMON).</param>
        /// <param name="buftype">Buffer type.</param>
        /// <returns>Result flag.</returns>
        [DllImport("libc", EntryPoint="ioctl", SetLastError=true)]
        private static extern int StreamOn(int fd, uint request, ref BufferType buftype);

        /// <summary>
        /// Streaming off (POSIX ioctl VIDIOC_STREAMOFF).
        /// </summary>
        /// <param name="fd">Device file descriptor.</param>
        /// <param name="request">Request type (VIDIOC_STREAMOFF).</param>
        /// <param name="buftype">Buffer type.</param>
        /// <returns>Result flag.</returns>
        [DllImport("libc", EntryPoint="ioctl", SetLastError=true)]
        private static extern int StreamOff(int fd, uint request, ref BufferType buftype);

        /// <summary>
        /// Capabilities struct (v4l2_capability).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct Capability
        {
            /// <summary>
            /// Driver name.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=16)]
            public string Driver;

            /// <summary>
            /// Video card.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)]
            public string Card;

            /// <summary>
            /// Bus name.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)]
            public string Bus;

            /// <summary>
            /// Driver version.
            /// </summary>
            public uint Version;

            /// <summary>
            /// Driver capabilites.
            /// </summary>
            public CapsFlags Caps;

            /// <summary>
            /// Device capabilites.
            /// </summary>
            public CapsFlags DeviceCaps;

            /// <summary>
            /// Space reserved to ensure expected struct size.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=3)]
            private uint[] reserved;
        }

        /// <summary>
        /// Format description (v4l2_fmtdesc).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct FormatDescription
        {
            /// <summary>
            /// Index assigned by driver.
            /// </summary>
            public uint Index;

            /// <summary>
            /// Buffer type.
            /// </summary>
            public BufferType Type;

            /// <summary>
            /// Format flags.
            /// </summary>
            public FormatFlags Flags;

            /// <summary>
            /// Human readable format description.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)]
            public string Description;

            /// <summary>
            /// Pixel format.
            /// </summary>
            public PixelFormatId PixelFormatId;

            /// <summary>
            /// Reserved space to ensure struct size.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
            private uint[] reserved;
        }

        /// <summary>
        /// Pixel format (v4l2_pix_format).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct PixFormat
        {
            /// <summary>
            /// Width in pixels.
            /// </summary>
            public uint Width;

            /// <summary>
            /// Height in pixels.
            /// </summary>
            public uint Height;

            /// <summary>
            /// Pixel format.
            /// </summary>
            public PixelFormatId PixelFormatId; // pixelFormat

            /// <summary>
            /// Pixel field.
            /// </summary>
            public PixelField Field;

            /// <summary>
            /// Bytes per line (padding or zero).
            /// </summary>
            public uint BytesPerLine;

            /// <summary>
            /// Image size.
            /// </summary>
            public uint SizeImage;

            /// <summary>
            /// Color space.
            /// </summary>
            public ColorSpace ColorSpace;

            /// <summary>
            /// Internal use.
            /// </summary>
            public uint Private;

            /// <summary>
            /// Pixel format flags (V4L2_PIX_FMT_FLAG_*).
            /// </summary>
            public uint Flags;

            /// <summary>
            /// Encoding (v4l2_ycbcr_encoding / v4l2_hsv_encoding enum [union]).
            /// </summary>
            public uint Encoding;

            /// <summary>
            /// Quantization (v4l2_quantization enum).
            /// </summary>
            public uint Quantization;

            /// <summary>
            /// Transfer function (v4l2_xfer_func enum).
            /// </summary>
            public uint TransferFunction;
        }

        /// <summary>
        /// Video format (v4l2_format).
        /// </summary>
        /// <remarks>
        /// Note: only supporting pixel format (normally union of mplane, window, vbi, sdr, meta, etc.)
        /// </remarks>
        [StructLayout(LayoutKind.Explicit, Size=208)]
        internal struct VideoFormat
        {
            /// <summary>
            /// Buffer type.
            /// </summary>
            [FieldOffset(0)]
            public BufferType Type;

            /// <summary>
            /// Pixel format.
            /// </summary>
            [FieldOffset(8)]
            public PixFormat Pixel; // note: only supporting pixel format (normally union of mplane, window, vbi, sdr, meta, etc.)
        }

        /// <summary>
        /// Request buffers (v4l2_requestbuffers).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct RequestBuffers
        {
            /// <summary>
            /// Number of buffers requested.
            /// </summary>
            public uint Count;

            /// <summary>
            /// Buffer type.
            /// </summary>
            public BufferType Type;

            /// <summary>
            /// Memory sharing model.
            /// </summary>
            public Memory Memory;

            /// <summary>
            /// Reserved space to ensure struct size.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=2)]
            private uint[] reserved;
        }

        /// <summary>
        /// Time value.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct TimeVal
        {
            /// <summary>
            /// Seconds since epoch.
            /// </summary>
            public long Seconds;

            /// <summary>
            /// Microseconds since epoch.
            /// </summary>
            public long MicroSeconds;
        }

        /// <summary>
        /// Time code (v4l2_timecode).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct TimeCode
        {
            /// <summary>
            /// Type (V4L2_TC_TYPE_*FPS).
            /// </summary>
            public uint Type;

            /// <summary>
            /// Flags (V4L2_PC_FLAG_*).
            /// </summary>
            public uint Flags;

            /// <summary>
            /// Frames.
            /// </summary>
            public byte Frames;

            /// <summary>
            /// Seconds.
            /// </summary>
            public byte Seconds;

            /// <summary>
            /// Minutes.
            /// </summary>
            public byte Minutes;

            /// <summary>
            /// Hours.
            /// </summary>
            public byte Hours;

            /// <summary>
            /// User-specific bits.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
            public byte[] UserBits;
        }

        /// <summary>
        /// Buffer (v4l2_buffer).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct Buffer
        {
            /// <summary>
            /// Index.
            /// </summary>
            public uint Index;

            /// <summary>
            /// Buffer type.
            /// </summary>
            public BufferType Type;

            /// <summary>
            /// Bytes used.
            /// </summary>
            public uint BytesUsed;

            /// <summary>
            /// Flags (V4L2_BUF_FLAG_*).
            /// </summary>
            public uint Flags;

            /// <summary>
            /// Pixel field.
            /// </summary>
            public PixelField Field;

            /// <summary>
            /// Time value.
            /// </summary>
            public TimeVal TimeStamp;

            /// <summary>
            /// Time code.
            /// </summary>
            public TimeCode TimeCode;

            /// <summary>
            /// Sequence number.
            /// </summary>
            public uint Sequence;

            /// <summary>
            /// Memory sharing model.
            /// </summary>
            public Memory Memory;

            /// <summary>
            /// Pointer being exchanged.
            /// </summary>
            public ulong Pointer;

            /// <summary>
            /// Buffer length.
            /// </summary>
            public uint Length;

            /// <summary>
            /// Reserved space to ensure struct size.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            private uint[] reserved;
        }
    }
}
