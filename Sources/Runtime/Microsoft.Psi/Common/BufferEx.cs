// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Common
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Provides static methods to efficiently read and write simple value types as well as arrays of simple value types from/to memory buffers (managed or unmanaged).
    /// Similar to <see cref="System.Buffer"/>, but works with any simple struct, not just primitive types.
    /// The simple value types supported by this class can only contain fields of primitive types or other simple value types.
    /// For accessing unmanaged memory as an array of strong-typed elements, see <see cref="UnmanagedArray{T}"/>.
    /// For efficient reading and writing of complex types, see the <see cref="Serialization"/> namespace.
    /// </summary>
    public static unsafe class BufferEx
    {
        /// <summary>
        /// Reads a simple value type from a buffer starting at the specified byte offset.
        /// The method is equivalent to:
        /// <code>target = *(T*)(&amp;source + index)</code>
        /// </summary>
        /// <typeparam name="T">The type of the simple value type to read.</typeparam>
        /// <param name="source">The buffer to read from.</param>
        /// <param name="index">The byte offset in the buffer where the instance to read starts.</param>
        /// <returns>The value read from the buffer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(byte[] source, int index)
            where T : struct
        {
            if (source.Length < index + MemoryAccess.SizeOf<T>())
            {
                throw new IndexOutOfRangeException();
            }

            fixed (void* start = source)
            {
               return MemoryAccess.ReadValue<T>((IntPtr)start + index);
            }
        }

        /// <summary>
        /// Reads a simple value type from the beginning of the buffer.
        /// </summary>
        /// <typeparam name="T">The type of the simple value type to read.</typeparam>
        /// <param name="source">The buffer to read from.</param>
        /// <returns>The value read from the buffer.</returns>
        public static T Read<T>(IntPtr source)
            where T : struct
            => MemoryAccess.ReadValue<T>(source);

        /// <summary>
        /// Writes a simple value type to a buffer at the specified byte offset.
        /// </summary>
        /// <typeparam name="T">The type of the simple value type to write.</typeparam>
        /// <param name="value">The value to write.</param>
        /// <param name="target">The buffer to write to.</param>
        /// <param name="index">The byte offset in the buffer where the specified value will be written.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(T value, byte[] target, int index)
            where T : struct
        {
            if (index < 0)
            {
                throw new IndexOutOfRangeException();
            }

            fixed (void* start = target)
            {
                 MemoryAccess.WriteValue(value, (IntPtr)start + index);
            }
        }

        /// <summary>
        /// Writes a simple value type to buffer at the specified byte offset.
        /// </summary>
        /// <typeparam name="T">The type of the simple value type to write.</typeparam>
        /// <param name="value">The value to write.</param>
        /// <param name="target">The buffer to write to.</param>
        public static void Write<T>(T value, IntPtr target)
            where T : struct
            => MemoryAccess.WriteValue(value, target);

        /// <summary>
        /// Copies a specified number of items from a buffer starting at a particular offset to a destination array starting at a particular index.
        /// Similar to <see cref="System.Buffer.BlockCopy(Array, int, Array, int, int)"/>, but works for any simple struct, not just primitive types.
        /// </summary>
        /// <typeparam name="T">The type of the simple value type to copy.</typeparam>
        /// <param name="src">The source buffer to copy from.</param>
        /// <param name="srcIndex">The zero-based byte offset into src from which copying begins.</param>
        /// <param name="dest">The one-dimensional destination array.</param>
        /// <param name="destIndex">The zero-based index in the destination array at which copying begins.</param>
        /// <param name="length">The number of elements to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(byte[] src, int srcIndex, T[] dest, int destIndex, int length)
            where T : struct
        {
            if (srcIndex < 0)
            {
                throw new IndexOutOfRangeException();
            }

            if (src.Length < srcIndex + (length * MemoryAccess.SizeOf<T>()))
            {
                throw new IndexOutOfRangeException("The specified index and length exceed the size of the source buffer.");
            }

            fixed (void* start = src)
            {
                MemoryAccess.CopyToArray((IntPtr)start + srcIndex, dest, destIndex, length);
            }
        }

        /// <summary>
        /// Copies a specified number of items from a buffer starting at a particular offset to a destination array starting at a particular index.
        /// Similar to <see cref="System.Buffer.BlockCopy(Array, int, Array, int, int)"/>, but works for any simple struct, not just primitive types.
        /// </summary>
        /// <typeparam name="T">The type of the simple value type to copy.</typeparam>
        /// <param name="src">The source buffer to copy from.</param>
        /// <param name="srcIndex">The zero-based byte offset into src from which copying begins.</param>
        /// <param name="dest">The one-dimensional destination array.</param>
        /// <param name="destIndex">The zero-based index in the destination array at which copying begins.</param>
        /// <param name="length">The number of elements to copy.</param>
        public static void Copy<T>(IntPtr src, int srcIndex, T[] dest, int destIndex, int length)
            where T : struct
            => MemoryAccess.CopyToArray((IntPtr)src + srcIndex, dest, destIndex, length);

        /// <summary>
        /// Copies a specified number of items from a array starting at a particular index into a destination buffer starting at a particular offset.
        /// Similar to <see cref="System.Buffer.BlockCopy(Array, int, Array, int, int)"/>, but works for any simple struct, not just primitive types.
        /// </summary>
        /// <typeparam name="T">The type of the simple value type to copy.</typeparam>
        /// <param name="src">The source array to copy from.</param>
        /// <param name="srcIndex">The zero-based byte index into src from which copying begins.</param>
        /// <param name="dest">The destination buffer.</param>
        /// <param name="destIndex">The zero-based offset in the destination buffer at which copying begins.</param>
        /// <param name="length">The number of elements to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(T[] src, int srcIndex, byte[] dest, int destIndex, int length)
            where T : struct
        {
            if (destIndex < 0)
            {
                throw new IndexOutOfRangeException();
            }

            fixed (void* start = dest)
            {
                MemoryAccess.CopyFromArray(src, srcIndex, (IntPtr)start + destIndex, dest.Length - destIndex, length);
            }
        }

        /// <summary>
        /// Copies a specified number of items from a array starting at a particular index into a destination buffer starting at a particular offset.
        /// Similar to <see cref="System.Buffer.BlockCopy(Array, int, Array, int, int)"/>, but works for any simple struct, not just primitive types.
        /// </summary>
        /// <typeparam name="T">The type of the simple value type to copy.</typeparam>
        /// <param name="src">The source array to copy from.</param>
        /// <param name="srcIndex">The zero-based byte index into src from which copying begins.</param>
        /// <param name="dest">The destination buffer.</param>
        /// <param name="destIndex">The zero-based offset in the destination buffer at which copying begins.</param>
        /// <param name="length">The number of elements to copy.</param>
        public static void Copy<T>(T[] src, int srcIndex, IntPtr dest, int destIndex, int length)
            where T : struct
            => MemoryAccess.CopyFromArray(src, srcIndex, (IntPtr)dest + destIndex, length * MemoryAccess.SizeOf<T>(), length);

        /// <summary>
        /// Computes the size, in bytes, of an instance of a simple struct.
        /// </summary>
        /// <typeparam name="T">The type of the simple value type.</typeparam>
        /// <returns>The size, in bytes, of an instance of a simple struct.</returns>
        public static int SizeOf<T>()
            where T : struct
            => MemoryAccess.SizeOf<T>();

        /// <summary>
        /// Computes the size, in bytes, of an instance of a simple value type.
        /// </summary>
        /// <typeparam name="T">The type of the simple value type.</typeparam>
        /// <param name="value">The simple value type.</param>
        /// <returns>The size, in bytes, of an instance of a simple value type.</returns>
        public static int SizeOf<T>(T value)
            where T : struct
            => MemoryAccess.SizeOf<T>();

        /// <summary>
        /// Computes the total size, in bytes, of an array of simple struct elements.
        /// </summary>
        /// <typeparam name="T">The type of the simple struct.</typeparam>
        /// <param name="array">The array of simple struct elements.</param>
        /// <returns>The total size, in bytes, of an array of simple struct elements.</returns>
        public static int SizeOf<T>(T[] array)
            where T : struct
            => MemoryAccess.SizeOf<T>() * array.Length;
    }

    #region templates

    // TEMPLATES FOR GENERATING IL. DO NOT DELETE

    /////// <summary>
    /////// Contains the generated, per-type delegates
    /////// </summary>
    ////public static class MemoryAccess<T>
    ////    where T : struct
    ////{
    ////    public static readonly int ElementSize;

    ////    static unsafe MemoryAccess()
    ////    {
    ////        ElementSize = 0;
    ////    }

    ////    public unsafe static void CopyFromArray(T[] src, int srcIndex, IntPtr dest, int destSize, int count) { }

    ////    public unsafe static void CopyToArray(IntPtr src, T[] dest, int targetIndex, int count) { }

    ////    public unsafe static void WriteValue(ref T source, IntPtr target, int targetSize) { }

    ////    public unsafe static void ReadValue(IntPtr source, ref T target) { }
    ////}

    ////public struct RGB { int r, g, b; }

    /////// <summary>
    /////// Contains the generated, per-type delegates
    /////// </summary>
    ////public static class MemoryAccess
    ////{
    ////    public static readonly int ElementSize;

    ////    static unsafe MemoryAccess()
    ////    {
    ////        ElementSize = sizeof(RGB);
    ////    }

    ////    public unsafe static void CopyFromArray(RGB[] src, int srcIndex, IntPtr dest, int destSizeInBytes, int elementCount)
    ////    {
    ////        fixed (RGB* from = src)
    ////        {
    ////            Buffer.MemoryCopy(from + srcIndex, dest.ToPointer(), destSizeInBytes, elementCount * ElementSize);
    ////        }
    ////    }

    ////    public unsafe static void CopyToArray(IntPtr src, RGB[] dest, int targetIndex, int elementCount)
    ////    {
    ////        fixed (RGB* dst = dest)
    ////        {
    ////            Buffer.MemoryCopy(src.ToPointer(), dst + targetIndex, dest.Length * ElementSize, elementCount * ElementSize);
    ////        }
    ////    }

    ////    public unsafe static void WriteValue(ref RGB source, IntPtr target, int targetSize)
    ////    {
    ////        unsafe
    ////        {
    ////            *(RGB*)target = source;
    ////        }
    ////    }

    ////    public unsafe static void ReadValue(IntPtr source, ref RGB target)
    ////    {
    ////        target = *(RGB*)(source);
    ////    }
    ////}

#endregion
}
