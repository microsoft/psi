// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Common
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Auto-resizable buffer (similar to MemoryStream) but with methods to read arrays of any simple value type, not just byte[].
    /// This class is typically used in conjunction with <see cref="BufferWriter"/>.
    /// </summary>
    public class BufferReader
    {
        private int currentPosition;
        private int length;
        private byte[] buffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferReader"/> class.
        /// </summary>
        public BufferReader()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferReader"/> class using the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        public BufferReader(byte[] buffer)
            : this(buffer, buffer.Length)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferReader"/> class using the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="length">The count of valid bytes in the buffer.</param>
        public BufferReader(byte[] buffer, int length)
        {
            this.buffer = buffer;
            this.length = length;
            this.currentPosition = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferReader"/> class using the buffer already filled in by the specified <see cref="BufferWriter"/>.
        /// Note that the underlying buffer is shared, and the writer could corrupt it.
        /// </summary>
        /// <param name="writer">The writer to share the buffer with.</param>
        public BufferReader(BufferWriter writer)
        {
            this.buffer = writer.Buffer;
            this.length = writer.Position;
            this.currentPosition = 0;
        }

        /// <summary>
        /// Gets the current position of the reader.
        /// </summary>
        public int Position => this.currentPosition;

        /// <summary>
        /// Gets the number of valid bytes in the underlying buffer.
        /// </summary>
        public int Length => this.length;

        /// <summary>
        /// Gets the number of unread bytes.
        /// </summary>
        public int RemainingLength => this.length - this.currentPosition;

        /// <summary>
        /// Gets the underlying buffer.
        /// </summary>
        public byte[] Buffer => this.buffer;

        /// <summary>
        /// Resets the position of the reader to the beginning of the buffer.
        /// </summary>
        public void Reset()
        {
            this.currentPosition = 0;
        }

        /// <summary>
        /// Moves the current position to the specified place in the underlying buffer.
        /// </summary>
        /// <param name="position">The position to move the to.</param>
        public void Seek(int position)
        {
            if (position < 0 || position > this.length)
            {
                throw new ArgumentException("The specified position is outside the valid range.");
            }

            this.currentPosition = position;
        }

        /// <summary>
        /// Resets the reader to the beginning and resizes the buffer as needed. Note that any data in the buffer is lost.
        /// </summary>
        /// <param name="length">The new buffer length.</param>
        public void Reset(int length)
        {
            if (this.buffer == null || this.buffer.Length < length)
            {
                this.buffer = new byte[length];
            }

            this.length = length;
            this.currentPosition = 0;
        }

        /// <summary>
        /// Copies the specified number of bytes from the underlying buffer to the specified memory address.
        /// </summary>
        /// <param name="target">The target memory address.</param>
        /// <param name="lengthInBytes">The number of bytes to copy.</param>
        public unsafe void Read(void* target, int lengthInBytes)
        {
            var start = this.MoveCurrentPosition(lengthInBytes);

            fixed (byte* buf = this.buffer)
            {
                // more efficient then Array.Copy or cpblk IL instruction because it handles small sizes explicitly
                // http://referencesource.microsoft.com/#mscorlib/system/buffer.cs,c2ca91c0d34a8f86
                System.Buffer.MemoryCopy(buf + start, target, lengthInBytes, lengthInBytes);
            }
        }

        /// <summary>
        /// Copies the specified number of bytes from the underlying buffer into the specified array.
        /// </summary>
        /// <param name="target">The array to copy to.</param>
        /// <param name="count">The number of bytes to copy.</param>
        public void Read(byte[] target, int count)
        {
            unsafe
            {
                fixed (byte* t = target)
                {
                    this.Read(t, count);
                }
            }
        }

        /// <summary>
        /// Copies the specified number of elements of type Double from the underlying buffer into the specified array.
        /// </summary>
        /// <param name="target">The array to copy to.</param>
        /// <param name="count">The number of elements to copy.</param>
        public void Read(double[] target, int count)
        {
            unsafe
            {
                fixed (double* t = target)
                {
                    this.Read(t, count * sizeof(double));
                }
            }
        }

        /// <summary>
        /// Copies the specified number of elements of type Single from the underlying buffer into the specified array.
        /// </summary>
        /// <param name="target">The array to copy to.</param>
        /// <param name="count">The number of elements to copy.</param>
        public void Read(float[] target, int count)
        {
            unsafe
            {
                fixed (float* t = target)
                {
                    this.Read(t, count * sizeof(float));
                }
            }
        }

        /// <summary>
        /// Copies the specified number of elements of type UInt16 from the underlying buffer into the specified array.
        /// </summary>
        /// <param name="target">The array to copy to.</param>
        /// <param name="count">The number of elements to copy.</param>
        public void Read(ushort[] target, int count)
        {
            unsafe
            {
                fixed (ushort* t = target)
                {
                    this.Read(t, count * sizeof(ushort));
                }
            }
        }

        /// <summary>
        /// Copies the specified number of elements of type Int16 from the underlying buffer into the specified array.
        /// </summary>
        /// <param name="target">The array to copy to.</param>
        /// <param name="count">The number of elements to copy.</param>
        public void Read(short[] target, int count)
        {
            unsafe
            {
                fixed (short* t = target)
                {
                    this.Read(t, count * sizeof(short));
                }
            }
        }

        /// <summary>
        /// Copies the specified number of elements of type Int32 from the underlying buffer into the specified array.
        /// </summary>
        /// <param name="target">The array to copy to.</param>
        /// <param name="count">The number of elements to copy.</param>
        public void Read(int[] target, int count)
        {
            unsafe
            {
                fixed (int* t = target)
                {
                    this.Read(t, count * sizeof(int));
                }
            }
        }

        /// <summary>
        /// Copies the specified number of elements of type Int64 from the underlying buffer into the specified array.
        /// </summary>
        /// <param name="target">The array to copy to.</param>
        /// <param name="count">The number of elements to copy.</param>
        public void Read(long[] target, int count)
        {
            unsafe
            {
                fixed (long* t = target)
                {
                    this.Read(t, count * sizeof(long));
                }
            }
        }

        /// <summary>
        /// Copies the specified number of elements of type Char from the underlying buffer into the specified array.
        /// </summary>
        /// <param name="target">The array to copy to.</param>
        /// <param name="count">The number of elements to copy.</param>
        public void Read(char[] target, int count)
        {
            unsafe
            {
                fixed (char* t = target)
                {
                    this.Read(t, count * sizeof(char));
                }
            }
        }

        /// <summary>
        /// Copies the specified number of bytes from the underlying buffer into the specified stream.
        /// </summary>
        /// <param name="target">The stream to copy to.</param>
        /// <param name="count">The number of bytes to copy.</param>
        public void CopyToStream(Stream target, int count)
        {
            var start = this.MoveCurrentPosition(count);
            target.Write(this.buffer, start, count);
        }

        /// <summary>
        /// Reads one Int16 value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public short ReadInt16()
        {
            var start = this.MoveCurrentPosition(sizeof(short));

            unsafe
            {
                fixed (byte* buf = this.buffer)
                {
                    return *(short*)(buf + start);
                }
            }
        }

        /// <summary>
        /// Reads one UInt16 value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public ushort ReadUInt16()
        {
            var start = this.MoveCurrentPosition(sizeof(ushort));

            unsafe
            {
                fixed (byte* buf = this.buffer)
                {
                    return *(ushort*)(buf + start);
                }
            }
        }

        /// <summary>
        /// Reads one Int32 value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public int ReadInt32()
        {
            var start = this.MoveCurrentPosition(sizeof(int));

            unsafe
            {
                fixed (byte* buf = this.buffer)
                {
                    return *(int*)(buf + start);
                }
            }
        }

        /// <summary>
        /// Reads one UInt32 value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public uint ReadUInt32()
        {
            var start = this.MoveCurrentPosition(sizeof(uint));

            unsafe
            {
                fixed (byte* buf = this.buffer)
                {
                    return *(uint*)(buf + start);
                }
            }
        }

        /// <summary>
        /// Reads one Int64 value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public long ReadInt64()
        {
            var start = this.MoveCurrentPosition(sizeof(long));

            unsafe
            {
                fixed (byte* buf = this.buffer)
                {
                    return *(long*)(buf + start);
                }
            }
        }

        /// <summary>
        /// Reads one UInt64 value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public ulong ReadUInt64()
        {
            var start = this.MoveCurrentPosition(sizeof(ulong));

            unsafe
            {
                fixed (byte* buf = this.buffer)
                {
                    return *(ulong*)(buf + start);
                }
            }
        }

        /// <summary>
        /// Reads one Byte value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public byte ReadByte()
        {
            var start = this.MoveCurrentPosition(sizeof(byte));

            unsafe
            {
                fixed (byte* buf = this.buffer)
                {
                    return *(buf + start);
                }
            }
        }

        /// <summary>
        /// Reads one SByte value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public sbyte ReadSByte()
        {
            var start = this.MoveCurrentPosition(sizeof(sbyte));

            unsafe
            {
                fixed (byte* buf = this.buffer)
                {
                    return *(sbyte*)(buf + start);
                }
            }
        }

        /// <summary>
        /// Reads one Single value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public float ReadSingle()
        {
            var start = this.MoveCurrentPosition(sizeof(float));

            unsafe
            {
                fixed (byte* buf = this.buffer)
                {
                    return *(float*)(buf + start);
                }
            }
        }

        /// <summary>
        /// Reads one Double value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public double ReadDouble()
        {
            var start = this.MoveCurrentPosition(sizeof(double));

            unsafe
            {
                fixed (byte* buf = this.buffer)
                {
                    return *(double*)(buf + start);
                }
            }
        }

        /// <summary>
        /// Reads one DateTime value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public DateTime ReadDateTime()
        {
            unsafe
            {
                var start = this.MoveCurrentPosition(sizeof(DateTime));
                fixed (byte* buf = this.buffer)
                {
                    return *(DateTime*)(buf + start);
                }
            }
        }

        /// <summary>
        /// Reads one Char value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public char ReadChar()
        {
            var start = this.MoveCurrentPosition(sizeof(char));

            unsafe
            {
                fixed (byte* buf = this.buffer)
                {
                    return *(char*)(buf + start);
                }
            }
        }

        /// <summary>
        /// Reads one Bool value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public bool ReadBool()
        {
            var start = this.MoveCurrentPosition(sizeof(bool));

            unsafe
            {
                fixed (byte* buf = this.buffer)
                {
                    return *(bool*)(buf + start);
                }
            }
        }

        /// <summary>
        /// Reads one String value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public string ReadString()
        {
            int length = this.ReadInt32();
            if (length == -1)
            {
                return null;
            }

            int start = this.MoveCurrentPosition(length);
            unsafe
            {
                fixed (byte* buf = this.buffer)
                {
                    return Encoding.UTF8.GetString(buf + start, length);
                }
            }
        }

        /// <summary>
        /// Reads one Envelope value from the underlying buffer.
        /// </summary>
        /// <returns>The value read from the buffer.</returns>
        public Envelope ReadEnvelope()
        {
            unsafe
            {
                int start = this.MoveCurrentPosition(sizeof(Envelope));
                fixed (byte* buf = this.buffer)
                {
                    return *(Envelope*)(buf + start);
                }
            }
        }

        private int MoveCurrentPosition(int requiredBytes)
        {
            if (requiredBytes > this.length - this.currentPosition)
            {
                throw new InvalidOperationException("Attempted to read past the end of the buffer");
            }

            var start = this.currentPosition;
            this.currentPosition += requiredBytes;
            return start;
        }
    }
}
