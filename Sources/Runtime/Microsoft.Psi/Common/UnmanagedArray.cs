// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Provides efficient, array-style access to data stored in unmanaged memory.
    /// The element type can be any simple value type (a primitive type or a struct with no reference-type fields.
    /// The simple value types supported by this class can only contain fields of primitive types or other simple value types.
    /// For efficient reading and writing of complex types, see the <see cref="Serialization"/> namespace.
    /// </summary>
    /// <typeparam name="T">The element type. Must be a blitable, simple value type (a struct with no reference-type fields).</typeparam>
    [Serializer(typeof(UnmanagedArray<>.CustomSerializer))]
    public unsafe class UnmanagedArray<T> : IList<T>, IDisposable
        where T : struct
    {
        /// <summary>
        /// The size, in bytes, of one array element, as returned by the MSIL sizeof instruction.
        /// </summary>
        public static readonly int ElementSize = BufferEx.SizeOf<T>();

        private IntPtr data;
        private int length;
        private bool ownsMemory;
        private bool isReadOnly;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedArray{T}"/> class from an existing allocation.
        /// </summary>
        /// <param name="buffer">The unmanaged buffer to wrap.</param>
        /// <param name="size">The size, in bytes, of the unmanaged memory allocation.</param>
        /// <param name="isReadOnly">True if the array should be read-only.</param>
        public UnmanagedArray(IntPtr buffer, int size, bool isReadOnly = false)
        {
            if (!Generator.IsSimpleValueType(typeof(T)))
            {
                throw new InvalidOperationException($"Cannot create an unmanaged array of type {typeof(T).FullName} because this type is either a reference type or contains reference-type fields.");
            }

            this.data = buffer;
            this.length = size / ElementSize;
            this.isReadOnly = isReadOnly;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedArray{T}"/> class.
        /// </summary>
        /// <param name="length">The array length (in number of elements) to allocate.</param>
        /// <param name="zeroMemory">Indicates whether the allocated array should be set to 0 before returning.</param>
        public UnmanagedArray(int length, bool zeroMemory = true)
            : this(Marshal.AllocHGlobal(length * ElementSize), length * ElementSize)
        {
            if (zeroMemory)
            {
                this.Clear(0, length);
            }

            this.ownsMemory = true;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="UnmanagedArray{T}"/> class.
        /// </summary>
        ~UnmanagedArray()
        {
            this.DisposeUnmanaged();
        }

        /// <summary>
        /// Gets the pointer to the underlying memory.
        /// </summary>
        public IntPtr Data => this.isReadOnly ? this.data : throw new InvalidOperationException("The array is read-only.");

        /// <summary>
        /// Gets the number of elements in the array.
        /// </summary>
        public int Length => this.length;

        /// <summary>
        /// Gets the number of elements in the array.
        /// </summary>
        int ICollection<T>.Count => this.length;

        /// <summary>
        /// Gets the size of the allocated memory, in bytes.
        /// </summary>
        public int Size => this.length * ElementSize;

        /// <summary>
        /// Gets a value indicating whether the array can be modified or not.
        /// </summary>
        bool ICollection<T>.IsReadOnly => this.isReadOnly;

        /// <summary>
        /// Gets or sets the value of the element at the specified index.
        /// </summary>
        /// <param name="index">The index of the element to set.</param>
        /// <returns>The value of the element at the specified index.</returns>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index < 0 || index > this.length)
                {
                    throw new IndexOutOfRangeException();
                }

                return MemoryAccess.ReadValue<T>(this.data + (index * ElementSize));
            }

            set
            {
                if (this.isReadOnly)
                {
                    throw new InvalidOperationException("The array is read-only.");
                }

                if (index < 0 || index > this.length)
                {
                    throw new IndexOutOfRangeException();
                }

                MemoryAccess.WriteValue(value, this.data + (index * ElementSize));
            }
        }

        /// <summary>
        /// Gets the value at the specified index, without performing bounds checking.
        /// </summary>
        /// <param name="index">Index of the element to get.</param>
        /// <returns>The value at the specified index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T UncheckedGet(int index)
            => MemoryAccess.ReadValue<T>(this.data + (index * ElementSize));

        /// <summary>
        /// Sets a value at the specified index, without performing bounds checking.
        /// </summary>
        /// <param name="index">Index of the element to set.</param>
        /// <param name="value">the value to set the element to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UncheckedSet(int index, T value)
            => MemoryAccess.WriteValue(value, this.data + (index * ElementSize));

        /// <summary>
        /// Gets a reference to the element at the specified index.
        /// Note that this method call is not inlined by the JIT compiler, potentially making it slower than get+set via the indexer.
        /// </summary>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>A reference to the element at the specified index.</returns>
        public ref T GetRef(int index)
        {
            if (this.isReadOnly)
            {
                throw new InvalidOperationException("The array is read-only.");
            }

            if (index < 0 || index > this.length)
            {
                throw new IndexOutOfRangeException();
            }

            return ref MemoryAccess.ReadRef<T>(this.data + (index * ElementSize));
        }

        /// <summary>
        /// Clears the array.
        /// </summary>
        /// <param name="start">The index of the first element to clear.</param>
        /// <param name="length">The number of elements to clear.</param>
        public void Clear(int start, int length)
        {
            if (this.isReadOnly)
            {
                throw new InvalidOperationException("The array is read-only.");
            }

            if (start < 0 || start + length > this.length)
            {
                throw new IndexOutOfRangeException();
            }

            var startByte = start * ElementSize;
            var endByte = (start + length) * ElementSize;
            var p = (long*)(this.data + startByte);
            var end = (long*)this.data + (endByte >> 3);
            for (; p < end; p++)
            {
                *p = 0;
            }

            var b = (byte*)p;
            var endPtr = (byte*)this.data + endByte;
            for (; b < endPtr; b++)
            {
                *b = 0;
            }
        }

        /// <summary>
        /// Clears the array.
        /// </summary>
        public void Clear()
        {
            this.Clear(0, this.length);
        }

        /// <summary>
        /// Copies the elements of the current one-dimensional array to the specified one-dimensional array.
        /// </summary>
        /// <param name="destination">The one-dimensional array that is the destination of the elements copied from the current array.</param>
        /// <param name="srcIndex">The index in the source array at which copying begins.</param>
        /// <param name="destIndex">The index in the destination array at which copying begins.</param>
        /// <param name="length">The number of elements to copy.</param>
        public void CopyTo(T[] destination, int srcIndex, int destIndex, int length)
        {
            if (srcIndex < 0)
            {
                throw new IndexOutOfRangeException("Source index cannot be negative.");
            }

            if (this.length < srcIndex + length)
            {
                throw new IndexOutOfRangeException("The specified length is greater than the number of elements from srcIndex to the end of the source array.");
            }

            if (destIndex < 0)
            {
                throw new IndexOutOfRangeException("Destination index cannot be negative.");
            }

            if (destination.Length < destIndex + length)
            {
                throw new IndexOutOfRangeException("The specified  length is greater than the number of elements from destIndex to the end of the destination array.");
            }

            MemoryAccess.CopyToArray(this.data + (srcIndex * ElementSize), destination, destIndex, length);
        }

        /// <summary>
        /// Copies all the elements of the current one-dimensional array to the specified one-dimensional array.
        /// </summary>
        /// <param name="destination">The one-dimensional array that is the destination of the elements copied from the current array.</param>
        public void CopyTo(T[] destination)
        {
            this.CopyTo(destination, 0);
        }

        /// <summary>
        /// Copies all the elements of the current one-dimensional array to the specified one-dimensional array.
        /// </summary>
        /// <param name="destination">The one-dimensional array that is the destination of the elements copied from the current array.</param>
        /// <param name="index">The index in the destination array at which copying begins.</param>
        public void CopyTo(T[] destination, int index)
        {
            this.CopyTo(destination, 0, index, this.length);
        }

        /// <summary>
        /// Copies the elements of the current one-dimensional array to the specified one-dimensional array.
        /// </summary>
        /// <param name="destination">The one-dimensional array that is the destination of the elements copied from the current array.</param>
        /// <param name="srcIndex">The index in the source array at which copying begins.</param>
        /// <param name="destIndex">The index in the destination array at which copying begins.</param>
        /// <param name="length">The number of elements to copy.</param>
        public void CopyTo(UnmanagedArray<T> destination, int srcIndex, int destIndex, int length)
        {
            if (srcIndex < 0)
            {
                throw new IndexOutOfRangeException("Source index cannot be negative.");
            }

            if (this.length < srcIndex + length)
            {
                throw new IndexOutOfRangeException("The specified length is greater than the number of elements from srcIndex to the end of the source array.");
            }

            if (destIndex < 0)
            {
                throw new IndexOutOfRangeException("Destination index cannot be negative.");
            }

            if (destination.length < destIndex + length)
            {
                throw new IndexOutOfRangeException("The specified  length is greater than the number of elements from destIndex to the end of the destination array.");
            }

            Buffer.MemoryCopy((void*)(this.data + (srcIndex * ElementSize)), (void*)(destination.data + (destIndex * ElementSize)), destination.length, length);
        }

        /// <summary>
        /// Copies all the elements of the current one-dimensional array to the specified one-dimensional array.
        /// </summary>
        /// <param name="destination">The one-dimensional array that is the destination of the elements copied from the current array.</param>
        public void CopyTo(UnmanagedArray<T> destination)
        {
            this.CopyTo(destination, 0);
        }

        /// <summary>
        /// Copies all the elements of the current one-dimensional array to the specified one-dimensional array.
        /// </summary>
        /// <param name="destination">The one-dimensional array that is the destination of the elements copied from the current array.</param>
        /// <param name="index">The index in the destination array at which copying begins.</param>
        public void CopyTo(UnmanagedArray<T> destination, int index)
        {
            this.CopyTo(destination, 0, 0, this.length);
        }

        /// <summary>
        /// Copies the specified number of elements from a one-dimensional array to the current array.
        /// </summary>
        /// <param name="source">The source one-dimensional array.</param>
        /// <param name="srcIndex">The index in the source array at which copying begins.</param>
        /// <param name="destIndex">The index in the destination array at which copying begins.</param>
        /// <param name="length">The number of elements to copy.</param>
        public void Copy(T[] source, int srcIndex, int destIndex, int length)
        {
            if (this.isReadOnly)
            {
                throw new InvalidOperationException("The array is read-only.");
            }

            if (srcIndex < 0)
            {
                throw new IndexOutOfRangeException("Source index cannot be negative.");
            }

            if (source.Length < srcIndex + length)
            {
                throw new IndexOutOfRangeException("The specified length is greater than the number of elements from srcIndex to the end of the source array.");
            }

            if (destIndex < 0)
            {
                throw new IndexOutOfRangeException("Destination index cannot be negative.");
            }

            if (this.Length < destIndex + length)
            {
                throw new IndexOutOfRangeException("The specified  length is greater than the number of elements from destIndex to the end of the destination array.");
            }

            MemoryAccess.CopyFromArray(source, srcIndex, this.data + (destIndex * ElementSize), length * ElementSize, length);
        }

        /// <summary>
        /// Copies all the elements of the specified one-dimensional array to the current array.
        /// </summary>
        /// <param name="source">The source one-dimensional array.</param>
        public void Copy(T[] source)
        {
            this.Copy(source, 0);
        }

        /// <summary>
        /// Copies all the elements of the specified one-dimensional array to the current array.
        /// </summary>
        /// <param name="source">The source one-dimensional array.</param>
        /// <param name="index">The index in the destination array at which copying begins.</param>
        public void Copy(T[] source, int index)
        {
            this.Copy(source, 0, index, source.Length);
        }

        /// <summary>
        /// Resizes the underlying buffer, preserving the existing data.
        /// </summary>
        /// <param name="length">The new length, in count of elements.</param>
        /// <param name="zeroMemory">Indicates whether the trailing end of the new allocation should be set to 0 before returning.</param>
        public unsafe void Resize(int length, bool zeroMemory = true)
        {
            if (this.length == length)
            {
                return;
            }

            if (this.isReadOnly)
            {
                throw new InvalidOperationException("The array is read-only.");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var oldLength = this.length;
            this.length = length;

            var oldData = this.data;
            this.data = Marshal.AllocHGlobal(length * ElementSize);
            Buffer.MemoryCopy((void*)oldData, (void*)this.data, length * ElementSize, Math.Min(oldLength, length) * ElementSize);
            if (length > oldLength && zeroMemory)
            {
                this.Clear(oldLength, length - oldLength);
            }

            if (this.ownsMemory)
            {
                Marshal.FreeHGlobal(oldData);
            }

            this.ownsMemory = true;
        }

        /// <summary>
        /// Frees the underlying memory.
        /// </summary>
        public void Dispose()
        {
            this.DisposeUnmanaged();
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        int IList<T>.IndexOf(T item)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        void IList<T>.Insert(int index, T item)
        {
            throw new InvalidOperationException("Collection was of a fixed size.");
        }

        /// <inheritdoc />
        void IList<T>.RemoveAt(int index)
        {
            throw new InvalidOperationException("Collection was of a fixed size.");
        }

        /// <inheritdoc />
        void ICollection<T>.Add(T item)
        {
            throw new InvalidOperationException("Collection was of a fixed size.");
        }

        /// <inheritdoc />
        bool ICollection<T>.Contains(T item)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        bool ICollection<T>.Remove(T item)
        {
            throw new InvalidOperationException("Collection was of a fixed size.");
        }

        /// <inheritdoc />
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new UnmanagedEnumerator(this);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new UnmanagedEnumerator(this);
        }

        private void DisposeUnmanaged()
        {
            if (this.ownsMemory)
            {
                Marshal.FreeHGlobal(this.data);
                this.data = IntPtr.Zero;
                this.length = 0;
                this.ownsMemory = false;
            }
        }

        // iterator over the array
        private class UnmanagedEnumerator : IEnumerator<T>
        {
            private IntPtr position;
            private IntPtr last;
            private UnmanagedArray<T> array; // to keep alive

            public UnmanagedEnumerator(UnmanagedArray<T> array)
            {
                this.array = array;
                this.position = array.data - UnmanagedArray<T>.ElementSize;
                this.last = array.data + ((array.Length - 1) * UnmanagedArray<T>.ElementSize);
            }

            public T Current => BufferEx.Read<T>(this.position);

            object IEnumerator.Current => this.Current;

            public void Dispose()
            {
                this.array = null;
                this.position = IntPtr.Zero;
                this.last = IntPtr.Zero;
            }

            public bool MoveNext()
            {
                if (this.position == this.last)
                {
                    return false;
                }

                this.position += UnmanagedArray<T>.ElementSize;
                return true;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        // serializer compatible with T[]
        private class CustomSerializer : ISerializer<UnmanagedArray<T>>
        {
            private const int Version = 1;

            public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
            {
                serializers.GetHandler<T>(); // register element type

                var type = typeof(T[]);
                var name = TypeSchema.GetContractName(type, serializers.RuntimeVersion);
                var elementsMember = new TypeMemberSchema("Elements", typeof(T).AssemblyQualifiedName, true);
                var schema = new TypeSchema(name, TypeSchema.GetId(name), type.AssemblyQualifiedName, TypeFlags.IsCollection, new TypeMemberSchema[] { elementsMember }, Version);
                return targetSchema ?? schema;
            }

            public void Serialize(BufferWriter writer, UnmanagedArray<T> instance, SerializationContext context)
            {
                writer.Write(instance.length);
                if (instance.Length > 0)
                {
                    writer.Write((void*)instance.data, instance.length);
                }
            }

            public void PrepareDeserializationTarget(BufferReader reader, ref UnmanagedArray<T> target, SerializationContext context)
            {
                var size = reader.ReadInt32();
                if (target == null)
                {
                    target = new UnmanagedArray<T>(size);
                }
                else
                {
                    target.Resize(size);
                }
            }

            public void Deserialize(BufferReader reader, ref UnmanagedArray<T> target, SerializationContext context)
            {
                // we already read the size in PrepareDeserializationTarget
                if (target.Length > 0)
                {
                    reader.Read((void*)target.data, target.Length);
                }
            }

            public void PrepareCloningTarget(UnmanagedArray<T> instance, ref UnmanagedArray<T> target, SerializationContext context)
            {
                if (target == null)
                {
                    target = new UnmanagedArray<T>(instance.Length);
                }
                else
                {
                    target.Resize(instance.Length);
                }
            }

            public void Clone(UnmanagedArray<T> instance, ref UnmanagedArray<T> target, SerializationContext context)
            {
                Buffer.MemoryCopy((void*)instance.data, (void*)target.data, target.length, instance.Length);
            }

            public void Clear(ref UnmanagedArray<T> target, SerializationContext context)
            {
                // nothing to clear since T is a pure value type
            }
        }
    }
}
