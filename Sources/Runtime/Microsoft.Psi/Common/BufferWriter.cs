// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Common
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Auto-resizable buffer (similar to MemoryStream) but with methods to copy pointers any primitive arrays, not just byte[].
    /// This class is typically used in conjunction with <see cref="BufferReader"/>.
    /// </summary>
    public class BufferWriter
    {
        private int currentPosition;
        private byte[] buffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferWriter"/> class.
        /// </summary>
        /// <param name="size">The initial size of the underlying buffer.</param>
        public BufferWriter(int size)
            : this(new byte[size])
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferWriter"/> class.
        /// </summary>
        /// <param name="buffer">The underlying buffer to use in the beginning. The underlying buffer will change once it becomes insufficient.</param>
        public BufferWriter(byte[] buffer)
        {
            this.buffer = buffer;
            this.currentPosition = 0;
        }

        /// <summary>
        /// Gets the current position of the writer.
        /// </summary>
        public int Position
        {
            get
            {
                return this.currentPosition;
            }
        }

        /// <summary>
        /// Gets the current size of the underlying buffer.
        /// </summary>
        public int Size
        {
            get
            {
                return this.buffer.Length;
            }
        }

        /// <summary>
        /// Gets the underlying buffer.
        /// </summary>
        public byte[] Buffer
        {
            get
            {
                return this.buffer;
            }
        }

        /// <summary>
        /// Resets the writer without reallocating the underlying buffer.
        /// </summary>
        public void Reset()
        {
            this.currentPosition = 0;
        }

        /// <summary>
        /// Writes the specified number of bytes from the specified address.
        /// </summary>
        /// <param name="source">The pointer to the memory to copy into the buffer.</param>
        /// <param name="lengthInBytes">The number of bytes to copy.</param>
        public unsafe void Write(void* source, int lengthInBytes)
        {
            int start = this.MoveCurrentPosition(lengthInBytes);

            fixed (byte* buf = this.buffer)
            {
                // more efficient than Array.Copy or cpblk IL instruction because it handles small sizes explicitly
                // http://referencesource.microsoft.com/#mscorlib/system/buffer.cs,c2ca91c0d34a8f86
                System.Buffer.MemoryCopy(source, buf + start, this.buffer.Length - start, lengthInBytes);
            }
        }

        /// <summary>
        /// Writes an array of bytes to the underlying buffer.
        /// </summary>
        /// <param name="source">The array of bytes to read from.</param>
        public void Write(byte[] source)
        {
            unsafe
            {
                fixed (byte* src = source)
                {
                    this.Write(src, source.Length);
                }
            }
        }

        /// <summary>
        /// Writes a portion of an array of bytes to the underlying buffer.
        /// </summary>
        /// <param name="source">The array of bytes to read from.</param>
        /// <param name="start">The index into the source array to start reading from.</param>
        /// <param name="count">The count of bytes to write.</param>
        public void WriteEx(byte[] source, int start, int count)
        {
            unsafe
            {
                if (start > source.Length)
                {
                    return;
                }

                if (count > source.Length - start)
                {
                    count = source.Length - start;
                }

                fixed (byte* src = source)
                {
                    this.Write(src + start, count);
                }
            }
        }

        /// <summary>
        /// Writes an array of values of type SByte to the underlying buffer.
        /// </summary>
        /// <param name="source">The array to read from.</param>
        public void Write(sbyte[] source)
        {
            unsafe
            {
                fixed (sbyte* src = source)
                {
                    this.Write(src, source.Length);
                }
            }
        }

        /// <summary>
        /// Writes an array of values of type Bool to the underlying buffer.
        /// </summary>
        /// <param name="source">The array to read from.</param>
        public void Write(bool[] source)
        {
            unsafe
            {
                fixed (bool* src = source)
                {
                    this.Write(src, source.Length * sizeof(bool));
                }
            }
        }

        /// <summary>
        /// Writes an array of values of type Double to the underlying buffer.
        /// </summary>
        /// <param name="source">The array to read from.</param>
        public void Write(double[] source)
        {
            unsafe
            {
                fixed (double* src = source)
                {
                    this.Write(src, source.Length * sizeof(double));
                }
            }
        }

        /// <summary>
        /// Writes an array of values of type Single to the underlying buffer.
        /// </summary>
        /// <param name="source">The array to read from.</param>
        public void Write(float[] source)
        {
            unsafe
            {
                fixed (float* src = source)
                {
                    this.Write(src, source.Length * sizeof(float));
                }
            }
        }

        /// <summary>
        /// Writes an array of values of type Int16 to the underlying buffer.
        /// </summary>
        /// <param name="source">The array to read from.</param>
        public void Write(short[] source)
        {
            unsafe
            {
                fixed (short* src = source)
                {
                    this.Write(src, source.Length * sizeof(short));
                }
            }
        }

        /// <summary>
        /// Writes an array of values of type Int32 to the underlying buffer.
        /// </summary>
        /// <param name="source">The array to read from.</param>
        public void Write(int[] source)
        {
            unsafe
            {
                fixed (int* src = source)
                {
                    this.Write(src, source.Length * sizeof(int));
                }
            }
        }

        /// <summary>
        /// Writes an array of values of type Int64 to the underlying buffer.
        /// </summary>
        /// <param name="source">The array to read from.</param>
        public void Write(long[] source)
        {
            unsafe
            {
                fixed (long* src = source)
                {
                    this.Write(src, source.Length * sizeof(long));
                }
            }
        }

        /// <summary>
        /// Writes an array of values of type char to the underlying buffer.
        /// </summary>
        /// <param name="source">The array to read from.</param>
        public void Write(char[] source)
        {
            unsafe
            {
                fixed (char* src = source)
                {
                    this.Write(src, source.Length * sizeof(char));
                }
            }
        }

        /// <summary>
        /// Writes an array of values of type UInt16 to the underlying buffer.
        /// </summary>
        /// <param name="source">The array to read from.</param>
        public void Write(ushort[] source)
        {
            unsafe
            {
                fixed (ushort* src = source)
                {
                    this.Write(src, source.Length * sizeof(ushort));
                }
            }
        }

        /// <summary>
        /// Writes an array of values of type UInt32 to the underlying buffer.
        /// </summary>
        /// <param name="source">The array to read from.</param>
        public void Write(uint[] source)
        {
            unsafe
            {
                fixed (uint* src = source)
                {
                    this.Write(src, source.Length * sizeof(uint));
                }
            }
        }

        /// <summary>
        /// Writes an array of values of type UInt64 to the underlying buffer.
        /// </summary>
        /// <param name="source">The array to read from.</param>
        public void Write(ulong[] source)
        {
            unsafe
            {
                fixed (ulong* src = source)
                {
                    this.Write(src, source.Length * sizeof(ulong));
                }
            }
        }

        /// <summary>
        /// Writes a value of type byte to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(byte source)
        {
            unsafe
            {
                this.Write(&source, sizeof(byte));
            }
        }

        /// <summary>
        /// Writes a value of type sbyte to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(sbyte source)
        {
            unsafe
            {
                this.Write(&source, sizeof(sbyte));
            }
        }

        /// <summary>
        /// Writes a value of type bool to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(bool source)
        {
            unsafe
            {
                this.Write(&source, sizeof(bool));
            }
        }

        /// <summary>
        /// Writes a value of type Int16 to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(short source)
        {
            unsafe
            {
                this.Write((byte*)&source, sizeof(short));
            }
        }

        /// <summary>
        /// Writes a value of type UInt16 to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(ushort source)
        {
            unsafe
            {
                this.Write((byte*)&source, sizeof(ushort));
            }
        }

        /// <summary>
        /// Writes a value of type Int32 to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(int source)
        {
            unsafe
            {
                this.Write((byte*)&source, sizeof(int));
            }
        }

        /// <summary>
        /// Writes a value of type UInt32 to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(uint source)
        {
            unsafe
            {
                this.Write((byte*)&source, sizeof(uint));
            }
        }

        /// <summary>
        /// Writes a value of type Int64 to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(long source)
        {
            unsafe
            {
                this.Write((byte*)&source, sizeof(long));
            }
        }

        /// <summary>
        /// Writes a value of type UInt64 to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(ulong source)
        {
            unsafe
            {
                this.Write((byte*)&source, sizeof(ulong));
            }
        }

        /// <summary>
        /// Writes a value of type Single to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(float source)
        {
            unsafe
            {
                this.Write((byte*)&source, sizeof(float));
            }
        }

        /// <summary>
        /// Writes a value of type Double to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(double source)
        {
            unsafe
            {
                this.Write((byte*)&source, sizeof(double));
            }
        }

        /// <summary>
        /// Writes a value of type DateTime to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(DateTime source)
        {
            unsafe
            {
                this.Write((byte*)&source, sizeof(DateTime));
            }
        }

        /// <summary>
        /// Writes a value of type Char to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(char source)
        {
            unsafe
            {
                this.Write((byte*)&source, sizeof(char));
            }
        }

        /// <summary>
        /// Writes a value of type String to the underlying buffer.
        /// </summary>
        /// <param name="source">The value to write.</param>
        public void Write(string source)
        {
            unsafe
            {
                int l;
                if (source == null)
                {
                    l = -1;
                    this.Write(&l, sizeof(int));
                    return;
                }

                l = Encoding.UTF8.GetByteCount(source);
                this.Write(&l, sizeof(int));
                int start = this.MoveCurrentPosition(l);

                fixed (char* chars = source)
                fixed (byte* buf = this.buffer)
                {
                    Encoding.UTF8.GetBytes(chars, source.Length, buf + start, l);
                }
            }
        }

        /// <summary>
        /// Writes a value of type Envelope to the underlying buffer.
        /// </summary>
        /// <param name="envelope">The value to write.</param>
        public void Write(Envelope envelope)
        {
            unsafe
            {
                this.Write((byte*)&envelope, sizeof(Envelope));
            }
        }

        /// <summary>
        /// Copies the specified number of bytes from a <see cref="Stream"/> to the underlying buffer.
        /// </summary>
        /// <param name="stream">The stream from which to read.</param>
        /// <param name="count">The count of bytes to write.</param>
        public void CopyFromStream(Stream stream, int count)
        {
            // ensure buffer has sufficient capacity
            int start = this.MoveCurrentPosition(count);

            int totalBytesRead = 0;
            int remainingCount = count;
            while (remainingCount > 0)
            {
                int bytesRead = stream.Read(this.buffer, start, remainingCount);
                if (bytesRead == 0)
                {
                    // end of stream reached; adjust currentPosition if we read fewer bytes than requested
                    this.currentPosition -= remainingCount;
                    break;
                }

                totalBytesRead += bytesRead;
                start += bytesRead;
                remainingCount -= bytesRead;
            }
        }

        private int MoveCurrentPosition(int requiredBytes)
        {
            // reallocate as needed
            if (this.buffer == null)
            {
                this.buffer = new byte[requiredBytes];
                this.currentPosition = 0;
            }
            else if (requiredBytes > this.buffer.Length - this.currentPosition)
            {
                int freeSpace = this.buffer.Length + requiredBytes;
                var newBuffer = new byte[freeSpace];
                Array.Copy(this.buffer, newBuffer, this.currentPosition);
                this.buffer = newBuffer;
            }

            var start = this.currentPosition;
            this.currentPosition += requiredBytes;
            return start;
        }
    }
}
