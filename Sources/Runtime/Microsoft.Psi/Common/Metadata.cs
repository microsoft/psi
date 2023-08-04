// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
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
        /// Metadata using in storing the schema definitions used when serializing and deserializing a type in a Psi Store.
        /// </summary>
        TypeSchema = 2,
    }

    /// <summary>
    /// Represents common metadata used in Psi stores.
    /// </summary>
    public abstract class Metadata
    {
        internal Metadata(MetadataKind kind, string name, int id, int version, int serializationSystemVersion)
        {
            this.Kind = kind;
            this.Name = name;
            this.Id = id;
            this.Version = version;
            this.SerializationSystemVersion = serializationSystemVersion;
        }

        /// <summary>
        /// Gets the name of the object the metadata represents.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the id of the object the metadata represents.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets or sets the version number.
        /// </summary>
        public int Version { get; protected set; }

        /// <summary>
        /// Gets the serialization system version number.
        /// </summary>
        public int SerializationSystemVersion { get; }

        /// <summary>
        /// Gets the metadata kind.
        /// </summary>
        /// <seealso cref="MetadataKind"/>
        public MetadataKind Kind { get; }

        // The Metadata deserializer has no dependency on the Serializer subsystem.
        //
        // For legacy reasons, the metadata is manually persisted with the
        // following fields and following semantics, in the order below (do not change!)
        // string -> name of the metadata, meaning:
        //           - the stream name for PsiStreamMetadata
        //           - the schema name for TypeSchema
        //           - the assembly name for the runtime for RuntimeInfo
        // int -> id of the metadata
        //           - the stream id for PsiStreamMetadata
        //           - the schema id for TypeSchema
        //           - N/A (0) for RuntimeInfo
        // string -> type name
        //           - the stream type for PsiStreamMetadata
        //           - the data type represented by the schema for TypeSchema
        //           - N/A (null) for RuntimeInfo
        // int -> version
        //           - the metadata version for PsiStreamMetadata
        //           - the schema version for TypeSchema
        //           - the runtime assembly version (major << 16 | minor) for RuntimeInfo
        // string -> serializer type assembly qualified name
        //           - N/A (null) for PsiStreamMetadata
        //           - the serializer type assembly qualified name (for TypeSchema)
        //           - N/A (null) for RuntimeInfo
        // int -> serialization system version (for PsiStreamMetadata, TypeSchema and RuntimeInfo)
        // ushort -> stream metadata flags
        //           - stream metadata flags for PsiStreamMetadata
        //           - N/A (0) for TypeSchema
        //           - N/A (0) for RuntimeInfo
        // MetadataKind -> the type of metadata (for PsiStreamMetadata, TypeSchema and RuntimeInfo)
        //
        // This method is static because the entry type differentiator is not the first field,
        // and we need to read several fields before we can decide what type to create. This is for
        // legacy reasons, since the early versions of the catalog file only contained one type of
        // entry (stream metadata).
        //
        // Serialization happens via the overriden Deserialize method in the derived
        // classes (PsiStreamMetadata, TypeSchema and RuntimeInfo). The fields described
        // above are serialized in the order above, followed by fields specific to the
        // derived metadata type.
        internal static Metadata Deserialize(BufferReader metadataBuffer)
        {
            // Read the legacy field structure, as described above.
            var name = metadataBuffer.ReadString();
            var id = metadataBuffer.ReadInt32();
            var typeName = metadataBuffer.ReadString();
            var version = metadataBuffer.ReadInt32();
            var serializerTypeName = metadataBuffer.ReadString();
            var serializationSystemVersion = metadataBuffer.ReadInt32();
            var customFlags = metadataBuffer.ReadUInt16();
            var kind = (MetadataKind)metadataBuffer.ReadUInt16();

            if (kind == MetadataKind.StreamMetadata)
            {
                var result = new PsiStreamMetadata(name, id, typeName, version, serializationSystemVersion, customFlags);
                result.Deserialize(metadataBuffer);
                return result;
            }
            else if (kind == MetadataKind.TypeSchema)
            {
                var result = new TypeSchema(typeName, name, id, version, serializerTypeName, serializationSystemVersion);
                result.Deserialize(metadataBuffer);
                return result;
            }
            else if (kind == MetadataKind.RuntimeInfo)
            {
                var result = new RuntimeInfo(name, version, serializationSystemVersion);
                return result;
            }
            else
            {
                throw new NotSupportedException($"Unknown metadata kind: {kind}");
            }
        }

        internal abstract void Serialize(BufferWriter metadataBuffer);
    }
}
