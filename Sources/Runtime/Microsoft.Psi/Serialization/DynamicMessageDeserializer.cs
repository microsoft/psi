// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Deserializer for any message type to dynamic.
    /// </summary>
    /// <remarks>Uses TypeSchema to construct message type as dynamic primitive and/or ExpandoObject of dynamic.</remarks>
    internal sealed class DynamicMessageDeserializer
    {
        private readonly string rootTypeName;
        private readonly IDictionary<string, TypeSchema> schemasByTypeName;
        private readonly IDictionary<int, TypeSchema> schemasById;
        private readonly List<dynamic> instanceCache = new ();
        private readonly IDictionary<string, string> typeNameSynonyms;
        private readonly XsdDataContractExporter dataContractExporter = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicMessageDeserializer"/> class.
        /// </summary>
        /// <param name="typeName">Type name of message.</param>
        /// <param name="schemas">Collection of known TypeSchemas.</param>
        /// <param name="typeNameSynonyms">Type name synonyms.</param>
        public DynamicMessageDeserializer(string typeName, IDictionary<string, TypeSchema> schemas, IDictionary<string, string> typeNameSynonyms)
        {
            this.rootTypeName = typeName;
            this.schemasByTypeName = schemas;
            this.schemasById = schemas.Values.ToDictionary(s => s.Id);
            this.typeNameSynonyms = typeNameSynonyms;
        }

        /// <summary>
        /// Deserialize message bytes to dynamic.
        /// </summary>
        /// <param name="reader">BufferReader of message bytes.</param>
        /// <returns>dynamic (primitive or ExpandoObject).</returns>
        public dynamic Deserialize(BufferReader reader)
        {
            var message = this.Read(this.rootTypeName, reader);
            this.instanceCache.Clear();
            return message;
        }

        private dynamic Read(string typeName, BufferReader reader, bool isCollectionElement = false)
        {
            // handle primitive types
            var simpleTypeName = typeName.Split(',')[0]; // without assembly qualification
            switch (simpleTypeName)
            {
                case "System.Boolean":
                    return reader.ReadBool();
                case "System.Byte":
                    return reader.ReadByte();
                case "System.Char":
                    return reader.ReadChar();
                case "System.DateTime":
                    return reader.ReadDateTime();
                case "System.Double":
                    return reader.ReadDouble();
                case "System.Int16":
                    return reader.ReadInt16();
                case "System.Int32":
                    return reader.ReadInt32();
                case "System.Int64":
                    return reader.ReadInt64();
                case "System.SByte":
                    return reader.ReadSByte();
                case "System.Single":
                    return reader.ReadSingle();
                case "System.UInt16":
                    return reader.ReadUInt16();
                case "System.UInt32":
                    return reader.ReadUInt32();
                case "System.UInt64":
                    return reader.ReadUInt64();
            }

            // determine type info and schema
            var isString = simpleTypeName == "System.String";
            if (!isString && !this.schemasByTypeName.ContainsKey(typeName))
            {
                string ResolveTypeName()
                {
                    if (this.typeNameSynonyms.TryGetValue(typeName, out var synonym))
                    {
                        return synonym;
                    }
                    else
                    {
                        // try contract name (if type can be resolved)
                        var typ = Type.GetType(typeName, false);
                        if (typ != null)
                        {
                            var contractName = this.dataContractExporter.GetSchemaTypeName(typ);
                            if (contractName != null)
                            {
                                synonym = contractName.ToString();
                                this.typeNameSynonyms.Add(typeName, synonym);
                                return synonym;
                            }
                        }

                        // try custom serializer
                        var prefix = typeName.Split('[', ',')[0];
                        var customTypeName = $"{prefix}+CustomSerializer{typeName.Substring(prefix.Length)}";
                        if (!this.schemasByTypeName.ContainsKey(customTypeName))
                        {
                            throw new Exception($"Unknown schema type name ({typeName}).\nA synonym may be needed (see {nameof(KnownSerializers)}.{nameof(KnownSerializers.RegisterDynamicTypeSchemaNameSynonym)}())");
                        }

                        return customTypeName;
                    }
                }

                typeName = ResolveTypeName();
            }

            var schema = isString ? null : this.schemasByTypeName[typeName];
            var isStruct = !isString && (schema.Flags & TypeFlags.IsStruct) != 0;
            var isClass = !isString && (schema.Flags & TypeFlags.IsClass) != 0;
            var isContract = !isString && (schema.Flags & TypeFlags.IsContract) != 0;
            var isCollection = !isString && (schema.Flags & TypeFlags.IsCollection) != 0;

            // reference types and strings (except when members of a collection) have ref-prefix flags
            if (isClass || isCollection || isContract || (isString && !isCollectionElement))
            {
                var prefix = reader.ReadUInt32();
                switch (prefix & SerializationHandler.RefPrefixMask)
                {
                    case SerializationHandler.RefPrefixNull:
                        return null;
                    case SerializationHandler.RefPrefixExisting:
                        // get existing instance from cache
                        return this.instanceCache[(int)(prefix & SerializationHandler.RefPrefixValueMask)];
                    case SerializationHandler.RefPrefixTyped:
                        // update schema to concrete derived type
                        schema = this.schemasById[(int)(prefix & SerializationHandler.RefPrefixValueMask)];
                        break;
                    case SerializationHandler.RefPrefixNew:
                        // fall through to deserialize below
                        break;
                    default:
                        throw new ArgumentException($"Unexpected ref prefix: {prefix}");
                }
            }

            if (isString)
            {
                var str = reader.ReadString();
                this.instanceCache.Add(str);
                return str;
            }

            if (isCollection)
            {
                var len = reader.ReadUInt32();
                var subType = schema.Members[0].Type; // single Elements member describes contained type
                var elements = new dynamic[len];
                this.instanceCache.Add(elements); // add before contents
                for (var i = 0; i < len; i++)
                {
                    elements[i] = this.Read(subType, reader, true);
                }

                return elements;
            }

            var message = new ExpandoObject() as IDictionary<string, dynamic>;
            if (!isStruct)
            {
                this.instanceCache.Add(message); // add before members
            }

            if (schema.Members != null)
            {
                foreach (var mem in schema.Members)
                {
                    var name = mem.Name;
                    var type = mem.Type;
                    message.Add(name, this.Read(type, reader));
                }
            }

            return message;
        }
    }
}