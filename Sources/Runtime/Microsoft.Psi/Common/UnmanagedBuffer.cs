// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Common
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Unmanaged buffer wrapper class.
    /// </summary>
    [Serializer(typeof(UnmanagedBuffer.CustomSerializer))]
    public class UnmanagedBuffer : IDisposable
    {
        private IntPtr data;

        private int size;

        private bool mustDeallocate;

        private UnmanagedBuffer(IntPtr data, int size, bool mustDeallocate)
        {
            this.data = data;
            this.size = size;
            this.mustDeallocate = mustDeallocate;
        }

        private UnmanagedBuffer(int size)
            : this(Marshal.AllocHGlobal(size), size, true)
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="UnmanagedBuffer"/> class.
        /// </summary>
        ~UnmanagedBuffer()
        {
            this.DisposeUnmanaged();
        }

        /// <summary>
        /// Gets a pointer to underlying data.
        /// </summary>
        public IntPtr Data => this.data;

        /// <summary>
        /// Gets size of underlying data.
        /// </summary>
        public int Size => this.size;

        /// <summary>
        /// Allocate unmanaged buffer.
        /// </summary>
        /// <param name="size">Size (bytes) to allocate.</param>
        /// <returns>Allocated unmanaged buffer.</returns>
        public static UnmanagedBuffer Allocate(int size)
        {
            var data = Marshal.AllocHGlobal(size);
            unsafe
            {
                var d = (byte*)data.ToPointer();
                for (var i = d; i < d + size; i++)
                {
                    *i = 0;
                }
            }

            return new UnmanagedBuffer(data, size, true);
        }

        /// <summary>
        /// Wrap existing unmanaged memory.
        /// </summary>
        /// <param name="data">Pointer to data.</param>
        /// <param name="size">Data size (bytes).</param>
        /// <returns>Wrapped unmanaged buffer.</returns>
        public static UnmanagedBuffer WrapIntPtr(IntPtr data, int size)
        {
            return new UnmanagedBuffer(data, size, false);
        }

        /// <summary>
        /// Create a copy of existing unmanaged memory.
        /// </summary>
        /// <param name="data">Pointer to data.</param>
        /// <param name="size">Data size (bytes).</param>
        /// <returns>Wrapped copy of unmanaged buffer.</returns>
        public static UnmanagedBuffer CreateCopyFrom(IntPtr data, int size)
        {
            var newData = Marshal.AllocHGlobal(size);
            CopyUnmanagedMemory(newData, data, size);
            return new UnmanagedBuffer(newData, size, true);
        }

        /// <summary>
        /// Create a copy of existing managed data.
        /// </summary>
        /// <param name="data">Data to be copied.</param>
        /// <returns>Wrapped copy to unmanaged buffer.</returns>
        public static UnmanagedBuffer CreateCopyFrom(byte[] data)
        {
            var result = UnmanagedBuffer.Allocate(data.Length);
            result.CopyFrom(data);
            return result;
        }

        /// <summary>
        /// Clone unmanaged buffer.
        /// </summary>
        /// <returns>Cloned unmanaged buffer.</returns>
        public UnmanagedBuffer Clone()
        {
            var newData = Marshal.AllocHGlobal(this.size);
            CopyUnmanagedMemory(newData, this.data, this.size);
            return new UnmanagedBuffer(newData, this.size, true);
        }

        /// <summary>
        /// Copy this unmanaged buffer to another instance.
        /// </summary>
        /// <param name="destination">Destination instance to which to copy.</param>
        /// <param name="size">Size (bytes) to copy.</param>
        public void CopyTo(UnmanagedBuffer destination, int size)
        {
            if (destination == null)
            {
                throw new ArgumentException("Destination unmanaged buffer is null.");
            }
            else if (this.size < size)
            {
                throw new ArgumentException("Source unmanaged buffer is not of sufficient size.");
            }
            else if (destination.Size < size)
            {
                throw new ArgumentException("Destination unmanaged buffer is not of sufficient size.");
            }
            else
            {
                CopyUnmanagedMemory(destination.data, this.data, this.size);
            }
        }

        /// <summary>
        /// Read bytes from unmanaged buffer.
        /// </summary>
        /// <param name="count">Count of bytes to copy.</param>
        /// <param name="offset">Offset into buffer.</param>
        /// <returns>Bytes having been copied.</returns>
        public byte[] ReadBytes(int count, int offset = 0)
        {
            if (this.size < count + offset)
            {
                throw new ArgumentException("Unmanaged buffer is not of sufficient size.");
            }

            var result = new byte[count];
            Marshal.Copy(IntPtr.Add(this.data, offset), result, 0, count);
            return result;
        }

        /// <summary>
        /// Copy unmanaged buffer to managed array.
        /// </summary>
        /// <param name="destination">Destination array to which to copy.</param>
        /// <param name="size">Size (bytes) to copy.</param>
        public void CopyTo(byte[] destination, int size)
        {
            if (destination == null)
            {
                throw new ArgumentException("Destination array is null.");
            }
            else if (this.size < size)
            {
                throw new ArgumentException("Source unmanaged buffer is not of sufficient size.");
            }
            else if (destination.Length < size)
            {
                throw new ArgumentException("Destination array is not of sufficient size.");
            }

            Marshal.Copy(this.data, destination, 0, size);
        }

        /// <summary>
        /// Copy unmanaged buffer to address.
        /// </summary>
        /// <param name="destination">Destination address to which to copy.</param>
        /// <param name="size">Size (bytes) to copy.</param>
        public void CopyTo(IntPtr destination, int size)
        {
            if (this.size < size)
            {
                throw new ArgumentException("Source unmanaged buffer is not of sufficient size.");
            }

            CopyUnmanagedMemory(destination, this.data, size);
        }

        /// <summary>
        /// Copy from unmanaged buffer.
        /// </summary>
        /// <param name="source">Unmanaged buffer from which to copy.</param>
        /// <param name="size">Size (bytes) to copy.</param>
        public void CopyFrom(UnmanagedBuffer source, int size)
        {
            if (source == null)
            {
                throw new ArgumentException("Source unmanaged array is null.");
            }
            else if (this.size < size)
            {
                throw new ArgumentException("Destination unmanaged array is not of sufficient size.");
            }
            else if (source.Size < size)
            {
                throw new ArgumentException("Source unmanaged array is not of sufficient size.");
            }

            CopyUnmanagedMemory(this.data, source.data, size);
        }

        /// <summary>
        /// Copy from managed array.
        /// </summary>
        /// <param name="source">Managed array from which to copy.</param>
        public void CopyFrom(byte[] source)
        {
            this.CopyFrom(source, 0, source.Length);
        }

        /// <summary>
        /// Copy from managed array.
        /// </summary>
        /// <param name="source">Managed array from which to copy.</param>
        /// <param name="offset">The zero-based index in the source array where copying should start.</param>
        /// <param name="length">The number of bytes to copy.</param>
        public void CopyFrom(byte[] source, int offset, int length)
        {
            if (source == null)
            {
                throw new ArgumentException("Source array is null.");
            }
            else if (this.size < length)
            {
                throw new ArgumentException("Destination unmanaged buffer is not of sufficient size.");
            }
            else if (source.Length < offset + length)
            {
                throw new ArgumentException("Source array is not of sufficient size.");
            }

            Marshal.Copy(source, offset, this.data, length);
        }

        /// <summary>
        /// Copy from address.
        /// </summary>
        /// <param name="source">Source address from which to copy.</param>
        /// <param name="size">Size (bytes) to copy.</param>
        public void CopyFrom(IntPtr source, int size)
        {
            if (this.size < size)
            {
                throw new ArgumentException("Destination unmanaged buffer is not of sufficient size.");
            }

            CopyUnmanagedMemory(this.data, source, size);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.DisposeUnmanaged();
            GC.SuppressFinalize(this);
        }

        private static unsafe void CopyUnmanagedMemory(IntPtr dst, IntPtr src, int count)
        {
            unsafe
            {
                Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), count, count);
            }
        }

        private void DisposeUnmanaged()
        {
            if (this.mustDeallocate && (this.data != IntPtr.Zero))
            {
                Marshal.FreeHGlobal(this.data);
                this.data = IntPtr.Zero;
                this.size = 0;
                this.mustDeallocate = false;
            }
        }

        private class CustomSerializer : ISerializer<UnmanagedBuffer>
        {
            public const int LatestSchemaVersion = 2;

            /// <inheritdoc />
            public bool? IsClearRequired => false;

            public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
            {
                serializers.GetHandler<byte>(); // register element type
                var type = typeof(byte[]);
                var name = TypeSchema.GetContractName(type, serializers.RuntimeInfo.SerializationSystemVersion);
                var elementsMember = new TypeMemberSchema("Elements", typeof(byte).AssemblyQualifiedName, true);
                var schema = new TypeSchema(
                    type.AssemblyQualifiedName,
                    TypeFlags.IsCollection,
                    new TypeMemberSchema[] { elementsMember },
                    name,
                    TypeSchema.GetId(name),
                    LatestSchemaVersion,
                    this.GetType().AssemblyQualifiedName,
                    serializers.RuntimeInfo.SerializationSystemVersion);
                return targetSchema ?? schema;
            }

            public void Serialize(BufferWriter writer, UnmanagedBuffer instance, SerializationContext context)
            {
                unsafe
                {
                    writer.Write(instance.Size);
                    writer.Write(instance.Data.ToPointer(), instance.Size);
                }
            }

            public void PrepareCloningTarget(UnmanagedBuffer instance, ref UnmanagedBuffer target, SerializationContext context)
            {
                if (target == null || target.Size != instance.Size)
                {
                    target?.Dispose();
                    target = new UnmanagedBuffer(instance.Size);
                }
            }

            public void Clone(UnmanagedBuffer instance, ref UnmanagedBuffer target, SerializationContext context)
            {
                CopyUnmanagedMemory(target.data, instance.data, instance.size);
            }

            public void PrepareDeserializationTarget(BufferReader reader, ref UnmanagedBuffer target, SerializationContext context)
            {
                int size = reader.ReadInt32();
                if (target == null || target.Size != size)
                {
                    target?.Dispose();
                    target = new UnmanagedBuffer(size);
                }
            }

            public void Deserialize(BufferReader reader, ref UnmanagedBuffer target, SerializationContext context)
            {
                unsafe
                {
                    reader.Read(target.Data.ToPointer(), target.Size);
                }
            }

            public void Clear(ref UnmanagedBuffer target, SerializationContext context)
            {
                // nothing to clear in an unmanaged buffer
            }
        }
    }
}
