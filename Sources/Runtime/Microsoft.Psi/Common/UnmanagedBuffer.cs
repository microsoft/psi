// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Common
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Serialization;

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

        public IntPtr Data => this.data;

        public int Size => this.size;

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

        public static UnmanagedBuffer WrapIntPtr(IntPtr data, int size)
        {
            return new UnmanagedBuffer(data, size, false);
        }

        public static UnmanagedBuffer CreateCopyFrom(IntPtr data, int size)
        {
            var newData = Marshal.AllocHGlobal(size);
            CopyUnmanagedMemory(newData, data, size);
            return new UnmanagedBuffer(newData, size, true);
        }

        public static UnmanagedBuffer CreateCopyFrom(byte[] data)
        {
            var result = UnmanagedBuffer.Allocate(data.Length);
            result.CopyFrom(data);
            return result;
        }

        public UnmanagedBuffer Clone()
        {
            var newData = Marshal.AllocHGlobal(this.size);
            CopyUnmanagedMemory(newData, this.data, this.size);
            return new UnmanagedBuffer(newData, this.size, true);
        }

        public void CopyTo(UnmanagedBuffer destination)
        {
            if (destination == null)
            {
                throw new ArgumentException("Destination unmanaged array is null.");
            }
            else if (destination.size != this.size)
            {
                throw new ArgumentException("Destination unmanaged array is not of the same size.");
            }
            else
            {
                CopyUnmanagedMemory(destination.data, this.data, this.size);
            }
        }

        public byte[] ReadBytes(int count, int offset = 0)
        {
            var result = new byte[count];
            Marshal.Copy(IntPtr.Add(this.data, offset), result, 0, count);
            return result;
        }

        public void CopyTo(byte[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentException("Destination buffer is null.");
            }
            else if (this.size != destination.Length)
            {
                throw new ArgumentException("Destination buffer is not of the same size.");
            }

            Marshal.Copy(this.data, destination, 0, destination.Length);
        }

        public void CopyTo(IntPtr destination, int size)
        {
            if (size != this.size)
            {
                throw new ArgumentException("Destination size is not the same as source.");
            }

            CopyUnmanagedMemory(destination, this.data, this.size);
        }

        public void CopyFrom(UnmanagedBuffer source)
        {
            if (source == null)
            {
                throw new ArgumentException("Source unmanaged array is null.");
            }
            else if (this.size != source.Size)
            {
                throw new ArgumentException("Source unmanaged array is not of the same size.");
            }

            CopyUnmanagedMemory(this.data, source.data, this.size);
        }

        public void CopyFrom(byte[] source)
        {
            if (source == null)
            {
                throw new ArgumentException("Source buffer is null.");
            }
            else if (this.size != source.Length)
            {
                throw new ArgumentException("Source buffer is not of the same size.");
            }

            Marshal.Copy(source, 0, this.data, source.Length);
        }

        public void CopyFrom(IntPtr source, int size)
        {
            if (size != this.size)
            {
                throw new ArgumentException("Destination size is not the same as source.");
            }

            CopyUnmanagedMemory(this.data, source, this.size);
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
            public const int Version = 2;

            public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
            {
                serializers.GetHandler<byte>(); // register element type
                var type = typeof(byte[]);
                var name = TypeSchema.GetContractName(type, serializers.RuntimeVersion);
                var elementsMember = new TypeMemberSchema("Elements", typeof(byte).AssemblyQualifiedName, true);
                var schema = new TypeSchema(name, TypeSchema.GetId(name), type.AssemblyQualifiedName, TypeFlags.IsCollection, new TypeMemberSchema[] { elementsMember }, Version);
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
