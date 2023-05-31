// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    /// <summary>
    /// Provides a blocking, buffered audio stream using a circular buffer.
    /// </summary>
    public class BufferedAudioStream : CircularBufferStream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BufferedAudioStream"/> class.
        /// </summary>
        /// <param name="capacity">
        /// The capacity of the internal buffer.
        /// </param>
        /// <remarks>
        /// This class implements a blocking stream such that its Read and Write methods
        /// will not return until the specified number of bytes have been read or written.
        /// This is mainly to support streaming audio to and from .NET System.Speech classes
        /// that read from or write to System.IO.Stream using file stream semantics.
        /// </remarks>
        public BufferedAudioStream(long capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// Reads a sequence of bytes from the stream into the supplied buffer.
        /// </summary>
        /// <param name="buffer">
        /// An array of bytes. When this method returns, the buffer contains
        /// the specified byte array with the values between
        /// <paramref name="offset"/> and (<paramref name="offset"/> +
        /// <paramref name="count"/> - 1) replaced by the bytes read from the
        /// current source.
        /// </param>
        /// <param name="offset">
        /// The zero-based byte offset in <paramref name="buffer"/> at which to
        /// begin storing the data read from the current stream.
        /// </param>
        /// <param name="count">
        /// The maximum number of bytes to be read from the stream.
        /// </param>
        /// <returns>
        /// The total number of bytes read into the buffer.
        /// </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < count)
            {
                int bytesRead = base.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

                // If stream was closed, stop reading.
                if (bytesRead == 0)
                {
                    break;
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }

        /// <summary>
        /// Writes a sequence of bytes to the stream.
        /// </summary>
        /// <param name="buffer">
        /// An array of bytes. This method copies <paramref name="count"/>
        /// bytes from <paramref name="buffer"/> to the current stream.
        /// </param>
        /// <param name="offset">
        /// The zero-based byte offset in <paramref name="buffer"/> at which to
        /// begin copying bytes to the stream.
        /// </param>
        /// <param name="count">
        /// The number of bytes to be written to the stream.
        /// </param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            int totalBytesWritten = 0;
            while (totalBytesWritten < count)
            {
                int bytesWritten = this.WriteNoOverrun(buffer, offset + totalBytesWritten, count - totalBytesWritten);

                // If stream was closed, stop writing.
                if (bytesWritten == 0)
                {
                    break;
                }

                totalBytesWritten += bytesWritten;
            }
        }
    }
}