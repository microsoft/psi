// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#if FFMPEG
#pragma warning disable SA1615, SA1600
namespace Microsoft.Psi.Media.Native.Linux
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Defines our wrapper class for calling into our Native FFMPEG reader
    /// </summary>
    public class FFMPEGReader
    {
        private IntPtr unmanagedData;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFMPEGReader"/> class.
        /// </summary>
        /// <param name="imageDepth">Depth of requested output images. Must be 24 or 32</param>
        public FFMPEGReader(int imageDepth)
        {
            this.unmanagedData = FFMPEGReaderNative_Alloc(imageDepth);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="FFMPEGReader"/> class.
        /// </summary>
        ~FFMPEGReader()
        {
            if (this.unmanagedData != IntPtr.Zero)
            {
                FFMPEGReaderNative_Dealloc(this.unmanagedData);
                this.unmanagedData = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Gets the width of the current video frame
        /// </summary>
        public int Width
        {
            get
            {
                return (this.unmanagedData != IntPtr.Zero) ? FFMPEGReaderNative_GetWidth(this.unmanagedData) : 0;
            }
        }

        /// <summary>
        /// Gets the height of the current video frame
        /// </summary>
        public int Height
        {
            get
            {
                return (this.unmanagedData != IntPtr.Zero) ? FFMPEGReaderNative_GetHeight(this.unmanagedData) : 0;
            }
        }

        /// <summary>
        /// Gets the audio sample rate
        /// </summary>
        public int AudioSampleRate
        {
            get
            {
                return (this.unmanagedData != IntPtr.Zero) ? FFMPEGReaderNative_GetAudioSampleRate(this.unmanagedData) : 0;
            }
        }

        /// <summary>
        /// Gets the audio bits per sample
        /// </summary>
        public int AudioBitsPerSample
        {
            get
            {
                return (this.unmanagedData != IntPtr.Zero) ? FFMPEGReaderNative_GetAudioBitsPerSample(this.unmanagedData) : 0;
            }
        }

        /// <summary>
        /// Gets the number of audio channels
        /// </summary>
        public int AudioNumChannels
        {
            get
            {
                return (this.unmanagedData != IntPtr.Zero) ? FFMPEGReaderNative_GetAudioNumChannels(this.unmanagedData) : 0;
            }
        }

        [DllImport("Microsoft.Psi.Media.Native.so", EntryPoint="FFMPEGReaderNative_Alloc")]
        public static extern IntPtr FFMPEGReaderNative_Alloc(int imageDepth);

        [DllImport("Microsoft.Psi.Media.Native.so", EntryPoint="FFMPEGReaderNative_Dealloc")]
        public static extern void FFMPEGReaderNative_Dealloc(IntPtr obj);

        [DllImport("Microsoft.Psi.Media.Native.so", EntryPoint="FFMPEGReaderNative_GetWidth")]
        public static extern int FFMPEGReaderNative_GetWidth(IntPtr obj);

        [DllImport("Microsoft.Psi.Media.Native.so", EntryPoint="FFMPEGReaderNative_GetHeight")]
        public static extern int FFMPEGReaderNative_GetHeight(IntPtr obj);

        [DllImport("Microsoft.Psi.Media.Native.so", EntryPoint="FFMPEGReaderNative_GetAudioSampleRate")]
        public static extern int FFMPEGReaderNative_GetAudioSampleRate(IntPtr obj);

        [DllImport("Microsoft.Psi.Media.Native.so", EntryPoint="FFMPEGReaderNative_GetAudioBitsPerSample")]
        public static extern int FFMPEGReaderNative_GetAudioBitsPerSample(IntPtr obj);

        [DllImport("Microsoft.Psi.Media.Native.so", EntryPoint="FFMPEGReaderNative_GetAudioNumChannels")]
        public static extern int FFMPEGReaderNative_GetAudioNumChannels(IntPtr obj);

        [DllImport("Microsoft.Psi.Media.Native.so", EntryPoint="FFMPEGReaderNative_Open", CharSet=CharSet.Ansi)]
        public static extern int FFMPEGReaderNative_Open(IntPtr obj, [MarshalAs(UnmanagedType.LPStr)]string fn);

        [DllImport("Microsoft.Psi.Media.Native.so", EntryPoint="FFMPEGReaderNative_NextFrame")]
        public static extern int FFMPEGReaderNative_NextFrame(IntPtr obj, ref int frameType, ref int requiredBufferSize, ref bool eos);

        [DllImport("Microsoft.Psi.Media.Native.so", EntryPoint="FFMPEGReaderNative_ReadFrameData")]
        public static extern int FFMPEGReaderNative_ReadFrameData(IntPtr obj, IntPtr buffer, ref int bytesRead, ref double timestamp);

        [DllImport("Microsoft.Psi.Media.Native.so", EntryPoint="FFMPEGReaderNative_Close")]
        public static extern int FFMPEGReaderNative_Close(IntPtr obj);

        /// <summary>
        /// Opens a MP4 file for writing.
        /// </summary>
        /// <param name="fn">File to open</param>
        /// <param name="config">Configuration</param>
        public void Open(string fn, FFMPEGReaderConfiguration config)
        {
            int hr = FFMPEGReaderNative_Open(this.unmanagedData, fn);
            if (hr < 0)
            {
                throw new Exception("Failed to read video frame. HRESULT=" + hr.ToString());
            }
        }

        /// <summary>
        /// NextFrame() advances the playback engine to the next audio or video
        /// packet to be processed. This method will fill in 'info' with the type
        /// of packet we are about to process (FrameType), the presentation time
        /// stamp for the frame (Timestamp), and the size of the buffer required
        /// to hold the decompressed data (BufferSize). The actual data is then
        /// read by the client via a call to ReadFrameData().
        /// Returns true if a frame was read; false otherwise.
        /// </summary>
        /// <param name="info">Filled with info about the next frame</param>
        /// <param name="endOfStream">Returns true if end of stream detected</param>
        /// <returns>false if error detected. true otherwise</returns>
        public bool NextFrame(ref FFMPEGFrameInfo info, out bool endOfStream)
        {
            int frameType = 0;
            int requiredBufferSize = 0;
            bool eos = false;
            endOfStream = false;
            int hr = FFMPEGReaderNative_NextFrame(this.unmanagedData, ref frameType, ref requiredBufferSize, ref eos);
            if (hr == 1)
            {
                return false;
            }

            if (eos)
            {
                endOfStream = true;
                return false;
            }

            info.FrameType = frameType;
            info.BufferSize = requiredBufferSize;
            if (hr < 0)
            {
                throw new Exception("Failed to read video frame. HRESULT=" + hr.ToString());
            }

            return true;
        }

        /// <summary>
        /// ReadFrameData() reads the next video or audio frame from the stream.
        /// 'dataBuffer' will be filled with the decompressed data. The buffer
        /// is allocated and controlled by the calling client. The size of the required
        /// buffer was returned by the client's previous call to NextFrame().
        /// Return true if we successfully decoded a frame
        /// </summary>
        /// <param name="dataBuffer">Buffer to fill with data</param>
        /// <param name="bufferSize">Size of buffer in bytes</param>
        /// <param name="timestampMillisecs">Timestamp associated with the data</param>
        /// <returns>false if error detected. true otherwise</returns>
        public bool ReadFrameData(IntPtr dataBuffer, ref int bufferSize, ref double timestampMillisecs)
        {
            double ts = 0.0;
            int bytesRead = 0;
            int hr = FFMPEGReaderNative_ReadFrameData(this.unmanagedData, dataBuffer, ref bytesRead, ref ts);
            if (hr < 0)
            {
                throw new Exception("Failed to read video frame. HRESULT=" + hr.ToString());
            }

            if (hr != 1)
            {
                timestampMillisecs = ts;
                bufferSize = bytesRead;
                return true; // Successfully decoded frame
            }

            return false;
        }

        /// <summary>
        /// Close the reader
        /// </summary>
        public void Close()
        {
            int hr = 0;
            if (this.unmanagedData != IntPtr.Zero)
            {
                hr = FFMPEGReaderNative_Close(this.unmanagedData);
                FFMPEGReaderNative_Dealloc(this.unmanagedData);
                this.unmanagedData = IntPtr.Zero;
            }

            if (hr < 0)
            {
                throw new Exception("Failed to read video frame. HRESULT=" + hr.ToString());
            }
        }
    }
}
#pragma warning restore SA1615, SA1600
#endif // FFMPEG
