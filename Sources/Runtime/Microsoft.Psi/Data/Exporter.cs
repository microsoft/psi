// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Component that writes messages to a multi-stream store.
    /// The store can be backed by a file on disk, can be ephemeral (in-memory) for inter-process communication
    /// or can be a network protocol for cross-machine communication.
    /// Instances of this component can be created using <see cref="Store.Create(Pipeline, string, string, bool, Serialization.KnownSerializers)"/>.
    /// </summary>
    public sealed class Exporter : IDisposable
    {
        private readonly StoreWriter writer;
        private readonly Merger<Message<BufferReader>, string> merger;
        private readonly Pipeline pipeline;
        private readonly Receiver<Message<BufferReader>> writerInput;
        private readonly ManualResetEvent throttle = new ManualResetEvent(true);
        private KnownSerializers serializers;

        /// <summary>
        /// Initializes a new instance of the <see cref="Exporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline that owns this instance.</param>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store</param>
        /// <param name="createSubdirectory">If true, a numbered sub-directory is created for this store</param>
        /// <param name="serializers">
        /// A collection of known serializers, or null to infer it from the data being written to the store.
        /// The known serializer set can be accessed and modified afterwards via the <see cref="Serializers"/> property.
        /// </param>
        internal Exporter(Pipeline pipeline, string name, string path, bool createSubdirectory = true, KnownSerializers serializers = null)
        {
            this.pipeline = pipeline;
            this.serializers = serializers ?? new KnownSerializers();
            this.writer = new StoreWriter(name, path, createSubdirectory);

            // write the version info
            this.writer.WriteToCatalog(this.serializers.RuntimeVersion);

            // copy the schemas present so far and also make sure the catalog captures schemas added in the future
            this.serializers.SchemaAdded += (o, e) => this.writer.WriteToCatalog(e);
            foreach (var schema in this.serializers.Schemas)
            {
                this.writer.WriteToCatalog(schema);
            }

            this.merger = new Merger<Message<BufferReader>, string>(pipeline);
            this.writerInput = pipeline.CreateReceiver<Message<BufferReader>>(this, (m, e) => this.writer.Write(m.Data, m.Envelope), nameof(this.writerInput));
            this.merger.Select(this.ThrottledMessages).PipeTo(this.writerInput, DeliveryPolicy.Unlimited);
        }

        /// <summary>
        /// Used to define which emitters should be written to the store using WriteEmitters
        /// </summary>
        public enum WriteWhichEmitters
        {
            /// <summary>
            /// Write all emitters.
            /// </summary>
            All,

            /// <summary>
            /// Write specified emitters.
            /// </summary>
            Specified,

            /// <summary>
            /// Write connected emitters.
            /// </summary>
            Connected,

            /// <summary>
            /// Write attributed emitters.
            /// </summary>
            Attributed
        }

        /// <summary>
        /// Gets the name of the store being written to
        /// </summary>
        public string Name => this.writer.Name;

        /// <summary>
        /// Gets the path to the store being written to if the store is persisted to disk, or null if the store is volatile.
        /// </summary>
        public string Path => this.writer.Path;

        /// <summary>
        /// Gets the set of types that this Importer can deserialize.
        /// Types can be added or re-mapped using the <see cref="KnownSerializers.Register{T}(string)"/> method.
        /// </summary>
        public KnownSerializers Serializers => this.serializers;

        /// <summary>
        /// Gets the event that allows remoting to throttle data reading to match a specified network bandwidth.
        /// </summary>
        internal ManualResetEvent Throttle => this.throttle;

        /// <summary>
        /// Closes the store.
        /// </summary>
        public void Dispose()
        {
            this.writer.Dispose();
        }

        /// <summary>
        /// Writes the messages from the specified stream to the matching storage stream in this store.
        /// </summary>
        /// <typeparam name="T">The type of messages in the stream</typeparam>
        /// <param name="source">The source stream to write</param>
        /// <param name="name">The name of the storage stream.</param>
        /// <param name="largeMessages">Indicates whether the stream contains large messages (typically >4k). If true, the messages will be written to the large message file.</param>
        /// <param name="policy">An optional delivery policy</param>
        public void Write<T>(Emitter<T> source, string name, bool largeMessages = false, DeliveryPolicy policy = null)
        {
            // make sure we can serialize this type
            var handler = this.serializers.GetHandler<T>();

            // add another input to the merger to hook up the serializer to
            // and check for duplicate names in the process
            var mergeInput = this.merger.Add(name);

            // name the stream if it's not already named
            source.Name = source.Name ?? name;

            // tell the writer to write the serialized stream
            var meta = this.writer.OpenStream(source.Id, name, largeMessages, handler.Name);

            // register this stream with the store catalog
            this.pipeline.ConfigurationStore.Set(Store.StreamMetadataNamespace, name, meta);

            // hook up the serializer
            var serializer = new SerializerComponent<T>(this.pipeline, this.serializers);
            serializer.PipeTo(mergeInput, DeliveryPolicy.Immediate);
            source.PipeTo(serializer, policy ?? DeliveryPolicy.Unlimited);
        }

        /// <summary>
        /// Given a pipeline source object (such as KinectSensor) this method will
        /// write all emitters that match the requested properties to the current store.
        /// This is useful for writing streams from a sensor to a store for later playback
        /// via Importer.OpenAs().
        /// </summary>
        /// <typeparam name="T">Type of source to get emitters from (e.g. KinectSensor)</typeparam>
        /// <param name="sensor">The source to write data from</param>
        /// <param name="streamsToEmit">Used to indicate which emitters should be written</param>
        /// <param name="streamNames">List of stream names if streamToEmit==WriteWhichEmitters.Specified</param>
        public void WriteEmitters<T>(T sensor, WriteWhichEmitters streamsToEmit = WriteWhichEmitters.Connected, string[] streamNames = null)
        {
            var objType = typeof(T);
            var objProps = objType.GetProperties();
            foreach (var f in objProps)
            {
                var objPropType = f.PropertyType;
                if (objPropType.GetInterface(nameof(IEmitter)) != null)
                {
                    bool emit = false;
                    switch (streamsToEmit)
                    {
                        case WriteWhichEmitters.All:
                            emit = true;
                            break;
                        case WriteWhichEmitters.Specified:
                            if (streamNames != null)
                            {
                                foreach (var name in streamNames)
                                {
                                    if (name == f.Name)
                                    {
                                        emit = true;
                                        break;
                                    }
                                }
                            }

                            break;
                        case WriteWhichEmitters.Connected:
                            var theEmitter = f.GetValue(sensor);
                            var hasSubscribers = objPropType.GetProperty("HasSubscribers").GetValue(theEmitter);
                            if ((bool)hasSubscribers)
                            {
                                emit = true;
                            }

                            break;
                        case WriteWhichEmitters.Attributed:
                            if (Attribute.IsDefined(f, typeof(WriteStreamAttribute)))
                            {
                                emit = true;
                            }

                            break;
                    }

                    if (emit)
                    {
                        var propInfo = typeof(T).GetProperty(f.Name);
                        var writeMethods = typeof(Exporter).GetMethods();
                        System.Reflection.MethodInfo writeMethod = null;
                        foreach (var mi in writeMethods)
                        {
                            if (mi.Name == nameof(this.Write) && mi.IsGenericMethod == true)
                            {
                                writeMethod = mi;
                                break;
                            }
                        }

                        if (writeMethod != null)
                        {
                            var writeStream = writeMethod.MakeGenericMethod(objPropType.GetGenericArguments());
                            object[] args = { propInfo.GetValue(sensor), f.Name, true, Microsoft.Psi.DeliveryPolicy.LatestMessage };
                            writeStream.Invoke(this, args);
                        }
                    }
                }
            }
        }

        internal void Write(Emitter<Message<BufferReader>> source, PsiStreamMetadata meta, DeliveryPolicy policy = null)
        {
            var mergeInput = this.merger.Add(meta.Name); // this checks for duplicates
            this.writer.OpenStream(meta);
            Operators.PipeTo(source, mergeInput, policy);
        }

        private Message<BufferReader> ThrottledMessages(ValueTuple<string, Message<Message<BufferReader>>> messages)
        {
            this.Throttle.WaitOne();
            return messages.Item2.Data;
        }

        /// <summary>
        /// Class that defines an attribute used to indicate which Emitters should be
        /// written when WriteEmitters is called.
        /// Clients may place the [WriteStream] attribute on each emitter property
        /// they want written.
        /// </summary>
        public class WriteStreamAttribute : Attribute
        {
        }
    }
}
