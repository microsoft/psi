// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Static class that provides the methods to generate code for serializing, deserializing and cloning objects.
    /// </summary>
    internal static class Generator
    {
        private const BindingFlags PrivateOrPublicInstanceFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags AllDeclaredFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        private static readonly MethodInfo GetHandlerFromIndexMethod;
        private static readonly MethodInfo BufferRead;
        private static readonly MethodInfo BufferWrite;
        private static readonly MethodInfo MemCpy;

        static Generator()
        {
            GetHandlerFromIndexMethod = typeof(Generator).GetMethod(nameof(Generator.GetHandlerFromIndex), BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(SerializationContext), typeof(int) }, null);
            BufferRead = typeof(BufferReader).GetMethod(nameof(BufferReader.Read), new[] { typeof(void*), typeof(int) });
            BufferWrite = typeof(BufferWriter).GetMethod(nameof(BufferWriter.Write), new[] { typeof(void*), typeof(int) });

            // Buffer.MemoryCopy is more efficient then Array.Copy or cpblk IL instruction because it handles small sizes explicitly
            // http://referencesource.microsoft.com/#mscorlib/system/buffer.cs,c2ca91c0d34a8f86
            MemCpy = typeof(Buffer).GetMethod(nameof(Buffer.MemoryCopy), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(void*), typeof(void*), typeof(long), typeof(long) }, null);
        }

        // The following methods create serialization delegates
        // Note: we generate dynamic delegates instead of full dynamic types because only dynamic delegates can bypass accessibility constraints (private/internal)
        internal static SerializeDelegate<T> GenerateSerializeMethod<T>(Action<ILGenerator> emit)
        {
            var prototype = typeof(ISerializer<T>).GetMethod(nameof(ISerializer<T>.Serialize));
            return (SerializeDelegate<T>)GenerateMethodFromPrototype(prototype, typeof(SerializeDelegate<T>), emit);
        }

        internal static DeserializeDelegate<T> GenerateDeserializeMethod<T>(Action<ILGenerator> emit)
        {
            var prototype = typeof(ISerializer<T>).GetMethod(nameof(ISerializer<T>.Deserialize));
            return (DeserializeDelegate<T>)GenerateMethodFromPrototype(prototype, typeof(DeserializeDelegate<T>), emit);
        }

        internal static CloneDelegate<T> GenerateCloneMethod<T>(Action<ILGenerator> emit)
        {
            var prototype = typeof(ISerializer<T>).GetMethod(nameof(ISerializer<T>.Clone));
            return (CloneDelegate<T>)GenerateMethodFromPrototype(prototype, typeof(CloneDelegate<T>), emit);
        }

        internal static ClearDelegate<T> GenerateClearMethod<T>(Action<ILGenerator> emit)
        {
            var prototype = typeof(ISerializer<T>).GetMethod(nameof(ISerializer<T>.Clear));
            return (ClearDelegate<T>)GenerateMethodFromPrototype(prototype, typeof(ClearDelegate<T>), emit);
        }

        /// <summary>
        /// Generates a dynamic method with the same signature as the specified method prototype.
        /// </summary>
        /// <param name="prototype">the method whose signature to copy.</param>
        /// <param name="delegateType">A delegate type that matches the method signature.</param>
        /// <param name="emit">The IL emitter that knows how to populate the method body.</param>
        /// <returns>The new delegate.</returns>
        internal static Delegate GenerateMethodFromPrototype(MethodInfo prototype, Type delegateType, Action<ILGenerator> emit)
        {
            var method = new DynamicMethod(prototype.Name, prototype.ReturnType, prototype.GetParameters().Select(p => p.ParameterType).ToArray(), typeof(Serializer), true);
            emit(method.GetILGenerator());
            return method.CreateDelegate(delegateType);
        }

        // default field filter that excludes fields marked with [NonSerialized]
        internal static bool NonSerializedFilter(FieldInfo fi)
        {
            return !fi.IsNotSerialized;
        }

        // **********************
        // the following methods do the heavy lifting of generating the IL code for each operation we support
        // **********************

        // emits code to clone all the fields (public, private, readonly) that meet the specified criteria
        internal static void EmitCloneFields(Type type, KnownSerializers serializers, ILGenerator il)
        {
            var cloningFlags = serializers.GetCloningFlags(type);

            // simply invoke the type serializer for each relevant field
            foreach (FieldInfo field in GetAllFields(type))
            {
                // fields with NonSerialized attribute should be skipped if SkipNonSerializedFields is set
                if (field.IsNotSerialized && cloningFlags.HasFlag(CloningFlags.SkipNonSerializedFields))
                {
                    continue;
                }

                // emit exceptions for other non-clonable fields
                else if ((field.FieldType == typeof(IntPtr) || field.FieldType == typeof(UIntPtr)) && !cloningFlags.HasFlag(CloningFlags.CloneIntPtrFields))
                {
                    EmitException(typeof(NotSupportedException), $"Cannot clone field:{field.Name} because cloning of {field.FieldType.Name} fields is disabled by default. To enable cloning of {field.FieldType.Name} fields for the containing type, register the type {type.AssemblyQualifiedName} with the {CloningFlags.CloneIntPtrFields} flag.", il);
                }
                else if (field.FieldType.IsPointer && !cloningFlags.HasFlag(CloningFlags.ClonePointerFields))
                {
                    EmitException(typeof(NotSupportedException), $"Cannot clone field:{field.Name} because cloning of pointer fields is disabled by default. To enable cloning of pointer fields for the containing type, register the type {type.AssemblyQualifiedName} with the {CloningFlags.ClonePointerFields} flag.", il);
                }

                // emit cloning code for clonable fields
                // argument legend: 0 = source, 1 = ref target, 2 = context
                else if (IsSimpleValueType(field.FieldType) || field.FieldType == typeof(string) || field.FieldType.IsPointer)
                {
                    // for primitive types, simply copy the value from one field to the other
                    il.Emit(OpCodes.Ldarg_1); // target
                    if (type.IsClass)
                    {
                        il.Emit(OpCodes.Ldind_Ref);
                    }

                    il.Emit(OpCodes.Ldarg_0); // source
                    il.Emit(OpCodes.Ldfld, field); // source.field
                    il.Emit(OpCodes.Stfld, field); // target.field
                }
                else
                {
                    // find the right serializer
                    var handler = serializers.GetUntypedHandler(field.FieldType);
                    int index = serializers.GetIndex(handler);
                    il.Emit(OpCodes.Ldarg_2); // push context
                    il.Emit(OpCodes.Ldc_I4, index); // push the serializer id
                    il.Emit(OpCodes.Call, GetHandlerFromIndexMethod); // call GetHandler to push the handler

                    // simply invoke the type serializer for each relevant field
                    il.Emit(OpCodes.Ldarg_0); // source
                    il.Emit(OpCodes.Ldfld, field); // source.field
                    il.Emit(OpCodes.Ldarg_1); // target
                    if (type.IsClass)
                    {
                        il.Emit(OpCodes.Ldind_Ref);
                    }

                    il.Emit(OpCodes.Ldflda, field); // target.field &
                    il.Emit(OpCodes.Ldarg_2); // context

                    // handler.Clone(source.field, target.field, context);
                    var mi = handler.GetType().GetMethod(nameof(SerializationHandler<int>.Clone), BindingFlags.Public | BindingFlags.Instance);
                    il.Emit(OpCodes.Call, mi); // we have the right type, no need for callvirt
                }
            }

            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Emits code to throw an exception with the specified message.
        /// </summary>
        /// <param name="type">The type of the exception to throw.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="il">The IL generator of the method in which to throw the exception.</param>
        internal static void EmitException(Type type, string message, ILGenerator il)
        {
            il.Emit(OpCodes.Ldstr, message);
            il.Emit(OpCodes.Newobj, type.GetConstructor(new[] { typeof(string) }));
            il.Emit(OpCodes.Throw);
        }

        // clones a struct into a boxed struct
        internal static void EmitCopyToBox(Type type, ILGenerator il)
        {
            if (!type.IsValueType)
            {
                throw new SerializationException($"The type {type.FullName} is not a value type.");
            }

            // argument legend: 0 = source, 1 = ref target, 2 = context
            il.Emit(OpCodes.Ldarg_1);           // target object
            il.Emit(OpCodes.Ldind_Ref);           // object&
            il.Emit(OpCodes.Unbox, type);  // target T&
            il.Emit(OpCodes.Ldarg_0);           // instance
            il.Emit(OpCodes.Unbox_Any, type);          // instance
            il.Emit(OpCodes.Stobj, type);
            il.Emit(OpCodes.Ret);
        }

        // deserializes a struct as a byte blob, without any unpacking
        internal static void EmitPrimitiveDeserialize(Type type, ILGenerator il)
        {
            if (!IsSimpleValueType(type))
            {
                throw new SerializationException($"The type {type.FullName} is not primitive type.");
            }

            // pin the target
            var localBuilder = il.DeclareLocal(type.MakeByRefType(), pinned: true);
            il.Emit(OpCodes.Ldarg_1); // arg 1 is ref target
            il.Emit(OpCodes.Stloc_0);

            // argument legend: 0 = reader, 1 = ref target, (no context parameter)
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Sizeof, type);
            il.EmitCall(OpCodes.Call, BufferRead, null);
            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Deserializes a simple struct array from a continuous byte blob, without any packing (one memcpy operation).
        /// The generated code is equivalent to:
        /// fixed (void* parray = array)
        /// {
        ///     bufferReader.Read(parray, size);
        /// }
        ///
        /// where bufferReader is <see cref="BufferReader"/>.
        ///
        /// The common usage is;
        /// deserializeFn = Generator.GenerateDeserializeMethod{T[]}(il => Generator.EmitPrimitiveArrayDeserialize(typeof(T), il));.
        /// </summary>
        /// <param name="type">The element type.</param>
        /// <param name="il">The IL generator (typically the body of a method being generated).</param>
        internal static void EmitPrimitiveArrayDeserialize(Type type, ILGenerator il)
        {
            if (!IsSimpleValueType(type))
            {
                throw new SerializationException($"The type {type.FullName} is not a primitive type.");
            }

            // pin the target array
            var localBuilder = il.DeclareLocal(type.MakeByRefType(), pinned: true);
            il.Emit(OpCodes.Ldarg_1); // arg 1 is target&
            il.Emit(OpCodes.Ldind_Ref);
            il.Emit(OpCodes.Stloc_0);

            // the only way to get a pointer to the beginning of a managed array of an unknown generic type is to generate the code on the fly
            // argument legend: 0 = reader, 1 = ref target (no context parameter)
            il.Emit(OpCodes.Ldarg_0); // arg 0 is reader
            il.Emit(OpCodes.Ldloc_0);

            il.Emit(OpCodes.Ldc_I4_0); // load the address of the first array element
            il.Emit(OpCodes.Ldelema, type);
            il.Emit(OpCodes.Sizeof, type); // load the total size in bytes to read
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldind_Ref);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Call, BufferRead);
            il.Emit(OpCodes.Ret);
        }

        // emits code to deserialize all the fields (public, private, readonly) that meet the specified criteria
        internal static void EmitDeserializeFields(Type type, KnownSerializers serializers, ILGenerator il, IEnumerable<MemberInfo> members = null)
        {
            LocalBuilder localBuilder = null;

            // enumerate all fields and deserialize each of them
            foreach (MemberInfo member in members ?? GetSerializableFields(type))
            {
                // inner loop generates this code for each field or property:
                // var handler = GetHandlerFromIndex(index);
                // handler.Deserialize(reader, ref target.fieldOrProp, context)

                // make sure the member is valid
                ValidateMember(type, member);

                // find the right serializer first
                var mt = (member.MemberType == MemberTypes.Field) ? ((FieldInfo)member).FieldType : ((PropertyInfo)member).PropertyType;
                var handler = serializers.GetUntypedHandler(mt);
                int index = serializers.GetIndex(handler);
                il.Emit(OpCodes.Ldarg_2); // push context
                il.Emit(OpCodes.Ldc_I4, index); // push the serializer id
                il.Emit(OpCodes.Call, GetHandlerFromIndexMethod); // call GetHandler to push the handler

                // argument legend: 0 = reader, 1 = ref target, 2 = context
                il.Emit(OpCodes.Ldarg_0); // reader
                il.Emit(OpCodes.Ldarg_1); // target&
                if (type.IsClass)
                {
                    il.Emit(OpCodes.Ldind_Ref);
                }

                if (member.MemberType == MemberTypes.Field)
                {
                    il.Emit(OpCodes.Ldflda, (FieldInfo)member); // target.field &
                }
                else
                {
                    var getter = ((PropertyInfo)member).GetGetMethod(true);
                    il.Emit(getter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getter);
                    localBuilder = il.DeclareLocal(mt);
                    il.Emit(OpCodes.Stloc, localBuilder);
                    il.Emit(OpCodes.Ldloca, localBuilder);
                }

                il.Emit(OpCodes.Ldarg_2); // context

                // call handler.Deserialize(reader, ref target.field, context)
                var mi = handler.GetType().GetMethod(nameof(SerializationHandler<int>.Deserialize), BindingFlags.Instance | BindingFlags.Public);
                il.Emit(OpCodes.Call, mi); // not callvirt, since we have the right type

                // for properties, call the setter with the result
                if (member.MemberType == MemberTypes.Property)
                {
                    il.Emit(OpCodes.Ldarg_1); // target&
                    if (type.IsClass)
                    {
                        il.Emit(OpCodes.Ldind_Ref);
                    }

                    il.Emit(OpCodes.Ldloc, localBuilder); // deserialization result
                    var setter = ((PropertyInfo)member).GetSetMethod(true);
                    il.Emit(setter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, setter);
                }
            }

            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Writes a primitive struct as byte blob without any packing.
        /// The generated code is equivalent to:
        /// <code>bufferWriter.Write(&amp;val, sizeof(val));</code>
        ///
        /// where bufferWriter is <see cref="BufferWriter"/>.
        /// </summary>
        /// <param name="type">The type of object to serialize (must be a simple value type).</param>
        /// <param name="il">The IL generator of a method taking a BufferWriter as first argument and the value type to serialize as the second argument.</param>
        /// <remarks>We don't have to pin the argument, since we expect a simple struct.</remarks>
        internal static void EmitPrimitiveSerialize(Type type, ILGenerator il)
        {
            if (!IsSimpleValueType(type))
            {
                throw new SerializationException($"The type {type.FullName} is not a primitive type.");
            }

            // argument legend: 0 = "writer", 1 = "source", (no context parameter)
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarga_S, 1);
            il.Emit(OpCodes.Sizeof, type);
            il.Emit(OpCodes.Call, BufferWrite);
            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Serializes a simple struct array as continuous byte blob, without any packing (one memcpy operation).
        /// The generated code is equivalent to:
        /// fixed (void* parray = array)
        /// {
        ///     bufferWriter.Write(parray, array.Length);
        /// }
        ///
        /// where bufferWriter is <see cref="BufferWriter"/>.
        ///
        /// The common usage is;
        /// SerializeFn = Generator.GenerateSerializeMethod{T[]}(il => Generator.EmitPrimitiveArraySerialize(typeof(T), il));.
        /// </summary>
        /// <param name="type">The element type.</param>
        /// <param name="il">The IL generator (typically the body of a method being generated).</param>
        internal static void EmitPrimitiveArraySerialize(Type type, ILGenerator il)
        {
            if (!IsSimpleValueType(type))
            {
                throw new SerializationException($"The type {type.FullName} is not a primitive type.");
            }

            // argument legend: 0 = writer, 1 = source (no context parameter)

            // pin the source array
            var localBuilder = il.DeclareLocal(type.MakeByRefType(), pinned: true);
            il.Emit(OpCodes.Ldarg_1); // arg 1 is source
            il.Emit(OpCodes.Stloc_0);

            // the only way to get a pointer to the beginning of a managed array of an unknown generic type is to generate the code on the fly
            il.Emit(OpCodes.Ldarg_0); // writer
            il.Emit(OpCodes.Ldloc_0); // source
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldelema, type); // load the address of the first array element
            il.Emit(OpCodes.Sizeof, type);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Mul);
            il.EmitCall(OpCodes.Call, BufferWrite, null);
            il.Emit(OpCodes.Ret);
        }

        // emits code to serialize all the fields (public, private, readonly) that meet the specified criteria
        internal static void EmitSerializeFields(Type type, KnownSerializers serializers, ILGenerator il, IEnumerable<MemberInfo> members = null)
        {
            // enumerate all fields and serialize each of them
            foreach (MemberInfo member in members ?? GetSerializableFields(type))
            {
                // inner loop generates this code for each field or property:
                // var handler = GetHandlerFromIndex(index);
                // handler.Serialize(writer, source.fieldOrProp, context);

                // make sure the member is valid
                ValidateMember(type, member);

                // find the right serializer
                var mt = (member.MemberType == MemberTypes.Field) ? ((FieldInfo)member).FieldType : ((PropertyInfo)member).PropertyType;
                var handler = serializers.GetUntypedHandler(mt);
                int index = serializers.GetIndex(handler);
                il.Emit(OpCodes.Ldarg_2); // push context
                il.Emit(OpCodes.Ldc_I4, index); // push the serializer id
                il.Emit(OpCodes.Call, GetHandlerFromIndexMethod); // call GetHandler to push the handler

                // push the arguments
                il.Emit(OpCodes.Ldarg_0); // writer
                il.Emit(OpCodes.Ldarg_1); // source
                if (member.MemberType == MemberTypes.Field)
                {
                    il.Emit(OpCodes.Ldfld, (FieldInfo)member); // source.field
                }
                else
                {
                    var getter = ((PropertyInfo)member).GetGetMethod(true);
                    il.Emit(getter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getter);
                }

                il.Emit(OpCodes.Ldarg_2); // context

                // call handler.Serialize(writer, source.field, context)
                var mi = handler.GetType().GetMethod(nameof(SerializationHandler<int>.Serialize), BindingFlags.Instance | BindingFlags.Public);
                il.Emit(OpCodes.Call, mi); // we have the right type, no need for callvirt
            }

            il.Emit(OpCodes.Ret);
        }

        // emits code to clear all the fields (public, private, readonly) that meet the specified criteria
        internal static void EmitClearFields(Type type, KnownSerializers serializers, ILGenerator il)
        {
            // simply invoke the type serializer for each relevant field
            foreach (FieldInfo field in GetSerializableFields(type))
            {
                // optimization to omit clearing field types which do not require clearing prior to reuse
                if (!IsClearRequired(field.FieldType, serializers))
                {
                    continue;
                }

                // find the right handler
                var handler = serializers.GetUntypedHandler(field.FieldType);
                int index = serializers.GetIndex(handler);

                // argument legend: 0 = ref target, 1 = context
                il.Emit(OpCodes.Ldarg_1); // push context
                il.Emit(OpCodes.Ldc_I4, index); // push the serializer id
                il.Emit(OpCodes.Call, GetHandlerFromIndexMethod); // call GetHandler to push the handler

                // handler.Clear(ref target.Field, context);
                // il.EmitWriteLine(string.Format("Clear(ref target.{0});", field.Name));
                il.Emit(OpCodes.Ldarg_0); // target
                if (type.IsClass)
                {
                    il.Emit(OpCodes.Ldind_Ref);
                }

                il.Emit(OpCodes.Ldflda, field); // target.field &
                il.Emit(OpCodes.Ldarg_1); // context
                var mi = handler.GetType().GetMethod(nameof(SerializationHandler<int>.Clear), BindingFlags.Instance | BindingFlags.Public);
                il.Emit(OpCodes.Call, mi);
            }

            il.Emit(OpCodes.Ret);
        }

        // true if the type is a primitive type or a value type with only primitive type fields
        internal static bool IsSimpleValueType(Type type)
        {
            return
                IsPrimitiveOrEnum(type) ||
                    (type.IsValueType &&
                    !type.IsGenericTypeDefinition &&
                    GetAllFields(type).All(fi => IsSerializable(fi) && IsPrimitiveOrEnum(fi.FieldType)));
        }

        // true if the type is either primitive (byte, int etc.) or enum.
        internal static bool IsPrimitiveOrEnum(Type type)
        {
            return type.IsPrimitive || type.IsEnum;
        }

        // true if the type is immutable
        internal static bool IsImmutableType(Type type)
        {
            return
                type == typeof(string) ||
                IsSimpleValueType(type) ||
                    (!type.IsArray &&
                    !type.IsGenericTypeDefinition &&
                    GetAllFields(type).All(fi => IsSerializable(fi) && fi.IsInitOnly && IsImmutableType(fi.FieldType)));

            // when changing this definition, make sure a Shared<> instance is not recognized as immutable
        }

        /// <summary>
        /// Determines whether an instance of the specified type must be cleared prior to reuse
        /// as a cloning or deserialization target.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="serializers">A registry of known serializers.</param>
        /// <returns>true if the type requires clearing prior to reuse; otherwise, false.</returns>
        internal static bool IsClearRequired(Type type, KnownSerializers serializers)
        {
            // first check whether a cached result exists
            var handler = serializers.GetUntypedHandler(type);
            if (handler.IsClearRequired.HasValue)
            {
                return handler.IsClearRequired.Value;
            }

            // Initialize the cached value so that we know that we have seen it if we encounter it again during
            // the current evaluation (i.e. if the object graph contains a cycle back to the current type).
            handler.IsClearRequired = false;

            // Skip evaluation for simple value types and strings. Otherwise, clearing is only required
            // for Shared<> types, arrays of Shared<>, or object graphs which may contain a Shared<>.
            bool result =
                !IsSimpleValueType(type) &&
                type != typeof(string) &&
                    ((type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Shared<>)) ||
                    (type.IsArray && IsClearRequired(type.GetElementType(), serializers)) ||
                    GetSerializableFields(type).Any(fi => IsClearRequired(fi.FieldType, serializers)));

            // cache the result
            handler.IsClearRequired = result;

            return result;
        }

        internal static int SizeOf(Type type)
        {
            var method = new DynamicMethod("SizeOf", typeof(int), null, typeof(Serializer));
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Sizeof, type);
            il.Emit(OpCodes.Ret);
            var sizeOf = (Func<int>)method.CreateDelegate(typeof(Func<int>));
            return sizeOf();
        }

        internal static IEnumerable<FieldInfo> GetAllFields(Type type)
        {
            // we need to get all fields, public and private, from the type and all its base types
            // Type.GetFields only returns public fields from base types, so we need to walk the hierarchy and ask for declared fields on every type
            // This might appear to produce duplicates for private fields that are hidden by derived classes
            // since fields are always hid-by-sig, but they still produce different fieldinfo objects, and we want them all.
            IEnumerable<FieldInfo> allFields = type.GetFields(AllDeclaredFlags);
            type = type.BaseType;
            while (type != null && type != typeof(object))
            {
                // add all fields from all base classes up the inheritance chain
                var privateInherited = type.GetFields(AllDeclaredFlags);
                allFields = allFields.Union(privateInherited);
                type = type.BaseType;
            }

            return allFields;
        }

        internal static bool IsSerializable(FieldInfo field)
        {
            return !field.FieldType.IsPointer && !field.IsNotSerialized;
        }

        internal static IEnumerable<FieldInfo> GetSerializableFields(Type type)
        {
            return GetAllFields(type).Where(fi => IsSerializable(fi));
        }

        internal static SerializationHandler GetHandlerFromIndex(SerializationContext context, int index)
        {
            return context.Serializers.GetUntypedHandler(index);
        }

        // insert calls to this method to verify inlining in release mode
        internal static void DumpStack()
        {
            // add this to generated code
            // il.Emit(OpCodes.Call, typeof(Generator).GetMethod(nameof(DumpStack), BindingFlags.Static | BindingFlags.NonPublic));
            Console.WriteLine(new System.Diagnostics.StackTrace());
        }

        internal static void ValidateMember(Type owner, MemberInfo mi)
        {
            if (mi.MemberType == MemberTypes.Property)
            {
                var prop = (PropertyInfo)mi;

                // must have get and set
                if (!prop.CanRead)
                {
                    throw new SerializationException($"There was an error deserializing the object of type {owner.AssemblyQualifiedName}. The property {mi.Name} in type {mi.DeclaringType.AssemblyQualifiedName} is missing the 'get' method.");
                }

                if (!prop.CanWrite)
                {
                    throw new SerializationException($"There was an error deserializing the object of type {owner.AssemblyQualifiedName}. The property {mi.Name} in type {mi.DeclaringType.AssemblyQualifiedName} is missing the 'set' method.");
                }
            }
            else if (mi.MemberType != MemberTypes.Field)
            {
                throw new SerializationException($"Member {mi.Name} in type {mi.DeclaringType.AssemblyQualifiedName} is not a field or property.");
            }
        }
    }
}