// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Represents the registry of all serializers.
    /// The <see cref="KnownSerializers.Default"/> contains system-wide serializers for the current version of the type system.
    /// Serializers explicitly registered with this instance are used by all other instances unless an override is specified.
    /// When deserializing from a persisted file, the <see cref="Microsoft.Psi.Data.Importer"/> instance returned by
    /// the <see cref="Data.PsiImporter"/> will create its own KnownSerializer instance, and register serializers
    /// compatible with the store being open.
    /// </summary>
    /// <remarks>
    /// The following rules are applied when searching for a suitable serializer:
    /// - registered serializer for the concrete type
    /// - annotated serializer on the concrete type
    /// - registered serializer for the generic type the current type is constructed from
    /// - annotated serializer on the generic type the current type is constructed from
    /// - auto-generated serializer
    ///
    /// When deserializing a polymorphic field, the field's object value might have a different type than the declared (static)
    /// type of the field (e.g the field is declared as IEnumerable{int} and is assigned an int[]).
    /// In such cases the runtime might not be able to find the correct type to use,
    /// and can only instantiate the correct deserializer if one of the following is true:
    /// - a serializer has been explicitly registered for the object type or contract using <see cref="Register{T, TSerializer}(CloningFlags)"/>
    /// - a type has been explicitly registered with an explicit contract name <see cref="Register{T}(string, CloningFlags)"/> or <see cref="Register{T, TSerializer}(string, CloningFlags)"/>.
    /// </remarks>
    public class KnownSerializers
    {
        // Developer notes:
        // The goal of this class is to cache (type -> serializer) mappings for a particular serialization system version
        // and to service two queries at runtime: (type -> serializer) and (id -> serializer).
        // There can be at most one entry in the cache for a given type.
        // Some serializers are registered explicitly, but most are generated on demand, the first time they are requested.
        // Serializers are created from a triad of [<schema>] + [<type>] + [<serializer>]
        // where any two of the three parts can be missing, in which case they are inferred.
        // The <schema> part, if specified, comes from persisted files (is persisted with serialized data)
        //    - the schema can be a full TypeSchema or a partial schema as a pair of (id, type hint)
        //    - if missing, schema is generated from <type> through reflection, according to DataContract or Binary Serialization rules.
        // The <serializer> part, if specified, comes from explicit registration
        //    - if missing, the serializer is determined according to the rules above
        // The <type> part, if specified, comes from the serialization subsystem, the first time it encounters the type during
        // serialization or deserialization.
        //    - if missing, the type is inferred from the schema
        // The <schema> <-> <type> mapping is based on contract name. When the <schema> contract name and the <type> contract name
        // don't match, users can force the association by registering the <type> with the expected <schema> contract name.
        // Even when the contract names match, if the type is used in a polymorphic context,
        // the serialization subsystem might fail to find it when searching based on id.
        // In this case the type needs to be registered explicitly as well.
        // The code makes the following assumptions:
        // - the serialization runtime version is known at construction time
        // - types can be registered before or after their corresponding schemas
        //    (e.g. in live mode, the schema might come after the pipeline starts)
        // - there can be at most one schema for a type and at most one type for a schema,
        //    and they must have matching IDs (implicitly or because of a user-specified name mapping)

        /// <summary>
        /// The default set of types and serializer creation rules globally known to the serialization subsystem.
        /// Custom serializers can be added directly to this set.
        /// </summary>
        public static readonly KnownSerializers Default;

        // the set of types we don't know how to serialize
        private static readonly HashSet<Type> UnserializableTypes = new HashSet<Type>();

        // the serialization subsystem version used by this instance
        private RuntimeInfo runtimeVersion;

        // the default instance marker
        private bool isDefault;

        private object syncRoot = new object();

        // *************** the rules for creating serializers ****************

        // the custom generic serializers, such as SharedSerializer<Shared<T>>, that need to be instantiated for every T
        private Dictionary<Type, Type> templates;

        // the custom serializers, such as ManagedBufferSerializer, which have been registered explicitly rather than with class annotations
        private Dictionary<Type, Type> serializers;

        // used to find a type for a given schema (when creating a handler from a polymorphic field: id -> schema -> type)
        private ConcurrentDictionary<string, Type> knownTypes;

        // used to find the name from a type (when creating a handler: type -> string -> schema)
        private ConcurrentDictionary<Type, string> knownNames;

        // used to find schema for a given contract name (when creating a handler: type -> string -> schema)
        private ConcurrentDictionary<string, TypeSchema> schemas;

        // used to find the schema by id (when creating a handler from a polymorphic field: id -> schema -> type)
        private ConcurrentDictionary<int, TypeSchema> schemasById;

        // used to find the cloning flags for a given type
        private ConcurrentDictionary<Type, CloningFlags> cloningFlags;

        // *************** the cached handlers and handler indexes ****************
        // these caches are accessed often once the rules are expanded into handlers,
        // so we use regular collections to optimize the read path at the expense of the update path (implemented as memcopy and swap)

        // the instantiated serializers, by target type
        private volatile SerializationHandler[] handlers;

        // the handler index, used by serializers to find and cache the index into the handlers set.
        private volatile Dictionary<SerializationHandler, int> index;

        // the instantiated serializers, by target type
        private volatile Dictionary<Type, SerializationHandler> handlersByType;

        // the instantiated serializers, by hash of contract names
        private volatile Dictionary<int, SerializationHandler> handlersById;

        static KnownSerializers()
        {
            UnserializableTypes.Add(typeof(Type));
            UnserializableTypes.Add(typeof(IntPtr));
            UnserializableTypes.Add(typeof(UIntPtr));
            UnserializableTypes.Add(typeof(MemberInfo));
            UnserializableTypes.Add(typeof(System.Diagnostics.StackTrace));
            Default = new KnownSerializers(true, RuntimeInfo.Current);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KnownSerializers"/> class.
        /// </summary>
        /// <param name="runtimeVersion">
        /// The version of the runtime to be compatible with. This dictates the behavior of automatic serialization.
        /// </param>
        public KnownSerializers(RuntimeInfo runtimeVersion = null)
            : this(false, runtimeVersion ?? RuntimeInfo.Current)
        {
        }

        private KnownSerializers(bool isDefault, RuntimeInfo runtimeVersion)
        {
            this.isDefault = isDefault;
            this.runtimeVersion = runtimeVersion; // this can change because of metadata updates!

            this.handlers = new SerializationHandler[0];
            this.handlersById = new Dictionary<int, SerializationHandler>();
            this.handlersByType = new Dictionary<Type, SerializationHandler>();
            this.index = new Dictionary<SerializationHandler, int>();

            if (isDefault)
            {
                this.templates = new Dictionary<Type, Type>();
                this.serializers = new Dictionary<Type, Type>();
                this.knownTypes = new ConcurrentDictionary<string, Type>();
                this.knownNames = new ConcurrentDictionary<Type, string>();
                this.schemas = new ConcurrentDictionary<string, TypeSchema>();
                this.schemasById = new ConcurrentDictionary<int, TypeSchema>();
                this.cloningFlags = new ConcurrentDictionary<Type, CloningFlags>();

                // register non-generic, custom serializers
                this.Register<string, StringSerializer>();
                this.Register<byte[], ByteArraySerializer>();
                this.Register<BufferReader, BufferSerializer>();
                this.Register<string[], StringArraySerializer>();
                this.RegisterGenericSerializer(typeof(EnumerableSerializer<>));
            }
            else
            {
                // all other instances start off with the Default rules
                this.templates = new Dictionary<Type, Type>(Default.templates);
                this.serializers = new Dictionary<Type, Type>(Default.serializers);
                this.knownTypes = new ConcurrentDictionary<string, Type>(Default.knownTypes);
                this.knownNames = new ConcurrentDictionary<Type, string>(Default.knownNames);
                this.schemas = new ConcurrentDictionary<string, TypeSchema>(Default.schemas);
                this.schemasById = new ConcurrentDictionary<int, TypeSchema>(Default.schemasById);
                this.cloningFlags = new ConcurrentDictionary<Type, CloningFlags>(Default.cloningFlags);
            }
        }

        /// <summary>
        /// Event which fires when each new type schema has been added.
        /// </summary>
        public event EventHandler<TypeSchema> SchemaAdded;

        /// <summary>
        /// Gets the version of the serialization subsystem this serializer set is compatible with.
        /// </summary>
        public RuntimeInfo RuntimeVersion => this.runtimeVersion;

        /// <summary>
        /// Gets the set of schemas in use.
        /// </summary>
        public IDictionary<string, TypeSchema> Schemas => this.schemas;

        /// <summary>
        /// Registers type T with the specified contract name.
        /// Use this overload to deserialize data persisted before a type name change.
        /// </summary>
        /// <typeparam name="T">The type to use when deserializing objects with the specified contract.</typeparam>
        /// <param name="contractName">The name to remap. This can be a full type name or a contract name.</param>
        /// <param name="cloningFlags">Optional flags that control the cloning behavior for this type.</param>
        public void Register<T>(string contractName, CloningFlags cloningFlags = CloningFlags.None) => this.Register(typeof(T), contractName, cloningFlags);

        /// <summary>
        /// Registers a given type with the specified contract name.
        /// Use this overload to deserialize data persisted before a type name change.
        /// </summary>
        /// <param name="type">The type to use when deserializing objects with the specified contract.</param>
        /// <param name="contractName">The name to remap. This can be a full type name or a contract name.</param>
        /// <param name="cloningFlags">Optional flags that control the cloning behavior for this type.</param>
        public void Register(Type type, string contractName, CloningFlags cloningFlags = CloningFlags.None)
        {
            contractName = contractName ?? TypeSchema.GetContractName(type, this.runtimeVersion);
            if (this.knownTypes.TryGetValue(contractName, out Type existingType) && existingType != type)
            {
                throw new SerializationException($"Cannot register type {type.AssemblyQualifiedName} under the contract name {contractName} because the type {existingType.AssemblyQualifiedName} is already registered under the same name.");
            }

            if (this.cloningFlags.TryGetValue(type, out var existingFlags) || this.handlersByType.ContainsKey(type))
            {
                // cannot re-register once type flags has been registered or handler has been created
                if (existingFlags != cloningFlags)
                {
                    throw new SerializationException($"Cannot register type {type.AssemblyQualifiedName} with cloning flags ({cloningFlags}) because a handler for it has already been created with flags ({existingFlags}).");
                }
            }

            this.knownTypes[contractName] = type;
            this.knownNames[type] = contractName;
            this.cloningFlags[type] = cloningFlags;
        }

        /// <summary>
        /// Registers a type that the serialization system would not be able find or resolve.
        /// Use this overload when type T is required in a polymorphic context.
        /// </summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <param name="cloningFlags">Optional flags that control the cloning behavior for this type.</param>
        /// <remarks>
        /// When deserializing a polymorphic field, the field's object value might have a different type than the declared (static)
        /// type of the field (e.g the field is declared as IEnumerable{int} and is assigned a MyCustomCollection{int}).
        /// Pre-registering the type allows the runtime to find it in such circumstances.
        /// </remarks>
        public void Register<T>(CloningFlags cloningFlags = CloningFlags.None) => this.Register(typeof(T), null, cloningFlags);

        /// <summary>
        /// Registers a serializer based on type.
        /// Use this overload to register a custom implementation of <see cref="ISerializer{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <typeparam name="TSerializer">
        /// The corresponding type of serializer to use, which replaces any <see cref="SerializerAttribute"/> annotation.
        /// </typeparam>
        /// <param name="cloningFlags">Optional flags that control the cloning behavior for this type.</param>
        public void Register<T, TSerializer>(CloningFlags cloningFlags = CloningFlags.None)
            where TSerializer : ISerializer<T>, new() => this.Register<T, TSerializer>(null, cloningFlags);

        /// <summary>
        /// Registers a type and serializer for the specified contract type.
        /// Use this overload to deserialize data persisted before a type name change.
        /// </summary>
        /// <param name="contractName">The previous contract name of type T.</param>
        /// <param name="cloningFlags">Optional flags that control the cloning behavior for this type.</param>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <typeparam name="TSerializer">
        /// The corresponding type of serializer to use, which replaces any <see cref="SerializerAttribute"/> annotation.
        /// </typeparam>
        public void Register<T, TSerializer>(string contractName, CloningFlags cloningFlags = CloningFlags.None)
            where TSerializer : ISerializer<T>, new()
        {
            Type t = typeof(T);
            this.Register(t, contractName, cloningFlags);
            this.serializers[t] = typeof(TSerializer);
        }

        /// <summary>
        /// Registers a generic serializer, that is, a serializer defined for a generic type.
        /// The generic serializer must implement <see cref="ISerializer{T}"/>.
        /// </summary>
        /// <param name="genericSerializer">The type of generic serializer to register.</param>
        public void RegisterGenericSerializer(Type genericSerializer)
        {
            // var interf = genericSerializer.GetInterface("ISerializer`1");
            var interf = genericSerializer.GetInterface(typeof(ISerializer<>).FullName);
            var serializableType = interf.GetGenericArguments()[0];
            serializableType = TypeResolutionHelper.GetVerifiedType(serializableType.Namespace + "." + serializableType.Name); // FullName doesn't work here
            this.templates[serializableType] = genericSerializer;
        }

        /// <summary>
        /// Gets the cloning flags for the specified type.
        /// </summary>
        /// <param name="type">The type for which to get the cloning flags.</param>
        /// <returns>The cloning flags for the type.</returns>
        internal CloningFlags GetCloningFlags(Type type)
        {
            return this.cloningFlags.TryGetValue(type, out var flags) ? flags : CloningFlags.None;
        }

        /// <summary>
        /// Captures the schema provided by a persisted store.
        /// </summary>
        /// <param name="schema">The schema to register.</param>
        internal void RegisterSchema(TypeSchema schema)
        {
            var id = schema.Id;

            // skip if a handler is already registered for this id
            if (this.handlersById.ContainsKey(id))
            {
                return;
            }

            if (schema.IsPartial && this.schemasById.TryGetValue(id, out TypeSchema otherSchema))
            {
                // schema is already registered
                return;
            }

            // store the schema and override whatever is present already
            this.schemas[schema.Name] = schema;
            this.schemasById[schema.Id] = schema;
        }

        // helper fn to deal with v0 "schema" ([id, type] pairs)
        internal void RegisterMetadata(IEnumerable<Metadata> metadata)
        {
            foreach (var meta in metadata)
            {
                if (meta.Kind == MetadataKind.StreamMetadata)
                {
                    var sm = meta as PsiStreamMetadata;
                    if (sm.RuntimeTypes != null)
                    {
                        // v0 has runtime types affixed to each stream metadata
                        foreach (var kv in sm.RuntimeTypes)
                        {
                            var schema = new TypeSchema(kv.Value, kv.Key, kv.Value, 0);
                            this.RegisterSchema(schema);
                        }
                    }
                }
                else if (meta.Kind == MetadataKind.TypeSchema)
                {
                    this.RegisterSchema((TypeSchema)meta);
                }
            }
        }

        // this is the slow-ish path, called at codegen time, from custom serializers that want to cache a handler
        // and for polymorphic fields, the first time the id is encountered
        internal SerializationHandler<T> GetHandler<T>()
        {
            // important: all code paths that could lead to the creation of a new handler need to lock.
            // We want to make sure a handler is fully created and initialized before it is returned, so we lock before accessing the dictionary
            // A thread that is generating code can re-enter here as it is expanding the type graph,
            // and can get a partially initialized handler to resolve circular type references
            // but other threads have to wait for the expansion to finish.
            lock (this.syncRoot)
            {
                // if we don't have one already, create one
                if (!this.handlersByType.TryGetValue(typeof(T), out SerializationHandler handler))
                {
                    handler = this.AddHandler<T>();
                }

                return (SerializationHandler<T>)handler;
            }
        }

        // called during codegen, to turn the index of the handler into a constant in the emitted code
        internal int GetIndex(SerializationHandler handler) => this.index[handler];

        // this is the fast path, called from generated code at runtime
        internal SerializationHandler GetUntypedHandler(int index) => this.handlers[index];

        // called during codegen, to resolve a field type to a handler
        internal SerializationHandler GetUntypedHandler(Type type)
        {
            // important: all code paths that could lead to the creation of a new handler need to lock.
            lock (this.syncRoot)
            {
                if (this.handlersByType.TryGetValue(type, out SerializationHandler handler))
                {
                    return handler;
                }
            }

            var mi = typeof(KnownSerializers).GetMethod(nameof(this.GetHandler), BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(type);
            return (SerializationHandler)mi.Invoke(this, null);
        }

        // called to find the correct handler for a polymorphic field
        internal SerializationHandler GetUntypedHandler(int handlerId, Type baseType)
        {
            // important: all code paths that could lead to the creation of a new handler need to lock.
            lock (this.syncRoot)
            {
                if (this.handlersById.TryGetValue(handlerId, out SerializationHandler handler))
                {
                    return handler;
                }
            }

            if (this.schemasById.TryGetValue(handlerId, out TypeSchema schema))
            {
                // do we have a type for this schema name?
                if (!this.knownTypes.TryGetValue(schema.Name, out Type type))
                {
                    // nothing registered, try getting a type based on the type hint
                    type = TypeResolutionHelper.GetVerifiedType(schema.TypeName);
                    if (type == null)
                    {
                        throw new SerializationException($"Failed to create a deserializer for type {schema.Name} because no type was registered for this name and the source type {schema.TypeName} could not be found. Add a reference to the assembly containing this type, or register an alternate type for this name.");
                    }

                    // register the type we found with schema name
                    this.Register(type, schema.Name);
                }

                // this will create the handler if needed
                return this.GetUntypedHandler(type);
            }

            throw new SerializationException($"Could not find the appropriate derived type with serialization handler id={handlerId} when deserializing a polymorphic instance or field of type {baseType.AssemblyQualifiedName}");
        }

        private static bool IsSerializable(Type type)
        {
            bool registeredAsNotSerializable = UnserializableTypes.Any(t => t.IsAssignableFrom(type));
            bool knownAsNotSerializable = type.CustomAttributes.Any(a => a.AttributeType == typeof(NativeCppClassAttribute));
            return !registeredAsNotSerializable && !knownAsNotSerializable && !type.IsPointer;
        }

        /// <summary>
        /// Creates and registers a handler for the specified type according to the rules added so far.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <returns>The newly created handler.</returns>
        private SerializationHandler AddHandler<T>()
        {
            SerializationHandler handler = null;
            var type = typeof(T);
            ISerializer<T> serializer = null;
            TypeSchema schema = null;

            if (!this.knownNames.TryGetValue(type, out string name))
            {
                name = TypeSchema.GetContractName(type, this.runtimeVersion);
            }

            if (!this.schemas.TryGetValue(name, out schema))
            {
                // try to match to an existing schema without assembly/version info
                string typeName = TypeResolutionHelper.RemoveAssemblyName(type.AssemblyQualifiedName);
                schema = this.schemas.Values.FirstOrDefault(s => TypeResolutionHelper.RemoveAssemblyName(s.TypeName) == typeName);
            }

            int id = schema?.Id ?? TypeSchema.GetId(name);

            serializer = this.CreateSerializer<T>();
            handler = SerializationHandler.Create<T>(serializer, schema?.Name ?? name, id);

            // first register the handler
            int oldCount = this.handlers.Length;
            var newHandlers = new SerializationHandler[oldCount + 1];
            Array.Copy(this.handlers, newHandlers, oldCount);
            newHandlers[oldCount] = handler;
            this.handlers = newHandlers;

            var newIndex = new Dictionary<SerializationHandler, int>(this.index);
            newIndex[handler] = oldCount;
            this.index = newIndex;

            var newHandlersByType = new Dictionary<Type, SerializationHandler>(this.handlersByType);
            newHandlersByType[type] = handler;
            this.handlersByType = newHandlersByType;

            var newHandlersById = new Dictionary<int, SerializationHandler>(this.handlersById);
            newHandlersById[handler.Id] = handler;
            this.handlersById = newHandlersById;

            // find the schema for this serializer (can be null for interfaces)
            if (serializer != null)
            {
                // initialize the serializer after the handler is registered,
                // to make sure all handlers are registered before initialization runs and
                // allow the serializer initialization code to find and cache the handlers for the types it needs
                schema = serializer.Initialize(this, schema);

                // let any subscribers know that we initialized a new serializer that publishes a schema
                if (schema != null)
                {
                    // store the updated schema and override whatever is present already
                    this.schemas[schema.Name] = schema;
                    this.schemasById[schema.Id] = schema;
                    this.SchemaAdded?.Invoke(this, schema);
                }
            }

            return handler;
        }

        // creates a serializer based on static registration (e.g. attributes),
        // generic templates or hardcoded mappings
        private ISerializer<T> CreateSerializer<T>()
        {
            var type = typeof(T);
            Type serializerType;

            if (this.serializers.TryGetValue(type, out serializerType))
            {
                return (ISerializer<T>)Activator.CreateInstance(serializerType);
            }

            // is this type known as not serializable?
            if (!IsSerializable(type))
            {
                return new NonSerializer<T>();
            }

            // is there a template for it?
            if (type.IsConstructedGenericType)
            {
                // generic
                // T is an instance of a generic type, such as Nullable<int>
                // and a corresponding generic serializer (template) is registered for the generic type
                var typeParams = type.GetGenericArguments();
                var genericType = type.GetGenericTypeDefinition();

                if (this.templates.TryGetValue(genericType, out serializerType))
                {
                    // we found a registered generic serializer, specialize it
                    serializerType = serializerType.MakeGenericType(typeParams);
                    return (ISerializer<T>)Activator.CreateInstance(serializerType, nonPublic: true);
                }
            }

            // is the target type annotated with the [Serializer(typeof(MySerializer))] attribute
            var attribute = type.GetCustomAttribute<SerializerAttribute>(inherit: true /*!!!*/);
            if (attribute != null)
            {
                // if the annotation is on a generic type, the serializer is also generic and we need to instantiate the concrete serializer given the generic arguments
                if (type.IsConstructedGenericType && attribute.SerializerType.IsGenericTypeDefinition)
                {
                    var typeParams = type.GetGenericArguments();
                    serializerType = attribute.SerializerType.MakeGenericType(typeParams);
                }
                else
                {
                    serializerType = attribute.SerializerType;
                }

                return (ISerializer<T>)Activator.CreateInstance(serializerType);
            }

            // for arrays, create an array serializer of the right type,
            // which in turn will delegate element serialization to the correct registered serializer
            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                {
                    throw new NotSupportedException("Multi-dimensional arrays are currently not supported. A workaround would be to convert to a one-dimensional array.");
                }

                // instantiate the correct array serializer based on the type of elements in the array
                var itemType = type.GetElementType();
                Type arraySerializer = Generator.IsSimpleValueType(itemType) ? typeof(SimpleArraySerializer<>) : typeof(ArraySerializer<>);
                serializerType = arraySerializer.MakeGenericType(itemType);
                return (ISerializer<T>)Activator.CreateInstance(serializerType, nonPublic: true);
            }

            return this.CreateWellKnownSerializer<T>();
        }

        private ISerializer<T> CreateWellKnownSerializer<T>()
        {
            Type type = typeof(T);

            if (!IsSerializable(type))
            {
                return new NonSerializer<T>();
            }

            if (type.IsInterface)
            {
                return null;
            }

            if (Generator.IsSimpleValueType(type))
            {
                return new SimpleSerializer<T>();
            }

            if (Generator.IsImmutableType(type))
            {
                return new ImmutableSerializer<T>();
            }

            if (type.IsValueType)
            {
                return new StructSerializer<T>();
            }

            if (type.IsClass)
            {
                return new ClassSerializer<T>();
            }

            throw new SerializationException("Don't know how to serialize objects of type " + type.FullName);
        }
    }
}
