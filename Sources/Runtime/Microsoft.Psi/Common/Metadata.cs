// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Specifies the kind of <see cref="Metadata"/>.
    /// </summary>
    public enum MetadataKind : ushort
    {
        /// <summary>
        /// Metadata used in storing stream data in a Psi store.
        /// </summary>
        StreamMetadata = 0,

        /// <summary>
        /// Metadata used in storing runtime data in a Psi store.
        /// </summary>
        RuntimeInfo = 1,

        /// <summary>
        /// Metadata usied in storing the schema definitions used when serializing and deserializing a type in a Psi Store.
        /// </summary>
        TypeSchema = 2,
    }

    /// <summary>
    /// Represents common metadata used in Psi stores.
    /// </summary>
    public class Metadata
    {
        internal Metadata(MetadataKind kind, string name, int id, string typeName, int version, string serializerTypeName, int serializerVersion, ushort customFlags)
        {
            this.Kind = kind;
            this.Name = name;
            this.Id = id;
            this.TypeName = typeName;
            this.Version = version;
            this.SerializerTypeName = serializerTypeName;
            this.SerializerVersion = serializerVersion;
            this.CustomFlags = customFlags;
        }

        internal Metadata()
        {
        }

        /// <summary>
        /// Gets or sets the name of the object the metadata represents.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets or sets the id of the object the metadata represents.
        /// </summary>
        public int Id { get; protected set; }

        /// <summary>
        /// Gets or sets the name of the type of data conatined in the object the metadata represents.
        /// </summary>
        public string TypeName { get; protected set; }

        /// <summary>
        /// Gets or sets the metadata serializer type name.
        /// </summary>
        public string SerializerTypeName { get; protected set; }

        /// <summary>
        /// Gets or sets the metadata version number.
        /// </summary>
        public int Version { get; protected set; }

        /// <summary>
        /// Gets or sets the metadata serializer verson number.
        /// </summary>
        public int SerializerVersion { get; protected set; }

        /// <summary>
        /// Gets or sets the metadata kind.
        /// </summary>
        /// <seealso cref="MetadataKind"/>
        public MetadataKind Kind { get; protected set; }

        /// <summary>
        /// Gets or sets custom flags implemented in derived types.
        /// </summary>
        public ushort CustomFlags { get; protected set; }

        // custom deserializer with no dependency on the Serializer subsystem
        // order of fields is important for backwards compat and must be the same as the order in Serialize, don't change!
        // This method is static because the entry type differentiator is not the first field, and we need to read several
        // fields before we can decide what type to create. This is for legacy reasons, since the early versions of the
        // catalog file only contained one type of entry (stream metadata).
        internal static Metadata Deserialize(BufferReader metadataBuffer)
        {
            var name = metadataBuffer.ReadString();
            var id = metadataBuffer.ReadInt32();
            var typeName = metadataBuffer.ReadString();
            var version = metadataBuffer.ReadInt32();
            var serializerTypeName = metadataBuffer.ReadString();
            var serializerVersion = metadataBuffer.ReadInt32();
            var customFlags = metadataBuffer.ReadUInt16();
            var kind = (MetadataKind)metadataBuffer.ReadUInt16();

            if (kind == MetadataKind.StreamMetadata)
            {
                var result = new PsiStreamMetadata(name, id, typeName, version, serializerTypeName, serializerVersion, customFlags);
                result.Deserialize(metadataBuffer);
                return result;
            }
            else if (kind == MetadataKind.TypeSchema)
            {
                var result = new TypeSchema(name, id, typeName, version, serializerTypeName, serializerVersion);
                result.Deserialize(metadataBuffer);
                return result;
            }
            else
            {
                // kind == MetadataKind.RuntimeInfo
                var result = new RuntimeInfo(name, id, typeName, version, serializerTypeName, serializerVersion);
                return result;
            }
        }

        // this must be called first by derived classes, before writing any of their own fields
        internal virtual void Serialize(BufferWriter metadataBuffer)
        {
            metadataBuffer.Write(this.Name);
            metadataBuffer.Write(this.Id);
            metadataBuffer.Write(this.TypeName);
            metadataBuffer.Write(this.Version);
            metadataBuffer.Write(this.SerializerTypeName);
            metadataBuffer.Write(this.SerializerVersion);
            metadataBuffer.Write(this.CustomFlags);
            metadataBuffer.Write((ushort)this.Kind);
        }
    }
}
