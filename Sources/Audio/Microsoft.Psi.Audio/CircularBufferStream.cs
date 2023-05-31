// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// Provides an in-memory stream using a circular buffer.
    /// </summary>
    public class CircularBufferStream : Stream
    {
        /// <summary>
        /// Lock object to synchronize access to the internal buffer.
        /// </summary>
        private readonly object bufferLock = new object();

        /// <summary>
        /// The internal buffer.
        /// </summary>
        private byte[] buffer;

        /// <summary>
        /// Flag to indicate that the stream has been closed.
        /// </summary>
        private bool isClosed;

        /// <summary>
        /// The size in bytes of the internal buffer.
        /// </summary>
        private long capacity;

        /// <summary>
        /// Index of the next read location in the internal buffer.
        /// </summary>
        private long readIndex;

        /// <summary>
        /// Index of the next write location in the internal buffer.
        /// </summary>
        private long writeIndex;

        /// <summary>
        /// The number of bytes available in the internal buffer.
        /// </summary>
        private long bytesAvailable;

        /// <summary>
        /// The total number of bytes written to the stream.
        /// </summary>
        private long bytesWritten;

        /// <summary>
        /// The total number of bytes read from the stream.
        /// </summary>
        private long bytesRead;

        /// <summary>
        /// The total number of bytes lost to buffer overruns.
        /// </summary>
        private long bytesOverrun;

        /// <summary>
        /// Whether reads should block if no data is currently available.
        /// </summary>
        private bool blockingReads = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircularBufferStream"/> class.
        /// </summary>
        /// <param name="capacity">The capacity of the circular buffer.</param>
        /// <param name="blockingReads">
        /// A flag indicating whether the stream blocks until data is available for reading.
        /// </param>
        public CircularBufferStream(long capacity, bool blockingReads = true)
        {
            // set the capacity of the circular buffer
            this.capacity = capacity;
            this.buffer = new byte[this.capacity];
            this.blockingReads = blockingReads;
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets the current position within the stream.
        /// </summary>
        public override long Position
        {
            get
            {
                return 0;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the length of the stream. Always returns -1 since the stream is endless.
        /// </summary>
        public override long Length
        {
            get { return -1; }
        }

        /// <summary>
        /// Gets the number of bytes currently available for reading from the buffer.
        /// </summary>
        public long BytesAvailable
        {
            get { return this.bytesAvailable; }
        }

        /// <summary>
        /// Gets the total number of bytes written to the stream.
        /// </summary>
        public long BytesWritten
        {
            get { return this.bytesWritten; }
        }

        /// <summary>
        /// Gets the total number of bytes read from the stream.
        /// </summary>
        public long BytesRead
        {
            get { return this.bytesRead; }
        }

        /// <summary>
        /// Gets the total number of bytes lost to buffer overruns.
        /// </summary>
        public long BytesOverrun
        {
            get { return this.bytesOverrun; }
        }

        /// <summary>
        /// Reads a chunk of data from the buffer.
        /// </summary>
        /// <returns>An array of bytes containing the data.</returns>
        public byte[] Read()
        {
            lock (this.bufferLock)
            {
                while ((this.bytesAvailable == 0) && (!this.isClosed && this.blockingReads))
                {
                    // wait until Write() signals that bytes are available
                    Monitor.Wait(this.bufferLock);
                }

                if (this.isClosed)
                {
                    // return null if stream is closed
                    return null;
                }

                byte[] buffer = new byte[this.bytesAvailable];
                if (this.Read(buffer, 0, buffer.Length) == 0)
                {
                    return null;
                }

                return buffer;
            }
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
            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    return this.Read((IntPtr)(bufferPtr + offset), buffer.Length - offset, count);
                }
            }
        }

        /// <summary>
        /// Reads a sequence of bytes from the stream to a memory location.
        /// </summary>
        /// <param name="destPtr">
        /// A pointer to the memory location to which the data will be copied.
        /// </param>
        /// <param name="destSize">
        /// The maximum number of bytes which may be copied to destPtr.
        /// </param>
        /// <param name="count">
        /// The maximum number of bytes to be read from the stream.
        /// </param>
        /// <returns>
        /// The total number of bytes read into the buffer.
        /// </returns>
        public int Read(IntPtr destPtr, int destSize, int count)
        {
            int bytesToRead = count;

            lock (this.bufferLock)
            {
                while ((this.bytesAvailable == 0) && (!this.isClosed && this.blockingReads))
                {
                    // wait until Write() signals that bytes are available
                    Monitor.Wait(this.bufferLock);
                }

                if (this.isClosed)
                {
                    // return 0 if stream is closed
                    return 0;
                }

                // limit the number of bytes to read to what's available
                if (count > this.bytesAvailable)
                {
                    count = (int)this.bytesAvailable;
                }

                if (this.readIndex + count < this.capacity)
                {
                    unsafe
                    {
                        // if we're not crossing the buffer edge
                        fixed (byte* srcPtr = this.buffer)
                        {
                            Buffer.MemoryCopy(srcPtr + this.readIndex, destPtr.ToPointer(), destSize, count);
                        }
                    }

                    this.readIndex += count;
                    this.bytesAvailable -= count;
                }
                else
                {
                    // if we're crossing the edge, read in two separate chunks
                    int count1 = (int)(this.capacity - this.readIndex);
                    int count2 = count - count1;
                    unsafe
                    {
                        fixed (byte* srcPtr = this.buffer)
                        {
                            Buffer.MemoryCopy(srcPtr + this.readIndex, destPtr.ToPointer(), destSize, count1);
                            Buffer.MemoryCopy(srcPtr, (byte*)destPtr.ToPointer() + count1, destSize - count1, count2);
                        }
                    }

                    this.readIndex = count2;
                    this.bytesAvailable -= count;
                }

                // keep track of the total number of bytes read
                this.bytesRead += count;

                // if the buffer is not full, set the not full event
                if (this.bytesAvailable < this.capacity)
                {
                    Monitor.Pulse(this.bufferLock);
                }
            }

            // return the number of bytes read
            return count;
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
            this.WriteOverrun(buffer, offset, count);
        }

        /// <summary>
        /// Writes a sequence of bytes to the buffer without overrunning.
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
        /// <returns>The number of bytes that were written to the buffer.</returns>
        public int WriteNoOverrun(byte[] buffer, int offset, int count)
        {
            lock (this.bufferLock)
            {
                while ((this.bytesAvailable == this.capacity) && !this.isClosed)
                {
                    // wait until Read() signals that space is available in the buffer
                    Monitor.Wait(this.bufferLock);
                }

                if (this.isClosed)
                {
                    // return 0 if stream is closed
                    return 0;
                }

                if (count > (this.capacity - this.bytesAvailable))
                {
                    count = (int)(this.capacity - this.bytesAvailable);
                }

                if (count > 0)
                {
                    this.WriteOverrun(buffer, offset, count);
                }
            }

            return count;
        }

        /// <summary>
        /// Overrides the <see cref="Stream.Flush"/> method so that no action is performed.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Sets the position within the current stream to the specified value.
        /// </summary>
        /// <param name="offset">
        /// The new position within the stream relative to the loc parameter.
        /// </param>
        /// <param name="origin">
        /// A value of type <see cref="SeekOrigin"/>, which acts as the seek
        /// reference point.
        /// </param>
        /// <returns>
        /// The new position within the stream, calculated by combining the
        /// initial reference point and the offset.
        /// </returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        /// <summary>
        /// Sets the length of the current stream to the specified value.
        /// </summary>
        /// <param name="value">The value at which to set the length.</param>
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        /// <param name="disposing">Flag to indicate whether Dispose() was called.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this.bufferLock)
                {
                    this.isClosed = true;

                    // signal shutdown to any waiting threads
                    Monitor.PulseAll(this.bufferLock);
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Writes a sequence of bytes to the stream allowing overruns.
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
        private void WriteOverrun(byte[] buffer, int offset, int count)
        {
            lock (this.bufferLock)
            {
                // check if the requested write would overrun the buffer
                bool bufferOverrun = this.bytesAvailable + count >= this.capacity;

                // limit the number of bytes to read to the buffer capacity
                if (count > this.capacity)
                {
                    offset = offset + (count - (int)this.capacity);
                    count = (int)this.capacity;
                }

                // check if we will cross a boundary in the write
                if (this.writeIndex + count < this.capacity)
                {
                    // if not, simply write
                    Array.Copy(buffer, offset, this.buffer, this.writeIndex, count);
                    this.writeIndex += count;
                }
                else
                {
                    // o/w we're wrapping so write in two chunks
                    int count1 = (int)(this.capacity - this.writeIndex);
                    int count2 = count - count1;
                    Array.Copy(buffer, offset, this.buffer, this.writeIndex, count1);
                    Array.Copy(buffer, offset + count1, this.buffer, 0, count2);
                    this.writeIndex = count2;
                }

                // keep track of the total number of bytes writeen
                this.bytesWritten += count;

                // now if we've spilled over, move the readIndex to the same
                // location as the writeIndex as the buffer is full
                if (bufferOverrun)
                {
                    this.readIndex = this.writeIndex;
                    this.bytesOverrun += this.bytesAvailable + count - this.capacity;
                    this.bytesAvailable = this.capacity;
                }
                else
                {
                    // o/w just increase available by count
                    this.bytesAvailable += count;
                }

                // if the buffer is not empty, set the not empty event
                if (this.bytesAvailable > 0)
                {
                    Monitor.Pulse(this.bufferLock);
                }
            }
        }
    }
}