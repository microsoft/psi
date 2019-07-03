// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Provides functionality to create Wave files.
    /// </summary>
    public sealed class WaveDataWriterClass : IDisposable
    {
        private WaveFormat format;
        private BinaryWriter writer;
        private uint dataLength;
        private long dataLengthFieldPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveDataWriterClass"/> class.
        /// </summary>
        /// <param name="stream">Stream to which to write.</param>
        /// <param name="format">The audio format.</param>
        public WaveDataWriterClass(Stream stream, WaveFormat format)
        {
            this.format = format;
            this.writer = new BinaryWriter(stream);
            this.WriteWaveFileHeader(format);
            this.WriteWaveDataHeader();
        }

        /// <summary>
        /// Disposes the component.
        /// </summary>
        public void Dispose()
        {
            if (this.writer != null)
            {
                try
                {
                    this.Flush();
                }
                finally
                {
                    this.writer.Close();
                    this.writer = null;
                }
            }
        }

        /// <summary>
        /// Writes the wave data to the file.
        /// </summary>
        /// <param name="data">The raw wave data.</param>
        public void Write(byte[] data)
        {
            this.writer.Write(data);
            this.dataLength += (uint)data.Length;
        }

        /// <summary>
        /// Flushes the data to disk and updates the headers.
        /// </summary>
        public void Flush()
        {
            this.writer.Flush();

            long pos = this.writer.BaseStream.Position;

            // Update the file length
            this.writer.Seek(4, SeekOrigin.Begin);
            this.writer.Write((uint)this.writer.BaseStream.Length - 8);

            // Update the data section length
            this.writer.Seek((int)this.dataLengthFieldPosition, SeekOrigin.Begin);
            this.writer.Write(this.dataLength);

            this.writer.BaseStream.Position = pos;
        }

        /// <summary>
        /// Writes out the wave header to the file.
        /// </summary>
        /// <param name="format">The wave audio format of the data.</param>
        private void WriteWaveFileHeader(WaveFormat format)
        {
            this.writer.Write(Encoding.UTF8.GetBytes("RIFF"));
            this.writer.Write(0u); // file length field which needs to be updated as data is written
            this.writer.Write(Encoding.UTF8.GetBytes("WAVE"));
            this.writer.Write(Encoding.UTF8.GetBytes("fmt "));

            uint headerLength = 18u + format.ExtraSize; // size of fixed portion of WaveFormat is 18
            this.writer.Write(headerLength);
            this.writer.Write(format);
        }

        /// <summary>
        /// Writes out the data header to the file.
        /// </summary>
        private void WriteWaveDataHeader()
        {
            this.writer.Write(Encoding.UTF8.GetBytes("data"));

            // capture the position of the data length field
            this.dataLengthFieldPosition = this.writer.BaseStream.Position;

            this.writer.Write(0u); // data length field which needs to be updated as data is written
        }
    }
}