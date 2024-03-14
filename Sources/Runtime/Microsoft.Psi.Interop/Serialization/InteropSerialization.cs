// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Diagnostics;
    using static Microsoft.Psi.Diagnostics.PipelineDiagnostics;

    /// <summary>
    /// Provides helper methods for interop serialization.
    /// </summary>
    public static class InteropSerialization
    {
        /// <summary>
        /// Writes an interop serializable object to a specified writer.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="value">The object to write.</param>
        /// <param name="writer">The writer to write the object to.</param>
        public static void Write<T>(T value, BinaryWriter writer)
            where T : IInteropSerializable, new()
        {
            WriteBool(value != null, writer);
            if (value == null)
            {
                return;
            }

            var type = value.GetType();
            if (type != typeof(T))
            {
                WriteBool(true, writer);
                WriteString(type.AssemblyQualifiedName, writer);
            }
            else
            {
                WriteBool(false, writer);
            }

            value.Write(writer);
        }

        /// <summary>
        /// Reads an interop serializable object from a specified reader.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="reader">The reader to read the object from.</param>
        /// <returns>The deserialized object.</returns>
        public static T Read<T>(BinaryReader reader)
            where T : IInteropSerializable, new()
        {
            if (!ReadBool(reader))
            {
                return default;
            }

            T result;
            var isPolymorphic = ReadBool(reader);
            if (isPolymorphic)
            {
                var typeName = ReadString(reader);
                var type = Type.GetType(typeName) ?? throw new Exception("Unknown type encountered during deserialization.");
                result = (T)Activator.CreateInstance(type);
            }
            else
            {
                result = new T();
            }

            result.ReadFrom(reader);
            return result;
        }

        /// <summary>
        /// Write <see cref="Nullable{T}"/> to <see cref="BinaryWriter"/> using an <see cref="Action{T}"/> to write each element.
        /// </summary>
        /// <typeparam name="T">Type of nullable value.</typeparam>
        /// <param name="value">Value to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        /// <param name="writeAction"><see cref="Action{T}"/> used to write each element.</param>
        public static void WriteNullable<T>(T? value, BinaryWriter writer, Action<T> writeAction)
            where T : struct
        {
            WriteBool(value.HasValue, writer);
            if (value.HasValue)
            {
                writeAction(value.Value);
            }
        }

        /// <summary>
        /// Write <see cref="Nullable{T}"/> to <see cref="BinaryWriter"/> using an <see cref="Action{T, BinaryWriter}"/> to write each element.
        /// </summary>
        /// <typeparam name="T">Type of nullable value.</typeparam>
        /// <param name="value">Value to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        /// <param name="writeAction"><see cref="Action{T, BinaryWriter}"/> used to write each element.</param>
        public static void WriteNullable<T>(T? value, BinaryWriter writer, Action<T, BinaryWriter> writeAction)
            where T : struct
            => WriteNullable(value, writer, v => writeAction(v, writer));

        /// <summary>
        /// Read <see cref="Nullable{T}"/> from <see cref="BinaryReader"/> using a <see cref="Func{T}"/> to read each element.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="readFunc"><see cref="Func{T}"/> used to read each element.</param>
        /// <returns><see cref="IEnumerable{T}"/> of elements.</returns>
        public static T? ReadNullable<T>(BinaryReader reader, Func<T> readFunc)
            where T : struct
            => ReadBool(reader) ? readFunc() : null;

        /// <summary>
        /// Read <see cref="Nullable{T}"/> from <see cref="BinaryReader"/> using a <see cref="Func{BinaryReader, T}"/> to read each element.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="readFunc"><see cref="Func{BinaryReader, T}"/> used to read each element.</param>
        /// <returns><see cref="IEnumerable{T}"/> of elements.</returns>
        public static T? ReadNullable<T>(BinaryReader reader, Func<BinaryReader, T> readFunc)
            where T : struct
            => ReadNullable(reader, () => readFunc(reader));

        /// <summary>
        /// Write <see cref="IEnumerable{T}"/> to <see cref="BinaryWriter"/> using an <see cref="Action{T}"/> to write each element.
        /// </summary>
        /// <typeparam name="T">Type of collection elements.</typeparam>
        /// <param name="collection"><see cref="IEnumerable{T}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        /// <param name="writeAction"><see cref="Action{T}"/> used to write each element.</param>
        public static void WriteCollection<T>(IEnumerable<T> collection, BinaryWriter writer, Action<T> writeAction)
        {
            if (collection == null)
            {
                WriteBool(false, writer);
            }
            else
            {
                WriteBool(true, writer);
                WriteInt32(collection.Count(), writer);
                foreach (var value in collection)
                {
                    writeAction(value);
                }
            }
        }

        /// <summary>
        /// Write <see cref="IEnumerable{T}"/> to <see cref="BinaryWriter"/> using an <see cref="Action{T, BinaryWriter}"/> to write each element.
        /// </summary>
        /// <typeparam name="T">Type of collection elements.</typeparam>
        /// <param name="collection"><see cref="IEnumerable{T}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        /// <param name="writeAction"><see cref="Action{T, BinaryWriter}"/> used to write each element.</param>
        public static void WriteCollection<T>(IEnumerable<T> collection, BinaryWriter writer, Action<T, BinaryWriter> writeAction)
            => WriteCollection(collection, writer, c => writeAction(c, writer));

        /// <summary>
        /// Writes a collection of <see cref="IInteropSerializable"/> objects.
        /// </summary>
        /// <typeparam name="T">The type of the objects.</typeparam>
        /// <param name="collection">The collection to write.</param>
        /// <param name="writer">The writer to write the collection to.</param>
        public static void WriteCollection<T>(IEnumerable<T> collection, BinaryWriter writer)
            where T : IInteropSerializable, new()
            => WriteCollection(collection, writer, v => Write(v, writer));

        /// <summary>
        /// Read <see cref="IEnumerable{T}"/> from <see cref="BinaryReader"/> using a <see cref="Func{T}"/> to read each element.
        /// </summary>
        /// <typeparam name="T">Type of collection elements.</typeparam>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="readFunc"><see cref="Func{T}"/> used to read each element.</param>
        /// <returns><see cref="IEnumerable{T}"/> of elements.</returns>
        public static IEnumerable<T> ReadCollection<T>(BinaryReader reader, Func<T> readFunc)
        {
            if (!ReadBool(reader))
            {
                return null;
            }
            else
            {
                var len = ReadInt32(reader);
                var collection = new T[len];
                for (var i = 0; i < len; i++)
                {
                    collection[i] = readFunc();
                }

                return collection;
            }
        }

        /// <summary>
        /// Read <see cref="IEnumerable{T}"/> from <see cref="BinaryReader"/> using a <see cref="Func{BinaryReader, T}"/> to read each element.
        /// </summary>
        /// <typeparam name="T">Type of collection elements.</typeparam>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="readFunc"><see cref="Func{BinaryReader, T}"/> used to read each element.</param>
        /// <returns><see cref="IEnumerable{T}"/> of elements.</returns>
        public static IEnumerable<T> ReadCollection<T>(BinaryReader reader, Func<BinaryReader, T> readFunc)
            => ReadCollection(reader, () => readFunc(reader));

        /// <summary>
        /// Reads a collection of <see cref="IInteropSerializable"/> objects from a specified reader.
        /// </summary>
        /// <typeparam name="T">The type of the objects.</typeparam>
        /// <param name="reader">The reader to read the collection from.</param>
        /// <returns>The collection of objects.</returns>
        public static IEnumerable<T> ReadCollection<T>(BinaryReader reader)
            where T : IInteropSerializable, new()
            => ReadCollection(reader, () => Read<T>(reader));

        /// <summary>
        /// Write <see cref="Dictionary{TKey, TValue}"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of dictionary key elements.</typeparam>
        /// <typeparam name="TValue">Type of dictionary value elements.</typeparam>
        /// <param name="dictionary"><see cref="Dictionary{TKey, TValue}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        /// <param name="writeKeyAction"><see cref="Action{TKey}"/> used to write each key element.</param>
        /// <param name="writeValueAction"><see cref="Action{TValue}"/> used to write each value element.</param>
        public static void WriteDictionary<TKey, TValue>(
            Dictionary<TKey, TValue> dictionary,
            BinaryWriter writer,
            Action<TKey> writeKeyAction,
            Action<TValue> writeValueAction)
        {
            if (dictionary == null)
            {
                WriteBool(false, writer);
            }
            else
            {
                WriteBool(true, writer);
                WriteInt32(dictionary.Count, writer);
                foreach (var kvp in dictionary)
                {
                    writeKeyAction(kvp.Key);
                    writeValueAction(kvp.Value);
                }
            }
        }

        /// <summary>
        /// Write <see cref="Dictionary{TKey, TValue}"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of dictionary key elements.</typeparam>
        /// <typeparam name="TValue">Type of dictionary value elements.</typeparam>
        /// <param name="dictionary"><see cref="Dictionary{TKey, TValue}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        /// <param name="writeKeyAction"><see cref="Action{TKey, BinaryWriter}"/> used to write each key element.</param>
        /// <param name="writeValueAction"><see cref="Action{TValue, BinaryWriter}"/> used to write each value element.</param>
        public static void WriteDictionary<TKey, TValue>(
            Dictionary<TKey, TValue> dictionary,
            BinaryWriter writer,
            Action<TKey, BinaryWriter> writeKeyAction,
            Action<TValue, BinaryWriter> writeValueAction)
            => WriteDictionary(dictionary, writer, d => writeKeyAction(d, writer), d => writeValueAction(d, writer));

        /// <summary>
        /// Write <see cref="Dictionary{TKey, TValue}"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of dictionary key elements.</typeparam>
        /// <typeparam name="TValue">Type of dictionary value elements.</typeparam>
        /// <param name="dictionary"><see cref="Dictionary{TKey, TValue}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        /// <param name="writeKeyAction"><see cref="Action{TKey}"/> used to write each key element.</param>
        public static void WriteDictionary<TKey, TValue>(
            Dictionary<TKey, TValue> dictionary,
            BinaryWriter writer,
            Action<TKey> writeKeyAction)
            where TValue : IInteropSerializable, new()
            => WriteDictionary(dictionary, writer, writeKeyAction, v => Write(v, writer));

        /// <summary>
        /// Read <see cref="Dictionary{TKey, TValue}"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of dictionary key elements.</typeparam>
        /// <typeparam name="TValue">Type of dictionary value elements.</typeparam>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="readKeyFunc"><see cref="Func{TKey}"/> used to read each key element.</param>
        /// <param name="readValueFunc"><see cref="Func{TValue}"/> used to read each value element.</param>
        /// <returns><see cref="Dictionary{TKey, TValue}"/>.</returns>
        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(
            BinaryReader reader,
            Func<TKey> readKeyFunc,
            Func<TValue> readValueFunc)
        {
            if (!ReadBool(reader))
            {
                return null;
            }
            else
            {
                var result = new Dictionary<TKey, TValue>();
                var count = ReadInt32(reader);
                for (int i = 0; i < count; i++)
                {
                    result.Add(readKeyFunc(), readValueFunc());
                }

                return result;
            }
        }

        /// <summary>
        /// Read <see cref="Dictionary{TKey, TValue}"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of dictionary key elements.</typeparam>
        /// <typeparam name="TValue">Type of dictionary value elements.</typeparam>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="readKeyFunc"><see cref="Func{BinaryReader, TKey}"/> used to read each key element.</param>
        /// <param name="readValueFunc"><see cref="Func{Reader, TValue}"/> used to read each value element.</param>
        /// <returns><see cref="Dictionary{TKey, TValue}"/>.</returns>
        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(
            BinaryReader reader,
            Func<BinaryReader, TKey> readKeyFunc,
            Func<BinaryReader, TValue> readValueFunc)
            => ReadDictionary(reader, () => readKeyFunc(reader), () => readValueFunc(reader));

        /// <summary>
        /// Read <see cref="Dictionary{TKey, TValue}"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of dictionary key elements.</typeparam>
        /// <typeparam name="TValue">Type of dictionary value elements.</typeparam>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="readKeyFunc"><see cref="Func{TKey}"/> used to read each key element.</param>
        /// <returns><see cref="Dictionary{TKey, TValue}"/>.</returns>
        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(
            BinaryReader reader,
            Func<TKey> readKeyFunc)
            where TValue : IInteropSerializable, new()
            => ReadDictionary(reader, readKeyFunc, () => Read<TValue>(reader));

        /// <summary>
        /// Format for <see cref="int"/>.
        /// </summary>
        /// <returns><see cref="Format{Int32}"/> serializer/deserializer.</returns>
        public static Format<int> Int32Format()
            => new (WriteInt32, ReadInt32);

        /// <summary>
        /// Write <see cref="int"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="value">Value to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteInt32(int value, BinaryWriter writer) => writer.Write(value);

        /// <summary>
        /// Read <see cref="int"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="int"/> value.</returns>
        public static int ReadInt32(BinaryReader reader) => reader.ReadInt32();

        /// <summary>
        /// Format for <see cref="PipelineDiagnostics"/>.
        /// </summary>
        /// <returns><see cref="Format{PipelineDiagnostics}"/> serializer/deserializer.</returns>
        public static Format<PipelineDiagnostics> PipelineDiagnosticsFormat()
        {
            /* Pipeline diagnostics structure is a graph with many cycles. The strategy to serialize/deserialize
             * this is to use mutually recursive write/read functions and hashsets/dictionaries of known entities.
             * We write out whole entities (emitters/receivers/elements/...) the first time they're seen while
             * walking the graph, but then to write out only the ID when seen again in the future. Then, while
             * deserializing, we remember entities that have already been deserialized and upon seeing an ID for
             * which we have a cached entity, we stop and return the cached instance. One catch is that instanced
             * may be in the process of being deserialized when referenced to them are seen. In this case, a
             * partially complete instance is used that is later completed by the time deserialization has finished.
             */
            return new (
                (pipelineDiagnostics, writer) =>
                {
                    WriteBool(pipelineDiagnostics != null, writer);
                    if (pipelineDiagnostics == null)
                    {
                        return;
                    }

                    var knownEmitterIds = new HashSet<int>();
                    var knownReceiverIds = new HashSet<int>();
                    var knownPipelineElementIds = new HashSet<int>();
                    var knownPipelineIds = new HashSet<int>();

                    void WriteEmitterDiagnostics(EmitterDiagnostics emitterDiagnostics)
                    {
                        WriteBool(emitterDiagnostics != null, writer);
                        if (emitterDiagnostics == null)
                        {
                            return;
                        }

                        WriteInt32(emitterDiagnostics.Id, writer);

                        // serialize only if an instance with this ID has not already been serialized
                        if (!knownEmitterIds.Contains(emitterDiagnostics.Id))
                        {
                            knownEmitterIds.Add(emitterDiagnostics.Id);
                            WriteString(emitterDiagnostics.Name, writer);
                            WriteString(emitterDiagnostics.Type, writer);
                            WritePipelineElementDiagnostics(emitterDiagnostics.PipelineElement);
                            WriteCollection(emitterDiagnostics.Targets, writer, WriteReceiverDiagnostics);
                        }
                    }

                    void WriteReceiverDiagnostics(ReceiverDiagnostics receiverDiagnostics)
                    {
                        WriteBool(receiverDiagnostics != null, writer);
                        if (receiverDiagnostics == null)
                        {
                            return;
                        }

                        WriteInt32(receiverDiagnostics.Id, writer);

                        // serialize only if an instance with this ID has not already been serialized
                        if (!knownReceiverIds.Contains(receiverDiagnostics.Id))
                        {
                            knownReceiverIds.Add(receiverDiagnostics.Id);
                            WriteString(receiverDiagnostics.ReceiverName, writer);
                            WriteString(receiverDiagnostics.DeliveryPolicyName, writer);
                            WriteString(receiverDiagnostics.TypeName, writer);
                            WriteBool(receiverDiagnostics.ReceiverIsThrottled, writer);
                            writer.Write(receiverDiagnostics.LastDeliveryQueueSize);
                            writer.Write(receiverDiagnostics.AvgDeliveryQueueSize);
                            WriteInt32(receiverDiagnostics.TotalMessageEmittedCount, writer);
                            WriteInt32(receiverDiagnostics.WindowMessageEmittedCount, writer);
                            WriteInt32(receiverDiagnostics.TotalMessageProcessedCount, writer);
                            WriteInt32(receiverDiagnostics.WindowMessageProcessedCount, writer);
                            WriteInt32(receiverDiagnostics.TotalMessageDroppedCount, writer);
                            WriteInt32(receiverDiagnostics.WindowMessageDroppedCount, writer);
                            writer.Write(receiverDiagnostics.LastMessageCreatedLatency);
                            writer.Write(receiverDiagnostics.AvgMessageCreatedLatency);
                            writer.Write(receiverDiagnostics.LastMessageEmittedLatency);
                            writer.Write(receiverDiagnostics.AvgMessageEmittedLatency);
                            writer.Write(receiverDiagnostics.LastMessageReceivedLatency);
                            writer.Write(receiverDiagnostics.AvgMessageReceivedLatency);
                            writer.Write(receiverDiagnostics.LastMessageProcessTime);
                            writer.Write(receiverDiagnostics.AvgMessageProcessTime);
                            writer.Write(receiverDiagnostics.LastMessageSize);
                            writer.Write(receiverDiagnostics.AvgMessageSize);
                            WritePipelineElementDiagnostics(receiverDiagnostics.PipelineElement);
                            WriteEmitterDiagnostics(receiverDiagnostics.Source);
                        }
                    }

                    void WritePipelineElementDiagnostics(PipelineElementDiagnostics pipelineElementDiagnostics)
                    {
                        WriteBool(pipelineElementDiagnostics != null, writer);
                        if (pipelineElementDiagnostics == null)
                        {
                            return;
                        }

                        WriteInt32(pipelineElementDiagnostics.Id, writer);

                        // serialize only if an instance with this ID has not already been serialized
                        if (!knownPipelineElementIds.Contains(pipelineElementDiagnostics.Id))
                        {
                            knownPipelineElementIds.Add(pipelineElementDiagnostics.Id);
                            WriteString(pipelineElementDiagnostics.Name, writer);
                            WriteString(pipelineElementDiagnostics.TypeName, writer);
                            WriteInt32((int)pipelineElementDiagnostics.Kind, writer);
                            WriteBool(pipelineElementDiagnostics.IsRunning, writer);
                            WriteBool(pipelineElementDiagnostics.Finalized, writer);
                            WriteString(pipelineElementDiagnostics.DiagnosticState, writer);
                            WriteInt32(pipelineElementDiagnostics.PipelineId, writer);
                            WriteCollection(pipelineElementDiagnostics.Emitters, writer, WriteEmitterDiagnostics);
                            WriteCollection(pipelineElementDiagnostics.Receivers, writer, WriteReceiverDiagnostics);
                            WritePipelineDiagnostics(pipelineElementDiagnostics.RepresentsSubpipeline);
                            WritePipelineElementDiagnostics(pipelineElementDiagnostics.ConnectorBridgeToPipelineElement);
                        }
                    }

                    void WritePipelineDiagnostics(PipelineDiagnostics diagnostics)
                    {
                        WriteBool(diagnostics != null, writer);
                        if (diagnostics == null)
                        {
                            return;
                        }

                        WriteInt32(diagnostics.Id, writer);

                        // serialize only if an instance with this ID has not already been serialized
                        if (!knownPipelineIds.Contains(diagnostics.Id))
                        {
                            knownPipelineIds.Add(diagnostics.Id);
                            WriteString(diagnostics.Name, writer);
                            WriteBool(diagnostics.IsPipelineRunning, writer);
                            WritePipelineDiagnostics(diagnostics.ParentPipelineDiagnostics);
                            WriteCollection(diagnostics.SubpipelineDiagnostics, writer, WritePipelineDiagnostics);
                            WriteCollection(diagnostics.PipelineElements, writer, WritePipelineElementDiagnostics);
                        }
                    }

                    WritePipelineDiagnostics(pipelineDiagnostics);
                },
                (reader) =>
                {
                    if (!ReadBool(reader))
                    {
                        return null;
                    }

                    var knownEmitters = new Dictionary<int, EmitterDiagnostics>();
                    var knownReceivers = new Dictionary<int, ReceiverDiagnostics>();
                    var knownPipelineElements = new Dictionary<int, PipelineElementDiagnostics>();
                    var knownPipelines = new Dictionary<int, PipelineDiagnostics>();

                    EmitterDiagnostics ReadEmitterDiagnostics()
                    {
                        if (!ReadBool(reader))
                        {
                            return null;
                        }

                        var id = ReadInt32(reader);
                        if (knownEmitters.TryGetValue(id, out var emitter))
                        {
                            // if this ID is already known, merely return the known instance (potentially partially complete)
                            return emitter;
                        }

                        // construct and add a partially complete instance before recursing
                        emitter = new EmitterDiagnostics(
                            id,
                            ReadString(reader), // name
                            ReadString(reader), // type
                            null, // pipelineElement
                            null); // targets
                        knownEmitters.Add(id, emitter);

                        // recurse and complete assignment of instance fields
                        emitter.PipelineElement = ReadPipelineElementDiagnostics();
                        emitter.Targets = ReadCollection(reader, ReadReceiverDiagnostics).ToArray();

                        return emitter;
                    }

                    ReceiverDiagnostics ReadReceiverDiagnostics()
                    {
                        if (!ReadBool(reader))
                        {
                            return null;
                        }

                        var id = ReadInt32(reader);
                        if (knownReceivers.TryGetValue(id, out var receiver))
                        {
                            // if this ID is already known, merely return the known instance (potentially partially complete)
                            return receiver;
                        }

                        // construct and add a partially complete instance before recursing
                        receiver = new ReceiverDiagnostics(
                            id,
                            ReadString(reader), // receiverName
                            ReadString(reader), // deliverPolicyName
                            ReadString(reader), // typeName
                            ReadBool(reader), // receiverIsThrottled
                            reader.ReadDouble(), // lastDeliveryQueueSize
                            reader.ReadDouble(), // avgDeliveryQueueSize
                            ReadInt32(reader), // totalMessageEmittedCount
                            ReadInt32(reader), // windowMessageEmittedCount
                            ReadInt32(reader), // totalMessageProcessedCount
                            ReadInt32(reader), // windowMessageProcessedCount
                            ReadInt32(reader), // totalMessageDroppedCount
                            ReadInt32(reader), // windowMessageDroppedCount
                            reader.ReadDouble(), // lastMessageCreatedLatency
                            reader.ReadDouble(), // avgMessageCreatedLatency
                            reader.ReadDouble(), // lastMessageEmittedLatency
                            reader.ReadDouble(), // avgMessageEmittedLatency
                            reader.ReadDouble(), // lastMessageReceivedLatency
                            reader.ReadDouble(), // avgMessageReceivedLatency
                            reader.ReadDouble(), // lastMessageProcessTime
                            reader.ReadDouble(), // avgMessageProcessTime
                            reader.ReadDouble(), // lastMessageSize
                            reader.ReadDouble(), // avgMessageSize
                            null, // pipelineElement
                            null); // source
                        knownReceivers.Add(id, receiver);

                        // recurse and complete assignment of instance fields
                        receiver.PipelineElement = ReadPipelineElementDiagnostics();
                        receiver.Source = ReadEmitterDiagnostics();

                        return receiver;
                    }

                    PipelineElementDiagnostics ReadPipelineElementDiagnostics()
                    {
                        if (!ReadBool(reader))
                        {
                            return null;
                        }

                        var id = ReadInt32(reader);
                        if (knownPipelineElements.TryGetValue(id, out var pipelineElement))
                        {
                            // if this ID is already known, merely return the known instance (potentially partially complete)
                            return pipelineElement;
                        }

                        // construct and add a partially complete instance before recursing
                        pipelineElement = new PipelineElementDiagnostics(
                            id,
                            ReadString(reader), // name
                            ReadString(reader), // typeName
                            (PipelineElementKind)ReadInt32(reader), // kind
                            ReadBool(reader), // isRunning
                            ReadBool(reader), // finalized
                            ReadString(reader), // diagnosticState
                            ReadInt32(reader), // pipelineId
                            null, // emitters
                            null, // receivers
                            null, // representsSubpipeline
                            null); // connectorBridgeToPipelineElement
                        knownPipelineElements.Add(id, pipelineElement);

                        // recurse and complete assignment of instance fields
                        pipelineElement.Emitters = ReadCollection(reader, ReadEmitterDiagnostics).ToArray();
                        pipelineElement.Receivers = ReadCollection(reader, ReadReceiverDiagnostics).ToArray();
                        pipelineElement.RepresentsSubpipeline = ReadPipelineDiagnostics();
                        pipelineElement.ConnectorBridgeToPipelineElement = ReadPipelineElementDiagnostics();

                        return pipelineElement;
                    }

                    PipelineDiagnostics ReadPipelineDiagnostics()
                    {
                        if (!ReadBool(reader))
                        {
                            return null;
                        }

                        var id = ReadInt32(reader);
                        if (knownPipelines.TryGetValue(id, out var pipeline))
                        {
                            // if this ID is already known, merely return the known instance (potentially partially complete)
                            return pipeline;
                        }

                        // construct and add a partially complete instance before recursing
                        pipeline = new PipelineDiagnostics(
                            id,
                            ReadString(reader), // name
                            ReadBool(reader), // isPipelineRunning
                            null, // parentPipelineDiagnostics
                            null, // subpipelineDiagnostics
                            null); // pipelineElements
                        knownPipelines.Add(id, pipeline);

                        // recurse and complete assignment of instance fields
                        pipeline.ParentPipelineDiagnostics = ReadPipelineDiagnostics();
                        pipeline.SubpipelineDiagnostics = ReadCollection(reader, ReadPipelineDiagnostics).ToArray();
                        pipeline.PipelineElements = ReadCollection(reader, ReadPipelineElementDiagnostics).ToArray();

                        return pipeline;
                    }

                    return ReadPipelineDiagnostics();
                });
        }

        /// <summary>
        /// Format for <see cref="DateTime"/>.
        /// </summary>
        /// <returns><see cref="Format{DateTime}"/> serializer/deserializer.</returns>
        public static Format<DateTime> DateTimeFormat()
            => new (WriteDateTime, ReadDateTime);

        /// <summary>
        /// Write <see cref="DateTime"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="dateTime"><see cref="DateTime"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteDateTime(DateTime dateTime, BinaryWriter writer)
            => writer.Write(dateTime.ToBinary());

        /// <summary>
        /// Read <see cref="DateTime"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="DateTime"/>.</returns>
        public static DateTime ReadDateTime(BinaryReader reader)
            => DateTime.FromBinary(reader.ReadInt64());

        /// <summary>
        /// Format for <see cref="TimeSpan"/>.
        /// </summary>
        /// <returns><see cref="Format{TimeSpan}"/> serializer/deserializer.</returns>
        public static Format<TimeSpan> TimeSpanFormat()
            => new (WriteTimeSpan, ReadTimeSpan);

        /// <summary>
        /// Write <see cref="TimeSpan"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="timeSpan"><see cref="TimeSpan"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteTimeSpan(TimeSpan timeSpan, BinaryWriter writer)
            => writer.Write(timeSpan.Ticks);

        /// <summary>
        /// Read <see cref="TimeSpan"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="TimeSpan"/>.</returns>
        public static TimeSpan ReadTimeSpan(BinaryReader reader)
            => TimeSpan.FromTicks(reader.ReadInt64());

        /// <summary>
        /// Format for <see cref="bool"/>.
        /// </summary>
        /// <returns><see cref="Format{Boolean}"/> serializer/deserializer.</returns>
        public static Format<bool> BoolFormat()
            => new (WriteBool, ReadBool);

        /// <summary>
        /// Write <see cref="bool"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="boolean"><see cref="bool"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteBool(bool boolean, BinaryWriter writer)
            => writer.Write((byte)(boolean ? 0xff : 0));

        /// <summary>
        /// Read <see cref="bool"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="bool"/>.</returns>
        public static bool ReadBool(BinaryReader reader)
            => reader.ReadByte() == 0xff;

        /// <summary>
        /// Format for <see cref="Guid"/>.
        /// </summary>
        /// <returns><see cref="Format{Guid}"/> serializer/deserializer.</returns>
        public static Format<Guid> GuidFormat()
            => new (WriteGuid, ReadGuid);

        /// <summary>
        /// Write <see cref="Guid"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="guid"><see cref="Guid"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteGuid(Guid guid, BinaryWriter writer)
            => WriteString(guid.ToString(), writer);

        /// <summary>
        /// Read <see cref="Guid"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="Guid"/>.</returns>
        public static Guid ReadGuid(BinaryReader reader)
            => Guid.Parse(ReadString(reader));

        /// <summary>
        /// Format for <see cref="string"/>.
        /// </summary>
        /// <returns><see cref="Format{String}"/> serializer/deserializer.</returns>
        public static Format<string> StringFormat()
            => new (WriteString, ReadString);

        /// <summary>
        /// Write optional string (may be null) to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="value">String value to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteString(string value, BinaryWriter writer)
        {
            WriteBool(value != null, writer);
            if (value == null)
            {
                return;
            }

            writer.Write(value);
        }

        /// <summary>
        /// Read optional string (may be null) from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns>Optional value (may be null).</returns>
        public static string ReadString(BinaryReader reader)
        {
            if (!ReadBool(reader))
            {
                return null;
            }

            return reader.ReadString();
        }

        /// <summary>
        /// Gets the serialization format for a interop serializable type.
        /// </summary>
        /// <typeparam name="T">The interop serializable type.</typeparam>
        /// <param name="supportPolymorphic">Indicates whether the formatter should support polymorphic instances.</param>
        /// <returns>The format.</returns>
        public static Format<T> GetFormat<T>(bool supportPolymorphic = false)
            where T : IInteropSerializable, new()
            => new (Write, Read<T>);
    }
}
